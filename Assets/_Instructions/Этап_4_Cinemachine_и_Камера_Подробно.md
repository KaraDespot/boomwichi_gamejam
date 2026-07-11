# Урок 4: Cinemachine и камера от третьего лица (подробная инструкция)

## Зачем нужна Cinemachine?

В игре от третьего лица нужна камера, которая:
- **Следит за игроком** — всегда держит игрока в центре
- **Плавно поворачивается** — когда игрок двигает мышью
- **Не проваливается сквозь стены** — автоматически отодвигается при столкновении
- **Плавно следует** — без рывков и тряски

**Проблема обычной камеры:**
- Нужно писать много кода для следования за игроком
- Сложно настроить плавность
- Нет автоматической обработки столкновений

**Преимущества Cinemachine:**
- ✅ Готовые решения для разных типов камер
- ✅ Визуальная настройка в Editor
- ✅ Автоматическая обработка столкновений
- ✅ Плавные переходы между камерами

---

## ⚠️ ВАЖНО: Проверка перед началом

**Перед началом убедитесь:**
1. **Input System настроен** (Этап 3 выполнен)
2. **InputManager создан и работает**
3. **Префаб игрока уже сделан** (из предыдущих этапов) и его можно поставить в `GameScene`
4. **Cinemachine установлен** (версия `3.1.5`)

---

## Часть 1: Установка Cinemachine

### Шаг 1: Установка пакета через Package Manager

1. Откройте `Window` → `Package Manager`
2. В левом верхнем углу выберите `Unity Registry` (не `My Assets`)
3. В поиске введите: `Cinemachine`
4. Найдите пакет `Cinemachine` (от Unity Technologies)
5. Нажмите `Install` (в правом нижнем углу)
6. Дождитесь установки (может занять несколько секунд)

**Проверка:**
- После установки в меню `GameObject` должно появиться `Cinemachine`
- Если не появилось — перезапустите Unity

### Шаг 2: Проверка установки

1. Откройте `Edit` → `Project Settings` → `Package Manager`
2. В списке пакетов должен быть `Cinemachine`
3. Или проверьте файл `Packages/manifest.json` — там должна быть строка с `com.unity.cinemachine`

---

## Часть 2: Понимание Cinemachine

### Что такое Cinemachine Camera (CM Camera)?

В Cinemachine 3.x (Unity 6) используется компонент **`CinemachineCamera`**.
В Hierarchy он обычно выглядит как объект **CM Camera** (или “Cinemachine Camera”).

**Cinemachine Camera** — это “контроллер камеры”, который:
- Определяет, как должна вести себя камера
- Следит за целью (Follow target)
- Смотрит на цель (Look At target)
- Настраивает позицию, поворот, плавность

**Аналогия:**
- CM Camera (Cinemachine Camera) = "Инструкция для камеры"
- Main Camera = "Реальная камера, которая следует инструкциям"

### Как работает Cinemachine?

1. **CinemachineBrain** (на Main Camera) — читает все CM Cameras и применяет их состояние к реальной камере
2. **Cinemachine Camera** — содержит Follow/LookAt и процедурную логику камеры
3. В Cinemachine 3.x в Inspector ты часто увидишь не “Body/Aim”, а блоки типа **Position Control** / **Rotation Control**
   - Это те же этапы “пайплайна”, просто показаны современными названиями

---

## Часть 3: Настройка CM камеры в GameScene

### Шаг 1: Подготовка сцены

1. Откройте сцену `GameScene` (или `Gameplay`)
2. Убедитесь, что есть:
   - **Main Camera** (должна быть по умолчанию)
   - **Directional Light** (для освещения)
   - **Plane или Terrain** (для земли)

### Шаг 2: Добавление CinemachineBrain

1. Выберите объект **Main Camera** в Hierarchy
2. В Inspector нажмите `Add Component`
3. Найдите и добавьте `Cinemachine Brain`
4. Оставьте настройки по умолчанию (они подходят для начала)

**Что делает CinemachineBrain:**
- Читает настройки из CM Cameras
- Применяет их к Main Camera
- Управляет переходами между камерами

### Шаг 3: Создание CM камеры (Cinemachine)

1. В меню выберите `GameObject` → `Cinemachine` → `Cinemachine Camera`
2. Назовите объект `CM_PlayerCamera`

> Если у тебя есть пункт `3rd Person Camera` — можно выбрать его. Главное, чтобы итоговый объект был **Cinemachine Camera** и у него был включен **Third Person Follow**.

### Шаг 4: Настройка CM камеры под нашего игрока

#### Шаг 4.1: Добавляем `CameraTarget` в префаб игрока

1. Открой префаб игрока.
2. Внутри игрока создай пустой объект: `Create Empty`
3. Назови его `CameraTarget`
4. Выставь локальную позицию (пример): `(0, 1.5, 0)`
5. Сохрани префаб.

> **Важно:** Third Person Follow не “крутится мышью” сам. Камера ориентируется по повороту **Tracking Target**. Поэтому мы будем вращать `CameraTarget`.

#### Шаг 4.2: Назначаем Follow/LookAt

1. Поставь игрока (префаб) на сцену `GameScene`.
2. Выбери `CM_PlayerCamera`.
3. Назначь:
   - **Follow** → `Player/CameraTarget`
   - **Look At** → `Player/CameraTarget` (можно оставить так)

#### Шаг 4.3: Включаем Third Person Follow 

1. На `CM_PlayerCamera` найди блок **Position Control** (или похожий) и выбери:
   - **Third Person Follow**

**Рекомендуемые стартовые значения (Third Person Follow):**
- **Shoulder Offset**: `(0.7, 0.3, -0.5)`
- **Vertical Arm Length**: `0.5`
- **Camera Side**: `1`
- **Camera Distance**: `5`
- **Damping**: `0.3` (пример)

---

## Часть 4: Управление поворотом камеры (CameraTarget)


Для режима **Third Person Follow** важно следующее:

- **Third Person Follow не управляется вводом напрямую**.
- Камера следует и ориентируется **относительно Tracking Target**.
- Чтобы камера вращалась от мыши, нужно **вращать Tracking Target**.

Поэтому мы сделали `Player/CameraTarget`, и теперь будем вращать именно его.

### Шаг 1: Создание скрипта `CameraTargetController`

1. Создайте скрипт `CameraTargetController.cs` в папке `Assets/_Scripts/Core/`
2. Повесьте этот скрипт на объект `Player/CameraTarget` (в префабе игрока)
3. Вставьте код:

```csharp
using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -30f; // вниз
    [SerializeField] private float maxVerticalAngle = 60f;  // вверх

    [Header("State")]
    [SerializeField] private float currentYaw = 0f;    // Y
    [SerializeField] private float currentPitch = 20f; // X

    private void Awake()
    {
        // Берём стартовый поворот из инспектора (если выставляли руками)
        Vector3 euler = transform.localRotation.eulerAngles;
        currentYaw = euler.y;
        currentPitch = NormalizeAngle(euler.x);
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        Vector2 lookInput = InputManager.Instance.GetLookInput();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        currentYaw += mouseX;
        currentPitch -= mouseY; // инверсия Y для “как в большинстве 3rd-person”
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }

    public float GetMouseSensitivity() => mouseSensitivity;

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
```

---

## Часть 5: Интеграция и тест

### Шаг 1: Проверяем `CM_PlayerCamera`

1. Выбери `CM_PlayerCamera`.
2. Проверь:
   - **Follow** = `Player/CameraTarget`
   - **Look At** = `Player/CameraTarget` (можно оставить так)
   - **Position Control** = `Third Person Follow`

### Шаг 2: Тестирование

1. Запусти игру через `Bootstrap`.
2. Перейди в `GameScene`.
3. Двигай мышью — камера должна вращаться вокруг игрока.
4. WASD — игрок двигается, камера следует.

#### Что проверить и как добавить: пауза и камера

**Что проверить:** При паузе (Esc) камера не должна реагировать на мышь. После снятия паузы — снова реагирует.

**Почему так работает:** InputManager при паузе отключает карту Player и включает UI (см. Этап 3). Пока Player отключена, `LookInput` равен нулю — `CameraTargetController` не получает ввод. Отдельно останавливать `CameraTargetController` не нужно.

**Если пауза через Input System ещё не добавлена** — сделай по инструкции **Этап 3**:
- В карте **Player** добавь действие **Pause** (Button), привязки: Escape, \<Gamepad\>/start.
- В **InputManager**: подписка на Pause и UI/Cancel, события `OnPausePressed` и `OnCancelPressed`; при паузе отключать Player и включать UI, при возобновлении — наоборот.
- В **PauseController**: убрать проверку `Input.GetKeyDown(KeyCode.Escape)` в `Update()`; подписаться на `InputManager.Instance.OnPausePressed` (если игра идёт — вызвать `GameManager.Pause()`) и на `InputManager.Instance.OnCancelPressed` (если игра на паузе — вызвать `GameManager.Resume()`).

**Если камера не поворачивается:**
- Проверь, что `InputManager` создан
- Проверь, что `Look` action работает (см. тест из Этапа 3)
- Проверь, что `CameraTargetController` висит на `Player/CameraTarget` и включен

**Если камера не следует за игроком:**
- Проверь, что в `CM_PlayerCamera` назначен `Follow = Player/CameraTarget`
- Проверь, что на `CM_PlayerCamera` выбран режим `Third Person Follow`

---

## Часть 6: Улучшение (опционально)

### Добавление плавности поворота

Если поворот слишком резкий, можно добавить плавность в `CameraTargetController`.
Суть: отдельно храним **целевые** углы и плавно “догоняем” их текущими.

```csharp
[Header("Smoothing")]
[SerializeField] private float rotationSmoothing = 5f;

// Добавь поля:
private float targetYaw;
private float targetPitch;

// В Awake() после инициализации currentYaw/currentPitch:
targetYaw = currentYaw;
targetPitch = currentPitch;

// Используй вместо прямого применения в Update():
private void ApplyLookSmoothed(Vector2 lookInput)
{
    float mouseX = lookInput.x * mouseSensitivity;
    float mouseY = lookInput.y * mouseSensitivity;

    // Целевые углы
    targetYaw += mouseX;
    targetPitch -= mouseY;
    targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);

    // Плавное вращение
    currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationSmoothing * Time.deltaTime);
    currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationSmoothing * Time.deltaTime);

    transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
}
```

**Настройка:**
- `rotationSmoothing = 5f` — плавность поворота (больше = плавнее, но медленнее)

### Зум камеры через Input System

Зум делаем через Input System, чтобы вся настройка ввода была в одном месте и при необходимости можно было перепривязать клавиши.

**Что сделать (по шагам):**

1. **В InputSystem_Actions (карта Player)**  
   - Добавь действие **Zoom**: тип **Value**, тип значения **Vector2**, привязка **\<Mouse\>/scroll** (колесо мыши даёт Vector2; для зума используем компонент .y).  
   - Сохрани Asset.

2. **В InputManager**  
   - Поле: `private InputAction zoomAction;`  
   - Свойство: `public float ZoomInput { get; private set; }`  
   - В `InitializeInputSystem()`: `zoomAction = playerActionMap.FindAction("Zoom");`  
   - В `UpdateInputValues()`: `ZoomInput = zoomAction != null ? zoomAction.ReadValue<Vector2>().y : 0f;`  
   - Публичный метод: `public float GetZoomInput() { return ZoomInput; }`

3. **Скрипт ThirdPersonFollowZoom**  
   - Оставь на `CM_PlayerCamera`.  
   - Вместо `Input.mouseScrollDelta.y` читай зум из InputManager:  
     `float scroll = InputManager.Instance != null ? InputManager.Instance.GetZoomInput() : 0f;`  
   - Остальная логика без изменений: по `scroll` меняешь `follow.CameraDistance` в допустимых пределах.

**Полный код обновлённого ThirdPersonFollowZoom:**

```csharp
using UnityEngine;
using Unity.Cinemachine;

public class ThirdPersonFollowZoom : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private float minCameraDistance = 2f;
    [SerializeField] private float maxCameraDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;

    private CinemachineThirdPersonFollow follow;

    private void Awake()
    {
        if (cmCamera == null)
            cmCamera = GetComponent<CinemachineCamera>();

        if (cmCamera != null)
            follow = cmCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
    }

    private void Update()
    {
        if (follow == null)
            return;

        float scroll = InputManager.Instance != null ? InputManager.Instance.GetZoomInput() : 0f;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            follow.CameraDistance = Mathf.Clamp(
                follow.CameraDistance - scroll * zoomSpeed,
                minCameraDistance,
                maxCameraDistance
            );
        }
    }
}
```

**Настройка:**
- `minCameraDistance = 2f` — минимальное расстояние (близко к игроку)
- `maxCameraDistance = 10f` — максимальное расстояние (далеко от игрока)
- `zoomSpeed = 2f` — скорость зума

---

## Объяснение: Что такое `CinemachineThirdPersonFollow`?

**`CinemachineThirdPersonFollow`** — это компонент позиционирования камеры (Position/Body) для `CinemachineCamera`, который:
- Следит за целью на заданном расстоянии
- Имеет смещение (shoulder offset) для камеры справа/слева
- Имеет встроенную систему коллизий (камера не залезает в стены)

**Основные параметры:**
- **Shoulder Offset** — смещение плеча
- **Vertical Arm Length** — вертикальная “рука”
- **Camera Side** — на каком плече камера
- **Camera Distance** — дистанция камеры
- **Damping** — плавность следования (X, Y, Z)

---

## Объяснение: Углы поворота камеры

**Горизонтальный угол (Y):**
- Поворот влево/вправо
- Вращение вокруг оси Y (вертикальной)
- Пример: `currentYaw = 90` → камера “сбоку” относительно игрока

**Вертикальный угол (X):**
- Поворот вверх/вниз
- Вращение вокруг оси X (горизонтальной)
- Пример: `currentPitch = 20` → камера смотрит слегка вниз

**Ограничение углов:**
- Вертикальный угол ограничиваем, чтобы камера не перевернулась
- Горизонтальный угол можно не ограничивать (полный оборот вокруг игрока)

---

## Частые ошибки

### CM камера не следует за игроком

**Причина:** `Follow` или `Look At` не назначены.

**Решение:**
1. Выберите `CM_PlayerCamera`
2. В компоненте `Cinemachine Camera` назначьте:
   - **Follow** → `Player/CameraTarget`
   - **Look At** → `Player/CameraTarget` (можно оставить так)

### Камера не поворачивается от мыши

**Причина:** `CameraTargetController` не получает ввод или не применяется поворот `Player/CameraTarget`.

**Решение:**
1. Проверьте, что `InputManager` создан и работает
2. Проверьте, что `Look` action работает (см. тест из Этапа 3)
3. Проверьте, что `CameraTargetController` висит на `Player/CameraTarget` и включен
4. Проверьте, что в `CM_PlayerCamera` назначен `Follow = Player/CameraTarget`

### Камера проваливается сквозь стены

**Причина:** чаще всего неверный слой/фильтр коллизий или используется не `Third Person Follow`.

**Решение для `Third Person Follow`:**
1. Выберите `CM_PlayerCamera`
2. В `Third Person Follow` проверьте **Avoid Obstacles / Camera Collision Filter**
   - Убедитесь, что слой стен/препятствий включён
3. (Опционально) выставьте `Ignore Tag` в тег игрока, чтобы камера игнорировала персонажа

> `CinemachineCollider` обычно нужен для других режимов позиционирования, а для `Third Person Follow` коллизии встроены.

### Камера трясется или дергается

**Причина:** Слишком высокая чувствительность или нет плавности.

**Решение:**
1. Уменьшите `Mouse Sensitivity` в `CameraTargetController`
2. Добавьте плавность поворота (см. раздел "Улучшение")
3. Увеличьте `Damping` в `3rd Person Follow`

---

## Дополнительные советы

### Организация камеры

**Рекомендуемая структура:**
```
GameScene
├── Main Camera (с CinemachineBrain)
├── CM_PlayerCamera (Cinemachine Camera)
└── Player (Prefab)
    └── CameraTarget (с CameraTargetController)
```

### Настройка чувствительности

**Как найти подходящую чувствительность:**
1. Начните с `mouseSensitivity = 2f`
2. Запустите игру и протестируйте
3. Если слишком быстро — уменьшите
4. Если слишком медленно — увеличьте
5. Обычно хорошие значения: `1.5f - 3f`

### Оптимизация

**Совет по порядку обновления:**
- Если позже появятся рывки (из-за того, что игрок двигается в `Update()`, а камера тоже), попробуйте перенести поворот `CameraTarget` в `LateUpdate()`.

---

## Проверочный сценарий

1. **Запустить Play** → автоматически попали в `MainMenu`
2. **Перейти в GameScene** → через кнопку "Новая игра"
3. **Проверить Hierarchy** → должен быть объект `CM_PlayerCamera`
4. **Двигать мышью** → камера должна вращаться вокруг игрока
5. **Проверить ограничения** → камера не должна переворачиваться вверх ногами
6. **Проверить следование** → камера должна следовать за игроком
7. **Остановить Play** → убедиться, что нет ошибок/исключений

---

## Что дальше?

После настройки камеры мы перейдем к:
- **Этап 5: Игрок** — создание PlayerController (движение/поворот персонажа). Камера уже будет работать через `CameraTargetController`.

Камера будет автоматически следовать за игроком и поворачиваться от ввода мыши.

---

## Дополнительно: Альтернативные режимы камеры

### Free Look Camera (для изучения)

Если хотите камеру, которая может свободно вращаться вокруг игрока:

1. Создайте `GameObject` → `Cinemachine` → `Free Look Camera`
2. Настройте:
   - **Follow** → игрок
   - **Look At** → игрок
   - **Rigs** → три позиции камеры (близко, средняя, далеко)

**Преимущества:**
- Более гибкая настройка
- Можно переключаться между тремя позициями

**Недостатки:**
- Сложнее настраивать
- Больше параметров

### Top Down Camera (для изометрических игр)

Если нужна камера сверху:

1. Создайте `GameObject` → `Cinemachine` → `Cinemachine Camera`
2. В блоке **Position Control** выберите `Framing Transposer`
3. Настройте:
   - **Follow Offset** → `(0, 10, 0)` (камера сверху)
   - **Camera Distance** → `10` (расстояние)

**Использование:**
- Для стратегий, изометрических игр
- Камера всегда сверху, не поворачивается
