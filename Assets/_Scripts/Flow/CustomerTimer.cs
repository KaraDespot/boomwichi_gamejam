/*
 * CustomerTimer
 * Назначение: таймер ожидания клиента (90 секунд на заказ).
 * Что делает: отсчитывает время, публикует прогресс в EventBus, проваливает заказ по истечении.
 * Связи: OrderManager, GameManager (пауза), GameplayHUDController.
 * Паттерны: компонент сцены, Observer через EventBus.
 */

using UnityEngine;

public class CustomerTimer : MonoBehaviour
{
    [Header("Ожидание клиента")]
    [Tooltip("Сколько секунд клиент ждёт заказ.")]
    [SerializeField] private float customerWaitDuration = 90f;

    [Header("Предупреждение")]
    [Tooltip("За сколько секунд до конца подсветить таймер.")]
    [SerializeField] private float warningThreshold = 10f;

    private float elapsedWaitTime;
    private bool isRunning;
    private bool isExpired;
    private bool warningRaised;

    public float CustomerWaitDuration => customerWaitDuration;
    public float RemainingTime => Mathf.Max(0f, customerWaitDuration - elapsedWaitTime);
    public float WarningThreshold => warningThreshold;
    public bool IsRunning => isRunning;
    public bool IsExpired => isExpired;
    public bool IsInWarningZone => isRunning && RemainingTime <= warningThreshold;

    private void Update()
    {
        if (!isRunning)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            return;

        elapsedWaitTime += Time.deltaTime;
        PublishTimeUpdated();
        TryPublishTimeWarning();

        if (!isExpired && elapsedWaitTime >= customerWaitDuration)
        {
            isExpired = true;
            isRunning = false;

            if (EventBus.Instance != null)
                EventBus.Instance.RaiseCustomerTimeExpired();
        }
    }

    public void StartTimer()
    {
        elapsedWaitTime = 0f;
        isExpired = false;
        warningRaised = false;
        isRunning = true;
        PublishTimeUpdated();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsedWaitTime = 0f;
        isExpired = false;
        warningRaised = false;
        isRunning = false;
        PublishTimeUpdated();
    }

    private void PublishTimeUpdated()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseCustomerTimeUpdated(RemainingTime, customerWaitDuration);
    }

    private void TryPublishTimeWarning()
    {
        if (warningRaised || warningThreshold <= 0f || RemainingTime > warningThreshold)
            return;

        warningRaised = true;
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseCustomerTimeWarning();
    }
}
