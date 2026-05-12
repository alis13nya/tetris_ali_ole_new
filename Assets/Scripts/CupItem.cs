using UnityEngine;

public class CupItem : TetrisShape
{
    [Header("Префабы кружки")]
    public GameObject normalCupPrefab;

    [Header("Пролитие вправо")]
    public GameObject spilledRight1Prefab;
    public GameObject spilledRight2Prefab;
    public GameObject spilledRight3Prefab;

    [Header("Пролитие влево")]
    public GameObject spilledLeft1Prefab;
    public GameObject spilledLeft2Prefab;
    public GameObject spilledLeft3Prefab;

    private bool isFlipped = false;
    private bool spilledToRight = true;

    void Start()
    {
        Debug.Log($"Кружка: Start() вызван");
        InitializeAsNormalCup();
    }

    void InitializeAsNormalCup()
    {
        Debug.Log("Кружка: Инициализация как обычной кружки");
        isFlipped = false;
        spilledToRight = true;

        blocks = new Vector2[] { new Vector2(0, 0) };
        CreateCupFromPrefab(normalCupPrefab);
    }

    void TransformToSpilledCup(bool spillToRight)
    {
        Debug.Log($"Кружка: Преобразование в пролитую кружку, направление: {(spillToRight ? "вправо" : "влево")}");
        isFlipped = true;
        spilledToRight = spillToRight;

        blocks = new Vector2[] {
            new Vector2(-1, 0),
            new Vector2(0, 0),
            new Vector2(1, 0)
        };

        CreateSpilledCupFromPrefabs(spillToRight);
    }

    void CreateCupFromPrefab(GameObject cupPrefab)
    {
        if (cupPrefab == null)
        {
            Debug.LogError("Кружка: Префаб не назначен!");
            return;
        }

        Debug.Log($"Кружка: Создание из префаба {cupPrefab.name}");

        ClearAllBlocks();

        GameObject cupInstance = Instantiate(cupPrefab, transform.position, transform.rotation);
        cupInstance.transform.SetParent(transform);

        UpdateShapeBlocks();
    }

    void CreateSpilledCupFromPrefabs(bool spillToRight)
    {
        Debug.Log($"Кружка: Создание пролитой кружки из 3 префабов, направление: {(spillToRight ? "вправо" : "влево")}");

        ClearAllBlocks();

        if (spillToRight)
        {
            CreateSpilledBlock(spilledRight1Prefab, -1, 0);
            CreateSpilledBlock(spilledRight2Prefab, 0, 0);
            CreateSpilledBlock(spilledRight3Prefab, 1, 0);
        }
        else
        {
            CreateSpilledBlock(spilledLeft1Prefab, -1, 0);
            CreateSpilledBlock(spilledLeft2Prefab, 0, 0);
            CreateSpilledBlock(spilledLeft3Prefab, 1, 0);
        }

        UpdateShapeBlocks();
    }

    void CreateSpilledBlock(GameObject blockPrefab, float x, float y)
    {
        if (blockPrefab == null) return;

        Vector3 blockPosition = transform.position + new Vector3(x, y, 0);
        GameObject blockInstance = Instantiate(blockPrefab, blockPosition, transform.rotation);
        blockInstance.transform.SetParent(transform);
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

    public void SpillCup(bool rotateRight = true)
    {
        Debug.Log($"Кружка: SpillCup() вызван, isFlipped = {isFlipped}, направление: {(rotateRight ? "вправо" : "влево")}");

        if (!isFlipped)
        {
            TransformToSpilledCup(rotateRight);
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.UnlockAchievement("spill_cup");

            if (!CanMove(Vector2.zero))
            {
                Debug.Log("Кружка: Нельзя разместить пролитую кружку - откат");
                InitializeAsNormalCup();
            }
        }
        else
        {
            Debug.Log("Кружка: Уже пролита, стандартное вращение");
            if (rotateRight)
            {
                transform.Rotate(0, 0, -90);
            }
            else
            {
                transform.Rotate(0, 0, 90);
            }
        }
    }

    // ПЕРЕОПРЕДЕЛЕНИЕ ВРАЩЕНИЯ - ПРОЛИТИЕ
    public override void RotateLeft()
    {
        Debug.Log("CupItem: RotateLeft() - пролитие влево");
        SpillCup(false);  // false = влево
    }

    public override void RotateRight()
    {
        Debug.Log("CupItem: RotateRight() - пролитие вправо");
        SpillCup(true);   // true = вправо
    }

    public new void Move(Vector2 direction)
    {
        base.Move(direction);
        UpdateShapeBlocks();
    }

    // НОВЫЙ МЕТОД ДЛЯ ИНИЦИАЛИЗАЦИИ ПРЕДПРОСМОТРА
    public override void InitializeShape()
    {
        Debug.Log($"CupItem: InitializeShape() вызван");
        InitializeAsNormalCup();
    }
}