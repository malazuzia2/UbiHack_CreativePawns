using UnityEngine;
using System.IO;
using Unity.VisualScripting.Dependencies.Sqlite;

public class DataManager : MonoBehaviour
{
    private SQLiteConnection dbConnection;

    void Awake()
    {
        string dbPath = Path.Combine(Application.streamingAssetsPath, "wesad_processed.sqlite");

#if UNITY_ANDROID && !UNITY_EDITOR
            var loadDb = new WWW(dbPath);
            while (!loadDb.isDone) { }
            string persistentPath = Path.Combine(Application.persistentDataPath, "wesad_processed.sqlite");
            File.WriteAllBytes(persistentPath, loadDb.bytes);
            dbPath = persistentPath;
#endif

        dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
        Debug.Log("DataManager: Po³¹czenie z baz¹ danych udane.");
    }

    public SensorData GetDataAtOffset(int subjectId, int offset)
    {
        return dbConnection.Table<SensorData>()
                           .Where(row => row.subject_id == subjectId)
                           .Skip(offset)
                           .FirstOrDefault();
    }

    void OnDestroy()
    {
        if (dbConnection != null)
        {
            dbConnection.Close();
        }
    }
}