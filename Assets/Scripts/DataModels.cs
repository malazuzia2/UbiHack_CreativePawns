using TMPro;

using UnityEngine;
using UnityEngine.UI;
using SQLite4Unity3d;
[Table("sensor_data")]
public class SensorData
{
    public int subject_id { get; set; }
    public int label { get; set; }
    public float ecg { get; set; }
    public float emg { get; set; }
    public float eda { get; set; }
    public float temp { get; set; }
    public float resp { get; set; }
    public float acc_x { get; set; }
    public float acc_y { get; set; }
    public float acc_z { get; set; }
}

[System.Serializable]
public class UserInterfaceSet
{
    public TMP_InputField TimeInputField;
    public TMP_InputField SubjectIDInputField; 
    public TextMeshProUGUI LabelText;
    public TextMeshProUGUI Acc_xText;
    public TextMeshProUGUI Acc_yText;
    public TextMeshProUGUI Acc_zText;
    public TextMeshProUGUI EcgText;
    public TextMeshProUGUI EmgText;
    public TextMeshProUGUI EdaText;
    public TextMeshProUGUI TempText;
    public TextMeshProUGUI RespText;
    public TextMeshProUGUI StressText;
    public TextMeshProUGUI RelaxText;
    public TextMeshProUGUI HappyText;
    [Header("Visuals")]
    public RectTransform breathVisual;
    public Transform accelerometerBlock;
    [Header("LLM Output")]
    public TextMeshProUGUI LlmResponseText;
}

[System.Serializable]
public class ApiMessage { public string role; public string content; }
[System.Serializable]
public class ApiRequest { public string model; public ApiMessage[] messages; }
[System.Serializable]
public class ApiChoice { public ApiMessage message; }
[System.Serializable]
public class ApiResponse { public ApiChoice[] choices; }