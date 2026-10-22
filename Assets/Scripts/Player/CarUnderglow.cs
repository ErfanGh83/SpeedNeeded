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

    [Header("Boost")]
    [Tooltip("Found automatically on the car if left empty.")]
    [SerializeField] PlayerSpeed speed;
    [Tooltip("ON: the glow also lights up while boosting during the day. OFF: boost effects only at night.")]
    [SerializeField] bool boostGlowsInDay = true;
    [Tooltip("Higher = the glow follows the boost faster.")]
    [SerializeField] float boostResponse = 6f;
    [SerializeField] float boostIntensityMultiplier = 4f;
    [SerializeField] float boostRangeMultiplier = 2f;
    [SerializeField] float boostSpotAngleMultiplier = 1.3f;
    [SerializeField] float boostEmissionMultiplier = 2f;
    [SerializeField] Color boostColor = new Color(0.6f, 0.9f, 1f);
    [Tooltip("How much the color moves toward Boost Color at full boost. 0 keeps the rainbow/solid color.")]
    [Range(0f, 1f)]
    [SerializeField] float boostColorBlend = 0.5f;

    [Header("Boost flicker")]
    [Range(0f, 0.5f)]
    [SerializeField] float flicker = 0.12f;
    [SerializeField] float flickerSpeed = 25f;

    [Header("Soft streak")]
    [SerializeField] bool showStreak = true;
    [Tooltip("Where the streak starts, in the car's local space (its front edge). The streak extends backward from here.")]
    [SerializeField] Vector3 streakStart = new Vector3(0f, 0.03f, 0f);
    [SerializeField] float streakWidth = 1.4f;
    [SerializeField] float streakMinLength = 0.5f;
    [SerializeField] float streakMaxLength = 7f;
    [Range(0f, 1f)]
    [SerializeField] float streakMaxAlpha = 0.6f;
    [SerializeField] int sortingOrder = 5;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    float[] onIntensity;
    float[] baseRange;
    float[] baseSpotAngle;
    MaterialPropertyBlock block;
    float level;  // 0 = off, 1 = on (night)
    float target;
    float boost;
    float hue;
    bool isOn;

    Texture2D streakTexture;
    Sprite streakSprite;
    SpriteRenderer streak;

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
        baseRange = new float[lights.Length];
        baseSpotAngle = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            onIntensity[i] = lights[i].intensity;
            baseRange[i] = lights[i].range;
            baseSpotAngle[i] = lights[i].spotAngle;
        }

        block = new MaterialPropertyBlock();

        if (speed == null) speed = GetComponentInParent<PlayerSpeed>();
        if (speed == null) Debug.LogWarning("CarUnderglow: no PlayerSpeed found, the boost effect is disabled.", this);

        if (showStreak) CreateStreak();

        var manager = DayNightCycleManager.Instance;
        if (manager == null) Debug.LogWarning("CarUnderglow: no DayNightCycleManager in the scene, the lights stay off.", this);

        target = manager != null && manager.Current == TimeOfDay.Night ? 1f : 0f;
        level = target;

        if (level <= 0f) SetOff();
    }

    void OnDestroy()
    {
        if (streakSprite != null) Destroy(streakSprite);
        if (streakTexture != null) Destroy(streakTexture);
    }

    void OnTimeChanged(TimeOfDay time)
    {
        target = time == TimeOfDay.Night ? 1f : 0f;
    }

    void CreateStreak()
    {
        const int size = 64;
        streakTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            float v = y / (size - 1f);
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f);
                float side = Mathf.Pow(1f - Mathf.Abs(u * 2f - 1f), 2f);
                float tail = Mathf.Pow(v, 1.5f);
                streakTexture.SetPixel(x, y, new Color(1f, 1f, 1f, side * tail));
            }
        }
        streakTexture.Apply();

        streakSprite = Sprite.Create(streakTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

        var go = new GameObject("BoostStreak");
        go.transform.SetParent(speed != null ? speed.transform : transform, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        streak = go.AddComponent<SpriteRenderer>();
        streak.sprite = streakSprite;
        streak.sortingOrder = sortingOrder;
        streak.enabled = false;
    }

    void Update()
    {
        float boostTarget = speed != null && GameManager.IsPlaying ? speed.BoostFactor : 0f;
        boost = Mathf.Lerp(boost, boostTarget, 1f - Mathf.Exp(-boostResponse * Time.unscaledDeltaTime));
        if (boost < 0.001f) boost = 0f;

        if (!Mathf.Approximately(level, target))
        {
            float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
            level = Mathf.MoveTowards(level, target, step);
        }

        float glow = Mathf.Max(level, boostGlowsInDay ? boost : 0f);

        if (glow <= 0f)
        {
            if (isOn) SetOff();
            UpdateStreak(0f, Color.white);
            return;
        }

        isOn = true;
        hue = Mathf.Repeat(hue + hueSpeed * Time.deltaTime, 1f);

        float noise = (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0.37f) - 0.5f) * 2f;
        float flick = 1f + noise * flicker * boost;

        Apply(glow, flick);

        float streakStrength = boostGlowsInDay ? boost : boost * level;
        Color c = Color.Lerp(GetColor(0), boostColor, boost * boostColorBlend);
        UpdateStreak(streakStrength * flick, c);
    }

    void Apply(float glow, float flick)
    {
        float intensityMul = Mathf.Lerp(1f, boostIntensityMultiplier, boost) * flick;
        float rangeMul = Mathf.Lerp(1f, boostRangeMultiplier, boost);
        float angleMul = Mathf.Lerp(1f, boostSpotAngleMultiplier, boost);
        float emissionMul = Mathf.Lerp(1f, boostEmissionMultiplier, boost) * flick;

        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null) continue;

            l.enabled = true;
            l.color = BoostedColor(i);
            l.intensity = onIntensity[i] * glow * intensityMul;
            l.range = baseRange[i] * rangeMul;
            if (l.type == LightType.Spot) l.spotAngle = Mathf.Min(baseSpotAngle[i] * angleMul, 179f);
        }

        for (int i = 0; i < emissiveTargets.Length; i++)
        {
            EmissiveTarget e = emissiveTargets[i];
            if (e.renderer == null) continue;

            e.renderer.GetPropertyBlock(block, e.materialIndex);
            block.SetColor(EmissionColorId, BoostedColor(i) * (emissionIntensity * glow * emissionMul));
            e.renderer.SetPropertyBlock(block, e.materialIndex);
        }
    }

    void UpdateStreak(float strength, Color color)
    {
        if (streak == null) return;

        streak.enabled = strength > 0.001f;
        if (!streak.enabled) return;

        float length = Mathf.Lerp(streakMinLength, streakMaxLength, boost);

        Transform t = streak.transform;
        t.localPosition = streakStart + new Vector3(0f, 0f, -length * 0.5f);
        t.localScale = new Vector3(streakWidth, length, 1f);

        color.a = Mathf.Clamp01(strength * streakMaxAlpha);
        streak.color = color;
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

    Color BoostedColor(int index)
    {
        return Color.Lerp(GetColor(index), boostColor, boost * boostColorBlend);
    }

    Color GetColor(int index)
    {
        if (mode == ColorMode.Solid) return solidColor;
        return Color.HSVToRGB(Mathf.Repeat(hue + index * hueSpread, 1f), saturation, 1f);
    }
}