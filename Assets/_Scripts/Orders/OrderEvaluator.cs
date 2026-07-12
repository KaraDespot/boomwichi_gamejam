/*
 * OrderEvaluator
 * Назначение: проверка готового сендвича против текущего OrderDataAsset.
 * Что делает: ищет недостающие, лишние и грязные ингредиенты, проверяет хлеб и считает чаевые.
 * Связи: вызывается PackageZone, читает OrderDataAsset, SandwichState и IngredientInstance.
 * Паттерны: Domain Service.
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OrderEvaluator : MonoBehaviour
{
    [Header("Tips")]
    [Tooltip("Максимальные чаевые за идеальный заказ.")]
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

    public OrderEvaluationResult Evaluate(OrderDataAsset order, SandwichState sandwichState)
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

    private void EvaluateBread(OrderDataAsset order, SandwichState sandwichState, List<OrderEvaluationIssue> issues)
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

        SandwichCookState requiredCookState = ConvertToastState(order.RequiredTopToast);
        if (sandwichState.CookState == SandwichCookState.Raw && requiredCookState != SandwichCookState.Raw)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.RawTopBread,
                IngredientType.Bread,
                (int)requiredCookState,
                (int)sandwichState.CookState,
                "Верхний хлеб сырой."));

            return;
        }

        if (sandwichState.CookState != requiredCookState)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.WrongCookState,
                IngredientType.Bread,
                (int)requiredCookState,
                (int)sandwichState.CookState,
                $"Нужна прожарка {requiredCookState}, получено {sandwichState.CookState}."));
        }
    }

    private void EvaluateIngredients(OrderDataAsset order, SandwichState sandwichState, List<OrderEvaluationIssue> issues)
    {
        Dictionary<IngredientType, int> expectedCounts = BuildExpectedCounts(order);

        foreach (KeyValuePair<IngredientType, int> expectedCount in expectedCounts)
        {
            int actualCount = sandwichState.GetIngredientCount(expectedCount.Key);
            if (actualCount < expectedCount.Value)
            {
                issues.Add(new OrderEvaluationIssue(
                    OrderEvaluationIssueType.MissingIngredient,
                    expectedCount.Key,
                    expectedCount.Value,
                    actualCount,
                    $"Не хватает {expectedCount.Key}: нужно {expectedCount.Value}, есть {actualCount}."));
            }

            if (actualCount > expectedCount.Value)
            {
                issues.Add(new OrderEvaluationIssue(
                    OrderEvaluationIssueType.ExtraIngredient,
                    expectedCount.Key,
                    expectedCount.Value,
                    actualCount,
                    $"Лишний {expectedCount.Key}: нужно {expectedCount.Value}, есть {actualCount}."));
            }
        }

        foreach (KeyValuePair<IngredientType, int> ingredientCount in sandwichState.IngredientCounts)
        {
            if (ingredientCount.Value <= 0 || expectedCounts.ContainsKey(ingredientCount.Key))
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

    private Dictionary<IngredientType, int> BuildExpectedCounts(OrderDataAsset order)
    {
        Dictionary<IngredientType, int> expectedCounts = new Dictionary<IngredientType, int>();

        if (order.RequiresSauce)
            expectedCounts[IngredientType.Sauce] = 1;

        OrderIngredientRequirement[] ingredients = order.Ingredients;
        if (ingredients == null)
            return expectedCounts;

        for (int i = 0; i < ingredients.Length; i++)
        {
            IngredientType type = ingredients[i].type;
            int count = Mathf.Max(0, ingredients[i].count);

            if (count <= 0 || type == IngredientType.Bread)
                continue;

            if (!expectedCounts.ContainsKey(type))
                expectedCounts.Add(type, 0);

            expectedCounts[type] += count;
        }

        return expectedCounts;
    }

    private OrderEvaluationResult CreateResult(
        OrderDataAsset order,
        SandwichState sandwichState,
        IReadOnlyList<OrderEvaluationIssue> issues)
    {
        int tips = CalculateTips(issues);
        string summary = issues.Count == 0
            ? $"Заказ выполнен. Чаевые: {tips}."
            : $"Заказ с ошибками: {issues.Count}. Чаевые: {tips}.";

        return new OrderEvaluationResult(order, sandwichState, issues, tips, summary);
    }

    private int CalculateTips(IReadOnlyList<OrderEvaluationIssue> issues)
    {
        if (ContainsBlockingIssue(issues))
            return minimumTips;

        int tips = defaultMaxTips;

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

    private SandwichCookState ConvertToastState(ToastState toastState)
    {
        switch (toastState)
        {
            case ToastState.Burnt:
                return SandwichCookState.Burnt;

            case ToastState.Toasted:
                return SandwichCookState.Toasted;

            default:
                return SandwichCookState.Raw;
        }
    }
}
