using UnityEngine;
using UnityEngine.UI;

public class KeyboardControlsDisplay : MonoBehaviour
{
    [Header("Панель справки по управлению")]
    public GameObject controlsPanel;

    [Header("Настройки отображения")]
    public bool showOnPC = true;
    public bool autoHideOnMobile = true;

    [Header("Содержимое справки")]
    public Text controlsText;
    public Image controlsImage;

    [Header("Позиционирование")]
    public ScreenPosition screenPosition = ScreenPosition.TopRight;
    public Vector2 customOffset = Vector2.zero;

    [Header("Анимация")]
    public bool useFadeAnimation = true;
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.3f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    public enum ScreenPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        TopCenter,
        BottomCenter,
        Custom
    }

    void Start()
    {
        InitializeDisplay();
        UpdateDisplay();
    }

    void InitializeDisplay()
    {
        if (controlsPanel == null)
        {
            Debug.LogError("ControlsPanel не назначен!");
            return;
        }

        // Получаем или создаем CanvasGroup для анимаций прозрачности
        canvasGroup = controlsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null && useFadeAnimation)
        {
            canvasGroup = controlsPanel.AddComponent<CanvasGroup>();
        }

        rectTransform = controlsPanel.GetComponent<RectTransform>();

        // Настраиваем начальное состояние
        if (canvasGroup != null && useFadeAnimation)
        {
            canvasGroup.alpha = 0f;
        }

        controlsPanel.SetActive(false);
    }

    void UpdateDisplay()
    {
        if (controlsPanel == null) return;

        // Определяем платформу
        bool isMobile = Application.isMobilePlatform ||
                       SystemInfo.deviceType == DeviceType.Handheld;

        bool shouldShow = showOnPC && !isMobile;

        if (shouldShow)
        {
            ShowControls();
        }
        else if (autoHideOnMobile && isMobile)
        {
            HideControls();
        }
    }

    public void ShowControls()
    {
        if (controlsPanel == null) return;

        controlsPanel.SetActive(true);
        UpdatePosition();

        if (useFadeAnimation && canvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Debug.Log("Справка по управлению показана");
    }

    public void HideControls()
    {
        if (controlsPanel == null) return;

        if (useFadeAnimation && canvasGroup != null)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            controlsPanel.SetActive(false);
        }

        Debug.Log("Справка по управлению скрыта");
    }

    public void ToggleControls()
    {
        if (controlsPanel == null) return;

        if (controlsPanel.activeSelf)
        {
            HideControls();
        }
        else
        {
            ShowControls();
        }
    }

    System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        controlsPanel.SetActive(true);

        while (elapsedTime < fadeInTime)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    System.Collections.IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeOutTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        controlsPanel.SetActive(false);
    }

    void UpdatePosition()
    {
        if (rectTransform == null) return;

        Vector2 position = Vector2.zero;
        Vector2 pivot = Vector2.zero;

        switch (screenPosition)
        {
            case ScreenPosition.TopLeft:
                position = new Vector2(0, 1);
                pivot = new Vector2(0, 1);
                break;
            case ScreenPosition.TopRight:
                position = new Vector2(1, 1);
                pivot = new Vector2(1, 1);
                break;
            case ScreenPosition.BottomLeft:
                position = new Vector2(0, 0);
                pivot = new Vector2(0, 0);
                break;
            case ScreenPosition.BottomRight:
                position = new Vector2(1, 0);
                pivot = new Vector2(1, 0);
                break;
            case ScreenPosition.TopCenter:
                position = new Vector2(0.5f, 1);
                pivot = new Vector2(0.5f, 1);
                break;
            case ScreenPosition.BottomCenter:
                position = new Vector2(0.5f, 0);
                pivot = new Vector2(0.5f, 0);
                break;
            case ScreenPosition.Custom:
                position = customOffset;
                pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rectTransform.anchorMin = position;
        rectTransform.anchorMax = position;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    // Метод для обновления текста справки (можно вызывать извне)
    public void UpdateControlsText(string newText)
    {
        if (controlsText != null)
        {
            controlsText.text = newText;
        }
    }

    // Метод для обновления картинки (например, для разных платформ)
    public void UpdateControlsImage(Sprite newImage)
    {
        if (controlsImage != null)
        {
            controlsImage.sprite = newImage;
        }
    }

    // Проверка видимости
    public bool IsVisible()
    {
        return controlsPanel != null && controlsPanel.activeSelf;
    }
}