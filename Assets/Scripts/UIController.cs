using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private UserInterfaceSet uiSet;

    // ... (Zmienne oddechu i akcelerometru bez zmian) ...
    [Header("Breathing Visual Settings")]
    [SerializeField] private float breathEffectMultiplier = 0.5f;
    [SerializeField] private float breathSensitivity = 0.5f;
    [SerializeField] private float breathSmoothing = 5f;
    [Header("Accelerometer Visual Settings")]
    [SerializeField] private float accelerometerSmoothing = 10f;

    // --- NOWA LOGIKA DŁAWIENIA AKTUALIZACJI TEKSTU ---
    [Header("Performance Settings")]
    [Tooltip("Jak często (w sekundach) aktualizować pola tekstowe. 0.1 = 10 razy na sekundę.")]
    [SerializeField] private float textUpdateInterval = 0.1f;
    private float textUpdateTimer = 0f;
    // --------------------------------------------------

    public GameObject myMaterial;
    private Vector3 initialBreathScale;

    void Awake()
    {
        if (uiSet.breathVisual != null) initialBreathScale = uiSet.breathVisual.transform.localScale;
    }

    public void UpdateUI(SensorData data, int currentOffset, int samplingRate, float stress, float amusement, float relaxation)
    {
        if (data == null) return;

        
        // Aktualizacje wizualne (płynne) w każdej klatce
        UpdateBreathingVisual(data.resp);
        UpdateAccelerometerVisual(new Vector3(data.acc_x, data.acc_y, data.acc_z));
        UpdateStressBall(stress, amusement, relaxation);

        // Aktualizacje tekstowe (dławione) co określony czas
        textUpdateTimer += Time.deltaTime;
        if (textUpdateTimer >= textUpdateInterval)
        {
            textUpdateTimer = 0f;
            UpdateTextElements(data, currentOffset, samplingRate);
        }
    }

    // Ta nowa funkcja zawiera tylko logikę aktualizacji tekstu
    private void UpdateTextElements(SensorData data, int currentOffset, int samplingRate)
    {
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
    }


    private void UpdateStressBall(float stress, float amusement, float relaxation)
    {
        float all = stress + amusement + relaxation;
        if(all == 0) all = 1;
        myMaterial.GetComponent<MeshRenderer>().material.SetFloat("StressCount", stress/all);
        myMaterial.GetComponent<MeshRenderer>().material.SetFloat("HappyCount", amusement/all);
    }

    // --- NOWA, NIELINIOWA FUNKCJA WIZUALIZACJI ODDECHU ---
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

    private void UpdateAccelerometerVisual(Vector3 acceleration)
    {
        if (uiSet.accelerometerBlock == null) return;

        // KROK 1: Normalizujemy wektor, aby uzyskać czysty kierunek grawitacji.
        // Jeśli wektor jest zerowy, nie robimy nic, aby uniknąć błędów.
        if (acceleration.sqrMagnitude < 0.001f) return;
        Vector3 gravityDirection = acceleration.normalized;

        // KROK 2: Użyj Quaternion.FromToRotation.
        // Ta instrukcja oblicza rotację potrzebną, aby obrócić wektor Vector3.down
        // (naturalny "dół" obiektu w Unity) tak, aby wskazywał w tym samym kierunku,
        // co nasz wektor grawitacji z czujnika.
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, gravityDirection);

        // KROK 3: Płynnie obracamy bloczek w kierunku nowej, pełnej rotacji 3D.
        uiSet.accelerometerBlock.rotation = Quaternion.Slerp(
            uiSet.accelerometerBlock.rotation,
            targetRotation,
            Time.deltaTime * accelerometerSmoothing
        );
    }

    public void ResetUI()
    {
        uiSet.TimeText.text = "Czas: 00:00";
        uiSet.LlmResponseText.text = "Oczekiwanie na dane...";
        // Możesz dodać resetowanie pozostałych pól tekstowych, jeśli chcesz
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