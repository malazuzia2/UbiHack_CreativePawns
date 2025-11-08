using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System; // Potrzebne do Action

public class LLMService : MonoBehaviour
{
    [SerializeField] private string openAI_API_Key;
    private const string OPENAI_API_ENDPOINT = "https://api.openai.com/v1/chat/completions";

    public void RequestSummary(SensorData data, Action<string> onComplete)
    {
        StartCoroutine(GetLlmResponse(data, onComplete));
    }

    private IEnumerator GetLlmResponse(SensorData data, Action<string> onComplete)
    {
        onComplete?.Invoke("<i>Analizowanie...</i>");

        string labelName = GetLabelName(data.label);
        string prompt = $"Jesteś empatycznym asystentem zdrowia. Na podstawie tych danych z czujników: EDA={data.eda:F2} μS, Temperatura={data.temp:F1}°C, Oddech={data.resp:F1}%, Napięcie mięśni (EMG)={data.emg:F4}mV. Etykieta ze badania wskazuje, że użytkownik jest w stanie '{labelName}'. Podaj krótką, jednozdaniową obserwację i jednozdaniową, konkretną radę. Mów bezpośrednio do użytkownika.";

        ApiRequest requestBody = new ApiRequest { model = "gpt-3.5-turbo", messages = new ApiMessage[] { new ApiMessage { role = "user", content = prompt } } };
        string jsonRequestBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(OPENAI_API_ENDPOINT, "POST"))
        {
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