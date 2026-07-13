/*
 * MoldSystem
 * Назначение: плесень на ингредиентах и очистка тряской мыши.
 * Что делает: назначает плесень при спавне, отслеживает осциллирующую тряску во время drag, падает на сендвич при ошибке.
 * Связи: DayTimer, DragController, InputRaycaster, EventBus, IngredientInstance, SandwichState, MoldVisual.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections.Generic;
using UnityEngine;

public class MoldSystem : MonoBehaviour
{
    [Header("Шанс плесени")]
    [Tooltip("Минимальный шанс плесени в начале дня.")]
    [SerializeField] private float minMoldChance = 0.10f;

    [Tooltip("Максимальный шанс плесени к концу дня.")]
    [SerializeField] private float maxMoldChance = 0.70f;

    [Header("Тряска")]
    [Tooltip("Сколько секунд нужно активно трясти мышью, чтобы очистить плесень.")]
    [SerializeField] private float requiredShakeDuration = 1.2f;

    [Tooltip("Минимальная скорость движения мыши (пикс/сек), чтобы считать жест тряской.")]
    [SerializeField] private float minShakeSpeedPixelsPerSecond = 700f;

    [Tooltip("Минимальное движение за кадр (пикс), иначе кадр не считается тряской.")]
    [SerializeField] private float minShakeMovementPerFrame = 6f;

    [Tooltip("Пауза без тряски (сек), после которой прогресс сбрасывается.")]
    [SerializeField] private float shakeResetDelay = 0.25f;

    [Tooltip("Сколько раз нужно сменить направление движения мыши за одну попытку очистки.")]
    [SerializeField] private int minDirectionChanges = 6;

    [Tooltip("Минимальный угол между сменами направления (градусы). 90 = только резкие рывки туда-сюда.")]
    [SerializeField] private float minDirectionChangeAngle = 90f;

    [Tooltip("Звук тряски стартует только после короткого подтверждения жеста, чтобы не срабатывать от обычного drag из контейнера.")]
    [SerializeField] private float minShakeAudioDelay = 0.18f;

    [Tooltip("Минимум смен направления перед стартом звука тряски.")]
    [SerializeField] private int minShakeAudioDirectionChanges = 2;

    [Header("Связи")]
    [SerializeField] private DayTimer dayTimer;
    [SerializeField] private DragController dragController;
    [SerializeField] private InputRaycaster inputRaycaster;

    [Header("FX")]
    [Tooltip("Парящие частицы над плесневым ингредиентом. Следуют за объектом, пока IsMoldy = true.")]
    [SerializeField] private GameObject moldIdleFxPrefab;

    [Tooltip("Падающие частицы во время тряски. Следуют за объектом, пока идёт тряска.")]
    [SerializeField] private GameObject moldShakeFxPrefab;

    [Tooltip("Смещение idle-FX относительно центра ингредиента.")]
    [SerializeField] private Vector3 idleFxLocalOffset = new Vector3(0f, 0.12f, 0f);

    [Tooltip("Смещение shake-FX относительно центра ингредиента.")]
    [SerializeField] private Vector3 shakeFxLocalOffset = new Vector3(0f, 0.06f, 0f);

    [Tooltip("Если в префабе idle loop выключен — включить его в рантайме для постоянного эффекта.")]
    [SerializeField] private bool forceIdleFxLoop = true;

    [Tooltip("Сколько секунд ждать перед уничтожением отцепленного shake-FX после завершения тряски.")]
    [SerializeField] private float shakeFxReleaseLifetime = 2f;

    [Tooltip("Множитель масштаба mold-toxic FX. Чуть меньше 1 — компактнее облако.")]
    [SerializeField] private float moldToxicFxScaleMultiplier = 0.88f;

    [Tooltip("Размер частиц mold-toxic в local space.")]
    [SerializeField] private float moldToxicFxParticleSize = 0.34f;

    [Tooltip("Множитель масштаба FX_mold_spread при тряске. Не трогает toxic.")]
    [SerializeField] private float moldSpreadFxScaleMultiplier = 1f;

    private readonly Dictionary<IngredientInstance, GameObject> idleFxByIngredient = new();

    private IngredientInstance shakeIngredient;
    private GameObject activeShakeFx;
    private Vector2 lastPointerPosition;
    private Vector2 lastShakeDirection;
    private float shakeActiveTime;
    private float timeSinceLastShakeInput;
    private int directionChanges;
    private bool pointerTrackingActive;

    private void Awake()
    {
        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        if (dragController == null)
            dragController = FindFirstObjectByType<DragController>();

        if (inputRaycaster == null)
            inputRaycaster = FindFirstObjectByType<InputRaycaster>();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnIngredientSpawned += HandleIngredientSpawned;
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnIngredientSpawned -= HandleIngredientSpawned;

        ClearAllFx();
    }

    private void Update()
    {
        UpdateShakeCleaning();
    }

    private void HandleIngredientSpawned(IngredientInstance ingredient)
    {
        if (ingredient == null || !ingredient.CanBecomeMoldy)
            return;

        float dayProgress = dayTimer != null ? dayTimer.DayProgress : 0f;
        float moldChance = Mathf.Lerp(minMoldChance, maxMoldChance, dayProgress);

        if (Random.value > moldChance)
            return;

        ApplyMold(ingredient);
    }

    public void ApplyMold(IngredientInstance ingredient)
    {
        if (ingredient == null || !ingredient.CanBecomeMoldy)
            return;

        ingredient.SetMoldy(true);

        if (!TrySetCookAwareMoldVisual(ingredient, true))
        {
            MoldVisual moldVisual = MoldVisual.GetOrCreate(ingredient);
            if (moldVisual != null)
                moldVisual.SetMoldyVisual(true);
        }

        EnsureIdleFx(ingredient);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseIngredientMolded(ingredient);
    }

    public void ClearMold(IngredientInstance ingredient)
    {
        if (ingredient == null)
            return;

        ingredient.ClearMold();

        if (!TrySetCookAwareMoldVisual(ingredient, false))
        {
            MoldVisual moldVisual = ingredient.GetComponent<MoldVisual>();
            if (moldVisual != null)
                moldVisual.SetMoldyVisual(false);
        }

        DestroyIdleFx(ingredient);

        if (shakeIngredient == ingredient)
            StopShakeFx(false);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseMoldCleaned(ingredient);
    }

    private void UpdateShakeCleaning()
    {
        if (dragController == null || InputManager.Instance == null)
        {
            ResetShakeSession();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            ResetShakeSession();
            return;
        }

        DraggableObject draggable = dragController.CurrentDraggable;
        if (draggable == null || !InputManager.Instance.PrimaryHeld)
        {
            ResetShakeSession();
            return;
        }

        IngredientInstance ingredient = draggable.GetComponent<IngredientInstance>();
        if (ingredient == null || !ingredient.IsMoldy)
        {
            ResetShakeSession();
            return;
        }

        if (shakeIngredient != ingredient)
            ResetShakeSession();

        shakeIngredient = ingredient;
        Vector2 pointer = InputManager.Instance.PointerScreenPosition;

        if (!pointerTrackingActive)
        {
            pointerTrackingActive = true;
            lastPointerPosition = pointer;
            return;
        }

        Vector2 delta = pointer - lastPointerPosition;
        lastPointerPosition = pointer;

        float movement = delta.magnitude;
        float speed = movement / Mathf.Max(Time.deltaTime, 0.0001f);
        bool isShakingNow = movement >= minShakeMovementPerFrame &&
                            speed >= minShakeSpeedPixelsPerSecond;

        if (isShakingNow)
        {
            RegisterDirectionChange(delta);
            shakeActiveTime += Time.deltaTime;
            timeSinceLastShakeInput = 0f;
            EnsureShakeFx(ingredient);

            if (shakeActiveTime >= minShakeAudioDelay &&
                directionChanges >= minShakeAudioDirectionChanges)
            {
                AudioManager.Instance?.StartMoldShakeLoop();
            }
        }
        else
        {
            timeSinceLastShakeInput += Time.deltaTime;
            if (timeSinceLastShakeInput >= shakeResetDelay)
                ResetShakeProgress();
        }

        if (shakeActiveTime < requiredShakeDuration || directionChanges < minDirectionChanges)
            return;

        ProcessMoldRemoval(ingredient, pointer);
        ResetShakeSession();
    }

    private static bool TrySetCookAwareMoldVisual(IngredientInstance ingredient, bool isMoldy)
    {
        BreadCookVisual breadCookVisual = ingredient.GetComponent<BreadCookVisual>();
        if (breadCookVisual == null)
            return false;

        breadCookVisual.SetMoldyVisual(isMoldy);
        return true;
    }

    private void RegisterDirectionChange(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.01f)
            return;

        Vector2 direction = delta.normalized;
        if (lastShakeDirection.sqrMagnitude < 0.01f)
        {
            lastShakeDirection = direction;
            return;
        }

        float angle = Vector2.Angle(lastShakeDirection, direction);
        if (angle < minDirectionChangeAngle)
            return;

        directionChanges++;
        lastShakeDirection = direction;
    }

    private void ProcessMoldRemoval(IngredientInstance ingredient, Vector2 screenPosition)
    {
        AudioManager.Instance?.StopMoldShakeLoop();

        if (TryGetSandwichUnderPointer(screenPosition, out SandwichState sandwichState))
        {
            ApplyFallenMold(ingredient, sandwichState);
            return;
        }

        ReleaseShakeFx();
        ClearMold(ingredient);
    }

    private void ApplyFallenMold(IngredientInstance ingredient, SandwichState sandwichState)
    {
        if (sandwichState != null)
            sandwichState.MarkFallenMold();

        ReleaseShakeFx();
        ClearMold(ingredient);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseMoldFallenOnSandwich(sandwichState);
    }

    private bool TryGetSandwichUnderPointer(Vector2 screenPosition, out SandwichState sandwichState)
    {
        sandwichState = null;

        if (inputRaycaster == null)
            return false;

        if (!inputRaycaster.TryGetDropZone(screenPosition, out DropZone dropZone))
            return false;

        if (dropZone.ZoneType != DropZoneType.Board)
            return false;

        SandwichBoard board = dropZone.GetComponent<SandwichBoard>();
        if (board == null || !board.HasBottomBread)
            return false;

        sandwichState = board.CurrentSandwich;
        return sandwichState != null;
    }

    private void EnsureIdleFx(IngredientInstance ingredient)
    {
        if (moldIdleFxPrefab == null || ingredient == null)
            return;

        if (idleFxByIngredient.TryGetValue(ingredient, out GameObject existingFx) && existingFx != null)
            return;

        GameObject instance = IngredientFxUtility.Attach(
            moldIdleFxPrefab,
            ingredient.transform,
            idleFxLocalOffset,
            moldToxicFxScaleMultiplier,
            loop: forceIdleFxLoop,
            playOnAttach: true,
            particleStartSize: moldToxicFxParticleSize);
        idleFxByIngredient[ingredient] = instance;
    }

    private void DestroyIdleFx(IngredientInstance ingredient)
    {
        if (ingredient == null)
            return;

        if (!idleFxByIngredient.TryGetValue(ingredient, out GameObject instance))
            return;

        idleFxByIngredient.Remove(ingredient);

        if (instance != null)
            Destroy(instance);
    }

    private void EnsureShakeFx(IngredientInstance ingredient)
    {
        if (moldShakeFxPrefab == null || ingredient == null)
            return;

        if (activeShakeFx != null)
            return;

        activeShakeFx = IngredientFxUtility.Attach(
            moldShakeFxPrefab,
            ingredient.transform,
            shakeFxLocalOffset,
            moldSpreadFxScaleMultiplier,
            loop: false,
            playOnAttach: true,
            particleStartSize: null);
    }

    private void StopShakeFx(bool releaseParticles)
    {
        AudioManager.Instance?.StopMoldShakeLoop();

        if (activeShakeFx == null)
            return;

        if (releaseParticles)
            ReleaseShakeFx();
        else
        {
            Destroy(activeShakeFx);
            activeShakeFx = null;
        }

        AudioManager.Instance?.StopMoldShakeLoop();
    }

    private void ReleaseShakeFx()
    {
        if (activeShakeFx == null)
            return;

        GameObject fx = activeShakeFx;
        activeShakeFx = null;
        fx.transform.SetParent(null, true);
        IngredientFxUtility.StopEmitting(fx);
        Destroy(fx, shakeFxReleaseLifetime);
        AudioManager.Instance?.StopMoldShakeLoop();
    }

    private void ResetShakeProgress()
    {
        shakeActiveTime = 0f;
        directionChanges = 0;
        lastShakeDirection = Vector2.zero;
        timeSinceLastShakeInput = 0f;
        StopShakeFx(false);
    }

    private void ResetShakeSession()
    {
        shakeIngredient = null;
        pointerTrackingActive = false;
        ResetShakeProgress();
    }

    private void ClearAllFx()
    {
        foreach (KeyValuePair<IngredientInstance, GameObject> pair in idleFxByIngredient)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        idleFxByIngredient.Clear();
        StopShakeFx(false);
        ResetShakeSession();
    }
}
