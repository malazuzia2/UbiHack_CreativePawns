// W pliku PlaybackController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlaybackController : MonoBehaviour
{
    [Header("Zale¿noœci")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private LLMService llmService;
    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject info;

    [Header("Sliding Window Statistics")]
    [Tooltip("D³ugoœæ okna czasowego (w sekundach), z którego liczone s¹ statystyki.")]
    [SerializeField] private int statsWindowInSeconds = 1200; // 1200 sekund = 20 minut
    private Queue<int> labelHistory = new Queue<int>(); // Kolejka przechowuj¹ca etykiety z ostatniego okna
    private int[] labelCounts = new int[8]; // Tablica do zliczania wyst¹pieñ ka¿dej etykiety (0-7)

    [Header("Ustawienia Odtwarzania")]
    public int activeSubjectId = 13;
    [Range(0f, 100f)] public float speed = 1f;
    [SerializeField] private int timeJumpInSeconds = 15; // O ile sekund przeskakiwaæ

    // ... (wszystkie inne zmienne: chunk, bpm, liczniki czasu, itd. bez zmian) ...
    [Header("Ustawienia Wydajnoœci")]
    [SerializeField] private int chunkSizeInSeconds = 5;
    private List<SensorData> currentChunk;
    private int chunkIndex = 0;

    [Header("BPM Calculation Settings")]
    [SerializeField] private float bpmDetectionThreshold = 0.5f;
    [SerializeField] private int bpmAverageCount = 5;
    private bool isAboveThreshold = false;
    private List<float> beatTimestamps = new List<float>();
    private float currentBPM = 0f;

    public float stressTime = 0f;
    public float amusementTime = 0f;
    public float relaxationTime = 1f;

    private int totalOffset = 0;
    private bool isDataFinished = false;
    private SensorData _lastDataSample;
    private Coroutine playbackCoroutine;
    private int currentlyPlayingSubjectId = -1;
    private const int DATA_SAMPLING_RATE = 700;
    private float sampleDebt = 0f;
    private bool isSeeking = false; // Flaga, aby zatrzymaæ odtwarzanie podczas przeskoku

    void Update()
    {
        if (activeSubjectId != currentlyPlayingSubjectId)
        {
            StartPlayback();
        }
    }

    private void StartPlayback()
    {
        SeekTo(0); // U¿yj nowej funkcji SeekTo do startowania/resetowania
    }

    // --- NOWE PUBLICZNE FUNKCJE DLA PRZYCISKÓW ---
    public void Rewind()
    {
        int newOffset = Mathf.Max(0, totalOffset - (timeJumpInSeconds * DATA_SAMPLING_RATE));
        SeekTo(newOffset);
    }

    public void Forward()
    {
        // Tutaj musimy dodaæ logikê, aby nie przeskoczyæ za daleko, ale na razie uproœæmy
        int newOffset = totalOffset + (timeJumpInSeconds * DATA_SAMPLING_RATE);
        SeekTo(newOffset);
    }

    public void ResetTime()
    {
        SeekTo(0);
    }

    public void StopTime()
    {
        speed = 0f;
    }

    public void PlayTime()
    {
        speed = 1f;
    }

    public void ShowInfo()
    {
        info.SetActive(!info.activeSelf);
    }

    public void OnTimeChanged(string timeString)
    {
        // Próbujemy przekonwertowaæ wpisany tekst na liczbê (sekundy)
        if (int.TryParse(timeString, out int timeInSeconds))
        {
            // Przeliczamy sekundy na offset w próbkach
            int newOffset = timeInSeconds * DATA_SAMPLING_RATE;
            SeekTo(newOffset);
        }
        else
        {
            Debug.LogWarning("Wprowadzono nieprawid³owy format czasu.");
        }
    }
    public void OnSubjectIdChanged(string idString)
    {
        if (int.TryParse(idString, out int newId))
        {
            // Po prostu zaktualizuj publiczn¹ zmienn¹.
            // Pêtla Update() w nastêpnej klatce wykryje tê zmianê i
            // automatycznie wywo³a StartPlayback(), który zresetuje czas.
            activeSubjectId = newId;
        }
        else
        {
            Debug.LogWarning("Wprowadzono nieprawid³owy format ID uczestnika.");
        }
    }

    // ---------------------------------------------

    // --- G£ÓWNA NOWA FUNKCJA DO ZARZ¥DZANIA CZASEM ---
    private void SeekTo(int newOffset)
    {
        if (isSeeking) return; // Zapobiegaj wielokrotnym skokom
        isSeeking = true;

        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);

        Debug.Log($"Przeskakujê do próbki: {newOffset}...");

        // Zresetuj stan odtwarzania
        currentlyPlayingSubjectId = activeSubjectId;
        totalOffset = newOffset;
        chunkIndex = 0;
        currentChunk = new List<SensorData>();
        isDataFinished = false;
        sampleDebt = 0;
        isAboveThreshold = false;
        beatTimestamps.Clear();
        currentBPM = 0f;

        labelHistory.Clear();
        System.Array.Clear(labelCounts, 0, labelCounts.Length);

        int historyStartOffset = Mathf.Max(0, newOffset - (statsWindowInSeconds * DATA_SAMPLING_RATE));
        int historySize = newOffset - historyStartOffset;

        Debug.Log($"Rekonstruujê statystyki z {historySize} próbek...");

        // U¿yj nowej, super szybkiej funkcji, aby pobraæ TYLKO etykiety
        List<int> labelHistoryChunk = dataManager.GetLabelHistoryChunk(currentlyPlayingSubjectId, historyStartOffset, historySize);

        foreach (var label in labelHistoryChunk)
        {
            labelHistory.Enqueue(label);
            labelCounts[label]++;
        }

        Debug.Log("Rekonstrukcja statystyk zakoñczona.");
        // --------------------------------------------------------

        uiController.ResetUI();
        playbackCoroutine = StartCoroutine(PlaybackDataRoutine());
        isSeeking = false;
    }


    public void OnRequestLlmSummary()
    {
        if (_lastDataSample != null && llmService != null)
        {
            Debug.Log("Wysy³am zapytanie do LLM na ¿¹danie...");
            llmService.RequestSummary(_lastDataSample, response => uiController.SetLlmResponse(response));
        }
        else
        {
            Debug.LogWarning("Brak danych do analizy lub brak serwisu LLM.");
            uiController.SetLlmResponse("Dane nie s¹ jeszcze gotowe do analizy.");
        }
    }

    private IEnumerator PlaybackDataRoutine()
    {
        Debug.Log($"Rozpoczynam odtwarzanie dla S{currentlyPlayingSubjectId}...");

        while (!isDataFinished)
        {
            if (speed <= 0f) { yield return null; continue; }

            sampleDebt += Time.deltaTime * DATA_SAMPLING_RATE * speed;
            int samplesToProcess = Mathf.FloorToInt(sampleDebt);

            if (samplesToProcess > 0)
            {
                sampleDebt -= samplesToProcess;

                for (int i = 0; i < samplesToProcess; i++)
                {
                    if (chunkIndex >= currentChunk.Count)
                    {
                        currentChunk = dataManager.GetDataChunk(currentlyPlayingSubjectId, totalOffset, chunkSizeInSeconds * DATA_SAMPLING_RATE);
                        chunkIndex = 0;
                        if (currentChunk.Count == 0) { isDataFinished = true; break; }
                    }

                    SensorData data = currentChunk[chunkIndex];

                    CalculateBPM(data.ecg, totalOffset);

                    // --- NOWA LOGIKA AKTUALIZACJI STATYSTYK ---
                    // 1. Dodaj now¹ etykietê do historii
                    labelHistory.Enqueue(data.label);
                    labelCounts[data.label]++;

                    // 2. Jeœli historia jest za d³uga, usuñ najstarsz¹ etykietê
                    if (labelHistory.Count > statsWindowInSeconds * DATA_SAMPLING_RATE)
                    {
                        int oldestLabel = labelHistory.Dequeue();
                        labelCounts[oldestLabel]--;
                    }
                    // ------------------------------------------

                    if (i == samplesToProcess - 1)
                    {
                        // Przelicz zliczenia na sekundy i przeka¿ do UI
                        float stress = (float)labelCounts[2] / DATA_SAMPLING_RATE;
                        float amusement = (float)labelCounts[3] / DATA_SAMPLING_RATE;
                        float relaxation = (float)labelCounts[4] / DATA_SAMPLING_RATE;

                        uiController.UpdateUI(data, totalOffset, DATA_SAMPLING_RATE, stress, amusement, relaxation, data.resp, currentBPM);
                    }

                    _lastDataSample = data;
                    chunkIndex++;
                    totalOffset++;
                }
            }

            if (isDataFinished) { uiController.SetFinished(); break; }

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