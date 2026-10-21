using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The fill image. Its Image Type is switched to Filled automatically.")]
    [SerializeField] Image fillImage;

    [Tooltip("Leave empty to find the HealthSystem in the scene automatically.")]
    [SerializeField] HealthSystem health;

    [Header("Smoothing")]
    [Tooltip("How quickly the bar catches up after a hit. 0 = instant.")]
    [SerializeField, Min(0f)] float smoothing = 10f;

    float targetFill = 1f;

    void Awake()
    {
        if (health == null)
            health = FindAnyObjectByType<HealthSystem>();

        if (fillImage != null && fillImage.type != Image.Type.Filled)
            fillImage.type = Image.Type.Filled;
    }

    void OnEnable()
    {
        if (health == null) return;

        health.OnHealthChanged += OnHealthChanged;

        // Show the current value right away
        targetFill = health.Normalized;
        SetFill(targetFill);
    }

    void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (fillImage == null) return;

        float current = fillImage.fillAmount;
        if (Mathf.Approximately(current, targetFill)) return;

        if (smoothing <= 0f)
        {
            SetFill(targetFill);
            return;
        }

        // Unscaled time so the bar still settles while the game is paused
        float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        SetFill(Mathf.Lerp(current, targetFill, t));
    }

    void OnHealthChanged(int current, int max)
    {
        targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (smoothing <= 0f)
            SetFill(targetFill);
    }

    void SetFill(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = value;
    }
}