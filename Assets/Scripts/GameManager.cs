using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Управление")]
    public float fallSpeed = 1f;
    public float fastFallSpeed = 0.1f;
    public Vector3 defaultSpawnPosition = new Vector3(3f, 19f, -1f);

    [Header("UI - Панель паузы")]
    public GameObject pausePanel;

    [Header("Справка по управлению")]
    public GameObject controlsCanvasPrefab; // Префаб Canvas справки
    private GameObject controlsCanvasInstance;

    [Header("Настройки сцен")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Префабы фигур")]
    public GameObject[] shapePrefabs;

    [Header("Настройки предпросмотра для предметных фигур")]
    public List<ShapePreviewSettings> shapePreviewSettings = new List<ShapePreviewSettings>();

    [Header("Настройки предпросмотра по умолчанию")]
    public Vector3 defaultPreviewPosition = new Vector3(12f, 16f, -5f);
    public float defaultPreviewScale = 0.7f;

    [Header("Текущая фигура")]
    public TetrisShape currentShape;

    [Header("Шкала усиления")]
    public PowerScaleManager powerScaleManager;

    [Header("Система счета")]
    public int score = 0;
    public int linesCleared = 0;

    [Header("UI - Отображение счета")]
    public TMP_Text scoreText;

    private TetrisShape nextShape;
    private int nextShapeIndex;
    private float fallTimer = 0f;
    private Keyboard keyboard;
    public FieldGrid fieldGrid;
    private bool gameOver = false;
    private bool isFastFalling = false;
    private bool isPaused = false;

    void Start()
    {
        AspectRatioManager aspectManager = FindObjectOfType<AspectRatioManager>();
        if (aspectManager != null)
        {
            aspectManager.ApplyAspectRatio();
            StartCoroutine(DelayedStart());
            return;
        }

        StandardStart();
    }

    System.Collections.IEnumerator DelayedStart()
    {
        yield return null;
        StandardStart();
    }

    void StandardStart()
    {
        Debug.Log("=== ИГРА ЗАПУЩЕНА ===");
        keyboard = Keyboard.current;
        fieldGrid = FindObjectOfType<FieldGrid>();

        // Скрываем панель паузы при старте
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            // Устанавливаем высокий порядок сортировки для панели паузы
            SetCanvasSortingOrder(pausePanel, 1000);
        }

        // Инициализация справки - ТОЛЬКО если была нажата кнопка "Играть с ПК"
        InitializeControls();

        UpdateScoreUI();
        GenerateNextShape();
        StartGame();
    }

    void StartGame()
    {
        gameOver = false;
        isPaused = false;
        score = 0;
        linesCleared = 0;
        UpdateScoreUI();
        SpawnNewShape();
    }

    void Update()
    {
        // Проверка клавиши P для паузы
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // Проверка клавиши R для рестарта (работает даже из паузы)
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                isPaused = false;
                Time.timeScale = 1f;

                if (pausePanel != null)
                {
                    pausePanel.SetActive(false);
                }
            }

            RestartGame();
            return;
        }

        // Если игра на паузе или закончена - не обрабатываем игровую логику
        if (isPaused || gameOver) return;

        if (currentShape == null) return;

        float currentFallSpeed = isFastFalling ? fastFallSpeed : fallSpeed;

        fallTimer += Time.deltaTime;
        if (fallTimer >= currentFallSpeed)
        {
            if (currentShape.CanMove(Vector2.down))
            {
                currentShape.Move(Vector2.down);
            }
            else
            {
                fieldGrid.LockShape(currentShape);

                int lines = fieldGrid.CheckAndClearLines();
                if (lines > 0)
                {
                    AddScore(lines);
                }

                SpawnNewShape();
            }
            fallTimer = 0f;
        }

        if (keyboard != null)
        {
            if (keyboard.downArrowKey.isPressed)
            {
                isFastFalling = true;
            }
            else
            {
                isFastFalling = false;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (powerScaleManager != null && powerScaleManager.IsPowerReady())
                {
                    powerScaleManager.UsePower();
                }
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame && currentShape.CanMove(Vector2.left))
            {
                currentShape.Move(Vector2.left);
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame && currentShape.CanMove(Vector2.right))
            {
                currentShape.Move(Vector2.right);
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                Debug.Log("Вращение влево (A)");
                if (currentShape != null)
                {
                    currentShape.RotateLeft();
                }
            }

            if (keyboard.dKey.wasPressedThisFrame)
            {
                Debug.Log("Вращение вправо (D)");
                if (currentShape != null)
                {
                    currentShape.RotateRight();
                }
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                Debug.Log("Вращение (стрелка вверх)");
                PencilCupItem pencilCup = currentShape as PencilCupItem;
                if (pencilCup != null)
                {
                    pencilCup.RotateLeft();  // ← было SpillPencils(true), стало RotateLeft()
                }
                else if (currentShape is CupItem cupItem)
                {
                    cupItem.RotateLeft();     // ← было SpillCup(true), стало RotateLeft()
                }
                else
                {
                    currentShape.RotateLeft(); // ← было Rotate(), стало RotateLeft()
                }
            }
        }
    }

    // Инициализация справки - ТОЛЬКО при нажатии "Играть с ПК"
    private void InitializeControls()
    {
        // Проверяем флаг из MainMenuManager - только если была нажата кнопка "Играть с ПК"
        bool shouldShowControls = MainMenuManager.ShowControlsInGame && !IsMobilePlatform();

        if (shouldShowControls && controlsCanvasPrefab != null)
        {
            CreateControlsCanvas();
        }
        else if (!shouldShowControls)
        {
            Debug.Log("Справка не будет показана (кнопка 'Играть с ПК' не была нажата)");
        }
        else if (controlsCanvasPrefab == null)
        {
            Debug.LogWarning("ControlsCanvasPrefab не назначен в GameManager!");
        }
    }

    // Проверка мобильной платформы
    private bool IsMobilePlatform()
    {
        return Application.isMobilePlatform ||
               SystemInfo.deviceType == DeviceType.Handheld;
    }

    // Создание Canvas из префаба
    private void CreateControlsCanvas()
    {
        if (controlsCanvasPrefab == null) return;

        // Создаем экземпляр префаба
        controlsCanvasInstance = Instantiate(controlsCanvasPrefab);
        controlsCanvasInstance.name = "GameControlsCanvas";

        // Устанавливаем НИЗКИЙ порядок сортировки для справки
        SetCanvasSortingOrder(controlsCanvasInstance, 100);

        // Позиционируем в правом верхнем углу
        SetupControlsPosition();

        Debug.Log("Справка по управлению создана из префаба (только для ПК версии)");
    }

    // Настройка позиции справки
    private void SetupControlsPosition()
    {
        if (controlsCanvasInstance == null) return;

        RectTransform rectTransform = controlsCanvasInstance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Правый верхний угол с небольшим отступом
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-20, -20);

            // Устанавливаем размер
            rectTransform.sizeDelta = new Vector2(300, 200);
        }
    }

    // Установка порядка сортировки для Canvas
    private void SetCanvasSortingOrder(GameObject canvasObject, int order)
    {
        if (canvasObject == null) return;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.GetComponentInChildren<Canvas>();
        }

        if (canvas != null)
        {
            canvas.sortingOrder = order;
            Debug.Log($"Установлен sortingOrder {order} для {canvasObject.name}");
        }
    }

    // Метод для переключения справки (если нужно сделать кнопку в паузе)
    public void ToggleControls()
    {
        if (controlsCanvasInstance != null)
        {
            bool isActive = controlsCanvasInstance.activeSelf;
            controlsCanvasInstance.SetActive(!isActive);
            Debug.Log($"Справка {(isActive ? "скрыта" : "показана")}");
        }
    }

    void GenerateNextShape()
    {
        if (nextShape != null)
        {
            Destroy(nextShape.gameObject);
        }

        nextShapeIndex = Random.Range(0, shapePrefabs.Length);
        GameObject nextShapePrefab = shapePrefabs[nextShapeIndex];

        GameObject nextShapeObj = Instantiate(nextShapePrefab);
        nextShape = nextShapeObj.GetComponent<TetrisShape>();

        nextShape.InitializeShape();
        nextShape.enabled = false;

        string shapeTypeName = nextShape.GetShapeTypeName();

        if (!string.IsNullOrEmpty(shapeTypeName))
        {
            ShapePreviewSettings settings = GetPreviewSettingsForShape(shapeTypeName);
            if (settings != null)
            {
                SetupShapePreview(nextShape, settings.previewPosition, settings.previewScale);
                Debug.Log($"Предпросмотр для предметной фигуры {shapeTypeName}: позиция={settings.previewPosition}, масштаб={settings.previewScale}");
            }
            else
            {
                SetupShapePreview(nextShape, defaultPreviewPosition, defaultPreviewScale);
                Debug.Log($"Предпросмотр для предметной фигуры {shapeTypeName}: настройки не найдены, используем по умолчанию");
            }
        }
        else
        {
            SetupShapePreview(nextShape, defaultPreviewPosition, defaultPreviewScale);
            Debug.Log($"Предпросмотр для классической фигуры");
        }

        SetShapeBrightness(nextShape, 1.2f);
    }

    void SetupShapePreview(TetrisShape shape, Vector3 previewPosition, float previewScale)
    {
        if (fieldGrid != null)
        {
            var fieldSettings = fieldGrid.GetFieldSettings();
            Vector2 fieldScale = fieldSettings.scale;
            Vector2 fieldOffset = fieldSettings.offset;

            shape.UpdateShapeScale(fieldScale * previewScale);

            Vector2 scaledPreviewPos = new Vector2(
                (previewPosition.x - fieldOffset.x) / fieldScale.x,
                (previewPosition.y - fieldOffset.y) / fieldScale.y
            );

            Vector3 actualPreviewPosition = new Vector3(
                scaledPreviewPos.x * fieldScale.x + fieldOffset.x,
                scaledPreviewPos.y * fieldScale.y + fieldOffset.y,
                previewPosition.z
            );

            shape.transform.position = actualPreviewPosition;
        }
        else
        {
            shape.transform.position = previewPosition;
            shape.transform.localScale = Vector3.one * previewScale;
        }
    }

    ShapePreviewSettings GetPreviewSettingsForShape(string shapeTypeName)
    {
        foreach (var setting in shapePreviewSettings)
        {
            if (setting.shapeName == shapeTypeName)
            {
                return setting;
            }
        }
        return null;
    }

    void SetShapeBrightness(TetrisShape shape, float brightness)
    {
        if (shape == null) return;

        foreach (Transform block in shape.transform)
        {
            SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.r = Mathf.Clamp(color.r * brightness, 0f, 1f);
                color.g = Mathf.Clamp(color.g * brightness, 0f, 1f);
                color.b = Mathf.Clamp(color.b * brightness, 0f, 1f);
                color.a = renderer.color.a;
                renderer.color = color;
            }
        }
    }

    void SetShapeTransparency(TetrisShape shape, float alpha)
    {
        if (shape == null) return;

        foreach (Transform block in shape.transform)
        {
            SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
    }

    void SpawnNewShape()
    {
        if (isPaused || gameOver) return;

        if (shapePrefabs.Length == 0) return;

        GameObject shapeToSpawn = shapePrefabs[nextShapeIndex];
        GameObject newShapeObj = Instantiate(shapeToSpawn);
        currentShape = newShapeObj.GetComponent<TetrisShape>();
        currentShape.SetFieldGrid(fieldGrid);

        if (fieldGrid != null)
        {
            var fieldSettings = fieldGrid.GetFieldSettings();
            Vector2 fieldScale = fieldSettings.scale;
            Vector2 fieldOffset = fieldSettings.offset;

            currentShape.UpdateShapeScale(fieldScale);

            Vector2 scaledSpawnPosition = new Vector2(
                (defaultSpawnPosition.x - fieldOffset.x) / fieldScale.x,
                (defaultSpawnPosition.y - fieldOffset.y) / fieldScale.y
            );

            int spawnGridX = Mathf.RoundToInt(scaledSpawnPosition.x);
            int spawnGridY = Mathf.RoundToInt(scaledSpawnPosition.y);

            Vector3 actualSpawnPosition = new Vector3(
                spawnGridX * fieldScale.x + fieldOffset.x,
                spawnGridY * fieldScale.y + fieldOffset.y,
                defaultSpawnPosition.z
            );

            currentShape.transform.position = actualSpawnPosition;
            currentShape.InitializeShape();

            Debug.Log($"Спавн фигуры {currentShape.GetType().Name}: сетка ({spawnGridX}, {spawnGridY}), мир ({actualSpawnPosition.x:F2}, {actualSpawnPosition.y:F2}), масштаб {fieldScale}");
        }
        else
        {
            currentShape.transform.position = defaultSpawnPosition;
            currentShape.InitializeShape();
        }

        SetShapeTransparency(currentShape, 1f);

        if (!currentShape.CanMove(Vector2.zero))
        {
            bool foundValidPosition = false;
            Vector2[] tryDirections = { Vector2.left, Vector2.right, Vector2.up };

            foreach (Vector2 dir in tryDirections)
            {
                if (currentShape.CanMove(dir))
                {
                    currentShape.Move(dir);
                    foundValidPosition = true;
                    Debug.Log($"Фигура {currentShape.GetType().Name} смещена на {dir} для корректного спавна");
                    break;
                }
            }

            if (!foundValidPosition)
            {
                Debug.Log($"GAME OVER! Фигура {currentShape.GetType().Name} достигла верха!");
                gameOver = true;
                Destroy(currentShape.gameObject);
                currentShape = null;

                if (fieldGrid != null)
                {
                    var (offset, scale) = fieldGrid.GetFieldSettings();
                    Debug.Log($"FieldGrid настройки: offset={offset}, scale={scale}");
                }

                return;
            }
        }

        GenerateNextShape();
    }

    public void RestartGame()
    {
        Debug.Log("ПЕРЕЗАПУСК ИГРЫ");
        Time.timeScale = 1f;

        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Уничтожаем старую справку если она есть
        if (controlsCanvasInstance != null)
        {
            Destroy(controlsCanvasInstance);
            controlsCanvasInstance = null;
        }

        // Создаем новую справку ТОЛЬКО если нужно
        if (MainMenuManager.ShowControlsInGame && !IsMobilePlatform() && controlsCanvasPrefab != null)
        {
            CreateControlsCanvas();
        }

        if (fieldGrid != null)
        {
            fieldGrid.ClearGrid();
        }

        if (currentShape != null)
        {
            Destroy(currentShape.gameObject);
            currentShape = null;
        }
        if (nextShape != null)
        {
            Destroy(nextShape.gameObject);
            nextShape = null;
        }

        gameOver = false;
        isFastFalling = false;
        fallTimer = 0f;

        StartGame();
    }

    void AddScore(int lines)
    {
        int points = 0;
        switch (lines)
        {
            case 1: points = 100; break;
            case 2: points = 300; break;
            case 3: points = 500; break;
            case 4: points = 800; break;
        }

        score += points;
        linesCleared += lines;

        UpdateScoreUI();

        Debug.Log($"Очков: +{points} | Всего: {score} | Линий: {linesCleared}");
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            // Убедимся что панель паузы поверх всего
            SetCanvasSortingOrder(pausePanel, 1000);
        }

        
        

        // Скрываем справку при паузе (опционально)
        if (controlsCanvasInstance != null)
        {
            controlsCanvasInstance.SetActive(false);
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }

        Debug.Log($"Игра {(isPaused ? "приостановлена" : "возобновлена")}");
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        

        

        // Показываем справку обратно при продолжении игры
        if (controlsCanvasInstance != null && MainMenuManager.ShowControlsInGame)
        {
            controlsCanvasInstance.SetActive(true);
        }
    }

    public void LoadMainMenu()
    {
        Debug.Log("LoadMainMenu ВЫЗВАН");

        Time.timeScale = 1f;
        isPaused = false;

        // Уничтожаем справку при выходе в меню
        if (controlsCanvasInstance != null)
        {
            Destroy(controlsCanvasInstance);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    [ContextMenu("Добавить настройки для предметных фигур")]
    void AddSettingsForItemShapes()
    {
        shapePreviewSettings.Clear();

        string[] itemShapeNames = {
            "BookStackItem",
            "ChairItemJ",
            "ChairItemL",
            "ComputerItem",
            "CupItem",
            "PencilCupItem",
            "TableItem",
            "EmptyPencilCupItem",
            "LoosePencilsItem",
            "EmptyCupItem"
        };

        foreach (var shapeName in itemShapeNames)
        {
            ShapePreviewSettings settings = new ShapePreviewSettings();
            settings.shapeName = shapeName;

            switch (shapeName)
            {
                case "TableItem":
                    settings.previewPosition = new Vector3(12f, 14f, -5f);
                    settings.previewScale = 0.5f;
                    break;

                case "BookStackItem":
                    settings.previewPosition = new Vector3(12f, 17f, -5f);
                    settings.previewScale = 0.6f;
                    break;

                case "ComputerItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.7f;
                    break;

                case "ChairItemJ":
                case "ChairItemL":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.65f;
                    break;

                case "CupItem":
                case "EmptyCupItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.8f;
                    break;

                case "PencilCupItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.75f;
                    break;
                case "EmptyPencilCupItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.8f;
                    break;

                case "LoosePencilsItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.8f;
                    break;
                default:
                    settings.previewPosition = defaultPreviewPosition;
                    settings.previewScale = defaultPreviewScale;
                    break;
            }

            shapePreviewSettings.Add(settings);
            Debug.Log($"Добавлены настройки для: {shapeName}");
        }

        Debug.Log($"Всего добавлено {shapePreviewSettings.Count} настроек для предметных фигур");
    }
}