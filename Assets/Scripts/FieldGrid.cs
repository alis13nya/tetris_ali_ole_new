using UnityEngine;
using System.Collections.Generic;

public class FieldGrid : MonoBehaviour
{
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

    private Dictionary<Vector2Int, string> blockToItemId = new Dictionary<Vector2Int, string>();
    private Dictionary<string, HashSet<Vector2Int>> itemBlocks = new Dictionary<string, HashSet<Vector2Int>>();
    private Dictionary<string, System.Type> itemTypes = new Dictionary<string, System.Type>();
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
        bool chairAdjacentToTable = false; // Кресло рядом со столом
        bool tableAdjacentToChair = false; // НОВОЕ: стол рядом с креслом
        bool isOnChair = false; // Предмет стоит на кресле
        bool emptyCupOnTable = false; // Пустой стакан на столе
        bool emptyCupItemOnTable = false; // Пустая кружка на столе
        bool bookStackOnChair = false; // Стопка книг на кресле

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

                // ===== ПЕРЕНЕСИ ЛОГИ СЮДА =====
                Debug.Log($"Текущие блоки столов в tableBlocks: {string.Join(", ", tableBlocks)}");
                Debug.Log($"Проверка стола: ищу стол на позиции ({gridX}, {gridY - 1})");
                Debug.Log($"tableBlocks содержит {new Vector2Int(gridX, gridY - 1)}? = {tableBlocks.Contains(new Vector2Int(gridX, gridY - 1))}");
                // ==============================

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

                        // Проверка: пустая кружка на столе
                        if (gridY > 0 && tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            emptyCupItemOnTable = true;
                            Debug.Log($"Пустая кружка стоит на столе!");

                            // ===== ДОБАВЬ ЗДЕСЬ =====
                            if (AchievementManager.Instance != null)
                            {
                                AchievementManager.Instance.UnlockAchievement("empty_cup_on_table");
                            }
                            // ========================
                        }
                    }
                    else
                    {
                        Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
                    }
                }
                else
                {
                    Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
                }
            }

            if (itemBlockPositions.Count > 0)
            {
                itemBlocks[itemId] = itemBlockPositions;
                itemTypes[itemId] = itemType;
                Debug.Log($"Зарегистрирована пустая кружка {itemId} с {itemBlockPositions.Count} блоками");
            }

            Destroy(shape.gameObject);

            // ПРОВЕРКА МИНУСА ЗА ПУСТУЮ КРУЖКУ НА СТОЛЕ
            if (emptyCupItemOnTable && powerScaleManager != null)
            {
                powerScaleManager.RemoveEmptyCupItemOnTable();
                Debug.Log("Пустая кружка поставлена на стол! Шкала усиления уменьшилась.");
            }

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

                        // Проверка: пустой стакан на столе
                        if (gridY > 0 && tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            emptyCupOnTable = true;
                            Debug.Log($"Пустой стакан стоит на столе!");

                            if (AchievementManager.Instance != null)
                            {
                                AchievementManager.Instance.UnlockAchievement("empty_pencil_cup_on_table");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
                    }
                }
                else
                {
                    Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
                }
            }

            if (itemBlockPositions.Count > 0)
            {
                itemBlocks[itemId] = itemBlockPositions;
                itemTypes[itemId] = itemType;
                Debug.Log($"Зарегистрирован пустой стакан {itemId} с {itemBlockPositions.Count} блоками");
            }

            Destroy(shape.gameObject);

            // ПРОВЕРКА МИНУСА ЗА ПУСТОЙ СТАКАН НА СТОЛЕ
            if (emptyCupOnTable && powerScaleManager != null)
            {
                powerScaleManager.RemoveEmptyCupOnTable();
                Debug.Log("Пустой стакан поставлен на стол! Шкала усиления уменьшилась.");
            }

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
                        Debug.Log($"✓ Блок стола зафиксирован на ({gridX}, {gridY})");

                        // НОВОЕ: проверяем, касается ли блок стола кресла сбоку
                        if (IsTableBlockAdjacentToChair(gridX, gridY))
                        {
                            tableAdjacentToChair = true;
                            Debug.Log($"Блок стола ({gridX}, {gridY}) касается кресла!");
                        }
                    }

                    else if (isComputer)
                    {
                        computerBlocks.Add(new Vector2Int(gridX, gridY));
                        Debug.Log($"✓ Блок компьютера зафиксирован на ({gridX}, {gridY})");

                        // Проверяем, касается ли блок компьютера стола напрямую
                        if (gridY > 0)
                        {
                            if (tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                            {
                                computerTouchesTable = true;
                                Debug.Log($"✓ Блок компьютера ({gridX}, {gridY}) касается стола!");
                            }
                        }
                    }
                    else if (isBookStack)
                    {
                        bookStackBlocks.Add(new Vector2Int(gridX, gridY));
                        Debug.Log($"✓ Блок стопки книг зафиксирован на ({gridX}, {gridY})");
                        
                        // ===== ДОБАВЬ ЭТО =====
                        // Проверка: стопка книг на столе
                        if (IsOnTable(gridX, gridY))
                        {
                            isOnTable = true;
                            Debug.Log($"Стопка книг стоит на столе!");
                        }
                        // ======================

                        // ===== ДОБАВЬ ЭТИ ПРОВЕРКИ =====
                        // Проверка: стоит ли на другой стопке книг
                        if (IsOnBookStack(gridX, gridY))
                        {
                            isOnBookStack = true;
                            Debug.Log($"Стопка книг стоит на другой стопке книг!");
                        }

                        // Проверка: рядом с другой стопкой книг
                        if (IsAdjacentToBookStack(gridX, gridY))
                        {
                            isAdjacentToBookStack = true;
                            Debug.Log($"Стопка книг стоит рядом с другой стопкой книг!");
                        }
                        // =============================
                        // Проверка: стопка книг на кресле
                        if (gridY > 0 && chairBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            bookStackOnChair = true;
                            Debug.Log($"Стопка книг стоит на кресле!");
                        }
                        // ===== ДОБАВЬ ЭТОТ БЛОК =====
                        // Также проверяем, стоит ли этот блок на кресле для достижения
                        if (gridY > 0 && chairBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
                        {
                            isOnChair = true;  // <-- ЭТО ВАЖНО ДЛЯ ДОСТИЖЕНИЯ
                            Debug.Log($"Блок стопки книг ({gridX}, {gridY}) стоит на кресле!");
                        }
                    }
                    else if (isChair)
                    {
                        chairBlocks.Add(new Vector2Int(gridX, gridY));
                        Debug.Log($"✓ Блок кресла {(isChairL ? "L" : "J")} зафиксирован на ({gridX}, {gridY})");
                    }
                    else
                    {
                        Debug.Log($"✓ Блок зафиксирован на ({gridX}, {gridY})");

                        // Проверка взаимодействий для обычных фигур
                        if (IsOnTable(gridX, gridY))
                        {
                            isOnTable = true;
                        }

                        if (IsOnComputer(gridX, gridY))
                        {
                            isOnComputer = true;
                        }

                        if (IsOnEmptyCup(gridX, gridY))
                        {
                            isOnEmptyCup = true;
                        }

                        if (IsOnEmptyCupItem(gridX, gridY))
                        {
                            isOnEmptyCupItem = true;
                        }

                        if (IsOnBookStack(gridX, gridY))
                        {
                            isOnBookStack = true;
                            Debug.Log($"Обнаружено: блок стоит на стопке книг");
                        }

                        if (IsAdjacentToBookStack(gridX, gridY))
                        {
                            isAdjacentToBookStack = true;
                            Debug.Log($"Обнаружено: блок рядом с стопкой книг");
                        }

                        if (IsOnChair(gridX, gridY))
                        {
                            isOnChair = true;
                            Debug.Log($"Обнаружено: блок стоит на кресле");
                        }
                    }
                }
                else
                {
                    Debug.Log($"✗ Позиция ({gridX}, {gridY}) уже занята!");
                }
            }
            else
            {
                Debug.Log($"✗ Позиция ({gridX}, {gridY}) вне границ!");
            }
        }

        if (itemBlockPositions.Count > 0)
        {
            itemBlocks[itemId] = itemBlockPositions;
            itemTypes[itemId] = itemType;
            Debug.Log($"Зарегистрирован предмет {itemId} (тип: {itemType.Name}) с {itemBlockPositions.Count} блоками");
        }

        // ПРОВЕРКА ДЛЯ КАРАНДАШЕЙ НА ПУСТОМ СТАКАНЕ (ВЕРТИКАЛЬНО)
        if (shape is LoosePencilsItem && isOnEmptyCup)
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;

            if (pencils != null && pencils.IsVerticalOrientation)
            {
                if (powerScaleManager != null)
                {
                    powerScaleManager.AddPencilsOnEmptyCup();
                    Debug.Log("✓ Карандаши поставлены на пустой стакан (вертикально)! Шкала усиления увеличилась.");
                }

                StartCoroutine(RemoveTouchingPencilsFromEmptyCupDelayed(pencils));
            }
            else if (pencils != null && !pencils.IsVerticalOrientation)
            {
                Debug.Log("✗ Карандаши на пустом стакане, но не вертикально - бонус не начислен");
            }
        }

        // ПРОВЕРКА ДЛЯ КАРАНДАШЕЙ НА ПУСТОЙ КРУЖКЕ (ВЕРТИКАЛЬНО)
        if (shape is LoosePencilsItem && isOnEmptyCupItem)
        {
            LoosePencilsItem pencils = shape as LoosePencilsItem;

            if (pencils != null && pencils.IsVerticalOrientation)
            {
                // УМЕНЬШАЕМ ШКАЛУ ЗА КАРАНДАШИ В КРУЖКЕ
                if (powerScaleManager != null)
                {
                    float currentAmount = powerScaleManager.currentFillAmount;
                    powerScaleManager.SetFillAmount(Mathf.Max(0, currentAmount - 0.15f));
                    Debug.Log("✗ Карандаши вставлены в пустую кружку (вертикально)! Шкала усиления уменьшилась на 15%.");
                }

                StartCoroutine(RemoveTouchingPencilsFromEmptyCupItemDelayed(pencils));
            }
            else if (pencils != null && !pencils.IsVerticalOrientation)
            {
                Debug.Log("✗ Карандаши на пустой кружке, но не вертикально - эффект не срабатывает");
            }
        }

        // ПРОВЕРКА КОМПЬЮТЕРА НА СТОЛЕ
        if (isComputer && computerTouchesTable)
        {
            if (powerScaleManager != null)
            {
                powerScaleManager.AddComputerOnTable();
                Debug.Log("Компьютер поставлен на стол! Шкала усиления увеличилась.");
            }
        }

        // ПРОВЕРКА КРЕСЛА РЯДОМ СО СТОЛОМ
        if (isChair)
        {
            // Проверяем, касается ли кресло стола сбоку
            chairAdjacentToTable = IsChairAdjacentToTable();
            if (chairAdjacentToTable && powerScaleManager != null)
            {
                powerScaleManager.AddChairAdjacentToTable();
                Debug.Log($"Кресло {(isChairL ? "L" : "J")} поставлено рядом со столом! Шкала усиления увеличилась.");
            }
        }
        // НОВОЕ: ПРОВЕРКА СТОЛА РЯДОМ С КРЕСЛОМ
        if (isTable)
        {
            if (tableAdjacentToChair && powerScaleManager != null)
            {
                powerScaleManager.AddChairAdjacentToTable();
                Debug.Log($"Стол поставлен рядом с креслом! Шкала усиления увеличилась.");
            }
        }
        // ЛОГИКА ДЛЯ СТОПОК КНИГ
        if (isBookStack)
        {
            // Проверяем все возможные взаимодействия с новой стопкой книг
            CheckBookStackInteractions(shape);

            // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: СТОПКА КНИГ НА КРЕСЛЕ
            if (bookStackOnChair && powerScaleManager != null)
            {
                powerScaleManager.RemoveBookStackOnChair();
                Debug.Log("Стопка книг поставлена на кресло! Шкала усиления уменьшилась.");
            }
        }
        else // Для обычных фигур
        {
            // Проверяем взаимодействия в зависимости от позиции
            if (isOnTable)
            {
                CheckForSpecialItemsOnTable(shape);
            }

            if (isOnComputer)
            {
                CheckForSpecialItemsOnComputer(shape);
            }

            if (isOnBookStack)
            {
                CheckForItemsOnBookStack(shape);
            }

            if (isAdjacentToBookStack)
            {
                CheckForItemsAdjacentToBookStack(shape);
            }

            if (isOnChair)
            {
                CheckForItemsOnChair(shape);
            }
        }
        // После блока проверок для стопки книг, кресел, столов и т.д.
        CheckAchievements(shape, isOnTable, isOnComputer, computerTouchesTable,
                          emptyCupOnTable, emptyCupItemOnTable, isBookStack,
                          isOnBookStack, isAdjacentToBookStack, isOnChair,
                          shape.GetType() == typeof(ChairItemL) || shape.GetType() == typeof(ChairItemJ));

        Debug.Log($"Зафиксировано: {fixedBlocks}/{currentBlocksToProcess.Count} блоков");
        Destroy(shape.gameObject);
    }

    // НОВЫЙ МЕТОД: Проверка стоит ли блок на кресле
    private bool IsOnChair(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            if (chairBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
        // Здесь можно добавить логику для предметов, стоящих на кресле
        Debug.Log($"Предмет {shape.GetType().Name} стоит на кресле");
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

    // Вспомогательные методы проверки
    private bool IsOnTable(int gridX, int gridY)
    {
        if (gridY == 0) return false;

        if (gridY - 1 >= 0)
        {
            if (tableBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
            if (computerBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
            if (emptyCupBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
            if (emptyCupItemBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
            if (bookStackBlocks.Contains(new Vector2Int(gridX, gridY - 1)))
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
            if (block.y > 0 && chairBlocks.Contains(new Vector2Int(block.x, block.y - 1)))
            {
                return true;
            }
        }

        return false;
    }

    // МЕТОД: Проверка стоит ли стопка книг на столе (физическое соприкосновение снизу)
    private bool IsBookStackOnTable()
    {
        // Получаем текущую стопку книг (последнюю добавленную)
        var newBookStackBlocks = GetNewestBookStackBlocks();

        if (newBookStackBlocks.Count == 0) return false;

        Debug.Log($"Проверка стоит ли стопка книг на столе. Новых блоков: {newBookStackBlocks.Count}");

        foreach (var newBlock in newBookStackBlocks)
        {
            // Проверяем позицию ПОД текущим блоком
            if (newBlock.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(newBlock.x, newBlock.y - 1);

                // Проверяем, есть ли стол ПОД текущим блоком
                if (tableBlocks.Contains(belowPos))
                {
                    Debug.Log($"✓ Обнаружено: новый блок стопки книг ({newBlock.x}, {newBlock.y}) " +
                             $"стоит на столе в позиции ({belowPos.x}, {belowPos.y})");
                    return true;
                }
            }
        }

        Debug.Log($"✗ Новая стопка книг не стоит на столе");
        return false;
    }

    // МЕТОД: Проверка стоит ли стопка книг на другой стопке книг (физическое соприкосновение снизу)
    private bool IsBookStackOnOtherBookStack()
    {
        // Получаем текущую стопку книг (последнюю добавленную)
        var newBookStackBlocks = GetNewestBookStackBlocks();

        if (newBookStackBlocks.Count == 0) return false;

        Debug.Log($"Проверка стоит ли стопка книг на другой стопке. Новых блоков: {newBookStackBlocks.Count}");

        foreach (var newBlock in newBookStackBlocks)
        {
            // Проверяем позицию ПОД текущим блоком
            if (newBlock.y > 0)
            {
                Vector2Int belowPos = new Vector2Int(newBlock.x, newBlock.y - 1);

                // Проверяем, есть ли блок другой стопки книг ПОД текущим блоком
                if (bookStackBlocks.Contains(belowPos) && !newBookStackBlocks.Contains(belowPos))
                {
                    Debug.Log($"✓ Обнаружено: новый блок ({newBlock.x}, {newBlock.y}) " +
                             $"стоит на другой стопке книг в позиции ({belowPos.x}, {belowPos.y})");
                    return true;
                }
            }
        }

        Debug.Log($"✗ Новая стопка книг не стоит на другой стопке книг");
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
        // Мгновенно меняем визуал (без корутины)
        for (int x = 0; x < 10; x++)
        {
            if (grid[x, y] != null)
            {
                // Взрыв частиц
                if (blockBreakParticle != null)
                {
                    ParticleSystem particles = Instantiate(blockBreakParticle, grid[x, y].transform.position, Quaternion.identity);
                    particles.Play();
                    Destroy(particles.gameObject, 1f);
                }

                // Мгновенное удаление (без плавности)
                Destroy(grid[x, y]);
                grid[x, y] = null;
            }
        }
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
                // Эффект частиц
                QuickClearAnimation(y);

                // Физическое удаление строки и сдвиг вниз
                ClearLine(y);
                MoveLinesDown(y);

                linesCleared++;
                // while продолжит проверять ту же строку y (там теперь другие блоки)
            }
        }

        if (linesCleared > 0)
        {   // ===== ДОБАВЬ ЭТО =====
            ApplyGravity();
            // =====================
            Debug.Log($"Удалено линий: {linesCleared}");
        }
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

                SpriteRenderer renderer = neutralBlock.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(255f, 255f, 255f, 1f);
                }

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

        tableBlocks = newTableBlocks;
        computerBlocks = newComputerBlocks;
        bookStackBlocks = newBookStackBlocks;
        emptyCupBlocks = newEmptyCupBlocks;
        emptyCupItemBlocks = newEmptyCupItemBlocks;
        chairBlocks = newChairBlocks;
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
}