using UnityEngine;

public class EmptyPencilCupItem : TetrisShape
{
    [Header("Префабы пустого стакана")]
    public GameObject emptyCupPrefab;

    [Header("Префабы стакана с карандашами")]
    public GameObject pencilCupBottomPrefab;
    public GameObject pencilCupTopPrefab;

    [Header("Состояние")]
    private bool hasPencils = false; // Есть ли карандаши в стакане

    void Start()
    {
        Debug.Log($"Пустой стакан: Start() вызван");
        InitializeShape();
    }

    public override void InitializeShape()
    {
        Debug.Log("Пустой стакан: Инициализация");

        // Один блок для пустого стакана
        blocks = new Vector2[] { new Vector2(0, 0) };

        if (hasPencils)
        {
            // Если уже имеет карандаши (после перезагрузки)
            CreatePencilCup();
        }
        else
        {
            // Обычный пустой стакан
            CreateEmptyCup();
        }
    }

    void CreateEmptyCup()
    {
        if (emptyCupPrefab == null)
        {
            Debug.LogError("Пустой стакан: Префаб пустого стакана не назначен!");
            return;
        }

        Debug.Log($"Пустой стакан: Создание пустого стакана");

        ClearAllBlocks();

        GameObject cupInstance = Instantiate(emptyCupPrefab, transform.position, transform.rotation);
        cupInstance.transform.SetParent(transform);
        cupInstance.transform.localPosition = Vector3.zero;

        UpdateShapeBlocks();
    }

    // НОВЫЙ МЕТОД: Создание стакана с карандашами
    void CreatePencilCup()
    {
        if (pencilCupBottomPrefab == null || pencilCupTopPrefab == null)
        {
            Debug.LogError("Пустой стакан: Не все префабы для стакана с карандашами назначены!");
            return;
        }

        Debug.Log("Пустой стакан: Создание стакана с карандашами");

        ClearAllBlocks();

        // Создаем нижнюю часть стакана
        GameObject bottomPart = Instantiate(pencilCupBottomPrefab, transform.position, transform.rotation);
        bottomPart.transform.SetParent(transform);
        bottomPart.transform.localPosition = new Vector3(0, 0, 0);

        // Создаем верхнюю часть (карандаши)
        GameObject topPart = Instantiate(pencilCupTopPrefab, transform.position, transform.rotation);
        topPart.transform.SetParent(transform);
        topPart.transform.localPosition = new Vector3(0, 1, 0); // На одну клетку выше

        UpdateShapeBlocks();

        Debug.Log("Пустой стакан теперь выглядит как стакан с карандашами");
    }

    // Метод для превращения в стакан с карандашами
    public void TransformToPencilCup()
    {
        Debug.Log("Пустой стакан: Превращение в стакан с карандашами");
        hasPencils = true;

        // Обновляем блоки: теперь это 2 блока
        blocks = new Vector2[] {
            new Vector2(0, 0),  // Нижняя часть
            new Vector2(0, 1)   // Верхняя часть (карандаши)
        };

        CreatePencilCup();
    }

    // Свойство для проверки состояния
    public bool HasPencils
    {
        get { return hasPencils; }
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
        }
    }

    public new void Move(Vector2 direction)
    {
        base.Move(direction);
        UpdateShapeBlocks();
    }
}