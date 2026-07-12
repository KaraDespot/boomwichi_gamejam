/*
 * GrillZone
 * Назначение: зона грильницы для приготовления закрытого сендвича.
 * Что делает: принимает закрытый Raw/Toasted root-сендвич, закрывается, ведет таймер Raw/Toasted/Burnt и открывается кликом после готовности.
 * Связи: вызывается DropZone типа Grill, меняет SandwichState и возвращает DraggableObject игроку после открытия.
 * Паттерны: Domain Controller, Timer State Machine.
 */

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GrillZone : MonoBehaviour
{
    [Header("Grill")]
    [Tooltip("Точка, куда ставится сендвич внутри грильницы. Если пусто, используется позиция грильницы.")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("Дополнительное смещение от snap point.")]
    [SerializeField] private Vector3 snapOffset = Vector3.zero;

    [Tooltip("Сколько секунд нужно до состояния Toasted.")]
    [SerializeField] private float toastedSeconds = 3f;

    [Tooltip("Сколько секунд нужно до состояния Burnt. Должно быть больше Toasted.")]
    [SerializeField] private float burntSeconds = 6f;

    [Header("Visual State")]
    [Tooltip("Опциональная крышка грильницы. Если назначена, будет поворачиваться при закрытии/открытии.")]
    [SerializeField] private Transform lidTransform;

    [Tooltip("Локальный угол крышки, когда грильница открыта.")]
    [SerializeField] private Vector3 openLidEulerAngles = new Vector3(-55f, 0f, 0f);

    [Tooltip("Локальный угол крышки, когда грильница закрыта.")]
    [SerializeField] private Vector3 closedLidEulerAngles = Vector3.zero;

    private Coroutine cookRoutine;
    private DraggableObject currentDraggable;
    private SandwichState currentSandwich;
    private bool isClosed;
    private bool canOpen;

    public bool HasSandwich => currentSandwich != null;
    public bool IsClosed => isClosed;
    public bool CanOpen => canOpen;
    public SandwichState CurrentSandwich => currentSandwich;

    private void Awake()
    {
        ApplyLidState(false);
    }

    public bool CanAccept(DraggableObject draggableObject)
    {
        if (draggableObject == null || HasSandwich)
            return false;

        SandwichState sandwichState = draggableObject.GetComponent<SandwichState>();
        return sandwichState != null &&
            sandwichState.HasTopBread &&
            sandwichState.CookState != SandwichCookState.Burnt &&
            !sandwichState.IsInGrill &&
            !sandwichState.IsDelivered;
    }

    public bool Accept(DraggableObject draggableObject)
    {
        return Accept(draggableObject, draggableObject != null ? draggableObject.transform.position : transform.position);
    }

    public bool Accept(DraggableObject draggableObject, Vector3 dropWorldPoint)
    {
        if (!CanAccept(draggableObject))
            return false;

        currentDraggable = draggableObject;
        currentSandwich = draggableObject.GetComponent<SandwichState>();

        currentSandwich.DetachFromBoard();
        currentSandwich.MarkEnteredGrill();

        currentDraggable.FinishDrag();
        currentDraggable.SetCanDrag(false);
        currentDraggable.SetPhysicsLocked(true);
        currentDraggable.transform.SetPositionAndRotation(GetSnapPosition(), transform.rotation);

        CloseGrill();
        StartCooking();
        return true;
    }

    public bool TryOpenAfterCooking()
    {
        if (!HasSandwich || !isClosed || !canOpen)
            return false;

        OpenGrill();
        StopCookingTimer();

        currentDraggable.SetCanDrag(true);
        currentDraggable.SetPhysicsLocked(true);
        currentDraggable.SetFailedDropAction(DragFailedDropAction.ReturnToStart);
        currentDraggable.DragStarted += HandleCurrentSandwichDragStarted;

        return true;
    }

    public Vector3 GetSnapPosition()
    {
        Vector3 basePosition = snapPoint != null ? snapPoint.position : transform.position;
        return basePosition + snapOffset;
    }

    private void StartCooking()
    {
        StopCookingTimer();
        canOpen = currentSandwich != null && currentSandwich.CookState != SandwichCookState.Raw;
        cookRoutine = StartCoroutine(CookRoutine());
    }

    private void StopCookingTimer()
    {
        if (cookRoutine == null)
            return;

        StopCoroutine(cookRoutine);
        cookRoutine = null;
    }

    private IEnumerator CookRoutine()
    {
        float toastedAt = Mathf.Max(0f, toastedSeconds);
        float burntAt = Mathf.Max(toastedAt, burntSeconds);

        while (currentSandwich != null && currentSandwich.CookProgressSeconds < burntAt)
        {
            currentSandwich.AddCookProgress(Time.deltaTime);
            float cookProgress = currentSandwich.CookProgressSeconds;

            if (cookProgress >= toastedAt && currentSandwich.CookState == SandwichCookState.Raw)
            {
                currentSandwich.SetCookState(SandwichCookState.Toasted);
                canOpen = true;
                Debug.Log($"{name}: сендвич готов, можно открыть грильницу.", this);
            }

            if (cookProgress >= burntAt && currentSandwich.CookState == SandwichCookState.Toasted)
            {
                currentSandwich.SetCookState(SandwichCookState.Burnt);
                canOpen = true;
                Debug.Log($"{name}: сендвич пережарен.", this);
            }

            yield return null;
        }

        cookRoutine = null;
    }

    private void CloseGrill()
    {
        isClosed = true;
        canOpen = false;
        ApplyLidState(true);
    }

    private void OpenGrill()
    {
        isClosed = false;
        ApplyLidState(false);
    }

    private void ApplyLidState(bool closed)
    {
        if (lidTransform == null)
            return;

        lidTransform.localEulerAngles = closed
            ? closedLidEulerAngles
            : openLidEulerAngles;
    }

    private void HandleCurrentSandwichDragStarted(DraggableObject draggableObject)
    {
        if (draggableObject != currentDraggable)
            return;

        currentDraggable.DragStarted -= HandleCurrentSandwichDragStarted;
        currentSandwich.MarkRemovedFromGrill();
        currentDraggable = null;
        currentSandwich = null;
    }
}
