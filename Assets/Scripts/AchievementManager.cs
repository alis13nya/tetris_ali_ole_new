using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AchievementManager : MonoBehaviour
{
    private const string SAVE_KEY_BEST_SCORE = "BestScore";
    private int bestScore = 0;
    public static AchievementManager Instance { get; private set; }

    [Header("Настройки отображения")]
    public Sprite defaultLockedIcon; // Иконка для неразблокированных достижений

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
    public Sprite GetAchievementIcon(string id)
    {
        AchievementData ach = allAchievements.Find(a => a.id == id);
        if (ach == null) return null;

        bool isUnlocked = IsUnlocked(id);

        if (isUnlocked && ach.icon != null)
        {
            return ach.icon;
        }
        else
        {
            return defaultLockedIcon;
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

        // Сброс рекорда
        bestScore = 0;

        // Обновляем поле recordValue в AchievementData (если используется)
        AchievementData recordAch = allAchievements.Find(a => a.id == "high_score");
        if (recordAch != null)
        {
            recordAch.recordValue = 0;
        }

        Save();
        Debug.Log("Все достижения и рекорд сброшены");
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
        bestScore = PlayerPrefs.GetInt(SAVE_KEY_BEST_SCORE, 0);
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
        PlayerPrefs.SetInt(SAVE_KEY_BEST_SCORE, bestScore);
    }

    private string GetAchievementTitle(string id)
    {
        var ach = allAchievements.Find(a => a.id == id);
        return ach != null ? ach.title : id;
    }

    public string GetAchievementDisplayText(string id)
    {
        AchievementData ach = allAchievements.Find(a => a.id == id);
        if (ach == null) return "";

        if (ach.isRecordAchievement)
        {
            return $"Ваш рекорд: {bestScore}";
        }
        else
        {
            return ach.description;
        }
    }
    [System.Serializable]
    private class AchievementSaveData
    {
        public List<string> achievements = new List<string>();
    }

    public void CheckHighScore(int newScore)
    {
        if (newScore <= bestScore) return;

        bestScore = newScore;
        Save();

        // Находим достижение-рекорд
        AchievementData recordAch = allAchievements.Find(a => a.id == "high_score");
        if (recordAch == null)
        {
            Debug.LogWarning("Достижение 'high_score' не найдено!");
            return;
        }

        // Если достижение ещё не разблокировано – разблокируем
        if (!IsUnlocked("high_score"))
        {
            UnlockAchievement("high_score");
        }
        else
        {
            // Уже разблокировано – помечаем как непросмотренное (чтобы появился NEW)
            if (viewedAchievements.Contains("high_score"))
            {
                viewedAchievements.Remove("high_score");
                hasUnviewedUnlocks = true;
                Save();
            }
        }

        // *** ВАЖНО: обновляем поле recordValue в AchievementData, если оно используется для отображения
        recordAch.recordValue = bestScore;

        // *** ДОПОЛНИТЕЛЬНО: вызываем событие обновления UI для достижений, если панель открыта
        AchievementsUI ui = FindObjectOfType<AchievementsUI>();
        if (ui != null && ui.IsPanelOpen)
        {
            ui.RefreshAchievementsList(); // добавим этот метод позже
        }

        Debug.Log($"🏆 НОВЫЙ РЕКОРД: {bestScore}!");
    }

    // Получить текущий рекорд
    public int GetBestScore()
    {
        return bestScore;
    }
}