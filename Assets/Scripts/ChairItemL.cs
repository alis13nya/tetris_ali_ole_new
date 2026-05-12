using UnityEngine;

public class ChairItemL : TetrisShape
{
    [Header("Префабы кресла (L-фигура)")]
    public GameObject chairSeatPrefab;          // Сиденье кресла (основание)
    public GameObject chairBackPrefab;          // Спинка кресла (вертикальная часть)
    public GameObject chairArmrestPrefab;       // Подлокотник
    public GameObject chairLegPrefab;           // Ножка кресла

    void Start()
    {
        Debug.Log($"Кресло: Start() вызван");
        InitializeChair();
    }

    // Инициализация кресла
    void InitializeChair()
    {
        Debug.Log("Кресло: Инициализация");

        // Устанавливаем координаты для классической L-фигуры (3x2 блока)
        blocks = new Vector2[] {
            new Vector2(0, 0),   // Ножка (нижний левый)
            new Vector2(0, 1),   // Сиденье (средний левый)
            new Vector2(0, 2),   // Спинка (верхний левый)
            new Vector2(1, 0)    // Подлокотник/верх спинки (верхний правый)
        };

        // Создаем кресло из префабов
        CreateChairFromPrefabs();
    }

    // Создание кресла из префабов
    void CreateChairFromPrefabs()
    {
        if (chairSeatPrefab == null || chairBackPrefab == null ||
            chairArmrestPrefab == null || chairLegPrefab == null)
        {
            Debug.LogError("Кресло: Не все префабы назначены!");
            return;
        }

        Debug.Log("Кресло: Создание из префабов");

        ClearAllBlocks();

        // Создаем части кресла на соответствующих позициях L-фигуры
        CreateChairPart(chairLegPrefab, 0, 0);             // Ножка кресла
        CreateChairPart(chairSeatPrefab, 0, 1);            // Сиденье
        CreateChairPart(chairBackPrefab, 0, 2);            // Спинка
        CreateChairPart(chairArmrestPrefab, 1, 0);         // Подлокотник/верх спинки

        UpdateShapeBlocks();
    }

    // Создание одной части кресла
    void CreateChairPart(GameObject chairPartPrefab, float x, float y)
    {
        if (chairPartPrefab == null) return;

        Vector3 partPosition = transform.position + new Vector3(x, y, 0);
        GameObject partInstance = Instantiate(chairPartPrefab, partPosition, transform.rotation);
        partInstance.transform.SetParent(transform);

        Debug.Log($"Кресло: Создана часть {chairPartPrefab.name} на позиции ({x}, {y})");
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
            Debug.Log($"Кресло: Обновлено блоков в системе: {blockList.Count}");
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
        Debug.Log($"ChairItemL: InitializeShape() вызван");
        InitializeChair();
    }
}