using UnityEngine;

public class PencilCupItem : TetrisShape
{
    [Header("Префабы стакана с карандашами")]
    public GameObject pencilCupBottomPrefab;
    public GameObject pencilCupTopPrefab;

    [Header("Рассыпание вправо")]
    public GameObject spilledRight1Prefab;
    public GameObject spilledRight2Prefab;
    public GameObject spilledRight3Prefab;

    [Header("Рассыпание влево")]
    public GameObject spilledLeft1Prefab;
    public GameObject spilledLeft2Prefab;
    public GameObject spilledLeft3Prefab;

    private bool isSpilled = false;
    private bool spilledToRight = true;

    void Start()
    {
        Debug.Log($"Стакан с карандашами: Start() вызван");
        InitializeAsCup();
    }

    void InitializeAsCup()
    {
        Debug.Log("Стакан с карандашами: Инициализация как стакана");
        isSpilled = false;
        spilledToRight = true;

        blocks = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 1)
        };

        CreateCupFromPrefabs();
    }

    void TransformToSpilledPencils(bool spillToRight)
    {
        Debug.Log($"Стакан с карандашами: Преобразование в рассыпанные карандаши, направление: {(spillToRight ? "вправо" : "влево")}");
        isSpilled = true;
        spilledToRight = spillToRight;

        blocks = new Vector2[] {
            new Vector2(-1, 0),
            new Vector2(0, 0),
            new Vector2(1, 0)
        };

        CreateSpilledPencilsFromPrefabs(spillToRight);
    }

    void CreateCupFromPrefabs()
    {
        Debug.Log("Стакан с карандашами: Создание стакана из 2 префабов");

        ClearAllBlocks();

        CreatePencilPart(pencilCupBottomPrefab, 0, 0);
        CreatePencilPart(pencilCupTopPrefab, 0, 1);

        UpdateShapeBlocks();
    }

    void CreateSpilledPencilsFromPrefabs(bool spillToRight)
    {
        Debug.Log($"Стакан с карандашами: Создание рассыпанных карандашей из 3 префабов, направление: {(spillToRight ? "вправо" : "влево")}");

        ClearAllBlocks();

        if (spillToRight)
        {
            CreatePencilPart(spilledRight1Prefab, -1, 0);
            CreatePencilPart(spilledRight2Prefab, 0, 0);
            CreatePencilPart(spilledRight3Prefab, 1, 0);
        }
        else
        {
            CreatePencilPart(spilledLeft1Prefab, -1, 0);
            CreatePencilPart(spilledLeft2Prefab, 0, 0);
            CreatePencilPart(spilledLeft3Prefab, 1, 0);
        }

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

    public void SpillPencils(bool rotateRight = true)
    {
        Debug.Log($"Стакан с карандашами: SpillPencils() вызван, isSpilled = {isSpilled}, направление: {(rotateRight ? "вправо" : "влево")}");

        if (!isSpilled)
        {
            TransformToSpilledPencils(rotateRight);

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.UnlockAchievement("spill_pencils");

            if (!CanMove(Vector2.zero))
            {
                Debug.Log("Стакан с карандашами: Нельзя разместить рассыпанные карандаши - откат");
                InitializeAsCup();
            }
        }
        else
        {
            Debug.Log("Стакан с карандашами: Уже рассыпаны, стандартное вращение");
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

    // ПЕРЕОПРЕДЕЛЕНИЕ ВРАЩЕНИЯ - РАССЫПАНИЕ
    public override void RotateLeft()
    {
        Debug.Log("PencilCupItem: RotateLeft() - рассыпание влево");
        SpillPencils(false);  // false = влево
    }

    public override void RotateRight()
    {
        Debug.Log("PencilCupItem: RotateRight() - рассыпание вправо");
        SpillPencils(true);   // true = вправо
    }

    public new void Move(Vector2 direction)
    {
        base.Move(direction);
        UpdateShapeBlocks();
    }

    // НОВЫЙ МЕТОД ДЛЯ ИНИЦИАЛИЗАЦИИ ПРЕДПРОСМОТРА
    public override void InitializeShape()
    {
        Debug.Log($"PencilCupItem: InitializeShape() вызван");
        InitializeAsCup();
    }
}