// W pliku DataManager.cs
using UnityEngine;
using System.IO;
using SQLite4Unity3d;
using System.Collections.Generic; // Potrzebne do List<T>
using System.Linq; // Potrzebne do .ToList()
using SQLite4Unity3d;

public class DataManager : MonoBehaviour
{
    private SQLiteConnection dbConnection;

    [Table("sensor_data")]
    public class LabelOnlyData
    {
        public int label { get; set; }
    }

    void Awake()
    {
        // ... (kod Awake bez zmian) ...
        string dbPath = Path.Combine(Application.streamingAssetsPath, "wesad_processed.sqlite");
#if UNITY_ANDROID && !UNITY_EDITOR
            // ...
#endif
        dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
        Debug.Log("DataManager: Po³¹czenie z baz¹ danych udane.");
    }

    // Ta funkcja zostaje, mo¿e siê przydaæ do czegoœ innego
    public SensorData GetDataAtOffset(int subjectId, int offset)
    {
        return dbConnection.Table<SensorData>()
                           .Where(row => row.subject_id == subjectId)
                           .Skip(offset)
                           .FirstOrDefault();
    }

    // --- NOWA, ZOPTYMALIZOWANA FUNKCJA ---
    public List<SensorData> GetDataChunk(int subjectId, int offset, int chunkSize)
    {
        return dbConnection.Table<SensorData>()
                           .Where(row => row.subject_id == subjectId)
                           .Skip(offset)
                           .Take(chunkSize)
                           .ToList();
    }
    // ------------------------------------
    public List<int> GetLabelHistoryChunk(int subjectId, int offset, int chunkSize)
    {
        // Tworzymy surowe zapytanie SQL.
        string sql = $"SELECT label FROM sensor_data WHERE subject_id = ? ORDER BY rowid LIMIT ? OFFSET ?";

        // U¿yj standardowej funkcji Query, mapuj¹c wynik na nasz¹ now¹, lekk¹ klasê.
        List<LabelOnlyData> resultObjects = dbConnection.Query<LabelOnlyData>(sql, subjectId, chunkSize, offset);

        // Przekonwertuj listê obiektów na prost¹ listê liczb ca³kowitych (int).
        return resultObjects.Select(item => item.label).ToList();
    }
    void OnDestroy()
    {
        if (dbConnection != null)
        {
            dbConnection.Close();
        }
    }
}