using UnityEngine;

public class LoosePencilsItem : TetrisShape
{
    [Header("Префабы карандашей")]
    public GameObject pencil1Prefab;  // Нижний карандаш
    public GameObject pencil2Prefab;  // Верхний карандаш

    void Start()
    {
        Debug.Log($"Карандаши: Start() вызван");
        InitializeShape();
    }

    public override void InitializeShape()
    {
        Debug.Log("Карандаши: Инициализация");

        blocks = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 1)
        };

        CreatePencilsFromPrefabs();
    }

    void CreatePencilsFromPrefabs()
    {
        if (pencil1Prefab == null || pencil2Prefab == null)
        {
            Debug.LogError("Карандаши: Не все префабы назначены!");
            return;
        }

        Debug.Log("Карандаши: Создание из префабов");

        ClearAllBlocks();

        CreatePencilPart(pencil1Prefab, 0, 0);
        CreatePencilPart(pencil2Prefab, 0, 1);

        UpdateShapeBlocks();
    }

    void CreatePencilPart(GameObject pencilPrefab, float x, float y)
    {
        if (pencilPrefab == null) return;

        Vector3 pencilPosition = transform.position + new Vector3(x, y, 0);
        GameObject pencilInstance = Instantiate(pencilPrefab, pencilPosition, transform.rotation);
        pencilInstance.transform.SetParent(transform);
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

    // УДАЛЕН переопределенный метод Rotate - используем стандартный из TetrisShape
    // Теперь карандаши вращаются как все фигуры

    // НОВОЕ: Свойство для проверки вертикальной ориентации
    public bool IsVerticalOrientation
    {
        get
        {
            // Проверяем угол вращения
            float angle = Mathf.Abs(transform.rotation.eulerAngles.z % 360f);

            // Допуск 15 градусов для удобства игры
            const float tolerance = 15f;

            // Проверяем вертикальные ориентации:
            // 0° (изначальное положение) или 180° (верх ногами)
            bool isVertical = (angle <= tolerance ||
                              (angle >= 180f - tolerance && angle <= 180f + tolerance) ||
                              angle >= 360f - tolerance);

            Debug.Log($"Карандаши: угол вращения = {angle:F1}°, вертикальны = {isVertical}");
            return isVertical;
        }
    }
}