using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Аудиоисточники (создаются автоматически)")]
    public AudioSource positiveSource;
    public AudioSource negativeSource;
    public AudioSource musicSource;
    public AudioSource effectsSource;

    [Header("Тестовые звуки для ползунков (опционально)")]
    public AudioClip positiveTestSound;
    public AudioClip negativeTestSound;
    public AudioClip musicTestSound;
    public AudioClip effectsTestSound;

    [Header("Громкость по умолчанию")]
    [Range(0f, 1f)] public float defaultMasterVolume = 0.8f;
    [Range(0f, 1f)] public float defaultPositiveVolume = 0.8f;
    [Range(0f, 1f)] public float defaultNegativeVolume = 0.8f;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.6f;
    [Range(0f, 1f)] public float defaultEffectsVolume = 0.8f;

    [Header("UI элементы (назначаются автоматически)")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;
    public Slider masterSlider;
    public Slider positiveSlider;
    public Slider negativeSlider;
    public Slider musicSlider;
    public Slider effectsSlider;

    // Текущие значения громкости
    private float masterVolume;
    private float positiveVolume;
    private float negativeVolume;
    private float musicVolume;
    private float effectsVolume;

    private const string PREFS_MASTER = "AudioMaster";
    private const string PREFS_POSITIVE = "AudioPositive";
    private const string PREFS_NEGATIVE = "AudioNegative";
    private const string PREFS_MUSIC = "AudioMusic";
    private const string PREFS_EFFECTS = "AudioEffects";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Создаём аудиоисточники, если не назначены
        if (positiveSource == null) positiveSource = gameObject.AddComponent<AudioSource>();
        if (negativeSource == null) negativeSource = gameObject.AddComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (effectsSource == null) effectsSource = gameObject.AddComponent<AudioSource>();

        // Настройка музыкального источника
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        LoadVolumes();
        ApplyVolumes();
    }

    void Start()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndConnectUI();
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        FindAndConnectUI();
    }

    void FindAndConnectUI()
    {
        // Ищем панель настроек по имени
        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingsPanel");

        if (settingsPanel != null)
        {
            // Ищем кнопки
            if (openSettingsButton == null)
            {
                Button btn = settingsPanel.transform.Find("OpenButton")?.GetComponent<Button>();
                if (btn == null) btn = settingsPanel.GetComponentInChildren<Button>();
                openSettingsButton = btn;
            }

            if (closeSettingsButton == null)
            {
                Button btn = settingsPanel.transform.Find("CloseButton")?.GetComponent<Button>();
                if (btn == null)
                {
                    Button[] btns = settingsPanel.GetComponentsInChildren<Button>();
                    foreach (Button b in btns)
                        if (b.name.ToLower().Contains("close"))
                            closeSettingsButton = b;
                }
            }

            // Ищем слайдеры
            Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>();
            foreach (Slider s in sliders)
            {
                string name = s.name.ToLower();
                if (name.Contains("master") && masterSlider == null) masterSlider = s;
                else if (name.Contains("positive") && positiveSlider == null) positiveSlider = s;
                else if (name.Contains("negative") && negativeSlider == null) negativeSlider = s;
                else if (name.Contains("music") && musicSlider == null) musicSlider = s;
                else if (name.Contains("effect") && effectsSlider == null) effectsSlider = s;
            }
        }

        // Назначаем обработчики кнопок
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveListener(OpenSettingsPanel);
            openSettingsButton.onClick.AddListener(OpenSettingsPanel);
        }
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettingsPanel);
            closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        }

        // Настраиваем слайдеры
        SetupSlider(masterSlider, OnMasterChanged, masterVolume);
        SetupSlider(positiveSlider, OnPositiveChanged, positiveVolume);
        SetupSlider(negativeSlider, OnNegativeChanged, negativeVolume);
        SetupSlider(musicSlider, OnMusicChanged, musicVolume);
        SetupSlider(effectsSlider, OnEffectsChanged, effectsVolume);

        // Панель по умолчанию скрыта
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void SetupSlider(Slider slider, System.Action<float> onChanged, float defaultValue)
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;
            slider.onValueChanged.AddListener((v) => onChanged(v));
        }
    }

    void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER, defaultMasterVolume);
        positiveVolume = PlayerPrefs.GetFloat(PREFS_POSITIVE, defaultPositiveVolume);
        negativeVolume = PlayerPrefs.GetFloat(PREFS_NEGATIVE, defaultNegativeVolume);
        musicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC, defaultMusicVolume);
        effectsVolume = PlayerPrefs.GetFloat(PREFS_EFFECTS, defaultEffectsVolume);
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PREFS_MASTER, masterVolume);
        PlayerPrefs.SetFloat(PREFS_POSITIVE, positiveVolume);
        PlayerPrefs.SetFloat(PREFS_NEGATIVE, negativeVolume);
        PlayerPrefs.SetFloat(PREFS_MUSIC, musicVolume);
        PlayerPrefs.SetFloat(PREFS_EFFECTS, effectsVolume);
        PlayerPrefs.Save();
    }

    void ApplyVolumes()
    {
        if (positiveSource != null) positiveSource.volume = positiveVolume * masterVolume;
        if (negativeSource != null) negativeSource.volume = negativeVolume * masterVolume;
        if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
        if (effectsSource != null) effectsSource.volume = effectsVolume * masterVolume;
    }

    void OnMasterChanged(float v) { masterVolume = v; ApplyVolumes(); SaveVolumes(); PlayTestSound(positiveTestSound, positiveSource); }
    void OnPositiveChanged(float v) { positiveVolume = v; ApplyVolumes(); SaveVolumes(); PlayTestSound(positiveTestSound, positiveSource); }
    void OnNegativeChanged(float v) { negativeVolume = v; ApplyVolumes(); SaveVolumes(); PlayTestSound(negativeTestSound, negativeSource); }
    void OnMusicChanged(float v) { musicVolume = v; ApplyVolumes(); SaveVolumes(); PlayTestSound(musicTestSound, musicSource); }
    void OnEffectsChanged(float v) { effectsVolume = v; ApplyVolumes(); SaveVolumes(); PlayTestSound(effectsTestSound, effectsSource); }

    void PlayTestSound(AudioClip clip, AudioSource source)
    {
        if (clip != null && source != null) source.PlayOneShot(clip);
    }

    // ----- Публичные методы для проигрывания звуков (их вызывать из ваших скриптов) -----
    public void PlayPositiveSound(AudioClip clip)
    {
        if (clip != null && positiveSource != null) positiveSource.PlayOneShot(clip);
    }

    public void PlayNegativeSound(AudioClip clip)
    {
        if (clip != null && negativeSource != null) negativeSource.PlayOneShot(clip);
    }

    public void PlayEffectSound(AudioClip clip)
    {
        if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null) return;
        if (musicSource.clip != clip || !musicSource.isPlaying)
        {
            musicSource.loop = loop;
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}