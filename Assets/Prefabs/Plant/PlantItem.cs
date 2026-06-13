using UnityEngine;
using System.Collections.Generic;

public class PlantItem : TetrisShape
{
    [Header("Префабы частей растения (Z-форма)")]
    public GameObject part1Prefab; // 0,1
    public GameObject part2Prefab; // 1,1
    public GameObject part3Prefab; // 1,0
    public GameObject part4Prefab; // 2,0

    void Start() => InitializePlant();

    void InitializePlant()
    {
        blocks = new Vector2[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(2, 0) };
        CreatePlantFromPrefabs();
    }

    void CreatePlantFromPrefabs()
    {
        if (part1Prefab == null || part2Prefab == null || part3Prefab == null || part4Prefab == null)
        { Debug.LogError("PlantItem: missing prefabs"); return; }
        ClearAllBlocks();
        CreatePlantPart(part1Prefab, blocks[0].x, blocks[0].y);
        CreatePlantPart(part2Prefab, blocks[1].x, blocks[1].y);
        CreatePlantPart(part3Prefab, blocks[2].x, blocks[2].y);
        CreatePlantPart(part4Prefab, blocks[3].x, blocks[3].y);
        UpdateShapeBlocks();
    }

    GameObject CreatePlantPart(GameObject prefab, float x, float y)
    {
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
        var field = typeof(TetrisShape).GetField("shapeBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, list.ToArray());
    }

    public new void Move(Vector2 dir) { base.Move(dir); UpdateShapeBlocks(); }
    public override void InitializeShape() => InitializePlant();
    public override string GetShapeTypeName() => "PlantItem";
}