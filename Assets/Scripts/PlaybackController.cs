using UnityEngine;
using System.Collections;

public class PlaybackController : MonoBehaviour
{
    [Header("Zale¿noœci")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private LLMService llmService;
    [SerializeField] private UIController uiControllerS13;
    [SerializeField] private UIController uiControllerS17;

    [Header("Ustawienia Odtwarzania")]
    [Range(0f, 100f)] public float speed = 1f;
    [SerializeField] private float llmUpdateInterval = 10f;

    private int currentRowOffsetS13 = 0;
    private int currentRowOffsetS17 = 0;
    private bool isS13DataFinished = false;
    private bool isS17DataFinished = false;
    private float llmUpdateTimer = 0f;
    private const int DATA_SAMPLING_RATE = 700;

    void Start()
    {
        StartCoroutine(PlaybackDataRoutine());
    }

    private IEnumerator PlaybackDataRoutine()
    {
        while (!isS13DataFinished || !isS17DataFinished)
        {
            if (speed <= 0f)
            {
                yield return null;
                continue;
            }

            // SprawdŸ, czy czas na aktualizacjê LLM
            bool timeForLlmUpdate = llmUpdateTimer >= llmUpdateInterval;

            // Uczestnik 13
            if (!isS13DataFinished)
            {
                var dataS13 = dataManager.GetDataAtOffset(13, currentRowOffsetS13);
                if (dataS13 != null)
                {
                    uiControllerS13.UpdateUI(dataS13, currentRowOffsetS13, DATA_SAMPLING_RATE);
                    if (timeForLlmUpdate) llmService.RequestSummary(dataS13, response => uiControllerS13.SetLlmResponse(response));
                    currentRowOffsetS13 += DATA_SAMPLING_RATE;
                }
                else
                {
                    uiControllerS13.SetFinished();
                    isS13DataFinished = true;
                }
            }

            // Uczestnik 17
            if (!isS17DataFinished)
            {
                var dataS17 = dataManager.GetDataAtOffset(17, currentRowOffsetS17);
                if (dataS17 != null)
                {
                    uiControllerS17.UpdateUI(dataS17, currentRowOffsetS17, DATA_SAMPLING_RATE);
                    if (timeForLlmUpdate) llmService.RequestSummary(dataS17, response => uiControllerS17.SetLlmResponse(response));
                    currentRowOffsetS17 += DATA_SAMPLING_RATE;
                }
                else
                {
                    uiControllerS17.SetFinished();
                    isS17DataFinished = true;
                }
            }

            if (timeForLlmUpdate) llmUpdateTimer = 0f;

            float waitTime = 1f / speed;
            llmUpdateTimer += waitTime;
            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("Odtwarzanie zakoñczone dla wszystkich uczestników.");
    }
}