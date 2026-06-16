using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameOverUI gameOverUI;

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

    [Header("Настройки баланса фигур")]
    [Range(0f, 1f)] public float classicShapeChance = 0.6f; // 60% классические, 40% предметные
    public List<GameObject> classicShapePrefabs;   // заполните вручную в инспекторе
    public List<GameObject> itemShapePrefabs;      // заполните вручную в инспекторе
    private List<int> recentClassicIndices = new List<int>();
    private List<int> recentItemIndices = new List<int>();
    private int maxHistory = 3;
    private GameObject nextShapePrefab;            // для хранения следующей фигуры

    [Header("Баланс взаимодействий")]
    public float interactionBonusWeight = 3f; // во сколько раз выше шанс для взаимодействующей фигуры
    private string lastItemShapeName;         // имя последней выпавшей предметной фигуры


    // Словарь: ключ - имя фигуры, значение - список фигур, с которыми она взаимодействует (повышенный шанс)
    private Dictionary<string, List<string>> interactionBonus = new Dictionary<string, List<string>>();

    void InitInteractionBonus()
    {
        interactionBonus.Clear();

        interactionBonus["ShelfItem"] = new List<string> { "FileFolderItem" };
        interactionBonus["EmptyPencilCupItem"] = new List<string> { "LoosePencilsItem" };
        interactionBonus["EmptyCupItem"] = new List<string> { "LoosePencilsItem" };
        interactionBonus["TableItem"] = new List<string> { "ComputerItem", "CupItem", "BookStackItem", "PlantItem", "LampItem", "PrinterItem" };
        interactionBonus["ComputerItem"] = new List<string> { "CupItem" };
        interactionBonus["BookStackItem"] = new List<string> { "BookStackItem", "CupItem" };
        interactionBonus["ChairItemL"] = new List<string> { "BookStackItem", "CupItem" };
        interactionBonus["ChairItemJ"] = new List<string> { "BookStackItem", "CupItem" };
        interactionBonus["LampItem"] = new List<string> { "PlantItem" };
        interactionBonus["PlantItem"] = new List<string> { "LampItem" };
    }

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

    [Header("Блокировка движения при штрафах")]
    public GameObject redScreenPanel;      // Панель красного экрана (Image с черно-красным фоном)
    public TextMeshProUGUI warningText;   // Текст предупреждения
    public float redScreenBlinkDuration = 1.5f; // Длительность мигания
    public float redScreenBlinkInterval = 0.2f; // Интервал мигания

    private bool movementBlocked = false;
    private Coroutine blinkCoroutine;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Принудительная инициализация списков, если они не назначены в инспекторе
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("В GameManager не назначены префабы фигур! Игра не может работать.");
            return;
        }

        if (classicShapePrefabs == null) classicShapePrefabs = new List<GameObject>();
        if (itemShapePrefabs == null) itemShapePrefabs = new List<GameObject>();

        if (classicShapePrefabs.Count == 0 && itemShapePrefabs.Count == 0)
        {
            foreach (var prefab in shapePrefabs)
            {
                if (prefab == null) continue;
                TetrisShape shape = prefab.GetComponent<TetrisShape>();
                string typeName = shape != null ? shape.GetShapeTypeName() : "";
                if (string.IsNullOrEmpty(typeName))
                    classicShapePrefabs.Add(prefab);
                else
                    itemShapePrefabs.Add(prefab);
            }
        }
    }

    private int blocksToSkip = 0;

    public void BlockNextMovement()
    {
        if (movementBlocked) return;
        movementBlocked = true;
        blocksToSkip = 1;  // пропустить текущую фиксацию
        Debug.Log($"BlockNextMovement: movementBlocked = true, blocksToSkip = {blocksToSkip}");

        if (redScreenPanel != null)
        {
            redScreenPanel.SetActive(true);
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkRedScreen());
        }
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "Движение и вращение заблокированы!";
        }
    }

    public void TryDisableMovementBlock()
    {
        if (blocksToSkip > 0)
        {
            blocksToSkip--;
            Debug.Log($"TryDisableMovementBlock: пропускаем фиксацию, осталось пропустить: {blocksToSkip}");
            return;
        }
        if (movementBlocked)
        {
            DisableMovementBlock();
            Debug.Log("TryDisableMovementBlock: блокировка снята");
        }
    }

    private IEnumerator BlinkRedScreen()
    {
        float elapsed = 0f;
        bool visible = true;
        CanvasGroup cg = redScreenPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = redScreenPanel.AddComponent<CanvasGroup>();

        while (elapsed < redScreenBlinkDuration)
        {
            visible = !visible;
            cg.alpha = visible ? 0.7f : 0f;
            yield return new WaitForSeconds(redScreenBlinkInterval);
            elapsed += redScreenBlinkInterval;
        }
        cg.alpha = 0f;
    }


    private void DisableMovementBlock()
    {
        movementBlocked = false;
        if (redScreenPanel != null) redScreenPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        Debug.Log("Блокировка движения снята.");
    }
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
        if (classicShapePrefabs.Count == 0 && itemShapePrefabs.Count == 0 && shapePrefabs.Length > 0)
        {
            foreach (var prefab in shapePrefabs)
            {
                TetrisShape shape = prefab.GetComponent<TetrisShape>();
                string typeName = shape != null ? shape.GetShapeTypeName() : "";
                if (string.IsNullOrEmpty(typeName))
                    classicShapePrefabs.Add(prefab);
                else
                    itemShapePrefabs.Add(prefab);
            }
            Debug.Log($"Classic shapes: {classicShapePrefabs.Count}, Item shapes: {itemShapePrefabs.Count}");
        }
        // ВЫЗОВИТЕ ИНИЦИАЛИЗАЦИЮ СЛОВАРЯ
        InitInteractionBonus();

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
            // Клавиши, которые работают всегда (ускоренное падение и способность)
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
            Debug.Log($"movementBlocked = {movementBlocked}");
            // Движение и вращение (блокируются при штрафе)
            if (!movementBlocked)
            {
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
                        pencilCup.RotateLeft();
                    }
                    else if (currentShape is CupItem cupItem)
                    {
                        cupItem.RotateLeft();
                    }
                    else
                    {
                        currentShape.RotateLeft();
                    }
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
            Destroy(nextShape.gameObject);

        GameObject selectedPrefab = null;
        bool isClassic = Random.value < classicShapeChance;

        // Проверяем, есть ли вообще префабы
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("shapePrefabs не назначен или пуст! Игра не может продолжаться.");
            return;
        }

        // Выбор фигуры
        if (isClassic && classicShapePrefabs != null && classicShapePrefabs.Count > 0)
        {
            selectedPrefab = GetNonRepeatingShape(classicShapePrefabs, recentClassicIndices, true);
            if (selectedPrefab == null && classicShapePrefabs.Count > 0)
                selectedPrefab = classicShapePrefabs[0]; // запасной вариант
        }
        else if (!isClassic && itemShapePrefabs != null && itemShapePrefabs.Count > 0)
        {
            int selectedIndex = GetWeightedItemShapeIndex(itemShapePrefabs, recentItemIndices, true, lastItemShapeName);
            if (selectedIndex >= 0)
            {
                selectedPrefab = itemShapePrefabs[selectedIndex];
                recentItemIndices.Add(selectedIndex);
                if (recentItemIndices.Count > maxHistory)
                    recentItemIndices.RemoveAt(0);
            }
            else
            {
                selectedPrefab = itemShapePrefabs[Random.Range(0, itemShapePrefabs.Count)];
            }

            if (selectedPrefab != null)
            {
                TetrisShape shapeComp = selectedPrefab.GetComponent<TetrisShape>();
                string selectedTypeName = shapeComp != null ? shapeComp.GetShapeTypeName() : "";
                lastItemShapeName = string.IsNullOrEmpty(selectedTypeName) ? null : selectedTypeName;
            }
        }
        else
        {
            // fallback на общий массив
            selectedPrefab = shapePrefabs[Random.Range(0, shapePrefabs.Length)];
        }

        // Критическая проверка
        if (selectedPrefab == null)
        {
            Debug.LogError($"Невозможно выбрать фигуру! classicPrefabs: {(classicShapePrefabs != null ? classicShapePrefabs.Count : 0)}, itemPrefabs: {(itemShapePrefabs != null ? itemShapePrefabs.Count : 0)}, shapePrefabs: {(shapePrefabs != null ? shapePrefabs.Length : 0)}");
            return;
        }

        nextShapePrefab = selectedPrefab;

        // Создаём объект для предпросмотра
        GameObject nextShapeObj = Instantiate(nextShapePrefab);
        if (nextShapeObj == null)
        {
            Debug.LogError($"Не удалось создать объект из префаба: {nextShapePrefab.name}");
            return;
        }

        nextShape = nextShapeObj.GetComponent<TetrisShape>();
        if (nextShape == null)
        {
            Debug.LogError($"У созданного объекта {nextShapeObj.name} нет компонента TetrisShape!");
            Destroy(nextShapeObj);
            return;
        }

        nextShape.InitializeShape();
        nextShape.enabled = false;

        // ---- Настройка предпросмотра ----
        string shapeTypeName = nextShape.GetShapeTypeName();
        if (!string.IsNullOrEmpty(shapeTypeName))
        {
            ShapePreviewSettings settings = GetPreviewSettingsForShape(shapeTypeName);
            if (settings != null)
                SetupShapePreview(nextShape, settings.previewPosition, settings.previewScale);
            else
                SetupShapePreview(nextShape, defaultPreviewPosition, defaultPreviewScale);
        }
        else
        {
            SetupShapePreview(nextShape, defaultPreviewPosition, defaultPreviewScale);
        }
        SetShapeBrightness(nextShape, 1.2f);
    }

    GameObject GetNonRepeatingShape(List<GameObject> prefabs, List<int> recentIndices, bool allowRepeatForBooks)
    {
        if (prefabs.Count == 0) return null;

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            TetrisShape shape = prefab.GetComponent<TetrisShape>();
            string typeName = shape != null ? shape.GetShapeTypeName() : "";

            // Книги можно повторять всегда
            if (allowRepeatForBooks && typeName == "BookStackItem")
            {
                availableIndices.Add(i);
                continue;
            }

            if (!recentIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
        {
            for (int i = 0; i < prefabs.Count; i++)
                availableIndices.Add(i);
        }

        int selected = availableIndices[Random.Range(0, availableIndices.Count)];
        recentIndices.Add(selected);
        if (recentIndices.Count > maxHistory)
            recentIndices.RemoveAt(0);

        return prefabs[selected];
    }
    int GetWeightedItemShapeIndex(List<GameObject> prefabs, List<int> recentIndices, bool allowRepeatForBooks, string lastShape)
    {
        if (prefabs.Count == 0) return -1;

        List<int> availableIndices = new List<int>();
        List<float> weights = new List<float>();

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            TetrisShape shape = prefab.GetComponent<TetrisShape>();
            string typeName = shape != null ? shape.GetShapeTypeName() : "";

            // Проверка на повтор (книги можно повторять)
            bool isBook = (typeName == "BookStackItem");
            if (!allowRepeatForBooks || !isBook)
            {
                if (recentIndices.Contains(i))
                    continue; // недавно была – пропускаем
            }

            availableIndices.Add(i);
            float weight = 1f;

            // Бонус за взаимодействие с последней фигурой
            if (!string.IsNullOrEmpty(lastShape) && interactionBonus.ContainsKey(lastShape))
            {
                if (interactionBonus[lastShape].Contains(typeName))
                    weight = interactionBonusWeight;
            }

            weights.Add(weight);
        }

        if (availableIndices.Count == 0)
        {
            // если все заблокированы, берём все (кроме повторов, но без бонуса)
            for (int i = 0; i < prefabs.Count; i++)
            {
                availableIndices.Add(i);
                weights.Add(1f);
            }
        }

        // Взвешенный случайный выбор
        float totalWeight = 0f;
        foreach (float w in weights) totalWeight += w;
        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int j = 0; j < availableIndices.Count; j++)
        {
            cumulative += weights[j];
            if (rand <= cumulative)
            {
                int selectedIndex = availableIndices[j];
                return selectedIndex;
            }
        }
        return availableIndices[0];
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

        GameObject shapeToSpawn = nextShapePrefab;
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
                // Показываем нужную панель Game Over
                if (gameOverUI != null)
                {
                    gameOverUI.ShowGameOver();
                } 
                if (fieldGrid != null)
                {
                    var (offset, scale) = fieldGrid.GetFieldSettings();
                    Debug.Log($"FieldGrid настройки: offset={offset}, scale={scale}");
                }

                return;
            }
        }

        GenerateNextShape();
        // Снимаем блокировку движения после спавна новой фигуры
        
    }

    public void RestartGame()
    {
        lastItemShapeName = null;

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

        if (fieldGrid != null)
        {
            fieldGrid.ClearGrid();
        }

        // ===== ДОБАВЬ ПРИНУДИТЕЛЬНЫЙ СБРОС ЛАМП =====
        // Уничтожаем все оставшиеся объекты ламп в сцене
        LampItem[] remainingLamps = FindObjectsOfType<LampItem>();
        foreach (var lamp in remainingLamps)
        {
            Destroy(lamp.gameObject);
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

        recentClassicIndices.Clear();
        recentItemIndices.Clear();
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

        

        // === ПРОВЕРКА РЕКОРДА ===
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckHighScore(score);
        }
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
            "EmptyCupItem", 
            "PlantItem",
            "LampItem",
            "FileFolderItem"
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
                case "FileFolderItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.7f;
                    break;
                case "LampItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.7f;
                    break;
                case "ShelfItem":
                    settings.previewPosition = new Vector3(12f, 16f, -5f);
                    settings.previewScale = 0.7f;
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