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

    [Header("Timer Visual")]
    [Tooltip("Показывать простой runtime-индикатор таймера над грильницей.")]
    [SerializeField] private bool showTimerVisual = true;

    [Tooltip("Локальная позиция индикатора таймера относительно грильницы.")]
    [SerializeField] private Vector3 timerLocalOffset = new Vector3(0f, 0.85f, 0f);

    [Tooltip("Размер индикатора таймера.")]
    [SerializeField] private Vector3 timerSize = new Vector3(0.8f, 0.06f, 0.06f);

    [Tooltip("Цвет таймера в состоянии Raw.")]
    [SerializeField] private Color rawTimerColor = new Color(1f, 0.75f, 0.18f, 1f);

    [Tooltip("Цвет таймера в состоянии Toasted.")]
    [SerializeField] private Color toastedTimerColor = new Color(0.35f, 0.95f, 0.35f, 1f);

    [Tooltip("Цвет таймера в состоянии Burnt.")]
    [SerializeField] private Color burntTimerColor = new Color(0.12f, 0.08f, 0.05f, 1f);

    private Coroutine cookRoutine;
    private DraggableObject currentDraggable;
    private SandwichState currentSandwich;
    private Transform timerRoot;
    private Transform timerFill;
    private Renderer timerFillRenderer;
    private Material timerFillMaterial;
    private bool isClosed;
    private bool canOpen;
    private bool isSandwichBeingDragged;

    public bool HasSandwich => currentSandwich != null;
    public bool IsClosed => isClosed;
    public bool CanOpen => canOpen;
    public SandwichState CurrentSandwich => currentSandwich;

    private void Awake()
    {
        CreateTimerVisual();
        UpdateTimerVisual();
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
        isSandwichBeingDragged = false;

        currentSandwich.DetachFromBoard();
        currentSandwich.MarkEnteredGrill();

        currentDraggable.CompleteDrop(GetSnapPosition());
        currentDraggable.SetCanDrag(false);
        currentDraggable.SetPhysicsLocked(true);
        currentDraggable.transform.rotation = transform.rotation;

        Debug.Log(
            $"[GrillZone] Принят сендвич {currentSandwich.name}: CookState={currentSandwich.CookState}, " +
            $"прогресс={currentSandwich.CookProgressSeconds:F2}s",
            this);

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

        UpdateTimerVisual();
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
        UpdateTimerVisual();
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
                UpdateTimerVisual();
                Debug.Log(
                    $"[GrillZone] {currentSandwich.name}: Raw → Toasted за {cookProgress:F2}s " +
                    $"(порог={toastedAt:F2}s)",
                    this);
            }

            if (cookProgress >= burntAt && currentSandwich.CookState == SandwichCookState.Toasted)
            {
                currentSandwich.SetCookState(SandwichCookState.Burnt);
                canOpen = true;
                UpdateTimerVisual();
                Debug.Log(
                    $"[GrillZone] {currentSandwich.name}: Toasted → Burnt за {cookProgress:F2}s " +
                    $"(порог={burntAt:F2}s)",
                    this);
            }

            UpdateTimerVisual();
            yield return null;
        }

        cookRoutine = null;
        UpdateTimerVisual();
    }

    private void CloseGrill()
    {
        isClosed = true;
        canOpen = false;
        ApplyLidState(true);
        AudioManager.Instance?.PlaySfx(AudioCue.GrillOpenClose);
        UpdateTimerVisual();
    }

    private void OpenGrill()
    {
        isClosed = false;
        ApplyLidState(false);
        AudioManager.Instance?.PlaySfx(AudioCue.GrillOpenClose);
        UpdateTimerVisual();
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
        currentDraggable.DragEnded += HandleCurrentSandwichDragEnded;
        currentSandwich.MarkRemovedFromGrill();
        isSandwichBeingDragged = true;
        StopCookingTimer();
        Debug.Log(
            $"[GrillZone] Сендвич снят с гриля: {currentSandwich.name}, " +
            $"CookState={currentSandwich.CookState}, прогресс={currentSandwich.CookProgressSeconds:F2}s",
            this);
        UpdateTimerVisual();
    }

    private void HandleCurrentSandwichDragEnded(DraggableObject draggableObject, DragEndResult result)
    {
        if (draggableObject != currentDraggable)
            return;

        currentDraggable.DragEnded -= HandleCurrentSandwichDragEnded;
        isSandwichBeingDragged = false;

        if (result == DragEndResult.Completed)
        {
            ClearCurrentSandwich();
            return;
        }

        ReturnSandwichToGrill();
    }

    private void ReturnSandwichToGrill()
    {
        if (currentDraggable == null || currentSandwich == null)
            return;

        currentSandwich.MarkEnteredGrill();
        currentDraggable.SetCanDrag(false);
        currentDraggable.SetPhysicsLocked(true);
        currentDraggable.transform.SetPositionAndRotation(GetSnapPosition(), transform.rotation);

        if (currentSandwich.CookState == SandwichCookState.Burnt)
        {
            OpenGrill();
            currentDraggable.SetCanDrag(true);
            currentDraggable.DragStarted += HandleCurrentSandwichDragStarted;
            UpdateTimerVisual();
            return;
        }

        CloseGrill();
        StartCooking();
    }

    private void ClearCurrentSandwich()
    {
        StopCookingTimer();

        if (currentSandwich != null)
            currentSandwich.MarkRemovedFromGrill();

        currentDraggable = null;
        currentSandwich = null;
        isSandwichBeingDragged = false;
        OpenGrill();
        HideTimerVisual();
    }

    private void CreateTimerVisual()
    {
        if (!showTimerVisual || timerRoot != null)
            return;

        GameObject rootObject = new GameObject("GrillTimer");
        timerRoot = rootObject.transform;
        timerRoot.SetParent(transform, false);
        timerRoot.localPosition = timerLocalOffset;
        timerRoot.localRotation = Quaternion.identity;

        GameObject backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backgroundObject.name = "TimerBackground";
        backgroundObject.transform.SetParent(timerRoot, false);
        backgroundObject.transform.localPosition = Vector3.zero;
        backgroundObject.transform.localScale = timerSize;
        RemoveRuntimeCollider(backgroundObject);

        Renderer backgroundRenderer = backgroundObject.GetComponent<Renderer>();
        if (backgroundRenderer != null)
            backgroundRenderer.sharedMaterial = CreateTimerMaterial(new Color(0.05f, 0.05f, 0.05f, 1f));

        GameObject fillObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillObject.name = "TimerFill";
        timerFill = fillObject.transform;
        timerFill.SetParent(timerRoot, false);
        RemoveRuntimeCollider(fillObject);

        timerFillRenderer = fillObject.GetComponent<Renderer>();
        timerFillMaterial = CreateTimerMaterial(rawTimerColor);
        if (timerFillRenderer != null)
            timerFillRenderer.sharedMaterial = timerFillMaterial;
    }

    private void UpdateTimerVisual()
    {
        if (timerRoot == null || timerFill == null)
            return;

        bool shouldShow = currentSandwich != null && !isSandwichBeingDragged;
        timerRoot.gameObject.SetActive(shouldShow);
        if (!shouldShow)
            return;

        float burntAt = Mathf.Max(Mathf.Max(0f, toastedSeconds), burntSeconds);
        float normalizedProgress = burntAt > 0f
            ? Mathf.Clamp01(currentSandwich.CookProgressSeconds / burntAt)
            : 1f;

        timerFill.localScale = new Vector3(timerSize.x * normalizedProgress, timerSize.y, timerSize.z);
        timerFill.localPosition = new Vector3(-timerSize.x * (1f - normalizedProgress) * 0.5f, 0f, 0f);

        if (timerFillMaterial != null)
            timerFillMaterial.color = GetTimerColor(currentSandwich.CookState);
    }

    private void HideTimerVisual()
    {
        if (timerRoot != null)
            timerRoot.gameObject.SetActive(false);
    }

    private Color GetTimerColor(SandwichCookState cookState)
    {
        switch (cookState)
        {
            case SandwichCookState.Toasted:
                return toastedTimerColor;

            case SandwichCookState.Burnt:
                return burntTimerColor;

            default:
                return rawTimerColor;
        }
    }

    private Material CreateTimerMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void RemoveRuntimeCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }
}
