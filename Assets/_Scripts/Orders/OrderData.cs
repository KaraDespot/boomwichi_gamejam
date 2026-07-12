/*
 * OrderData
 * Назначение: общие структуры данных для заказов и результата проверки Boomwichi.
 * Что делает: описывает требования заказа, ошибки проверки и итог с чаевыми.
 * Связи: используется OrderManager, OrderEvaluator и PackageZone.
 * Паттерны: Value Object, Shared Contract.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public enum OrderEvaluationIssueType
{
    NoActiveOrder,
    NoSandwich,
    MissingBottomBread,
    MissingTopBread,
    RawTopBread,
    WrongCookState,
    MissingIngredient,
    ExtraIngredient,
    MoldyIngredient,
    FallenMold,
    RoachContamination
}

[Serializable]
public class OrderIngredientRequirement
{
    [SerializeField] private IngredientType ingredientType = IngredientType.Sausage;
    [SerializeField] private int count = 1;

    public OrderIngredientRequirement()
    {
    }

    public OrderIngredientRequirement(IngredientType ingredientType, int count)
    {
        this.ingredientType = ingredientType;
        this.count = Mathf.Max(0, count);
    }

    public IngredientType IngredientType => ingredientType;
    public int Count => Mathf.Max(0, count);
}

[Serializable]
public class OrderDefinition
{
    [SerializeField] private string orderId = "order";
    [SerializeField] private string customerText = string.Empty;
    [SerializeField] private SandwichCookState requiredCookState = SandwichCookState.Toasted;
    [SerializeField] private int maxTips = 10;
    [SerializeField] private OrderIngredientRequirement[] requiredIngredients = Array.Empty<OrderIngredientRequirement>();

    public OrderDefinition()
    {
    }

    public OrderDefinition(
        string orderId,
        string customerText,
        SandwichCookState requiredCookState,
        int maxTips,
        OrderIngredientRequirement[] requiredIngredients)
    {
        this.orderId = orderId;
        this.customerText = customerText;
        this.requiredCookState = requiredCookState;
        this.maxTips = Mathf.Max(0, maxTips);
        this.requiredIngredients = requiredIngredients ?? Array.Empty<OrderIngredientRequirement>();
    }

    public string OrderId => orderId;
    public string CustomerText => customerText;
    public SandwichCookState RequiredCookState => requiredCookState;
    public int MaxTips => Mathf.Max(0, maxTips);
    public int RequiredIngredientCount => requiredIngredients != null ? requiredIngredients.Length : 0;
    public IReadOnlyList<OrderIngredientRequirement> RequiredIngredients => requiredIngredients;

    public OrderIngredientRequirement GetRequirement(int index)
    {
        return requiredIngredients[index];
    }

    public bool TryGetRequirement(IngredientType ingredientType, out OrderIngredientRequirement requirement)
    {
        if (requiredIngredients == null)
        {
            requirement = null;
            return false;
        }

        for (int i = 0; i < requiredIngredients.Length; i++)
        {
            if (requiredIngredients[i] == null || requiredIngredients[i].IngredientType != ingredientType)
                continue;

            requirement = requiredIngredients[i];
            return true;
        }

        requirement = null;
        return false;
    }
}

public readonly struct OrderEvaluationIssue
{
    public OrderEvaluationIssue(
        OrderEvaluationIssueType issueType,
        IngredientType ingredientType,
        int expectedCount,
        int actualCount,
        string message)
    {
        IssueType = issueType;
        IngredientType = ingredientType;
        ExpectedCount = expectedCount;
        ActualCount = actualCount;
        Message = message;
    }

    public OrderEvaluationIssueType IssueType { get; }
    public IngredientType IngredientType { get; }
    public int ExpectedCount { get; }
    public int ActualCount { get; }
    public string Message { get; }
}

public readonly struct OrderEvaluationResult
{
    public OrderEvaluationResult(
        OrderDefinition order,
        SandwichState sandwichState,
        IReadOnlyList<OrderEvaluationIssue> issues,
        int tipAmount,
        string summary)
    {
        Order = order;
        SandwichState = sandwichState;
        Issues = issues;
        TipAmount = Mathf.Max(0, tipAmount);
        Summary = summary;
    }

    public OrderDefinition Order { get; }
    public SandwichState SandwichState { get; }
    public IReadOnlyList<OrderEvaluationIssue> Issues { get; }
    public int TipAmount { get; }
    public string Summary { get; }
    public bool IsSuccess => Issues == null || Issues.Count == 0;
    public bool IsDirty => HasIssue(OrderEvaluationIssueType.MoldyIngredient) ||
        HasIssue(OrderEvaluationIssueType.FallenMold) ||
        HasIssue(OrderEvaluationIssueType.RoachContamination);

    public bool HasIssue(OrderEvaluationIssueType issueType)
    {
        if (Issues == null)
            return false;

        for (int i = 0; i < Issues.Count; i++)
        {
            if (Issues[i].IssueType == issueType)
                return true;
        }

        return false;
    }
}
