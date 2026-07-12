/*
 * SandwichState
 * Назначение: состояние текущего собранного сендвича, живущее на нижнем хлебе-root.
 * Что делает: хранит нижний хлеб, верхний хлеб, список слоёв и сообщает доске, когда root-сендвич исчезает.
 * Связи: создаётся SandwichBoard на нижнем хлебе, читается будущими зонами грильницы/упаковки/оценки заказа.
 * Паттерны: Aggregate Root State.
 */

using System.Collections.Generic;
using UnityEngine;

public enum SandwichCookState
{
    Raw,
    Toasted,
    Burnt
}

[DisallowMultipleComponent]
public class SandwichState : MonoBehaviour
{
    private readonly List<IngredientInstance> ingredients = new List<IngredientInstance>();
    private readonly Dictionary<IngredientType, int> ingredientCounts = new Dictionary<IngredientType, int>();

    private SandwichBoard ownerBoard;
    private IngredientInstance bottomBread;
    private IngredientInstance topBread;

    public IngredientInstance BottomBread => bottomBread;
    public IngredientInstance TopBread => topBread;
    public IReadOnlyList<IngredientInstance> Ingredients => ingredients;
    public IReadOnlyDictionary<IngredientType, int> IngredientCounts => ingredientCounts;
    public bool HasBottomBread => bottomBread != null;
    public bool HasTopBread => topBread != null;
    public bool HasSauce => GetIngredientCount(IngredientType.Sauce) > 0;
    public bool IsClosed => HasTopBread;
    public bool IsDelivered { get; private set; }
    public bool IsInGrill { get; private set; }
    public SandwichCookState CookState { get; private set; }
    public float CookProgressSeconds { get; private set; }
    public int IngredientCount => ingredients.Count;

    public void Initialize(SandwichBoard board, IngredientInstance bread)
    {
        ownerBoard = board;
        bottomBread = bread;
        topBread = null;
        IsDelivered = false;
        IsInGrill = false;
        CookState = SandwichCookState.Raw;
        CookProgressSeconds = 0f;
        ingredients.Clear();
        ingredientCounts.Clear();
        ApplyCookVisuals();
    }

    public void RegisterIngredient(IngredientInstance ingredient)
    {
        if (ingredient == null || ingredients.Contains(ingredient))
            return;

        ingredients.Add(ingredient);

        if (!ingredientCounts.ContainsKey(ingredient.Type))
            ingredientCounts.Add(ingredient.Type, 0);

        ingredientCounts[ingredient.Type]++;
    }

    public int GetIngredientCount(IngredientType ingredientType)
    {
        return ingredientCounts.TryGetValue(ingredientType, out int count)
            ? count
            : 0;
    }

    public int GetNextIngredientLayerIndex(IngredientType ingredientType)
    {
        if (!IsStackedIngredient(ingredientType))
            return 0;

        if (!TryGetLastStackedIngredientType(out IngredientType lastIngredientType))
            return 1;

        int highestLayerIndex = GetHighestIngredientLayerIndex();
        return lastIngredientType == ingredientType
            ? highestLayerIndex
            : highestLayerIndex + 1;
    }

    public int GetHighestIngredientLayerIndex()
    {
        int layerIndex = 0;
        IngredientType previousType = default;
        bool hasStackedIngredient = false;

        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientType currentType = ingredients[i].Type;
            if (!IsStackedIngredient(currentType))
                continue;

            if (!hasStackedIngredient)
            {
                layerIndex = 1;
                previousType = currentType;
                hasStackedIngredient = true;
                continue;
            }

            if (currentType == previousType)
                continue;

            layerIndex++;
            previousType = currentType;
        }

        return layerIndex;
    }

    private bool TryGetLastStackedIngredientType(out IngredientType ingredientType)
    {
        for (int i = ingredients.Count - 1; i >= 0; i--)
        {
            if (!IsStackedIngredient(ingredients[i].Type))
                continue;

            ingredientType = ingredients[i].Type;
            return true;
        }

        ingredientType = default;
        return false;
    }

    private static bool IsStackedIngredient(IngredientType ingredientType)
    {
        return ingredientType != IngredientType.Sauce;
    }

    public void RegisterTopBread(IngredientInstance bread)
    {
        topBread = bread;
        ApplyCookVisual(bread);
    }

    public void MarkDelivered()
    {
        IsDelivered = true;
    }

    public void MarkEnteredGrill()
    {
        IsInGrill = true;
    }

    public void MarkRemovedFromGrill()
    {
        IsInGrill = false;
    }

    public void SetCookState(SandwichCookState cookState)
    {
        if (CookState == cookState)
            return;

        CookState = cookState;
        ApplyCookVisuals();
    }

    public void AddCookProgress(float seconds)
    {
        CookProgressSeconds += Mathf.Max(0f, seconds);
    }

    public void DetachFromBoard()
    {
        if (ownerBoard != null)
            ownerBoard.ReleaseSandwich(this);

        ownerBoard = null;
    }

    public void AttachToBoard(SandwichBoard board)
    {
        ownerBoard = board;
    }

    private void OnDisable()
    {
        if (ownerBoard != null)
            ownerBoard.ReleaseSandwich(this);
    }

    private void ApplyCookVisuals()
    {
        ApplyCookVisual(bottomBread);
        ApplyCookVisual(topBread);
    }

    private void ApplyCookVisual(IngredientInstance ingredient)
    {
        if (ingredient == null)
            return;

        BreadCookVisual cookVisual = ingredient.GetComponent<BreadCookVisual>();
        if (cookVisual != null)
            cookVisual.Apply(CookState);
    }
}
