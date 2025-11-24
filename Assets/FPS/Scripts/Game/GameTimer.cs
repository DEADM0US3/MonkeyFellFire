using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    public enum GameMode { ModoA, ModoB }
    public GameMode currentMode;

    public Text timerText;

    private float timer = 0f;
    private bool isRunning = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(timerText.transform.root.gameObject); // Mantiene el canvas entero
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        if (isRunning)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(timer);
    }

    public void StopTimer()
    {
        isRunning = false;
        SaveTimeToFile();
    }

    void SaveTimeToFile()
    {
        string filePath = Application.persistentDataPath + "/timestats.txt";
        string entry = $"{System.DateTime.Now} | {currentMode} | Tiempo: {FormatTime(timer)}\n";
        File.AppendAllText(filePath, entry);
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000);
        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }
}
