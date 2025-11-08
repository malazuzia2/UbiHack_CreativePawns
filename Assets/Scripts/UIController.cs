using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private UserInterfaceSet uiSet;

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