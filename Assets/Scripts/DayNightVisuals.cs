using UnityEngine;

public class DayNightVisuals : MonoBehaviour
{
    [Header("References")]
    [Tooltip("All directional lights to dim. If empty, the scene's sun (Lighting settings) is used.")]
    [SerializeField] Light[] suns;
    [SerializeField] Material daySkybox;
    [SerializeField] Material nightSkybox;

    [Header("Sun")]
    [Tooltip("Reference day intensity. Each light is scaled by Night Intensity / Day Intensity.")]
    [SerializeField] float dayIntensity = 1.2f;
    [SerializeField] float nightIntensity = 0.15f;
    [SerializeField] Color nightColor = new Color(0.45f, 0.55f, 1f);

    [Header("Ambient")]
    [SerializeField] float dayAmbient = 1f;
    [SerializeField] float nightAmbient = 0.3f;

    [Header("Fog")]
    [SerializeField] bool changeFog = false;
    [SerializeField] Color dayFog = new Color(0.75f, 0.82f, 0.9f);
    [SerializeField] Color nightFog = new Color(0.05f, 0.07f, 0.15f);

    [Header("Transition")]
    [Tooltip("Seconds to change from day to night and back. The sky fades out and the other one fades in.")]
    [SerializeField] float transitionDuration = 4f;

    static readonly int ExposureId = Shader.PropertyToID("_Exposure");

    Material dayMat, nightMat;
    float dayExposure = 1f, nightExposure = 1f;
    bool skyReady;

    float[] baseIntensity;
    Color[] baseColor;

    float blend;  // 0 = day, 1 = night
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
        if (suns == null || suns.Length == 0)
        {
            if (RenderSettings.sun != null) suns = new[] { RenderSettings.sun };
            else Debug.LogWarning("DayNightVisuals: no sun Light assigned, only sky and ambient will change.", this);
        }

        CaptureLights();
        SetupSky();

        var manager = DayNightCycleManager.Instance;
        if (manager == null) Debug.LogWarning("DayNightVisuals: no DayNightCycleManager in the scene, staying on day.", this);

        target = manager != null && manager.Current == TimeOfDay.Night ? 1f : 0f;
        blend = target;

        Apply();
        DynamicGI.UpdateEnvironment();
    }

    void OnDestroy()
    {
        if (dayMat != null) Destroy(dayMat);
        if (nightMat != null) Destroy(nightMat);
    }

    void CaptureLights()
    {
        int count = suns != null ? suns.Length : 0;
        baseIntensity = new float[count];
        baseColor = new Color[count];

        for (int i = 0; i < count; i++)
        {
            if (suns[i] == null) continue;
            baseIntensity[i] = suns[i].intensity;
            baseColor[i] = suns[i].color;
        }
    }

    void SetupSky()
    {
        if (daySkybox == null || nightSkybox == null)
        {
            Debug.LogWarning("DayNightVisuals: assign both skybox materials to change the sky.", this);
            return;
        }

        // Copies, so the fade never changes the material assets in the project
        dayMat = new Material(daySkybox);
        nightMat = new Material(nightSkybox);

        if (dayMat.HasProperty(ExposureId)) dayExposure = dayMat.GetFloat(ExposureId);
        if (nightMat.HasProperty(ExposureId)) nightExposure = nightMat.GetFloat(ExposureId);

        skyReady = true;
    }

    void OnTimeChanged(TimeOfDay time)
    {
        target = time == TimeOfDay.Night ? 1f : 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(blend, target)) return;

        float step = transitionDuration > 0f ? Time.deltaTime / transitionDuration : 1f;
        blend = Mathf.MoveTowards(blend, target, step);
        Apply();

        // Ambient from the skybox is only recalculated on request, and it is correct only when the sky is fully visible
        if (Mathf.Approximately(blend, target)) DynamicGI.UpdateEnvironment();
    }

    void Apply()
    {
        ApplyLights();

        RenderSettings.ambientIntensity = Mathf.Lerp(dayAmbient, nightAmbient, blend);

        if (changeFog) RenderSettings.fogColor = Color.Lerp(dayFog, nightFog, blend);

        if (skyReady) ApplySky();
    }

    void ApplyLights()
    {
        if (suns == null) return;

        float ratio = dayIntensity > 0f ? nightIntensity / dayIntensity : 1f;
        float k = Mathf.Lerp(1f, ratio, blend);

        for (int i = 0; i < suns.Length; i++)
        {
            if (suns[i] == null) continue;

            suns[i].intensity = baseIntensity[i] * k;
            suns[i].color = Color.Lerp(baseColor[i], nightColor, blend);
        }
    }

    void ApplySky()
    {
        bool night = blend >= 0.5f;
        Material sky = night ? nightMat : dayMat;

        if (RenderSettings.skybox != sky) RenderSettings.skybox = sky;

        // First half: current sky fades to black. Second half: the other sky fades in.
        float fade = night ? (blend - 0.5f) * 2f : 1f - blend * 2f;
        fade = Mathf.SmoothStep(0f, 1f, fade);

        if (sky.HasProperty(ExposureId))
            sky.SetFloat(ExposureId, (night ? nightExposure : dayExposure) * fade);
    }
}