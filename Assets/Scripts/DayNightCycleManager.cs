using System;
using UnityEngine;

public enum TimeOfDay { Day, Night }

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    public static event Action<TimeOfDay> Changed;

    [Header("Start")]
    [SerializeField] TimeOfDay startTime = TimeOfDay.Day;

    [Header("Cycle (in score)")]
    [Tooltip("Score gained while it stays day.")]
    [SerializeField] int dayScore = 500;
    [Tooltip("Score gained while it stays night.")]
    [SerializeField] int nightScore = 300;

    [Header("Debug")]
    [SerializeField] bool logChanges = true;

    public TimeOfDay Current { get; private set; }

    GameManager game;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Current = startTime;
    }

    void OnEnable()
    {
        game = GameManager.Instance;
        if (game != null)
        {
            game.OnScoreChanged += OnScoreChanged;
            game.OnRunStarted += OnRunStarted;
        }
        else Debug.LogWarning("DayNightCycleManager: no GameManager in the scene, the cycle will not run.", this);
    }

    void Start()
    {
        if (game != null && game.State == GameState.Menu)
            SetTime(TimeOfDay.Day);
    }

    void OnDisable()
    {
        if (game != null)
        {
            game.OnScoreChanged -= OnScoreChanged;
            game.OnRunStarted -= OnRunStarted;
        }
        game = null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnRunStarted() => Evaluate(game.Score);

    void OnScoreChanged(int score)
    {
        if (game.State == GameState.Menu) return;
        Evaluate(score);
    }

    void Evaluate(int score)
    {
        int day = Mathf.Max(1, dayScore);
        int night = Mathf.Max(1, nightScore);

        // Starting at night just shifts the position in the cycle by one day length
        int pos = (score + (startTime == TimeOfDay.Night ? day : 0)) % (day + night);

        SetTime(pos < day ? TimeOfDay.Day : TimeOfDay.Night);
    }

    void SetTime(TimeOfDay time)
    {
        if (time == Current) return;

        Current = time;
        if (logChanges) Debug.Log($"[DayNightCycleManager] {time} (score {game.Score}).", this);
        Changed?.Invoke(time);
    }

    [ContextMenu("Toggle Now")]
    void ToggleNow()
    {
        if (Application.isPlaying) SetTime(Current == TimeOfDay.Day ? TimeOfDay.Night : TimeOfDay.Day);
    }
}