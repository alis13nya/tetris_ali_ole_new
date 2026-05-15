using UnityEngine;
using UnityEngine.UI;

public class AboutUI : MonoBehaviour
{
    [Header("Панель Об игре")]
    public GameObject aboutPanel;
    public Button openAboutButton;
    public Button closeAboutButton;
    [Header("Ссылки")]
    public Transform contentParent; // перетащи сюда Content из Scroll View
    void Start()
    {
        // Изначально панель закрыта
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        // Назначаем кнопки
        if (openAboutButton != null)
            openAboutButton.onClick.AddListener(OpenPanel);

        if (closeAboutButton != null)
            closeAboutButton.onClick.AddListener(ClosePanel);
    }

    void OpenPanel()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    void ClosePanel()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
    }

}