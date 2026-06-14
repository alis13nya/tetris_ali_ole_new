using UnityEngine;

public class LoosePencilsItem : TetrisShape
{
    [Header("Префабы карандашей")]
    public GameObject pencil1Prefab;
    public GameObject pencil2Prefab;

    void Start() => InitializeShape();

    public override void InitializeShape()
    {
        blocks = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1) };
        CreatePencilsFromPrefabs();
        // Поворачиваем фигуру, чтобы грифель был внизу (неправильно)
        transform.Rotate(0, 0, 180);
    }

    void CreatePencilsFromPrefabs()
    {
        if (pencil1Prefab == null || pencil2Prefab == null)
        {
            Debug.LogError("Карандаши: Не все префабы назначены!");
            return;
        }
        ClearAllBlocks();
        CreatePencilPart(pencil1Prefab, 0, 0);
        CreatePencilPart(pencil2Prefab, 0, 1);
        UpdateShapeBlocks();
    }

    GameObject CreatePencilPart(GameObject prefab, float x, float y)
    {
        Vector3 pos = transform.position + new Vector3(x, y, 0);
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
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
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform)
            if (child != transform && child.gameObject != null) list.Add(child.gameObject);
        var field = typeof(TetrisShape).GetField("shapeBlocks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, list.ToArray());
    }

    public new void Move(Vector2 dir) { base.Move(dir); UpdateShapeBlocks(); }

    // Грифель вверху (правильно для вставки)
    public bool IsUpright
    {
        get
        {
            float angle = transform.rotation.eulerAngles.z % 360f;
            return Mathf.Abs(angle) < 5f;
        }
    }

    // Любая вертикальная ориентация (0° или 180°)
    public bool IsVerticalOrientation
    {
        get
        {
            float angle = transform.rotation.eulerAngles.z % 360f;
            return Mathf.Abs(angle) < 5f || Mathf.Abs(angle - 180f) < 5f;
        }
    }
}