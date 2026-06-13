using UnityEngine;
using UnityEngine.UI;
public class AboutUI : MonoBehaviour
{
    [Header("ѕанель ќб игре")]
    public GameObject aboutPanel;
    public Button openAboutButton;
    public Button closeAboutButton;
    [Header("—сылки")]
    public Transform contentParent; 
    void Start()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
 
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