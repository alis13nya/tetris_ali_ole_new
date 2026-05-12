using UnityEngine;
using UnityEngine.UI;

public class AspectRatioManager : MonoBehaviour
{
    [Header("Настройки")]
    public float targetAspect = 0.546f; // 1136×2080

    [Header("Ссылки")]
    public Camera gameCamera;

    // Для черных полос
    public GameObject letterboxContainer;
    public Image topBar, bottomBar, leftBar, rightBar;

    private void Start()
    {
        Debug.Log($"Экран: {Screen.width}x{Screen.height}, Аспект: {(float)Screen.width / Screen.height:F3}");

        // Создаем letterbox если нет
        if (letterboxContainer == null)
            CreateLetterbox();

        ApplyAspectRatio();
    }

    private void CreateLetterbox()
    {
        // Создаем контейнер для letterbox
        letterboxContainer = new GameObject("LetterboxContainer");
        letterboxContainer.transform.SetParent(transform);

        // Создаем 4 черных полосы
        topBar = CreateLetterboxBar("TopBar", new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        bottomBar = CreateLetterboxBar("BottomBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        leftBar = CreateLetterboxBar("LeftBar", new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        rightBar = CreateLetterboxBar("RightBar", new Vector2(1, 0.5f), new Vector2(1, 0.5f));
    }

    private Image CreateLetterboxBar(string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(letterboxContainer.transform);

        Image img = bar.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);

        return img;
    }

    public void ApplyAspectRatio()
    {
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError("Камера не найдена!");
                return;
            }
        }

        float screenAspect = (float)Screen.width / Screen.height;

        Debug.Log($"Целевой аспект: {targetAspect}, Текущий: {screenAspect:F3}");

        if (Mathf.Approximately(screenAspect, targetAspect))
        {
            // Совпадает - скрываем letterbox
            SetLetterboxVisible(false);
            gameCamera.rect = new Rect(0, 0, 1, 1);
            return;
        }

        if (screenAspect > targetAspect)
        {
            // Широкий экран (iPad) - letterbox по бокам
            float gameWidth = targetAspect / screenAspect;
            float barWidth = (1f - gameWidth) / 2f;

            gameCamera.rect = new Rect(barWidth, 0, gameWidth, 1);
            UpdateLetterbox(true, barWidth);
        }
        else
        {
            // Узкий экран - letterbox сверху и снизу
            float gameHeight = screenAspect / targetAspect;
            float barHeight = (1f - gameHeight) / 2f;

            gameCamera.rect = new Rect(0, barHeight, 1, gameHeight);
            UpdateLetterbox(false, barHeight);
        }

        SetLetterboxVisible(true);
    }

    private void UpdateLetterbox(bool isWideScreen, float barSize)
    {
        if (isWideScreen)
        {
            // Боковые полосы
            SetBarSize(leftBar, barSize, 1);
            SetBarSize(rightBar, barSize, 1);
            SetBarSize(topBar, 1, 0);
            SetBarSize(bottomBar, 1, 0);
        }
        else
        {
            // Верхняя/нижняя полосы
            SetBarSize(topBar, 1, barSize);
            SetBarSize(bottomBar, 1, barSize);
            SetBarSize(leftBar, 0, 1);
            SetBarSize(rightBar, 0, 1);
        }
    }

    private void SetBarSize(Image bar, float width, float height)
    {
        if (bar == null) return;

        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Screen.width * width, Screen.height * height);

        if (width == 0 || height == 0)
            bar.gameObject.SetActive(false);
        else
            bar.gameObject.SetActive(true);
    }

    private void SetLetterboxVisible(bool visible)
    {
        if (letterboxContainer != null)
            letterboxContainer.SetActive(visible);
    }

    // Для тестирования в Update
    private Vector2 lastScreenSize;
    private void Update()
    {
        Vector2 currentSize = new Vector2(Screen.width, Screen.height);
        if (currentSize != lastScreenSize)
        {
            lastScreenSize = currentSize;
            ApplyAspectRatio();
        }
    }
}