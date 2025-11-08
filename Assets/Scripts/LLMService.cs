using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class LLMService : MonoBehaviour
{
    [SerializeField] private string openAI_API_Key;
    private const string OPENAI_API_ENDPOINT = "https://api.openai.com/v1/chat/completions";

    // --- NOWA LOGIKA: Pamiętamy poprzedni stan, aby wykrywać trendy ---
    private SensorData _previousDataSample;
    // -----------------------------------------------------------------

    public void RequestSummary(SensorData currentData, Action<string> onComplete)
    {
        string prompt;

        // Jeśli to pierwsze zapytanie, użyj prostego promptu
        if (_previousDataSample == null)
        {
            prompt = CreateInitialPrompt(currentData);
        }
        // Jeśli mamy poprzednie dane, stwórz prompt porównawczy
        else
        {
            prompt = CreateTrendPrompt(currentData, _previousDataSample);
        }

        // Zapamiętaj obecne dane jako "poprzednie" dla następnego zapytania
        _previousDataSample = currentData;

        StartCoroutine(GetLlmResponse(prompt, onComplete));
    }

    public void ResetMemory()
    {
        _previousDataSample = null;
    }

    private string CreateInitialPrompt(SensorData data)
    {
        string labelName = GetLabelName(data.label);
        return $"Jesteś empatycznym asystentem zdrowia. Aktualny stan użytkownika to '{labelName}', co potwierdzają dane z czujników: EDA (aktywność potowa) = {data.eda:F2} μS, Temperatura skóry = {data.temp:F1}°C. Podaj krótką, jednozdaniową obserwację na temat tego stanu i zadaj otwarte pytanie, które zachęci do refleksji.";
    }

    private string CreateTrendPrompt(SensorData current, SensorData previous)
    {
        string prevLabel = GetLabelName(previous.label);
        string currentLabel = GetLabelName(current.label);
        string edaTrend;
        float edaChange = current.eda - previous.eda;

        // Określ trend dla EDA (najważniejszy wskaźnik stresu)
        if (Mathf.Abs(edaChange) < 0.1) edaTrend = "jest stabilna";
        else if (edaChange > 0) edaTrend = "wzrasta, co może wskazywać na rosnące pobudzenie";
        else edaTrend = "spada, co sugeruje uspokojenie";

        string promptIntro = $"Jesteś empatycznym asystentem zdrowia. Poprzednio stan użytkownika był '{prevLabel}', a teraz jest '{currentLabel}'. Aktywność potowa (EDA) {edaTrend} (z {previous.eda:F2} na {current.eda:F2} μS). ";
        string promptConclusion = "Skomentuj tę zmianę w jednym zdaniu i podaj jedną, konkretną, pozytywną radę na podstawie obecnego stanu.";

        return promptIntro + promptConclusion;
    }

    private IEnumerator GetLlmResponse(string prompt, Action<string> onComplete)
    {
        onComplete?.Invoke("<i>Analizowanie...</i>");

        ApiRequest requestBody = new ApiRequest { model = "gpt-3.5-turbo", messages = new ApiMessage[] { new ApiMessage { role = "user", content = prompt } } };
        string jsonRequestBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(OPENAI_API_ENDPOINT, "POST"))
        {
            // ... (reszta kodu zapytania bez zmian) ...
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {openAI_API_Key}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Błąd API: {request.error}\nOdpowiedź: {request.downloadHandler.text}");
                onComplete?.Invoke("<color=red>Błąd połączenia z AI.</color>");
            }
            else
            {
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                string llmMessage = response.choices[0].message.content.Trim();
                onComplete?.Invoke(llmMessage);
            }
        }
    }

    private string GetLabelName(int labelId)
    {
        switch (labelId) { case 2: return "STRES"; case 3: return "Radość"; case 4: return "Relaks"; case 1: return "Neutralny"; default: return "Nieznany"; }
    }
}