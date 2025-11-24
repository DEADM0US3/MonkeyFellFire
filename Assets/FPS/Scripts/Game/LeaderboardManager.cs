using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    public Text topTimesText;

    public enum GameMode { ModoA, ModoB }

    // Clase para almacenar datos
    public class TimeRecord
    {
        public GameMode mode;
        public float rawTime;
        public string formattedTime;

        public TimeRecord(GameMode m, float r, string f)
        {
            mode = m;
            rawTime = r;
            formattedTime = f;
        }
    }

    // ============================
    // GUARDAR TIEMPO EN TXT
    // ============================
    public void SaveTime(GameMode mode, float timeInSeconds, string formattedTime)
    {
        string filePath = Application.persistentDataPath + "/timestats.txt";
        string entry = $"{mode}|{timeInSeconds}|{formattedTime}\n";

        File.AppendAllText(filePath, entry);
        Debug.Log("Tiempo guardado: " + entry);
    }

    // ============================
    // LEER TODOS LOS TIEMPOS
    // ============================
    public List<TimeRecord> LoadAllTimes()
    {
        string filePath = Application.persistentDataPath + "/timestats.txt";

        List<TimeRecord> times = new List<TimeRecord>();

        if (!File.Exists(filePath))
            return times;

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split('|');
            if (parts.Length != 3) continue;

            GameMode mode = (GameMode)System.Enum.Parse(typeof(GameMode), parts[0]);
            float raw = float.Parse(parts[1]);
            string formatted = parts[2];

            times.Add(new TimeRecord(mode, raw, formatted));
        }

        return times;
    }

    // ============================
    // OBTENER TOP 10 POR MODO
    // ============================
    public List<TimeRecord> GetTop10(GameMode mode)
    {
        List<TimeRecord> all = LoadAllTimes();
        List<TimeRecord> filtered = all.FindAll(t => t.mode == mode);

        filtered.Sort((a, b) => a.rawTime.CompareTo(b.rawTime)); // ascendente

        if (filtered.Count > 10)
            filtered = filtered.GetRange(0, 10);

        return filtered;
    }

    // ============================
    // MOSTRAR EN LA UI
    // ============================
    public void ShowTopTimes(GameMode mode)
    {
        List<TimeRecord> top = GetTop10(mode);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"TOP 10 - {mode}");

        int index = 1;
        foreach (var record in top)
        {
            sb.AppendLine($"{index}. {record.formattedTime}");
            index++;
        }

        topTimesText.text = sb.ToString();
    }
}
