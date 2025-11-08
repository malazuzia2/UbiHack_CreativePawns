using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private UserInterfaceSet uiSet;

    // ... (Zmienne oddechu i akcelerometru bez zmian) ...
    [Header("Breathing Visual Settings")]
    [Tooltip("Obiekt (np. Sfera), którego materiałem będziemy sterować.")]
    [SerializeField] private Renderer breathVisualRenderer;

    [Tooltip("Minimalna wartość oddechu (pełny wydech), która zostanie zmapowana na 0.")]
    [SerializeField] private float minBreathValue = -4f; // Możesz dostosować te wartości

    [Tooltip("Maksymalna wartość oddechu (pełny wdech), która zostanie zmapowana na 1.")]
    [SerializeField] private float maxBreathValue = 4f; // na podstawie obserwacji danych

    [Tooltip("Jak płynnie materiał ma reagować na zmiany oddechu.")]
    [SerializeField] private float breathSmoothing = 5f;


    [Header("Accelerometer Visual Settings")]
    [SerializeField] private float accelerometerSmoothing = 10f;

    [SerializeField] private MeshRenderer stressMaterial;
    [SerializeField] private MeshRenderer breathMaterial;
    [SerializeField] private MeshRenderer heartMaterial;

    // --- NOWA LOGIKA DŁAWIENIA AKTUALIZACJI TEKSTU ---
    [SerializeField] private Renderer heartRenderer; // <<< NOWA ZMIENNA
    private float currentBPM = 0f;
    private Material _heartMaterialInstance;
    private static readonly int DarknessID = Shader.PropertyToID("_Darkness");

    [Header("Performance Settings")]
    [Tooltip("Jak często (w sekundach) aktualizować pola tekstowe. 0.1 = 10 razy na sekundę.")]
    [SerializeField] private float textUpdateInterval = 0.1f;
    private float textUpdateTimer = 0f;
    // --------------------------------------------------

    private Vector3 initialBreathScale;

    void Awake()
    {
        if (uiSet.breathVisual != null) initialBreathScale = uiSet.breathVisual.transform.localScale;
        if (heartRenderer != null)
        {
            _heartMaterialInstance = heartRenderer.material;
        }
    }

    public void UpdateUI(SensorData data, int currentOffset, int samplingRate, float stress, float amusement, float relaxation, float respValue, float bpm)
    {
        if (data == null) return;

        
        // Aktualizacje wizualne (płynne) w każdej klatce
        UpdateBreathingVisual(data.resp);
        UpdateAccelerometerVisual(new Vector3(data.acc_x, data.acc_y, data.acc_z));
        UpdateStressBall(stress, amusement, relaxation);
        currentBPM = bpm;
        // Aktualizacje tekstowe (dławione) co określony czas
        textUpdateTimer += Time.deltaTime;
        if (textUpdateTimer >= textUpdateInterval)
        {
            textUpdateTimer = 0f;
            UpdateTextElements(data, currentOffset, samplingRate, bpm);
        }
    }
    void LateUpdate()
    {
        UpdateHeartVisual();
    }

    // Ta nowa funkcja zawiera tylko logikę aktualizacji tekstu
    private void UpdateTextElements(SensorData data, int currentOffset, int samplingRate, float bpm)
    {
        float totalSeconds = (float)currentOffset / samplingRate;
        string timeString = string.Format("{0:00}:{1:00}", (int)totalSeconds / 60, (int)totalSeconds % 60);

        if (!uiSet.TimeInputField.isFocused)
        {
            uiSet.TimeInputField.text = $"{(int)totalSeconds}";
        }
        if (!uiSet.SubjectIDInputField.isFocused)
        {
            uiSet.SubjectIDInputField.text = $"{data.subject_id}";
        }
        uiSet.LabelText.text = $"Etykieta: {GetLabelName(data.label)}";
        uiSet.EcgText.text = $"EKG: {bpm} BPM";
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
        stressMaterial.material.SetFloat("_StressCount", stress/all);
        stressMaterial.material.SetFloat("_HappyCount", amusement/all);
    }

    private void UpdateHeartVisual()
    {
        if (_heartMaterialInstance == null || currentBPM <= 0) return;

        // KROK 1: Przelicz BPM na częstotliwość (Hz).
        // 60 BPM = 1 uderzenie na sekundę = 1 Hz.
        float frequency = currentBPM / 60.0f;

        // KROK 2: Użyj funkcji sinus z upływającym czasem, aby stworzyć oscylację.
        // Mnożymy czas przez częstotliwość, aby kontrolować prędkość pulsowania.
        // Wynik funkcji Sin jest w zakresie od -1 do 1.
        float sinValue = Mathf.Sin(Time.time * frequency * Mathf.PI * 2);

        // KROK 3: Zmapuj wynik z zakresu [-1, 1] do pożądanego zakresu [0, 3].
        // Najpierw mapujemy z [-1, 1] do [0, 1] -> (sinValue + 1) / 2
        // Następnie mnożymy przez 3, aby uzyskać zakres [0, 3].
        float targetDarkness = ((sinValue + 1.0f) / 2.0f) * 3.0f;

        // KROK 4: Ustaw wartość w shaderze.
        _heartMaterialInstance.SetFloat(DarknessID, targetDarkness);
    }


    private void UpdateBreathingVisual(float respValue)
    {

        // KROK 1: Zmapuj wartość oddechu z zakresu [min, max] do zakresu [0, 1].
        // Funkcja InverseLerp robi to za nas. Mathf.Clamp zapewnia, że surowa wartość
        // nie wyjdzie poza zdefiniowany przez nas zakres.
        float clampedResp = Mathf.Clamp(respValue, minBreathValue, maxBreathValue);
        float targetPulse = Mathf.InverseLerp(minBreathValue, maxBreathValue, clampedResp);

        // KROK 2: Odczytaj obecną wartość pulsowania z materiału.
        float currentPulse = breathMaterial.material.GetFloat("_pulsing");

        // KROK 3: Płynnie interpoluj w kierunku nowej wartości.
        float smoothedPulse = Mathf.Lerp(currentPulse, targetPulse, Time.deltaTime * breathSmoothing);

        // KROK 4: Ustaw nową, płynną wartość w shaderze.
        breathMaterial.material.SetFloat("_pulsing", smoothedPulse);
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
        if (uiSet.TimeInputField != null) uiSet.TimeInputField.text = "0";
        uiSet.LlmResponseText.text = "Oczekiwanie na dane...";
        //if (uiSet.SubjectIDInputField != null) uiSet.SubjectIDInputField.text = GetComponentInParent<PlaybackController>().activeSubjectId.ToString();
        // Możesz dodać resetowanie pozostałych pól tekstowych, jeśli chcesz
    }
    public void SetLlmResponse(string text)
    {
        uiSet.LlmResponseText.text = text;
    }

    public void SetFinished()
    {
        uiSet.LlmResponseText.text = "Koniec nagrania";
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