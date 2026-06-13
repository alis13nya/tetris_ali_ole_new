using UnityEngine;
using System.Collections.Generic;

public class LampItem : TetrisShape
{
    [Header("Префабы частей лампы")]
    public GameObject basePrefab;      // основание
    public GameObject stemPrefab;      // стойка
    public GameObject shadePrefab;     // плафон (один префаб, у него будем менять спрайт)

    [Header("Спрайты плафона")]
    public Sprite normalSprite;        // обычный (нейтральный)
    public Sprite yellowSprite;        // жёлтый (на столе)
    public Sprite purpleSprite;        // фиолетовый (рядом с растением)

    private GameObject currentShade;
    private SpriteRenderer shadeRenderer;
    private Vector2Int shadeLocalPos;

    public enum LampLightState { Normal, Yellow, Purple }
    private LampLightState currentState = LampLightState.Normal;

    void Start() => InitializeLamp();

    void InitializeLamp()
    {
        // Отзеркаленная Г: основание (0,0), стойка (0,1), плафон слева (-1,1)
        blocks = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, 1) };
        shadeLocalPos = new Vector2Int(-1, 1);
        CreateLampFromPrefabs();
    }

    void CreateLampFromPrefabs()
    {
        if (basePrefab == null || stemPrefab == null || shadePrefab == null)
        { Debug.LogError("LampItem: missing prefabs"); return; }
        ClearAllBlocks();
        CreateLampPart(basePrefab, 0, 0);
        CreateLampPart(stemPrefab, 0, 1);
        currentShade = CreateLampPart(shadePrefab, -1, 1);
        shadeRenderer = currentShade.GetComponent<SpriteRenderer>();
        if (shadeRenderer != null && normalSprite != null)
            shadeRenderer.sprite = normalSprite;
        UpdateShapeBlocks();
    }

    GameObject CreateLampPart(GameObject prefab, float x, float y)
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
        var field = typeof(TetrisShape).GetField("shapeBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, list.ToArray());
    }

    public void SetNormalLight()
    {
        if (currentState != LampLightState.Normal && shadeRenderer != null && normalSprite != null)
        {
            shadeRenderer.sprite = normalSprite;
            currentState = LampLightState.Normal;
            Debug.Log("[LampItem] Нормальный свет");
        }
    }

    public void SetYellowLight()
    {
        if (currentState != LampLightState.Yellow && shadeRenderer != null && yellowSprite != null)
        {
            shadeRenderer.sprite = yellowSprite;
            currentState = LampLightState.Yellow;
            Debug.Log("[LampItem] Жёлтый свет");
        }
    }

    public void SetPurpleLight()
    {
        if (currentState != LampLightState.Purple && shadeRenderer != null && purpleSprite != null)
        {
            shadeRenderer.sprite = purpleSprite;
            currentState = LampLightState.Purple;
            Debug.Log("[LampItem] Фиолетовый свет");
        }
    }

    public new void Move(Vector2 dir) { base.Move(dir); UpdateShapeBlocks(); }
    public override void InitializeShape() => InitializeLamp();
    public override string GetShapeTypeName() => "LampItem";
}