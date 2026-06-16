using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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

    [Header("Scroll View для отслеживания видимости")]
    public ScrollRect achievementsScrollRect;
    public RectTransform viewportRect;

    [Header("NEW значок")]
    public GameObject newBadgePrefab;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private Dictionary<string, GameObject> achievementItems = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> newBadges = new Dictionary<string, GameObject>();
    private Dictionary<string, bool> wasViewed = new Dictionary<string, bool>();
    private Dictionary<string, float> visibilityStartTime = new Dictionary<string, float>();
    private Dictionary<string, Coroutine> fadeCoroutines = new Dictionary<string, Coroutine>();
    private const float VIEW_DELAY = 5f;
    private const float FADE_DURATION = 0.5f;
    public bool IsPanelOpen { get; private set; } = false;

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

    void Update()
    {
        if (IsPanelOpen && achievementItems.Count > 0)
        {
            CheckVisibleAchievements();
        }
    }

    void OpenPanel()
    {
        if (achievementsPanel != null)
        {
            achievementsPanel.SetActive(true);
            IsPanelOpen = true;
            PopulateAchievementsList();
            UpdateExclamationMark();
        }
    }

    void ClosePanel()
    {
        if (achievementsPanel != null)
        {
            // Перед закрытием отмечаем все видимые достижения
            MarkAllVisibleAsViewed();

            achievementsPanel.SetActive(false);
            IsPanelOpen = false;
        }
    }

    void PopulateAchievementsList()
    {
        foreach (var item in spawnedItems)
            Destroy(item);
        spawnedItems.Clear();
        achievementItems.Clear();
        newBadges.Clear();
        wasViewed.Clear();
        visibilityStartTime.Clear();

        if (AchievementManager.Instance == null)
        {
            Debug.LogWarning("AchievementManager.Instance == null");
            return;
        }

        var achievements = AchievementManager.Instance.GetAllAchievements();

        foreach (var achievement in achievements)
        {
            bool isUnlocked = AchievementManager.Instance.IsUnlocked(achievement.id);
            bool isViewed = AchievementManager.Instance.IsViewed(achievement.id);

            // Для рекордного достижения оно всегда считается "разблокированным" после первого рекорда
            if (achievement.isRecordAchievement && AchievementManager.Instance.GetBestScore() > 0)
            {
                isUnlocked = true;
            }

            GameObject item = CreateAchievementItem(achievement, isUnlocked);
            achievementItems[achievement.id] = item;
            wasViewed[achievement.id] = isViewed;

            if (isUnlocked && !isViewed)
            {
                AddNewBadge(achievement.id, item);
            }
        }

        Debug.Log($"Создано элементов достижений: {spawnedItems.Count}");

        if (achievementsScrollRect != null)
        {
            achievementsScrollRect.verticalNormalizedPosition = 1f;
        }
    }
    public void RefreshAchievementsList()
    {
        if (!IsPanelOpen) return;

        foreach (var achievement in AchievementManager.Instance.GetAllAchievements())
        {
            if (achievementItems.ContainsKey(achievement.id))
            {
                GameObject item = achievementItems[achievement.id];
                bool isUnlocked = AchievementManager.Instance.IsUnlocked(achievement.id);
                bool isViewed = AchievementManager.Instance.IsViewed(achievement.id);

                // Для рекордного достижения обновляем текст описания
                if (achievement.isRecordAchievement)
                {
                    TextMeshProUGUI descText = item.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
                    if (descText != null)
                    {
                        int currentRecord = AchievementManager.Instance.GetBestScore();
                        descText.text = $"Ваш рекорд: {currentRecord}";
                    }
                }

                // Управление NEW бейджем
                if (isUnlocked && !isViewed)
                {
                    if (!newBadges.ContainsKey(achievement.id))
                    {
                        AddNewBadge(achievement.id, item);
                    }
                }
                else
                {
                    // Если достижение просмотрено или не разблокировано – удаляем бейдж
                    if (newBadges.ContainsKey(achievement.id))
                    {
                        RemoveNewBadge(achievement.id);
                    }
                }
            }
        }

        UpdateExclamationMark();
    }

    GameObject CreateAchievementItem(AchievementData data, bool isUnlocked)
    {
        if (achievementItemPrefab == null || contentParent == null) return null;

        GameObject item = Instantiate(achievementItemPrefab, contentParent);
        spawnedItems.Add(item);

        Image achievementIcon = item.transform.Find("AchievementIcon")?.GetComponent<Image>();
        if (achievementIcon != null)
        {
            if (isUnlocked && data.icon != null)
            {
                achievementIcon.sprite = data.icon;
            }
            else
            {
                // Используем дефолтную иконку для неразблокированных
                achievementIcon.sprite = AchievementManager.Instance.defaultLockedIcon;
            }
        }

        TextMeshProUGUI titleText = item.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = isUnlocked ? data.title : "???";
        }

        TextMeshProUGUI descText = item.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
        {
            if (data.isRecordAchievement)
            {
                int currentRecord = AchievementManager.Instance.GetBestScore();
                descText.text = $"Ваш рекорд: {currentRecord}";
            }
            else
            {
                descText.text = isUnlocked ? data.description : "???";
            }
        }

        return item;
    }

    void AddNewBadge(string id, GameObject parentItem)
    {
        if (newBadgePrefab == null) return;

        GameObject badge = Instantiate(newBadgePrefab, parentItem.transform);

        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        if (badgeRect != null)
        {
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(1, 1);
            badgeRect.anchoredPosition = new Vector2(-10, -10);
        }

        newBadges[id] = badge;
    }

    void RemoveNewBadge(string id)
    {
        if (newBadges.ContainsKey(id) && newBadges[id] != null)
        {
            if (fadeCoroutines.ContainsKey(id) && fadeCoroutines[id] != null)
            {
                StopCoroutine(fadeCoroutines[id]);
            }
            fadeCoroutines[id] = StartCoroutine(FadeOutBadge(id));
        }
        else
        {
            if (newBadges.ContainsKey(id))
                newBadges.Remove(id);
        }
    }

    IEnumerator FadeOutBadge(string id)
    {
        if (!newBadges.ContainsKey(id) || newBadges[id] == null)
        {
            newBadges.Remove(id);
            yield break;
        }

        GameObject badge = newBadges[id];

        CanvasGroup canvasGroup = badge.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = badge.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;

        while (elapsed < FADE_DURATION)
        {
            float t = elapsed / FADE_DURATION;
            float alpha = Mathf.Lerp(1f, 0f, t);
            canvasGroup.alpha = alpha;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(badge);
        newBadges.Remove(id);
        fadeCoroutines.Remove(id);
    }

    void CheckVisibleAchievements()
    {
        if (achievementsScrollRect == null || viewportRect == null) return;

        Vector3[] viewportCorners = new Vector3[4];
        viewportRect.GetWorldCorners(viewportCorners);
        Rect viewportRectWorld = new Rect(viewportCorners[0].x, viewportCorners[0].y,
                                           viewportCorners[2].x - viewportCorners[0].x,
                                           viewportCorners[2].y - viewportCorners[0].y);

        foreach (var kvp in achievementItems)
        {
            string id = kvp.Key;
            GameObject item = kvp.Value;

            if (wasViewed.ContainsKey(id) && wasViewed[id]) continue;
            if (!AchievementManager.Instance.IsUnlocked(id)) continue;

            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null) continue;

            Vector3[] itemCorners = new Vector3[4];
            itemRect.GetWorldCorners(itemCorners);

            bool isVisible = false;
            foreach (Vector3 corner in itemCorners)
            {
                if (viewportRectWorld.Contains(new Vector2(corner.x, corner.y)))
                {
                    isVisible = true;
                    break;
                }
            }

            if (isVisible)
            {
                if (!visibilityStartTime.ContainsKey(id))
                {
                    visibilityStartTime[id] = Time.time;
                }
                else if (Time.time - visibilityStartTime[id] >= VIEW_DELAY)
                {
                    AchievementManager.Instance.MarkAsViewed(id);
                    wasViewed[id] = true;
                    RemoveNewBadge(id);
                    visibilityStartTime.Remove(id);
                }
            }
            else
            {
                if (visibilityStartTime.ContainsKey(id))
                {
                    visibilityStartTime.Remove(id);
                }
            }
        }

        UpdateExclamationMark();
    }
    void MarkAllVisibleAsViewed()
    {
        if (achievementsScrollRect == null || viewportRect == null) return;

        Vector3[] viewportCorners = new Vector3[4];
        viewportRect.GetWorldCorners(viewportCorners);
        Rect viewportRectWorld = new Rect(viewportCorners[0].x, viewportCorners[0].y,
                                           viewportCorners[2].x - viewportCorners[0].x,
                                           viewportCorners[2].y - viewportCorners[0].y);

        foreach (var kvp in achievementItems)
        {
            string id = kvp.Key;
            GameObject item = kvp.Value;

            // Если уже просмотрено или не разблокировано — пропускаем
            if (wasViewed.ContainsKey(id) && wasViewed[id]) continue;
            if (!AchievementManager.Instance.IsUnlocked(id)) continue;

            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null) continue;

            Vector3[] itemCorners = new Vector3[4];
            itemRect.GetWorldCorners(itemCorners);

            bool isVisible = false;
            foreach (Vector3 corner in itemCorners)
            {
                if (viewportRectWorld.Contains(new Vector2(corner.x, corner.y)))
                {
                    isVisible = true;
                    break;
                }
            }

            if (isVisible)
            {
                // Отмечаем как просмотренное сразу
                AchievementManager.Instance.MarkAsViewed(id);
                wasViewed[id] = true;
                RemoveNewBadge(id);
                visibilityStartTime.Remove(id);
                Debug.Log($"Достижение {id} просмотрено (при закрытии панели)!");
            }
        }

        UpdateExclamationMark();
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