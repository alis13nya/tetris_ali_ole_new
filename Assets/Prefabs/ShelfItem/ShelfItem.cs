using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Полка - горизонтальная фигура из трёх блоков.
/// Папки с бумагами, поставленные сверху, заполняют всю полку сразу.
/// </summary>
public class ShelfItem : TetrisShape
{
    [Header("Префабы частей полки")]
    public GameObject leftPartPrefab;    // Левая часть
    public GameObject middlePartPrefab;  // Средняя часть
    public GameObject rightPartPrefab;   // Правая часть

    [Header("Спрайты для заполнения (когда есть папки)")]
    public Sprite leftWithFolderSprite;    // Левая часть с папкой
    public Sprite middleWithFolderSprite;  // Средняя часть с папкой
    public Sprite rightWithFolderSprite;   // Правая часть с папкой

    private GameObject[] shelfParts;       // Массив созданных частей полки
    private SpriteRenderer[] partRenderers; // Рендеры для смены спрайтов
    public bool isFilled = false; // Заполнена ли полка полностью

    void Start() => InitializeShelf();

    void Update()
    {
        // Если полка перевёрнута на 180°, возвращаем в нормальное положение
        float angle = transform.rotation.eulerAngles.z % 360f;
        if (Mathf.Abs(angle - 180f) < 1f)
        {
            transform.rotation = Quaternion.identity;
            UpdateShapeBlocks();
            Debug.Log("Полка возвращена в нормальное положение (0°)");
        }
    }

    void InitializeShelf()
    {
        // Три блока по горизонтали: (-1,0), (0,0), (1,0)
        blocks = new Vector2[]
        {
            new Vector2(-1, 0),   // левая часть
            new Vector2(0, 0),    // средняя часть
            new Vector2(1, 0)     // правая часть
        };
        CreateShelfFromPrefabs();
    }

    void CreateShelfFromPrefabs()
    {
        if (leftPartPrefab == null || middlePartPrefab == null || rightPartPrefab == null)
        {
            Debug.LogError("ShelfItem: Не все префабы назначены!");
            return;
        }

        ClearAllBlocks();

        shelfParts = new GameObject[3];
        partRenderers = new SpriteRenderer[3];

        shelfParts[0] = CreateShelfPart(leftPartPrefab, -1, 0);
        shelfParts[1] = CreateShelfPart(middlePartPrefab, 0, 0);
        shelfParts[2] = CreateShelfPart(rightPartPrefab, 1, 0);

        for (int i = 0; i < 3; i++)
        {
            if (shelfParts[i] != null)
            {
                partRenderers[i] = shelfParts[i].GetComponent<SpriteRenderer>();
            }
        }
        isFilled = false;

        UpdateShapeBlocks();
    }

    GameObject CreateShelfPart(GameObject prefab, float x, float y)
    {
        if (prefab == null) return null;
        Vector3 pos = transform.position + new Vector3(x, y, 0);
        GameObject obj = Instantiate(prefab, pos, transform.rotation);
        obj.transform.SetParent(transform);
        return obj;
    }

    void ClearAllBlocks()
    {
        foreach (Transform child in transform)
            if (child != transform) Destroy(child.gameObject);
    }

    void UpdateShapeBlocks()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Transform child in transform)
            if (child != transform && child.gameObject != null) list.Add(child.gameObject);
        var field = typeof(TetrisShape).GetField("shapeBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, list.ToArray());
    }

    /// <summary>
    /// Положить папки на полку - заполняет ВСЮ полку (все 3 спрайта меняются)
    /// </summary>
    public void FillShelf()
    {
        if (isFilled) return;

        isFilled = true;

        // Меняем все три спрайта
        if (leftWithFolderSprite != null && partRenderers[0] != null)
            partRenderers[0].sprite = leftWithFolderSprite;
        if (middleWithFolderSprite != null && partRenderers[1] != null)
            partRenderers[1].sprite = middleWithFolderSprite;
        if (rightWithFolderSprite != null && partRenderers[2] != null)
            partRenderers[2].sprite = rightWithFolderSprite;

        Debug.Log($"Полка заполнена папками!");
    }

    /// <summary>
    /// Проверка, является ли полка горизонтальной (только 0°)
    /// </summary>
    public bool IsHorizontal()
    {
        float angle = transform.rotation.eulerAngles.z % 360f;
        return Mathf.Abs(angle) < 1f;
    }

    public new void Move(Vector2 dir) { base.Move(dir); UpdateShapeBlocks(); }
    public override void InitializeShape() => InitializeShelf();
    public override string GetShapeTypeName() => "ShelfItem";
}