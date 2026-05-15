using UnityEngine;

public class TetrisShape : MonoBehaviour
{
    [Header("Настройки фигуры")]
    public Vector2[] blocks;
    public Color shapeColor = Color.white;

    [Header("Ссылки")]
    public GameObject blockPrefab;
    public FieldGrid GetFieldGrid()
    {
        return fieldGrid;
    }
    private GameObject[] shapeBlocks;
    private FieldGrid fieldGrid;
    private Vector2 currentScale = Vector2.one;

    void Start()
    {
        fieldGrid = FindObjectOfType<FieldGrid>();
        InitializeShape();
    }

    private void CreateShape()
    {
        if (blocks == null || blocks.Length == 0)
        {
            Debug.LogError($"TetrisShape {name}: blocks массив пуст!");
            return;
        }

        shapeBlocks = new GameObject[blocks.Length];

        for (int i = 0; i < blocks.Length; i++)
        {
            GameObject block = Instantiate(blockPrefab);

            Vector3 localPosition = new Vector3(
                blocks[i].x * currentScale.x,
                blocks[i].y * currentScale.y,
                -1f
            );

            block.transform.SetParent(transform);
            block.transform.localPosition = localPosition;

            SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = shapeColor;
            }

            shapeBlocks[i] = block;
        }

        ApplyScale();
        Debug.Log($"TetrisShape {name}: Создано {shapeBlocks.Length} блоков");
    }

    public void ForceCreateShape()
    {
        CreateShape();
    }

    public void SetFieldGrid(FieldGrid grid)
    {
        fieldGrid = grid;
    }

    public void UpdateShapeScale(Vector2 newScale)
    {
        if (currentScale == newScale) return;

        currentScale = newScale;
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (shapeBlocks == null) return;

        transform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);

        for (int i = 0; i < shapeBlocks.Length; i++)
        {
            if (shapeBlocks[i] != null)
            {
                Vector3 localPosition = new Vector3(
                    blocks[i].x,
                    blocks[i].y,
                    -1f
                );

                shapeBlocks[i].transform.localPosition = localPosition;
                shapeBlocks[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void Move(Vector2 direction)
    {
        if (CanMove(direction))
        {
            Vector2 scaledDirection = new Vector2(
                direction.x * currentScale.x,
                direction.y * currentScale.y
            );

            transform.position += new Vector3(scaledDirection.x, scaledDirection.y, 0);
        }
    }

    public virtual void Rotate()
    {
        // Сохраняем исходное состояние
        Vector3 oldPosition = transform.position;
        Quaternion oldRotation = transform.rotation;

        // Пробуем повернуть
        transform.Rotate(0, 0, 90);

        // Проверяем, помещается ли фигура после поворота
        if (CanMove(Vector2.zero))
        {
            // Всё хорошо, оставляем
            return;
        }

        // Пробуем варианты смещения (wall kicks)
        Vector2[] kickOffsets = new Vector2[]
        {
        Vector2.left,           // смещение влево
        Vector2.right,          // смещение вправо
        Vector2.up,             // смещение вверх
        new Vector2(-1, -1),    // влево-вниз
        new Vector2(1, -1),     // вправо-вниз
        new Vector2(-2, 0),     // на две клетки влево
        new Vector2(2, 0),      // на две клетки вправо
        new Vector2(0, 1),      // вверх
        };

        foreach (Vector2 offset in kickOffsets)
        {
            // Смещаем фигуру
            transform.position += new Vector3(offset.x, offset.y, 0);

            if (CanMove(Vector2.zero))
            {
                // Нашли подходящее смещение — оставляем
                return;
            }

            // Возвращаем обратно
            transform.position -= new Vector3(offset.x, offset.y, 0);
        }

        // Ни одно смещение не подошло — откатываем вращение
        transform.position = oldPosition;
        transform.rotation = oldRotation;
    }

    public virtual void RotateLeft()
    {
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
    }

    public virtual void RotateRight()
    {
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
    }

    public bool CanMove(Vector2 direction)
    {
        if (fieldGrid == null)
        {
            Debug.LogError("FieldGrid is NULL!");
            return false;
        }

        Vector3 originalPosition = transform.position;

        Vector2 scaledDirection = new Vector2(
            direction.x * currentScale.x,
            direction.y * currentScale.y
        );

        transform.position += new Vector3(scaledDirection.x, scaledDirection.y, 0);
        bool isValid = fieldGrid.IsValidPosition(this);

        transform.position = originalPosition;

        return isValid;
    }

    public void HardDrop()
    {
        while (CanMove(Vector2.down))
        {
            Move(Vector2.down);
        }
    }

    public Vector3[] GetBlockWorldPositions()
    {
        if (shapeBlocks == null) return new Vector3[0];

        Vector3[] positions = new Vector3[shapeBlocks.Length];
        for (int i = 0; i < shapeBlocks.Length; i++)
        {
            if (shapeBlocks[i] != null)
            {
                positions[i] = shapeBlocks[i].transform.position;
            }
        }
        return positions;
    }

    public Bounds GetShapeBounds()
    {
        if (shapeBlocks == null || shapeBlocks.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds bounds = new Bounds(shapeBlocks[0].transform.position, Vector3.zero);
        for (int i = 1; i < shapeBlocks.Length; i++)
        {
            if (shapeBlocks[i] != null)
            {
                bounds.Encapsulate(shapeBlocks[i].transform.position);
            }
        }
        return bounds;
    }

    public bool IsSpecialItem()
    {
        return (this is CupItem || this is PencilCupItem ||
                this is TableItem || this is ComputerItem ||
                this is BookStackItem);
    }

    public void SetBlocksAlpha(float alpha)
    {
        if (shapeBlocks == null) return;

        foreach (GameObject block in shapeBlocks)
        {
            if (block != null)
            {
                SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = alpha;
                    renderer.color = color;
                }
            }
        }
    }

    public void ResetBlocksAlpha()
    {
        SetBlocksAlpha(1f);
    }

    public void SetBlocksColor(Color color)
    {
        if (shapeBlocks == null) return;

        foreach (GameObject block in shapeBlocks)
        {
            if (block != null)
            {
                SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = color;
                }
            }
        }
    }

    public void ResetBlocksColor()
    {
        if (shapeBlocks == null) return;

        foreach (GameObject block in shapeBlocks)
        {
            if (block != null)
            {
                SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = shapeColor;
                }
            }
        }
    }

    public virtual void InitializeShape()
    {
        Debug.Log($"TetrisShape: InitializeShape() вызван для {gameObject.name}");

        if (shapeBlocks == null || shapeBlocks.Length == 0)
        {
            CreateShape();
        }
    }

    public virtual string GetShapeTypeName()
    {
        if (this is BookStackItem) return "BookStackItem";
        if (this is ChairItemJ) return "ChairItemJ";
        if (this is ChairItemL) return "ChairItemL";
        if (this is ComputerItem) return "ComputerItem";
        if (this is CupItem) return "CupItem";
        if (this is PencilCupItem) return "PencilCupItem";
        if (this is TableItem) return "TableItem";
        if (this is EmptyPencilCupItem) return "EmptyPencilCupItem";
        if (this is LoosePencilsItem) return "LoosePencilsItem";
        if (this is EmptyCupItem) return "EmptyCupItem"; 

        return null;
    }
}