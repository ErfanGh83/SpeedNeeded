using UnityEngine;

public class PlayerSpeed : MonoBehaviour
{
    [Header("Base speed (cruising)")]
    [SerializeField] float startBaseSpeed = 15f;
    [SerializeField] float maxBaseSpeed = 30f;
    [Tooltip("Base speed gained per second of running.")]
    [SerializeField] float baseSpeedGain = 0.05f;

    [Header("Speed limit (reached when boosting)")]
    [SerializeField] float startSpeedLimit = 25f;
    [SerializeField] float maxSpeedLimit = 50f;
    [Tooltip("Speed limit gained per second of running.")]
    [SerializeField] float speedLimitGain = 0.08f;

    [Header("Boost")]
    [Tooltip("Units/s gained per second while boosting.")]
    [SerializeField] float acceleration = 12f;
    [Tooltip("Units/s lost per second when you let go of boost.")]
    [SerializeField] float deceleration = 8f;
    [Tooltip("Extra fuel per second used while boosting (on top of the normal drain).")]
    [SerializeField] float boostFuelPerSecond = 6f;

    [Header("References")]
    [SerializeField] FuelSystem fuel;

    public float CurrentSpeed { get; private set; }
    public float BaseSpeed { get; private set; }
    public float SpeedLimit { get; private set; }
    public bool IsBoosting { get; private set; }

    InputSystem_Actions input;

    void Awake()
    {
        if (fuel == null) fuel = GetComponent<FuelSystem>();

        input = new InputSystem_Actions();
        ResetSpeed();
    }

    void OnEnable()
    {
        input.Player.Enable();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted += ResetSpeed;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    void OnDisable()
    {
        input.Player.Disable();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted -= ResetSpeed;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    void OnDestroy() => input.Dispose();

    void Update()
    {
        if (!GameManager.IsPlaying) return; // menu, paused or game over

        float dt = Time.deltaTime;

        // Difficulty ramps up over time
        BaseSpeed = Mathf.MoveTowards(BaseSpeed, maxBaseSpeed, baseSpeedGain * dt);
        SpeedLimit = Mathf.MoveTowards(SpeedLimit, maxSpeedLimit, speedLimitGain * dt);

        // Boost only while there is fuel
        IsBoosting = input.Player.Sprint.IsPressed() && (fuel == null || fuel.HasFuel);

        float target = IsBoosting ? SpeedLimit : BaseSpeed;
        float rate = CurrentSpeed < target ? acceleration : deceleration;
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, target, rate * dt);

        WorldSpeed.Value = CurrentSpeed;

        if (IsBoosting && fuel != null) fuel.Consume(boostFuelPerSecond * dt);

        if (GameManager.Instance != null) GameManager.Instance.AddDistance(CurrentSpeed * dt);
    }

    void ResetSpeed()
    {
        BaseSpeed = startBaseSpeed;
        SpeedLimit = startSpeedLimit;
        CurrentSpeed = BaseSpeed;
        IsBoosting = false;
        WorldSpeed.Value = CurrentSpeed;
    }

    void HandleStateChanged(GameState state)
    {
        if (state != GameState.GameOver) return;

        CurrentSpeed = 0f;
        IsBoosting = false;
        WorldSpeed.Value = 0f;
    }
}