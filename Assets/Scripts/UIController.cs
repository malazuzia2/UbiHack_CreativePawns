using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private UserInterfaceSet uiSet;

    [Header("Breathing Visual Settings")]
    [Tooltip("Jak silny ma być EFEKT KOŃCOWY pulsowania oddechu.")]
    [SerializeField] private float breathEffectMultiplier = 0.5f; // Zwiększone, bo teraz pracujemy w zakresie -1 do 1

    [Tooltip("Jak 'czuła' ma być reakcja na małe zmiany oddechu. Wyższe wartości = bardziej gwałtowna reakcja na starcie.")]
    [SerializeField] private float breathSensitivity = 0.5f; // Nowa, kluczowa zmienna!

    [Tooltip("Jak płynnie obrazek ma zmieniać rozmiar.")]
    [SerializeField] private float breathSmoothing = 5f;

    private Vector3 initialBreathScale;

    void Awake()
    {
        if (uiSet.breathVisual != null)
        {
            initialBreathScale = uiSet.breathVisual.transform.localScale;
        }
    }


    public void UpdateUI(SensorData data, int currentOffset, int samplingRate)
    {
        if (data == null) return;

        float totalSeconds = (float)currentOffset / samplingRate;
        string timeString = string.Format("{0:00}:{1:00}", (int)totalSeconds / 60, (int)totalSeconds % 60);

        uiSet.TimeText.text = $"Czas: {timeString}";
        uiSet.SubjectID.text = $"ID Uczestnika: {data.subject_id}";
        uiSet.LabelText.text = $"Etykieta: {GetLabelName(data.label)}";
        uiSet.EcgText.text = $"EKG: {data.ecg:F4} mV";
        uiSet.EmgText.text = $"EMG: {data.emg:F4} mV";
        uiSet.EdaText.text = $"EDA: {data.eda:F4} μS";
        uiSet.TempText.text = $"Temp: {data.temp:F2} °C";
        uiSet.RespText.text = $"Oddech: {data.resp:F2} %";
        uiSet.Acc_xText.text = $"ACC X: {data.acc_x:F3} g";
        uiSet.Acc_yText.text = $"ACC Y: {data.acc_y:F3} g";
        uiSet.Acc_zText.text = $"ACC Z: {data.acc_z:F3} g";
        UpdateBreathingVisual(data.resp);
    }

    private void UpdateBreathingVisual(float respValue)
    {
        if (uiSet.breathVisual == null) return;

        // KROK 1: Przemnóż surową wartość oddechu przez naszą "czułość".
        // To kontroluje, jak szybko dochodzimy do "płaskiej" części krzywej atan.
        float sensitiveResp = respValue * breathSensitivity;

        // KROK 2: Użyj funkcji Atan, aby "spłaszczyć" sygnał.
        // Wynik Atan jest w radianach (ok. -1.57 do 1.57). Dzielimy go przez Pi/2,
        // aby uzyskać idealnie znormalizowaną wartość w zakresie od -1 do 1.
        float squashedBreath = Mathf.Atan(sensitiveResp) / (Mathf.PI / 2.0f);

        // KROK 3: Użyj tej nowej, "spłaszczonej" wartości do stworzenia modyfikatora skali.
        float breathModifier = squashedBreath * breathEffectMultiplier;

        // KROK 4: Oblicz docelową skalę i płynnie do niej dąż.
        Vector3 targetScale = initialBreathScale + initialBreathScale * breathModifier;

        targetScale.x = Mathf.Max(0.1f, targetScale.x);
        targetScale.y = Mathf.Max(0.1f, targetScale.y);

        uiSet.breathVisual.transform.localScale = Vector3.Lerp(
            uiSet.breathVisual.transform.localScale,
            targetScale,
            Time.deltaTime * breathSmoothing
        );
    }

    public void SetLlmResponse(string text)
    {
        uiSet.LlmResponseText.text = text;
    }

    public void SetFinished()
    {
        uiSet.TimeText.text = "Koniec nagrania";
    }

    private string GetLabelName(int labelId)
    {
        switch (labelId)
        {
            case 2: return "<color=red>STRES</color>";
            case 3: return "<color=yellow>Radość</color>";
            case 4: return "<color=cyan>Relaks</color>";
            case 1: return "Neutralny";
            case 0: return "Przejście";
            default: return "Nieznany";
        }
    }
}