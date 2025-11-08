// W pliku DataManager.cs
using UnityEngine;
using System.IO;
using Unity.VisualScripting.Dependencies.Sqlite;
using System.Collections.Generic; // Potrzebne do List<T>
using System.Linq; // Potrzebne do .ToList()

public class DataManager : MonoBehaviour
{
    private SQLiteConnection dbConnection;

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

    void OnDestroy()
    {
        if (dbConnection != null)
        {
            dbConnection.Close();
        }
    }
}