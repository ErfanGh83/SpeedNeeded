using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Running, Paused, GameOver }

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static bool IsPlaying => Instance == null || Instance.State == GameState.Running;

    [Header("Start")]
    [Tooltip("Begin running immediately. Turn off once you have a menu that calls StartRun().")]
    [SerializeField] bool autoStartRun = true;

    [Header("Score")]
    [SerializeField] float pointsPerMeter = 1f;
    [SerializeField] int pointsPerCoin = 10;

    [Header("Debug")]
    [SerializeField] bool logEvents = true;

    static bool skipMenuOnNextLoad;
    static bool forceMenuOnNextLoad;

    public GameState State { get; private set; } = GameState.Menu;
    public bool IsRunning => State == GameState.Running;

    public float Distance { get; private set; }
    public int Score => Mathf.FloorToInt(Distance * pointsPerMeter) + bonusScore;
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }
    public int RunCoins => MoneyStore.RunEarned;
    public int TotalMoney => MoneyStore.Balance;
    public string GameOverReason { get; private set; } = "";

    public event Action<GameState> OnStateChanged;
    public event Action OnRunStarted;
    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnCoinsChanged;

    InputSystem_Actions input;
    int bonusScore;
    int lastReportedScore;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        HighScore = SaveSystem.HighScore;

        input = new InputSystem_Actions();
    }

    void OnEnable() => input.UI.Enable();
    void OnDisable() => input.UI.Disable();

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        input.Dispose();
    }

    void Start()
    {
        bool startNow = !forceMenuOnNextLoad && (autoStartRun || skipMenuOnNextLoad);
        skipMenuOnNextLoad = false;
        forceMenuOnNextLoad = false;

        if (startNow) StartRun();
        else if (logEvents) Debug.Log("[GameManager] Menu");
    }

    void Update()
    {
        if (input.UI.Cancel.WasPressedThisFrame()) TogglePause();
    }

    public void StartRun()
    {
        Distance = 0f;
        bonusScore = 0;
        lastReportedScore = 0;
        IsNewHighScore = false;
        GameOverReason = "";

        MoneyStore.BeginRun();

        OnScoreChanged?.Invoke(0);
        OnCoinsChanged?.Invoke(RunCoins, TotalMoney);

        SetState(GameState.Running);
        OnRunStarted?.Invoke();
    }

    public void Pause()  { if (State == GameState.Running) SetState(GameState.Paused); }
    public void Resume() { if (State == GameState.Paused)  SetState(GameState.Running); }

    public void TogglePause()
    {
        if (State == GameState.Running) Pause();
        else if (State == GameState.Paused) Resume();
    }

    public void GameOver(string reason = "")
    {
        if (State != GameState.Running) return;

        GameOverReason = reason;

        if (Score > HighScore)
        {
            HighScore = Score;
            IsNewHighScore = true;
            SaveSystem.HighScore = HighScore;
        }

        SaveSystem.Save();

        if (logEvents)
            Debug.Log($"[GameManager] Game over: {reason} | score {Score} | best {HighScore} | money this run {RunCoins}");

        SetState(GameState.GameOver);
    }

    public void Restart()
    {
        skipMenuOnNextLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        forceMenuOnNextLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddDistance(float meters)
    {
        if (!IsRunning) return;

        Distance += meters;
        RefreshScore();
    }

    public void AddScore(int points)
    {
        if (!IsRunning) return;

        bonusScore += points;
        RefreshScore();
    }

    public void AddCoins(int amount)
    {
        if (!IsRunning) return;

        MoneyStore.Add(amount);
        bonusScore += amount * pointsPerCoin;

        OnCoinsChanged?.Invoke(RunCoins, TotalMoney);
        RefreshScore();
    }

    void RefreshScore()
    {
        int score = Score;
        if (score == lastReportedScore) return;

        lastReportedScore = score;
        OnScoreChanged?.Invoke(score);
    }

    void SetState(GameState newState)
    {
        if (State == newState) return;

        GameState old = State;
        State = newState;
        Time.timeScale = newState == GameState.Paused ? 0f : 1f;

        if (logEvents) Debug.Log($"[GameManager] {old} -> {newState}");
        OnStateChanged?.Invoke(newState);
    }

    void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
    void OnApplicationQuit() => SaveSystem.Save();
}