using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    public enum GameMode { ModoA, ModoB }
    public GameMode currentMode;

    [ContextMenu("Modo A")]
    void SetModoA() { currentMode = GameMode.ModoA; }

    [ContextMenu("Modo B")]
    void SetModoB() { currentMode = GameMode.ModoB; }
    public Text timerText;

    private static GameObject persistentCanvas;

    private float timer = 0f;
    private bool isRunning = true;

    void Awake()
    {
        // SINGLETON DEL TIMER
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;  // <── IMPORTANTE
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // SINGLETON DEL CANVAS
        RegisterCanvas();
    }

    void RegisterCanvas()
    {
        var canvasRoot = timerText.transform.root.gameObject;

        if (persistentCanvas == null)
        {
            persistentCanvas = canvasRoot;
            DontDestroyOnLoad(persistentCanvas);
        }
        else if (persistentCanvas != canvasRoot)
        {
            // Destruir Canvas duplicado
            Destroy(canvasRoot);
        }
    }

    // CUANDO CAMBIA DE ESCENA, REVISAR SI LA ESCENA TRAE UN TIMER NUEVO
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Intentar encontrar un nuevo timer en la escena
        GameTimer[] timers = FindObjectsOfType<GameTimer>();

        foreach (var t in timers)
        {
            if (t != Instance)
            {
                Destroy(t.gameObject);
            }
        }

        // Intentar encontrar un nuevo Canvas con otro timerText
        var allTexts = FindObjectsOfType<Text>();

        foreach (var txt in allTexts)
        {
            if (txt != timerText && txt.name == timerText.name)
            {
                Destroy(txt.transform.root.gameObject);
            }
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

        // Calculamos el tiempo crudo
        float rawTime = timer;
        string formattedTime = FormatTime(timer);

        // Guardamos solo lo que LeaderboardManager puede leer
        string entry = $"{currentMode}|{rawTime}|{formattedTime}\n";

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
