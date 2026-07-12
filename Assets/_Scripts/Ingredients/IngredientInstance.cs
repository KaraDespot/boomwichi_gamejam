/*
 * IngredientInstance
 * Назначение: runtime-данные конкретного созданного ингредиента.
 * Что делает: хранит тип, роль хлеба, факт размещения на сендвиче и флаги грязи.
 * Связи: создаётся IngredientContainer, читается SandwichBoard и OrderEvaluator.
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

    [Tooltip("Вариант соуса. Используется только для IngredientType.Sauce.")]
    [SerializeField] private SauceType sauceType = SauceType.None;

    public IngredientType Type => ingredientType;
    public BreadRole BreadRole => breadRole;
    public SauceType SauceType => sauceType;
    public bool IsPlacedOnSandwich { get; private set; }
    public bool IsMoldy { get; private set; }
    public bool IsRoachContaminated { get; private set; }
    public bool IsDirty => IsMoldy || IsRoachContaminated;
    public SandwichState SandwichState { get; private set; }

    /// <summary> Может ли этот ингредиент получить плесень при спавне. </summary>
    public bool CanBecomeMoldy => ingredientType != IngredientType.Sauce;

    public void Initialize(IngredientType type, BreadRole role, SauceType sauce = SauceType.None)
    {
        ingredientType = type;
        breadRole = role;
        sauceType = type == IngredientType.Sauce ? sauce : SauceType.None;
        IsPlacedOnSandwich = false;
        IsMoldy = false;
        IsRoachContaminated = false;
        SandwichState = null;
    }

    public void SetMoldy(bool isMoldy)
    {
        if (!CanBecomeMoldy)
        {
            IsMoldy = false;
            return;
        }

        IsMoldy = isMoldy;
    }

    public void ClearMold()
    {
        IsMoldy = false;
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
    public void MarkRoachContaminated()
    {
        IsRoachContaminated = true;
    }

    public void ClearRoachContamination()
    {
        IsRoachContaminated = false;
    }
}
