/*
 * InputManager
 * Назначение: единая точка доступа к вводу Boomwichi.
 * Что делает: читает только курсор, левую кнопку мыши, паузу и отмену; не хранит управление персонажем из RPG-шаблона.
 * Связи: создаётся BootstrapManager, используется DragController, PauseController и GameManager.
 * Паттерны: Singleton, Facade над Unity Input System, Observer через события.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input Actions")]
    [Tooltip("InputActionAsset из Resources/InputSystem_Actions. Для Boomwichi нужны только Gameplay и UI.")]
    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap gameplayActionMap;
    private InputActionMap uiActionMap;

    private InputAction pointerPositionAction;
    private InputAction primaryPressAction;
    private InputAction pauseAction;
    private InputAction cancelAction;

    public Vector2 PointerScreenPosition { get; private set; }
    public bool PrimaryPressedThisFrame { get; private set; }
    public bool PrimaryReleasedThisFrame { get; private set; }
    public bool PrimaryHeld { get; private set; }

    public event Action<Vector2> OnPrimaryPressed;
    public event Action<Vector2> OnPrimaryReleased;
    public event Action OnPausePressed;
    public event Action OnCancelPressed;

    public InputActionAsset InputActions
    {
        get => inputActions;
        set
        {
            if (inputActions == value)
                return;

            DeinitializeInputSystem();
            inputActions = value;

            if (isActiveAndEnabled)
                InitializeInputSystem();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeInputSystem();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused += HandleGamePaused;
            EventBus.Instance.OnGameResumed += HandleGameResumed;
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= HandleGamePaused;
            EventBus.Instance.OnGameResumed -= HandleGameResumed;
        }

        DisableAllInput();
    }

    private void OnDestroy()
    {
        DeinitializeInputSystem();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        PointerScreenPosition = pointerPositionAction != null
            ? pointerPositionAction.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private void LateUpdate()
    {
        PrimaryPressedThisFrame = false;
        PrimaryReleasedThisFrame = false;
    }

    private void InitializeInputSystem()
    {
        if (gameplayActionMap != null)
            return;

        if (inputActions == null)
        {
            Debug.LogError("InputManager: Input Actions Asset не назначен.");
            return;
        }

        gameplayActionMap = inputActions.FindActionMap("Gameplay");
        uiActionMap = inputActions.FindActionMap("UI");

        if (gameplayActionMap == null)
        {
            Debug.LogError("InputManager: Action Map 'Gameplay' не найден.");
            return;
        }

        pointerPositionAction = gameplayActionMap.FindAction("PointerPosition");
        primaryPressAction = gameplayActionMap.FindAction("PrimaryPress");
        pauseAction = gameplayActionMap.FindAction("Pause");
        cancelAction = uiActionMap != null ? uiActionMap.FindAction("Cancel") : null;

        if (pointerPositionAction == null)
            Debug.LogError("InputManager: Action 'PointerPosition' не найден в Gameplay.");
        if (primaryPressAction == null)
            Debug.LogError("InputManager: Action 'PrimaryPress' не найден в Gameplay.");

        SubscribeInputActions();
        EnableGameplayInput();
    }

    private void DeinitializeInputSystem()
    {
        UnsubscribeInputActions();
        DisableAllInput();

        gameplayActionMap = null;
        uiActionMap = null;
        pointerPositionAction = null;
        primaryPressAction = null;
        pauseAction = null;
        cancelAction = null;

        PrimaryPressedThisFrame = false;
        PrimaryReleasedThisFrame = false;
        PrimaryHeld = false;
    }

    private void SubscribeInputActions()
    {
        if (primaryPressAction != null)
        {
            primaryPressAction.started += HandlePrimaryStarted;
            primaryPressAction.canceled += HandlePrimaryCanceled;
        }

        if (pauseAction != null)
            pauseAction.performed += HandlePausePerformed;

        if (cancelAction != null)
            cancelAction.performed += HandleCancelPerformed;
    }

    private void UnsubscribeInputActions()
    {
        if (primaryPressAction != null)
        {
            primaryPressAction.started -= HandlePrimaryStarted;
            primaryPressAction.canceled -= HandlePrimaryCanceled;
        }

        if (pauseAction != null)
            pauseAction.performed -= HandlePausePerformed;

        if (cancelAction != null)
            cancelAction.performed -= HandleCancelPerformed;
    }

    private void HandlePrimaryStarted(InputAction.CallbackContext context)
    {
        RefreshPointerPosition();
        PrimaryHeld = true;
        PrimaryPressedThisFrame = true;
        OnPrimaryPressed?.Invoke(PointerScreenPosition);
    }

    private void HandlePrimaryCanceled(InputAction.CallbackContext context)
    {
        RefreshPointerPosition();
        PrimaryHeld = false;
        PrimaryReleasedThisFrame = true;
        OnPrimaryReleased?.Invoke(PointerScreenPosition);
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        OnPausePressed?.Invoke();
    }

    private void HandleCancelPerformed(InputAction.CallbackContext context)
    {
        OnCancelPressed?.Invoke();
    }

    public void EnableGameplayInput()
    {
        if (gameplayActionMap != null)
            gameplayActionMap.Enable();

        if (uiActionMap != null)
            uiActionMap.Disable();
    }

    public void EnableUIInput()
    {
        if (gameplayActionMap != null)
            gameplayActionMap.Disable();

        if (uiActionMap != null)
            uiActionMap.Enable();
    }

    public void DisableAllInput()
    {
        if (gameplayActionMap != null)
            gameplayActionMap.Disable();

        if (uiActionMap != null)
            uiActionMap.Disable();
    }

    private void HandleGamePaused()
    {
        EnableUIInput();
    }

    private void HandleGameResumed()
    {
        EnableGameplayInput();
    }

    private void RefreshPointerPosition()
    {
        if (pointerPositionAction != null)
            PointerScreenPosition = pointerPositionAction.ReadValue<Vector2>();
    }
}
