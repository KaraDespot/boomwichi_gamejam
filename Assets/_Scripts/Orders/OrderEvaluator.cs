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

    [Tooltip("Штраф за неправильный цвет соуса.")]
    [SerializeField] private int wrongSaucePenalty = 5;

    [Tooltip("Штраф за отсутствующий нижний или верхний хлеб.")]
    [SerializeField] private int missingBreadPenalty = 5;

    [Tooltip("Штраф за любую грязь, плесень или заражение тараканом.")]
    [SerializeField] private int dirtyPenalty = 10;

    [Tooltip("Минимальные чаевые после всех штрафов.")]
    [SerializeField] private int minimumTips = 0;

    [Header("Отладка")]
    [Tooltip("Подробные логи проверки заказа и прожарки в консоль.")]
    [SerializeField] private bool debugEvaluationLogs = true;

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

            OrderEvaluationResult noOrderResult = CreateResult(order, sandwichState, issues);
            LogEvaluationResult(order, sandwichState, issues, noOrderResult);
            return noOrderResult;
        }

        if (sandwichState == null)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.NoSandwich,
                IngredientType.Bread,
                1,
                0,
                "В пакет не передан сендвич."));

            OrderEvaluationResult noSandwichResult = CreateResult(order, sandwichState, issues);
            LogEvaluationResult(order, sandwichState, issues, noSandwichResult);
            return noSandwichResult;
        }

        LogSandwichSnapshot("до проверки", order, sandwichState);

        EvaluateBread(order, sandwichState, issues);
        EvaluateSauce(order, sandwichState, issues);
        EvaluateIngredients(order, sandwichState, issues);
        EvaluateDirt(sandwichState, issues);

        OrderEvaluationResult result = CreateResult(order, sandwichState, issues);
        LogEvaluationResult(order, sandwichState, issues, result);
        return result;
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

    private void EvaluateSauce(OrderDataAsset order, SandwichState sandwichState, List<OrderEvaluationIssue> issues)
    {
        SauceType requiredSauce = order.RequiredSauce;

        if (requiredSauce == SauceType.None)
            return;

        if (!sandwichState.HasSauce)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.MissingIngredient,
                IngredientType.Sauce,
                1,
                0,
                $"Не хватает соуса: нужен {GetSauceLabel(requiredSauce)}."));
            return;
        }

        SauceType actualSauce = sandwichState.GetSauceType();
        if (actualSauce != SauceType.None && actualSauce != requiredSauce)
        {
            issues.Add(new OrderEvaluationIssue(
                OrderEvaluationIssueType.WrongSauceType,
                IngredientType.Sauce,
                (int)requiredSauce,
                (int)actualSauce,
                $"Нужен {GetSauceLabel(requiredSauce)}, а на сендвиче {GetSauceLabel(actualSauce)}."));
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
            if (ingredientCount.Value <= 0)
                continue;

            if (ingredientCount.Key == IngredientType.Sauce)
            {
                if (order.RequiredSauce == SauceType.None)
                {
                    issues.Add(new OrderEvaluationIssue(
                        OrderEvaluationIssueType.ExtraIngredient,
                        IngredientType.Sauce,
                        0,
                        ingredientCount.Value,
                        "В заказе соус не нужен."));
                }

                continue;
            }

            if (expectedCounts.ContainsKey(ingredientCount.Key))
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

                case OrderEvaluationIssueType.WrongSauceType:
                    tips -= wrongSaucePenalty;
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

    private static string GetSauceLabel(SauceType sauceType)
    {
        return sauceType switch
        {
            SauceType.Red => "красный соус",
            SauceType.White => "белый соус",
            _ => "соус"
        };
    }

    private void LogSandwichSnapshot(string stage, OrderDataAsset order, SandwichState sandwichState)
    {
        if (!debugEvaluationLogs)
            return;

        string orderName = order != null ? order.name : "null";
        if (sandwichState == null)
        {
            Debug.Log($"[OrderEvaluator] {stage}: заказ={orderName}, сендвич=null", this);
            return;
        }

        string ingredientSummary = BuildIngredientSummary(sandwichState);
        Debug.Log(
            $"[OrderEvaluator] {stage}: заказ={orderName}, " +
            $"требуется прожарка={order?.RequiredTopToast}, соус={order?.RequiredSauce}, " +
            $"сендвич={sandwichState.name}, CookState={sandwichState.CookState}, " +
            $"CookProgress={sandwichState.CookProgressSeconds:F2}s, " +
            $"нижний хлеб={sandwichState.HasBottomBread}, верхний={sandwichState.HasTopBread}, " +
            $"соус на сендвиче={sandwichState.GetSauceType()}, " +
            $"ингредиенты=[{ingredientSummary}], " +
            $"грязь: плесень={sandwichState.HasFallenMold}, таракан={sandwichState.HasRoachContact}",
            this);
    }

    private void LogEvaluationResult(
        OrderDataAsset order,
        SandwichState sandwichState,
        IReadOnlyList<OrderEvaluationIssue> issues,
        OrderEvaluationResult result)
    {
        if (!debugEvaluationLogs)
            return;

        string orderName = order != null ? order.name : "null";
        if (issues.Count == 0)
        {
            Debug.Log($"[OrderEvaluator] Итог: заказ={orderName}, ошибок нет, чаевые={result.TipAmount}", this);
            return;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine($"[OrderEvaluator] Итог: заказ={orderName}, ошибок={issues.Count}, чаевые={result.TipAmount}");
        builder.AppendLine($"  сендвич CookState={sandwichState?.CookState}, прогресс={sandwichState?.CookProgressSeconds:F2}s");

        for (int i = 0; i < issues.Count; i++)
        {
            OrderEvaluationIssue issue = issues[i];
            builder.AppendLine(
                $"  [{i + 1}] {issue.IssueType}: {issue.Message} " +
                $"(ожидалось={issue.ExpectedCount}, факт={issue.ActualCount}, штраф≈{GetIssuePenalty(issue)})");
        }

        Debug.Log(builder.ToString(), this);
    }

    private int GetIssuePenalty(OrderEvaluationIssue issue)
    {
        switch (issue.IssueType)
        {
            case OrderEvaluationIssueType.MissingBottomBread:
            case OrderEvaluationIssueType.MissingTopBread:
                return missingBreadPenalty;

            case OrderEvaluationIssueType.RawTopBread:
            case OrderEvaluationIssueType.WrongCookState:
                return wrongCookPenalty;

            case OrderEvaluationIssueType.WrongSauceType:
                return wrongSaucePenalty;

            case OrderEvaluationIssueType.MissingIngredient:
                return missingIngredientPenalty * Mathf.Max(1, issue.ExpectedCount - issue.ActualCount);

            case OrderEvaluationIssueType.ExtraIngredient:
                return extraIngredientPenalty * Mathf.Max(1, issue.ActualCount - issue.ExpectedCount);

            case OrderEvaluationIssueType.MoldyIngredient:
            case OrderEvaluationIssueType.FallenMold:
            case OrderEvaluationIssueType.RoachContamination:
                return dirtyPenalty;

            case OrderEvaluationIssueType.NoActiveOrder:
            case OrderEvaluationIssueType.NoSandwich:
                return defaultMaxTips;

            default:
                return 0;
        }
    }

    private static string BuildIngredientSummary(SandwichState sandwichState)
    {
        if (sandwichState == null || sandwichState.IngredientCounts.Count == 0)
            return "пусто";

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (KeyValuePair<IngredientType, int> pair in sandwichState.IngredientCounts)
        {
            if (pair.Value <= 0)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            if (pair.Key == IngredientType.Sauce)
                builder.Append($"{pair.Key}({sandwichState.GetSauceType()})x{pair.Value}");
            else
                builder.Append($"{pair.Key}x{pair.Value}");
        }

        return builder.Length > 0 ? builder.ToString() : "пусто";
    }
}
