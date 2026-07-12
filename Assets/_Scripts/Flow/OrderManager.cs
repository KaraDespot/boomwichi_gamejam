/*
 * OrderManager
 * Назначение: очередь заказов игрового дня.
 * Что делает: выдаёт заказы из массива Order Data Asset, проваливает по таймеру, переходит к следующему.
 * Связи: GameManager, CustomerTimer, EventBus, CustomerUI.
 * Паттерны: Manager, Observer через EventBus.
 */

using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("Таймер клиента")]
    [SerializeField] private CustomerTimer customerTimer;

    [Header("Заказы дня")]
    [Tooltip("Перетащи сюда Order Data Asset в нужном порядке. Создать: ПКМ в Project → Create → Boomwichi → Order Data.")]
    [SerializeField] private OrderDataAsset[] dayOrders;

    private int currentOrderIndex = -1;
    private bool dayOrdersFinished;

    public int CurrentOrderIndex => currentOrderIndex;
    public int TotalOrders => dayOrders != null ? dayOrders.Length : 0;
    public bool IsDayOrdersFinished => dayOrdersFinished;

    public OrderDataAsset CurrentOrder =>
        dayOrders != null && currentOrderIndex >= 0 && currentOrderIndex < dayOrders.Length
            ? dayOrders[currentOrderIndex]
            : null;

    private void Awake()
    {
        if (customerTimer == null)
            customerTimer = GetComponent<CustomerTimer>();

        ValidateDayOrders();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayStateChanged += HandleDayStateChanged;
            EventBus.Instance.OnCustomerTimeExpired += HandleCustomerTimeExpired;
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayStateChanged -= HandleDayStateChanged;
            EventBus.Instance.OnCustomerTimeExpired -= HandleCustomerTimeExpired;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dayOrders == null || dayOrders.Length == 0)
            return;

        for (int i = 0; i < dayOrders.Length; i++)
        {
            if (dayOrders[i] == null)
                Debug.LogWarning($"{name}: слот заказа #{i + 1} пустой — назначь Order Data Asset.", this);
        }
    }
#endif

    /// <summary>
    /// Сброс перед новым днём.
    /// </summary>
    public void ResetDayOrders()
    {
        currentOrderIndex = -1;
        dayOrdersFinished = false;

        if (customerTimer != null)
            customerTimer.ResetTimer();
    }

    /// <summary>
    /// Запускает следующий заказ или завершает день.
    /// </summary>
    public void StartNextOrder()
    {
        if (dayOrdersFinished)
            return;

        currentOrderIndex++;

        if (dayOrders == null || currentOrderIndex >= dayOrders.Length)
        {
            FinishAllOrders();
            return;
        }

        OrderDataAsset order = dayOrders[currentOrderIndex];
        if (order == null)
        {
            Debug.LogWarning($"{name}: заказ #{currentOrderIndex + 1} не назначен, пропускаю.", this);
            StartNextOrder();
            return;
        }

        if (customerTimer != null)
            customerTimer.StartTimer();

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseOrderStarted(currentOrderIndex, order);

        if (GameManager.Instance != null)
            GameManager.Instance.SetDayState(DayFlowState.ShowingOrder);
    }

    /// <summary>
    /// Провал текущего заказа (таймер клиента или грязный сендвич позже).
    /// </summary>
    public void FailCurrentOrder()
    {
        if (dayOrdersFinished || currentOrderIndex < 0)
            return;

        if (customerTimer != null)
            customerTimer.StopTimer();

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseOrderFailed(currentOrderIndex);

        StartNextOrder();
    }

    /// <summary>
    /// Успешная сдача заказа — вызывается Dev 1 после OrderEvaluator.
    /// </summary>
    public void CompleteCurrentOrder(int tipsEarned)
    {
        if (dayOrdersFinished || currentOrderIndex < 0)
            return;

        if (customerTimer != null)
            customerTimer.StopTimer();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddTips(tipsEarned);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseOrderCompleted(currentOrderIndex, tipsEarned);

        if (GameManager.Instance != null)
            GameManager.Instance.SetDayState(DayFlowState.CustomerReaction);

        StartNextOrder();
    }

    private void HandleDayStateChanged(DayFlowState state)
    {
        if (state == DayFlowState.DayStarting && currentOrderIndex < 0)
            StartNextOrder();
    }

    private void HandleCustomerTimeExpired()
    {
        if (GameManager.Instance == null)
        {
            FailCurrentOrder();
            return;
        }

        DayFlowState state = GameManager.Instance.CurrentDayState;
        if (state == DayFlowState.ShowingOrder || state == DayFlowState.PlayingOrder)
            FailCurrentOrder();
    }

    private void FinishAllOrders()
    {
        dayOrdersFinished = true;
        currentOrderIndex = dayOrders != null ? dayOrders.Length : 0;

        if (customerTimer != null)
            customerTimer.StopTimer();

        if (GameManager.Instance != null)
            GameManager.Instance.FinishDay();
    }

    private void ValidateDayOrders()
    {
        if (dayOrders == null || dayOrders.Length == 0)
        {
            Debug.LogError(
                $"{name}: не назначены заказы. Создай Order Data Asset " +
                "(Create → Boomwichi → Order Data) и перетащи в массив Day Orders.",
                this);
        }
    }
}
