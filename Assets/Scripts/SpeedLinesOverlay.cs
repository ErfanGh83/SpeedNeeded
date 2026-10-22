using UnityEngine;
using UnityEngine.UI;

public class SpeedLinesOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerSpeed speed;

    [Header("Look")]
    [SerializeField] Color color = Color.white;
    [Range(0f, 1f)]
    [SerializeField] float maxAlpha = 0.7f;
    [SerializeField] int lineCount = 90;
    [Tooltip("Size of the empty area in the middle (0 = lines reach the center, 1 = only the corners).")]
    [Range(0f, 1f)]
    [SerializeField] float clearCenter = 0.45f;
    [Tooltip("How softly the lines fade in from the clear area.")]
    [Range(0.05f, 1f)]
    [SerializeField] float edgeSoftness = 0.35f;
    [Range(0.05f, 1f)]
    [SerializeField] float minThickness = 0.15f;
    [Range(0.05f, 1f)]
    [SerializeField] float maxThickness = 0.6f;

    [Header("Animation")]
    [Range(2, 8)]
    [SerializeField] int frameCount = 4;
    [SerializeField] float framesPerSecond = 12f;

    [Header("Response")]
    [Range(0f, 1f)]
    [SerializeField] float startAtBoost = 0.15f;
    [SerializeField] float response = 5f;
    [SerializeField] int sortingOrder = 50;

    const int TextureSize = 256;

    Texture2D[] frames;
    RawImage image;
    float amount;
    float frameTimer;
    int frameIndex;

    void Awake()
    {
        if (speed == null) speed = FindAnyObjectByType<PlayerSpeed>();
        if (speed == null) Debug.LogWarning("SpeedLinesOverlay: no PlayerSpeed found.", this);

        BuildFrames();
        BuildCanvas();
    }

    void OnDestroy()
    {
        if (frames == null) return;
        foreach (Texture2D t in frames)
            if (t != null) Destroy(t);
    }

    void BuildCanvas()
    {
        var canvasObject = new GameObject("SpeedLinesCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var imageObject = new GameObject("Lines", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);

        var rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        image = imageObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = frames[0];
        image.enabled = false;
    }

    void BuildFrames()
    {
        frames = new Texture2D[frameCount];
        var pixels = new Color32[TextureSize * TextureSize];

        for (int f = 0; f < frameCount; f++)
        {
            var rng = new System.Random(1234 + f * 7919);

            var thickness = new float[lineCount];
            var start = new float[lineCount];
            var brightness = new float[lineCount];

            for (int i = 0; i < lineCount; i++)
            {
                thickness[i] = Mathf.Lerp(minThickness, maxThickness, (float)rng.NextDouble());
                start[i] = clearCenter + (float)rng.NextDouble() * 0.35f;
                brightness[i] = 0.4f + (float)rng.NextDouble() * 0.6f;
            }

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float px = (x + 0.5f) / TextureSize * 2f - 1f;
                    float py = (y + 0.5f) / TextureSize * 2f - 1f;

                    float radius = Mathf.Sqrt(px * px + py * py) / 1.4142f;
                    float angle = Mathf.Atan2(py, px) / (Mathf.PI * 2f) + 0.5f;

                    float s = angle * lineCount;
                    int index = Mathf.Clamp(Mathf.FloorToInt(s), 0, lineCount - 1);
                    float within = Mathf.Abs((s - index) * 2f - 1f);

                    float line = 1f - Mathf.SmoothStep(0f, 1f, within / thickness[index]);
                    float radial = Mathf.SmoothStep(0f, 1f, (radius - start[index]) / edgeSoftness);

                    byte a = (byte)(Mathf.Clamp01(line * radial * brightness[index]) * 255f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, a);
                }
            }

            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            frames[f] = tex;
        }
    }

    void Update()
    {
        float boost = speed != null && GameManager.IsPlaying ? speed.BoostFactor : 0f;
        float target = boost > startAtBoost ? Mathf.InverseLerp(startAtBoost, 1f, boost) : 0f;

        amount = Mathf.Lerp(amount, target, 1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
        if (amount < 0.001f) amount = 0f;

        image.enabled = amount > 0f;
        if (!image.enabled) return;

        frameTimer += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, framesPerSecond);
        if (frameTimer >= interval)
        {
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            image.texture = frames[frameIndex];
        }

        Color c = color;
        c.a = amount * maxAlpha * color.a;
        image.color = c;
    }
}