using UnityEngine;

public class LedBoostGlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerSpeed speed;
    [SerializeField] Light[] lights;

    [Header("Light")]
    [SerializeField] Color idleColor = new Color(0.15f, 0.5f, 1f);
    [SerializeField] Color boostColor = new Color(0.6f, 0.9f, 1f);
    [SerializeField] float idleIntensity = 1f;
    [SerializeField] float boostIntensity = 6f;
    [SerializeField] float idleRange = 4f;
    [SerializeField] float boostRange = 9f;
    [SerializeField] float idleSpotAngle = 90f;
    [SerializeField] float boostSpotAngle = 130f;

    [Header("Flicker")]
    [Range(0f, 0.5f)]
    [SerializeField] float flicker = 0.12f;
    [SerializeField] float flickerSpeed = 25f;

    [Header("Soft streak")]
    [SerializeField] bool showStreak = true;
    [SerializeField] float streakWidth = 1.4f;
    [SerializeField] float streakMinLength = 0.5f;
    [SerializeField] float streakMaxLength = 7f;
    [Range(0f, 1f)]
    [SerializeField] float streakMaxAlpha = 0.6f;
    [SerializeField] float streakHeight = 0.03f;
    [SerializeField] int sortingOrder = 5;

    [Header("Response")]
    [Tooltip("Higher = the glow follows the boost faster.")]
    [SerializeField] float response = 6f;

    float factor;
    Texture2D texture;
    Sprite sprite;
    SpriteRenderer streak;

    void Awake()
    {
        if (speed == null) speed = GetComponentInParent<PlayerSpeed>();
        if (showStreak) CreateStreak();
    }

    void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }

    void CreateStreak()
    {
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
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
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, side * tail));
            }
        }
        texture.Apply();

        sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

        var go = new GameObject("BoostStreak");
        go.transform.SetParent(transform, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        streak = go.AddComponent<SpriteRenderer>();
        streak.sprite = sprite;
        streak.sortingOrder = sortingOrder;
        streak.enabled = false;
    }

    void Update()
    {
        float target = speed != null && GameManager.IsPlaying ? speed.BoostFactor : 0f;
        factor = Mathf.Lerp(factor, target, 1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
        if (factor < 0.001f) factor = 0f;

        float noise = (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0.37f) - 0.5f) * 2f;
        float flick = 1f + noise * flicker * factor;

        Color color = Color.Lerp(idleColor, boostColor, factor);

        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null) continue;

            l.color = color;
            l.intensity = Mathf.Lerp(idleIntensity, boostIntensity, factor) * flick;
            l.range = Mathf.Lerp(idleRange, boostRange, factor);
            if (l.type == LightType.Spot) l.spotAngle = Mathf.Lerp(idleSpotAngle, boostSpotAngle, factor);
        }

        if (streak != null) UpdateStreak(color, flick);
    }

    void UpdateStreak(Color color, float flick)
    {
        streak.enabled = factor > 0f;
        if (!streak.enabled) return;

        float length = Mathf.Lerp(streakMinLength, streakMaxLength, factor);

        Transform t = streak.transform;
        t.localPosition = new Vector3(0f, streakHeight, -length * 0.5f);
        t.localScale = new Vector3(streakWidth, length, 1f);

        color.a = factor * streakMaxAlpha * flick;
        streak.color = color;
    }
}