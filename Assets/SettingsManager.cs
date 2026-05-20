using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Музыка")]
    public Slider musicVolumeSlider;
    public Toggle muteToggle;
    public TextMeshProUGUI volumePercentText;

    [Header("Подсказки")]
    public Toggle showHintsToggle;
    public Button nextHintButton;
    public Button prevHintButton;
    public TextMeshProUGUI hintText;

    [Header("Правила")]
    public Button rulesButton;
    public GameObject rulesPanel;
    public Button closeRulesButton;
    public TextMeshProUGUI rulesText;

    [Header("Другое")]
    public Button resetButton;
    public Button closeButton;

    private string[] hints = {
        "В дураке козырь бьёт любую не козырную карту",
        "В покере флеш-рояль - самая сильная комбинация",
        "В 21 туз может быть 11 или 1 очко",
        "Следите за балансом, не ставьте всё сразу",
        "В промокодах можно получить бонусные монеты",
        "Если не знаете какую карту отбить - возьмите карты"
    };
    private int hintIndex = 0;

    void Start()
    {
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (muteToggle != null)
            muteToggle.onValueChanged.AddListener(OnMuteChanged);

        if (showHintsToggle != null)
            showHintsToggle.onValueChanged.AddListener(OnShowHintsChanged);

        

        if (rulesButton != null)
            rulesButton.onClick.AddListener(ShowRules);

        if (closeRulesButton != null)
            closeRulesButton.onClick.AddListener(CloseRules);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        LoadSettings();
        UpdateHintText();

        // Скрываем текст правил в начале
        if (rulesText != null)
            rulesText.gameObject.SetActive(false);
    }

    void LoadSettings()
    {
        float volume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        bool muted = PlayerPrefs.GetInt("Muted", 0) == 1;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = volume;

        if (muteToggle != null)
            muteToggle.isOn = muted;

        if (volumePercentText != null)
            volumePercentText.text = "Громкость: " + Mathf.RoundToInt(volume * 100) + "%";

        bool showHints = PlayerPrefs.GetInt("ShowHints", 1) == 1;
        if (showHintsToggle != null)
            showHintsToggle.isOn = showHints;

        if (hintText != null)
            hintText.gameObject.SetActive(showHints);

        AudioSource audio = FindObjectOfType<AudioSource>();
        if (audio != null)
        {
            audio.volume = volume;
            audio.mute = muted;
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();

        if (volumePercentText != null)
            volumePercentText.text = "Громкость: "+Mathf.RoundToInt(value * 100) + "%";

        AudioSource audio = FindObjectOfType<AudioSource>();
        if (audio != null)
            audio.volume = value;
    }

    public void OnMuteChanged(bool isMuted)
    {
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();

        AudioSource audio = FindObjectOfType<AudioSource>();
        if (audio != null)
            audio.mute = isMuted;
    }

    public void OnShowHintsChanged(bool show)
    {
        PlayerPrefs.SetInt("ShowHints", show ? 1 : 0);
        PlayerPrefs.Save();

        if (hintText != null)
            hintText.gameObject.SetActive(show);
    }



    void UpdateHintText()
    {
        if (hintText != null)
            hintText.text = hints[hintIndex];
    }

    public void ShowRules()
    {
        if (rulesText != null)
        {
            
            rulesText.gameObject.SetActive(true);
        }
    }

    

    public void CloseRules()
    {
        if (rulesText != null)
            rulesText.gameObject.SetActive(false);
    }

    public void ResetSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", 0.7f);
        PlayerPrefs.SetInt("Muted", 0);
        PlayerPrefs.SetInt("ShowHints", 1);
        PlayerPrefs.Save();

        LoadSettings();

        hintIndex = 0;
        UpdateHintText();

        Debug.Log("Настройки сброшены");
    }

    public void CloseSettings()
    {
        gameObject.SetActive(false);
    }
}