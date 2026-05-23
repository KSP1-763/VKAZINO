using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Sett : MonoBehaviour
{
    [Header("Кнопка вкл/выкл")]
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    public Image buttonImage;

    [Header("Слайдер громкости")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumePercentText;

    [Header("Сброс настроек")]
    public Button resetButton;

    private AudioSource audioSource;
    private bool isMusicOn = true;

    void Awake()
    {
        // Ищем AudioSource на сцене
        audioSource = FindObjectOfType<AudioSource>();

        // Если нет — создаём
        if (audioSource == null)
        {
            GameObject go = new GameObject("MusicManager");
            audioSource = go.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            Debug.Log("AudioSource создан автоматически");
        }

        // Загружаем настройки
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);

        // Применяем настройки ДО старта игры
        if (audioSource != null)
        {
            audioSource.volume = savedVolume;
            audioSource.mute = !isMusicOn;
        }
    }

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ToggleMusic);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetSettings);
        }

        UpdateButtonSprite();
        UpdateVolumeText();
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = volume;

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
        UpdateVolumeText();
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;

        if (audioSource != null)
            audioSource.mute = !isMusicOn;

        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtonSprite();
    }

    public void ResetSettings()
    {
        Debug.Log("Сброс настроек");

        isMusicOn = true;
        float defaultVolume = 0.7f;

        if (volumeSlider != null)
            volumeSlider.value = defaultVolume;

        if (audioSource != null)
        {
            audioSource.volume = defaultVolume;
            audioSource.mute = false;
        }

        PlayerPrefs.SetInt("MusicOn", 1);
        PlayerPrefs.SetFloat("MusicVolume", defaultVolume);
        PlayerPrefs.Save();

        UpdateButtonSprite();
        UpdateVolumeText();
    }

    void UpdateButtonSprite()
    {
        if (buttonImage != null)
        {
            if (isMusicOn && musicOnSprite != null)
                buttonImage.sprite = musicOnSprite;
            else if (!isMusicOn && musicOffSprite != null)
                buttonImage.sprite = musicOffSprite;
        }
    }

    void UpdateVolumeText()
    {
        if (volumePercentText != null && volumeSlider != null)
            volumePercentText.text = "Громкость: "+Mathf.RoundToInt(volumeSlider.value * 100) + "%";
    }
}