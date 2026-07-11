/*
 * DraggableObject
 * Назначение: маркер и локальное состояние объекта, который можно перетаскивать по столу.
 * Что делает: запоминает стартовую позицию, двигается за DragController и корректно завершает/откатывает drag.
 * Связи: выбирается InputRaycaster, управляется DragController, может быть принят DropZone.
 * Паттерны: Component, Encapsulation of state.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class DraggableObject : MonoBehaviour
{
    [Header("Drag")]
    [Tooltip("Высота объекта над точкой стола во время перетаскивания.")]
    [SerializeField] private float dragHeight = 0.18f;

    [Tooltip("Если отпускание произошло не над подходящей зоной, объект вернётся туда, откуда его взяли.")]
    [SerializeField] private bool returnToStartOnFailedDrop = true;

    [Tooltip("На время drag коллайдеры объекта отключаются, чтобы raycast попадал в стол и drop zones, а не в сам объект.")]
    [SerializeField] private bool disableCollidersWhileDragging = true;

    private Rigidbody cachedRigidbody;
    private Collider[] cachedColliders;
    private bool hadRigidbody;
    private bool previousKinematicState;
    private bool previousGravityState;
    private Vector3 startPosition;
    private Quaternion startRotation;

    public float DragHeight => dragHeight;
    public bool ReturnToStartOnFailedDrop => returnToStartOnFailedDrop;
    public bool IsDragging { get; private set; }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>();
        hadRigidbody = cachedRigidbody != null;
    }

    public void BeginDrag()
    {
        IsDragging = true;
        startPosition = transform.position;
        startRotation = transform.rotation;

        SetCollidersEnabled(!disableCollidersWhileDragging);

        if (!hadRigidbody)
            return;

        previousKinematicState = cachedRigidbody.isKinematic;
        previousGravityState = cachedRigidbody.useGravity;
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;
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

    public void FinishDrag()
    {
        IsDragging = false;
        SetCollidersEnabled(true);

        if (!hadRigidbody)
            return;

        cachedRigidbody.isKinematic = previousKinematicState;
        cachedRigidbody.useGravity = previousGravityState;
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
