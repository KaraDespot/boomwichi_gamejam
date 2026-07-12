/*
 * OrderDefinition
 * Назначение: общие типы данных для заказов Boomwichi.
 * Что делает: enum прожарки и структура требования по ингредиенту.
 * Связи: OrderDataAsset, OrderManager, OrderEvaluator (Dev 1).
 * Паттерны: Shared Contract.
 */

using System;
using UnityEngine;

public enum ToastState
{
    Raw,
    Toasted,
    Burnt,
}

[Serializable]
public struct OrderIngredientRequirement
{
    [Tooltip("Тип ингредиента в заказе.")]
    public IngredientType type;

    [Tooltip("Сколько кусочков нужно положить.")]
    [Min(1)]
    public int count;
}
