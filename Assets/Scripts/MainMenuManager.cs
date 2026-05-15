using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static bool OpenAchievementsOnStart = false;

    [Header("Кнопки")]
    public Button playButton;
    public Button quitButton;
    public Button playWithKeyboardButton; // Кнопка для ПК версии

    [Header("Справка по управлению")]
    public GameObject controlsCanvasPrefab; // НОВОЕ: Префаб Canvas справки

    [Header("Настройки сцен")]
    public string gameSceneName = "GameScene"; // Имя вашей игровой сцены

    [Header("Эффекты")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public ParticleSystem buttonClickEffect;

    [Header("Анимации")]
    public Animator menuAnimator;
    public GameObject loadingPanel;

    // Статическая переменная для передачи в игровую сцену
    public static bool ShowControlsInGame { get; private set; } = false;
    public static GameObject ControlsPrefabToUse { get; private set; } = null;

    void Start()
    {
        // Сбрасываем флаги при старте меню
        ShowControlsInGame = false;
        ControlsPrefabToUse = null;

        // Настраиваем кнопки
        if (playButton != null)
        {
            playButton.onClick.AddListener(() => PlayGame(false)); // Без справки
        }

        if (playWithKeyboardButton != null)
        {
            playWithKeyboardButton.onClick.AddListener(() => PlayGame(true)); // Со справкой
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        // Восстанавливаем нормальную скорость времени
        Time.timeScale = 1f;

        Debug.Log("Главное меню загружено");

        // Автопоиск игровой сцены
        if (string.IsNullOrEmpty(gameSceneName))
        {
            FindGameScene();
        }
    }

    // Метод для начала игры с опцией показа справки
    public void PlayGame(bool showControls)
    {
        Debug.Log($"Нажата кнопка 'Играть', загрузка сцены: {gameSceneName}");

        // Устанавливаем флаги для передачи в игровую сцену
        ShowControlsInGame = showControls;
        if (showControls && controlsCanvasPrefab != null)
        {
            ControlsPrefabToUse = controlsCanvasPrefab;
        }

        if (showControls)
        {
            Debug.Log("Игра будет запущена с отображением справки управления");
        }
        else
        {
            Debug.Log("Игра будет запущена без справки");
        }

        // Эффекты нажатия
        PlayButtonEffects();

        // Проверяем существование сцены
        if (!SceneExists(gameSceneName))
        {
            Debug.LogError($"Игровая сцена '{gameSceneName}' не найдена в Build Settings!");
            ShowErrorMessage($"Сцена '{gameSceneName}' не найдена!");
            return;
        }

        // Показываем анимацию/загрузку
        ShowLoadingScreen();

        // Загружаем игровую сцену
        SceneManager.LoadScene(gameSceneName);
    }

    // Метод для выхода из игры
    public void QuitGame()
    {
        Debug.Log("Выход из игры...");

        PlayButtonEffects();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Воспроизведение эффектов кнопки
    private void PlayButtonEffects()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }

        if (buttonClickEffect != null)
        {
            buttonClickEffect.Play();
        }
    }

    // Проверка существования сцены
    private bool SceneExists(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameInBuild == sceneName)
                return true;
        }

        return false;
    }

    // Автопоиск игровой сцены
    private void FindGameScene()
    {
        string[] gameScenePatterns = { "Game", "Level", "Play", "MainGame", "Tetris" };

        foreach (string pattern in gameScenePatterns)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                // Пропускаем текущую сцену (главное меню)
                if (SceneManager.GetActiveScene().name == sceneName)
                    continue;

                if (sceneName.Contains(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    gameSceneName = sceneName;
                    Debug.Log($"Автоматически найдена игровая сцена: {gameSceneName}");
                    return;
                }
            }
        }

        Debug.LogWarning("Игровая сцена не найдена автоматически");
    }

    // Показ экрана загрузки
    private void ShowLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (menuAnimator != null)
        {
            menuAnimator.SetTrigger("StartGame");
        }
    }

    // Показать сообщение об ошибке (опционально)
    private void ShowErrorMessage(string message)
    {
        Debug.LogError(message);
        // Можно добавить UI для отображения ошибок
    }

    // Дополнительные кнопки (опционально)
    public void OpenSettings()
    {
        Debug.Log("Открытие настроек");
        PlayButtonEffects();
        // Здесь можно открыть панель настроек
    }

    public void OpenCredits()
    {
        Debug.Log("Открытие титров");
        PlayButtonEffects();
        // Здесь можно открыть титры
    }

    // Старый метод для совместимости
    public void PlayWithKeyboard()
    {
        PlayGame(true); // Запуск со справкой
    }

    // Метод для проверки, мобильная ли платформа
    private bool IsMobilePlatform()
    {
        return Application.isMobilePlatform ||
               SystemInfo.deviceType == DeviceType.Handheld;
    }

    // Контекстное меню для удобной настройки
    [ContextMenu("Настроить кнопки меню")]
    void SetupMenuButtons()
    {
        // Автопоиск кнопок в сцене
        Button[] allButtons = FindObjectsOfType<Button>(true);

        foreach (Button button in allButtons)
        {
            string buttonText = GetButtonText(button);

            if (buttonText.Contains("Играть") && !buttonText.Contains("ПК") && !buttonText.Contains("комп"))
            {
                playButton = button;
                Debug.Log($"Найдена кнопка 'Играть': {button.name}");
            }
            else if (buttonText.Contains("ПК") || buttonText.Contains("комп") ||
                     buttonText.Contains("клавиатур") || buttonText.Contains("управлен"))
            {
                playWithKeyboardButton = button;
                Debug.Log($"Найдена кнопка 'Играть с ПК': {button.name}");
            }
            else if (buttonText.Contains("Выход") || buttonText.Contains("Quit") ||
                     buttonText.Contains("Exit"))
            {
                quitButton = button;
                Debug.Log($"Найдена кнопка 'Выход': {button.name}");
            }
        }
    }

    // Вспомогательный метод для получения текста кнопки
    private string GetButtonText(Button button)
    {
        // Пробуем получить TextMeshPro
        TMPro.TextMeshProUGUI tmpText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            return tmpText.text.ToLower();
        }

        // Пробуем получить обычный Text
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            return text.text.ToLower();
        }

        return "";
    }

    // Контекстное меню для поиска префаба справки
    [ContextMenu("Найти префаб справки")]
    void FindControlsPrefab()
    {
        // Ищем в папке Resources
        GameObject[] foundPrefabs = Resources.LoadAll<GameObject>("");

        foreach (GameObject prefab in foundPrefabs)
        {
            if (prefab.name.Contains("Controls") ||
                prefab.name.Contains("Keyboard") ||
                prefab.name.Contains("Help"))
            {
                controlsCanvasPrefab = prefab;
                Debug.Log($"Найден префаб справки: {prefab.name}");
                return;
            }
        }

        // Ищем в проекте
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("Controls t:Prefab");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            controlsCanvasPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"Найден префаб справки: {controlsCanvasPrefab.name}");
        }
        else
        {
            Debug.LogWarning("Префаб справки не найден в проекте!");
        }
#endif
    }
}