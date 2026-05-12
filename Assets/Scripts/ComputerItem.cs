using UnityEngine;

public class ComputerItem : TetrisShape
{
    [Header("Префабы компьютера")]
    public GameObject computerScreenPrefab;     // Экран компьютера (верхний левый)
    public GameObject computerKeyboardPrefab;   // Клавиатура (верхний правый)
    public GameObject computerBase1Prefab;      // Основание 1 (нижний левый)
    public GameObject computerBase2Prefab;      // Основание 2 (нижний правый)

    void Start()
    {
        Debug.Log($"Компьютер: Start() вызван");
        InitializeComputer();
    }

    // Инициализация компьютера
    void InitializeComputer()
    {
        Debug.Log("Компьютер: Инициализация");

        // Устанавливаем координаты для O-фигуры (2x2 блока)
        blocks = new Vector2[] {
            new Vector2(0, 0),   // Нижний левый
            new Vector2(1, 0),   // Нижний правый
            new Vector2(0, 1),   // Верхний левый
            new Vector2(1, 1)    // Верхний правый
        };

        // Создаем компьютер из префабов
        CreateComputerFromPrefabs();
    }

    // Создание компьютера из префабов
    void CreateComputerFromPrefabs()
    {
        if (computerScreenPrefab == null || computerKeyboardPrefab == null ||
            computerBase1Prefab == null || computerBase2Prefab == null)
        {
            Debug.LogError("Компьютер: Не все префабы назначены!");
            return;
        }

        Debug.Log("Компьютер: Создание из префабов");

        ClearAllBlocks();

        // Создаем части компьютера на соответствующих позициях O-фигуры
        CreateComputerPart(computerBase1Prefab, 0, 0);         // Основание 1 (нижний левый)
        CreateComputerPart(computerBase2Prefab, 1, 0);         // Основание 2 (нижний правый)
        CreateComputerPart(computerScreenPrefab, 0, 1);        // Экран (верхний левый)
        CreateComputerPart(computerKeyboardPrefab, 1, 1);      // Клавиатура (верхний правый)

        UpdateShapeBlocks();
    }

    // Создание одной части компьютера
    void CreateComputerPart(GameObject computerPartPrefab, float x, float y)
    {
        if (computerPartPrefab == null) return;

        Vector3 partPosition = transform.position + new Vector3(x, y, 0);
        GameObject partInstance = Instantiate(computerPartPrefab, partPosition, transform.rotation);
        partInstance.transform.SetParent(transform);

        Debug.Log($"Компьютер: Создана часть {computerPartPrefab.name} на позиции ({x}, {y})");
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
            Debug.Log($"Компьютер: Обновлено блоков в системе: {blockList.Count}");
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
        Debug.Log($"ComputerItem: InitializeShape() вызван");
        InitializeComputer();
    }
}