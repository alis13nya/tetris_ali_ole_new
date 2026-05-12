using UnityEngine;

public class EmptyCupItem : TetrisShape
{
    [Header("Префабы пустой кружки")]
    public GameObject emptyCupPrefab;

    void Start()
    {
        Debug.Log($"Пустая кружка: Start() вызван");
        InitializeShape();
    }

    public override void InitializeShape()
    {
        Debug.Log("Пустая кружка: Инициализация");

        blocks = new Vector2[] { new Vector2(0, 0) };
        CreateEmptyCup();
    }

    void CreateEmptyCup()
    {
        if (emptyCupPrefab == null)
        {
            Debug.LogError("Пустая кружка: Префаб не назначен!");
            return;
        }

        Debug.Log($"Пустая кружка: Создание из префаба {emptyCupPrefab.name}");

        ClearAllBlocks();

        GameObject cupInstance = Instantiate(emptyCupPrefab, transform.position, transform.rotation);
        cupInstance.transform.SetParent(transform);
        cupInstance.transform.localPosition = Vector3.zero;

        UpdateShapeBlocks();
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