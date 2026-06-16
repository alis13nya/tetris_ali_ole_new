using UnityEngine;

[CreateAssetMenu(fileName = "NewAchievement", menuName = "Tetris/AchievementData")]
public class AchievementData : ScriptableObject
{
    public string id;            // уникальный идентификатор
    public string title;         // название достижения
    public string description;   // описание
    public Sprite icon;          // иконка
    public bool isPositive = true; // положительное или отрицательное (для окраски)
    public bool isRecordAchievement; // true для достижения-рекорда
    public int recordValue;          // текущее значение рекорда (только для isRecordAchievement = true)
}