using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Папка с бумагами - горизонтальная фигура из двух блоков.
/// </summary>
public class FileFolderItem : TetrisShape
{
    [Header("Префабы папки")]
    public GameObject leftPartPrefab;   // Левая часть папки
    public GameObject rightPartPrefab;  // Правая часть папки

    void Start()
    {
        Debug.Log($"Папка: Start() вызван");
        InitializeFolder();
    }

    /// <summary>
    /// Инициализация папки: задаёт локальные координаты блоков и создаёт из префабов.
    /// </summary>
    void InitializeFolder()
    {
        Debug.Log("Папка: Инициализация");

        // Два блока по горизонтали: левый (0,0) и правый (1,0)
        blocks = new Vector2[]
        {
            new Vector2(0, 0),   // левая часть
            new Vector2(1, 0)    // правая часть
        };

        // Создаём папку из префабов
        CreateFolderFromPrefabs();
    }

    /// <summary>
    /// Создание визуального представления папки из назначенных префабов.
    /// </summary>
    void CreateFolderFromPrefabs()
    {
        if (leftPartPrefab == null || rightPartPrefab == null)
        {
            Debug.LogError("Папка: Не все префабы назначены!");
            return;
        }

        Debug.Log("Папка: Создание из префабов");

        ClearAllBlocks();

        // Создаём левую часть на позиции (0,0)
        CreateFolderPart(leftPartPrefab, 0, 0);
        // Создаём правую часть на позиции (1,0)
        CreateFolderPart(rightPartPrefab, 1, 0);

        UpdateShapeBlocks();
    }

    /// <summary>
    /// Создаёт одну часть папки в заданных локальных координатах.
    /// </summary>
    void CreateFolderPart(GameObject partPrefab, float x, float y)
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
            Debug.Log($"Папка: Обновлено блоков в системе: {blockList.Count}");
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
        Debug.Log($"FileFolderItem: InitializeShape() вызван");
        InitializeFolder();
    }

    /// <summary>
    /// Возвращает строковое имя типа фигуры для системы предпросмотра.
    /// </summary>
    public override string GetShapeTypeName()
    {
        return "FileFolderItem";
    }
}