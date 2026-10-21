using TMPro;
using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The needle image's RectTransform (the pivot should be at the needle's base).")]
    [SerializeField] RectTransform needle;

    [Tooltip("Text that shows only the number (no 'km/h').")]
    [SerializeField] TMP_Text speedText;

    [Header("Speed")]
    [Tooltip("Speed that puts the needle at its maximum rotation.")]
    [SerializeField] float maxSpeed = 240f;

    [Tooltip("Multiplies the measured speed. 3.6 converts m/s to km/h.")]
    [SerializeField] float speedMultiplier = 3.6f;

    [Tooltip("Averaging window in seconds. Higher = more stable speed, but slower to react.")]
    [SerializeField, Min(0.05f)] float sampleInterval = 0.25f;

    [Tooltip("How quickly the needle catches up to the measured speed. Higher = snappier.")]
    [SerializeField] float needleSmoothing = 8f;

    [Header("Number")]
    [Tooltip("How often the number is refreshed (seconds).")]
    [SerializeField, Min(0.05f)] float textUpdateInterval = 0.2f;

    [Tooltip("Round the displayed number to the nearest multiple of this (1 = no rounding, 5 = 0, 5, 10...).")]
    [SerializeField, Min(1)] int roundTo = 5;

    [Header("Needle")]
    [SerializeField] float minAngle = 180f;  // needle at 0 speed
    [SerializeField] float maxAngle = -80f;  // needle at max speed

    float lastDistance;
    float sampleDistance;
    float sampleTime;

    float rawSpeed;        // averaged speed from the last window
    float displayedSpeed;  // smoothed, drives the needle
    float textTimer;

    void OnEnable()
    {
        if (GameManager.Instance != null)
            lastDistance = GameManager.Instance.Distance;

        sampleDistance = 0f;
        sampleTime = 0f;
        rawSpeed = 0f;
        displayedSpeed = 0f;
        textTimer = 0f;

        UpdateNeedle(0f);
        UpdateText(0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && Time.deltaTime > 0f) // deltaTime is 0 while paused
        {
            float delta = gm.Distance - lastDistance;
            lastDistance = gm.Distance;

            if (delta < 0f)
            {
                sampleDistance = 0f;
                sampleTime = 0f;
                rawSpeed = 0f;
            }
            else
            {
                sampleDistance += delta;
                sampleTime += Time.deltaTime;

                if (sampleTime >= sampleInterval)
                {
                    rawSpeed = (sampleDistance / sampleTime) * speedMultiplier;
                    sampleDistance = 0f;
                    sampleTime = 0f;
                }
            }
        }

        if (gm != null && !gm.IsRunning && gm.State != GameState.Paused)
            rawSpeed = 0f;

        displayedSpeed = Mathf.Lerp(displayedSpeed, rawSpeed, 1f - Mathf.Exp(-needleSmoothing * Time.unscaledDeltaTime));
        UpdateNeedle(displayedSpeed);

        textTimer += Time.unscaledDeltaTime;
        if (textTimer >= textUpdateInterval)
        {
            textTimer = 0f;
            UpdateText(displayedSpeed);
        }
    }

    public void SetSpeed(float speed)
    {
        rawSpeed = speed;
    }

    void UpdateNeedle(float speed)
    {
        float t = Mathf.Clamp01(speed / maxSpeed);
        float angle = Mathf.Lerp(minAngle, maxAngle, t);

        if (needle != null)
            needle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void UpdateText(float speed)
    {
        if (speedText == null) return;

        int step = Mathf.Max(1, roundTo);
        int shown = Mathf.RoundToInt(speed / step) * step;
        speedText.text = shown.ToString();
    }
}