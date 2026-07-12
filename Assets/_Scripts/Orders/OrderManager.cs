/*
 * OrderManager
 * Назначение: хранит текущий заказ игрового дня и переключает MVP-цепочку заказов.
 * Что делает: выдаёт один активный заказ, принимает результат сдачи и переходит к следующему.
 * Связи: читается PackageZone, публикует события для будущего UI заказа.
 * Паттерны: Scene Service, Event Publisher.
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OrderManager : MonoBehaviour
{
    [Header("Orders")]
    [Tooltip("Список заказов на день. Если пусто, будут созданы три MVP-заказа из спецификации.")]
    [SerializeField] private OrderDefinition[] orders = Array.Empty<OrderDefinition>();

    [Tooltip("Создавать три тестовых заказа из спецификации, если список заказов пуст.")]
    [SerializeField] private bool useDefaultOrdersIfEmpty = true;

    [Tooltip("Начальный индекс заказа для тестирования.")]
    [SerializeField] private int startOrderIndex;

    public static OrderManager Instance { get; private set; }

    public event Action<OrderDefinition> OrderIssued;
    public event Action<OrderEvaluationResult> OrderCompleted;
    public event Action DayOrdersCompleted;

    private int currentOrderIndex;
    private bool firstOrderIssued;

    public OrderDefinition CurrentOrder => HasCurrentOrder ? orders[currentOrderIndex] : null;
    public bool HasCurrentOrder => orders != null && currentOrderIndex >= 0 && currentOrderIndex < orders.Length;
    public bool IsDayComplete => orders != null && orders.Length > 0 && currentOrderIndex >= orders.Length;
    public int CurrentOrderNumber => HasCurrentOrder ? currentOrderIndex + 1 : 0;
    public int TotalOrderCount => orders != null ? orders.Length : 0;
    public int CompletedOrderCount { get; private set; }
    public int SuccessfulOrderCount { get; private set; }
    public int FailedOrderCount { get; private set; }
    public int DirtyOrderCount { get; private set; }
    public int TotalEarnedTips { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{name}: на сцене уже есть OrderManager, этот экземпляр будет работать локально.", this);
        }
        else
        {
            Instance = this;
        }

        EnsureOrders();
        currentOrderIndex = Mathf.Clamp(startOrderIndex, 0, Mathf.Max(0, orders.Length - 1));
    }

    private void Start()
    {
        IssueCurrentOrderOnce();
    }

    public bool TryGetCurrentOrder(out OrderDefinition order)
    {
        order = CurrentOrder;
        return order != null;
    }

    public void CompleteCurrentOrder(OrderEvaluationResult evaluationResult)
    {
        if (!HasCurrentOrder)
            return;

        CompletedOrderCount++;
        TotalEarnedTips += evaluationResult.TipAmount;

        if (evaluationResult.IsSuccess)
            SuccessfulOrderCount++;
        else
            FailedOrderCount++;

        if (evaluationResult.IsDirty)
            DirtyOrderCount++;

        OrderCompleted?.Invoke(evaluationResult);
        currentOrderIndex++;
        firstOrderIssued = false;

        if (HasCurrentOrder)
        {
            IssueCurrentOrderOnce();
            return;
        }

        DayOrdersCompleted?.Invoke();
    }

    public static OrderDefinition[] CreateDefaultOrders()
    {
        return new[]
        {
            new OrderDefinition(
                "order_01",
                "Соус, колбаса x4, хлеб хорошо поджарить. Без лишнего.",
                SandwichCookState.Toasted,
                10,
                new[]
                {
                    new OrderIngredientRequirement(IngredientType.Sauce, 1),
                    new OrderIngredientRequirement(IngredientType.Sausage, 4)
                }),
            new OrderDefinition(
                "order_02",
                "Соус, огурцы x4, лук x4, сыр x1. Хлеб пережарить.",
                SandwichCookState.Burnt,
                10,
                new[]
                {
                    new OrderIngredientRequirement(IngredientType.Sauce, 1),
                    new OrderIngredientRequirement(IngredientType.Cucumber, 4),
                    new OrderIngredientRequirement(IngredientType.Onion, 4),
                    new OrderIngredientRequirement(IngredientType.Cheese, 1)
                }),
            new OrderDefinition(
                "order_03",
                "Без соуса. Помидоры x4, огурцы x4, колбаса x4, сыр x1.",
                SandwichCookState.Toasted,
                10,
                new[]
                {
                    new OrderIngredientRequirement(IngredientType.Tomato, 4),
                    new OrderIngredientRequirement(IngredientType.Cucumber, 4),
                    new OrderIngredientRequirement(IngredientType.Sausage, 4),
                    new OrderIngredientRequirement(IngredientType.Cheese, 1)
                })
        };
    }

    private void EnsureOrders()
    {
        if ((orders == null || orders.Length == 0) && useDefaultOrdersIfEmpty)
            orders = CreateDefaultOrders();
    }

    private void IssueCurrentOrderOnce()
    {
        if (firstOrderIssued || !HasCurrentOrder)
            return;

        firstOrderIssued = true;
        OrderIssued?.Invoke(CurrentOrder);
        Debug.Log($"{name}: выдан заказ {CurrentOrderNumber}/{TotalOrderCount}: {CurrentOrder.CustomerText}", this);
    }
}
