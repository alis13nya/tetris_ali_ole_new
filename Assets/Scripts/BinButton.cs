using UnityEngine;
using UnityEngine.UI;

public class BinButton : MonoBehaviour
{
    [Header("Ссылки")]
    public PowerScaleManager powerScaleManager;
    public Animator lidAnimator;
    public Button button;

    private void Start()
    {
        // Если ссылка на кнопку не назначена, берём с этого же объекта
        if (button == null)
            button = GetComponent<Button>();

        // Если не назначен PowerScaleManager, ищем на сцене
        if (powerScaleManager == null)
            powerScaleManager = FindObjectOfType<PowerScaleManager>();

        // Изначально кнопка неактивна (шкала пуста)
        if (button != null)
            button.interactable = false;

        // Подписываемся на событие готовности шкалы
        if (powerScaleManager != null)
            powerScaleManager.OnPowerReady += EnableButton;

    }

    private void EnableButton()
    {
        // Когда шкала заполнилась — активируем кнопку
        if (button != null)
            button.interactable = true;
    }

    // Этот метод вызывается при нажатии на кнопку (через OnClick)
    public void OnButtonClick()
    {
        // Проверяем, готова ли способность
        if (powerScaleManager == null || !powerScaleManager.IsPowerReady())
            return;

        // Проигрываем анимацию крышки (один раз)
        if (lidAnimator != null)
        {
            lidAnimator.SetTrigger("OpenClose");
        }

        // Используем способность (удаляет нижнюю строку и сбрасывает шкалу)
        powerScaleManager.UsePower();

        // Деактивируем кнопку до следующего заполнения
        if (button != null)
            button.interactable = false;
    }

    private void OnDestroy()
    {
        // Отписываемся от события, чтобы избежать ошибок
        if (powerScaleManager != null)
            powerScaleManager.OnPowerReady -= EnableButton;
    }
}