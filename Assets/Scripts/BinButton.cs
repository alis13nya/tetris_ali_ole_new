using UnityEngine;
using UnityEngine.UI;

public class BinButton : MonoBehaviour
{
    [Header("Ссылки")]
    public PowerScaleManager powerScaleManager;
    public Animator lidAnimator;
    public Button button;
    private Animator buttonAnimator;

    [Header("Настройки")]
    public float animationDuration = 0.5f;

    private void Start()
    {
        if (button == null) button = GetComponent<Button>();
        if (powerScaleManager == null) powerScaleManager = FindObjectOfType<PowerScaleManager>();

        buttonAnimator = GetComponent<Animator>();

        if (button != null)
            button.interactable = false;

        if (powerScaleManager != null)
            powerScaleManager.OnPowerReady += EnableButton;
    }

    private void EnableButton()
    {
        if (button != null)
            button.interactable = true;

        // Включаем пульсацию
        if (buttonAnimator != null)
            buttonAnimator.SetBool("isReady", true);
    }

    public void OnButtonClick()
    {
        if (powerScaleManager == null || !powerScaleManager.IsPowerReady())
            return;

        if (lidAnimator != null)
        {
            lidAnimator.SetTrigger("OpenClose");
            button.interactable = false;

            // Выключаем пульсацию
            if (buttonAnimator != null)
                buttonAnimator.SetBool("isReady", false);

            // Удаляем строку после анимации
            Invoke(nameof(ExecutePower), animationDuration);
        }
        else
        {
            ExecutePower();
        }
    }

    private void ExecutePower()
    {
        powerScaleManager.UsePower();
    }

    private void OnDestroy()
    {
        if (powerScaleManager != null)
            powerScaleManager.OnPowerReady -= EnableButton;
    }
}