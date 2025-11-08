
using UnityEngine;
using System.IO;
using SQLite4Unity3d;
using System.Collections.Generic; 
using System.Linq; 
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
        string dbPath = Path.Combine(Application.streamingAssetsPath, "wesad_processed.sqlite");
#if UNITY_ANDROID && !UNITY_EDITOR
#endif
        dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
        Debug.Log("DataManager: Po��czenie z baz� danych udane.");
    }

    public SensorData GetDataAtOffset(int subjectId, int offset)
    {
        return dbConnection.Table<SensorData>()
                           .Where(row => row.subject_id == subjectId)
                           .Skip(offset)
                           .FirstOrDefault();
    }

    public List<SensorData> GetDataChunk(int subjectId, int offset, int chunkSize)
    {
        return dbConnection.Table<SensorData>()
                           .Where(row => row.subject_id == subjectId)
                           .Skip(offset)
                           .Take(chunkSize)
                           .ToList();
    }
    public List<int> GetLabelHistoryChunk(int subjectId, int offset, int chunkSize)
    {
        string sql = $"SELECT label FROM sensor_data WHERE subject_id = ? ORDER BY rowid LIMIT ? OFFSET ?";

        List<LabelOnlyData> resultObjects = dbConnection.Query<LabelOnlyData>(sql, subjectId, chunkSize, offset);

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