using UnityEngine;

public class CarHeadlights : MonoBehaviour
{
    [System.Serializable]
    class EmissiveTarget
    {
        public Renderer renderer;
        public int materialIndex = 0;
    }

    [Header("Lights")]
    [Tooltip("The headlight Lights (spot lights). Their intensity in the Inspector is used as the 'on' brightness.")]
    [SerializeField] Light[] lights;

    [Tooltip("Optional: beam meshes, glow sprites or anything else that should only be visible while the lights are on.")]
    [SerializeField] GameObject[] visuals;

    [Header("Lamp glow (optional)")]
    [Tooltip("Headlight lens materials. The material needs Emission enabled.")]
    [SerializeField] EmissiveTarget[] emissiveTargets;
    [SerializeField] Color emissionColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] float emissionIntensity = 2f;

    [Header("Behaviour")]
    [Tooltip("Seconds the lights take to fade in or out.")]
    [SerializeField] float fadeDuration = 0.6f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    float[] onIntensity;
    MaterialPropertyBlock block;
    float level;  // 0 = off, 1 = on
    float target;

    void OnEnable()
    {
        DayNightCycleManager.Changed += OnTimeChanged;
    }

    void OnDisable()
    {
        DayNightCycleManager.Changed -= OnTimeChanged;
    }

    void Start()
    {
        onIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) onIntensity[i] = lights[i].intensity;

        block = new MaterialPropertyBlock();

        var manager = DayNightCycleManager.Instance;
        if (manager == null) Debug.LogWarning("CarHeadlights: no DayNightCycleManager in the scene, lights stay off.", this);
        Debug.Log(DayNightCycleManager.Instance.Current);
        target = manager != null && manager.Current == TimeOfDay.Night ? 1f : 0f;
        level = target;
        Apply();
    }

    void OnTimeChanged(TimeOfDay time)
    {
        target = time == TimeOfDay.Night ? 1f : 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(level, target)) return;

        float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
        level = Mathf.MoveTowards(level, target, step);
        Apply();
    }

    void Apply()
    {
        bool lit = level > 0.001f;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].enabled = lit;
            lights[i].intensity = onIntensity[i] * level;
        }

        foreach (GameObject go in visuals)
            if (go != null && go.activeSelf != lit) go.SetActive(lit);

        Color emission = emissionColor * (emissionIntensity * level);
        foreach (EmissiveTarget e in emissiveTargets)
        {
            if (e.renderer == null) continue;
            block.SetColor(EmissionColorId, emission);
            e.renderer.SetPropertyBlock(block, e.materialIndex);
        }
    }
}