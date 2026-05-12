using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimplePowerOrbsManager : MonoBehaviour
{
    [Header("Настройки кружков")]
    public int maxOrbs = 3;
    public float fillPerOrb = 0.333f;

    [Header("Ссылки на кружки")]
    public List<OrbSimpleUI> orbUIs;

    [Header("Ссылки")]
    public PowerScaleManager powerScaleManager;
    public FieldGrid fieldGrid;

    [Header("Визуальные эффекты")]
    public ParticleSystem orbFillEffect;
    public ParticleSystem orbUseEffect;
    public AudioClip orbFillSound;
    public AudioClip orbUseSound;

    // Текущее состояние
    private int currentFilledOrbs = 0;
    private AudioSource audioSource;

    [System.Serializable]
    public class OrbSimpleUI
    {
        public Button orbButton;           // Кнопка для активации
        public Image fillImage;            // Fill Image (заполненное состояние)
        public GameObject highlightEffect; // Эффект подсветки (опционально)
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Инициализация UI
        InitializeOrbsUI();
        Debug.Log($"Инициализировано {orbUIs.Count} кружков");
    }

    void InitializeOrbsUI()
    {
        for (int i = 0; i < orbUIs.Count; i++)
        {
            int orbIndex = i;

            if (orbUIs[i].orbButton != null)
            {
                orbUIs[i].orbButton.onClick.AddListener(() => UseOrb(orbIndex));
                orbUIs[i].orbButton.interactable = false;
            }

            // Скрываем заполненные состояния изначально
            if (orbUIs[i].fillImage != null)
            {
                orbUIs[i].fillImage.gameObject.SetActive(false);
            }

            // Скрываем эффекты подсветки
            if (orbUIs[i].highlightEffect != null)
            {
                orbUIs[i].highlightEffect.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (powerScaleManager == null)
        {
            Debug.LogError("PowerScaleManager не назначен!");
            return;
        }

        // Отслеживаем изменения шкалы
        float currentPower = powerScaleManager.currentFillAmount;

        // Если у нас уже максимальное количество кружков, сбрасываем шкалу
        if (currentFilledOrbs >= maxOrbs)
        {
            powerScaleManager.SetFillAmount(0f);
            return;
        }

        // Проверяем, можно ли добавить новый кружок
        if (currentPower >= fillPerOrb && currentFilledOrbs < maxOrbs)
        {
            AddFilledOrb();
            // После добавления кружка сбрасываем шкалу до 0
            powerScaleManager.SetFillAmount(0f);
        }
    }

    // Метод для получения текущего количества заполненных кружков
    public int GetFilledOrbsCount()
    {
        return currentFilledOrbs;
    }

    void AddFilledOrb()
    {
        if (currentFilledOrbs >= orbUIs.Count)
        {
            Debug.LogWarning($"Нельзя добавить кружок #{currentFilledOrbs}, максимально {orbUIs.Count}");
            return;
        }

        Debug.Log($"Добавляем заполненный кружок #{currentFilledOrbs}");

        // Активируем заполненное состояние
        if (orbUIs[currentFilledOrbs].fillImage != null)
        {
            orbUIs[currentFilledOrbs].fillImage.gameObject.SetActive(true);
            Debug.Log($"Активирован FillImage для кружка {currentFilledOrbs}");
        }
        else
        {
            Debug.LogError($"FillImage не назначен для кружка {currentFilledOrbs}!");
        }

        // Активируем кнопку
        if (orbUIs[currentFilledOrbs].orbButton != null)
        {
            orbUIs[currentFilledOrbs].orbButton.interactable = true;
            Debug.Log($"Активирована кнопка для кружка {currentFilledOrbs}");
        }
        else
        {
            Debug.LogError($"OrbButton не назначен для кружка {currentFilledOrbs}!");
        }

        // Включаем эффект подсветки
        if (orbUIs[currentFilledOrbs].highlightEffect != null)
        {
            orbUIs[currentFilledOrbs].highlightEffect.SetActive(true);
        }

        currentFilledOrbs++;

        // Визуальные эффекты
        if (orbFillEffect != null)
        {
            orbFillEffect.Play();
        }

        if (orbFillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(orbFillSound);
        }

        Debug.Log($"Добавлен заполненный кружок. Всего: {currentFilledOrbs}/{maxOrbs}");
    }

    public void UseOrb(int orbIndex)
    {
        Debug.Log($"Попытка использовать кружок {orbIndex}");

        // Проверяем, можно ли использовать этот кружок
        if (orbIndex >= currentFilledOrbs || orbIndex >= orbUIs.Count)
        {
            Debug.LogWarning($"Кружок {orbIndex} не заполнен или не существует. Заполнено: {currentFilledOrbs}");
            return;
        }

        Debug.Log($"Используем кружок {orbIndex}");

        // Используем способность
        RemoveBottomLine();

        // Опустошаем кружок
        EmptyOrb(orbIndex);

        // Сдвигаем остальные кружки, если нужно
        ReorderOrbs();

        // Визуальные эффекты
        if (orbUseEffect != null)
        {
            orbUseEffect.Play();
        }

        if (orbUseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(orbUseSound);
        }

        Debug.Log($"Использован кружок {orbIndex}");
    }

    void EmptyOrb(int orbIndex)
    {
        if (orbIndex >= orbUIs.Count) return;

        Debug.Log($"Опустошаем кружок {orbIndex}");

        // Скрываем заполненное состояние
        if (orbUIs[orbIndex].fillImage != null)
        {
            orbUIs[orbIndex].fillImage.gameObject.SetActive(false);
        }

        // Деактивируем кнопку
        if (orbUIs[orbIndex].orbButton != null)
        {
            orbUIs[orbIndex].orbButton.interactable = false;
        }

        // Скрываем эффект подсветки
        if (orbUIs[orbIndex].highlightEffect != null)
        {
            orbUIs[orbIndex].highlightEffect.SetActive(false);
        }

        currentFilledOrbs--;
        Debug.Log($"Теперь заполненных кружков: {currentFilledOrbs}");
    }

    void ReorderOrbs()
    {
        Debug.Log("Реорганизация кружков...");

        // Создаем временный список заполненных кружков
        List<int> filledOrbIndices = new List<int>();

        // Находим все заполненные кружки
        for (int i = 0; i < orbUIs.Count; i++)
        {
            if (orbUIs[i].fillImage != null && orbUIs[i].fillImage.gameObject.activeSelf)
            {
                filledOrbIndices.Add(i);
                Debug.Log($"Кружок {i} все еще заполнен");
            }
        }

        Debug.Log($"Найдено {filledOrbIndices.Count} заполненных кружков после использования");

        // Опустошаем все кружки
        for (int i = 0; i < orbUIs.Count; i++)
        {
            EmptyOrb(i);
        }

        // Заполняем кружки с начала
        currentFilledOrbs = 0;
        foreach (int index in filledOrbIndices)
        {
            if (currentFilledOrbs < orbUIs.Count)
            {
                AddFilledOrb();
            }
        }

        Debug.Log($"После реорганизации: {currentFilledOrbs} заполненных кружков");
    }

    void RemoveBottomLine()
    {
        Debug.Log("Начало удаления нижней строки...");

        if (powerScaleManager != null)
        {
            Debug.Log("Используем PowerScaleManager для удаления строки");

            // Используем публичный метод PowerScaleManager
            powerScaleManager.RemoveBottomLinePublic();
        }
        else if (fieldGrid != null)
        {
            Debug.Log("PowerScaleManager не найден, удаляем напрямую");
            RemoveBottomLineDirectly();
        }
        else
        {
            Debug.LogError("Не найден PowerScaleManager или FieldGrid!");
        }
    }

    void RemoveBottomLineDirectly()
    {
        Debug.Log("Прямое удаление нижней строки");

        // Реализация удаления нижней строки
        var gridField = typeof(FieldGrid).GetField("grid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (gridField == null || fieldGrid == null)
        {
            Debug.LogError("Не удалось получить доступ к сетке FieldGrid");
            return;
        }

        GameObject[,] grid = (GameObject[,])gridField.GetValue(fieldGrid);

        // Удаляем нижнюю строку
        int bottomLineY = 0;
        int destroyedBlocks = 0;

        for (int x = 0; x < 10; x++)
        {
            if (grid[x, bottomLineY] != null)
            {
                Destroy(grid[x, bottomLineY]);
                grid[x, bottomLineY] = null;
                destroyedBlocks++;
            }
        }

        Debug.Log($"Уничтожено блоков на нижней строке: {destroyedBlocks}");

        // Перемещаем все блоки вниз
        int movedBlocks = 0;
        for (int y = bottomLineY + 1; y < 20; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = null;

                    // Обновляем позицию
                    Vector3 newPos = fieldGrid.GridToWorldPosition(x, y - 1);
                    grid[x, y - 1].transform.position = newPos;
                    movedBlocks++;
                }
            }
        }

        Debug.Log($"Перемещено блоков вниз: {movedBlocks}");
        Debug.Log("Нижняя строка удалена напрямую");
    }

    // Метод для получения максимального количества кружков
    public int GetMaxOrbs() => maxOrbs;

    // Метод для получения текущего прогресса (для отладки)
    public float GetFillProgress()
    {
        if (powerScaleManager != null)
        {
            return powerScaleManager.currentFillAmount / fillPerOrb;
        }
        return 0f;
    }

    // Метод для ручного добавления кружка (для тестирования)
    [ContextMenu("Добавить тестовый кружок")]
    public void AddTestOrb()
    {
        if (currentFilledOrbs < maxOrbs)
        {
            Debug.Log("Добавляем тестовый кружок через контекстное меню");
            AddFilledOrb();
        }
        else
        {
            Debug.Log("Достигнуто максимальное количество кружков");
        }
    }

    // Метод для использования всех кружков (для тестирования)
    [ContextMenu("Использовать все кружки")]
    public void UseAllOrbs()
    {
        Debug.Log("Используем все кружки через контекстное меню");
        while (currentFilledOrbs > 0)
        {
            UseOrb(0); // Всегда используем первый доступный
        }
    }

    // Метод для сброса всех кружков (для тестирования)
    [ContextMenu("Сбросить все кружки")]
    public void ResetAllOrbs()
    {
        Debug.Log("Сбрасываем все кружки");
        for (int i = 0; i < orbUIs.Count; i++)
        {
            EmptyOrb(i);
        }
        currentFilledOrbs = 0;
    }
}