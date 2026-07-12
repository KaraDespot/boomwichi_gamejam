/*
 * DropZone
 * Назначение: зона, которая принимает отпущенный DraggableObject.
 * Что делает: различает доску и мусорку, возвращает позицию snap и выполняет простой результат drop.
 * Связи: вызывается DragController при отпускании объекта над зоной.
 * Паттерны: Strategy через DropZoneAction, Component.
 */

using UnityEngine;

public enum DropZoneType
{
    Board,
    Trash
}

public enum DropZoneAction
{
    SnapToZone,
    DeactivateObject
}

[DisallowMultipleComponent]
public class DropZone : MonoBehaviour
{
    [Header("Drop Zone")]
    [Tooltip("Тип зоны: доска для сборки или мусорка для сброса.")]
    [SerializeField] private DropZoneType zoneType = DropZoneType.Board;

    [Tooltip("Точка, куда ставится объект после drop. Если пусто, используется позиция самой зоны.")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("Дополнительное смещение от snap point.")]
    [SerializeField] private Vector3 snapOffset = Vector3.zero;

    [Tooltip("Правила сборки сендвича для зоны доски. Если пусто, объект просто ставится в snap point.")]
    [SerializeField] private SandwichBoard sandwichBoard;

    public DropZoneType ZoneType => zoneType;
    public DropZoneAction DropAction => GetDropAction();

    private void Awake()
    {
        if (sandwichBoard == null)
            sandwichBoard = GetComponent<SandwichBoard>();
    }

    public bool CanAccept(DraggableObject draggableObject)
    {
        if (draggableObject == null)
            return false;

        if (zoneType == DropZoneType.Trash)
            return true;

        if (sandwichBoard != null)
            return sandwichBoard.CanAccept(draggableObject);

        return true;
    }

    public void Accept(DraggableObject draggableObject)
    {
        Accept(draggableObject, GetSnapPosition());
    }

    public void Accept(DraggableObject draggableObject, Vector3 dropWorldPoint)
    {
        if (!CanAccept(draggableObject))
            return;

        switch (GetDropAction())
        {
            case DropZoneAction.SnapToZone:
                if (sandwichBoard != null)
                {
                    sandwichBoard.Accept(draggableObject, dropWorldPoint);
                    break;
                }

                draggableObject.CompleteDrop(GetSnapPosition());
                break;

            case DropZoneAction.DeactivateObject:
                draggableObject.FinishDrag();
                draggableObject.gameObject.SetActive(false);
                break;
        }
    }

    public Vector3 GetSnapPosition()
    {
        Vector3 basePosition = snapPoint != null ? snapPoint.position : transform.position;
        return basePosition + snapOffset;
    }

    private DropZoneAction GetDropAction()
    {
        return zoneType == DropZoneType.Trash
            ? DropZoneAction.DeactivateObject
            : DropZoneAction.SnapToZone;
    }
}
