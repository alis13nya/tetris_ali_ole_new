using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerScaleManager : MonoBehaviour
{
    [Header("Настройки шкалы")]
    public float currentFillAmount = 0f;
    public float maxFillAmount = 1f;

    [Header("Нейтральный блок (для замены предметов)")]
    public GameObject neutralBlockPrefab; // Префаб нейтрального блока

    [Header("Настройки заполнения - КРЕСЛА")]
    public float chairAdjacentToTableAdd = 0.1f;    // Кресло рядом со столом

    [Header("Настройки заполнения - СТОПКА КНИГ НА КРЕСЛЕ")]
    public float bookStackOnChairRemove = 0.15f;    // Стопка книг на кресле
    public float bookStackOnBookStackOnChairRemove = 0.1f; // Стопка книг на стопке книг, которая на кресле

    [Header("Настройки заполнения - ПУСТЫЕ ПРЕДМЕТЫ НА СТОЛЕ")]
    public float emptyCupOnTableRemove = 0.1f;      // Пустой стакан на столе
    public float emptyCupItemOnTableRemove = 0.1f;  // Пустая кружка на столе

    [Header("Настройки заполнения - СТОЛ")]
    public float normalCupOnTableAdd = 0.1f;           // Непролитая кружка на стол
    public float spilledCupOnTableRemove = 0.1f;       // Пролитая кружка на стол
    public float normalPencilCupOnTableAdd = 0.1f;     // Не рассыпанные карандаши на стол
    public float spilledPencilsOnTableRemove = 0.1f;   // Рассыпанные карандаши на стол
    public float computerOnTableAdd = 0.2f;            // Компьютер на стол
    public float bookStackOnTableAdd = 0.15f;          // Стопка книг на стол

    [Header("Настройки заполнения - КОМПЬЮТЕР")]
    public float normalCupOnComputerRemove = 0.15f;    // Непролитая кружка на компьютер
    public float spilledCupOnComputerRemove = 0.2f;    // Пролитая кружка на компьютер

    [Header("Настройки заполнения - СТОПКА КНИГ")]
    public float bookStackOnBookStackAdd = 0.12f;    // Стопка книг на стопку книг

    [Header("Настройки заполнения - ПУСТОЙ СТАКАН")] // НОВАЯ СЕКЦИЯ
    public float pencilsOnEmptyCupAdd = 0.18f;        // Карандаши на пустой стакан

    [Header("Настройки заполнения - КРУЖКА НА КРЕСЛЕ")]
    public float cupOnChairRemove = 0.15f;

    [Header("UI элементы")]
    public Slider powerScaleSlider;
    public Image fillImage;
    public Color normalColor = Color.blue;
    public Color fullColor = Color.red;

    [Header("Визуальные эффекты")]
    public ParticleSystem fullScaleEffect;
    public ParticleSystem decreaseEffect;
    public ParticleSystem increaseEffect;
    public AudioClip fullScaleSound;
    public AudioClip decreaseSound;
    public AudioClip increaseSound;

    [Header("Автоматическая активация")]
    public bool autoActivate = true;

    private FieldGrid fieldGrid;
    private AudioSource audioSource;
    private bool isPowerReady = false;

    public System.Action OnPowerReady;
    void Start()
    {
        fieldGrid = FindObjectOfType<FieldGrid>();
        audioSource = GetComponent<AudioSource>();

        if (powerScaleSlider != null)
        {
            powerScaleSlider.gameObject.SetActive(true);
            powerScaleSlider.maxValue = maxFillAmount;
            powerScaleSlider.value = currentFillAmount;
        }

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
        }

        
    }

    void Update()
    {

        // Старая логика для обратной совместимости
        if (currentFillAmount >= maxFillAmount && !isPowerReady)
        {
            if (autoActivate)
            {
                UsePower();
            }
            else
            {
                ActivatePower();
            }
        }
    }
    // === НОВЫЕ МЕТОДЫ ДЛЯ КРЕСЕЛ ===
    public void AddChairAdjacentToTable()
    {
        ChangeFillAmount(0.1f, $"Кресло рядом со столом: +10%");
    }
    public void RemoveCupOnChair()
    {
        ChangeFillAmount(-cupOnChairRemove, $"Кружка с чаем на кресле: -{cupOnChairRemove * 100}%");
    }
    // === НОВЫЕ МЕТОДЫ ДЛЯ СТОПОК КНИГ НА КРЕСЛЕ ===
    public void RemoveBookStackOnChair()
    {
        ChangeFillAmount(-0.15f, $"Стопка книг на кресле: -15%");
    }

    public void RemoveBookStackOnBookStackOnChair()
    {
        ChangeFillAmount(-0.1f, $"Стопка книг на стопке книг, которая на кресле: -10%");
    }

    // === НОВЫЕ МЕТОДЫ ДЛЯ ПУСТОГО СТАКАНА НА СТОЛЕ ===
    public void RemoveEmptyCupOnTable()
    {
        ChangeFillAmount(-0.1f, $"Пустой стакан на столе: -10%");
    }

    // === НОВЫЕ МЕТОДЫ ДЛЯ ПУСТОЙ КРУЖКИ НА СТОЛЕ ===
    public void RemoveEmptyCupItemOnTable()
    {
        ChangeFillAmount(-0.1f, $"Пустая кружка на столе: -10%");
    }
    // === МЕТОДЫ ДЛЯ СТОЛА ===

    public void RemovePencilsInEmptyCupItem()
    {
        ChangeFillAmount(-0.15f, "Карандаши в пустой кружке: -15%");
    }
    public void AddBookStackAdjacentToBookStack()
    {
        ChangeFillAmount(0.1f, $"Стопка книг рядом с другой стопкой книг: +10%");
    }
    public void AddNormalCupOnTable()
    {
        ChangeFillAmount(normalCupOnTableAdd, $"Непролитая кружка на стол: +{normalCupOnTableAdd * 100}%");
    }

    public void RemoveSpilledCupOnTable()
    {
        ChangeFillAmount(-spilledCupOnTableRemove, $"Пролитая кружка на стол: -{spilledCupOnTableRemove * 100}%");
    }

    public void AddNormalPencilCupOnTable()
    {
        ChangeFillAmount(normalPencilCupOnTableAdd, $"Нерассыпанные карандаши на стол: +{normalPencilCupOnTableAdd * 100}%");
    }

    public void RemoveSpilledPencilsOnTable()
    {
        ChangeFillAmount(-spilledPencilsOnTableRemove, $"Рассыпанные карандаши на стол: -{spilledPencilsOnTableRemove * 100}%");
    }

    public void AddComputerOnTable()
    {
        ChangeFillAmount(computerOnTableAdd, $"Компьютер на стол: +{computerOnTableAdd * 100}%");
    }

    public void AddBookStackOnTable()
    {
        ChangeFillAmount(bookStackOnTableAdd, $"Стопка книг на стол: +{bookStackOnTableAdd * 100}%");
    }

    // === МЕТОДЫ ДЛЯ КОМПЬЮТЕРА ===

    public void RemoveNormalCupOnComputer()
    {
        ChangeFillAmount(-normalCupOnComputerRemove, $"Непролитая кружка на компьютер: -{normalCupOnComputerRemove * 100}%");
    }

    public void RemoveSpilledCupOnComputer()
    {
        ChangeFillAmount(-spilledCupOnComputerRemove, $"Пролитая кружка на компьютер: -{spilledCupOnComputerRemove * 100}%");
    }

    // === МЕТОДЫ ДЛЯ СТОПКИ КНИГ ===

    public void AddBookStackOnBookStack()
    {
        ChangeFillAmount(bookStackOnBookStackAdd, $"Стопка книг на стопку книг: +{bookStackOnBookStackAdd * 100}%");
    }

    // === НОВЫЕ МЕТОДЫ ДЛЯ ПУСТОГО СТАКАНА ===

    public void AddPencilsOnEmptyCup()
    {
        ChangeFillAmount(pencilsOnEmptyCupAdd, $"Карандаши на пустой стакан: +{pencilsOnEmptyCupAdd * 100}%");
    }

    // === ОСНОВНЫЕ МЕТОДЫ ===

    private void ChangeFillAmount(float change, string message)
    {

        // Старая логика для обратной совместимости
        if (isPowerReady) return;

        currentFillAmount += change;

        if (currentFillAmount > maxFillAmount)
        {
            currentFillAmount = maxFillAmount;
        }
        else if (currentFillAmount < 0f)
        {
            currentFillAmount = 0f;
        }

        UpdateUI();
        

        if (change > 0)
        {
            if (increaseEffect != null)
                increaseEffect.Play();
            if (increaseSound != null && audioSource != null)
                audioSource.PlayOneShot(increaseSound);
        }
        else if (change < 0)
        {
            if (decreaseEffect != null)
                decreaseEffect.Play();
            if (decreaseSound != null && audioSource != null)
                audioSource.PlayOneShot(decreaseSound);
        }

        Debug.Log($"{message} | Всего: {currentFillAmount * 100}%");
    }

    private void ActivatePower()
    {
        isPowerReady = true;
        Debug.Log("Шкала усиления заполнена! Способность готова к использованию.");

        OnPowerReady?.Invoke();  // <-- эта строка должна быть

        if (fullScaleEffect != null) fullScaleEffect.Play();
        if (fullScaleSound != null && audioSource != null)
            audioSource.PlayOneShot(fullScaleSound);
    }

    public void UsePower()
    {
    

        if (!isPowerReady && !autoActivate) return;

        Debug.Log("Использование способности: удаление нижней строки");
        RemoveBottomLine();
        ResetScale();
    }

    private void RemoveBottomLine()
    {
        if (fieldGrid == null) return;

        var gridField = typeof(FieldGrid).GetField("grid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (gridField != null)
        {
            GameObject[,] grid = (GameObject[,])gridField.GetValue(fieldGrid);

            Vector2 fieldOffset = Vector2.zero;
            var fieldOffsetField = typeof(FieldGrid).GetField("fieldOffset",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fieldOffsetField != null)
            {
                fieldOffset = (Vector2)fieldOffsetField.GetValue(fieldGrid);
            }

            int bottomLineY = 0;
            bool lineHasBlocks = false;

            // Сначала удаляем блоки на нижней строке
            for (int x = 0; x < 10; x++)
            {
                if (grid[x, bottomLineY] != null)
                {
                    lineHasBlocks = true;
                    Destroy(grid[x, bottomLineY]);
                    grid[x, bottomLineY] = null;
                }
            }

            if (lineHasBlocks)
            {
                Debug.Log("Нижняя строка удалена через PowerScaleManager!");

                // Теперь нужно вызвать проверку целостности предметов
                CheckItemIntegrityForBottomLineClear(fieldGrid, bottomLineY);

                // Перемещаем все блоки вниз
                for (int y = bottomLineY + 1; y < 20; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (grid[x, y] != null)
                        {
                            grid[x, y - 1] = grid[x, y];
                            grid[x, y] = null;
                            grid[x, y - 1].transform.position = new Vector3(
                                x + fieldOffset.x,
                                y - 1 + fieldOffset.y,
                                -1f
                            );
                        }
                    }
                }

                // Также нужно обновить все словари после перемещения
                UpdateDictionariesAfterPowerScaleClear(fieldGrid, bottomLineY);
            }
            else
            {
                Debug.Log("Нижняя строка пуста, ничего не удалено");
            }
        }
    }

    // Новый метод: проверка целостности предметов после удаления нижней строки через PowerScale
    private void CheckItemIntegrityForBottomLineClear(FieldGrid fieldGrid, int clearedLineY)
    {
        // Используем рефлексию для доступа к приватным словарям FieldGrid
        var blockToItemIdField = typeof(FieldGrid).GetField("blockToItemId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemBlocksField = typeof(FieldGrid).GetField("itemBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemTypesField = typeof(FieldGrid).GetField("itemTypes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (blockToItemIdField == null || itemBlocksField == null || itemTypesField == null)
        {
            Debug.LogError("Не удалось получить доступ к словарям FieldGrid!");
            return;
        }

        // Получаем словари из FieldGrid
        var blockToItemId = (Dictionary<Vector2Int, string>)blockToItemIdField.GetValue(fieldGrid);
        var itemBlocks = (Dictionary<string, HashSet<Vector2Int>>)itemBlocksField.GetValue(fieldGrid);
        var itemTypes = (Dictionary<string, System.Type>)itemTypesField.GetValue(fieldGrid);

        // Собираем предметы, затронутые удалением
        HashSet<string> affectedItemIds = new HashSet<string>();

        // Ищем предметы на удаленной строке
        for (int x = 0; x < 10; x++)
        {
            Vector2Int pos = new Vector2Int(x, clearedLineY);
            if (blockToItemId.ContainsKey(pos))
            {
                affectedItemIds.Add(blockToItemId[pos]);
                // Удаляем из словаря
                blockToItemId.Remove(pos);
            }
        }

        // Проверяем целостность затронутых предметов
        foreach (string itemId in affectedItemIds)
        {
            if (!itemBlocks.ContainsKey(itemId)) continue;

            // Проверяем тип предмета - исключаем стопку книг
            if (itemTypes.ContainsKey(itemId) && itemTypes[itemId] == typeof(BookStackItem))
            {
                Debug.Log($"Стопка книг {itemId} не заменяется на нейтральные блоки (PowerScale)");
                continue; // Не заменяем стопку книг
            }

            // Собираем оставшиеся блоки предмета
            HashSet<Vector2Int> remainingBlocks = new HashSet<Vector2Int>();
            foreach (Vector2Int pos in itemBlocks[itemId])
            {
                // Проверяем, что блок еще существует (не на удаленной строке)
                if (pos.y != clearedLineY)
                {
                    remainingBlocks.Add(new Vector2Int(pos.x, pos.y));
                }
            }

            int originalBlockCount = itemBlocks[itemId].Count;
            int remainingCount = remainingBlocks.Count;

            // Если предмет почти уничтожен (менее 50% блоков осталось)
            if (remainingCount > 0 && remainingCount < originalBlockCount * 0.9f)
            {
                Debug.Log($"Предмет {itemId} (тип: {itemTypes[itemId].Name}) почти разрушен через PowerScale. Осталось {remainingCount}/{originalBlockCount} блоков.");

                // Заменяем на нейтральные блоки
                ReplaceItemWithNeutralBlocksForPowerScale(fieldGrid, itemId, remainingBlocks);
            }
            else if (remainingCount == 0)
            {
                // Предмет полностью уничтожен
                itemBlocks.Remove(itemId);
                itemTypes.Remove(itemId);
            }
            else
            {
                // Обновляем информацию о предмете
                itemBlocks[itemId] = remainingBlocks;
            }
        }

        // Обновляем словари в FieldGrid
        itemBlocksField.SetValue(fieldGrid, itemBlocks);
        itemTypesField.SetValue(fieldGrid, itemTypes);
        blockToItemIdField.SetValue(fieldGrid, blockToItemId);
    }

    // Замена предмета на нейтральные блоки для PowerScale
    private void ReplaceItemWithNeutralBlocksForPowerScale(FieldGrid fieldGrid, string itemId, HashSet<Vector2Int> blockPositions)
    {
        if (neutralBlockPrefab == null)
        {
            Debug.LogError("NeutralBlockPrefab не назначен в PowerScaleManager!");
            return;
        }

        // Получаем сетку через рефлексию
        var gridField = typeof(FieldGrid).GetField("grid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var blockToItemIdField = typeof(FieldGrid).GetField("blockToItemId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemBlocksField = typeof(FieldGrid).GetField("itemBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemTypesField = typeof(FieldGrid).GetField("itemTypes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (gridField == null || blockToItemIdField == null || itemBlocksField == null || itemTypesField == null)
            return;

        GameObject[,] grid = (GameObject[,])gridField.GetValue(fieldGrid);
        var blockToItemId = (Dictionary<Vector2Int, string>)blockToItemIdField.GetValue(fieldGrid);
        var itemBlocks = (Dictionary<string, HashSet<Vector2Int>>)itemBlocksField.GetValue(fieldGrid);
        var itemTypes = (Dictionary<string, System.Type>)itemTypesField.GetValue(fieldGrid);

        // Проходим по всем оставшимся блокам предмета
        foreach (Vector2Int pos in blockPositions)
        {
            if (grid[pos.x, pos.y] != null)
            {
                // Сохраняем позицию (используем метод FieldGrid)
                Vector3 worldPos = fieldGrid.GridToWorldPosition(pos.x, pos.y);

                // Удаляем старый блок
                Destroy(grid[pos.x, pos.y]);

                // Создаем нейтральный блок
                GameObject neutralBlock = Instantiate(neutralBlockPrefab, worldPos, Quaternion.identity);
                grid[pos.x, pos.y] = neutralBlock;

                // Визуальная настройка
                SpriteRenderer renderer = neutralBlock.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(255f, 255f, 255f, 1f);
                }

                // Удаляем из словаря
                blockToItemId.Remove(pos);
            }
        }

        // Удаляем предмет из отслеживания
        itemBlocks.Remove(itemId);
        itemTypes.Remove(itemId);

        // Обновляем словари в FieldGrid
        gridField.SetValue(fieldGrid, grid);
        blockToItemIdField.SetValue(fieldGrid, blockToItemId);
        itemBlocksField.SetValue(fieldGrid, itemBlocks);
        itemTypesField.SetValue(fieldGrid, itemTypes);

        Debug.Log($"Предмет {itemId} заменен на нейтральные блоки через PowerScale");
    }

    // Обновление словарей после удаления строки через PowerScale
    private void UpdateDictionariesAfterPowerScaleClear(FieldGrid fieldGrid, int clearedLineY)
    {
        var blockToItemIdField = typeof(FieldGrid).GetField("blockToItemId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemBlocksField = typeof(FieldGrid).GetField("itemBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var itemTypesField = typeof(FieldGrid).GetField("itemTypes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (blockToItemIdField == null || itemBlocksField == null || itemTypesField == null)
            return;

        var blockToItemId = (Dictionary<Vector2Int, string>)blockToItemIdField.GetValue(fieldGrid);
        var itemBlocks = (Dictionary<string, HashSet<Vector2Int>>)itemBlocksField.GetValue(fieldGrid);
        var itemTypes = (Dictionary<string, System.Type>)itemTypesField.GetValue(fieldGrid);

        // Обновляем blockToItemId
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

        // Обновляем itemBlocks и itemTypes
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
                // Сохраняем тип предмета
                if (itemTypes.ContainsKey(kvp.Key))
                {
                    newItemTypes[kvp.Key] = itemTypes[kvp.Key];
                }
            }
        }

        // Обновляем словари в FieldGrid
        blockToItemIdField.SetValue(fieldGrid, blockToItemId);
        itemBlocksField.SetValue(fieldGrid, newItemBlocks);
        itemTypesField.SetValue(fieldGrid, newItemTypes);
    }

    private void ResetScale()
    {
        currentFillAmount = 0f;
        isPowerReady = false;
        UpdateUI();
        
    }

    private void UpdateUI()
    {
        if (powerScaleSlider != null)
        {
            powerScaleSlider.maxValue = maxFillAmount;
            powerScaleSlider.value = currentFillAmount;
        }
    }

    
    public void SetFillAmount(float amount)
    {
        currentFillAmount = Mathf.Clamp(amount, 0f, maxFillAmount);
        UpdateUI();
        
    }

    public bool IsPowerReady()
    {
        return isPowerReady;
    }

    
}