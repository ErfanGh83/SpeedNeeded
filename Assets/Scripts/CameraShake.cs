using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] float frequency = 25f;
    [Tooltip("Sideways tilt in degrees at full strength.")]
    [SerializeField] float rollDegrees = 2f;

    Camera cam;
    float strength;
    float duration;
    float timer;
    bool applied;
    Vector3 positionOffset;
    Quaternion rotationOffset = Quaternion.identity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        Instance = this;
        Camera.onPreCull += HandleCameraPreCull;
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        Camera.onPreCull -= HandleCameraPreCull;
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        Undo();
    }

    public void Shake(float newStrength, float newDuration)
    {
        float remaining = duration > 0f ? strength * Mathf.Clamp01(1f - timer / duration) : 0f;
        strength = Mathf.Max(remaining, newStrength);
        duration = Mathf.Max(0.01f, newDuration);
        timer = 0f;
    }

    void Update()
    {
        Undo();
        timer += Time.unscaledDeltaTime;
    }

    void HandleCameraPreCull(Camera c)
    {
        if (c == cam) Apply();
    }

    void OnBeginCamera(ScriptableRenderContext context, Camera c)
    {
        if (c == cam) Apply();
    }

    void Apply()
    {
        if (applied || strength <= 0f) return;

        float k = 1f - timer / duration;
        if (k <= 0f)
        {
            strength = 0f;
            return;
        }

        float amount = strength * k * k;
        float time = Time.unscaledTime * frequency;

        float x = (Mathf.PerlinNoise(time, 0.1f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(time, 7.3f) - 0.5f) * 2f;
        float roll = (Mathf.PerlinNoise(time, 13.9f) - 0.5f) * 2f;

        positionOffset = new Vector3(x, y, 0f) * amount;
        rotationOffset = Quaternion.Euler(0f, 0f, roll * rollDegrees * k * k * (strength > 0f ? 1f : 0f));

        transform.localPosition += positionOffset;
        transform.localRotation *= rotationOffset;
        applied = true;
    }

    void Undo()
    {
        if (!applied) return;

        transform.localRotation *= Quaternion.Inverse(rotationOffset);
        transform.localPosition -= positionOffset;
        applied = false;
    }
}