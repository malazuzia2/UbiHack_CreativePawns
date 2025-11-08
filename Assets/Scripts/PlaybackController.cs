// W pliku PlaybackController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Potrzebne do List<T>

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


                    if (totalOffset > 0 && totalOffset % DATA_SAMPLING_RATE == 0)
                    {
                        // Jesteœmy dok³adnie na granicy sekundy, wiêc dodajemy czas do licznika
                        switch (data.label)
                        {
                            case 1: // Neutralny
                                relaxationTime++;
                                break;
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
                        uiController.UpdateUI(data, totalOffset, DATA_SAMPLING_RATE, stressTime, amusementTime, relaxationTime);
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
}