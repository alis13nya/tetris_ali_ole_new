using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Фигура "Принтер", состоящая из двух блоков по горизонтали.
/// </summary>
public class PrinterItem : TetrisShape
{
    [Header("Префабы принтера")]
    public GameObject leftPartPrefab;   // Левая часть (базовая)
    public GameObject rightPartPrefab;  // Правая часть

    void Start()
    {
        Debug.Log($"Принтер: Start() вызван");
        InitializePrinter();
    }

    /// <summary>
    /// Инициализация принтера: задаёт локальные координаты блоков и создаёт из префабов.
    /// </summary>
    void InitializePrinter()
    {
        Debug.Log("Принтер: Инициализация");

        // Два блока по горизонтали: левый (0,0) и правый (1,0)
        blocks = new Vector2[]
        {
            new Vector2(0, 0),   // левая часть
            new Vector2(1, 0)    // правая часть
        };

        // Создаём принтер из префабов
        CreatePrinterFromPrefabs();
    }

    /// <summary>
    /// Создание визуального представления принтера из назначенных префабов.
    /// </summary>
    void CreatePrinterFromPrefabs()
    {
        if (leftPartPrefab == null || rightPartPrefab == null)
        {
            Debug.LogError("Принтер: Не все префабы назначены!");
            return;
        }

        Debug.Log("Принтер: Создание из префабов");

        ClearAllBlocks();

        // Создаём левую часть на позиции (0,0)
        CreatePrinterPart(leftPartPrefab, 0, 0);
        // Создаём правую часть на позиции (1,0)
        CreatePrinterPart(rightPartPrefab, 1, 0);

        UpdateShapeBlocks();
    }

    /// <summary>
    /// Создаёт одну часть принтера в заданных локальных координатах.
    /// </summary>
    void CreatePrinterPart(GameObject partPrefab, float x, float y)
    {
        if (partPrefab == null) return;

        Vector3 partPosition = transform.position + new Vector3(x, y, 0);
        GameObject partInstance = Instantiate(partPrefab, partPosition, transform.rotation);
        partInstance.transform.SetParent(transform);
    }

    /// <summary>
    /// Удаляет все дочерние объекты (блоки) фигуры.
    /// </summary>
    void ClearAllBlocks()
    {
        foreach (Transform child in transform)
        {
            if (child != transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Обновляет массив shapeBlocks в базовом классе, синхронизируя его с текущими дочерними объектами.
    /// </summary>
    void UpdateShapeBlocks()
    {
        List<GameObject> blockList = new List<GameObject>();

        foreach (Transform child in transform)
        {
            if (child != transform && child.gameObject != null)
            {
                blockList.Add(child.gameObject);
            }
        }

        var shapeBlocksField = typeof(TetrisShape).GetField("shapeBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (shapeBlocksField != null)
        {
            shapeBlocksField.SetValue(this, blockList.ToArray());
            Debug.Log($"Принтер: Обновлено блоков в системе: {blockList.Count}");
        }
    }

    /// <summary>
    /// Перемещает фигуру и обновляет список блоков.
    /// </summary>
    public new void Move(Vector2 direction)
    {
        base.Move(direction);
        UpdateShapeBlocks();
    }

    /// <summary>
    /// Инициализация фигуры (вызывается GameManager при создании предпросмотра или спавне).
    /// </summary>
    public override void InitializeShape()
    {
        Debug.Log($"PrinterItem: InitializeShape() вызван");
        InitializePrinter();
    }

    /// <summary>
    /// Возвращает строковое имя типа фигуры для системы предпросмотра.
    /// </summary>
    public override string GetShapeTypeName()
    {
        return "PrinterItem";
    }
}