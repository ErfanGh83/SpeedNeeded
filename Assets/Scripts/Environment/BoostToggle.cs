using UnityEngine;

public class BoostToggle : MonoBehaviour
{
    [SerializeField] PlayerSpeed speed;
    [SerializeField] GameObject[] targets;

    [Tooltip("ON: effects are active while NOT boosting.")]
    [SerializeField] bool invert = false;
    [SerializeField] float offDelay = 0f;
    [Tooltip("ON: existing particles fade out. OFF: they vanish immediately.")]
    [SerializeField] bool softParticleStop = true;
    [Tooltip("Force Looping on and Stop Action to None, so the particles can always be started again.")]
    [SerializeField] bool fixParticleSettings = true;
    [SerializeField] bool logChanges = false;

    ParticleSystem[][] particles;
    TrailRenderer[][] trails;
    bool[] plainObject;
    bool active;
    float offTimer;

    [System.Obsolete]
    void Awake()
    {
        if (speed == null) speed = FindFirstObjectByType<PlayerSpeed>();
        if (speed == null) Debug.LogError("BoostToggle: no PlayerSpeed found in the scene.", this);

        int n = targets.Length;
        particles = new ParticleSystem[n][];
        trails = new TrailRenderer[n][];
        plainObject = new bool[n];

        for (int i = 0; i < n; i++)
        {
            GameObject t = targets[i];
            if (t == null)
            {
                particles[i] = new ParticleSystem[0];
                trails[i] = new TrailRenderer[0];
                continue;
            }

            particles[i] = t.GetComponentsInChildren<ParticleSystem>(true);
            trails[i] = t.GetComponentsInChildren<TrailRenderer>(true);

            bool containsSelf = t == gameObject || transform.IsChildOf(t.transform);
            plainObject[i] = particles[i].Length == 0 && trails[i].Length == 0;

            if (plainObject[i] && containsSelf)
            {
                Debug.LogError($"BoostToggle: this script is on '{t.name}' or inside it and the target has no particles, so it would switch itself off. Move the script to another object.", this);
                plainObject[i] = false;
            }

            if (!t.activeSelf) t.SetActive(true);

            if (fixParticleSettings)
            {
                foreach (ParticleSystem p in particles[i])
                {
                    var main = p.main;
                    main.loop = true;
                    main.stopAction = ParticleSystemStopAction.None;
                }
            }
        }
    }

    void Start() => Set(Wanted());

    bool Wanted() => speed != null && GameManager.IsPlaying && speed.IsBoosting != invert;

    void Update()
    {
        bool wanted = Wanted();

        if (wanted)
        {
            offTimer = offDelay;
            if (!active) Set(true);
        }
        else if (active)
        {
            offTimer -= Time.unscaledDeltaTime;
            if (offTimer <= 0f) Set(false);
        }

        if (!active) EnforceOff();
    }

    void Set(bool on)
    {
        active = on;
        if (logChanges) Debug.Log($"[BoostToggle] {(on ? "ON" : "OFF")}", this);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            foreach (ParticleSystem p in particles[i])
            {
                if (p == null) continue;

                var emission = p.emission;
                emission.enabled = on;

                if (on)
                {
                    if (!p.isPlaying) p.Play();
                }
                else if (!softParticleStop) p.Clear();
            }

            foreach (TrailRenderer tr in trails[i])
            {
                if (tr == null) continue;
                tr.emitting = on;
                if (!on && !softParticleStop) tr.Clear();
            }

            if (plainObject[i]) targets[i].SetActive(on);
        }
    }

    void EnforceOff()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            foreach (ParticleSystem p in particles[i])
            {
                if (p == null) continue;

                var emission = p.emission;
                if (emission.enabled) emission.enabled = false;
            }

            foreach (TrailRenderer tr in trails[i])
                if (tr != null && tr.emitting) tr.emitting = false;
        }
    }
}