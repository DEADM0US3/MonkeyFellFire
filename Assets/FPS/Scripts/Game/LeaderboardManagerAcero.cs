using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public class LeaderboardManagerAcero : MonoBehaviour
{
    public Text topTimesText;

    public enum GameMode { ModoA, ModoB }

    public class TimeRecord
    {
        public GameMode mode = GameMode.ModoB;
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
    public void SaveTime( float timeInSeconds, string formattedTime, GameMode mode = GameMode.ModoB)
    {
        string filePath = Application.persistentDataPath + "/timestats.txt";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "");
            Debug.Log("Archivo creado en: " + filePath);
        }

        string entry = $"{mode}|{timeInSeconds}|{formattedTime}\n";
        File.AppendAllText(filePath, entry);

        Debug.Log("Tiempo guardado: " + entry);
    }

    // ============================
    // CARGAR ARCHIVO Y FILTRAR LÍNEAS INVÁLIDAS
    // ============================
    public List<TimeRecord> LoadAllTimes()
    {
        string filePath = Application.persistentDataPath + "/timestats.txt";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "");
            return new List<TimeRecord>();
        }

        string[] lines = File.ReadAllLines(filePath);
        List<TimeRecord> times = new List<TimeRecord>();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split('|');

            // Solo aceptar líneas válidas del formato (modo|raw|formatted)
            if (parts.Length != 3) continue;
            if (parts[0] != "ModoA" && parts[0] != "ModoB") continue;

            GameMode mode = (GameMode)System.Enum.Parse(typeof(GameMode), parts[0]);
            float raw = float.Parse(parts[1]);
            string formatted = parts[2];

            times.Add(new TimeRecord(mode, raw, formatted));
        }

        return times;
    }

    // ============================
    // TOP 10 POR MODO
    // ============================
    public List<TimeRecord> GetTop10(GameMode mode = GameMode.ModoB)
    {
        List<TimeRecord> all = LoadAllTimes();
        List<TimeRecord> filtered = all.FindAll(t => t.mode == mode);

        filtered.Sort((a, b) => a.rawTime.CompareTo(b.rawTime));

        if (filtered.Count > 10)
            filtered = filtered.GetRange(0, 10);

        return filtered;
    }

    // ============================
    // MOSTRAR EN UI
    // ============================
    public void ShowTopTimes(GameMode mode = GameMode.ModoB)
    {
        List<TimeRecord> top = GetTop10(mode);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"TOP 10 - { (mode == GameMode.ModoA ? "Normal" : "Mono De Acero")}");

        if (top.Count == 0)
        {
            sb.AppendLine("No hay tiempos registrados aún.");
            topTimesText.text = sb.ToString();
            return;
        }

        int index = 1;
        foreach (var record in top)
        {
            sb.AppendLine($"{index}. {record.formattedTime}");
            index++;
        }

        topTimesText.text = sb.ToString();
    }
}
