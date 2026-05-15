using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;

    void Awake()
    {
        // ≈сли экземпл€р уже существует и это не € Ч уничтожаю себ€
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // ≈сли экземпл€ра нет Ч становлюсь им и остаюсь между сценами
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}