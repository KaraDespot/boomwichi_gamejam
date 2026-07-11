# Урок 3: Input System — система управления (подробная инструкция)

## Зачем нужна Input System?

В игре нужно обрабатывать ввод от игрока:
- **Движение** (WASD) — чтобы игрок мог ходить
- **Взгляд** (мышь) — чтобы игрок мог смотреть вокруг
- **Прыжок** (Space) — чтобы игрок мог прыгать
- **Атака, взаимодействие** — позже

**Проблема старого способа:**
- `Input.GetKey(KeyCode.W)` — работает только с клавиатурой
- Нет поддержки геймпада "из коробки"
- Сложно переключаться между устройствами

**Преимущества новой Input System:**
- ✅ Работает с клавиатурой, мышью, геймпадом, тачскрином
- ✅ Легко переключаться между устройствами
- ✅ Централизованное управление всеми действиями
- ✅ Можно перенастраивать клавиши без изменения кода

---

## ⚠️ ВАЖНО: Проверка перед началом

**Перед началом убедитесь:**
1. **Input System установлен:**
   - Откройте `Window` → `Package Manager`
   - В списке пакетов найдите `Input System`
   - Если не установлен — нажмите `Install`

2. **Active Input Handling настроен:**
   - Откройте `Edit` → `Project Settings` → `Player`
   - Найдите `Active Input Handling`
   - Должно быть: **`Both`** (старая и новая система работают вместе)
   - Если нет — выберите `Both` и перезапустите Unity

3. **Input Actions Asset уже создан:**
   - В корне проекта должен быть файл `InputSystem_Actions.inputactions`
   - Если его нет — создадим его сейчас

---

## Часть 1: Понимание структуры Input System

### Что такое Input Actions Asset?

**Input Actions Asset** (`InputSystem_Actions.inputactions`) — это файл, который содержит:
- **Action Maps** (карты действий) — группы действий (например, "Player", "UI")
- **Actions** (действия) — конкретные действия (например, "Move", "Jump")
- **Bindings** (привязки) — какие клавиши/кнопки вызывают действия

**Аналогия:**
- Action Map = "Режим игры" (игровой режим, режим меню)
- Action = "Что делать" (двигаться, прыгать)
- Binding = "Как делать" (WASD для движения, Space для прыжка)

### Структура нашего Input Actions Asset

У нас уже есть файл `InputSystem_Actions.inputactions` с такой структурой:

```
InputSystem_Actions
├── Player (Action Map)
│   ├── Move (Vector2) → WASD / Стрелки / Геймпад
│   ├── Look (Vector2) → Мышь / Правый стик геймпада
│   ├── Jump (Button) → Space / Кнопка A геймпада
│   ├── Attack (Button) → ЛКМ / X геймпада
│   ├── Interact (Button) → E / Y геймпада
│   ├── Sprint (Button) → Left Shift / Левый стик геймпада
│   ├── Crouch (Button) → C / B геймпада
│   ├── Pause (Button) → Escape / Start геймпада
│   └── Zoom (Value, Vector2) → Колесо мыши (для зума камеры, см. Этап 4)
└── UI (Action Map)
    ├── Navigate (Vector2) → Стрелки / WASD
    ├── Submit (Button) → Enter / A геймпада
    └── Cancel (Button) → Escape / B геймпада
```

**Важно:** Используются только карты **Player** и **UI** (отдельной карты Camera нет — управление камерой идёт через Player/Look). **UI** нужен для навигации по меню и для закрытия паузы (Cancel/Escape). Позже в тот же Asset можно добавить новые действия (например, Inventory, смена оружия) — архитектура это допускает.

---

## Часть 2: Создание InputManager

### Зачем нужен InputManager?

**Проблема:** Если каждый скрипт будет напрямую обращаться к Input System, получится:
- Много дублирующегося кода
- Сложно управлять переключением между Action Maps
- Нет единой точки доступа к вводу

**Решение:** Создаем **InputManager** — централизованный менеджер, который:
- Управляет Input Actions Asset
- Предоставляет простые методы для получения ввода
- Переключает Action Maps (Player ↔ UI)
- Работает как Singleton (как GameManager, SceneLoader)

### Шаг 1: Создание скрипта InputManager

1. Создайте скрипт `InputManager.cs` в папке `Assets/_Scripts/Core/`
2. Откройте скрипт и вставьте следующий код:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Ссылка на Input Actions Asset (назначается в Inspector)
    [SerializeField] private InputActionAsset inputActions;

    // Action Maps
    private InputActionMap playerActionMap;
    private InputActionMap uiActionMap;

    // Player Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pauseAction;
    private InputAction cancelAction;

    // Текущие значения (кэшируем для быстрого доступа)
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }

    // События для кнопок (вызываются один раз при нажатии)
    public System.Action OnJumpPressed;
    public System.Action OnAttackPressed;
    public System.Action OnInteractPressed;
    public System.Action OnPausePressed;
    public System.Action OnCancelPressed;

    private void Awake()
    {
        // Singleton паттерн (как в GameManager)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Инициализацию Input System делаем в Start(), а не здесь!
        // Bootstrap присваивает inputActions после AddComponent<InputManager>(),
        // а Awake() вызывается сразу при AddComponent — к этому моменту поле ещё пустое.
    }

    private void Start()
    {
        InitializeInputSystem();
    }

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
        jumpAction = playerActionMap.FindAction("Jump");
        attackAction = playerActionMap.FindAction("Attack");
        interactAction = playerActionMap.FindAction("Interact");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");
        pauseAction = playerActionMap.FindAction("Pause");
        if (uiActionMap != null)
            cancelAction = uiActionMap.FindAction("Cancel");

        // Подписываемся на события кнопок
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

        // Включаем Player Action Map по умолчанию
        EnablePlayerInput();
    }

    private void OnEnable()
    {
        // Включаем Input Actions при включении объекта
        if (inputActions != null)
            inputActions.Enable();
    }

    private void OnDisable()
    {
        // Выключаем Input Actions при выключении объекта
        if (inputActions != null)
            inputActions.Disable();
    }

    private void OnDestroy()
    {
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
    }

    private void Update()
    {
        // Обновляем значения ввода каждый кадр
        UpdateInputValues();
    }

    private void UpdateInputValues()
    {
        // Читаем текущие значения действий
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        LookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        SprintHeld = sprintAction != null && sprintAction.IsPressed();
        CrouchHeld = crouchAction != null && crouchAction.IsPressed();

        // Для кнопок используем события (OnJumpPerformed и т.д.)
        // Но также можно проверить через IsPressed() для удержания
    }

    // Обработчики событий кнопок
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        JumpPressed = true;
        OnJumpPressed?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        AttackPressed = true;
        OnAttackPressed?.Invoke();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractPressed = true;
        OnInteractPressed?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        OnPausePressed?.Invoke();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        OnCancelPressed?.Invoke();
    }

    // Методы для сброса флагов (вызываются в конце кадра)
    public void ResetButtonFlags()
    {
        JumpPressed = false;
        AttackPressed = false;
        InteractPressed = false;
    }

    // Методы для переключения Action Maps
    public void EnablePlayerInput()
    {
        if (playerActionMap != null)
            playerActionMap.Enable();
        if (uiActionMap != null)
            uiActionMap.Disable();
    }

    public void EnableUIInput()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();
        if (uiActionMap != null)
            uiActionMap.Enable();
    }

    // Обработчики событий паузы (интеграция с EventBus)
    private void HandleGamePaused()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();
        if (uiActionMap != null)
            uiActionMap.Enable();
        Debug.Log("InputManager: Player input disabled, UI enabled (game paused)");
    }

    private void HandleGameResumed()
    {
        if (uiActionMap != null)
            uiActionMap.Disable();
        if (playerActionMap != null)
            playerActionMap.Enable();
        Debug.Log("InputManager: Player input enabled (game resumed)");
    }

    // Публичные методы для получения ввода (альтернатива свойствам)
    public Vector2 GetMoveInput()
    {
        return MoveInput;
    }

    public Vector2 GetLookInput()
    {
        return LookInput;
    }

    public bool IsJumpPressed()
    {
        return JumpPressed;
    }

    public bool IsAttackPressed()
    {
        return AttackPressed;
    }

    public bool IsInteractPressed()
    {
        return InteractPressed;
    }

    public bool IsSprintHeld()
    {
        return SprintHeld;
    }

    public bool IsCrouchHeld()
    {
        return CrouchHeld;
    }
}
```

### Шаг 2: Объяснение кода

**1. Singleton паттерн:**
```csharp
public static InputManager Instance { get; private set; }
```
- Как в `GameManager` и `SceneLoader`
- Позволяет обращаться из любого места: `InputManager.Instance.GetMoveInput()`

**2. Awake и Start — почему инициализация в Start():**
- В `Awake()` мы только выставляем Singleton и `DontDestroyOnLoad`; **не** вызываем `InitializeInputSystem()`.
- `InitializeInputSystem()` вызывается в `Start()`.
- **Причина:** Bootstrap создаёт InputManager через `AddComponent<InputManager>()`. Unity сразу вызывает `Awake()` у нового компонента — ещё до того, как выполнится следующая строка в Bootstrap, где присваивается `inputActions`. В момент `Awake()` поле `inputActions` ещё пустое. В `Start()` же вызывается уже после того, как все `Awake()` отработали, то есть после того, как Bootstrap присвоил `inputActions`. Поэтому инициализацию ввода делаем в `Start()`.

**3. InputActionAsset:**
```csharp
public InputActionAsset inputActions;
```
- Ссылка на файл `InputSystem_Actions.inputactions`
- Назначается в коде в Bootstrap (из Resources); для обучения поле сделано публичным.

**4. Action Maps и Actions:**
```csharp
playerActionMap = inputActions.FindActionMap("Player");
moveAction = playerActionMap.FindAction("Move");
```
- Получаем Action Map по имени
- Получаем Action по имени внутри Action Map

**5. Свойства для доступа:**
```csharp
public Vector2 MoveInput { get; private set; }
```
- Публичное чтение, приватная запись
- Обновляется в `Update()` каждый кадр

**6. События для кнопок:**
```csharp
public System.Action OnJumpPressed;
jumpAction.performed += OnJumpPerformed;
```
- Вызываются один раз при нажатии
- Другие скрипты могут подписаться: `InputManager.Instance.OnJumpPressed += MyMethod;`

**7. Методы переключения:**
```csharp
EnablePlayerInput(); // Включает Player, выключает UI
EnableUIInput();     // Включает UI, выключает Player
```
- Нужно для переключения между игровым режимом и меню

**8. Пауза через Input System (Pause и Cancel):**
- В карте **Player** есть действие **Pause** (Escape / Start геймпада). InputManager подписывается на него и вызывает событие `OnPausePressed`.
- В карте **UI** есть действие **Cancel** (Escape / B геймпада). InputManager подписывается на него и вызывает `OnCancelPressed`.
- При паузе (EventBus.OnGamePaused) InputManager отключает Player и **включает UI** — тогда Escape обрабатывается как Cancel и закрывает паузу.
- **PauseController** не должен проверять `Input.GetKeyDown(KeyCode.Escape)` в `Update()`. Вместо этого он подписывается на `InputManager.Instance.OnPausePressed` (открыть паузу, если игра идёт) и на `InputManager.Instance.OnCancelPressed` (закрыть паузу, если игра на паузе).

**Как добавить паузу через Input System (если ещё не сделано):**

1. **В InputSystem_Actions:**  
   - В карте **Player** добавь действие **Pause** (тип Button).  
   - Привязки: **\<Keyboard\>/escape**, **\<Gamepad\>/start**.  
   - В карте **UI** действие **Cancel** уже есть (Escape / B).  
   - Сохрани Asset.

2. **В InputManager:**  
   - Поля: `private InputAction pauseAction;` и `private InputAction cancelAction;` (cancelAction — из `uiActionMap.FindAction("Cancel")`).  
   - События: `public System.Action OnPausePressed;` и `public System.Action OnCancelPressed;`.  
   - В `InitializeInputSystem()`: найти Pause и Cancel, подписаться на `pauseAction.performed` и `cancelAction.performed` (обработчики вызывают `OnPausePressed?.Invoke()` и `OnCancelPressed?.Invoke()`).  
   - В `OnDestroy()` отписаться от этих действий.  
   - В **HandleGamePaused**: кроме отключения Player — включить UI (`uiActionMap.Enable()`), чтобы во время паузы работало Cancel.  
   - В **HandleGameResumed**: выключить UI и включить Player.

3. **В PauseController** внести такие изменения:

   **Удалить:**
   - Метод `Update()` целиком (в нём была проверка `Input.GetKeyDown(KeyCode.Escape)` и вызов `TogglePause()`).
   - Метод `TogglePause()` целиком.

   **В `OnEnable()` добавить** (после подписки на EventBus):
   ```csharp
   if (InputManager.Instance != null)
   {
       InputManager.Instance.OnPausePressed += HandlePausePressed;
       InputManager.Instance.OnCancelPressed += HandleCancelPressed;
   }
   ```

   **В `OnDisable()` добавить** (после отписки от EventBus):
   ```csharp
   if (InputManager.Instance != null)
   {
       InputManager.Instance.OnPausePressed -= HandlePausePressed;
       InputManager.Instance.OnCancelPressed -= HandleCancelPressed;
   }
   ```

   **Добавить два новых метода** (например, после `Start()`, перед `ShowPausePanel`):
   ```csharp
   private void HandlePausePressed()
   {
       if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
           GameManager.Instance.Pause();
   }

   private void HandleCancelPressed()
   {
       if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
           GameManager.Instance.Resume();
   }
   ```

   Подписки на EventBus (`OnGamePaused` / `OnGameResumed`) и методы `ShowPausePanel`, `HidePausePanel`, `OnResumeClicked`, `OnMainMenuClicked` не трогать — они остаются как есть.

---

## Часть 3: Интеграция с BootstrapManager

### Шаг 1: Добавление InputManager в Bootstrap

Нужно создать `InputManager` при запуске игры, как мы делали с `GameManager`, `SceneLoader`, `EventBus`.

1. Откройте скрипт `BootstrapManager.cs`
2. Добавьте метод создания InputManager:

```csharp
private static void CreateInputManager()
{
    InputManager existing = FindFirstObjectByType<InputManager>();
    if (existing != null)
    {
        DontDestroyOnLoad(existing.gameObject);
        return;
    }

    GameObject go = new GameObject("InputManager");
    InputManager inputManager = go.AddComponent<InputManager>();
    DontDestroyOnLoad(go);
}
```

3. Вызовите этот метод в `Awake()`:

```csharp
private void Awake()
{
    if (_initialized)
    {
        Destroy(gameObject);
        return;
    }

    _initialized = true;
    DontDestroyOnLoad(gameObject);

    // Создаём/поднимаем основные менеджеры
    CreateGameManager();
    CreateSceneLoader();
    CreateEventBus();
    CreateInputManager(); // ← Добавьте эту строку

    // Переходим в главное меню
    SceneLoader.Instance.Load(SceneNames.MainMenu);
}
```

**Важно:** `InputManager` должен создаваться **после** других менеджеров, но это не критично.

### Шаг 2: Настройка InputManager в Inspector

**Проблема:** `InputManager` создается программно, но ему нужна ссылка на `InputSystem_Actions.inputactions`.

**Решение:** Есть два способа:

#### Вариант А: Назначить в коде (проще)

Добавьте в `BootstrapManager.CreateInputManager()`:

```csharp
private static void CreateInputManager()
{
    InputManager existing = FindFirstObjectByType<InputManager>();
    if (existing != null)
    {
        DontDestroyOnLoad(existing.gameObject);
        return;
    }

    GameObject go = new GameObject("InputManager");
    InputManager inputManager = go.AddComponent<InputManager>();
    
    // Загружаем Input Actions Asset из Resources или по пути
    InputActionAsset inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
    if (inputActions == null)
    {
        // Альтернативный способ: загрузить из Assets
        inputActions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
    }
    
    if (inputActions != null)
    {
        // Используем рефлексию для установки приватного поля
        var field = typeof(InputManager).GetField("inputActions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(inputManager, inputActions);
    }
    else
    {
        Debug.LogError("InputManager: Не удалось загрузить InputSystem_Actions.inputactions!");
    }
    
    DontDestroyOnLoad(go);
}
```

**Проблема:** Этот способ использует рефлексию и требует `UnityEditor` (работает только в редакторе).

#### Вариант Б: Сделать поле публичным (рекомендуется для обучения)

Измените в `InputManager.cs`:

```csharp
// Было:
[SerializeField] private InputActionAsset inputActions;

// Стало:
public InputActionAsset inputActions;
```

**Чтобы работало и в редакторе, и в билде**, ассет нужно загружать из папки **Resources** — тогда один и тот же код работает везде:

1. **Создайте папку Resources** (если её ещё нет):
   - В проекте: `Assets` → ПКМ → `Create` → `Folder`
   - Назовите папку **`Resources`** (именно так, с большой R). Unity загружает из неё ассеты по имени в коде.

2. **Положите Input Actions в Resources:**
   - Перетащите `InputSystem_Actions.inputactions` из корня `Assets/` в папку `Assets/Resources/`
   - Или оставьте файл там, где он есть, но создайте в Resources **дубликат**: ПКМ по `InputSystem_Actions.inputactions` → Duplicate, перенесите копию в `Resources`. Имя файла (без расширения) должно быть `InputSystem_Actions` — по нему будем грузить.

В `BootstrapManager.CreateInputManager()` используйте только **Resources** (без `#if UNITY_EDITOR`):

```csharp
private static void CreateInputManager()
{
    InputManager existing = FindFirstObjectByType<InputManager>();
    if (existing != null)
    {
        DontDestroyOnLoad(existing.gameObject);
        return;
    }

    GameObject go = new GameObject("InputManager");
    InputManager inputManager = go.AddComponent<InputManager>();
    
    // Загружаем из Resources — работает и в редакторе, и в билде
    inputManager.inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
    
    if (inputManager.inputActions == null)
    {
        Debug.LogError("InputManager: Не удалось загрузить InputSystem_Actions! " +
            "Убедитесь, что файл InputSystem_Actions.inputactions лежит в папке Assets/Resources/");
    }
    
    DontDestroyOnLoad(go);
}
```

**Почему так:**
- `Resources.Load("InputSystem_Actions")` ищет ассет по имени (без расширения) **только** в папках `Resources`. В билде в игру попадают только ассеты из таких папок (или сцены/префабы и т.д.), поэтому в билде это единственный способ загрузить наш `.inputactions`.
- Если ассета нет в `Resources`, в билде загрузка вернёт `null` и ввод не будет работать.
- В редакторе этот же вызов тоже работает, если файл лежит в `Assets/Resources/`, поэтому отдельная ветка для редактора не нужна.

**Итог:** Вариант Б будет работать и в редакторе, и в билде **только если** `InputSystem_Actions.inputactions` лежит в папке `Assets/Resources/` (и загружается через `Resources.Load`). Иначе в билде поле останется пустым.

---

## Часть 4: Использование InputManager

### Пример: Как получить ввод в других скриптах

**Способ 1: Через свойства (рекомендуется)**
```csharp
Vector2 moveInput = InputManager.Instance.MoveInput;
Vector2 lookInput = InputManager.Instance.LookInput;
bool isJumping = InputManager.Instance.JumpPressed;
```

**Способ 2: Через методы**
```csharp
Vector2 moveInput = InputManager.Instance.GetMoveInput();
Vector2 lookInput = InputManager.Instance.GetLookInput();
bool isJumping = InputManager.Instance.IsJumpPressed();
```

**Способ 3: Через события (для кнопок)**
```csharp
private void OnEnable()
{
    InputManager.Instance.OnJumpPressed += HandleJump;
}

private void OnDisable()
{
    InputManager.Instance.OnJumpPressed -= HandleJump;
}

private void HandleJump()
{
    Debug.Log("Игрок нажал прыжок!");
    // Логика прыжка
}
```

### Важно: Сброс флагов кнопок

**Проблема:** Флаги `JumpPressed`, `AttackPressed` остаются `true` до следующего кадра.

**Решение:** Вызывайте `ResetButtonFlags()` в конце кадра (например, в `GameManager` или в конце `Update()` всех скриптов, которые используют ввод).

**Или:** Используйте события вместо проверки флагов — события вызываются автоматически один раз.

---

## Часть 5: Проверка и тестирование

### Шаг 1: Проверка создания InputManager

1. Запустите игру через `Bootstrap` (Play в Unity)
2. Откройте `Hierarchy` во время Play
3. Должен появиться объект `InputManager` (с иконкой DontDestroyOnLoad)
4. Выберите его и проверьте в Inspector:
   - Поле `Input Actions` должно быть заполнено
   - В консоли не должно быть ошибок

### Шаг 2: Тестовый скрипт для проверки ввода

Создайте временный скрипт `InputTest.cs`:

```csharp
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private void Update()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("InputManager не создан!");
            return;
        }

        Vector2 move = InputManager.Instance.MoveInput;
        Vector2 look = InputManager.Instance.LookInput;

        if (move != Vector2.zero)
            Debug.Log($"Move: {move}");

        if (look != Vector2.zero)
            Debug.Log($"Look: {look}");

        if (InputManager.Instance.JumpPressed)
            Debug.Log("Jump pressed!");

        if (InputManager.Instance.AttackPressed)
            Debug.Log("Attack pressed!");

        // Сброс флагов
        InputManager.Instance.ResetButtonFlags();
    }
}
```

1. Прикрепите `InputTest` к любому объекту в сцене `GameScene`
2. Запустите игру
3. Нажмите WASD — в консоли должны появиться сообщения `Move: (x, y)`
4. Двигайте мышью — в консоли должны появиться сообщения `Look: (x, y)`
5. Нажмите Space — в консоли должно появиться `Jump pressed!`
6. Нажмите ЛКМ — в консоли должно появиться `Attack pressed!`

### Шаг 3: Проверка переключения Action Maps

В `GameManager` можно добавить переключение:

```csharp
public void Pause()
{
    if (CurrentState != GameState.Playing)
        return;

    CurrentState = GameState.Paused;
    Time.timeScale = 0f;
    InputManager.Instance.EnableUIInput(); // ← Переключаем на UI
    EventBus.Instance.RaiseGamePaused();
}

public void Resume()
{
    if (CurrentState != GameState.Paused)
        return;

    CurrentState = GameState.Playing;
    Time.timeScale = 1f;
    InputManager.Instance.EnablePlayerInput(); // ← Переключаем на Player
    EventBus.Instance.RaiseGameResumed();
}
```

### Переключение карт при смене сцены (игра ↔ меню)

**Проблема:** Если выйти из игры в меню (например, через «Главное меню» из паузы), а потом снова нажать «Новая игра», ввод и камера перестают работать. Причина: при выходе в меню вызывается только `GoToMenu()`, без `Resume()`, поэтому InputManager не переключает карты обратно на Player. При повторном входе в игру карта Player так и остаётся выключенной.

**Решение:** В `GameManager` при смене сцены явно переключать карты ввода:

- **В `StartGame()`** — после загрузки игровой сцены вызвать `InputManager.Instance.EnablePlayerInput()`, чтобы при каждом входе в игру была включена карта Player.
- **В `GoToMenu()`** — перед загрузкой главного меню вызвать `InputManager.Instance.EnableUIInput()`, чтобы в меню была включена карта UI (навигация по меню).

**Что добавить в GameManager:**

В `StartGame()` после `SceneLoader.Instance.Load(SceneNames.GameScene);`:
```csharp
if (InputManager.Instance != null)
    InputManager.Instance.EnablePlayerInput();
```

В `GoToMenu()` после `Time.timeScale = 1f;` и перед `SceneLoader.Instance.Load(SceneNames.MainMenu);`:
```csharp
if (InputManager.Instance != null)
    InputManager.Instance.EnableUIInput();
```

Тогда при цепочке «игра → пауза → Главное меню → Новая игра» ввод и камера будут работать при повторном входе в игровую сцену.

---

## Объяснение: Что такое InputActionAsset?

**InputActionAsset** — это ScriptableObject, который содержит все Action Maps и Actions.

**Как это работает:**
1. В Unity Editor создается файл `.inputactions`
2. В этом файле настраиваются все действия и привязки
3. В коде загружается этот файл как `InputActionAsset`
4. Из Asset получаются Action Maps и Actions
5. Actions можно читать или подписываться на события

**Преимущества:**
- ✅ Все настройки в одном месте
- ✅ Можно менять клавиши без изменения кода
- ✅ Легко добавлять новые действия

---

## Объяснение: Что такое Action Map?

**Action Map** — это группа действий, которые активны одновременно.

**Примеры:**
- **Player** — действия для управления игроком (Move, Jump, Attack)
- **UI** — действия для навигации по меню (Navigate, Submit, Cancel)

**Зачем нужны:**
- Когда игрок в меню, не нужно обрабатывать движение (WASD)
- Когда игрок в игре, не нужно обрабатывать навигацию по меню
- Переключение между Action Maps решает эту проблему

**Как использовать:**
```csharp
playerActionMap.Enable();  // Включает все действия в Player
uiActionMap.Disable();     // Выключает все действия в UI
```

---

## Объяснение: Типы Actions

**1. Value (Vector2, float, bool):**
- Возвращает текущее значение (например, направление движения)
- Используется для: Move, Look
- Читается через: `action.ReadValue<Vector2>()`

**2. Button:**
- Возвращает true/false (нажата/не нажата)
- Используется для: Jump, Attack, Interact
- Можно проверить через: `action.IsPressed()` или подписаться на событие `performed`

**3. PassThrough:**
- Пропускает ввод дальше (для UI)
- Используется для: Navigate, Point

---

## Частые ошибки

### InputManager.Instance == null

**Причина:** InputManager не создан в Bootstrap.

**Решение:**
1. Проверьте, что `CreateInputManager()` вызывается в `BootstrapManager.Awake()`
2. Проверьте, что сцена `Bootstrap` первая в Build Settings
3. Запускайте игру через `Bootstrap`, а не напрямую через `GameScene`

### Input Actions Asset не назначен

**Причина:** Поле `inputActions` в InputManager пустое в момент вызова `InitializeInputSystem()`.

**Типичный случай:** инициализация вызывалась в `Awake()`. У Unity при `AddComponent<InputManager>()` сразу вызывается `Awake()` у нового компонента — раньше, чем Bootstrap успевает выполнить следующую строку с `Resources.Load` и присвоением `inputActions`. Поэтому инициализацию нужно вызывать в `Start()` (как в актуальном коде выше).

**Решение:**
1. Убедитесь, что в `InputManager` вызов `InitializeInputSystem()` идёт из `Start()`, а не из `Awake()`.
2. Проверьте, что файл `InputSystem_Actions.inputactions` лежит в папке `Assets/Resources/` (для загрузки через `Resources.Load`).
3. Проверьте консоль на ошибки загрузки.

### Action Map не найден

**Причина:** Неправильное имя Action Map в коде.

**Решение:**
1. Откройте `InputSystem_Actions.inputactions` в Unity Editor
2. Проверьте точное имя Action Map (должно быть "Player", не "player" или "Player ")
3. Убедитесь, что в коде используется правильное имя: `inputActions.FindActionMap("Player")`

### Действия не работают

**Причина:** Action Map не включен.

**Решение:**
1. Убедитесь, что вызывается `EnablePlayerInput()` при старте игры
2. Проверьте, что `inputActions.Enable()` вызывается в `OnEnable()`
3. Проверьте, что Action Map включен: `playerActionMap.Enable()`

---

## Дополнительные советы

### Организация кода

**Рекомендуемая структура:**
```
InputManager
├── Singleton паттерн
├── Инициализация Input System
├── Свойства для доступа к вводу
├── События для кнопок
└── Методы переключения Action Maps
```

### Оптимизация

**Кэширование значений:**
- Значения ввода обновляются в `Update()` и сохраняются в свойствах
- Это быстрее, чем читать из Actions каждый раз

**События vs Проверка флагов:**
- События лучше для одноразовых действий (Jump, Attack)
- Флаги лучше для проверки в разных местах кода

### Расширение функциональности

**Добавление новых действий:**
1. Откройте `InputSystem_Actions.inputactions` в Unity Editor
2. Добавьте новое действие в Action Map "Player"
3. Настройте привязки (клавиши)
4. В `InputManager` добавьте:
   - `private InputAction newAction;`
   - Инициализацию в `InitializeInputSystem()`
   - Свойство или событие для доступа

---

## Проверочный сценарий

1. **Запустить Play** → автоматически попали в `MainMenu`
2. **Проверить Hierarchy** → должен быть объект `InputManager`
3. **Перейти в GameScene** → через кнопку "Новая игра"
4. **Нажать WASD** → в консоли должны появиться сообщения о движении
5. **Двигать мышью** → в консоли должны появиться сообщения о взгляде
6. **Нажать Space** → в консоли должно появиться "Jump pressed!"
7. **Нажать ЛКМ** → в консоли должно появиться "Attack pressed!"
8. **Остановить Play** → убедиться, что нет ошибок/исключений

---

## Что дальше?

После настройки Input System мы перейдем к:
- **Этап 4: Cinemachine и камера** — настройка камеры от третьего лица
- **Этап 5: Игрок** — создание PlayerController, который использует InputManager

InputManager будет использоваться во всех последующих системах для получения ввода от игрока.
