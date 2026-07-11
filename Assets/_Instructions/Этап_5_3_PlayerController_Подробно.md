# Урок 5.3: PlayerController — управление движением игрока

## Стрейфовый вариант для 3D RPG (игрок смотрит туда, куда смотрит камера)

В этом варианте логика ближе к современным action‑играм от третьего лица:

- **Камера** (`CameraTargetController` + Cinemachine) свободно крутится вокруг игрока мышью.
- **Движение**: `PlayerController` двигает игрока **относительно камеры**:
  - W/S — вперёд/назад по направлению камеры;
  - A/D — влево/вправо относительно камеры.
- **Поворот игрока**:
  - визуальная модель игрока всегда **смотрит в том же направлении, что и камера**;
  - можно идти вперёд, назад или в сторону, не разворачивая корпус отдельно от камеры.

Такой стиль называют **стрейфовым управлением**: мышь/стик крутит камеру (и, вместе с ней, “лицо” персонажа), а WASD задают движение вперёд/назад/влево/вправо относительно этого направления.

В реальном проекте визуальная модель игрока часто вынесена в отдельный дочерний объект (например, `Visual` или `visualRoot`), а на корневом объекте `Player` висят компоненты логики (`CharacterController`, `PlayerStats`, `PlayerController`). В таком случае мы поворачиваем именно `visualRoot`, а не весь объект `Player` — это удобнее для работы с физикой и префабом.

## Пример кода контроллера (с комментариями)

```csharp
using UnityEngine;

/// <summary>
/// Управляет перемещением игрока в 3D (вид от третьего лица).
/// Использует InputManager для получения ввода.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Компонент статистики игрока (здоровье, скорость и т.д.).")]
    public PlayerStats playerStats;

    [Tooltip("Трансформ камеры, относительно которой считается движение. Обычно main camera.")]
    public Transform cameraTransform;

    [Tooltip("Корневой трансформ визуальной модели (вращается в сторону камеры).")]
    public Transform visualRoot;

    [Header("Движение и физика")]
    [Tooltip("Гравитация (отрицательное значение).")]
    public float gravity = -9.81f;

    [Tooltip("Небольшая отрицательная скорость, чтобы прижимать игрока к земле.")]
    public float groundedGravity = -2f;

    [Tooltip("Множитель скорости при спринте.")]
    public float sprintMultiplier = 1.5f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        HandleMovement();
        HandleJump();

        InputManager.Instance.ResetButtonFlags();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 moveDirection = Vector3.zero;

        // Движение относительно камеры:
        // W/S – вперёд/назад по камере, A/D – влево/вправо по камере.
        if (moveInput.sqrMagnitude > 0.001f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            moveDirection = forward * moveInput.y + right * moveInput.x;
            moveDirection.Normalize();
        }

        // Скорость и скорость поворота берём из PlayerData (через PlayerStats), если возможно.
        float speed = 5f;
        float rotationSpeed = 720f;

        if (playerStats != null && playerStats.playerData != null)
        {
            speed = playerStats.playerData.moveSpeed;
            rotationSpeed = playerStats.playerData.rotationSpeed;
        }

        if (InputManager.Instance.IsSprintHeld())
        {
            speed *= sprintMultiplier;
        }

        Vector3 horizontalVelocity = moveDirection * speed;

        // Проверка на землю
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedGravity;
        }

        // Гравитация
        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + verticalVelocity;

        // Перемещаем игрока
        characterController.Move(velocity * Time.deltaTime);

        // ПОВОРОТ ИГРОКА (стрейфовый стиль):
        // - визуальная модель всегда смотрит туда же, куда и камера;
        // - направление взгляда зависит только от камеры, а не от W/S.
        if (cameraTransform != null && visualRoot != null)
        {
            // Берём направление вперёд камеры по плоскости XZ
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                visualRoot.rotation = Quaternion.Slerp(
                    visualRoot.rotation,
                    targetRotation,
                    rotationSpeed * Mathf.Deg2Rad * Time.deltaTime
                );
            }
        }
    }

    private void HandleJump()
    {
        if (!isGrounded)
            return;

        if (InputManager.Instance.IsJumpPressed())
        {
            float jumpForce = 5f;

            if (playerStats != null && playerStats.playerData != null)
            {
                jumpForce = playerStats.playerData.jumpForce;
            }

            verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
}
```

### Почему в конце Update вызывается ResetButtonFlags

`InputManager` в этом проекте хранит **одноразовые флаги** для действий вроде прыжка, удара, открытия меню и т.п.  
Например, метод `IsJumpPressed()` обычно означает “кнопка была **нажата в этом кадре**” (аналог `Input.GetButtonDown`), а не просто удерживается.

Чтобы такие действия:

- срабатывали **ровно один раз** на одно нажатие;
- не “залипали” на несколько кадров подряд;

в конце `Update()` контроллера (`PlayerController`, `CameraTargetController` и др.) вызывается:

```csharp
InputManager.Instance.ResetButtonFlags();
```

Этот вызов очищает внутренние флаги “нажато в этом кадре” после того, как вся игровая логика успела их прочитать.  
На следующем кадре флаги снова будут выставлены только при новом нажатии кнопки.

## Что важно проговорить ученикам

- **Камера и игрок — разные объекты**:
  - мышь управляет камерой (через `CameraTargetController` и Cinemachine);
  - `PlayerController` читает направление камеры, но не “тянет” её за собой.
- **Игрок смотрит туда же, куда смотрит камера**:
  - мышь/стик задают направление взгляда камеры и персонажа;
  - W/S двигают вперёд/назад по направлению камеры, A/D дают стрейф влево/вправо;
  - можно стоять на месте и просто крутить камеру, при этом модель игрока поворачивается вслед за ней.

Такой вариант удобно использовать как базу для будущих модификаций (лок‑он на цель, свободный прицел, разные пресеты камер и т.п.).

# Урок 5.3: PlayerController — управление движением игрока

## 1. Цели урока

- **Техническая цель**: создать `PlayerController`, который:
  - получает ввод через `InputManager`;
  - преобразует ввод в движение и поворот;
  - использует данные скорости/прыжка из `PlayerData` через `PlayerStats`.
- **Обучающая цель**: показать разделение ответственности между:
  - `InputManager` (собирает ввод),
  - `PlayerController` (движение/поворот),
  - `PlayerStats` (характеристики).

---

## 2. Роль PlayerController

`PlayerController` — это компонент на объекте `Player`, который:

- не хранит здоровье/ману (это делает `PlayerStats`);
- не занимается сохранениями/уровнями (`PlayerProgression`, `SaveSystem`);
- **только**:
  - читает ввод (`Move`, `Jump`, `Sprint`) у `InputManager`;
  - переводит этот ввод в движение/прыжок/поворот;
  - опирается на значения скорости/прыжка из `PlayerData` (через ссылку на `PlayerStats`).

**Принцип ООП:** каждый класс отвечает за одну задачу (Single Responsibility).

---

## 3. Подготовка

Перед началом убедись, что:

1. У тебя есть рабочий `InputManager` (из Урока 3: Input System).
2. На `Player` уже есть:
   - `CharacterController` (для 3D третьего лица),
   - `PlayerStats` (из урока 5.2).
3. Камера настроена через Cinemachine и следует за `Player/CameraTarget` (из Этапа 4).

---

## 4. Создание скрипта PlayerController

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/Scripts/Player/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`PlayerController`**.
4. Открой его в редакторе.

### 4.2. Реализация для 3D от третьего лица (основной вариант)

Ниже — пример контроллера для 3D игры от третьего лица, использующей `CharacterController` и `InputManager`.  
Код ориентирован на камеру, которая смотрит на игрока сверху‑сзади (Cinemachine).

```csharp
using UnityEngine;

/// <summary>
/// Управляет перемещением игрока в 3D (вид от третьего лица).
/// Использует InputManager для получения ввода.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Компонент статистики игрока (здоровье, скорость и т.д.).")]
    public PlayerStats playerStats;

    [Tooltip("Трансформ камеры, относительно которой считается движение. Обычно main camera.")]
    public Transform cameraTransform;

    [Header("Движение и физика")]
    [Tooltip("Гравитация (отрицательное значение).")]
    public float gravity = -9.81f;

    [Tooltip("Небольшая отрицательная скорость, чтобы прижимать игрока к земле.")]
    public float groundedGravity = -2f;

    [Tooltip("Множитель скорости при спринте.")]
    public float sprintMultiplier = 1.5f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // Если InputManager ещё не создан, выходим
        if (InputManager.Instance == null)
            return;

        HandleMovement();
        HandleJump();

        // Сбрасываем флаги нажатых кнопок (если используется такая логика в InputManager)
        InputManager.Instance.ResetButtonFlags();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 moveDirection = Vector3.zero;

        // Вычисляем направление движения относительно камеры
        if (moveInput.sqrMagnitude > 0.001f)
        {
            // Направление вперёд камеры (по плоскости XZ)
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            forward.y = 0f;
            forward.Normalize();

            // Направление вправо от камеры
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            right.y = 0f;
            right.Normalize();

            moveDirection = forward * moveInput.y + right * moveInput.x;
            moveDirection.Normalize();
        }

        // Скорость берём из PlayerData через PlayerStats
        float speed = 5f;
        float rotationSpeed = 720f;

        if (playerStats != null && playerStats.playerData != null)
        {
            speed = playerStats.playerData.moveSpeed;
            rotationSpeed = playerStats.playerData.rotationSpeed;
        }

        // Учитываем спринт (если он настроен в InputManager)
        if (InputManager.Instance.IsSprintHeld())
        {
            speed *= sprintMultiplier;
        }

        Vector3 horizontalVelocity = moveDirection * speed;

        // Проверка на землю (используем isGrounded CharacterController)
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
        {
            // Небольшое прижатие к земле
            verticalVelocity.y = groundedGravity;
        }

        // Применяем гравитацию
        verticalVelocity.y += gravity * Time.deltaTime;

        // Итоговая скорость = горизонтальное + вертикальное движение
        Vector3 velocity = horizontalVelocity + verticalVelocity;

        // Перемещаем игрока
        characterController.Move(velocity * Time.deltaTime);

        // Поворот по направлению КАМЕРЫ (игрок всегда смотрит туда же, куда и камера).
        // Поворот зависит только от положения камеры (мышь/геймпад), а не от W/S.
        // W/S просто двигают вперёд/назад по направлению камеры, A/D дают "стрейф"
        // влево/вправо без разворота персонажа.
        if (cameraTransform != null)
        {
            // Берём направление вперёд камеры по плоскости XZ
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Mathf.Deg2Rad * Time.deltaTime
                );
            }
        }
    }

    private void HandleJump()
    {
        if (!isGrounded)
            return;

        // Проверяем, был ли нажат прыжок в этом кадре
        if (InputManager.Instance.IsJumpPressed())
        {
            float jumpForce = 5f;

            if (playerStats != null && playerStats.playerData != null)
            {
                jumpForce = playerStats.playerData.jumpForce;
            }

            // Формула для расчёта начальной скорости прыжка
            verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
}
```

Обрати внимание:

- Направление движения считается **относительно камеры** (`cameraTransform.forward/right`).
- Скорость и сила прыжка берутся из `PlayerData`, если он назначен.
- Гравитация и прыжок реализованы через вертикальную скорость `verticalVelocity`.
- `RequireComponent(typeof(CharacterController))` гарантирует, что компонент будет добавлен.

---

## 5. Подключение PlayerController к объекту Player

1. Открой сцену `GameScene`.
2. Выбери объект `Player` в `Hierarchy`.
3. Убедись, что на нём уже есть:
   - `CharacterController`,
   - `PlayerStats`.
4. Нажми `Add Component` → добавь `PlayerController`.
5. В Inspector:
   - Поле `Player Stats` можно оставить пустым — скрипт сам попробует найти компонент на том же объекте.
   - Поле `Camera Transform` — можно оставить пустым, если основная камера отмечена как `MainCamera` (тогда берётся `Camera.main.transform`).

Запусти игру и убедись, что:

- движение по WASD работает;
- игрок поворачивается в сторону движения;
- прыжок срабатывает на назначенную кнопку (`Jump` в Input System).

---

## 6. Варианты управления для других форматов

Ниже — примеры **упрощённых вариантов** для других типов игр. Их можно реализовать в отдельных контроллерах (например, `PlayerController2D`, `PlayerControllerTopDown`), чтобы не усложнять один класс.

### 6.1. 2D платформер (движение по X, прыжок по Y)

Особенности:

- Используется `Rigidbody2D` и `Collider2D`.
- Движение только влево/вправо (ось X).
- Прыжок — импульс по оси Y.

Пример базовой структуры:

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public PlayerStats playerStats;

    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;

        float speed = playerStats != null && playerStats.playerData != null
            ? playerStats.playerData.moveSpeed
            : 5f;

        // Движение только по оси X
        Vector2 velocity = rb.velocity;
        velocity.x = moveInput.x * speed;
        rb.velocity = velocity;

        // Разворот спрайта по направлению движения
        if (moveInput.x > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (moveInput.x < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void HandleJump()
    {
        // Здесь можно сделать проверку на землю (Raycast/Overlap)
        // и при IsJumpPressed добавить импульс вверх.
    }
}
```

Это пример структуры — детали (проверка земли, анимация) можно добавить позже.

### 6.2. Вид сверху (top‑down)

Особенности:

- Камера сверху, игрок может двигаться во все стороны по плоскости.
- Можно поворачивать игрока в сторону движения или в сторону мыши.

Базовая идея:

```csharp
using UnityEngine;

public class PlayerControllerTopDown : MonoBehaviour
{
    public PlayerStats playerStats;

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;

        float speed = playerStats != null && playerStats.playerData != null
            ? playerStats.playerData.moveSpeed
            : 5f;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            transform.position += move * speed * Time.deltaTime;

            // Поворот в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = targetRotation;
        }
    }
}
```

Такой контроллер не использует `CharacterController`/`Rigidbody`, а двигает `transform` напрямую — для простых top‑down игр этого достаточно.

---

## 7. Переход от TestInput к PlayerController

Если у тебя уже есть временный скрипт `TestInput`, который напрямую использует `Input.GetAxis` и двигает игрока:

1. Открой `TestInput` и коротко законспектируй:
   - какие оси/клавиши он использует;
   - какую формулу движения применяет.
2. Реализуй аналогичную логику в `PlayerController`, но:
   - ввод бери из `InputManager.Instance.MoveInput`, `IsJumpPressed()`, `IsSprintHeld()` и т.д.;
   - скорость и силу прыжка — из `PlayerStats.playerData`.
3. На объекте `Player`:
   - выключи галочку `TestInput` (оставь компонент, но отключи).
   - включи `PlayerController`.
4. Протестируй:
   - если всё работает корректно, удали `TestInput` из префаба игрока.

Так ты плавно перейдёшь от временного решения к архитектурно правильному.

---

## 8. Проверка понимания

Ответь устно:

1. Как делят обязанности между собой `InputManager`, `PlayerController` и `PlayerStats`?
2. Почему движение считается относительно камеры, а не только по глобальным осям X/Z?
3. Чем будет отличаться реализация контроллера:
   - в 3D от третьего лица;
   - в 2D платформере;
   - в top‑down RPG?

Если код компилируется и управление работает — переходи к уроку 5.4 (сборка префаба игрока).

