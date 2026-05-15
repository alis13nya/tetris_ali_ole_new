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
        Debug.Log("CupItem: RotateLeft() - попытка пролития влево");

        if (isFlipped)
        {
            // Уже пролита — обычное вращение
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;

            transform.Rotate(0, 0, 90);

            if (CanMove(Vector2.zero)) return;

            Vector2[] kickOffsets = new Vector2[]
            {
            Vector2.left, Vector2.right, Vector2.up,
            new Vector2(-1, -1), new Vector2(1, -1),
            new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, 1)
            };

            foreach (Vector2 offset in kickOffsets)
            {
                transform.position += new Vector3(offset.x, offset.y, 0);
                if (CanMove(Vector2.zero)) return;
                transform.position -= new Vector3(offset.x, offset.y, 0);
            }

            transform.position = oldPosition;
            transform.rotation = oldRotation;
            return;
        }

        // Ещё не пролита — нужно пролить влево
        // Сначала симулируем пролитие, чтобы понять, как будет выглядеть фигура
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        // Временно превращаем в пролитую (без проверок)
        TransformToSpilledCup(false);

        // Проверяем, помещается ли с разными смещениями
        Vector2[] wallKicks = new Vector2[]
        {
        Vector2.zero,
        Vector2.left,
        Vector2.right,
        Vector2.up,
        new Vector2(-1, -1),
        new Vector2(1, -1),
        new Vector2(-2, 0),
        new Vector2(2, 0),
        new Vector2(0, 1)
        };

        bool success = false;
        Vector2 successfulOffset = Vector2.zero;

        foreach (Vector2 offset in wallKicks)
        {
            transform.position = oldPos + new Vector3(offset.x, offset.y, 0);

            if (CanMove(Vector2.zero))
            {
                success = true;
                successfulOffset = offset;
                break;
            }
        }

        if (success)
        {
            // Применяем пролитие с найденным смещением
            // Возвращаемся к нормальной кружке
            InitializeAsNormalCup();
            transform.position = oldPos;
            transform.rotation = oldRot;

            // Теперь正式 проливаем со смещением
            TransformToSpilledCup(false);
            transform.position = oldPos + new Vector3(successfulOffset.x, successfulOffset.y, 0);

            Debug.Log($"CupItem: пролита влево со смещением {successfulOffset}");

            // Отмечаем достижение
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.UnlockAchievement("spill_cup");
        }
        else
        {
            // Не помещается — откатываем
            InitializeAsNormalCup();
            transform.position = oldPos;
            transform.rotation = oldRot;
            Debug.Log("CupItem: пролитие влево невозможно");
        }
    }

    public override void RotateRight()
    {
        Debug.Log("CupItem: RotateRight() - попытка пролития вправо");

        if (isFlipped)
        {
            // Уже пролита — обычное вращение
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;

            transform.Rotate(0, 0, -90);

            if (CanMove(Vector2.zero)) return;

            Vector2[] kickOffsets = new Vector2[]
            {
            Vector2.left, Vector2.right, Vector2.up,
            new Vector2(-1, -1), new Vector2(1, -1),
            new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, 1)
            };

            foreach (Vector2 offset in kickOffsets)
            {
                transform.position += new Vector3(offset.x, offset.y, 0);
                if (CanMove(Vector2.zero)) return;
                transform.position -= new Vector3(offset.x, offset.y, 0);
            }

            transform.position = oldPosition;
            transform.rotation = oldRotation;
            return;
        }

        // Ещё не пролита — нужно пролить вправо
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        TransformToSpilledCup(true);

        Vector2[] wallKicks = new Vector2[]
        {
        Vector2.zero,
        Vector2.left,
        Vector2.right,
        Vector2.up,
        new Vector2(-1, -1),
        new Vector2(1, -1),
        new Vector2(-2, 0),
        new Vector2(2, 0),
        new Vector2(0, 1)
        };

        bool success = false;
        Vector2 successfulOffset = Vector2.zero;

        foreach (Vector2 offset in wallKicks)
        {
            transform.position = oldPos + new Vector3(offset.x, offset.y, 0);

            if (CanMove(Vector2.zero))
            {
                success = true;
                successfulOffset = offset;
                break;
            }
        }

        if (success)
        {
            InitializeAsNormalCup();
            transform.position = oldPos;
            transform.rotation = oldRot;

            TransformToSpilledCup(true);
            transform.position = oldPos + new Vector3(successfulOffset.x, successfulOffset.y, 0);

            Debug.Log($"CupItem: пролита вправо со смещением {successfulOffset}");

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.UnlockAchievement("spill_cup");
        }
        else
        {
            InitializeAsNormalCup();
            transform.position = oldPos;
            transform.rotation = oldRot;
            Debug.Log("CupItem: пролитие вправо невозможно");
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
        Debug.Log($"CupItem: InitializeShape() вызван");
        InitializeAsNormalCup();
    }
}