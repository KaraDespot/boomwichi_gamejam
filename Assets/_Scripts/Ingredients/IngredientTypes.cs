/*
 * IngredientTypes
 * Назначение: общие перечисления для ингредиентов Boomwichi.
 * Что делает: задаёт тип ингредиента и роль хлеба без привязки к prefab или сцене.
 * Связи: используется IngredientInstance, IngredientContainer и SandwichBoard.
 * Паттерны: Shared Contract.
 */

public enum IngredientType
{
    Bread,
    Tomato,
    Cucumber,
    Olive,
    Sauce,
    Cheese,
    Sausage,
    Onion
}

public enum BreadRole
{
    None,
    Bottom,
    Top
}

public enum SauceType
{
    None,
    Red,
    White
}
