using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Список всех достижений")]
    public List<AchievementData> allAchievements;

    private Dictionary<string, bool> unlockedStates = new Dictionary<string, bool>();
    private HashSet<string> viewedAchievements = new HashSet<string>();
    private bool hasUnviewedUnlocks = false;

    private const string SAVE_KEY_UNLOCKED = "AchievementsUnlocked";
    private const string SAVE_KEY_VIEWED = "AchievementsViewed";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Разблокировать достижение
    public void UnlockAchievement(string id)
    {
        if (unlockedStates.ContainsKey(id) && unlockedStates[id])
            return;

        if (!allAchievements.Any(a => a.id == id))
        {
            Debug.LogWarning($"Достижение с id {id} не найдено!");
            return;
        }

        unlockedStates[id] = true;

        // Новое достижение ещё не просмотрено
        if (!viewedAchievements.Contains(id))
        {
            hasUnviewedUnlocks = true;
        }

        Save();

        Debug.Log($"🏆 РАЗБЛОКИРОВАНО: {GetAchievementTitle(id)}");
    }

    // Проверить, разблокировано ли
    public bool IsUnlocked(string id)
    {
        return unlockedStates.ContainsKey(id) && unlockedStates[id];
    }

    // Проверить, просмотрено ли достижение
    public bool IsViewed(string id)
    {
        return viewedAchievements.Contains(id);
    }

    // Отметить конкретное достижение как просмотренное
    public void MarkAsViewed(string id)
    {
        if (!viewedAchievements.Contains(id))
        {
            viewedAchievements.Add(id);

            // Проверяем, остались ли ещё непросмотренные достижения
            bool anyUnviewed = false;
            foreach (var unlocked in unlockedStates.Where(kvp => kvp.Value))
            {
                if (!viewedAchievements.Contains(unlocked.Key))
                {
                    anyUnviewed = true;
                    break;
                }
            }
            hasUnviewedUnlocks = anyUnviewed;

            Save();
        }
    }

    // Отметить все как просмотренные (старый метод для совместимости)
    public void MarkAllAsViewed()
    {
        foreach (var unlocked in unlockedStates.Where(kvp => kvp.Value))
        {
            if (!viewedAchievements.Contains(unlocked.Key))
            {
                viewedAchievements.Add(unlocked.Key);
            }
        }
        hasUnviewedUnlocks = false;
        Save();
    }

    // Получить все достижения
    public List<AchievementData> GetAllAchievements()
    {
        return allAchievements;
    }

    // Есть ли непросмотренные достижения?
    public bool HasUnviewedUnlocks()
    {
        return hasUnviewedUnlocks;
    }

    // Отметить как просмотренные (когда открыли панель) — для совместимости
    public void MarkAsViewed()
    {
        MarkAllAsViewed();
    }

    // Сбросить все достижения
    public void ResetAllAchievements()
    {
        unlockedStates.Clear();
        viewedAchievements.Clear();
        hasUnviewedUnlocks = false;
        Save();
        Debug.Log("Все достижения сброшены");
    }

    private void Load()
    {
        // Загрузка разблокированных достижений
        string unlockedJson = PlayerPrefs.GetString(SAVE_KEY_UNLOCKED, "{}");
        var unlockedData = JsonUtility.FromJson<AchievementSaveData>(unlockedJson);
        if (unlockedData != null && unlockedData.achievements != null)
        {
            foreach (var id in unlockedData.achievements)
                unlockedStates[id] = true;
        }

        // Загрузка просмотренных достижений
        string viewedJson = PlayerPrefs.GetString(SAVE_KEY_VIEWED, "{}");
        var viewedData = JsonUtility.FromJson<AchievementSaveData>(viewedJson);
        if (viewedData != null && viewedData.achievements != null)
        {
            foreach (var id in viewedData.achievements)
                viewedAchievements.Add(id);
        }

        // Проверяем, есть ли непросмотренные
        hasUnviewedUnlocks = false;
        foreach (var unlocked in unlockedStates.Where(kvp => kvp.Value))
        {
            if (!viewedAchievements.Contains(unlocked.Key))
            {
                hasUnviewedUnlocks = true;
                break;
            }
        }
    }

    private void Save()
    {
        // Сохраняем разблокированные
        var unlockedData = new AchievementSaveData();
        unlockedData.achievements = unlockedStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        string unlockedJson = JsonUtility.ToJson(unlockedData);
        PlayerPrefs.SetString(SAVE_KEY_UNLOCKED, unlockedJson);

        // Сохраняем просмотренные
        var viewedData = new AchievementSaveData();
        viewedData.achievements = viewedAchievements.ToList();
        string viewedJson = JsonUtility.ToJson(viewedData);
        PlayerPrefs.SetString(SAVE_KEY_VIEWED, viewedJson);

        PlayerPrefs.Save();
    }

    private string GetAchievementTitle(string id)
    {
        var ach = allAchievements.Find(a => a.id == id);
        return ach != null ? ach.title : id;
    }

    [System.Serializable]
    private class AchievementSaveData
    {
        public List<string> achievements = new List<string>();
    }
}