using UnityEngine;

public class BookStackItem : TetrisShape
{
    [Header("Префабы стопки книг")]
    public GameObject bookBottomPrefab;     // Нижняя книга
    public GameObject bookMiddle1Prefab;    // Средняя книга 1
    public GameObject bookMiddle2Prefab;    // Средняя книга 2  
    public GameObject bookTopPrefab;        // Верхняя книга

    void Start()
    {
        Debug.Log($"Стопка книг: Start() вызван");
        InitializeBookStack();
    }

    // Инициализация стопки книг
    void InitializeBookStack()
    {
        Debug.Log("Стопка книг: Инициализация");

        // Устанавливаем координаты для I-фигуры (4 блока в ряд)
        blocks = new Vector2[] {
            new Vector2(0, 0),   // Нижняя книга
            new Vector2(0, 1),   // Средняя книга 1
            new Vector2(0, 2),   // Средняя книга 2
            new Vector2(0, 3)    // Верхняя книга
        };

        // Создаем стопку книг из префабов
        CreateBookStackFromPrefabs();
    }

    // Создание стопки книг из префабов
    void CreateBookStackFromPrefabs()
    {
        if (bookBottomPrefab == null || bookMiddle1Prefab == null ||
            bookMiddle2Prefab == null || bookTopPrefab == null)
        {
            Debug.LogError("Стопка книг: Не все префабы назначены!");
            return;
        }

        Debug.Log("Стопка книг: Создание из префабов");

        ClearAllBlocks();

        // Создаем книги на соответствующих позициях I-фигуры
        CreateBookPart(bookBottomPrefab, 0, 0);    // Нижняя книга
        CreateBookPart(bookMiddle1Prefab, 0, 1);   // Средняя книга 1
        CreateBookPart(bookMiddle2Prefab, 0, 2);   // Средняя книга 2
        CreateBookPart(bookTopPrefab, 0, 3);       // Верхняя книга

        UpdateShapeBlocks();
    }

    // Создание одной книги
    void CreateBookPart(GameObject bookPrefab, float x, float y)
    {
        if (bookPrefab == null) return;

        Vector3 bookPosition = transform.position + new Vector3(x, y, 0);
        GameObject bookInstance = Instantiate(bookPrefab, bookPosition, transform.rotation);
        bookInstance.transform.SetParent(transform);

        Debug.Log($"Стопка книг: Создана книга {bookPrefab.name} на позиции ({x}, {y})");
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
            Debug.Log($"Стопка книг: Обновлено блоков в системе: {blockList.Count}");
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
        Debug.Log($"BookStackItem: InitializeShape() вызван");
        InitializeBookStack();
    }
}