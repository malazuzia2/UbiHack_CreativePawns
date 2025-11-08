//using UnityEngine;
//using System.IO;
//using System.Collections;
//using TMPro;

//using Unity.VisualScripting.Dependencies.Sqlite;

//// Klasa-mapa dla tabeli w bazie danych
//[Table("sensor_data")]
//public class SensorData
//{
//    public int subject_id { get; set; }
//    public int label { get; set; }
//    public float ecg { get; set; }
//    public float emg { get; set; }
//    public float eda { get; set; }
//    public float temp { get; set; }
//    public float resp { get; set; }
//    public float acc_x { get; set; }
//    public float acc_y { get; set; }
//    public float acc_z { get; set; }
//}

//// --- NOWA KLASA POMOCNICZA ---
//// Ta klasa to kontener na wszystkie elementy UI dla jednego użytkownika.
//// Atrybut [System.Serializable] sprawia, że jest widoczna i edytowalna w Inspectorze Unity.
//[System.Serializable]
//public class UserInterfaceSet
//{
//    public TextMeshProUGUI TimeText;
//    public TextMeshProUGUI SubjectID;
//    public TextMeshProUGUI LabelText;
//    public TextMeshProUGUI Acc_xText;
//    public TextMeshProUGUI Acc_yText;
//    public TextMeshProUGUI Acc_zText;
//    public TextMeshProUGUI EcgText;
//    public TextMeshProUGUI EmgText;
//    public TextMeshProUGUI EdaText;
//    public TextMeshProUGUI TempText;
//    public TextMeshProUGUI RespText;
//}


//public class DataBase : MonoBehaviour
//{
//    [Header("UI dla Uczestnika 13")]
//    public UserInterfaceSet uiForSubject13;

//    [Header("UI dla Uczestnika 17")]
//    public UserInterfaceSet uiForSubject17;

//    [Header("Playback Settings")]
//    [Tooltip("Kontroluje prędkość odtwarzania. 1 = normalna, 2 = 2x szybciej, 0 = pauza.")]
//    [Range(0f, 1000000f)]
//    public float speed = 1f;

//    private SQLiteConnection dbConnection;
//    private int currentRowOffsetS13 = 0;
//    private int currentRowOffsetS17 = 0;
//    private bool isS13DataFinished = false;
//    private bool isS17DataFinished = false;
//    private const int DATA_SAMPLING_RATE = 700;

//    void Start()
//    {
//        string dbPath = Path.Combine(Application.streamingAssetsPath, "wesad_processed.sqlite");

//#if UNITY_ANDROID && !UNITY_EDITOR
//            var loadDb = new WWW(dbPath);
//            while (!loadDb.isDone) { }
//            string persistentPath = Path.Combine(Application.persistentDataPath, "wesad_processed.sqlite");
//            File.WriteAllBytes(persistentPath, loadDb.bytes);
//            dbPath = persistentPath;
//#endif

//        dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
//        Debug.Log("Połączenie z bazą danych udane! Rozpoczynam odtwarzanie dla S13 i S17...");

//        StartCoroutine(PlaybackDataRoutine());
//    }

//    IEnumerator PlaybackDataRoutine()
//    {
//        while (!isS13DataFinished || !isS17DataFinished) // Pętla działa, dopóki są dane dla któregokolwiek użytkownika
//        {
//            if (speed <= 0f)
//            {
//                yield return null;
//                continue;
//            }

//            // --- Aktualizacja dla Uczestnika 13 ---
//            if (!isS13DataFinished)
//            {
//                var dataS13 = dbConnection.Table<SensorData>().Where(row => row.subject_id == 13).Skip(currentRowOffsetS13).FirstOrDefault();
//                if (dataS13 != null)
//                {
//                    UpdateUI(uiForSubject13, dataS13, currentRowOffsetS13);
//                    currentRowOffsetS13 += DATA_SAMPLING_RATE;
//                }
//                else
//                {
//                    Debug.Log("Koniec danych dla uczestnika 13.");
//                    uiForSubject13.TimeText.text = "Koniec nagrania";
//                    isS13DataFinished = true;
//                }
//            }

//            // --- Aktualizacja dla Uczestnika 17 ---
//            if (!isS17DataFinished)
//            {
//                var dataS17 = dbConnection.Table<SensorData>().Where(row => row.subject_id == 17).Skip(currentRowOffsetS17).FirstOrDefault();
//                if (dataS17 != null)
//                {
//                    UpdateUI(uiForSubject17, dataS17, currentRowOffsetS17);
//                    currentRowOffsetS17 += DATA_SAMPLING_RATE;
//                }
//                else
//                {
//                    Debug.Log("Koniec danych dla uczestnika 17.");
//                    uiForSubject17.TimeText.text = "Koniec nagrania";
//                    isS17DataFinished = true;
//                }
//            }

//            float waitTime = 1f / speed;
//            yield return new WaitForSeconds(waitTime);
//        }

//        Debug.Log("Odtwarzanie zakończone dla wszystkich uczestników.");
//    }

//    // Ta funkcja teraz przyjmuje zestaw UI do zaktualizowania
//    void UpdateUI(UserInterfaceSet ui, SensorData data, int currentOffset)
//    {
//        float totalSeconds = (float)currentOffset / DATA_SAMPLING_RATE;
//        string timeString = string.Format("{0:00}:{1:00}", (int)totalSeconds / 60, (int)totalSeconds % 60);

//        ui.TimeText.text = $"Czas: {timeString}";
//        ui.SubjectID.text = $"ID Uczestnika: {data.subject_id}";
//        ui.LabelText.text = $"Etykieta: {GetLabelName(data.label)}";
//        ui.EcgText.text = $"EKG: {data.ecg:F4} mV";
//        ui.EmgText.text = $"EMG: {data.emg:F4} mV";
//        ui.EdaText.text = $"EDA: {data.eda:F4} μS";
//        ui.TempText.text = $"Temp: {data.temp:F2} °C";
//        ui.RespText.text = $"Oddech: {data.resp:F2} %";
//        ui.Acc_xText.text = $"ACC X: {data.acc_x:F3} g";
//        ui.Acc_yText.text = $"ACC Y: {data.acc_y:F3} g";
//        ui.Acc_zText.text = $"ACC Z: {data.acc_z:F3} g";
//    }

//    string GetLabelName(int labelId)
//    {
//        switch (labelId)
//        {
//            case 0: return "Przejście";
//            case 1: return "Neutralny";
//            case 2: return "<color=red>STRES</color>";
//            case 3: return "<color=yellow>Radość</color>";
//            case 4: return "<color=cyan>Relaks</color>";
//            default: return "Nieznany";
//        }
//    }

//    void OnDestroy()
//    {
//        if (dbConnection != null)
//        {
//            dbConnection.Close();
//            Debug.Log("Połączenie z bazą danych zostało zamknięte.");
//        }
//    }
//}   