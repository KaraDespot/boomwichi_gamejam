/*
 * SandwichState
 * Назначение: состояние текущего собранного сендвича, живущее на нижнем хлебе-root.
 * Что делает: хранит нижний хлеб, верхний хлеб, список слоёв и сообщает доске, когда root-сендвич исчезает.
 * Связи: создаётся SandwichBoard на нижнем хлебе, читается будущими зонами грильницы/упаковки/оценки заказа.
 * Паттерны: Aggregate Root State.
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SandwichState : MonoBehaviour
{
    private readonly List<IngredientInstance> ingredients = new List<IngredientInstance>();

    private SandwichBoard ownerBoard;
    private IngredientInstance bottomBread;
    private IngredientInstance topBread;

    public IngredientInstance BottomBread => bottomBread;
    public IngredientInstance TopBread => topBread;
    public IReadOnlyList<IngredientInstance> Ingredients => ingredients;
    public bool HasBottomBread => bottomBread != null;
    public bool HasTopBread => topBread != null;
    public bool IsClosed => HasTopBread;
    public bool HasFallenMold { get; private set; }
    public int IngredientCount => ingredients.Count;

    public void Initialize(SandwichBoard board, IngredientInstance bread)
    {
        ownerBoard = board;
        bottomBread = bread;
        topBread = null;
        HasFallenMold = false;
        ingredients.Clear();
    }

    public void RegisterIngredient(IngredientInstance ingredient)
    {
        if (ingredient == null || ingredients.Contains(ingredient))
            return;

        ingredients.Add(ingredient);
    }

    public void RegisterTopBread(IngredientInstance bread)
    {
        topBread = bread;
    }

    /// <summary>
    /// Плесень упала на сендвич при очистке над доской. Исправить нельзя.
    /// </summary>
    public void MarkFallenMold()
    {
        HasFallenMold = true;
    }

    public void DetachFromBoard()
    {
        ownerBoard = null;
    }

    private void OnDisable()
    {
        if (ownerBoard != null)
            ownerBoard.ReleaseSandwich(this);
    }
}
