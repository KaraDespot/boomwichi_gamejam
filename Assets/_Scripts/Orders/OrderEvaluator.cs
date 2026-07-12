/*
 * OrderEvaluator
 * Назначение: проверка готового сендвича против текущего заказа.
 * Что делает: ищет недостающие, лишние и грязные ингредиенты, проверяет хлеб и считает чаевые.
 * Связи: вызывается PackageZone, читает OrderDefinition, SandwichState и IngredientInstance.
 * Паттерны: Domain Service.
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OrderEvaluator : MonoBehaviour
{
    [Header("Tips")]
    [Tooltip("Чаевые по умолчанию, если в заказе не задан свой максимум.")]
    [SerializeField] private int defaultMaxTips = 10;

    [Tooltip("Штраф за каждую недостающую единицу ингредиента.")]
    [SerializeField] private int missingIngredientPenalty = 4;

    [Tooltip("Штраф за каждую лишнюю единицу ингредиента.")]
    [SerializeField] private int extraIngredientPenalty = 4;

    [Tooltip("Штраф за неправильную прожарку хлеба.")]
    [SerializeField] private int wrongCookPenalty = 5;

    [Tooltip("Штраф за отсутствующий нижний или верхний хлеб.")]
    [SerializeField] private int missingBreadPenalty = 5;

    [Tooltip("Штраф за любую грязь, плесень или заражение тараканом.")]
    [SerializeField] private int dirtyPenalty = 10;

    [Tooltip("Минимальные чаевые после всех штрафов.")]
    [SerializeField] private int minimumTips = 0;

    public OrderEvaluationResult Evaluate(OrderDefinition order, SandwichState sandwichState)
    {
        List<OrderEvaluationIssue> issues = new List<OrderEvaluationIssue>();

        if (order == null)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.NoActiveOrder,
                IngredientType.Bread,
                0,
                0,
                "Нет активного заказа."));

            return CreateResult(order, sandwichState, issues);
        }

        if (sandwichState == null)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.NoSandwich,
                IngredientType.Bread,
                1,
                0,
                "В пакет не передан сендвич."));

            return CreateResult(order, sandwichState, issues);
        }

        EvaluateBread(order, sandwichState, issues);
        EvaluateIngredients(order, sandwichState, issues);
        EvaluateDirt(sandwichState, issues);

        return CreateResult(order, sandwichState, issues);
    }

    private void EvaluateBread(OrderDefinition order, SandwichState sandwichState, List<OrderEvaluationIssue> issues)
    {
        if (!sandwichState.HasBottomBread)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.MissingBottomBread,
                IngredientType.Bread,
                1,
                0,
                "Нет нижнего хлеба."));
        }

        if (!sandwichState.HasTopBread)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.MissingTopBread,
                IngredientType.Bread,
                1,
                0,
                "Нет верхнего хлеба."));

            return;
        }

        if (sandwichState.CookState == SandwichCookState.Raw && order.RequiredCookState != SandwichCookState.Raw)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.RawTopBread,
                IngredientType.Bread,
                (int)order.RequiredCookState,
                (int)sandwichState.CookState,
                "Верхний хлеб сырой."));

            return;
        }

        if (sandwichState.CookState != order.RequiredCookState)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.WrongCookState,
                IngredientType.Bread,
                (int)order.RequiredCookState,
                (int)sandwichState.CookState,
                $"Нужна прожарка {order.RequiredCookState}, получено {sandwichState.CookState}."));
        }
    }

    private void EvaluateIngredients(OrderDefinition order, SandwichState sandwichState, List<OrderEvaluationIssue> issues)
    {
        for (int i = 0; i < order.RequiredIngredientCount; i++)
        {
            OrderIngredientRequirement requirement = order.GetRequirement(i);
            if (requirement == null)
                continue;

            int actualCount = sandwichState.GetIngredientCount(requirement.IngredientType);
            int expectedCount = requirement.Count;

            if (actualCount < expectedCount)
            {
                issues.Add(new OrderEvaluationIssue(
                    OrderEvaluationIssueType.MissingIngredient,
                    requirement.IngredientType,
                    expectedCount,
                    actualCount,
                    $"Не хватает {requirement.IngredientType}: нужно {expectedCount}, есть {actualCount}."));
            }

            if (actualCount > expectedCount)
            {
                issues.Add(new OrderEvaluationIssue(
                    OrderEvaluationIssueType.ExtraIngredient,
                    requirement.IngredientType,
                    expectedCount,
                    actualCount,
                    $"Лишний {requirement.IngredientType}: нужно {expectedCount}, есть {actualCount}."));
            }
        }

        foreach (KeyValuePair<IngredientType, int> ingredientCount in sandwichState.IngredientCounts)
        {
            if (ingredientCount.Value <= 0 || order.TryGetRequirement(ingredientCount.Key, out _))
                continue;

            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.ExtraIngredient,
                ingredientCount.Key,
                0,
                ingredientCount.Value,
                $"Лишний ингредиент {ingredientCount.Key}: {ingredientCount.Value}."));
        }
    }

    private void EvaluateDirt(SandwichState sandwichState, List<OrderEvaluationIssue> issues)
    {
        EvaluateIngredientDirt(sandwichState.BottomBread, issues);
        EvaluateIngredientDirt(sandwichState.TopBread, issues);

        for (int i = 0; i < sandwichState.Ingredients.Count; i++)
            EvaluateIngredientDirt(sandwichState.Ingredients[i], issues);

        if (sandwichState.HasFallenMold)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.FallenMold,
                IngredientType.Bread,
                0,
                1,
                "На сендвич упала плесень."));
        }

        if (sandwichState.HasRoachContact)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.RoachContamination,
                IngredientType.Bread,
                0,
                1,
                "Таракан коснулся сендвича."));
        }
    }

    private void EvaluateIngredientDirt(IngredientInstance ingredient, List<OrderEvaluationIssue> issues)
    {
        if (ingredient == null)
            return;

        if (ingredient.IsMoldy)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.MoldyIngredient,
                ingredient.Type,
                0,
                1,
                $"Плесневый ингредиент: {ingredient.Type}."));
        }

        if (ingredient.IsRoachContaminated)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.RoachContamination,
                ingredient.Type,
                0,
                1,
                $"Ингредиент заражен тараканом: {ingredient.Type}."));
        }
    }

    private OrderEvaluationResult CreateResult(
        OrderDefinition order,
        SandwichState sandwichState,
        IReadOnlyList<OrderEvaluationIssue> issues)
    {
        int tips = CalculateTips(order, issues);
        string summary = issues.Count == 0
            ? $"Заказ выполнен. Чаевые: {tips}."
            : $"Заказ с ошибками: {issues.Count}. Чаевые: {tips}.";

        return new OrderEvaluationResult(order, sandwichState, issues, tips, summary);
    }

    private int CalculateTips(OrderDefinition order, IReadOnlyList<OrderEvaluationIssue> issues)
    {
        if (ContainsBlockingIssue(issues))
            return minimumTips;

        int tips = order != null && order.MaxTips > 0
            ? order.MaxTips
            : defaultMaxTips;

        for (int i = 0; i < issues.Count; i++)
        {
            OrderEvaluationIssue issue = issues[i];
            switch (issue.IssueType)
            {
                case OrderEvaluationIssueType.MissingBottomBread:
                case OrderEvaluationIssueType.MissingTopBread:
                    tips -= missingBreadPenalty;
                    break;

                case OrderEvaluationIssueType.RawTopBread:
                case OrderEvaluationIssueType.WrongCookState:
                    tips -= wrongCookPenalty;
                    break;

                case OrderEvaluationIssueType.MissingIngredient:
                    tips -= missingIngredientPenalty * Mathf.Max(1, issue.ExpectedCount - issue.ActualCount);
                    break;

                case OrderEvaluationIssueType.ExtraIngredient:
                    tips -= extraIngredientPenalty * Mathf.Max(1, issue.ActualCount - issue.ExpectedCount);
                    break;

                case OrderEvaluationIssueType.MoldyIngredient:
                case OrderEvaluationIssueType.FallenMold:
                case OrderEvaluationIssueType.RoachContamination:
                    tips -= dirtyPenalty;
                    break;
            }
        }

        return Mathf.Max(minimumTips, tips);
    }

    private bool ContainsBlockingIssue(IReadOnlyList<OrderEvaluationIssue> issues)
    {
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i].IssueType == OrderEvaluationIssueType.NoActiveOrder ||
                issues[i].IssueType == OrderEvaluationIssueType.NoSandwich)
            {
                return true;
            }
        }

        return false;
    }
}
