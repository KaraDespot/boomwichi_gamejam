/*
 * OrderData
 * Назначение: общие структуры данных для результата проверки заказа Boomwichi.
 * Что делает: описывает ошибки проверки и итог с чаевыми.
 * Связи: используется OrderEvaluator и PackageZone вместе с OrderDataAsset из Flow.
 * Паттерны: Value Object, Shared Contract.
 */

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
    WrongSauceType,
    MoldyIngredient,
    FallenMold,
    RoachContamination
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
        OrderDataAsset order,
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

    public OrderDataAsset Order { get; }
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
