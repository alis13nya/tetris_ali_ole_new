using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    [Header("Ссылки")]
    public GameManager gameManager;

    [Header("Кнопки движения")]
    public Button leftButton;
    public Button rightButton;
    public Button rotateLeftButton;
    public Button rotateRightButton;
    public Button downButton;
    public Button hardDropButton; // Мгновенное падение (опционально)

    [Header("Настройки")]
    public float repeatDelay = 0.2f;    // Задержка перед повторением
    public float repeatRate = 0.05f;    // Частота повторения

    // Флаги для удержания кнопок
    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isDownPressed = false;

    // Таймеры
    private float leftTimer = 0f;
    private float rightTimer = 0f;
    private float downTimer = 0f;

    void Start()
    {
        // Назначаем методы на кнопки
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(MoveLeft);
            SetupButtonHold(leftButton, () => isLeftPressed = true, () => isLeftPressed = false);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(MoveRight);
            SetupButtonHold(rightButton, () => isRightPressed = true, () => isRightPressed = false);
        }

        if (rotateLeftButton != null)
        {
            rotateLeftButton.onClick.AddListener(RotateLeft);
        }

        if (rotateRightButton != null)
        {
            rotateRightButton.onClick.AddListener(RotateRight);
        }

        if (downButton != null)
        {
            downButton.onClick.AddListener(MoveDown);
            SetupButtonHold(downButton, () => isDownPressed = true, () => isDownPressed = false);
        }

        if (hardDropButton != null)
        {
            hardDropButton.onClick.AddListener(HardDrop);
        }
    }

    void Update()
    {
        // Обработка удержания кнопок
        HandleButtonHold(ref isLeftPressed, ref leftTimer, MoveLeft);
        HandleButtonHold(ref isRightPressed, ref rightTimer, MoveRight);
        HandleButtonHold(ref isDownPressed, ref downTimer, MoveDown);
    }

    // Настройка кнопки для удержания
    private void SetupButtonHold(Button button, System.Action onPress, System.Action onRelease)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Событие нажатия
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { onPress(); });
        trigger.triggers.Add(pointerDown);

        // Событие отпускания
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { onRelease(); });
        trigger.triggers.Add(pointerUp);
    }

    // Обработка удержания кнопки
    private void HandleButtonHold(ref bool isPressed, ref float timer, System.Action action)
    {
        if (isPressed)
        {
            timer += Time.deltaTime;

            if (timer >= repeatDelay)
            {
                // Первое действие с задержкой
                if (timer >= repeatDelay && timer < repeatDelay + repeatRate)
                {
                    action();
                }
                // Последующие действия с интервалом
                else if (timer >= repeatDelay + repeatRate)
                {
                    action();
                    timer = repeatDelay; // Сброс таймера для следующего интервала
                }
            }
        }
        else
        {
            timer = 0f;
        }
    }

    // Методы управления
    public void MoveLeft()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            if (gameManager.currentShape.CanMove(Vector2.left))
            {
                gameManager.currentShape.Move(Vector2.left);
            }
        }
    }

    public void MoveRight()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            if (gameManager.currentShape.CanMove(Vector2.right))
            {
                gameManager.currentShape.Move(Vector2.right);
            }
        }
    }

    public void MoveDown()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            if (gameManager.currentShape.CanMove(Vector2.down))
            {
                gameManager.currentShape.Move(Vector2.down);
            }
        }
    }

    public void RotateLeft()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            // ВЫЗЫВАЕМ НОВЫЙ МЕТОД ФИГУРЫ
            gameManager.currentShape.RotateLeft();
        }
    }

    public void RotateRight()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            // ВЫЗЫВАЕМ НОВЫЙ МЕТОД ФИГУРЫ
            gameManager.currentShape.RotateRight();
        }
    }

    public void HardDrop()
    {
        if (gameManager != null && gameManager.currentShape != null)
        {
            // Мгновенное падение до самого низа
            while (gameManager.currentShape.CanMove(Vector2.down))
            {
                gameManager.currentShape.Move(Vector2.down);
            }

            // Фиксация фигуры - нужно вызывать правильный метод
            // Если в GameManager есть метод LockCurrentShape, оставьте как есть
            // Если нет, нужно зафиксировать через FieldGrid
            if (gameManager.fieldGrid != null && gameManager.currentShape != null)
            {
                gameManager.fieldGrid.LockShape(gameManager.currentShape);

                // Очистка линий и спавн новой фигуры
                int lines = gameManager.fieldGrid.CheckAndClearLines();
                if (lines > 0)
                {
                    // Добавление очков, если есть такая логика
                }

                // Спавн новой фигуры
                gameManager.Invoke("SpawnNewShape", 0f);
            }
        }
    }

    // Метод для блокировки/разблокировки кнопок (например, при Game Over)
    public void SetControlsActive(bool active)
    {
        if (leftButton != null) leftButton.interactable = active;
        if (rightButton != null) rightButton.interactable = active;
        if (rotateLeftButton != null) rotateLeftButton.interactable = active;
        if (rotateRightButton != null) rotateRightButton.interactable = active;
        if (downButton != null) downButton.interactable = active;
        if (hardDropButton != null) hardDropButton.interactable = active;
    }
}