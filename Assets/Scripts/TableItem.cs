using UnityEngine;

public class TableItem : TetrisShape
{
    [Header("Префабы стола")]
    public GameObject tableTopPrefab;
    public GameObject tableLeg1Prefab;
    public GameObject tableLeg2Prefab;
    public GameObject tableLeg3Prefab;
    public GameObject tableLeg4Prefab;

    void Start()
    {
        Debug.Log($"Стол: Start() вызван");
        InitializeTable();
    }

    void InitializeTable()
    {
        Debug.Log("Стол: Инициализация");

        blocks = new Vector2[] {
        new Vector2(0, 0),    // Столешница
        new Vector2(-1, -1),  // Ножка 1
        new Vector2(1, -1),   // Ножка 2
        new Vector2(-1, 0),   // Ножка 3
        new Vector2(1, 0)     // Ножка 4
        };

        CreateTableFromPrefabs();
    }

    void CreateTableFromPrefabs()
    {
        if (tableTopPrefab == null || tableLeg1Prefab == null || tableLeg2Prefab == null ||
            tableLeg3Prefab == null || tableLeg4Prefab == null)
        {
            Debug.LogError("Стол: Не все префабы назначены!");
            return;
        }

        Debug.Log("Стол: Создание из префабов");

        ClearAllBlocks();

        CreateTablePart(tableTopPrefab, 0, 0);
        CreateTablePart(tableLeg1Prefab, -1, -1);
        CreateTablePart(tableLeg2Prefab, 1, -1);
        CreateTablePart(tableLeg3Prefab, -1, 0);
        CreateTablePart(tableLeg4Prefab, 1, 0);

        UpdateShapeBlocks();
    }

    void CreateTablePart(GameObject tablePartPrefab, float x, float y)
    {
        if (tablePartPrefab == null) return;

        Vector3 partPosition = transform.position + new Vector3(x, y, 0);
        GameObject partInstance = Instantiate(tablePartPrefab, partPosition, transform.rotation);
        partInstance.transform.SetParent(transform);

        Debug.Log($"Стол: Создана часть {tablePartPrefab.name} на позиции ({x}, {y})");
    }

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

    void UpdateShapeBlocks()
    {
        System.Collections.Generic.List<GameObject> blockList = new System.Collections.Generic.List<GameObject>();

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
            Debug.Log($"Стол: Обновлено блоков в системе: {blockList.Count}");
        }
    }

    public new void Move(Vector2 direction)
    {
        base.Move(direction);
        UpdateShapeBlocks();
    }

    // НОВЫЙ МЕТОД ДЛЯ ИНИЦИАЛИЗАЦИИ ПРЕДПРОСМОТРА
    public override void InitializeShape()
    {
        Debug.Log($"TableItem: InitializeShape() вызван");
        InitializeTable();
    }
}