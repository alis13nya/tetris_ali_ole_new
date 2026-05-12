using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementsUI : MonoBehaviour
{
    [Header("UI элементы")]
    public GameObject achievementsPanel;      // Сама панель
    public Transform contentParent;           // Родитель для элементов
    public GameObject achievementItemPrefab;  // Префаб элемента достижения
    public Button closeButton;                // Кнопка закрыть
    public Button clearAllButton;             // Кнопка очистить всё

    [Header("Кнопка открытия")]
    public Button openAchievementsButton;     // Кнопка "Достижения" в меню
    public GameObject exclamationMark;        // Восклицательный знак

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        // Открыть панель
        if (openAchievementsButton != null)
            openAchievementsButton.onClick.AddListener(OpenPanel);

        // Закрыть панель
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Очистить все достижения
        if (clearAllButton != null)
            clearAllButton.onClick.AddListener(ClearAllAchievements);

        // Изначально панель закрыта
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        // Обновить восклицательный знак
        UpdateExclamationMark();
    }

    void OnEnable()
    {
        // Обновляем знак каждый раз, когда объект активен
        UpdateExclamationMark();
    }

    void OpenPanel()
    {
        if (achievementsPanel != null)
        {
            achievementsPanel.SetActive(true);
            PopulateAchievementsList();

            // Отметить как просмотренные
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.MarkAsViewed();

            UpdateExclamationMark();
        }
    }

    void ClosePanel()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    void PopulateAchievementsList()
    {
        // Очищаем старые элементы
        foreach (var item in spawnedItems)
            Destroy(item);
        spawnedItems.Clear();

        if (AchievementManager.Instance == null) return;

        var achievements = AchievementManager.Instance.GetAllAchievements();

        foreach (var achievement in achievements)
        {
            bool isUnlocked = AchievementManager.Instance.IsUnlocked(achievement.id);
            CreateAchievementItem(achievement, isUnlocked);
        }
    }

    void CreateAchievementItem(AchievementData data, bool isUnlocked)
    {
        if (achievementItemPrefab == null || contentParent == null) return;

        GameObject item = Instantiate(achievementItemPrefab, contentParent);
        spawnedItems.Add(item);

        // Иконка
        Image icon = item.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && data.icon != null)
            icon.sprite = data.icon;

        // Название
        TextMeshProUGUI titleText = item.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
            titleText.text = data.title;

        // Описание
        TextMeshProUGUI descText = item.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
            descText.text = data.description;

        // Статус (замочек/галочка)
        Image statusImage = item.transform.Find("Status")?.GetComponent<Image>();
        if (statusImage != null)
        {
            if (isUnlocked)
            {
                // Можно поставить иконку галочки
                statusImage.color = Color.green;
            }
            else
            {
                // Иконка замка
                statusImage.color = Color.gray;
            }
        }

        // Цвет фона в зависимости от типа достижения
        Image background = item.GetComponent<Image>();
        if (background != null)
        {
            if (isUnlocked)
            {
                background.color = data.isPositive ? new Color(0.2f, 0.8f, 0.2f, 0.5f) : new Color(0.8f, 0.3f, 0.3f, 0.5f);
            }
            else
            {
                background.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }

    void UpdateExclamationMark()
    {
        if (exclamationMark != null && AchievementManager.Instance != null)
        {
            exclamationMark.SetActive(AchievementManager.Instance.HasUnviewedUnlocks());
        }
    }

    void ClearAllAchievements()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.ResetAllAchievements();
            PopulateAchievementsList(); // Обновляем список
            UpdateExclamationMark();
        }
    }
}