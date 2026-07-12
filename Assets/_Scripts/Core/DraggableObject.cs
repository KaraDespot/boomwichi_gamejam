/*
 * DraggableObject
 * Назначение: маркер и локальное состояние объекта, который можно перетаскивать по столу.
 * Что делает: запоминает стартовую позицию, двигается за DragController и корректно завершает/откатывает drag.
 * Связи: выбирается InputRaycaster, управляется DragController, может быть принят DropZone.
 * Паттерны: Component, Encapsulation of state.
 */

using System;
using UnityEngine;

public enum DragFailedDropAction
{
    ReturnToStart,
    DeactivateObject,
    DestroyObject
}

public enum DragEndResult
{
    Completed,
    Cancelled
}

[DisallowMultipleComponent]
public class DraggableObject : MonoBehaviour
{
    [Header("Drag")]
    [Tooltip("Высота объекта над точкой стола во время перетаскивания.")]
    [SerializeField] private float dragHeight = 0.4f;

    [Tooltip("Что сделать, если объект отпустили не над подходящей зоной.")]
    [SerializeField] private DragFailedDropAction failedDropAction = DragFailedDropAction.ReturnToStart;

    [Tooltip("На время drag коллайдеры объекта отключаются, чтобы raycast попадал в стол и drop zones, а не в сам объект.")]
    [SerializeField] private bool disableCollidersWhileDragging = true;

    [Tooltip("Плавно вести объект к курсору вместо мгновенного телепорта.")]
    [SerializeField] private bool smoothDragMovement = true;

    [Tooltip("Скорость плавного следования за курсором.")]
    [SerializeField] private float dragFollowSpeed = 28f;

    private Rigidbody cachedRigidbody;
    private Collider[] cachedColliders;
    private bool hadRigidbody;
    private bool previousKinematicState;
    private bool previousGravityState;
    private bool initialKinematicState;
    private bool initialGravityState;
    private bool physicsLocked;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetPosition;
    private bool canDrag = true;
    private bool hasTargetPosition;

    public float DragHeight => dragHeight;
    public bool CanDrag => canDrag && isActiveAndEnabled;
    public bool IsDragging { get; private set; }

    public event Action<DraggableObject> DragStarted;
    public event Action<DraggableObject, DragEndResult> DragEnded;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        RefreshCachedColliders();
        hadRigidbody = cachedRigidbody != null;

        if (!hadRigidbody)
            return;

        initialKinematicState = cachedRigidbody.isKinematic;
        initialGravityState = cachedRigidbody.useGravity;
    }

    private void Update()
    {
        if (!IsDragging || !smoothDragMovement || !hasTargetPosition)
            return;

        float progress = 1f - Mathf.Exp(-dragFollowSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, progress);
    }

    public void SetCanDrag(bool isAllowed)
    {
        canDrag = isAllowed;
    }

    public void SetFailedDropAction(DragFailedDropAction action)
    {
        failedDropAction = action;
    }

    public void SetPhysicsLocked(bool isLocked)
    {
        physicsLocked = isLocked;

        if (!hadRigidbody)
            return;

        if (physicsLocked)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
            return;
        }

        cachedRigidbody.isKinematic = initialKinematicState;
        cachedRigidbody.useGravity = initialGravityState;
    }

    public bool BeginDrag()
    {
        if (!CanDrag)
            return false;

        RefreshCachedColliders();
        IsDragging = true;
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = startPosition;
        hasTargetPosition = true;

        SetCollidersEnabled(!disableCollidersWhileDragging);

        if (!hadRigidbody)
        {
            DragStarted?.Invoke(this);
            return true;
        }

        previousKinematicState = cachedRigidbody.isKinematic;
        previousGravityState = cachedRigidbody.useGravity;
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;

        DragStarted?.Invoke(this);
        return true;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        if (IsDragging && smoothDragMovement)
        {
            targetPosition = worldPosition;
            hasTargetPosition = true;
            return;
        }

        transform.position = worldPosition;
    }

    public void CompleteDrop(Vector3 worldPosition)
    {
        hasTargetPosition = false;
        transform.position = worldPosition;
        FinishDrag();
        DragEnded?.Invoke(this, DragEndResult.Completed);
    }

    public void CancelDrag()
    {
        hasTargetPosition = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        FinishDrag();
        DragEnded?.Invoke(this, DragEndResult.Cancelled);
    }

    public void HandleFailedDrop()
    {
        switch (failedDropAction)
        {
            case DragFailedDropAction.ReturnToStart:
                CancelDrag();
                break;

            case DragFailedDropAction.DeactivateObject:
                hasTargetPosition = false;
                FinishDrag();
                DragEnded?.Invoke(this, DragEndResult.Cancelled);
                gameObject.SetActive(false);
                break;

            case DragFailedDropAction.DestroyObject:
                hasTargetPosition = false;
                FinishDrag();
                DragEnded?.Invoke(this, DragEndResult.Cancelled);
                Destroy(gameObject);
                break;
        }
    }

    public void FinishDrag()
    {
        IsDragging = false;
        SetCollidersEnabled(true);

        if (!hadRigidbody)
            return;

        if (physicsLocked)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
            return;
        }

        cachedRigidbody.isKinematic = previousKinematicState;
        cachedRigidbody.useGravity = previousGravityState;
    }

    private void RefreshCachedColliders()
    {
        cachedColliders = GetComponentsInChildren<Collider>();
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (cachedColliders == null)
            return;

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
                cachedColliders[i].enabled = isEnabled;
        }
    }
}
