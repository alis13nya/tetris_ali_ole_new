using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementsUI : MonoBehaviour
{
    [Header("UI элементы")]
    public GameObject achievementsPanel;
    public Transform contentParent;
    public GameObject achievementItemPrefab;
    public Button closeButton;
    public Button clearAllButton;

    [Header("Кнопка открытия")]
    public Button openAchievementsButton;
    public GameObject exclamationMark;

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        if (openAchievementsButton != null)
            openAchievementsButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (clearAllButton != null)
            clearAllButton.onClick.AddListener(ClearAllAchievements);

        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        UpdateExclamationMark();
    }

    void OnEnable()
    {
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

        if (AchievementManager.Instance == null)
        {
            Debug.LogWarning("AchievementManager.Instance == null");
            return;
        }

        var achievements = AchievementManager.Instance.GetAllAchievements();

        if (achievements == null || achievements.Count == 0)
        {
            Debug.LogWarning("Нет достижений в AchievementManager");
            return;
        }

        foreach (var achievement in achievements)
        {
            bool isUnlocked = AchievementManager.Instance.IsUnlocked(achievement.id);
            CreateAchievementItem(achievement, isUnlocked);
        }

        Debug.Log($"Создано элементов достижений: {spawnedItems.Count}");
    }

    void CreateAchievementItem(AchievementData data, bool isUnlocked)
    {
        if (achievementItemPrefab == null || contentParent == null) return;

        GameObject item = Instantiate(achievementItemPrefab, contentParent);
        spawnedItems.Add(item);

        // Иконка статуса (замочек/галочка) - ищем по имени "StatusIcon"
        Image statusIcon = item.transform.Find("StatusIcon")?.GetComponent<Image>();
        if (statusIcon != null)
        {
            statusIcon.color = isUnlocked ? Color.green : Color.gray;
        }

        // Иконка достижения - ищем по имени "AchievementIcon"
        Image achievementIcon = item.transform.Find("AchievementIcon")?.GetComponent<Image>();
        if (achievementIcon != null && data.icon != null && isUnlocked)
        {
            achievementIcon.sprite = data.icon;
        }

        // Название - ищем по имени "TitleText"
        TextMeshProUGUI titleText = item.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = isUnlocked ? data.title : "???";
        }

        // Описание - ищем по имени "DescriptionText"
        TextMeshProUGUI descText = item.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
        {
            descText.text = isUnlocked ? data.description : "???";
        }

        // Цвет фона
        Image background = item.GetComponent<Image>();
        if (background != null)
        {
            if (isUnlocked)
            {
                background.color = data.isPositive ? new Color(0.2f, 0.7f, 0.2f, 0.8f) : new Color(0.7f, 0.2f, 0.2f, 0.8f);
            }
            else
            {
                background.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
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
            PopulateAchievementsList();
            UpdateExclamationMark();
        }
    }
}