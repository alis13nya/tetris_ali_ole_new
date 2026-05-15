using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Панели Game Over")]
    public GameObject normalGameOverPanel;      // обычная панель (только крестик)
    public GameObject achievementsGameOverPanel; // панель с достижениями (крестик + кнопка перехода)

    [Header("Кнопки")]
    public Button closeButtonNormal;            // крестик на обычной панели
    public Button closeButtonAchievements;      // крестик на панели с достижениями
    public Button viewAchievementsButton;       // кнопка "Посмотреть достижения"

    void Start()
    {
        // Скрываем обе панели при старте
        if (normalGameOverPanel != null)
            normalGameOverPanel.SetActive(false);

        if (achievementsGameOverPanel != null)
            achievementsGameOverPanel.SetActive(false);

        // Назначаем кнопки
        if (closeButtonNormal != null)
            closeButtonNormal.onClick.AddListener(RestartGame);

        if (closeButtonAchievements != null)
            closeButtonAchievements.onClick.AddListener(RestartGame);

        if (viewAchievementsButton != null)
            viewAchievementsButton.onClick.AddListener(ViewAchievements);
    }

    public void ShowGameOver()
    {
        // Проверяем, есть ли новые достижения
        bool hasNewAchievements = (AchievementManager.Instance != null &&
                                   AchievementManager.Instance.HasUnviewedUnlocks());

        if (hasNewAchievements)
        {
            // Показываем панель с достижениями
            if (achievementsGameOverPanel != null)
                achievementsGameOverPanel.SetActive(true);
        }
        else
        {
            // Показываем обычную панель (только крестик)
            if (normalGameOverPanel != null)
                normalGameOverPanel.SetActive(true);
        }
    }

    void RestartGame()
    {
        // Скрываем обе панели
        if (normalGameOverPanel != null)
            normalGameOverPanel.SetActive(false);

        if (achievementsGameOverPanel != null)
            achievementsGameOverPanel.SetActive(false);

        // Перезапускаем игровую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ViewAchievements()
    {
        // Устанавливаем флаг, что нужно открыть достижения при загрузке главного меню
        MainMenuManager.OpenAchievementsOnStart = true;

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");
    }
}