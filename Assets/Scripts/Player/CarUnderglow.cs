using UnityEngine;

public class CarUnderglow : MonoBehaviour
{
    public enum ColorMode { Rainbow, Solid }

    [System.Serializable]
    class EmissiveTarget
    {
        public Renderer renderer;
        public int materialIndex = 0;
    }

    [Header("Lights")]
    [Tooltip("Lights under the car. Their intensity in the Inspector is used as the 'on' brightness.")]
    [SerializeField] Light[] lights;

    [Header("LED strips (optional)")]
    [Tooltip("Meshes that look like the LED strip. The material needs Emission enabled.")]
    [SerializeField] EmissiveTarget[] emissiveTargets;
    [SerializeField] float emissionIntensity = 3f;

    [Header("Color")]
    [SerializeField] ColorMode mode = ColorMode.Rainbow;
    [SerializeField] Color solidColor = Color.magenta;
    [Tooltip("Rainbow: full color cycles per second.")]
    [SerializeField] float hueSpeed = 0.15f;
    [Tooltip("Rainbow: hue difference between neighbouring lights/strips. 0 = all the same color.")]
    [Range(0f, 1f)]
    [SerializeField] float hueSpread = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] float saturation = 1f;

    [Header("Behaviour")]
    [Tooltip("Seconds the glow takes to fade in or out.")]
    [SerializeField] float fadeDuration = 0.6f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    float[] onIntensity;
    MaterialPropertyBlock block;
    float level;  // 0 = off, 1 = on
    float target;
    float hue;
    bool isOn;

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
        if (manager == null) Debug.LogWarning("CarUnderglow: no DayNightCycleManager in the scene, the lights stay off.", this);

        target = manager != null && manager.Current == TimeOfDay.Night ? 1f : 0f;
        level = target;

        if (level <= 0f) SetOff();
    }

    void OnTimeChanged(TimeOfDay time)
    {
        target = time == TimeOfDay.Night ? 1f : 0f;
    }

    void Update()
    {
        if (!Mathf.Approximately(level, target))
        {
            float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
            level = Mathf.MoveTowards(level, target, step);
        }

        if (level <= 0f)
        {
            if (isOn) SetOff();
            return;
        }

        isOn = true;
        hue = Mathf.Repeat(hue + hueSpeed * Time.deltaTime, 1f);
        Apply();
    }

    void Apply()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].enabled = true;
            lights[i].color = GetColor(i);
            lights[i].intensity = onIntensity[i] * level;
        }

        for (int i = 0; i < emissiveTargets.Length; i++)
        {
            EmissiveTarget e = emissiveTargets[i];
            if (e.renderer == null) continue;

            e.renderer.GetPropertyBlock(block, e.materialIndex);
            block.SetColor(EmissionColorId, GetColor(i) * (emissionIntensity * level));
            e.renderer.SetPropertyBlock(block, e.materialIndex);
        }
    }

    void SetOff()
    {
        isOn = false;

        foreach (Light l in lights)
            if (l != null) l.enabled = false;

        foreach (EmissiveTarget e in emissiveTargets)
        {
            if (e.renderer == null) continue;

            e.renderer.GetPropertyBlock(block, e.materialIndex);
            block.SetColor(EmissionColorId, Color.black);
            e.renderer.SetPropertyBlock(block, e.materialIndex);
        }
    }

    Color GetColor(int index)
    {
        if (mode == ColorMode.Solid) return solidColor;
        return Color.HSVToRGB(Mathf.Repeat(hue + index * hueSpread, 1f), saturation, 1f);
    }
}