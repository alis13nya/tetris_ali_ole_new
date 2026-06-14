using UnityEngine;
using System.Collections.Generic;

public class FieldGrid : MonoBehaviour
{
    [Header("Звуки")]
    public AudioClip lineClearSound;

    [Header("Эффекты")]
    public ParticleSystem blockBreakParticle; // Префаб частиц

    [Header("Смещение поля")]
    public Vector2 fieldOffset = Vector2.zero;

    [Header("Масштаб поля")]
    public Vector2 fieldScale = Vector2.one;

    [Header("Нейтральный блок")]
    public GameObject neutralBlockPrefab;

    [Header("ПРЕФАБЫ ДЛЯ СТАКАНОВ")]
    public GameObject emptyCupPrefab;          // Пустой стакан
    public GameObject pencilCupBottomPrefab;   // Нижняя часть стакана с карандашами
    public GameObject pencilCupTopPrefab;      // Верхняя часть с карандашами

    [Header("ПРЕФАБЫ ДЛЯ КРУЖЕК")]
    public GameObject emptyCupItemPrefab;      // Пустая кружка (EmptyCupItem) - один блок
    public GameObject cupWithPencilsBottomPrefab; // Нижняя часть кружки с карандашами
    public GameObject cupWithPencilsTopPrefab;    // Верхняя часть кружки с карандашами

    private GameObject[,] grid = new GameObject[10, 20];
    private PowerScaleManager powerScaleManager;
    private HashSet<Vector2Int> tableBlocks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> computerBlocks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bookStackBlocks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> emptyCupBlocks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> emptyCupItemBlocks = new HashSet<Vector2Int>(); // Блоки пустых кружек
    private HashSet<Vector2Int> chairBlocks = new HashSet<Vector2Int>(); // НОВОЕ: блоки всех кресел
    private HashSet<Vector2Int> plantBlocks = new HashSet<Vector2Int>();
    private Dictionary<string, LampItem> lampItems = new Dictionary<string, LampItem>();
    private Dictionary<Vector2Int, string> blockToItemId = new Dictionary<Vector2Int, string>();
    private Dictionary<string, HashSet<Vector2Int>> itemBlocks = new Dictionary<string, HashSet<Vector2Int>>();
    private Dictionary<string, System.Type> itemTypes = new Dictionary<string, System.Type>();
    private Dictionary<Vector2Int, Sprite> lampShadeSprites = new Dictionary<Vector2Int, Sprite>();
    private Dictionary<Vector2Int, string> lampState = new Dictionary<Vector2Int, string>(); // "normal", "yellow", "purple"
    private Dictionary<string, ShelfItem> shelfItems = new Dictionary<string, ShelfItem>();
    private int itemCounter = 0;


    private System.Collections.IEnumerator AnimateLineClear(int y)
    {
        // Собираем все блоки в строке
        List<GameObject> blocksInLine = new List<GameObject>();
        List<SpriteRenderer> renderersInLine = new List<SpriteRenderer>();
        List<Color> originalColors = new List<Color>();
        List<Vector3> originalScales = new List<Vector3>();

        for (int x = 0; x < 10; x++)
        {
            if (grid[x, y] != null)
            {
                blocksInLine.Add(grid[x, y]);
                SpriteRenderer sr = grid[x, y].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    renderersInLine.Add(sr);
                    originalColors.Add(sr.color);
                    originalScales.Add(grid[x, y].transform.localScale);
                }
            }
        }

        if (blocksInLine.Count == 0) yield break;

        // Анимация: блоки уменьшаются и становятся прозрачными
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = 1f - t;      // уменьшаем от 1 до 0
            float alpha = 1f - t;      // прозрачность от 1 до 0

            for (int i = 0; i < blocksInLine.Count; i++)
            {
                if (blocksInLine[i] != null)
                {
                    // Уменьшаем размер
                    blocksInLine[i].transform.localScale = originalScales[i] * scale;

                    // Меняем прозрачность
                    if (renderersInLine[i] != null)
                    {
                        Color c = originalColors[i];
                        c.a = alpha;
                        renderersInLine[i].color = c;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Создаём частицы для каждого блока
        foreach (GameObject block in blocksInLine)
        {
            if (block != null && blockBreakParticle != null)
            {
                // Создаём частицы в позиции блока
                ParticleSystem particles = Instantiate(blockBreakParticle, block.transform.position, Quaternion.identity);
                particles.Play();

                // Уничтожаем объект частиц через 1 секунду
                Destroy(particles.gameObject, 1f);
            }
        }

        // Немного ждём, чтобы частицы успели появиться
        yield return new WaitForSeconds(0.05f);

        // Удаляем строку
        ClearLine(y);
        MoveLinesDown(y);
    }

    // ДОБАВЛЯЕМ ПОЛЕ ДЛЯ ХРАНЕНИЯ ОБРАБАТЫВАЕМЫХ БЛОКОВ
    private List<Transform> currentBlocksToProcess = new List<Transform>();

    private bool IsOnEmptyCupItem()
    {
        foreach (Transform block in currentBlocksToProcess)
        {
            Vector2Int pos = WorldToGridPosition(block.position);
            if (pos.y > 0 && emptyCupItemBlocks.Contains(new Vector2Int(pos.x, pos.y - 1)))
                return true;
        }
        return false;
    }
    void Start()
    {
        powerScaleManager = FindObjectOfType<PowerScaleManager>();

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                grid[x, y] = null;
            }
        }
        Debug.Log("FieldGrid готов к работе");
        Debug.Log($"FieldGrid смещение: {fieldOffset}");
        Debug.Log($"FieldGrid масштаб: {fieldScale}");
    }
    private void CheckAchievements(TetrisShape shape, bool isOnTable, bool isOnComputer,
                               bool computerTouchesTable, bool emptyCupOnTable,
                               bool emptyCupItemOnTable, bool isBookStack,
                               bool isOnBookStack, bool isAdjacentToBookStack,
                               bool isOnChair, bool isChair)
    {
        if (AchievementManager.Instance == null) return;

        // 1. Кресло рядом со столом (фиксация кресла ИЛИ стола)
        if (isChair)
        {
            if (IsChairAdjacentToTable())
                AchievementManager.Instance.UnlockAchievement("chair_next_to_table");
        }
        else if (shape is TableItem)
        {
            // Проверка, что блок стола касается кресла
            bool tableAdjacentToChair = false;
            foreach (Transform block in currentBlocksToProcess)
            {
                Vector2Int pos = WorldToGridPosition(block.position);
                if (IsTableBlockAdjacentToChair(pos.x, pos.y))
                {
                    tableAdjacentToChair = true;
                    break;
                }
            }
            if (tableAdjacentToChair)
                AchievementManager.Instance.UnlockAchievement("chair_next_to_table");
        }

        // 2. Кружка (непролитая) на компьютере
        if (shape is CupItem && isOnComputer)
        {
            var cup = shape as CupItem;
            bool isFlipped = (bool)typeof(CupItem).GetField("isFlipped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cup);
            if (!isFlipped)
                AchievementManager.Instance.UnlockAchievement("cup_on_computer");
        }

        // 3. Пролитая кружка на компьютере
        if (shape is CupItem && isOnComputer)
        {
            var cup = shape as CupItem;
            bool isFlipped = (bool)typeof(CupItem).GetField("isFlipped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cup);
            if (isFlipped)
                AchievementManager.Instance.UnlockAchievement("spilled_cup_on_computer");
        }

        // 4. Стопка книг на столе
        if (shape is BookStackItem && isOnTable)
            AchievementManager.Instance.UnlockAchievement("bookstack_on_table");

        // 5. Стопка книг на стопку книг ИЛИ рядом (объединённое достижение)
        if (shape is BookStackItem && (isOnBookStack || isAdjacentToBookStack))
            AchievementManager.Instance.UnlockAchievement("bookstack_stack");

        // 7. Пустая кружка на столе
        if (shape is EmptyCupItem)
        {
            Debug.Log($"EmptyCupItem detected, emptyCupItemOnTable = {emptyCupItemOnTable}");
            if (emptyCupItemOnTable)
            {
                Debug.Log("=== ПОПЫТКА ОТКРЫТЬ ДОСТИЖЕНИЕ: Пустая кружка на столе ===");
                AchievementManager.Instance.UnlockAchievement("empty_cup_on_table");
            }
        }

        // 8. Пустой стакан на столе
        if (shape is EmptyPencilCupItem && emptyCupOnTable)
            AchievementManager.Instance.UnlockAchievement("empty_pencil_cup_on_table");

        // 9. Стакан с карандашами на столе
        if (shape is PencilCupItem && isOnTable)
            AchievementManager.Instance.UnlockAchievement("pencil_cup_on_table");

        // 10. Компьютер на столе
        if (shape is ComputerItem && computerTouchesTable)
            AchievementManager.Instance.UnlockAchievement("computer_on_table");

        // 11. Карандаши на пустой стакан (вертикально)
        if (shape is LoosePencilsItem)
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;
            if (pencils != null && pencils.IsVerticalOrientation && IsOnEmptyCupForPencils())
            {
                AchievementManager.Instance.UnlockAchievement("pencils_on_empty_cup");
            }
        }
        // Кружка с чаем на столе (непролитая)
        if (shape is CupItem && isOnTable)
        {
            CupItem cup = shape as CupItem;
            bool isFlipped = (bool)typeof(CupItem).GetField("isFlipped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cup);
            if (!isFlipped)
                AchievementManager.Instance?.UnlockAchievement("cup_on_table");
        }

        // Пролитая кружка на столе
        if (shape is CupItem && isOnTable)
        {
            CupItem cup = shape as CupItem;
            bool isFlipped = (bool)typeof(CupItem).GetField("isFlipped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cup);
            if (isFlipped)
                AchievementManager.Instance?.UnlockAchievement("spilled_cup_on_table");
        }

        // Карандаши в пустой кружке (вертикально)
        if (shape is LoosePencilsItem && IsOnEmptyCupItem())
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;
            if (pencils != null && pencils.IsVerticalOrientation)
                AchievementManager.Instance?.UnlockAchievement("pencils_in_empty_cup");
        }

        // Стопка книг на кресле
        if (shape is BookStackItem && isOnChair)
        {
            Debug.Log("=== ПОПЫТКА ОТКРЫТЬ ДОСТИЖЕНИЕ: Читательский уголок ===");
            AchievementManager.Instance?.UnlockAchievement("bookstack_on_chair");
        }
    }

    // Доп. метод для проверки карандашей на пустом стакане
    private bool IsOnEmptyCupForPencils()
    {
        foreach (Transform block in currentBlocksToProcess)
        {
            Vector2Int pos = WorldToGridPosition(block.position);
            if (pos.y > 0 && emptyCupBlocks.Contains(new Vector2Int(pos.x, pos.y - 1)))
                return true;
        }
        return false;
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        if (Mathf.Approximately(fieldScale.x, 0) || Mathf.Approximately(fieldScale.y, 0))
        {
            fieldScale = Vector2.one;
        }

        return new Vector2Int(
            Mathf.RoundToInt((worldPos.x - fieldOffset.x) / fieldScale.x),
            Mathf.RoundToInt((worldPos.y - fieldOffset.y) / fieldScale.y)
        );
    }

    public void UpdateFieldScale(Vector2 newScale)
    {
        fieldScale = newScale;

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].transform.position = new Vector3(
                        x * fieldScale.x + fieldOffset.x,
                        y * fieldScale.y + fieldOffset.y,
                        -1f
                    );
                }
            }
        }

        Debug.Log($"Масштаб поля обновлен: {fieldScale}");
    }

    public void UpdateFieldOffset(Vector2 newOffset)
    {
        fieldOffset = newOffset;

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].transform.position = new Vector3(
                        x * fieldScale.x + fieldOffset.x,
                        y * fieldScale.y + fieldOffset.y,
                        -1f
                    );
                }
            }
        }

        Debug.Log($"Смещение поля обновлено: {fieldOffset}");
    }

    public void LockShape(TetrisShape shape)
    {
        // Если фигура — нейтральный блок, игнорируем
        if (shape.GetComponent<NeutralBlockTag>() != null)
        {
            Debug.Log("Попытка зафиксировать нейтральный блок — игнорируем");
            Destroy(shape.gameObject);
            return;
        }

        Debug.Log($"=== ФИКСАЦИЯ ФИГУРЫ {shape.name} ===");

        string itemId = $"Item_{shape.GetType().Name}_{itemCounter++}";
        System.Type itemType = shape.GetType();
        HashSet<Vector2Int> itemBlockPositions = new HashSet<Vector2Int>();

        bool isTable = shape is TableItem;
        bool isComputer = shape is ComputerItem;
        bool isBookStack = shape is BookStackItem;
        bool isEmptyCup = shape is EmptyPencilCupItem;
        bool isEmptyCupItem = shape is EmptyCupItem;
        bool isChairL = shape is ChairItemL;
        bool isChairJ = shape is ChairItemJ;
        bool isChair = isChairL || isChairJ;
        bool isPlant = shape is PlantItem;
        bool isLamp = shape is LampItem;
        bool isShelf = shape is ShelfItem;
        bool isFileFolder = shape is FileFolderItem;

        // Сохраняем блоки для обработки в поле класса
        currentBlocksToProcess.Clear();
        foreach (Transform block in shape.transform)
        {
            currentBlocksToProcess.Add(block);
        }

        int fixedBlocks = 0;
        bool isOnTable = false;
        bool isOnComputer = false;
        bool isOnEmptyCup = false;
        bool isOnEmptyCupItem = false;
        bool isOnBookStack = false;
        bool isAdjacentToBookStack = false;
        bool computerTouchesTable = false;
        bool chairAdjacentToTable = false;
        bool tableAdjacentToChair = false;
        bool isOnChair = false;
        bool emptyCupOnTable = false;
        bool emptyCupItemOnTable = false;
        bool bookStackOnChair = false;

        // ОСОБАЯ ОБРАБОТКА ДЛЯ ЛАМПЫ
        if (shape is LampItem lamp)
        {
            lampItems[itemId] = lamp;
            CheckLampInteractions(lamp);

            foreach (Transform block in shape.transform)
            {
                Vector2Int pos = WorldToGridPosition(block.position);
                if (pos.x < (int)lamp.transform.position.x)
                {
                    SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        lampShadeSprites[pos] = sr.sprite;
                        lampState[pos] = "normal";
                        Debug.Log($"Запомнили плафон на позиции {pos}");
                    }
                    break;
                }
            }

            foreach (Transform block in currentBlocksToProcess)
            {
                Vector2Int gridPos = WorldToGridPosition(block.position);
                int gridX = gridPos.x;
                int gridY = gridPos.y;
                if (gridX >= 0 && gridX < 10 && gridY >= 0 && gridY < 20 && grid[gridX, gridY] == null)
                {
                    grid[gridX, gridY] = block.gameObject;
                    block.SetParent(null);
                    block.position = new Vector3(
                        gridX * fieldScale.x + fieldOffset.x,
                        gridY * fieldScale.y + fieldOffset.y,
                        -1f
                    );
                    blockToItemId[gridPos] = itemId;
                    itemBlockPositions.Add(gridPos);
                }
            }

            if (itemBlockPositions.Count > 0)
            {
                itemBlocks[itemId] = itemBlockPositions;
                itemTypes[itemId] = itemType;
            }

            lamp.gameObject.SetActive(false);
            return;
        }

        // ОСОБАЯ ОБРАБОТКА ДЛЯ ПОЛКИ
        if (shape is ShelfItem shelf)
        {
            shelfItems[itemId] = shelf;
        }

        // ОСОБАЯ ОБРАБОТКА ДЛЯ EmptyCupItem (пустая кружка)
        if (isEmptyCupItem)
        {
            Debug.Log("ФИКСАЦИЯ EmptyCupItem (пустая кружка) - используем префабы из FieldGrid");

            if (emptyCupItemPrefab == null)
            {
                Debug.LogError("emptyCupItemPrefab не назначен в FieldGrid!");
                Destroy(shape.gameObject);
                return;
            }

            foreach (Transform block in currentBlocksToProcess)
            {
                Vector2Int gridPos = WorldToGridPosition(block.position);
                int gridX = gridPos.x;
                int gridY = gridPos.y;

                Debug.Log($"Текущие блоки столов в tableBlocks: {string.Join(", ", tableBlocks)}");
                Debug.Log($"Проверка стола: ищу стол на позиции ({gridX}, {gridY - 1})");
                Debug.Log($"tableBlocks содержит {new Vector2Int(gridX, gridY - 1)}? = {tableBlocks.Contains(new Vector2Int(gridX, gridY - 1))}");
                Debug.Log($"Блок пустой кружки: ({block.position.x:F2}, {block.position.y:F2}) -> Сетка: ({gridX}, {gridY})");

                if (gridX >= 0 && gridX < 10 && gridY >= 0 && gridY < 20)
                {
                    if (grid[gridX, gridY] == null)
                    {
                        GameObject cupInstance = Instantiate(emptyCupItemPrefab);
                        cupInstance.transform.position = new Vector3(
                            gridX * fieldScale.x + fieldOffset.x,
                            gridY * fieldScale.y + fieldOffset.y,
                            -1f
                        );
                        grid[gridX, gridY] = cupInstance;
                        itemBlockPositions.Add(gridPos);
                        blockToItemId[gridPos] = itemId;
                        fixedBlocks++;
                        emptyCupItemBlocks.Add(new Vector2Int(gridX, gridY));
                        Debug.Log($"✓ Блок пустой кружки зафиксирован на ({gridX}, {gridY})");

                        if (gridY > 0 && tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            emptyCupItemOnTable = true;
                            Debug.Log($"Пустая кружка стоит на столе!");
                            if (AchievementManager.Instance != null)
                                AchievementManager.Instance.UnlockAchievement("empty_cup_on_table");
                        }
                    }
                    else Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
                }
                else Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
            }

            if (itemBlockPositions.Count > 0)
            {
                itemBlocks[itemId] = itemBlockPositions;
                itemTypes[itemId] = itemType;
                Debug.Log($"Зарегистрирована пустая кружка {itemId} с {itemBlockPositions.Count} блоками");
            }

            Destroy(shape.gameObject);
            if (emptyCupItemOnTable && powerScaleManager != null)
                powerScaleManager.RemoveEmptyCupItemOnTable();
            Debug.Log($"Зафиксировано: {fixedBlocks}/1 блоков пустой кружки");
            return;
        }

        // ОСОБАЯ ОБРАБОТКА ДЛЯ EmptyPencilCupItem (пустой стакан)
        if (isEmptyCup)
        {
            Debug.Log("ФИКСАЦИЯ EmptyPencilCupItem (пустой стакан) - используем префабы из FieldGrid");

            if (emptyCupPrefab == null)
            {
                Debug.LogError("emptyCupPrefab не назначен в FieldGrid!");
                Destroy(shape.gameObject);
                return;
            }

            foreach (Transform block in currentBlocksToProcess)
            {
                Vector2Int gridPos = WorldToGridPosition(block.position);
                int gridX = gridPos.x;
                int gridY = gridPos.y;
                Debug.Log($"Блок пустого стакана: ({block.position.x:F2}, {block.position.y:F2}) -> Сетка: ({gridX}, {gridY})");

                if (gridX >= 0 && gridX < 10 && gridY >= 0 && gridY < 20)
                {
                    if (grid[gridX, gridY] == null)
                    {
                        GameObject cupInstance = Instantiate(emptyCupPrefab);
                        cupInstance.transform.position = new Vector3(
                            gridX * fieldScale.x + fieldOffset.x,
                            gridY * fieldScale.y + fieldOffset.y,
                            -1f
                        );
                        grid[gridX, gridY] = cupInstance;
                        itemBlockPositions.Add(gridPos);
                        blockToItemId[gridPos] = itemId;
                        fixedBlocks++;
                        emptyCupBlocks.Add(new Vector2Int(gridX, gridY));
                        Debug.Log($"✓ Блок пустого стакана зафиксирован на ({gridX}, {gridY})");

                        if (gridY > 0 && tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            emptyCupOnTable = true;
                            Debug.Log($"Пустой стакан стоит на столе!");
                            if (AchievementManager.Instance != null)
                                AchievementManager.Instance.UnlockAchievement("empty_pencil_cup_on_table");
                        }
                    }
                    else Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
                }
                else Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
            }

            if (itemBlockPositions.Count > 0)
            {
                itemBlocks[itemId] = itemBlockPositions;
                itemTypes[itemId] = itemType;
                Debug.Log($"Зарегистрирован пустой стакан {itemId} с {itemBlockPositions.Count} блоками");
            }

            Destroy(shape.gameObject);
            if (emptyCupOnTable && powerScaleManager != null)
                powerScaleManager.RemoveEmptyCupOnTable();
            Debug.Log($"Зафиксировано: {fixedBlocks}/1 блоков пустого стакана");
            return;
        }

        // СТАНДАРТНАЯ ОБРАБОТКА ДЛЯ ВСЕХ ДРУГИХ ФИГУР
        foreach (Transform block in currentBlocksToProcess)
        {
            Vector2Int gridPos = WorldToGridPosition(block.position);
            int gridX = gridPos.x;
            int gridY = gridPos.y;

            Debug.Log($"Блок: ({block.position.x:F2}, {block.position.y:F2}) -> Сетка: ({gridX}, {gridY})");

            if (gridX >= 0 && gridX < 10 && gridY >= 0 && gridY < 20)
            {
                if (grid[gridX, gridY] == null)
                {
                    grid[gridX, gridY] = block.gameObject;
                    block.SetParent(null);
                    block.position = new Vector3(
                        gridX * fieldScale.x + fieldOffset.x,
                        gridY * fieldScale.y + fieldOffset.y,
                        -1f
                    );
                    itemBlockPositions.Add(gridPos);
                    blockToItemId[gridPos] = itemId;
                    fixedBlocks++;

                    if (isTable)
                    {
                        tableBlocks.Add(new Vector2Int(gridX, gridY));
                        if (IsTableBlockAdjacentToChair(gridX, gridY)) tableAdjacentToChair = true;
                    }
                    else if (isComputer)
                    {
                        computerBlocks.Add(new Vector2Int(gridX, gridY));
                        if (gridY > 0 && tableBlocks.Contains(new Vector2Int(gridX, gridY - 1))) computerTouchesTable = true;
                    }
                    else if (isBookStack)
                    {
                        HashSet<Vector2Int> currentBlocksSet = new HashSet<Vector2Int>();
                        foreach (Transform blk in currentBlocksToProcess)
                            currentBlocksSet.Add(WorldToGridPosition(blk.position));

                        foreach (Transform blk in currentBlocksToProcess)
                        {
                            Vector2Int pos = WorldToGridPosition(blk.position);
                            int gx = pos.x, gy = pos.y;
                            if (IsOnTable(gx, gy)) isOnTable = true;
                            if (gy > 0 && !currentBlocksSet.Contains(new Vector2Int(gx, gy - 1)) && bookStackBlocks.Contains(new Vector2Int(gx, gy - 1))) isOnBookStack = true;
                            if ((gx > 0 && !currentBlocksSet.Contains(new Vector2Int(gx - 1, gy)) && bookStackBlocks.Contains(new Vector2Int(gx - 1, gy))) ||
                                (gx < 9 && !currentBlocksSet.Contains(new Vector2Int(gx + 1, gy)) && bookStackBlocks.Contains(new Vector2Int(gx + 1, gy))))
                                isAdjacentToBookStack = true;
                            if (gy > 0 && chairBlocks.Contains(new Vector2Int(gx, gy - 1)))
                            {
                                bookStackOnChair = true;
                                isOnChair = true;
                            }
                        }

                        foreach (Transform blk in currentBlocksToProcess)
                            bookStackBlocks.Add(WorldToGridPosition(blk.position));
                    }
                    else if (isChair)
                    {
                        chairBlocks.Add(new Vector2Int(gridX, gridY));
                    }
                    else if (isPlant)
                    {
                        plantBlocks.Add(new Vector2Int(gridX, gridY));
                        UpdateAllLampsAfterPlantPlaced();
                    }
                    else if (isLamp)
                    {
                        Debug.Log($"✓ Блок лампы зафиксирован на ({gridX}, {gridY})");
                    }
                    else
                    {
                        Debug.Log($"✓ Блок зафиксирован на ({gridX}, {gridY})");

                        if (IsOnTable(gridX, gridY)) isOnTable = true;
                        if (IsOnComputer(gridX, gridY)) isOnComputer = true;
                        if (IsOnEmptyCup(gridX, gridY)) isOnEmptyCup = true;
                        if (IsOnEmptyCupItem(gridX, gridY)) isOnEmptyCupItem = true;
                        if (IsOnBookStack(gridX, gridY)) isOnBookStack = true;
                        if (IsAdjacentToBookStack(gridX, gridY)) isAdjacentToBookStack = true;
                        if (IsOnChair(gridX, gridY))
                        {
                            isOnChair = true;
                            CheckForItemsOnChair(shape);
                        }
                    }
                }
                else Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
            }
            else Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
        }

        if (itemBlockPositions.Count > 0)
        {
            itemBlocks[itemId] = itemBlockPositions;
            itemTypes[itemId] = itemType;
            Debug.Log($"Зарегистрирован предмет {itemId} (тип: {itemType.Name}) с {itemBlockPositions.Count} блоками");
        }

        // ===== ОБРАБОТКА ПАПКИ НА ПОЛКЕ =====
        if (isFileFolder)
        {
            List<Vector2Int> folderPositions = new List<Vector2Int>();
            foreach (Transform block in currentBlocksToProcess)
                folderPositions.Add(WorldToGridPosition(block.position));

            if (folderPositions.Count == 2 && folderPositions[0].y == folderPositions[1].y)
            {
                Vector2Int belowPos1 = new Vector2Int(folderPositions[0].x, folderPositions[0].y - 1);
                Vector2Int belowPos2 = new Vector2Int(folderPositions[1].x, folderPositions[1].y - 1);
                string shelfItemId = null;
                if (blockToItemId.ContainsKey(belowPos1)) shelfItemId = blockToItemId[belowPos1];
                else if (blockToItemId.ContainsKey(belowPos2)) shelfItemId = blockToItemId[belowPos2];

                if (shelfItemId != null && itemTypes.ContainsKey(shelfItemId) && itemTypes[shelfItemId] == typeof(ShelfItem))
                {
                    ShelfItem foundShelf = null;
                    if (shelfItems.ContainsKey(shelfItemId)) foundShelf = shelfItems[shelfItemId];
                    if (foundShelf != null && foundShelf.IsHorizontal())
                    {
                        Vector2Int shelfLeftPos = GetShelfLeftPosition(shelfItemId);
                        if (shelfLeftPos != Vector2Int.zero)
                        {
                            int folderStartX = Mathf.Min(folderPositions[0].x, folderPositions[1].x);
                            int offset = folderStartX - shelfLeftPos.x;
                            if (offset == 0 || offset == 1)
                            {
                                foundShelf.FillShelf();
                                foreach (Transform block in currentBlocksToProcess) Destroy(block.gameObject);
                                Destroy(shape.gameObject);
                                return;
                            }
                        }
                    }
                }
            }
        }

        // ===== ОСНОВНЫЕ ПРОВЕРКИ ДЛЯ ВСЕХ ФИГУР (КРОМЕ УСПЕШНОЙ ПАПКИ) =====
        // ПРОВЕРКА ДЛЯ КАРАНДАШЕЙ НА ПУСТОМ СТАКАНЕ
        if (shape is LoosePencilsItem && isOnEmptyCup)
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;
            if (pencils != null && pencils.IsVerticalOrientation)
            {
                if (powerScaleManager != null) powerScaleManager.AddPencilsOnEmptyCup();
                StartCoroutine(RemoveTouchingPencilsFromEmptyCupDelayed(pencils));
            }
        }

        // ПРОВЕРКА ДЛЯ КАРАНДАШЕЙ НА ПУСТОЙ КРУЖКЕ
        if (shape is LoosePencilsItem && isOnEmptyCupItem)
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;
            if (pencils != null && pencils.IsVerticalOrientation)
            {
                if (powerScaleManager != null)
                {
                    float currentAmount = powerScaleManager.currentFillAmount;
                    powerScaleManager.SetFillAmount(Mathf.Max(0, currentAmount - 0.15f));
                }
                StartCoroutine(RemoveTouchingPencilsFromEmptyCupItemDelayed(pencils));
            }
        }

        // ПРОВЕРКА КОМПЬЮТЕРА НА СТОЛЕ
        if (isComputer && computerTouchesTable && powerScaleManager != null)
            powerScaleManager.AddComputerOnTable();

        // ПРОВЕРКА КРЕСЛА РЯДОМ СО СТОЛОМ
        if (isChair)
        {
            chairAdjacentToTable = IsChairAdjacentToTable();
            if (chairAdjacentToTable && powerScaleManager != null)
                powerScaleManager.AddChairAdjacentToTable();
        }
        if (isTable && tableAdjacentToChair && powerScaleManager != null)
            powerScaleManager.AddChairAdjacentToTable();

        // ЛОГИКА ДЛЯ СТОПОК КНИГ
        if (isBookStack)
        {
            CheckBookStackInteractions(shape);
            if (bookStackOnChair && powerScaleManager != null)
                powerScaleManager.RemoveBookStackOnChair();
        }
        else // Для обычных фигур
        {
            if (isOnTable) CheckForSpecialItemsOnTable(shape);
            if (isOnComputer) CheckForSpecialItemsOnComputer(shape);
            if (isOnBookStack) CheckForItemsOnBookStack(shape);
            if (isAdjacentToBookStack) CheckForItemsAdjacentToBookStack(shape);
            if (isOnChair) CheckForItemsOnChair(shape);
        }

        CheckAchievements(shape, isOnTable, isOnComputer, computerTouchesTable,
                          emptyCupOnTable, emptyCupItemOnTable, isBookStack,
                          isOnBookStack, isAdjacentToBookStack, isOnChair,
                          shape is ChairItemL || shape is ChairItemJ);

        Debug.Log($"Зафиксировано: {fixedBlocks}/{currentBlocksToProcess.Count} блоков");
        if (shape is LampItem) shape.gameObject.SetActive(false);
        else Destroy(shape.gameObject);
    }
    // НОВЫЙ МЕТОД: Проверка стоит ли блок на кресле
    private bool IsOnChair(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            // Если под предметом нейтральный блок — не считаем креслом
            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;

            if (chairBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на кресле!");
                return true;
            }
        }

        return false;
    }
    // НОВЫЙ МЕТОД: Проверка касается ли блок стола кресла сбоку
    private bool IsTableBlockAdjacentToChair(int gridX, int gridY)
    {
        // Проверка слева от стола
        if (gridX - 1 >= 0)
        {
            if (chairBlocks.Contains(new Vector2Int(gridX - 1, gridY)))
            {
                Debug.Log($"Стол касается кресла слева на позиции ({gridX - 1}, {gridY})");
                return true;
            }
        }

        // Проверка справа от стола
        if (gridX + 1 < 10)
        {
            if (chairBlocks.Contains(new Vector2Int(gridX + 1, gridY)))
            {
                Debug.Log($"Стол касается кресла справа на позиции ({gridX + 1}, {gridY})");
                return true;
            }
        }

        return false;
    }
    // НОВЫЙ МЕТОД: Проверка касается ли кресло стола сбоку
    private bool IsChairAdjacentToTable()
    {
        if (currentBlocksToProcess == null || currentBlocksToProcess.Count == 0)
            return false;

        foreach (Transform block in currentBlocksToProcess)
        {
            Vector2Int gridPos = WorldToGridPosition(block.position);
            int gridX = gridPos.x;
            int gridY = gridPos.y;

            // Проверка слева от стола
            if (gridX - 1 >= 0)
            {
                if (tableBlocks.Contains(new Vector2Int(gridX - 1, gridY)))
                {
                    Debug.Log($"Кресло касается стола слева на позиции ({gridX - 1}, {gridY})");
                    return true;
                }
            }

            // Проверка справа от стола
            if (gridX + 1 < 10)
            {
                if (tableBlocks.Contains(new Vector2Int(gridX + 1, gridY)))
                {
                    Debug.Log($"Кресло касается стола справа на позиции ({gridX + 1}, {gridY})");
                    return true;
                }
            }
        }

        return false;
    }

    // НОВЫЙ МЕТОД: Проверка предметов на кресле
    private void CheckForItemsOnChair(TetrisShape shape)
    {
        Debug.Log($"Предмет {shape.GetType().Name} стоит на кресле");

        // Проверка: кружка с чаем на кресле
        if (shape is CupItem cup)
        {
            var isFlippedField = typeof(CupItem).GetField("isFlipped",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isFlippedField != null)
            {
                bool isFlipped = (bool)isFlippedField.GetValue(cup);
                if (!isFlipped) // Непролитая кружка
                {
                    if (powerScaleManager != null)
                        powerScaleManager.RemoveCupOnChair();
                    if (AchievementManager.Instance != null)
                        AchievementManager.Instance.UnlockAchievement("cup_on_chair");
                }
            }
        }
    }

    // Корутины для задержанной обработки
    private System.Collections.IEnumerator RemoveTouchingPencilsFromEmptyCupDelayed(LoosePencilsItem pencils)
    {
        yield return null;
        RemoveTouchingPencilsFromEmptyCup();
    }

    private System.Collections.IEnumerator RemoveTouchingPencilsFromEmptyCupItemDelayed(LoosePencilsItem pencils)
    {
        yield return null;
        RemoveTouchingPencilsFromEmptyCupItem();
    }

    // Метод для пустого стакана
    private void RemoveTouchingPencilsFromEmptyCup()
    {
        Debug.Log("Поиск карандашей для удаления с пустого стакана...");
        ProcessPencilRemovalAndTransformation(emptyCupBlocks, TransformEmptyCupToPencilCup, "пустой стакан");
    }

    // Метод для пустой кружки
    private void RemoveTouchingPencilsFromEmptyCupItem()
    {
        Debug.Log("Поиск карандашей для удаления с пустой кружки...");
        ProcessPencilRemovalAndTransformation(emptyCupItemBlocks, TransformEmptyCupItemToCupWithPencils, "пустая кружка");
    }

    // Общий метод для обработки удаления карандашей и превращения
    private void ProcessPencilRemovalAndTransformation(HashSet<Vector2Int> targetBlocks,
        System.Action<Vector2Int> transformationMethod, string targetName)
    {
        List<Vector2Int> pencilPositions = new List<Vector2Int>();
        Dictionary<Vector2Int, string> pencilItemIds = new Dictionary<Vector2Int, string>();

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                if (grid[x, y] != null)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (blockToItemId.ContainsKey(pos))
                    {
                        string itemId = blockToItemId[pos];
                        if (itemTypes.ContainsKey(itemId) && itemTypes[itemId] == typeof(LoosePencilsItem))
                        {
                            pencilPositions.Add(pos);
                            pencilItemIds[pos] = itemId;
                        }
                    }
                }
            }
        }

        bool anyPencilTouches = false;
        Vector2Int touchedPos = Vector2Int.zero;

        foreach (Vector2Int pencilPos in pencilPositions)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector2Int neighborPos = new Vector2Int(pencilPos.x + dx, pencilPos.y + dy);

                    if (neighborPos.x >= 0 && neighborPos.x < 10 && neighborPos.y >= 0 && neighborPos.y < 20)
                    {
                        if (targetBlocks.Contains(neighborPos))
                        {
                            anyPencilTouches = true;
                            touchedPos = neighborPos;
                            Debug.Log($"Карандаш на ({pencilPos.x}, {pencilPos.y}) касается {targetName} на ({neighborPos.x}, {neighborPos.y})");
                            break;
                        }
                    }
                }
                if (anyPencilTouches) break;
            }
            if (anyPencilTouches) break;
        }

        if (anyPencilTouches)
        {
            Debug.Log($"Найден касающийся карандаш. Удаляем все карандаши и превращаем {targetName} на {touchedPos}");

            int removedPencils = 0;
            HashSet<string> removedItemIds = new HashSet<string>();

            foreach (Vector2Int pencilPos in pencilPositions)
            {
                if (grid[pencilPos.x, pencilPos.y] != null)
                {
                    if (pencilItemIds.ContainsKey(pencilPos))
                    {
                        removedItemIds.Add(pencilItemIds[pencilPos]);
                    }

                    Destroy(grid[pencilPos.x, pencilPos.y]);
                    grid[pencilPos.x, pencilPos.y] = null;
                    blockToItemId.Remove(pencilPos);

                    removedPencils++;
                }
            }

            foreach (string itemId in removedItemIds)
            {
                if (itemBlocks.ContainsKey(itemId))
                {
                    itemBlocks.Remove(itemId);
                }
                if (itemTypes.ContainsKey(itemId))
                {
                    itemTypes.Remove(itemId);
                }
            }

            if (removedPencils > 0)
            {
                Debug.Log($"✓ Удалено карандашей: {removedPencils}");
                transformationMethod(touchedPos);
                CheckAndClearLines();
            }
        }
        else
        {
            Debug.Log($"Карандаши не касаются {targetName}");
        }
    }

    // Превращение пустого стакана в стакан с карандашами
    private void TransformEmptyCupToPencilCup(Vector2Int cupPos)
    {
        Debug.Log($"ПРЕВРАЩЕНИЕ: Пустой стакан на {cupPos} -> Стакан с карандашами");

        if (pencilCupBottomPrefab == null || pencilCupTopPrefab == null)
        {
            Debug.LogError("Префабы для стакана с карандашами не назначены в FieldGrid!");
            return;
        }

        if (cupPos.x < 0 || cupPos.x >= 10 || cupPos.y < 0 || cupPos.y >= 20)
        {
            Debug.LogError($"Невалидная позиция стакана: {cupPos}");
            return;
        }

        if (grid[cupPos.x, cupPos.y] != null)
        {
            Destroy(grid[cupPos.x, cupPos.y]);
            grid[cupPos.x, cupPos.y] = null;
        }

        emptyCupBlocks.Remove(cupPos);

        if (blockToItemId.ContainsKey(cupPos))
        {
            string oldItemId = blockToItemId[cupPos];
            blockToItemId.Remove(cupPos);

            if (itemBlocks.ContainsKey(oldItemId))
            {
                itemBlocks.Remove(oldItemId);
            }
            if (itemTypes.ContainsKey(oldItemId))
            {
                itemTypes.Remove(oldItemId);
            }
        }

        GameObject bottomPart = Instantiate(pencilCupBottomPrefab);
        bottomPart.transform.position = GridToWorldPosition(cupPos.x, cupPos.y);
        grid[cupPos.x, cupPos.y] = bottomPart;

        Vector2Int topPos = new Vector2Int(cupPos.x, cupPos.y + 1);

        if (topPos.y < 20)
        {
            if (grid[topPos.x, topPos.y] == null)
            {
                GameObject topPart = Instantiate(pencilCupTopPrefab);
                topPart.transform.position = GridToWorldPosition(topPos.x, topPos.y);
                grid[topPos.x, topPos.y] = topPart;

                string newItemId = $"PencilCup_{cupPos.x}_{cupPos.y}_{System.DateTime.Now.Ticks}";

                blockToItemId[cupPos] = newItemId;
                blockToItemId[topPos] = newItemId;

                HashSet<Vector2Int> newBlocks = new HashSet<Vector2Int> { cupPos, topPos };
                itemBlocks[newItemId] = newBlocks;
                itemTypes[newItemId] = typeof(EmptyPencilCupItem);

                Debug.Log($"✓ СТАКАН ПРЕВРАЩЕН! Блоки: {cupPos} и {topPos}");
            }
            else
            {
                Debug.LogWarning($"Не удалось разместить верхнюю часть на {topPos}");

                GameObject emptyCup = Instantiate(emptyCupPrefab);
                emptyCup.transform.position = GridToWorldPosition(cupPos.x, cupPos.y);
                grid[cupPos.x, cupPos.y] = emptyCup;
                emptyCupBlocks.Add(cupPos);

                string emptyItemId = $"EmptyCup_{cupPos.x}_{cupPos.y}";
                blockToItemId[cupPos] = emptyItemId;
                HashSet<Vector2Int> emptyBlock = new HashSet<Vector2Int> { cupPos };
                itemBlocks[emptyItemId] = emptyBlock;
                itemTypes[emptyItemId] = typeof(EmptyPencilCupItem);
            }
        }
        else
        {
            Debug.LogWarning($"Не удалось разместить верхнюю часть - позиция {topPos} вне границ");

            GameObject emptyCup = Instantiate(emptyCupPrefab);
            emptyCup.transform.position = GridToWorldPosition(cupPos.x, cupPos.y);
            grid[cupPos.x, cupPos.y] = emptyCup;
            emptyCupBlocks.Add(cupPos);

            string emptyItemId = $"EmptyCup_{cupPos.x}_{cupPos.y}";
            blockToItemId[cupPos] = emptyItemId;
            HashSet<Vector2Int> emptyBlock = new HashSet<Vector2Int> { cupPos };
            itemBlocks[emptyItemId] = emptyBlock;
            itemTypes[emptyItemId] = typeof(EmptyPencilCupItem);
        }
    }

    // Превращение пустой кружки в кружку с карандашами
    private void TransformEmptyCupItemToCupWithPencils(Vector2Int cupItemPos)
    {
        Debug.Log($"ПРЕВРАЩЕНИЕ: Пустая кружка на {cupItemPos} -> Кружка с карандашами");

        if (cupWithPencilsBottomPrefab == null || cupWithPencilsTopPrefab == null)
        {
            Debug.LogError("Префабы для кружки с карандашами не назначены в FieldGrid!");
            return;
        }

        if (cupItemPos.x < 0 || cupItemPos.x >= 10 || cupItemPos.y < 0 || cupItemPos.y >= 20)
        {
            Debug.LogError($"Невалидная позиция кружки: {cupItemPos}");
            return;
        }

        if (grid[cupItemPos.x, cupItemPos.y] != null)
        {
            Destroy(grid[cupItemPos.x, cupItemPos.y]);
            grid[cupItemPos.x, cupItemPos.y] = null;
        }

        emptyCupItemBlocks.Remove(cupItemPos);

        if (blockToItemId.ContainsKey(cupItemPos))
        {
            string oldItemId = blockToItemId[cupItemPos];
            blockToItemId.Remove(cupItemPos);

            if (itemBlocks.ContainsKey(oldItemId))
            {
                itemBlocks.Remove(oldItemId);
            }
            if (itemTypes.ContainsKey(oldItemId))
            {
                itemTypes.Remove(oldItemId);
            }
        }

        GameObject bottomPart = Instantiate(cupWithPencilsBottomPrefab);
        bottomPart.transform.position = GridToWorldPosition(cupItemPos.x, cupItemPos.y);
        grid[cupItemPos.x, cupItemPos.y] = bottomPart;

        Vector2Int topPos = new Vector2Int(cupItemPos.x, cupItemPos.y + 1);

        if (topPos.y < 20)
        {
            if (grid[topPos.x, topPos.y] == null)
            {
                GameObject topPart = Instantiate(cupWithPencilsTopPrefab);
                topPart.transform.position = GridToWorldPosition(topPos.x, topPos.y);
                grid[topPos.x, topPos.y] = topPart;

                string newItemId = $"CupWithPencils_{cupItemPos.x}_{cupItemPos.y}_{System.DateTime.Now.Ticks}";

                blockToItemId[cupItemPos] = newItemId;
                blockToItemId[topPos] = newItemId;

                HashSet<Vector2Int> newBlocks = new HashSet<Vector2Int> { cupItemPos, topPos };
                itemBlocks[newItemId] = newBlocks;
                itemTypes[newItemId] = typeof(EmptyCupItem);

                Debug.Log($"✓ КРУЖКА ПРЕВРАЩЕНА В КРУЖКУ С КАРАНДАШАМИ!");
                Debug.Log($"  Блоки: {cupItemPos} и {topPos}");
            }
            else
            {
                Debug.LogWarning($"Не удалось разместить верхнюю часть на {topPos}");

                GameObject emptyCup = Instantiate(emptyCupItemPrefab);
                emptyCup.transform.position = GridToWorldPosition(cupItemPos.x, cupItemPos.y);
                grid[cupItemPos.x, cupItemPos.y] = emptyCup;
                emptyCupItemBlocks.Add(cupItemPos);

                string emptyItemId = $"EmptyCupItem_{cupItemPos.x}_{cupItemPos.y}";
                blockToItemId[cupItemPos] = emptyItemId;
                HashSet<Vector2Int> emptyBlock = new HashSet<Vector2Int> { cupItemPos };
                itemBlocks[emptyItemId] = emptyBlock;
                itemTypes[emptyItemId] = typeof(EmptyCupItem);
            }
        }
        else
        {
            Debug.LogWarning($"Не удалось разместить верхнюю часть - позиция {topPos} вне границ");

            GameObject emptyCup = Instantiate(emptyCupItemPrefab);
            emptyCup.transform.position = GridToWorldPosition(cupItemPos.x, cupItemPos.y);
            grid[cupItemPos.x, cupItemPos.y] = emptyCup;
            emptyCupItemBlocks.Add(cupItemPos);

            string emptyItemId = $"EmptyCupItem_{cupItemPos.x}_{cupItemPos.y}";
            blockToItemId[cupItemPos] = emptyItemId;
            HashSet<Vector2Int> emptyBlock = new HashSet<Vector2Int> { cupItemPos };
            itemBlocks[emptyItemId] = emptyBlock;
            itemTypes[emptyItemId] = typeof(EmptyCupItem);
        }
    }
    private bool IsNeutralBlock(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= 10 || gridY < 0 || gridY >= 20)
            return false;

        if (grid[gridX, gridY] == null)
            return false;

        return grid[gridX, gridY].GetComponent<NeutralBlockTag>() != null;
    }

    // Вспомогательные методы проверки
    private bool IsOnTable(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            // Если под предметом нейтральный блок — не считаем столом
            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;

            if (tableBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на столе!");
                return true;
            }
        }

        return false;
    }

    private bool IsOnComputer(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;

            if (computerBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на компьютере!");
                return true;
            }
        }

        return false;
    }

    private bool IsOnEmptyCup(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            // ===== ДОБАВЬ ЭТО =====
            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;
            // =====================

            if (emptyCupBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на пустом стакане!");
                return true;
            }
        }

        return false;
    }

    private bool IsOnEmptyCupItem(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            // ===== ДОБАВЬ ЭТО =====
            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;
            // =====================

            if (emptyCupItemBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на пустой кружке!");
                return true;
            }
        }

        return false;
    }

    private bool IsOnBookStack(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            Vector2Int belowPos = new Vector2Int(gridX, gridY - 1);

            if (IsNeutralBlock(belowPos.x, belowPos.y))
                return false;

            if (bookStackBlocks.Contains(belowPos))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) стоит на стопке книг!");
                return true;
            }
        }

        return false;
    }

    private bool IsAdjacentToBookStack(int gridX, int gridY)
    {
        // Проверка слева
        if (gridX - 1 >= 0)
        {
            if (bookStackBlocks.Contains(new Vector2Int(gridX - 1, gridY)))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) находится слева от стопки книг!");
                return true;
            }
        }

        // Проверка справа
        if (gridX + 1 < 10)
        {
            if (bookStackBlocks.Contains(new Vector2Int(gridX + 1, gridY)))
            {
                Debug.Log($"Блок на позиции ({gridX}, {gridY}) находится справа от стопки книг!");
                return true;
            }
        }

        return false;
    }

    // МЕТОД: Проверка всех взаимодействий для стопки книг
    private void CheckBookStackInteractions(TetrisShape shape)
    {
        bool isOnTable = IsBookStackOnTable();
        bool isOnBookStack = IsBookStackOnOtherBookStack();
        bool isAdjacentToBookStack = IsBookStackAdjacentToOtherBookStack();
        bool isOnChair = IsBookStackOnChair();

        Debug.Log($"Стопка книг: На столе={isOnTable}, На стопке={isOnBookStack}, Соприкасается с стопкой={isAdjacentToBookStack}, На кресле={isOnChair}");

        if (powerScaleManager != null)
        {
            if (isOnTable)
            {
                powerScaleManager.AddBookStackOnTable();
                Debug.Log("Стопка книг поставлена на стол! Шкала усиления увеличилась.");
            }

            if (isOnBookStack)
            {
                // НОВОЕ: проверяем, стоит ли нижняя стопка на кресле
                if (IsBookStackOnChairRecursive())
                {
                    powerScaleManager.RemoveBookStackOnBookStackOnChair();
                    Debug.Log("Стопка книг на стопке книг, которая стоит на кресле! Шкала усиления уменьшилась.");
                }
                else
                {
                    powerScaleManager.AddBookStackOnBookStack();
                    Debug.Log("Стопка книг поставлена на стопку книг! Шкала усиления увеличилась.");
                }
            }

            if (isAdjacentToBookStack)
            {
                powerScaleManager.AddBookStackAdjacentToBookStack();
                Debug.Log("Стопка книг поставлена рядом с другой стопкой книг! Шкала усиления увеличилась.");
            }
        }
    }

    // НОВЫЙ МЕТОД: Проверка стоит ли стопка книг на кресле (рекурсивно)
    private bool IsBookStackOnChairRecursive()
    {
        var newBookStackBlocks = GetNewestBookStackBlocks();
        if (newBookStackBlocks.Count == 0) return false;

        foreach (var block in newBookStackBlocks)
        {
            if (block.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(block.x, block.y - 1);

                // Проверка прямого стояния на кресле
                if (chairBlocks.Contains(belowPos))
                {
                    return true;
                }

                // Проверка стоит ли на стопке книг, которая на кресле
                if (bookStackBlocks.Contains(belowPos))
                {
                    // Рекурсивно проверяем нижележащие блоки
                    if (IsBlockOnChairRecursive(belowPos))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // НОВЫЙ МЕТОД: Рекурсивная проверка блока на кресле
    private bool IsBlockOnChairRecursive(Vector2Int blockPos)
    {
        if (blockPos.y > 0)
        {
            Vector2Int belowPos = new Vector2Int(blockPos.x, blockPos.y - 1);

            if (chairBlocks.Contains(belowPos))
            {
                return true;
            }

            if (bookStackBlocks.Contains(belowPos))
            {
                return IsBlockOnChairRecursive(belowPos);
            }
        }

        return false;
    }

    // НОВЫЙ МЕТОД: Проверка стоит ли стопка книг на кресле
    private bool IsBookStackOnChair()
    {
        var newBookStackBlocks = GetNewestBookStackBlocks();
        if (newBookStackBlocks.Count == 0) return false;

        foreach (var block in newBookStackBlocks)
        {
            if (block.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(block.x, block.y - 1);

                // Дополнительная страховка
                if (IsNeutralBlock(belowPos.x, belowPos.y))
                    continue;

                if (chairBlocks.Contains(belowPos))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // МЕТОД: Проверка стоит ли стопка книг на столе (физическое соприкосновение снизу)
    private bool IsBookStackOnTable()
    {
        var newBookStackBlocks = GetNewestBookStackBlocks();
        if (newBookStackBlocks.Count == 0) return false;

        foreach (var newBlock in newBookStackBlocks)
        {
            if (newBlock.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(newBlock.x, newBlock.y - 1);

                // ===== ДОБАВЬ ЭТО =====
                if (IsNeutralBlock(belowPos.x, belowPos.y))
                    continue; // пропускаем, если под книгой нейтральный блок
                              // =====================

                if (tableBlocks.Contains(belowPos))
                {
                    Debug.Log($"Стопка книг стоит на столе!");
                    return true;
                }
            }
        }
        return false;
    }

    // МЕТОД: Проверка стоит ли стопка книг на другой стопке книг (физическое соприкосновение снизу)
    private bool IsBookStackOnOtherBookStack()
    {
        var newBookStackBlocks = GetNewestBookStackBlocks();
        if (newBookStackBlocks.Count == 0) return false;

        foreach (var newBlock in newBookStackBlocks)
        {
            if (newBlock.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(newBlock.x, newBlock.y - 1);

                // ===== ДОБАВЬ ЭТО =====
                if (IsNeutralBlock(belowPos.x, belowPos.y))
                    continue; // пропускаем, если под книгой нейтральный блок
                              // =====================

                if (bookStackBlocks.Contains(belowPos) && !newBookStackBlocks.Contains(belowPos))
                {
                    Debug.Log($"Стопка книг стоит на другой стопке книг!");
                    return true;
                }
            }
        }
        return false;
    }

    // МЕТОД: Проверка рядом ли стопка книг с другой стопкой книг (физическое соприкосновение сторон)
    private bool IsBookStackAdjacentToOtherBookStack()
    {
        // Получаем текущую стопка книг (последнюю добавленную)
        var newBookStackBlocks = GetNewestBookStackBlocks();

        if (newBookStackBlocks.Count == 0) return false;

        Debug.Log($"Проверка соседства стопки книг. Новых блоков: {newBookStackBlocks.Count}");

        // Проверяем каждую пару блоков на соприкосновение
        foreach (var newBlock in newBookStackBlocks)
        {
            // Проверка всех 4 направлений (верх, низ, лево, право)
            Vector2Int[] directions = {
                new Vector2Int(0, 1),   // Вверх
                new Vector2Int(0, -1),  // Вниз
                new Vector2Int(-1, 0),  // Влево
                new Vector2Int(1, 0)    // Вправо
            };

            foreach (var direction in directions)
            {
                Vector2Int neighborPos = new Vector2Int(newBlock.x + direction.x, newBlock.y + direction.y);

                // Проверяем, находится ли соседняя позиция в пределах сетки
                if (neighborPos.x >= 0 && neighborPos.x < 10 && neighborPos.y >= 0 && neighborPos.y < 20)
                {
                    // Проверяем, есть ли в соседней позиции блок другой стопки книг
                    if (bookStackBlocks.Contains(neighborPos) && !newBookStackBlocks.Contains(neighborPos))
                    {
                        Debug.Log($"✓ Обнаружено соприкосновение: новый блок ({newBlock.x}, {newBlock.y}) " +
                                 $"соприкасается с другой стопкой книг в позиции ({neighborPos.x}, {neighborPos.y}) " +
                                 $"направление: {GetDirectionName(direction)}");
                        return true;
                    }
                }
            }
        }

        Debug.Log($"✗ Нет соприкосновения с другими стопками книг");
        return false;
    }

    // Вспомогательный метод для получения названия направления
    private string GetDirectionName(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return "Вверх";
        if (direction == Vector2Int.down) return "Вниз";
        if (direction == Vector2Int.left) return "Влево";
        if (direction == Vector2Int.right) return "Вправо";
        return $"({direction.x}, {direction.y})";
    }

    // МЕТОД: Получение блоков последней добавленной стопки книг
    private HashSet<Vector2Int> GetNewestBookStackBlocks()
    {
        HashSet<Vector2Int> newestBlocks = new HashSet<Vector2Int>();

        if (currentBlocksToProcess == null || currentBlocksToProcess.Count == 0)
            return newestBlocks;

        // Используем позиции блоков, которые только что обработали
        foreach (Transform block in currentBlocksToProcess)
        {
            Vector2Int gridPos = WorldToGridPosition(block.position);
            newestBlocks.Add(gridPos);
        }

        Debug.Log($"Определены блоки новой стопки книг: {newestBlocks.Count} блоков");
        return newestBlocks;
    }

    // МЕТОД: Проверка предметов на стопке книг
    private void CheckForItemsOnBookStack(TetrisShape shape)
    {
        Debug.Log($"Предмет {shape.GetType().Name} стоит на стопке книг");
    }

    // МЕТОД: Проверка предметов рядом с стопкой книг
    private void CheckForItemsAdjacentToBookStack(TetrisShape shape)
    {
        Debug.Log($"Предмет {shape.GetType().Name} находится рядом с стопкой книг");
    }

    private void CheckForSpecialItemsOnTable(TetrisShape shape)
    {
        CupItem cupItem = shape.GetComponent<CupItem>();
        if (cupItem != null)
        {
            var isFlippedField = typeof(CupItem).GetField("isFlipped",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (isFlippedField != null)
            {
                bool isFlipped = (bool)isFlippedField.GetValue(cupItem);

                if (!isFlipped)
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.AddNormalCupOnTable();
                        Debug.Log("Непролитая кружка поставлена на стол! Шкала усиления увеличилась.");
                    }
                }
                else
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.RemoveSpilledCupOnTable();
                        Debug.Log("Пролитая кружка поставлена на стол! Шкала усиления уменьшилась.");
                    }
                }
            }
        }

        PencilCupItem pencilCupItem = shape.GetComponent<PencilCupItem>();
        if (pencilCupItem != null)
        {
            var isSpilledField = typeof(PencilCupItem).GetField("isSpilled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (isSpilledField != null)
            {
                bool isSpilled = (bool)isSpilledField.GetValue(pencilCupItem);

                if (!isSpilled)
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.AddNormalPencilCupOnTable();
                        Debug.Log("Не рассыпанный стакан с карандашами поставлен на стол! Шкала усиления увеличилась.");
                    }
                }
                else
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.RemoveSpilledPencilsOnTable();
                        Debug.Log("Рассыпанные карандаши поставлены на стол! Шкала усиления уменьшилась.");
                    }
                }
            }
        }
    }

    private void CheckForSpecialItemsOnComputer(TetrisShape shape)
    {
        CupItem cupItem = shape.GetComponent<CupItem>();
        if (cupItem != null)
        {
            var isFlippedField = typeof(CupItem).GetField("isFlipped",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (isFlippedField != null)
            {
                bool isFlipped = (bool)isFlippedField.GetValue(cupItem);

                if (!isFlipped)
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.RemoveNormalCupOnComputer();
                        Debug.Log("Непролитая кружка поставлена на компьютер! Шкала усиления уменьшилась.");
                    }
                }
                else
                {
                    if (powerScaleManager != null)
                    {
                        powerScaleManager.RemoveSpilledCupOnComputer();
                        Debug.Log("Пролитая кружка поставлена на компьютер! Шкала усиления уменьшилась.");
                    }
                }
            }
        }
    }

    public bool IsValidPosition(TetrisShape shape)
    {
        foreach (Transform block in shape.transform)
        {
            Vector2Int gridPos = WorldToGridPosition(block.position);
            int gridX = gridPos.x;
            int gridY = gridPos.y;

            if (gridX < 0 || gridX >= 10 || gridY < 0)
                return false;

            if (gridY < 20 && grid[gridX, gridY] != null)
                return false;
        }
        return true;
    }

    private void QuickClearAnimation(int y)
    {
        // Воспроизводим звук удаления строки
        if (lineClearSound != null)
        {
            AudioSource.PlayClipAtPoint(lineClearSound, Camera.main.transform.position, 0.3f);
        }
        // Сначала показываем частицы
        for (int x = 0; x < 10; x++)
        {
            if (grid[x, y] != null && blockBreakParticle != null)
            {
                ParticleSystem particles = Instantiate(blockBreakParticle, grid[x, y].transform.position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, 1f);
            }
        }

        // А теперь удаляем строку через ClearLine (она вызовет CheckItemIntegrity)
        ClearLine(y);
        MoveLinesDown(y);
    }
    private void ApplyGravity()
    {
        bool changed;
        do
        {
            changed = false;

            // Проверяем каждый столбец снизу вверх
            for (int x = 0; x < 10; x++)
            {
                for (int y = 1; y < 20; y++)  // с нижней строки наверх
                {
                    if (grid[x, y] != null && IsEmptyBelow(x, y))
                    {
                        // Блок может упасть
                        MoveBlockDown(x, y);
                        changed = true;
                    }
                }
            }
        } while (changed); // Повторяем, пока есть что падать
    }

    private bool IsEmptyBelow(int x, int y)
    {
        // Проверяем, пусто ли место под блоком
        return grid[x, y - 1] == null;
    }

    private void MoveBlockDown(int x, int y)
    {
        GameObject block = grid[x, y];
        grid[x, y] = null;
        grid[x, y - 1] = block;

        // Обновляем позицию блока в мире
        block.transform.position = GridToWorldPosition(x, y - 1);

        // Обновляем словари блоков (если нужно)
        Vector2Int oldPos = new Vector2Int(x, y);
        Vector2Int newPos = new Vector2Int(x, y - 1);

        if (blockToItemId.ContainsKey(oldPos))
        {
            string itemId = blockToItemId[oldPos];
            blockToItemId.Remove(oldPos);
            blockToItemId[newPos] = itemId;

            // Обновляем позицию в itemBlocks
            if (itemBlocks.ContainsKey(itemId))
            {
                itemBlocks[itemId].Remove(oldPos);
                itemBlocks[itemId].Add(newPos);
            }
        }
    }
    public int CheckAndClearLines()
    {
        int linesCleared = 0;

        for (int y = 0; y < 20; y++)
        {
            while (IsLineComplete(y))
            {
                QuickClearAnimation(y);  // показывает частицы и вызывает ClearLine
                linesCleared++;
                // while продолжит проверять ту же строку y
            }
        }

        if (linesCleared > 0)
            Debug.Log($"Удалено линий: {linesCleared}");

        return linesCleared;
    }


    private bool IsLineComplete(int y)
    {
        for (int x = 0; x < 10; x++)
        {
            if (grid[x, y] == null)
                return false;
        }
        return true;
    }

    private void ClearLine(int y)
    {
        Debug.Log($"Очищаем линию {y}");

        HashSet<string> affectedItemIds = new HashSet<string>();

        for (int x = 0; x < 10; x++)
        {
            if (grid[x, y] != null)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (blockToItemId.ContainsKey(pos))
                {
                    affectedItemIds.Add(blockToItemId[pos]);
                }

                tableBlocks.Remove(pos);
                computerBlocks.Remove(pos);
                bookStackBlocks.Remove(pos);
                emptyCupBlocks.Remove(pos);
                emptyCupItemBlocks.Remove(pos);
                chairBlocks.Remove(pos);
                Destroy(grid[x, y]);
                grid[x, y] = null;
                blockToItemId.Remove(pos);
                plantBlocks.Remove(pos);
            }
        }

        foreach (string itemId in affectedItemIds)
        {
            CheckItemIntegrity(itemId);
        }
    }

    private void CheckItemIntegrity(string itemId)
    {
        if (!itemBlocks.ContainsKey(itemId)) return;

        if (itemTypes.ContainsKey(itemId) && itemTypes[itemId] == typeof(BookStackItem))
        {
            Debug.Log($"Стопка книг {itemId} не заменяется на нейтральные блоки");
            return;
        }

        HashSet<Vector2Int> remainingBlocks = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in itemBlocks[itemId])
        {
            if (grid[pos.x, pos.y] != null &&
                blockToItemId.ContainsKey(pos) &&
                blockToItemId[pos] == itemId)
            {
                remainingBlocks.Add(pos);
            }
        }

        int originalBlockCount = itemBlocks[itemId].Count;
        int remainingCount = remainingBlocks.Count;

        if (remainingCount > 0 && remainingCount < originalBlockCount * 0.9f)
        {
            Debug.Log($"Предмет {itemId} (тип: {itemTypes[itemId].Name}) почти разрушен. Осталось {remainingCount}/{originalBlockCount} блоков. Заменяем на нейтральные...");
            ReplaceItemWithNeutralBlocks(itemId, remainingBlocks);
        }
        else if (remainingCount == 0)
        {
            itemBlocks.Remove(itemId);
            itemTypes.Remove(itemId);
        }
        else
        {
            itemBlocks[itemId] = remainingBlocks;
        }
    }

    private void ReplaceItemWithNeutralBlocks(string itemId, HashSet<Vector2Int> blockPositions)
    {
        if (neutralBlockPrefab == null)
        {
            Debug.LogError("NeutralBlockPrefab не назначен!");
            return;
        }

        foreach (Vector2Int pos in blockPositions)
        {
            if (grid[pos.x, pos.y] != null)
            {
                Vector3 worldPos = GridToWorldPosition(pos.x, pos.y);
                Destroy(grid[pos.x, pos.y]);

                GameObject neutralBlock = Instantiate(neutralBlockPrefab, worldPos, Quaternion.identity);
                grid[pos.x, pos.y] = neutralBlock;

                if (neutralBlock.GetComponent<NeutralBlockTag>() == null)
                    neutralBlock.AddComponent<NeutralBlockTag>();

                SpriteRenderer renderer = neutralBlock.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.color = new Color(255f, 255f, 255f, 1f);

                // ===== УДАЛЯЕМ ИЗ ВСЕХ СПЕЦИАЛИЗИРОВАННЫХ ХЕШСЕТОВ =====
                tableBlocks.Remove(pos);
                computerBlocks.Remove(pos);
                bookStackBlocks.Remove(pos);
                emptyCupBlocks.Remove(pos);
                emptyCupItemBlocks.Remove(pos);
                chairBlocks.Remove(pos);
                plantBlocks.Remove(pos);
                // ====================================================

                blockToItemId.Remove(pos);
            }
        }

        itemBlocks.Remove(itemId);
        itemTypes.Remove(itemId);

        Debug.Log($"Предмет {itemId} заменен на нейтральные блоки");
    }

    private void MoveLinesDown(int clearedLineY)
    {
        UpdateDictionariesAfterLineClear(clearedLineY);

        for (int y = clearedLineY + 1; y < 20; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y - 1].transform.position = new Vector3(
                        x * fieldScale.x + fieldOffset.x,
                        (y - 1) * fieldScale.y + fieldOffset.y,
                        -1f
                    );
                }
            }
        }
    }

    private void UpdateDictionariesAfterLineClear(int clearedLineY)
    {
        Dictionary<Vector2Int, string> newBlockToItemId = new Dictionary<Vector2Int, string>();
        foreach (var kvp in blockToItemId)
        {
            Vector2Int pos = kvp.Key;
            if (pos.y > clearedLineY)
            {
                newBlockToItemId[new Vector2Int(pos.x, pos.y - 1)] = kvp.Value;
            }
            else if (pos.y < clearedLineY)
            {
                newBlockToItemId[pos] = kvp.Value;
            }
        }
        blockToItemId = newBlockToItemId;

        Dictionary<string, HashSet<Vector2Int>> newItemBlocks = new Dictionary<string, HashSet<Vector2Int>>();
        Dictionary<string, System.Type> newItemTypes = new Dictionary<string, System.Type>();

        foreach (var kvp in itemBlocks)
        {
            HashSet<Vector2Int> newPositions = new HashSet<Vector2Int>();
            foreach (Vector2Int pos in kvp.Value)
            {
                if (pos.y > clearedLineY)
                {
                    newPositions.Add(new Vector2Int(pos.x, pos.y - 1));
                }
                else if (pos.y < clearedLineY)
                {
                    newPositions.Add(pos);
                }
            }

            if (newPositions.Count > 0)
            {
                newItemBlocks[kvp.Key] = newPositions;
                if (itemTypes.ContainsKey(kvp.Key))
                {
                    newItemTypes[kvp.Key] = itemTypes[kvp.Key];
                }
            }
        }

        itemBlocks = newItemBlocks;
        itemTypes = newItemTypes;

        UpdateSpecialBlocksAfterLineClear(clearedLineY);
    }

    private void UpdateSpecialBlocksAfterLineClear(int clearedLineY)
    {
        HashSet<Vector2Int> newTableBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newComputerBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newBookStackBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newEmptyCupBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newEmptyCupItemBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newChairBlocks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newPlantBlocks = new HashSet<Vector2Int>();

        foreach (var block in tableBlocks)
        {
            if (block.y > clearedLineY)
            {
                newTableBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newTableBlocks.Add(block);
            }
        }

        foreach (var block in computerBlocks)
        {
            if (block.y > clearedLineY)
            {
                newComputerBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newComputerBlocks.Add(block);
            }
        }

        foreach (var block in bookStackBlocks)
        {
            if (block.y > clearedLineY)
            {
                newBookStackBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newBookStackBlocks.Add(block);
            }
        }

        foreach (var block in emptyCupBlocks)
        {
            if (block.y > clearedLineY)
            {
                newEmptyCupBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newEmptyCupBlocks.Add(block);
            }
        }

        foreach (var block in emptyCupItemBlocks)
        {
            if (block.y > clearedLineY)
            {
                newEmptyCupItemBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newEmptyCupItemBlocks.Add(block);
            }
        }

        foreach (var block in chairBlocks)
        {
            if (block.y > clearedLineY)
            {
                newChairBlocks.Add(new Vector2Int(block.x, block.y - 1));
            }
            else if (block.y < clearedLineY)
            {
                newChairBlocks.Add(block);
            }
        }
        foreach (var block in plantBlocks)
        {
            if (block.y > clearedLineY)
                newPlantBlocks.Add(new Vector2Int(block.x, block.y - 1));
            else if (block.y < clearedLineY)
                newPlantBlocks.Add(block);
        }

        tableBlocks = newTableBlocks;
        computerBlocks = newComputerBlocks;
        bookStackBlocks = newBookStackBlocks;
        emptyCupBlocks = newEmptyCupBlocks;
        emptyCupItemBlocks = newEmptyCupItemBlocks;
        chairBlocks = newChairBlocks;
        plantBlocks = newPlantBlocks;
    }

    public void ClearGrid()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y]);
                    grid[x, y] = null;
                }
            }
        }

        tableBlocks.Clear();
        computerBlocks.Clear();
        bookStackBlocks.Clear();
        emptyCupBlocks.Clear();
        emptyCupItemBlocks.Clear();
        chairBlocks.Clear();
        itemBlocks.Clear();
        itemTypes.Clear();
        blockToItemId.Clear();
        itemCounter = 0;
        lampItems.Clear();
        lampShadeSprites.Clear();
        lampState.Clear();
        plantBlocks.Clear();
        shelfItems.Clear();


        Debug.Log("Поле очищено");
    }

    public Vector3 GridToWorldPosition(int gridX, int gridY)
    {
        return new Vector3(
            gridX * fieldScale.x + fieldOffset.x,
            gridY * fieldScale.y + fieldOffset.y,
            -1f
        );
    }

    public (Vector2 offset, Vector2 scale) GetFieldSettings()
    {
        return (fieldOffset, fieldScale);
    }

    // ========== НОВЫЕ МЕТОДЫ ДЛЯ ЛАМПЫ ==========
    private void CheckLampInteractions(LampItem lamp)
    {
        Debug.Log("=== ПОПАЛИ В CheckLampInteractions ===");

        // Находим основание (самый нижний блок)
        Vector2Int basePos = Vector2Int.zero;
        bool found = false;
        List<Vector2Int> allLampBlocks = new List<Vector2Int>();
        GameObject shadeBlock = null;

        foreach (Transform block in lamp.transform)
        {
            Vector2Int pos = WorldToGridPosition(block.position);
            allLampBlocks.Add(pos);
            if (!found || pos.y < basePos.y)
            {
                basePos = pos;
                found = true;
            }
            // Запоминаем блок плафона (самый верхний)
            if (shadeBlock == null || pos.y > WorldToGridPosition(shadeBlock.transform.position).y)
            {
                shadeBlock = block.gameObject;
            }
        }

        if (!found || shadeBlock == null)
        {
            lamp.SetNormalLight();
            return;
        }

        bool onTable = (basePos.y > 0 && tableBlocks.Contains(new Vector2Int(basePos.x, basePos.y - 1)));
        bool nearPlant = IsLampNearPlantOnlyRight(allLampBlocks);

        Debug.Log($"Основание на {basePos}, на столе: {onTable}, растение СПРАВА: {nearPlant}");

        // Сохраняем позицию плафона для будущих обновлений
        Vector2Int shadePos = WorldToGridPosition(shadeBlock.transform.position);

        if (nearPlant)
        {
            lamp.SetPurpleLight();
            if (lampShadeSprites.ContainsKey(shadePos)) lampShadeSprites[shadePos] = lamp.purpleSprite;
            if (lampState.ContainsKey(shadePos)) lampState[shadePos] = "purple";
        }
        else if (onTable)
        {
            lamp.SetYellowLight();
            if (lampShadeSprites.ContainsKey(shadePos)) lampShadeSprites[shadePos] = lamp.yellowSprite;
            if (lampState.ContainsKey(shadePos)) lampState[shadePos] = "yellow";
        }
        else
        {
            lamp.SetNormalLight();
            if (lampShadeSprites.ContainsKey(shadePos)) lampShadeSprites[shadePos] = lamp.normalSprite;
            if (lampState.ContainsKey(shadePos)) lampState[shadePos] = "normal";
        }
    }

    // НОВЫЙ МЕТОД: Проверка есть ли растение рядом с лампой (только вплотную)
    // Проверка, находится ли лампа СПРАВА от растения (растение слева от лампы)
    private bool IsLampNearPlantOnlyRight(List<Vector2Int> lampBlocks)
    {
        if (plantBlocks.Count == 0) return false;

        foreach (Vector2Int lampPos in lampBlocks)
        {
            foreach (Vector2Int plantPos in plantBlocks)
            {
                // Растение должно быть СЛЕВА от лампы (plantPos.x < lampPos.x)
                // На той же высоте (Y) или на соседней (диагональ)
                int dx = lampPos.x - plantPos.x; // положительное, если лампа справа
                int dy = Mathf.Abs(lampPos.y - plantPos.y);

                // Растение слева от лампы, расстояние по горизонтали 1 или 2 клетки
                // По вертикали разница 0 или 1 (диагональ)
                if (dx >= 1 && dx <= 2 && dy <= 1)
                {
                    Debug.Log($"Растение на {plantPos} СПРАВА от лампы на {lampPos} (dx={dx}, dy={dy})");
                    return true;
                }
            }
        }
        return false;
    }
    // Обновление всех ламп после установки растения
    // Обновление всех ламп после установки растения
    // Обновление всех ламп после установки растения
    // Обновление всех ламп после установки растения
    private void UpdateAllLampsAfterPlantPlaced()
    {
        Debug.Log("=== ПРОВЕРКА ЛАМП ПОСЛЕ УСТАНОВКИ РАСТЕНИЯ ===");

        // Сначала собираем все ID ламп
        HashSet<string> lampIds = new HashSet<string>();
        foreach (var item in itemTypes)
        {
            if (item.Value == typeof(LampItem))
            {
                lampIds.Add(item.Key);
            }
        }

        Debug.Log($"Всего ламп в itemTypes: {lampIds.Count}");
        Debug.Log($"Всего растений в plantBlocks: {plantBlocks.Count}");

        foreach (var p in plantBlocks)
        {
            Debug.Log($"Растение на {p}");
        }

        // Для каждой лампы проверяем, есть ли рядом растение
        foreach (string lampId in lampIds)
        {
            if (!itemBlocks.ContainsKey(lampId)) continue;

            List<Vector2Int> lampBlocks = new List<Vector2Int>(itemBlocks[lampId]);

            // Выводим все блоки лампы для отладки
            Debug.Log($"Блоки лампы {lampId}:");
            foreach (Vector2Int pos in lampBlocks)
            {
                Debug.Log($"  Блок на ({pos.x}, {pos.y})");
            }

            // НАХОДИМ БЛОК ПЛАФОНА (САМЫЙ ЛЕВЫЙ - МИНИМАЛЬНЫЙ X)
            GameObject shadeBlock = null;
            Vector2Int shadePos = Vector2Int.zero;
            int minX = 100;
            foreach (Vector2Int pos in lampBlocks)
            {
                if (pos.x < minX)
                {
                    minX = pos.x;
                    shadePos = pos;
                }
            }
            Debug.Log($"Плафон (самый левый) должен быть на ({shadePos.x}, {shadePos.y})");

            if (shadePos != Vector2Int.zero && grid[shadePos.x, shadePos.y] != null)
            {
                shadeBlock = grid[shadePos.x, shadePos.y];
            }

            if (shadeBlock == null) continue;

            // Проверяем, есть ли растение рядом (справа от лампы)
            bool nearPlant = false;
            foreach (Vector2Int lampPos in lampBlocks)
            {
                foreach (Vector2Int plantPos in plantBlocks)
                {
                    int dx = lampPos.x - plantPos.x;
                    int dy = Mathf.Abs(lampPos.y - plantPos.y);

                    // Растение слева от лампы (plantPos.x < lampPos.x)
                    // Расстояние 1-2 клетки по горизонтали, разница по вертикали 0-1
                    if (dx >= 1 && dx <= 2 && dy <= 1)
                    {
                        nearPlant = true;
                        Debug.Log($"Лампа на {lampPos} рядом с растением на {plantPos} (dx={dx}, dy={dy})");
                        break;
                    }
                }
                if (nearPlant) break;
            }

            // Находим основание для проверки стола (самый нижний блок)
            Vector2Int basePos = Vector2Int.zero;
            int minY = 100;
            foreach (Vector2Int pos in lampBlocks)
            {
                if (pos.y < minY)
                {
                    minY = pos.y;
                    basePos = pos;
                }
            }
            bool onTable = (basePos.y > 0 && tableBlocks.Contains(new Vector2Int(basePos.x, basePos.y - 1)));

            SpriteRenderer sr = shadeBlock.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Ищем компонент лампы в сохранённых
                LampItem foundLamp = null;
                if (lampItems.ContainsKey(lampId))
                {
                    foundLamp = lampItems[lampId];
                    Debug.Log($"Нашли лампу по ключу {lampId}");
                }
                else
                {
                    Debug.Log($"Ключ {lampId} не найден в lampItems");
                    foreach (var lampKvp in lampItems)
                    {
                        if (itemBlocks.ContainsKey(lampKvp.Key) && itemBlocks[lampKvp.Key].Contains(shadePos))
                        {
                            foundLamp = lampKvp.Value;
                            Debug.Log($"Нашли лампу через совпадение блоков! Ключ={lampKvp.Key}");
                            break;
                        }
                    }
                }

                if (foundLamp != null)
                {
                    if (nearPlant)
                    {
                        if (foundLamp.purpleSprite != null)
                        {
                            sr.sprite = foundLamp.purpleSprite;
                            Debug.Log($"Лампа на {shadePos} стала ФИОЛЕТОВОЙ");
                        }
                        else
                        {
                            Debug.LogError("purpleSprite = NULL!");
                        }
                    }
                    else if (onTable)
                    {
                        if (foundLamp.yellowSprite != null)
                        {
                            sr.sprite = foundLamp.yellowSprite;
                            Debug.Log($"Лампа на {shadePos} стала ЖЁЛТОЙ");
                        }
                        else
                        {
                            Debug.LogError("yellowSprite = NULL!");
                        }
                    }
                    else
                    {
                        if (foundLamp.normalSprite != null)
                        {
                            sr.sprite = foundLamp.normalSprite;
                            Debug.Log($"Лампа на {shadePos} стала НОРМАЛЬНОЙ");
                        }
                        else
                        {
                            Debug.LogError("normalSprite = NULL!");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"foundLamp = NULL! Лампа не найдена!");
                }
            }
        }
    }
    private Vector2Int GetShelfLeftPosition(string shelfId)
    {
        if (!itemBlocks.ContainsKey(shelfId)) return Vector2Int.zero;

        int minX = 100;
        foreach (Vector2Int pos in itemBlocks[shelfId])
        {
            if (pos.x < minX) minX = pos.x;
        }

        foreach (Vector2Int pos in itemBlocks[shelfId])
        {
            if (pos.x == minX) return pos;
        }
        return Vector2Int.zero;
    }

}





