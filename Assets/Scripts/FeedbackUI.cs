using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    public string url;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }
            });
        }
    }
}