/*
 * PackageZone
 * Назначение: зона сдачи сендвича клиенту через пакет.
 * Что делает: принимает root-сендвич, запускает OrderEvaluator, начисляет чаевые и завершает текущий заказ.
 * Связи: вызывается DropZone типа Package, использует OrderManager и OrderEvaluator.
 * Паттерны: Domain Controller, Event Publisher.
 */

using System;
using UnityEngine;

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

    [Header("Order Services")]
    [Tooltip("Менеджер текущего заказа из Flow. Если пусто, будет найден на сцене.")]
    [SerializeField] private OrderManager orderManager;

    [Tooltip("Сервис проверки заказа. Если пусто, будет найден на сцене или создан рядом с пакетом.")]
    [SerializeField] private OrderEvaluator orderEvaluator;

    [Tooltip("Создавать недостающий локальный сервис оценки на этом объекте в Play Mode.")]
    [SerializeField] private bool createRuntimeServicesIfMissing = true;

    public event Action<OrderEvaluationResult> SandwichDelivered;

    public bool HasDeliveryResult { get; private set; }
    public OrderEvaluationResult LastDeliveryResult { get; private set; }

    private void Awake()
    {
        ResolveServices();
    }

    public bool CanAccept(DraggableObject draggableObject)
    {
        if (draggableObject == null)
            return false;

        ResolveServices();
        if (orderEvaluator == null)
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        return sandwichState != null &&
            sandwichState.HasBottomBread &&
            !sandwichState.IsDelivered &&
            !sandwichState.IsInGrill;
    }

    public bool Accept(DraggableObject draggableObject)
    {
        return Accept(draggableObject, draggableObject != null ? draggableObject.transform.position : transform.position);
    }

    public bool Accept(DraggableObject draggableObject, Vector3 dropWorldPoint)
    {
        if (!CanAccept(draggableObject))
            return false;

        ResolveServices();
        if (orderEvaluator == null)
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        OrderDataAsset order = GetCurrentOrder();
        LastDeliveryResult = orderEvaluator.Evaluate(order, sandwichState);
        HasDeliveryResult = true;

        Vector3 targetPosition = GetSnapPosition();
        draggableObject.transform.rotation = transform.rotation;
        draggableObject.CompleteDrop(targetPosition);
        draggableObject.SetCanDrag(false);
        draggableObject.SetPhysicsLocked(true);
        sandwichState.MarkDelivered();

        if (orderManager != null)
            orderManager.CompleteCurrentOrder(LastDeliveryResult.TipAmount);

        SandwichDelivered?.Invoke(LastDeliveryResult);
        Debug.Log($"{name}: заказ сдан. {LastDeliveryResult.Summary}", this);

        if (deactivateDeliveredSandwich)
            draggableObject.gameObject.SetActive(false);

        return true;
    }

    public Vector3 GetSnapPosition()
    {
        Vector3 basePosition = snapPoint != null ? snapPoint.position : transform.position;
        return basePosition + snapOffset;
    }

    private void ResolveServices()
    {
        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        if (orderEvaluator == null)
            orderEvaluator = FindFirstObjectByType<OrderEvaluator>();

        if (!createRuntimeServicesIfMissing)
            return;

        if (orderEvaluator == null)
            orderEvaluator = gameObject.AddComponent<OrderEvaluator>();
    }

    private OrderDataAsset GetCurrentOrder()
    {
        return orderManager != null ? orderManager.CurrentOrder : null;
    }
}
