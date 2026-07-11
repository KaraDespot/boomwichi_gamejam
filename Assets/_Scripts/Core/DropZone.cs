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

    public DropZoneType ZoneType => zoneType;
    public DropZoneAction DropAction => GetDropAction();

    public bool CanAccept(DraggableObject draggableObject)
    {
        return draggableObject != null;
    }

    public void Accept(DraggableObject draggableObject)
    {
        if (!CanAccept(draggableObject))
            return;

        switch (GetDropAction())
        {
            case DropZoneAction.SnapToZone:
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
