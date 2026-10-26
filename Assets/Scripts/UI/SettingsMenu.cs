using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button applyButton;
    [SerializeField] Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    const string MusicParam = "MusicVolume";
    const string SfxParam = "SfxVolume";

    float savedMusic;
    float savedSfx;
    TMP_Text applyLabel;
    Color enabledTextColor;

    void Awake()
    {
        if (applyButton != null)
        {
            applyLabel = applyButton.GetComponentInChildren<TMP_Text>();
            if (applyLabel != null) enabledTextColor = applyLabel.color;
        }

        musicSlider.minValue = 0.0001f;
        sfxSlider.minValue = 0.0001f;
        musicSlider.maxValue = 1f;
        sfxSlider.maxValue = 1f;

        savedMusic = PlayerPrefs.GetFloat(MusicParam, 1f);
        savedSfx = PlayerPrefs.GetFloat(SfxParam, 1f);

        ResetToSaved();

        musicSlider.onValueChanged.AddListener(v => OnSliderChanged(MusicParam, v));
        sfxSlider.onValueChanged.AddListener(v => OnSliderChanged(SfxParam, v));
    }

    void Start()
    {
        // The mixer ignores SetFloat calls made in Awake
        ResetToSaved();
    }

    public void ApplySettings()
    {
        savedMusic = musicSlider.value;
        savedSfx = sfxSlider.value;

        PlayerPrefs.SetFloat(MusicParam, savedMusic);
        PlayerPrefs.SetFloat(SfxParam, savedSfx);
        PlayerPrefs.Save();

        RefreshApplyButton();
    }

    public void ResetToSaved()
    {
        musicSlider.SetValueWithoutNotify(savedMusic);
        sfxSlider.SetValueWithoutNotify(savedSfx);

        SetVolume(MusicParam, savedMusic);
        SetVolume(SfxParam, savedSfx);

        RefreshApplyButton();
    }

    void OnSliderChanged(string param, float value)
    {
        SetVolume(param, value);
        RefreshApplyButton();
    }

    bool HasChanges()
    {
        return !Mathf.Approximately(musicSlider.value, savedMusic)
            || !Mathf.Approximately(sfxSlider.value, savedSfx);
    }

    void RefreshApplyButton()
    {
        if (applyButton == null) return;

        bool changed = HasChanges();
        applyButton.interactable = changed;

        if (applyLabel != null)
            applyLabel.color = changed ? enabledTextColor : disabledTextColor;
    }

    void SetVolume(string param, float value)
    {
        mixer.SetFloat(param, Mathf.Log10(value) * 20f);
    }
}