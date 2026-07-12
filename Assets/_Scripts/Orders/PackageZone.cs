/*
 * PackageZone
 * Назначение: зона сдачи сендвича клиенту через пакет.
 * Что делает: принимает root-сендвич, фиксирует результат выдачи и убирает сендвич со сцены.
 * Связи: вызывается DropZone типа Package, читает SandwichState для первичной оценки заказа.
 * Паттерны: Domain Controller, Event Publisher.
 */

using System;
using UnityEngine;

public enum SandwichDeliveryRating
{
    Positive,
    Negative
}

public readonly struct SandwichDeliveryResult
{
    public SandwichDeliveryResult(SandwichState sandwichState, SandwichDeliveryRating rating, string reason)
    {
        SandwichState = sandwichState;
        Rating = rating;
        Reason = reason;
    }

    public SandwichState SandwichState { get; }
    public SandwichDeliveryRating Rating { get; }
    public string Reason { get; }
    public bool IsPositive => Rating == SandwichDeliveryRating.Positive;
}

[DisallowMultipleComponent]
public class PackageZone : MonoBehaviour
{
    [Header("Package")]
    [Tooltip("Точка, куда сендвич ставится перед выдачей. Если пусто, используется позиция пакета.")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("Дополнительное смещение от snap point.")]
    [SerializeField] private Vector3 snapOffset = Vector3.zero;

    [Tooltip("Отключать сданный сендвич после выдачи заказа.")]
    [SerializeField] private bool deactivateDeliveredSandwich = true;

    [Header("Temporary Rating Rules")]
    [Tooltip("Без верхнего хлеба сендвич принимается, но получает негативную оценку.")]
    [SerializeField] private bool requireTopBreadForPositiveRating = true;

    [Tooltip("Без начинки сендвич принимается, но получает негативную оценку.")]
    [SerializeField] private bool requireFillingForPositiveRating = true;

    [Tooltip("Сырой сендвич принимается, но получает негативную оценку.")]
    [SerializeField] private bool requireCookedForPositiveRating = true;

    public event Action<SandwichDeliveryResult> SandwichDelivered;

    public bool HasDeliveryResult { get; private set; }
    public SandwichDeliveryResult LastDeliveryResult { get; private set; }

    public bool CanAccept(DraggableObject draggableObject)
    {
        return draggableObject != null && draggableObject.GetComponent<SandwichState>() != null;
    }

    public bool Accept(DraggableObject draggableObject)
    {
        return Accept(draggableObject, draggableObject != null ? draggableObject.transform.position : transform.position);
    }

    public bool Accept(DraggableObject draggableObject, Vector3 dropWorldPoint)
    {
        if (!CanAccept(draggableObject))
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        LastDeliveryResult = EvaluateSandwich(sandwichState);
        HasDeliveryResult = true;

        Vector3 targetPosition = GetSnapPosition();
        draggableObject.transform.rotation = transform.rotation;
        draggableObject.CompleteDrop(targetPosition);
        draggableObject.SetCanDrag(false);
        draggableObject.SetPhysicsLocked(true);
        sandwichState.MarkDelivered();

        SandwichDelivered?.Invoke(LastDeliveryResult);
        Debug.Log($"{name}: сендвич сдан. Оценка: {LastDeliveryResult.Rating}. Причина: {LastDeliveryResult.Reason}.", this);

        if (deactivateDeliveredSandwich)
            draggableObject.gameObject.SetActive(false);

        return true;
    }

    public Vector3 GetSnapPosition()
    {
        Vector3 basePosition = snapPoint != null ? snapPoint.position : transform.position;
        return basePosition + snapOffset;
    }

    private SandwichDeliveryResult EvaluateSandwich(SandwichState sandwichState)
    {
        if (requireTopBreadForPositiveRating && !sandwichState.HasTopBread)
            return new SandwichDeliveryResult(sandwichState, SandwichDeliveryRating.Negative, "Нет верхнего хлеба.");

        if (requireFillingForPositiveRating && sandwichState.IngredientCount == 0)
            return new SandwichDeliveryResult(sandwichState, SandwichDeliveryRating.Negative, "Нет начинки.");

        if (requireCookedForPositiveRating && sandwichState.CookState == SandwichCookState.Raw)
            return new SandwichDeliveryResult(sandwichState, SandwichDeliveryRating.Negative, "Сендвич сырой.");

        return new SandwichDeliveryResult(sandwichState, SandwichDeliveryRating.Positive, "Сендвич принят.");
    }
}
