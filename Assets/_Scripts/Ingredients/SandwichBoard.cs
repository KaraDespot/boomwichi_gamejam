/*
 * SandwichBoard
 * Назначение: правила размещения ингредиентов на доске сборки сендвича.
 * Что делает: назначает первый хлеб нижним root, кладёт ингредиенты дочерними к нему и закрывает сендвич верхним хлебом.
 * Связи: вызывается DropZone типа Board, работает с DraggableObject и IngredientInstance.
 * Паттерны: Domain Controller для сборки сендвича.
 */

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SandwichBoard : MonoBehaviour
{
    [Header("Points")]
    [Tooltip("Фиксированная точка нижнего хлеба на доске.")]
    [SerializeField] private Transform bottomBreadPoint;

    [Header("Stack")]
    [Tooltip("Высота между слоями ингредиентов.")]
    [SerializeField] private float layerHeight = 0.04f;

    [Tooltip("Граница свободного размещения внутри нижнего хлеба в локальных координатах хлеба.")]
    [SerializeField] private Vector2 placementHalfExtents = new Vector2(0.36f, 0.36f);

    [Tooltip("Небольшой зазор над верхней поверхностью нижнего хлеба, чтобы первый ингредиент не проваливался.")]
    [SerializeField] private float surfacePadding = 0.01f;

    [Tooltip("Зазор соуса над хлебом. Соус всегда лежит на поверхности хлеба и не создаёт новый слой начинки.")]
    [SerializeField] private float sauceSurfacePadding = 0.004f;

    [Header("Placement Animation")]
    [Tooltip("Включить короткое падение ингредиента после отпускания мыши.")]
    [SerializeField] private bool animatePlacement = true;

    [Tooltip("С какой высоты ингредиент падает на итоговую позицию.")]
    [SerializeField] private float dropAnimationHeight = 0.22f;

    [Tooltip("Длительность падения ингредиента.")]
    [SerializeField] private float dropAnimationDuration = 0.16f;

    private SandwichState currentSandwich;

    public bool HasBottomBread => currentSandwich != null && currentSandwich.HasBottomBread;
    public bool HasTopBread => currentSandwich != null && currentSandwich.HasTopBread;
    public bool IsClosed => currentSandwich != null && currentSandwich.IsClosed;

    public bool CanAccept(DraggableObject draggableObject)
    {
        if (draggableObject == null)
            return false;

        SauceDispenser sauceDispenser = draggableObject.GetComponent<SauceDispenser>();
        if (sauceDispenser != null)
            return CanAcceptSauceDispenser(sauceDispenser);

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

        if (ingredient.Type == IngredientType.Sauce && currentSandwich.HasSauce)
            return false;

        return true;
    }

    public bool Accept(DraggableObject draggableObject)
    {
        return Accept(draggableObject, draggableObject != null ? draggableObject.transform.position : transform.position);
    }

    public bool Accept(DraggableObject draggableObject, Vector3 dropWorldPoint)
    {
        if (!CanAccept(draggableObject))
            return false;

        SauceDispenser sauceDispenser = draggableObject.GetComponent<SauceDispenser>();
        if (sauceDispenser != null)
            return DispenseSauce(draggableObject, sauceDispenser, dropWorldPoint);

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
            if (ingredient.Type == IngredientType.Sauce)
                PlaceSauce(draggableObject, ingredient, dropWorldPoint);
            else
                PlaceIngredient(draggableObject, ingredient, dropWorldPoint);
        }

        return true;
    }

    public void ReleaseSandwich(SandwichState sandwichState)
    {
        if (sandwichState == currentSandwich)
            currentSandwich = null;
    }

    private bool CanAcceptSauceDispenser(SauceDispenser sauceDispenser)
    {
        return currentSandwich != null && sauceDispenser.CanDispense(currentSandwich);
    }

    private bool DispenseSauce(DraggableObject dispenserDraggable, SauceDispenser sauceDispenser, Vector3 dropWorldPoint)
    {
        if (!sauceDispenser.TryCreateSauce(out DraggableObject sauceDraggable, out IngredientInstance sauceIngredient))
        {
            dispenserDraggable.HandleFailedDrop();
            return false;
        }

        PlaceSauce(sauceDraggable, sauceIngredient, dropWorldPoint);
        dispenserDraggable.CancelDrag();
        return true;
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

    private void PlaceIngredient(DraggableObject draggableObject, IngredientInstance ingredient, Vector3 dropWorldPoint)
    {
        Transform root = currentSandwich.transform;
        int layerIndex = currentSandwich.GetNextIngredientLayerIndex(ingredient.Type);
        Vector3 targetPosition = GetFreePlacementPosition(root, dropWorldPoint, layerIndex);

        Place(draggableObject, ingredient, targetPosition, root.rotation, root, true);
        currentSandwich.RegisterIngredient(ingredient);
    }

    private void PlaceSauce(DraggableObject draggableObject, IngredientInstance ingredient, Vector3 dropWorldPoint)
    {
        Transform root = currentSandwich.transform;
        float heightOffset = GetBottomBreadSurfaceOffset(root) + sauceSurfacePadding;
        Vector3 targetPosition = root.position + root.up * heightOffset;

        Place(draggableObject, ingredient, targetPosition, root.rotation, root, true);
        currentSandwich.RegisterIngredient(ingredient);
    }

    private void PlaceTopBread(DraggableObject draggableObject, IngredientInstance ingredient)
    {
        Transform root = currentSandwich.transform;
        int layerIndex = GetTopBreadLayerIndex();
        Vector3 targetPosition = root.position + root.up * GetLayerHeightOffset(root, layerIndex);

        ingredient.AssignBreadRole(BreadRole.Top);
        Place(draggableObject, ingredient, targetPosition, root.rotation, root, true);
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

    private void Place(DraggableObject draggableObject, IngredientInstance ingredient, Vector3 position, Quaternion rotation, Transform parent, bool shouldAnimate = false)
    {
        bool isRoot = parent == draggableObject.transform;

        draggableObject.FinishDrag();
        draggableObject.transform.rotation = rotation;

        if (!isRoot)
            draggableObject.transform.SetParent(parent, true);

        draggableObject.SetCanDrag(isRoot);
        draggableObject.SetPhysicsLocked(true);

        if (isRoot)
            draggableObject.SetFailedDropAction(DragFailedDropAction.ReturnToStart);

        if (shouldAnimate && animatePlacement)
            StartCoroutine(AnimatePlacement(draggableObject.transform, position));
        else
            draggableObject.MoveTo(position);

        ingredient.MarkPlacedOnSandwich(currentSandwich);
    }

    private Vector3 GetFreePlacementPosition(Transform root, Vector3 dropWorldPoint, int layerIndex)
    {
        return GetBreadSurfacePlacementPosition(root, dropWorldPoint, GetLayerHeightOffset(root, layerIndex));
    }

    private Vector3 GetBreadSurfacePlacementPosition(Transform root, Vector3 dropWorldPoint, float heightOffset)
    {
        Vector3 localDropPoint = root.InverseTransformPoint(dropWorldPoint);
        localDropPoint.y = 0f;
        localDropPoint = ClampToBreadBounds(localDropPoint);

        Vector3 horizontalPosition = root.TransformPoint(localDropPoint);
        return horizontalPosition + root.up * heightOffset;
    }

    private float GetLayerHeightOffset(Transform root, int layerIndex)
    {
        int additionalLayerCount = Mathf.Max(0, layerIndex - 1);
        return GetBottomBreadSurfaceOffset(root) + surfacePadding + layerHeight * additionalLayerCount;
    }

    private float GetBottomBreadSurfaceOffset(Transform root)
    {
        Collider bottomBreadCollider = currentSandwich.BottomBread != null
            ? currentSandwich.BottomBread.GetComponent<Collider>()
            : root.GetComponent<Collider>();

        if (bottomBreadCollider is BoxCollider boxCollider)
        {
            Vector3 localTopPoint = boxCollider.center + Vector3.up * boxCollider.size.y * 0.5f;
            Vector3 worldTopPoint = root.TransformPoint(localTopPoint);
            return Mathf.Max(0f, Vector3.Dot(worldTopPoint - root.position, root.up));
        }

        if (bottomBreadCollider != null)
            return Mathf.Max(0f, bottomBreadCollider.bounds.max.y - root.position.y);

        return layerHeight;
    }

    private Vector3 ClampToBreadBounds(Vector3 localPoint)
    {
        if (placementHalfExtents.x <= 0f || placementHalfExtents.y <= 0f)
            return Vector3.zero;

        float normalizedX = localPoint.x / placementHalfExtents.x;
        float normalizedZ = localPoint.z / placementHalfExtents.y;
        float normalizedMagnitude = Mathf.Sqrt(normalizedX * normalizedX + normalizedZ * normalizedZ);

        if (normalizedMagnitude <= 1f)
            return localPoint;

        normalizedX /= normalizedMagnitude;
        normalizedZ /= normalizedMagnitude;

        localPoint.x = normalizedX * placementHalfExtents.x;
        localPoint.z = normalizedZ * placementHalfExtents.y;
        return localPoint;
    }

    private int GetTopBreadLayerIndex()
    {
        return currentSandwich.GetHighestIngredientLayerIndex() + 1;
    }

    private IEnumerator AnimatePlacement(Transform placedTransform, Vector3 targetPosition)
    {
        Vector3 startPosition = targetPosition + Vector3.up * dropAnimationHeight;
        float elapsed = 0f;

        placedTransform.position = startPosition;

        while (elapsed < dropAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dropAnimationDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            placedTransform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        placedTransform.position = targetPosition;
    }
}
