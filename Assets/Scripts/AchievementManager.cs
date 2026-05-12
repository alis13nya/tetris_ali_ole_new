using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Список всех достижений")]
    public List<AchievementData> allAchievements;

    private Dictionary<string, bool> unlockedStates = new Dictionary<string, bool>();
    private bool hasUnviewedUnlocks = false;

    private const string SAVE_KEY = "Achievements";

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
        hasUnviewedUnlocks = true;
        Save();

        Debug.Log($"🏆 РАЗБЛОКИРОВАНО: {GetAchievementTitle(id)}");
    }

    // Проверить, разблокировано ли
    public bool IsUnlocked(string id)
    {
        return unlockedStates.ContainsKey(id) && unlockedStates[id];
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

    // Отметить как просмотренные (когда открыли панель)
    public void MarkAsViewed()
    {
        hasUnviewedUnlocks = false;
        Save();
    }

    // Сбросить все достижения
    public void ResetAllAchievements()
    {
        unlockedStates.Clear();
        hasUnviewedUnlocks = false;
        Save();
        Debug.Log("Все достижения сброшены");
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "{}");
        var data = JsonUtility.FromJson<AchievementSaveData>(json);
        if (data != null && data.unlocked != null)
        {
            foreach (var id in data.unlocked)
                unlockedStates[id] = true;
        }
        hasUnviewedUnlocks = data.hasUnviewed;
    }

    private void Save()
    {
        var data = new AchievementSaveData();
        data.unlocked = unlockedStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        data.hasUnviewed = hasUnviewedUnlocks;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
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
        public List<string> unlocked = new List<string>();
        public bool hasUnviewed = false;
    }
}