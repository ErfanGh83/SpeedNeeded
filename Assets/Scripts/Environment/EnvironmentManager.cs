using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnvironmentType { City, Village, Jungle }

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    public static event Action<EnvironmentType> Changed;

    [Header("Start")]
    [SerializeField] EnvironmentType startEnvironment = EnvironmentType.City;

    [Header("Random switching")]
    [SerializeField] bool autoSwitch = true;

    [Tooltip("Random time between environment changes (seconds of play time, min, max).")]
    [SerializeField] Vector2 switchInterval = new Vector2(30f, 60f);

    [Header("Debug")]
    [SerializeField] bool logChanges = true;

    public EnvironmentType Current { get; private set; }

    float timer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Current = startEnvironment;
        ResetTimer();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!autoSwitch) return;
        if (!GameManager.IsPlaying) return; // the timer doesn't run in menu, pause or game over

        timer -= Time.deltaTime;
        if (timer <= 0f) SetEnvironment(PickRandomOther());
    }

    /// <summary>Force a specific environment (e.g. from a story event). Also restarts the random timer.</summary>
    public void SetEnvironment(EnvironmentType type)
    {
        ResetTimer();
        if (type == Current) return;

        Current = type;
        if (logChanges) Debug.Log($"[EnvironmentManager] Environment changed to {type}.", this);
        Changed?.Invoke(type);
    }

    [ContextMenu("Switch Now (random)")]
    void SwitchNow()
    {
        if (Application.isPlaying) SetEnvironment(PickRandomOther());
    }

    // Always a different one from the current, so a switch is never a no-op
    EnvironmentType PickRandomOther()
    {
        List<EnvironmentType> options = new List<EnvironmentType>();
        foreach (EnvironmentType t in Enum.GetValues(typeof(EnvironmentType)))
            if (t != Current) options.Add(t);

        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    void ResetTimer()
    {
        timer = UnityEngine.Random.Range(switchInterval.x, switchInterval.y);
    }
}