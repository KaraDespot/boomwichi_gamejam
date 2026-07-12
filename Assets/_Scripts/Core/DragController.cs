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
            TryBeginDragFromPointer(InputManager.Instance.PointerScreenPosition);

        if (currentDraggable != null && InputManager.Instance.PrimaryHeld)
            UpdateDrag(InputManager.Instance.PointerScreenPosition);

        if (currentDraggable != null && InputManager.Instance.PrimaryReleasedThisFrame)
            CompleteDrag(InputManager.Instance.PointerScreenPosition);
    }

    private void TryBeginDragFromPointer(Vector2 screenPosition)
    {
        if (inputRaycaster.TryGetDraggable(screenPosition, out DraggableObject draggableObject))
        {
            BeginDrag(draggableObject, screenPosition, true);
            return;
        }

        if (inputRaycaster.TryGetIngredientContainer(screenPosition, out IngredientContainer ingredientContainer) &&
            ingredientContainer.TrySpawnIngredient(out DraggableObject spawnedDraggable))
        {
            BeginDrag(spawnedDraggable, screenPosition, false);
            return;
        }

        if (inputRaycaster.TryGetDropZone(screenPosition, out DropZone dropZone))
            dropZone.TryHandleClick();
    }

    private void BeginDrag(DraggableObject draggableObject, Vector2 screenPosition, bool preservePointerOffset)
    {
        currentDraggable = draggableObject;
        currentDraggable.BeginDrag();

        if (!inputRaycaster.TryGetTablePoint(screenPosition, out Vector3 tablePoint))
        {
            currentDraggable.HandleFailedDrop();
            currentDraggable = null;
            return;
        }

        dragOffset = preservePointerOffset
            ? currentDraggable.transform.position - tablePoint
            : Vector3.zero;
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
        if (inputRaycaster.TryGetDropZone(screenPosition, out DropZone dropZone, out Vector3 dropPoint) &&
            dropZone.CanAccept(currentDraggable))
        {
            dropZone.Accept(currentDraggable, dropPoint);
            currentDraggable = null;
            return;
        }

        currentDraggable.HandleFailedDrop();

        currentDraggable = null;
    }

    private Vector3 CreateDragPosition(Vector3 tablePoint)
    {
        Vector3 dragPosition = tablePoint + dragOffset;
        dragPosition.y = tablePoint.y + currentDraggable.DragHeight;
        return dragPosition;
    }
}
