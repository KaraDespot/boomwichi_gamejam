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

[DisallowMultipleComponent]
public class DraggableObject : MonoBehaviour
{
    [Header("Drag")]
    [Tooltip("Высота объекта над точкой стола во время перетаскивания.")]
    [SerializeField] private float dragHeight = 0.18f;

    [Tooltip("Что сделать, если объект отпустили не над подходящей зоной.")]
    [SerializeField] private DragFailedDropAction failedDropAction = DragFailedDropAction.ReturnToStart;

    [Tooltip("На время drag коллайдеры объекта отключаются, чтобы raycast попадал в стол и drop zones, а не в сам объект.")]
    [SerializeField] private bool disableCollidersWhileDragging = true;

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
    private bool canDrag = true;

    public float DragHeight => dragHeight;
    public bool CanDrag => canDrag && isActiveAndEnabled;
    public bool IsDragging { get; private set; }

    public event Action<DraggableObject> DragStarted;

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

    public void BeginDrag()
    {
        if (!CanDrag)
            return;

        RefreshCachedColliders();
        IsDragging = true;
        startPosition = transform.position;
        startRotation = transform.rotation;

        SetCollidersEnabled(!disableCollidersWhileDragging);

        if (!hadRigidbody)
        {
            DragStarted?.Invoke(this);
            return;
        }

        previousKinematicState = cachedRigidbody.isKinematic;
        previousGravityState = cachedRigidbody.useGravity;
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;

        DragStarted?.Invoke(this);
    }

    public void MoveTo(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void CompleteDrop(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        FinishDrag();
    }

    public void CancelDrag()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
        FinishDrag();
    }

    public void HandleFailedDrop()
    {
        switch (failedDropAction)
        {
            case DragFailedDropAction.ReturnToStart:
                CancelDrag();
                break;

            case DragFailedDropAction.DeactivateObject:
                FinishDrag();
                gameObject.SetActive(false);
                break;

            case DragFailedDropAction.DestroyObject:
                FinishDrag();
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
