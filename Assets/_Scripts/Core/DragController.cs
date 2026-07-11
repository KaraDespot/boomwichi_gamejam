/*
 * DragController
 * Назначение: управляет переносом объекта мышью по поверхности стола.
 * Что делает: начинает drag по клику на DraggableObject, двигает объект по точке InputRaycaster и завершает drop в DropZone.
 * Связи: читает InputManager, использует InputRaycaster, DraggableObject и DropZone.
 * Паттерны: Controller, Mediator между вводом и объектами сцены.
 */

using UnityEngine;

public class DragController : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Компонент, который превращает позицию курсора в попадания по столу, объектам и зонам.")]
    [SerializeField] private InputRaycaster inputRaycaster;

    [Header("Drag")]
    [Tooltip("Если курсор ушёл за стол во время drag, объект остаётся в последней валидной точке.")]
    [SerializeField] private bool keepLastValidPosition = true;

    private DraggableObject currentDraggable;
    private Vector3 dragOffset;
    private Vector3 lastValidDragPosition;

    private void Awake()
    {
        if (inputRaycaster == null)
            inputRaycaster = FindFirstObjectByType<InputRaycaster>();
    }

    private void Update()
    {
        if (InputManager.Instance == null || inputRaycaster == null)
            return;

        if (InputManager.Instance.PrimaryPressedThisFrame)
            TryBeginDrag(InputManager.Instance.PointerScreenPosition);

        if (currentDraggable != null && InputManager.Instance.PrimaryHeld)
            UpdateDrag(InputManager.Instance.PointerScreenPosition);

        if (currentDraggable != null && InputManager.Instance.PrimaryReleasedThisFrame)
            CompleteDrag(InputManager.Instance.PointerScreenPosition);
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (!inputRaycaster.TryGetDraggable(screenPosition, out DraggableObject draggableObject))
            return;

        currentDraggable = draggableObject;
        currentDraggable.BeginDrag();

        if (!inputRaycaster.TryGetTablePoint(screenPosition, out Vector3 tablePoint))
        {
            currentDraggable.CancelDrag();
            currentDraggable = null;
            return;
        }

        dragOffset = currentDraggable.transform.position - tablePoint;
        dragOffset.y = currentDraggable.DragHeight;

        lastValidDragPosition = CreateDragPosition(tablePoint);
        currentDraggable.MoveTo(lastValidDragPosition);
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        if (inputRaycaster.TryGetTablePoint(screenPosition, out Vector3 tablePoint))
        {
            lastValidDragPosition = CreateDragPosition(tablePoint);
            currentDraggable.MoveTo(lastValidDragPosition);
            return;
        }

        if (keepLastValidPosition)
            currentDraggable.MoveTo(lastValidDragPosition);
    }

    private void CompleteDrag(Vector2 screenPosition)
    {
        if (inputRaycaster.TryGetDropZone(screenPosition, out DropZone dropZone) &&
            dropZone.CanAccept(currentDraggable))
        {
            dropZone.Accept(currentDraggable);
            currentDraggable = null;
            return;
        }

        if (currentDraggable.ReturnToStartOnFailedDrop)
            currentDraggable.CancelDrag();
        else
            currentDraggable.FinishDrag();

        currentDraggable = null;
    }

    private Vector3 CreateDragPosition(Vector3 tablePoint)
    {
        Vector3 dragPosition = tablePoint + dragOffset;
        dragPosition.y = tablePoint.y + currentDraggable.DragHeight;
        return dragPosition;
    }
}
