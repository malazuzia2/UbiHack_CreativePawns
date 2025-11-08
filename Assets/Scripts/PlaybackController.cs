// W pliku PlaybackController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlaybackController : MonoBehaviour
{
    [Header("Zale�no�ci")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private LLMService llmService;
    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject info;

    [Header("Sliding Window Statistics")]
    [Tooltip("D�ugo�� okna czasowego (w sekundach), z kt�rego liczone s� statystyki.")]
    [SerializeField] private int statsWindowInSeconds = 1200; 
    private Queue<int> labelHistory = new Queue<int>(); 
    private int[] labelCounts = new int[8]; 

    [Header("Ustawienia Odtwarzania")]
    public int activeSubjectId = 13;
    [Range(0f, 100f)] public float speed = 1f;
    [SerializeField] private int timeJumpInSeconds = 15; 

    [Header("Ustawienia Wydajno�ci")]
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
    private bool isSeeking = false;

    void Update()
    {
        if (activeSubjectId != currentlyPlayingSubjectId)
        {
            StartPlayback();
        }
    }

    private void StartPlayback()
    {
        SeekTo(0);
    }

    public void Rewind()
    {
        int newOffset = Mathf.Max(0, totalOffset - (timeJumpInSeconds * DATA_SAMPLING_RATE));
        SeekTo(newOffset);
    }

    public void Forward()
    {
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
        if (int.TryParse(timeString, out int timeInSeconds))
        {
            int newOffset = timeInSeconds * DATA_SAMPLING_RATE;
            SeekTo(newOffset);
        }
        else
        {
            Debug.LogWarning("Wprowadzono nieprawid�owy format czasu.");
        }
    }
    public void OnSubjectIdChanged(string idString)
    {
        if (int.TryParse(idString, out int newId))
        {
            activeSubjectId = newId;
        }
        else
        {
            Debug.LogWarning("Wprowadzono nieprawid�owy format ID uczestnika.");
        }
    }

    private void SeekTo(int newOffset)
    {
        if (isSeeking) return; 
        isSeeking = true;

        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);

        Debug.Log($"Przeskakuj� do pr�bki: {newOffset}...");

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

        Debug.Log($"Rekonstruuj� statystyki z {historySize} pr�bek...");

        List<int> labelHistoryChunk = dataManager.GetLabelHistoryChunk(currentlyPlayingSubjectId, historyStartOffset, historySize);

        foreach (var label in labelHistoryChunk)
        {
            labelHistory.Enqueue(label);
            labelCounts[label]++;
        }

        Debug.Log("Rekonstrukcja statystyk zako�czona.");

        uiController.ResetUI();
        playbackCoroutine = StartCoroutine(PlaybackDataRoutine());
        isSeeking = false;
    }


    public void OnRequestLlmSummary()
    {
        if (_lastDataSample != null && llmService != null)
        {
            Debug.Log("Wysy�am zapytanie do LLM na ��danie...");
            llmService.RequestSummary(_lastDataSample, response => uiController.SetLlmResponse(response));
        }
        else
        {
            Debug.LogWarning("Brak danych do analizy lub brak serwisu LLM.");
            uiController.SetLlmResponse("Dane nie s� jeszcze gotowe do analizy.");
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

                    labelHistory.Enqueue(data.label);
                    labelCounts[data.label]++;

                    if (labelHistory.Count > statsWindowInSeconds * DATA_SAMPLING_RATE)
                    {
                        int oldestLabel = labelHistory.Dequeue();
                        labelCounts[oldestLabel]--;
                    }

                    if (i == samplesToProcess - 1)
                    {
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
        Debug.Log($"Odtwarzanie zako�czone dla S{currentlyPlayingSubjectId}.");
    }
    private void CalculateBPM(float ecgValue, int currentSampleIndex)
    {
        if (ecgValue > bpmDetectionThreshold && !isAboveThreshold)
        {
            isAboveThreshold = true; 
            float currentTime = (float)currentSampleIndex / DATA_SAMPLING_RATE;

            beatTimestamps.Add(currentTime);

            if (beatTimestamps.Count > bpmAverageCount)
            {
                beatTimestamps.RemoveAt(0);
            }

            if (beatTimestamps.Count > 1)
            {
                List<float> intervals = new List<float>();
                for (int i = 1; i < beatTimestamps.Count; i++)
                {
                    intervals.Add(beatTimestamps[i] - beatTimestamps[i - 1]);
                }
                float averageInterval = intervals.Average();

                if (averageInterval > 0)
                {
                    currentBPM = 60f / averageInterval;
                }
            }
        }
        else if (ecgValue < bpmDetectionThreshold && isAboveThreshold)
        {
            isAboveThreshold = false;
        }
    }
}