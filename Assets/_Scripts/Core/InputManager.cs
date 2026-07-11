/*
 * InputManager
 * Назначение: единая точка доступа к вводу (Unity Input System) для геймплея и UI.
 * Что делает: читает оси (Move/Look), отслеживает удержание (Sprint/Crouch) и отдаёт одноразовые нажатия
 *             (Jump/Attack/Interact/Pause/Cancel) через флаги и события.
 * Связи: обычно создаётся `BootstrapManager` (подкладывает `InputActionAsset` из Resources),
 *        подписывается на события паузы через `EventBus`, используется `GameManager` и UI-контроллерами.
 * Паттерны: Singleton, Facade (обёртка над Input System), Observer (события), интеграция с Event Bus.
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public InputActionAsset inputActions;

    // Action Maps (группы действий из Input Actions Asset)
    private InputActionMap playerActionMap;
    private InputActionMap uiActionMap;

    // Действия игрока
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction zoomAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pauseAction;
    private InputAction cancelAction;
    private InputAction weaponNextAction;
    private InputAction weaponPrevAction;

    // Текущие значения (кэшируем для быстрого доступа из других скриптов)
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float ZoomInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }

    // События для кнопок (вызываются один раз при нажатии/срабатывании действия)
    public System.Action OnJumpPressed;
    public System.Action OnAttackPressed;
    public System.Action OnInteractPressed;
    public System.Action OnPausePressed;
    public System.Action OnCancelPressed;
    /// <summary> Смена оружия: следующее в списке (кнопка 2). </summary>
    public System.Action OnWeaponNextPressed;
    /// <summary> Смена оружия: предыдущее в списке (кнопка 1). </summary>
    public System.Action OnWeaponPrevPressed;

    /// <summary>
    /// Инициализирует Singleton и делает объект переживающим смену сцен.
    /// </summary>
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

    /// <summary>
    /// Настраивает ссылки на Action Maps/Actions и подписки на события Input System.
    /// </summary>
    private void Start()
    {
        InitializeInputSystem();
    }

    /// <summary>
    /// Находит нужные Action Maps/Actions в <see cref="inputActions"/> и подключает обработчики.
    /// </summary>
    private void InitializeInputSystem()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputManager: Input Actions Asset не назначен!");
            return;
        }

        // Получаем Action Maps
        playerActionMap = inputActions.FindActionMap("Player");
        uiActionMap = inputActions.FindActionMap("UI");

        if (playerActionMap == null)
        {
            Debug.LogError("InputManager: Action Map 'Player' не найден!");
            return;
        }

        // Получаем Actions из Player Action Map
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        zoomAction = playerActionMap.FindAction("Zoom");
        jumpAction = playerActionMap.FindAction("Jump");
        attackAction = playerActionMap.FindAction("Attack");
        interactAction = playerActionMap.FindAction("Interact");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");
        pauseAction = playerActionMap.FindAction("Pause");
        if (uiActionMap != null)
            cancelAction = uiActionMap.FindAction("Cancel");
        weaponNextAction = playerActionMap.FindAction("Next");
        weaponPrevAction = playerActionMap.FindAction("Previous");

        // Подписываемся на события кнопок (одноразовые срабатывания performed)
        if (jumpAction != null)
            jumpAction.performed += OnJumpPerformed;
        if (attackAction != null)
            attackAction.performed += OnAttackPerformed;
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
        if (pauseAction != null)
            pauseAction.performed += OnPausePerformed;
        if (cancelAction != null)
            cancelAction.performed += OnCancelPerformed;
        if (weaponNextAction != null)
            weaponNextAction.performed += OnWeaponNextPerformed;
        if (weaponPrevAction != null)
            weaponPrevAction.performed += OnWeaponPrevPerformed;

        // По умолчанию включаем ввод для игрока
        EnablePlayerInput();
    }

    /// <summary>
    /// Включает Input Actions и подписывается на события паузы через <see cref="EventBus"/>.
    /// </summary>
    private void OnEnable()
    {
        // Включаем Input Actions при включении объекта
        if (inputActions != null)
            inputActions.Enable();

        // Подписываемся на события паузы через EventBus
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused += HandleGamePaused;
            EventBus.Instance.OnGameResumed += HandleGameResumed;
        } 
    }

    /// <summary>
    /// Выключает Input Actions и обязательно отписывается от событий (защита от утечек/двойных подписок).
    /// </summary>
    private void OnDisable()
    {
        // Выключаем Input Actions при выключении объекта
        if (inputActions != null)
            inputActions.Disable();

        // Отписываемся от событий паузы (важно для предотвращения утечек памяти!)
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= HandleGamePaused;
            EventBus.Instance.OnGameResumed -= HandleGameResumed;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий Input System
        if (jumpAction != null)
            jumpAction.performed -= OnJumpPerformed;
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;
        if (cancelAction != null)
            cancelAction.performed -= OnCancelPerformed;
        if (weaponNextAction != null)
            weaponNextAction.performed -= OnWeaponNextPerformed;
        if (weaponPrevAction != null)
            weaponPrevAction.performed -= OnWeaponPrevPerformed;
    }

    /// <summary>
    /// Обновляет кэшированные значения ввода каждый кадр.
    /// </summary>
    private void Update()
    {
        // Обновляем значения ввода каждый кадр
        UpdateInputValues();
    }

    /// <summary>
    /// Читает текущее состояние осей/удержаний из Input Actions.
    /// </summary>
    private void UpdateInputValues()
    {
        // Читаем текущие значения действий
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        LookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        ZoomInput = zoomAction != null ? zoomAction.ReadValue<Vector2>().y : 0f;
        SprintHeld = sprintAction != null && sprintAction.IsPressed();
        CrouchHeld = crouchAction != null && crouchAction.IsPressed();

        // Для «одноразовых» нажатий используем performed-колбэки (OnJumpPerformed и т.д.).
        // Для «удержания» используем IsPressed() (SprintHeld/CrouchHeld).
    }

    /// <summary>
    /// Обработчик нажатия Jump: выставляет флаг на кадр и уведомляет подписчиков.
    /// </summary>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        JumpPressed = true;
        OnJumpPressed?.Invoke();
    }

    /// <summary>
    /// Обработчик нажатия Attack: выставляет флаг на кадр и уведомляет подписчиков.
    /// </summary>
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        AttackPressed = true;
        OnAttackPressed?.Invoke();
    }

    /// <summary>
    /// Обработчик нажатия Interact: выставляет флаг на кадр и уведомляет подписчиков.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractPressed = true;
        OnInteractPressed?.Invoke();
    }

    /// <summary>
    /// Обработчик нажатия Pause: уведомляет подписчиков (пауза управляется <see cref="GameManager"/>).
    /// </summary>
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        OnPausePressed?.Invoke();
    }

    /// <summary>
    /// Обработчик Cancel (обычно ESC/Back): уведомляет подписчиков (например, закрыть меню/снять паузу).
    /// </summary>
    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        OnCancelPressed?.Invoke();
    }

    private void OnWeaponNextPerformed(InputAction.CallbackContext context)
    {
        OnWeaponNextPressed?.Invoke();
    }

    private void OnWeaponPrevPerformed(InputAction.CallbackContext context)
    {
        OnWeaponPrevPressed?.Invoke();
    }

    /// <summary>
    /// Сбрасывает «одноразовые» флаги нажатий.
    /// Обычно вызывается тем, кто читает эти флаги, в конце кадра (чтобы нажатие считалось ровно один кадр).
    /// </summary>
    public void ResetButtonFlags()
    {
        JumpPressed = false;
        AttackPressed = false;
        InteractPressed = false;
    }

    /// <summary>
    /// Включает ввод игрока и выключает UI-ввод (если он есть).
    /// </summary>
    public void EnablePlayerInput()
    {
        if (playerActionMap != null)
            playerActionMap.Enable();
        if (uiActionMap != null)
            uiActionMap.Disable();
    }

    /// <summary>
    /// Включает UI-ввод и выключает ввод игрока.
    /// Обычно используется при открытии меню, чтобы управление персонажем не реагировало на кнопки.
    /// </summary>
    public void EnableUIInput()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();
        if (uiActionMap != null)
            uiActionMap.Enable();
    }

    /// <summary>
    /// Реакция на паузу игры (через <see cref="EventBus"/>): отключаем карту ввода игрока.
    /// </summary>
    private void HandleGamePaused()
    {
        // При паузе отключаем Player Action Map: это блокирует управление персонажем.
        if (playerActionMap != null)
            playerActionMap.Disable();

        Debug.Log("InputManager: Player input disabled (game paused)");
    }

    /// <summary>
    /// Реакция на продолжение игры (через <see cref="EventBus"/>): включаем карту ввода игрока.
    /// </summary>
    private void HandleGameResumed()
    {
        // При возобновлении включаем Player Action Map обратно.
        if (playerActionMap != null)
            playerActionMap.Enable();

        Debug.Log("InputManager: Player input enabled (game resumed)");
    }

    /// <summary>
    /// Альтернативный способ получить движение (удобно для внешнего кода без доступа к свойству).
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return MoveInput;
    }

    /// <summary>
    /// Альтернативный способ получить ввод камеры/взгляда.
    /// </summary>
    public Vector2 GetLookInput()
    {
        return LookInput;
    }

    public float GetZoomInput()
    {
        return ZoomInput;
    }


    /// <summary>
    /// Возвращает, было ли нажатие Jump в этом кадре (до <see cref="ResetButtonFlags"/>).
    /// </summary>
    public bool IsJumpPressed()
    {
        return JumpPressed;
    }

    /// <summary>
    /// Возвращает, было ли нажатие Attack в этом кадре (до <see cref="ResetButtonFlags"/>).
    /// </summary>
    public bool IsAttackPressed()
    {
        return AttackPressed;
    }

    /// <summary>
    /// Возвращает, было ли нажатие Interact в этом кадре (до <see cref="ResetButtonFlags"/>).
    /// </summary>
    public bool IsInteractPressed()
    {
        return InteractPressed;
    }

    /// <summary>
    /// Возвращает, удерживается ли Sprint прямо сейчас.
    /// </summary>
    public bool IsSprintHeld()
    {
        return SprintHeld;
    }

    /// <summary>
    /// Возвращает, удерживается ли Crouch прямо сейчас.
    /// </summary>
    public bool IsCrouchHeld()
    {
        return CrouchHeld;
    }
}
