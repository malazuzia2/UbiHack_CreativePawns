using UnityEngine;
using System.IO;

// U¿ywamy przestrzeni nazw z biblioteki do³¹czonej do Visual Scripting
using Unity.VisualScripting.Dependencies.Sqlite;

// --- KLUCZOWA ZMIANA TUTAJ ---
// Mówimy bibliotece, ¿e ta klasa mapuje siê na tabelê o nazwie "sensor_data"
[Table("sensor_data")]
public class SensorData
{
    // Nie potrzebujemy klucza g³ównego, jeœli go nie u¿ywamy, ale nie zaszkodzi
    // [PrimaryKey, AutoIncrement]
    // public int Id { get; set; }

    public int subject_id { get; set; }
    public int label { get; set; }
    public float ecg { get; set; }
    public float emg { get; set; }
    public float eda { get; set; }
    public float temp { get; set; }
    public float resp { get; set; }

    // Dodajmy te¿ kolumny akcelerometru, aby by³y gotowe na przysz³oœæ
    public float acc_x { get; set; }
    public float acc_y { get; set; }
    public float acc_z { get; set; }
}

public class DataBase : MonoBehaviour
{
    void Start()
    {
        TestDatabaseConnection();
    }

    void TestDatabaseConnection()
    {
        string dbPath = Path.Combine(Application.streamingAssetsPath, "wesad_processed.sqlite");

#if UNITY_ANDROID && !UNITY_EDITOR
            var loadDb = new WWW(dbPath);
            while (!loadDb.isDone) { }
            string persistentPath = Path.Combine(Application.persistentDataPath, "wesad_processed.sqlite");
            File.WriteAllBytes(persistentPath, loadDb.bytes);
            dbPath = persistentPath;
#endif

        using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly))
        {
            Debug.Log("Po³¹czenie z baz¹ danych udane!");

            Debug.Log("Pobieranie danych dla uczestnika S13...");
            var dataS13 = db.Table<SensorData>().Where(row => row.subject_id == 13).FirstOrDefault();

            if (dataS13 != null)
            {
                Debug.Log($"<color=lime>Sukces! Dane dla S13: ID Uczestnika={dataS13.subject_id}, Etykieta={dataS13.label}, EKG={dataS13.ecg}, Temperatura={dataS13.temp}</color>");
            }
            else
            {
                Debug.LogError("Nie znaleziono danych dla uczestnika S13!");
            }

            Debug.Log("Pobieranie danych dla uczestnika S17...");
            var dataS17 = db.Table<SensorData>().Where(row => row.subject_id == 17).FirstOrDefault();

            if (dataS17 != null)
            {
                Debug.Log($"<color=cyan>Sukces! Dane dla S17: ID Uczestnika={dataS17.subject_id}, Etykieta={dataS17.label}, EKG={dataS17.ecg}, Temperatura={dataS17.temp}</color>");
            }
            else
            {
                Debug.LogError("Nie znaleziono danych dla uczestnika S17!");
            }
        }
    }
}