// W pliku PlaybackController.cs
using System.Collections;
using System.Collections.Generic; // Potrzebne do List<T>
using System.Linq;
using UnityEngine;

public class PlaybackController : MonoBehaviour
{
    [Header("Zale¿noœci")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private LLMService llmService;
    [SerializeField] private UIController uiController;

    [Header("Ustawienia Odtwarzania")]
    public int activeSubjectId = 13;
    [Range(0f, 100f)] public float speed = 1f;
    [SerializeField] private float llmUpdateInterval = 10f;

    [Header("Ustawienia Wydajnoœci")]
    [SerializeField] private int chunkSizeInSeconds = 5;
    private List<SensorData> currentChunk;
    private int chunkIndex = 0;

    [Header("BPM Calculation Settings")]
    [Tooltip("Wartoœæ EKG, powy¿ej której wykrywamy uderzenie serca.")]
    [SerializeField] private float bpmDetectionThreshold = 0.5f;
    [Tooltip("Ile ostatnich uderzeñ serca uœredniaæ do obliczenia BPM.")]
    [SerializeField] private int bpmAverageCount = 5;

    private bool isAboveThreshold = false; // Czy sygna³ jest aktualnie powy¿ej progu?
    private List<float> beatTimestamps = new List<float>(); // Przechowuje czasy ostatnich uderzeñ
    private float currentBPM = 0f;

    // --- NOWE ZMIENNE DO ZLICZANIA CZASU (w sekundach) ---
    public float stressTime = 0f;
    public float amusementTime = 0f;
    public float relaxationTime = 0f;
    // ---------------------------------------------------

    private int totalOffset = 0;
    private bool isDataFinished = false;
    private float llmUpdateTimer = 0f;
    private Coroutine playbackCoroutine;
    private int currentlyPlayingSubjectId = -1;
    private const int DATA_SAMPLING_RATE = 700;
    private float sampleDebt = 0f;

    void Update()
    {
        if (activeSubjectId != currentlyPlayingSubjectId)
        {
            StartPlayback();
        }
    }

    private void StartPlayback()
    {
        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);

        currentlyPlayingSubjectId = activeSubjectId;
        totalOffset = 0;
        chunkIndex = 0;
        currentChunk = new List<SensorData>();
        isDataFinished = false;
        llmUpdateTimer = 0;
        sampleDebt = 0;
        isAboveThreshold = false;
        beatTimestamps.Clear();
        currentBPM = 0f;


        uiController.ResetUI();
        playbackCoroutine = StartCoroutine(PlaybackDataRoutine());
    }

    private IEnumerator PlaybackDataRoutine()
    {
        Debug.Log($"Rozpoczynam odtwarzanie dla S{currentlyPlayingSubjectId}...");

        while (!isDataFinished)
        {
            if (speed <= 0f)
            {
                yield return null;
                continue;
            }

            // --- NOWA LOGIKA PETLI ---
            sampleDebt += Time.deltaTime * DATA_SAMPLING_RATE * speed;
            int samplesToProcess = Mathf.FloorToInt(sampleDebt);

            if (samplesToProcess > 0)
            {
                sampleDebt -= samplesToProcess;

                for (int i = 0; i < samplesToProcess; i++)
                {
                    // 1. SprawdŸ, czy potrzebujemy nowej paczki danych
                    if (chunkIndex >= currentChunk.Count)
                    {
                        Debug.Log("Pobieram now¹ paczkê danych z bazy...");
                        currentChunk = dataManager.GetDataChunk(currentlyPlayingSubjectId, totalOffset, chunkSizeInSeconds * DATA_SAMPLING_RATE);
                        chunkIndex = 0; // Zresetuj indeks w paczce

                        // Jeœli nowa paczka jest pusta, to koniec danych
                        if (currentChunk.Count == 0)
                        {
                            isDataFinished = true;
                            break;
                        }
                    }

                    // 2. Pobierz dane z szybkiej listy w pamiêci RAM
                    SensorData data = currentChunk[chunkIndex];

                    CalculateBPM(data.ecg, totalOffset);


                    if (totalOffset > 0 && totalOffset % DATA_SAMPLING_RATE == 0)
                    {
                        // Jesteœmy dok³adnie na granicy sekundy, wiêc dodajemy czas do licznika
                        switch (data.label)
                        {
                            case 2: // Stres
                                stressTime++;
                                break;
                            case 3: // Radoœæ
                                amusementTime++;
                                break;
                            case 4: // Relaks
                                relaxationTime++;
                                break;
                        }
                    }


                    // 3. Zaktualizuj UI (tylko w ostatnim obiegu pêtli dla wydajnoœci)
                    if (i == samplesToProcess - 1)
                    {
                        // Przeka¿ aktualne liczniki czasu do UIControllera
                        uiController.UpdateUI(data, totalOffset, DATA_SAMPLING_RATE, stressTime, amusementTime, relaxationTime, data.resp, currentBPM);
                    }

                    // 4. Logika LLM
                    llmUpdateTimer += (1.0f / DATA_SAMPLING_RATE);
                    if (llmUpdateTimer >= llmUpdateInterval)
                    {
                        llmService.RequestSummary(data, response => uiController.SetLlmResponse(response));
                        llmUpdateTimer = 0f;
                    }

                    chunkIndex++;
                    totalOffset++;
                }
            }

            if (isDataFinished)
            {
                uiController.SetFinished();
                break;
            }

            yield return null;
        }
        Debug.Log($"Odtwarzanie zakoñczone dla S{currentlyPlayingSubjectId}.");
    }
    private void CalculateBPM(float ecgValue, int currentSampleIndex)
    {
        // Sprawdzamy, czy sygna³ w³aœnie przekroczy³ próg (id¹c w górê)
        if (ecgValue > bpmDetectionThreshold && !isAboveThreshold)
        {
            isAboveThreshold = true; // Zaznacz, ¿e jesteœmy "w piku"

            // Oblicz aktualny czas w sekundach
            float currentTime = (float)currentSampleIndex / DATA_SAMPLING_RATE;

            // Dodaj czas uderzenia do listy
            beatTimestamps.Add(currentTime);

            // Utrzymuj listê w rozs¹dnym rozmiarze
            if (beatTimestamps.Count > bpmAverageCount)
            {
                beatTimestamps.RemoveAt(0);
            }

            // Oblicz BPM, jeœli mamy co najmniej 2 uderzenia
            if (beatTimestamps.Count > 1)
            {
                // Oblicz œredni czas miêdzy uderzeniami na liœcie
                List<float> intervals = new List<float>();
                for (int i = 1; i < beatTimestamps.Count; i++)
                {
                    intervals.Add(beatTimestamps[i] - beatTimestamps[i - 1]);
                }
                float averageInterval = intervals.Average();

                // Przelicz na uderzenia na minutê
                if (averageInterval > 0)
                {
                    currentBPM = 60f / averageInterval;
                }
            }
        }
        // Sprawdzamy, czy sygna³ opad³ z powrotem poni¿ej progu
        else if (ecgValue < bpmDetectionThreshold && isAboveThreshold)
        {
            isAboveThreshold = false; // Zresetuj flagê, gotowi na nastêpny pik
        }
    }
}