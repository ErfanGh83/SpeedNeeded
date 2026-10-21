using System;
using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [SerializeField] float maxFuel = 100f;

    [Tooltip("Fuel used per second just by driving.")]
    [SerializeField] float drainPerSecond = 2f;

    [Header("Debug")]
    [SerializeField] bool logFuelChanges = false;

    public float Fuel { get; private set; }
    public float MaxFuel => maxFuel;
    public float Normalized => maxFuel > 0f ? Fuel / maxFuel : 0f;
    public bool HasFuel => Fuel > 0f;

    public event Action<float, float> OnFuelChanged;

    float nextDrainLogTime;

    void Awake()
    {
        Fuel = maxFuel;
        if (logFuelChanges) Debug.Log($"[FuelSystem] Initialized: {Fuel:0.##}/{maxFuel:0.##}", this);
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted += ResetFuel;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted -= ResetFuel;
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        Consume(drainPerSecond * Time.deltaTime);
    }

    public void Consume(float amount)
    {
        if (!HasFuel) return;

        Fuel = Mathf.Max(0f, Fuel - amount);
        OnFuelChanged?.Invoke(Fuel, maxFuel);

        // Consume is normally called every frame, so report the drain at most once per second.
        if (logFuelChanges && Time.time >= nextDrainLogTime)
        {
            Debug.Log($"[FuelSystem] Fuel draining: {Fuel:0.##}/{maxFuel:0.##}", this);
            nextDrainLogTime = Time.time + 1f;
        }

        if (!HasFuel && GameManager.Instance != null)
        {
            if (logFuelChanges) Debug.Log("[FuelSystem] Fuel empty; ending the run.", this);
            GameManager.Instance.GameOver("Out of fuel");
        }
    }

    public void Add(float amount)
    {
        float previousFuel = Fuel;
        Fuel = Mathf.Min(maxFuel, Fuel + amount);
        OnFuelChanged?.Invoke(Fuel, maxFuel);
        if (logFuelChanges)
            Debug.Log($"[FuelSystem] Added {amount:0.##} fuel: {previousFuel:0.##} -> {Fuel:0.##}/{maxFuel:0.##}", this);
    }

    void ResetFuel()
    {
        Fuel = maxFuel;
        OnFuelChanged?.Invoke(Fuel, maxFuel);
        nextDrainLogTime = Time.time + 1f;
        if (logFuelChanges) Debug.Log($"[FuelSystem] Run started; fuel reset to {Fuel:0.##}/{maxFuel:0.##}.", this);
    }
}
