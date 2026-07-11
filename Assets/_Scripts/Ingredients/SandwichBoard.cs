/*
 * SandwichBoard
 * Назначение: правила размещения ингредиентов на доске сборки сендвича.
 * Что делает: назначает первый хлеб нижним root, кладёт ингредиенты дочерними к нему и закрывает сендвич верхним хлебом.
 * Связи: вызывается DropZone типа Board, работает с DraggableObject и IngredientInstance.
 * Паттерны: Domain Controller для сборки сендвича.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class SandwichBoard : MonoBehaviour
{
    [Header("Points")]
    [Tooltip("Фиксированная точка нижнего хлеба на доске.")]
    [SerializeField] private Transform bottomBreadPoint;

    [Header("Stack")]
    [Tooltip("Высота между слоями ингредиентов.")]
    [SerializeField] private float layerHeight = 0.08f;

    [Tooltip("Небольшое смещение ингредиентов по X/Z, чтобы слои не лежали идеально в одной точке.")]
    [SerializeField] private Vector2 ingredientSpread = new Vector2(0.18f, 0.12f);

    private SandwichState currentSandwich;

    public bool HasBottomBread => currentSandwich != null && currentSandwich.HasBottomBread;
    public bool HasTopBread => currentSandwich != null && currentSandwich.HasTopBread;
    public bool IsClosed => currentSandwich != null && currentSandwich.IsClosed;

    public bool CanAccept(DraggableObject draggableObject)
    {
        if (draggableObject == null)
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        if (sandwichState != null && sandwichState == currentSandwich)
            return true;

        IngredientInstance ingredient = draggableObject.GetComponent<IngredientInstance>();
        if (ingredient == null || ingredient.IsPlacedOnSandwich)
            return false;

        if (!HasBottomBread)
            return ingredient.Type == IngredientType.Bread;

        if (IsClosed)
            return false;

        return true;
    }

    public bool Accept(DraggableObject draggableObject)
    {
        if (!CanAccept(draggableObject))
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        if (sandwichState != null && sandwichState == currentSandwich)
        {
            RepositionCurrentSandwich(draggableObject);
            return true;
        }

        IngredientInstance ingredient = draggableObject.GetComponent<IngredientInstance>();
        if (ingredient.Type == IngredientType.Bread)
        {
            if (!HasBottomBread)
                PlaceBottomBread(draggableObject, ingredient);
            else
                PlaceTopBread(draggableObject, ingredient);
        }
        else
        {
            PlaceIngredient(draggableObject, ingredient);
        }

        return true;
    }

    public void ReleaseSandwich(SandwichState sandwichState)
    {
        if (sandwichState == currentSandwich)
            currentSandwich = null;
    }

    private void PlaceBottomBread(DraggableObject draggableObject, IngredientInstance ingredient)
    {
        Transform point = bottomBreadPoint != null ? bottomBreadPoint : transform;
        ingredient.AssignBreadRole(BreadRole.Bottom);

        currentSandwich = draggableObject.GetComponent<SandwichState>();
        if (currentSandwich == null)
            currentSandwich = draggableObject.gameObject.AddComponent<SandwichState>();

        currentSandwich.Initialize(this, ingredient);

        Place(draggableObject, ingredient, point.position, point.rotation, draggableObject.transform);
        draggableObject.SetCanDrag(true);
    }

    private void PlaceIngredient(DraggableObject draggableObject, IngredientInstance ingredient)
    {
        Transform root = currentSandwich.transform;
        int layerIndex = currentSandwich.IngredientCount;
        Vector3 targetPosition = root.position + Vector3.up * layerHeight * (layerIndex + 1);
        targetPosition += GetLayerOffset(layerIndex);

        Place(draggableObject, ingredient, targetPosition, root.rotation, root);
        currentSandwich.RegisterIngredient(ingredient);
    }

    private void PlaceTopBread(DraggableObject draggableObject, IngredientInstance ingredient)
    {
        Transform root = currentSandwich.transform;
        int layerIndex = currentSandwich.IngredientCount;
        Vector3 targetPosition = root.position + Vector3.up * layerHeight * (layerIndex + 1);

        ingredient.AssignBreadRole(BreadRole.Top);
        Place(draggableObject, ingredient, targetPosition, root.rotation, root);
        currentSandwich.RegisterTopBread(ingredient);
    }

    private void RepositionCurrentSandwich(DraggableObject draggableObject)
    {
        Transform point = bottomBreadPoint != null ? bottomBreadPoint : transform;
        draggableObject.CompleteDrop(point.position);
        draggableObject.transform.rotation = point.rotation;
        draggableObject.SetCanDrag(true);
        draggableObject.SetFailedDropAction(DragFailedDropAction.ReturnToStart);
        draggableObject.SetPhysicsLocked(true);
    }

    private void Place(DraggableObject draggableObject, IngredientInstance ingredient, Vector3 position, Quaternion rotation, Transform parent)
    {
        bool isRoot = parent == draggableObject.transform;

        draggableObject.CompleteDrop(position);
        draggableObject.transform.rotation = rotation;

        if (!isRoot)
            draggableObject.transform.SetParent(parent, true);

        draggableObject.SetCanDrag(isRoot);
        draggableObject.SetPhysicsLocked(true);

        if (isRoot)
            draggableObject.SetFailedDropAction(DragFailedDropAction.ReturnToStart);

        ingredient.MarkPlacedOnSandwich(currentSandwich);
    }

    private Vector3 GetLayerOffset(int layerIndex)
    {
        int patternIndex = layerIndex % 4;

        switch (patternIndex)
        {
            case 0:
                return new Vector3(ingredientSpread.x, 0f, ingredientSpread.y);
            case 1:
                return new Vector3(-ingredientSpread.x, 0f, ingredientSpread.y);
            case 2:
                return new Vector3(ingredientSpread.x, 0f, -ingredientSpread.y);
            default:
                return new Vector3(-ingredientSpread.x, 0f, -ingredientSpread.y);
        }
    }
}
