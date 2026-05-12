using UnityEngine;

public class PerfectGrid : MonoBehaviour
{
    public GameObject blockPrefab;

    void Start()
    {
        // янгдюел йнохч опетюаю дкъ рнвмшу пюглепнб
        GameObject sampleBlock = Instantiate(blockPrefab);
        float blockSize = 1f; // аЮГНБШИ ПЮГЛЕП

        // смхврнфюел йнохч
        Destroy(sampleBlock);

        // янгдюел хдеюкэмсч яерйс
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                GameObject block = Instantiate(blockPrefab);

                // рнвмюъ онгхжхъ аег деяърхвмшу онцпеьмняреи
                block.transform.position = new Vector3(
                    (float)x * blockSize,
                    (float)y * blockSize,
                    0f
                );

                // юаянкчрмши люяьрюа
                block.transform.localScale = Vector3.one;

                // вхярюъ хепюпухъ
                block.transform.SetParent(transform, false);
            }
        }

        Debug.Log("хдеюкэмюъ яерйю янгдюмю!");
    }
}