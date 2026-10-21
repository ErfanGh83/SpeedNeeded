using UnityEngine;
using UnityEngine.UI;

public class FuelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The fill image. Its Image Type is switched to Filled automatically.")]
    [SerializeField] Image fillImage;

    [Tooltip("Leave empty to find the FuelSystem in the scene automatically.")]
    [SerializeField] FuelSystem fuelSystem;

    [Header("Smoothing")]
    [Tooltip("How quickly the bar catches up to the real fuel. 0 = instant.")]
    [SerializeField, Min(0f)] float smoothing = 10f;

    float targetFill = 1f;

    void Awake()
    {
        if (fuelSystem == null)
            fuelSystem = FindAnyObjectByType<FuelSystem>();

        if (fillImage != null && fillImage.type != Image.Type.Filled)
            fillImage.type = Image.Type.Filled;
    }

    void OnEnable()
    {
        if (fuelSystem == null) return;

        fuelSystem.OnFuelChanged += OnFuelChanged;

        // Show the current value right away
        targetFill = fuelSystem.Normalized;
        SetFill(targetFill);
    }

    void OnDisable()
    {
        if (fuelSystem != null)
            fuelSystem.OnFuelChanged -= OnFuelChanged;
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

    void OnFuelChanged(float fuel, float maxFuel)
    {
        targetFill = maxFuel > 0f ? Mathf.Clamp01(fuel / maxFuel) : 0f;

        if (smoothing <= 0f)
            SetFill(targetFill);
    }

    void SetFill(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = value;
    }
}