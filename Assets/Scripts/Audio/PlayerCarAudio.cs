using UnityEngine;

public class PlayerCarAudio : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] AudioClip engineLoop;
    [SerializeField] AudioClip flameStart;
    [SerializeField] AudioClip flameLoop;
    [SerializeField] AudioClip hit;
    [SerializeField] AudioClip heal;
    [SerializeField] AudioClip fuelRefill;
    [SerializeField] AudioClip money;
    [SerializeField] AudioClip turn;
    [SerializeField] AudioClip music;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] float engineVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] float flameVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] float pausedMusicMultiplier = 0.4f;

    [Header("Engine pitch")]
    [SerializeField] float enginePitchMin = 0.8f;
    [SerializeField] float enginePitchMax = 1.8f;
    [Tooltip("Speed at which the engine pitch reaches its maximum.")]
    [SerializeField] float speedForMaxPitch = 50f;

    [Header("Money")]
    [SerializeField] float moneyPitchVariation = 0.08f;

    [Header("References (auto-found if empty)")]
    [SerializeField] PlayerSpeed speed;
    [SerializeField] HealthSystem health;
    [SerializeField] FuelSystem fuel;
    [SerializeField] CarController car;

    AudioSource engineSource;
    AudioSource flameStartSource;
    AudioSource flameLoopSource;
    AudioSource sfxSource;
    AudioSource musicSource;

    int previousHealth;
    float previousFuel;
    int previousBalance;

    void Awake()
    {
        if (speed == null) speed = GetComponentInParent<PlayerSpeed>();
        if (health == null) health = GetComponentInParent<HealthSystem>();
        if (fuel == null) fuel = GetComponentInParent<FuelSystem>();
        if (car == null) car = GetComponentInParent<CarController>();

        engineSource = CreateSource(engineLoop, true, engineVolume);
        flameStartSource = CreateSource(flameStart, false, flameVolume);
        flameLoopSource = CreateSource(flameLoop, true, flameVolume);
        sfxSource = CreateSource(null, false, 1f);
        musicSource = CreateSource(music, true, musicVolume);
        musicSource.ignoreListenerPause = true;
    }

    AudioSource CreateSource(AudioClip clip, bool loop, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    void OnEnable()
    {
        if (speed != null) speed.OnBoostChanged += HandleBoostChanged;

        if (health != null)
        {
            previousHealth = health.Health;
            health.OnDamaged += HandleDamaged;
            health.OnHealthChanged += HandleHealthChanged;
        }

        if (fuel != null)
        {
            previousFuel = fuel.Fuel;
            fuel.OnFuelChanged += HandleFuelChanged;
        }

        if (car != null)
        {
            car.OnTurnLeft += HandleTurn;
            car.OnTurnRight += HandleTurn;
        }

        previousBalance = MoneyStore.Balance;
        MoneyStore.OnMoneyChanged += HandleMoneyChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (speed != null) speed.OnBoostChanged -= HandleBoostChanged;

        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnHealthChanged -= HandleHealthChanged;
        }

        if (fuel != null) fuel.OnFuelChanged -= HandleFuelChanged;

        if (car != null)
        {
            car.OnTurnLeft -= HandleTurn;
            car.OnTurnRight -= HandleTurn;
        }

        MoneyStore.OnMoneyChanged -= HandleMoneyChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;

        AudioListener.pause = false;
    }

    void Start()
    {
        if (music != null) musicSource.Play();

        if (GameManager.Instance == null && engineLoop != null) engineSource.Play();
    }

    void Update()
    {
        if (!GameManager.IsPlaying || speed == null) return;

        float t = Mathf.Clamp01(speed.CurrentSpeed / speedForMaxPitch);
        engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, t);

        if (speed.IsBoosting && flameLoop != null && !flameStartSource.isPlaying && !flameLoopSource.isPlaying)
            flameLoopSource.Play();
    }

    void HandleStateChanged(GameState state)
    {
        AudioListener.pause = state == GameState.Paused;
        musicSource.volume = musicVolume * (state == GameState.Paused ? pausedMusicMultiplier : 1f);

        switch (state)
        {
            case GameState.Running:
                if (engineLoop != null && !engineSource.isPlaying) engineSource.Play();
                break;

            case GameState.GameOver:
            case GameState.Menu:
                engineSource.Stop();
                StopFlame();
                break;
        }
    }

    void HandleBoostChanged(bool boosting)
    {
        if (!boosting)
        {
            StopFlame();
            return;
        }

        flameLoopSource.Stop();

        if (flameStart != null)
        {
            flameStartSource.Play();
            if (flameLoop != null)
                flameLoopSource.PlayScheduled(AudioSettings.dspTime + flameStart.length);
        }
        else if (flameLoop != null)
        {
            flameLoopSource.Play();
        }
    }

    void StopFlame()
    {
        flameStartSource.Stop();
        flameLoopSource.Stop();
    }

    void HandleDamaged() => PlaySfx(hit);

    void HandleHealthChanged(int current, int max)
    {
        if (current > previousHealth) PlaySfx(heal);
        previousHealth = current;
    }

    void HandleFuelChanged(float current, float max)
    {
        if (current > previousFuel) PlaySfx(fuelRefill);
        previousFuel = current;
    }

    void HandleMoneyChanged(int balance, int runEarned)
    {
        if (balance > previousBalance) PlaySfx(money, moneyPitchVariation);
        previousBalance = balance;
    }

    void HandleTurn() => PlaySfx(turn);

    void PlaySfx(AudioClip clip, float pitchVariation = 0f)
    {
        if (clip == null) return;

        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}