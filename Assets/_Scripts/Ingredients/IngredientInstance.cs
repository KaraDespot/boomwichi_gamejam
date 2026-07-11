/*
 * IngredientInstance
 * Назначение: runtime-данные конкретного созданного ингредиента.
 * Что делает: хранит тип, роль хлеба и факт размещения на сендвиче.
 * Связи: создаётся IngredientContainer, читается SandwichBoard.
 * Паттерны: Data Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class IngredientInstance : MonoBehaviour
{
    [Header("Ingredient")]
    [Tooltip("Тип ингредиента, который будет учитываться при сборке сендвича.")]
    [SerializeField] private IngredientType ingredientType = IngredientType.Bread;

    [Tooltip("Роль хлеба. Для остальных ингредиентов оставлять None.")]
    [SerializeField] private BreadRole breadRole = BreadRole.None;

    public IngredientType Type => ingredientType;
    public BreadRole BreadRole => breadRole;
    public bool IsPlacedOnSandwich { get; private set; }
    public SandwichState SandwichState { get; private set; }

    public void Initialize(IngredientType type, BreadRole role)
    {
        ingredientType = type;
        breadRole = role;
        IsPlacedOnSandwich = false;
        SandwichState = null;
    }

    public void AssignBreadRole(BreadRole role)
    {
        breadRole = role;
    }

    public void MarkPlacedOnSandwich(SandwichState sandwichState)
    {
        SandwichState = sandwichState;
        IsPlacedOnSandwich = true;
    }
}
