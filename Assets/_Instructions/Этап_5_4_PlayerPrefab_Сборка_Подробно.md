# Урок 5.4: Сборка префаба игрока

## 1. Цели урока

- **Техническая цель**: собрать все части системы игрока на одном `GameObject`:
  - компоненты физики (CharacterController / Rigidbody);
  - `PlayerStats`;
  - `PlayerController`;
  - (позже) `PlayerProgression`;
  - объект `CameraTarget` для камеры.
- **Обучающая цель**: показать принцип **композиции** — объект игрока состоит из нескольких компонентов, каждый из которых отвечает за свою часть поведения.

---

## 2. Архитектура объекта Player

К этому моменту у тебя уже есть:

- `PlayerData` (ScriptableObject с базовыми параметрами).
- `PlayerStats` (текущее здоровье/мана и события).
- `PlayerController` (логика движения и прыжка).
- Настроенный `InputManager`.
- Настроенная Cinemachine‑камера (Этап 4).

Объект `Player` в сцене должен выглядеть примерно так:

- **GameObject `Player`**
  - `CharacterController` (или другой компонент физики).
  - `PlayerStats`.
  - `PlayerController`.
  - (позже) `PlayerProgression`.
  - `MeshRenderer` / `SkinnedMeshRenderer` или временный примитив (Capsule).
  - Дочерний объект `CameraTarget`.

---

## 3. Подготовка объекта Player в GameScene

### 3.1. Создание / проверка объекта Player

1. Открой сцену `GameScene`.
2. В `Hierarchy` найди объект `Player`:
   - Если он уже есть — используй его.
   - Если нет — создай новый объект:
     - `GameObject` → `Create Empty` → назови `Player`.

### 3.2. Добавление временной модели

Чтобы видеть игрока в сцене:

1. В `Hierarchy` выбери `Player`.
2. `GameObject` → `3D Object` → выбери, например, `Capsule`.
3. Переименуй капсулу в `Visual` (или `Model`) и сделай её **дочерним** объектом `Player`.
4. Убедись, что позиция `Visual` = `(0, 0, 0)` относительно `Player`.

---

## 4. Добавление компонентов физики

### 4.1. Вариант для 3D третьего лица (основной)

1. Выбери `Player` в `Hierarchy`.
2. В Inspector нажми `Add Component` и добавь:
   - `CharacterController`.
3. Настрой параметры `CharacterController`:
   - `Center` — примерно на половине высоты капсулы.
   - `Radius` — такой же, как у капсулы.
   - `Height` — равен высоте капсулы.

### 4.2. Вариант для других форматов (по желанию)

- **2D платформер**:
  - Вместо `CharacterController` используй `Rigidbody2D` + `BoxCollider2D`/`CapsuleCollider2D`.
- **Top‑down 3D**:
  - Можно использовать `Rigidbody` + `CapsuleCollider`, а контроллер движения писать отдельно (см. урок 5.3).

На этом уроке основной сценарий — 3D третье лицо с `CharacterController`.

---

## 5. Добавление PlayerStats и PlayerData

1. На объекте `Player` нажми `Add Component` → добавь `PlayerStats`.
2. В Inspector в компоненте `PlayerStats`:
   - В поле `Player Data` перетащи ассет `PlayerData_Default` из `Assets/ScriptableObjects/Player/`.
3. Запусти сцену и убедись, что:
   - Ошибок в консоли нет.
   - В режиме Play у `PlayerStats` свойства `CurrentHealth` и `CurrentMana` инициализировались (равны значениям из `PlayerData_Default`).

Если что‑то не так — вернись к урокам 5.1–5.2 и проверь код.

---

## 6. Добавление PlayerController

1. На объекте `Player` нажми `Add Component` → добавь `PlayerController`.
2. В Inspector:
   - Убедись, что:
     - `CharacterController` найден (скрипт с атрибутом `[RequireComponent]` гарантирует его наличие).
     - Если `PlayerStats` не назначен явно, `PlayerController` найдёт его в `Awake()`.
   - Поле `Camera Transform` можно оставить пустым, если основная камера отмечена как `MainCamera` — тогда в коде возьмётся `Camera.main.transform`.
3. Запусти сцену и проверь:
   - Игрок двигается по вводу (WASD / геймпад).
  - Игрок смотрит туда же, куда смотрит камера (стрейфовый стиль управления).
   - Прыжок работает.

Если движение есть, но камера не работает — проверь настройки Cinemachine (см. Этап 4).

---

## 7. Настройка CameraTarget для Cinemachine

Чтобы камера плавно следовала за игроком, обычно используют промежуточный объект `CameraTarget`:

1. В `Hierarchy` выбери `Player`.
2. ПКМ по `Player` → `Create Empty` → назови `CameraTarget`.
3. Перемести `CameraTarget`:
   - По оси Y — на уровень головы/груди персонажа (чтобы камера смотрела примерно в центр модели).
4. Выбери виртуальную камеру `CM_PlayerCamera` (или как она у тебя называется).
5. В Inspector настрои:
   - `Follow` = `Player/CameraTarget`.
   - `Look At` = `Player/CameraTarget`.

Если у тебя есть `CameraTargetController` (из Этапа 4), убедись, что он:

- висит на `CameraTarget`;
- использует `InputManager` для вращения камеры (`LookInput`).

---

## 8. Тестовый скрипт для проверки связи InputManager → PlayerController → PlayerStats

Чтобы убедиться, что всё вместе работает корректно, можно временно добавить простой тестовый скрипт, который, например, наносит урон при падении или по кнопке.

Пример (добавь в `Assets/Scripts/Player/` файл `PlayerDebugHUD.cs` и **по желанию** повесь на `Player` или пустой объект в сцене):

```csharp
using UnityEngine;

/// <summary>
/// Временный отладочный скрипт:
/// - показывает базовую информацию о здоровье,
/// - позволяет наносить урон/лечение по горячим клавишам.
/// </summary>
public class PlayerDebugHUD : MonoBehaviour
{
    public PlayerStats playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        if (playerStats == null)
            return;

        // Нанести урон (K)
        if (Input.GetKeyDown(KeyCode.K))
        {
            playerStats.TakeDamage(10f);
        }

        // Вылечить (L)
        if (Input.GetKeyDown(KeyCode.L))
        {
            playerStats.Heal(10f);
        }
    }

    private void OnGUI()
    {
        if (playerStats == null || playerStats.playerData == null)
            return;

        GUI.Label(new Rect(10, 10, 300, 20),
            $"HP: {playerStats.CurrentHealth} / {playerStats.playerData.maxHealth}");
    }
}
```

Этот скрипт не обязателен, но он наглядно показывает:

- что здоровье инициализируется из `PlayerData`;
- что методы `TakeDamage` и `Heal` работают.

После завершения отладки его можно удалить.

---

## 9. Создание префаба Player

Чтобы переиспользовать собранного игрока в других сценах, нужно сделать из него префаб.

1. В окне `Project` перейди в папку `Assets/Prefabs/Player/`.
   - Если папки ещё нет:
     - Создай `Assets/Prefabs/`,
     - внутри неё `Prefabs/Player/`.
2. В `Hierarchy` перетащи объект `Player` в папку `Prefabs/Player/`.
3. Unity создаст префаб `Player.prefab`.
4. Убедись, что:
   - На префабе `Player` все нужные компоненты присутствуют:
     - `CharacterController`;
     - `PlayerStats` (с назначенным `PlayerData_Default`);
     - `PlayerController`;
     - (позже — `PlayerProgression`);
   - Дочерний объект `CameraTarget` есть и находится в правильной точке.

После этого в любой новой сцене ты можешь просто перетащить `Player.prefab` из `Project` в `Hierarchy`.

---

## 10. Плавный отказ от TestInput

Если на `Player` всё ещё висит временный скрипт `TestInput`, который напрямую использует старый ввод:

1. Открой префаб `Player` для редактирования (двойной клик или `Open Prefab`).
2. Найди компонент `TestInput` в Inspector.
3. Временно **отключи** его (сними галочку).
4. Убедись, что `PlayerController` включён.
5. Запусти игру:
   - если управление работает корректно (InputManager → PlayerController), компонент `TestInput` больше не нужен.
6. Удали `TestInput` с префаба:
   - нажми на шестерёнку у компонента → `Remove Component`.

Сам файл `TestInput.cs` можно оставить в папке вроде `Scripts/_Legacy/` как пример “как было раньше”.

---

## 11. Отличия состава префаба в разных жанрах

Обсуди (с учеником или для себя), как будет отличаться набор компонентов на `Player` в разных играх:

- **3D Action‑RPG (вид от третьего лица)**:
  - `CharacterController`, `Animator`, `PlayerStats`, `PlayerController`, `PlayerProgression`, `WeaponManager`, `InventorySystem` и т.п.
- **2D платформер**:
  - `Rigidbody2D`, `Collider2D`, `SpriteRenderer`, 2D‑вариант контроллера (см. урок 5.3), `PlayerStats`.
- **Top‑down шутер**:
  - `Rigidbody`/`CharacterController`, контроллер движения в плоскости, прицеливание по мыши, `PlayerStats`, возможно — отдельный компонент прицеливания.

Во всех случаях сохраняется **идея композиции**: мы не делаем один гигантский скрипт, а собираем поведение из нескольких компонентов.

---

## 12. Проверка

Проверь, что:

1. В `GameScene` есть экземпляр `Player` (из префаба).
2. На объекте `Player`:
   - есть `CharacterController`, `PlayerStats`, `PlayerController`;
   - `PlayerStats` ссылается на `PlayerData_Default`;
   - при старте игры ошибок в консоли нет.
3. Камера следует за `Player/CameraTarget`.
4. Движение и прыжок работают.

Если всё так — можно переходить к уроку 5.5 (`PlayerProgression` — система прокачки).

