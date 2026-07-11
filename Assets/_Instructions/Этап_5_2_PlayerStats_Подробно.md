# Урок 5.2: PlayerStats — статистика игрока

## 1. Цели урока

- **Техническая цель**: создать компонент `PlayerStats`, который:
  - хранит текущее здоровье и ману игрока;
  - инициализирует эти значения из `PlayerData`;
  - обрабатывает урон и лечение;
  - оповещает другие системы (UI, эффекты) через события.
- **Обучающая цель**: показать **инкапсуляцию** — здоровье и мана изменяются только через методы класса, а не напрямую.

---

## 2. Роль PlayerStats в системе игрока

Напоминание:

- `PlayerData` (ScriptableObject) — хранит **базовые** значения (максимальное здоровье, скорость и т.п.).
- `PlayerStats` (MonoBehaviour на `Player`) — хранит **текущие** значения во время игры:
  - текущее здоровье;
  - текущую ману/энергию.

Позже:

- `PlayerProgression` будет отвечать за опыт и уровни.
- UI будет подписываться на события из `PlayerStats`, чтобы обновлять полоски здоровья/маны.

**Важно:** Другие скрипты не должны напрямую изменять поля `currentHealth` и `currentMana` (они приватные). Для чтения используйте свойства `CurrentHealth` и `CurrentMana`, а для изменения — методы `TakeDamage`, `Heal`, `AddMana` и т.д. Это обеспечивает **инкапсуляцию** — все изменения проходят через методы класса, что гарантирует вызов событий и валидацию данных.

---

## 3. Подготовка

Перед началом убедись, что:

1. Есть скрипт `PlayerData` и ассет `PlayerData_Default` (из урока 5.1).
2. Есть папка `Assets/Scripts/Player/`.

---

## 4. Создание скрипта PlayerStats

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/Scripts/Player/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`PlayerStats`**.
4. Открой его в редакторе.

### 4.2. Что должно быть в классе

Класс должен:

- наследоваться от `MonoBehaviour`;
- иметь ссылку на `PlayerData`;
- хранить текущие значения здоровья и маны;
- иметь методы для урона/лечения;
- иметь события, чтобы другие системы могли реагировать на изменения.

Ниже пример кода. Используй его как ориентир и старайся понимать каждую строку.

```csharp
using System;
using UnityEngine;

/// <summary>
/// Отвечает за текущие характеристики игрока:
/// здоровье, мана и события, связанные с ними.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Данные игрока")]
    [Tooltip("ScriptableObject с базовыми параметрами игрока.")]
    public PlayerData playerData;

    [Header("Текущее состояние")]
    [SerializeField]
    [Tooltip("Текущее здоровье игрока.")]
    private float currentHealth;

    [SerializeField]
    [Tooltip("Текущая мана (или энергия).")]
    private float currentMana;

    /// <summary>
    /// Текущее здоровье игрока (только для чтения).
    /// Для изменения используйте методы TakeDamage() или Heal().
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// Текущая мана игрока (только для чтения).
    /// Для изменения используйте метод AddMana().
    /// </summary>
    public float CurrentMana => currentMana;

    // События для связи с другими системами (UI, эффекты и т.п.)
    public event Action<float, float> OnHealthChanged; // (текущее, максимальное)
    public event Action<float, float> OnManaChanged;   // (текущая, максимальная)
    public event Action OnDeath;

    private void Awake()
    {
        InitializeFromData();
    }

    /// <summary>
    /// Инициализация текущих значений из PlayerData.
    /// </summary>
    public void InitializeFromData()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerStats: PlayerData не назначен!", this);
            return;
        }

        currentHealth = Mathf.Clamp(playerData.maxHealth, 1f, float.MaxValue);
        currentMana = Mathf.Clamp(playerData.maxMana, 0f, float.MaxValue);

        // Сигнализируем подписчикам начальные значения
        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }

    /// <summary>
    /// Нанесение урона игроку.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.TakeDamage: PlayerData не назначен.", this);
            return;
        }

        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);

        if (currentHealth <= 0f)
        {
            // Игрок "умирает"
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Лечение игрока.
    /// </summary>
    public void Heal(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.Heal: PlayerData не назначен.", this);
            return;
        }

        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
    }

    /// <summary>
    /// Изменение маны (может быть как расход, так и восстановление).
    /// </summary>
    public void AddMana(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.AddMana: PlayerData не назначен.", this);
            return;
        }

        if (Mathf.Approximately(amount, 0f))
            return;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0f, playerData.maxMana);

        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }
}
```

Обрати внимание:

- Поля `currentHealth` и `currentMana` сделаны **приватными** (`private`) с атрибутом `[SerializeField]`, чтобы они были видны в инспекторе Unity, но не доступны напрямую из других скриптов.
- Для чтения значений добавлены публичные свойства `CurrentHealth` и `CurrentMana` (только для чтения).
- Это пример **инкапсуляции** — защита данных от неправильного использования. Все изменения проходят через методы (`TakeDamage`, `Heal`, `AddMana`), что гарантирует:
  - вызов событий (`OnHealthChanged`, `OnDeath` и т.д.);
  - валидацию данных (проверка на отрицательные значения, ограничение максимумами);
  - единую точку контроля логики.
- Логика проверяет, что `playerData` не равен `null`.
- Здоровье и мана **ограничиваются** через `Mathf.Clamp`, чтобы не выйти за 0 и максимум.
- События вызываются каждый раз, когда значение меняется — это позволит UI обновлять полоски без прямого опроса.

---

## 5. Добавление PlayerStats на объект Player

1. Открой сцену `GameScene`.
2. Найди в `Hierarchy` объект `Player`.
3. Убедись, что на нём уже есть необходимые компоненты физики:
   - Для 3D третьего лица: `CharacterController` (или `Rigidbody` + `Collider`).
4. Добавь компонент `PlayerStats`:
   - В Inspector нажми `Add Component`.
   - Найди `PlayerStats` и добавь его.
5. В поле `Player Data` назначь ранее созданный `PlayerData_Default`.

При запуске игры (Play) компонент `PlayerStats` в `Awake` возьмёт значения из `PlayerData_Default` и установит стартовое здоровье и ману.

---

## 6. Пример использования PlayerStats в других скриптах

### 6.1. Простой тест урона/лечения

Создай временный тестовый скрипт `PlayerStatsTest.cs` (например, в `Assets/Scripts/Player/`), чтобы попрактиковаться в использовании методов и событий:

```csharp
using UnityEngine;

/// <summary>
/// Временный скрипт для проверки работы PlayerStats.
/// </summary>
public class PlayerStatsTest : MonoBehaviour
{
    public PlayerStats playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged += HandleHealthChanged;
        playerStats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged -= HandleHealthChanged;
        playerStats.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (playerStats == null) return;

        // Нанести урон по нажатию клавиши H
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerStats.TakeDamage(10f);
        }

        // Вылечить по нажатию клавиши J
        if (Input.GetKeyDown(KeyCode.J))
        {
            playerStats.Heal(10f);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        Debug.Log($"Здоровье изменилось: {current} / {max}");
    }

    private void HandleDeath()
    {
        Debug.Log("Игрок умер!");
    }
}
```

Шаги:

1. Добавь этот скрипт на `Player` (или другой объект) на время тестов.
2. Запусти сцену.
3. Нажимай `H` — здоровье уменьшается, логи пишутся в консоль.
4. Нажимай `J` — здоровье восстанавливается.

Позже, когда появится полноценная система ввода и урона от врагов, этот тестовый скрипт можно удалить.

---

## 7. Вариации для разных форматов игр

- **2D платформер**:
  - `PlayerStats` остаётся тем же — здоровье и мана не зависят от измерения.
  - Отличается только контроллер движения (в уроке 5.3).
- **Top‑down RPG**:
  - В `PlayerData` и `PlayerStats` можно добавить дополнительные поля:
    - физический/магический урон;
    - броню;
    - сопротивления и т.п.
  - Логика получения урона (`TakeDamage`) может стать сложнее (учёт брони, критов).
- **Рогалик / шутер**:
  - Может быть только здоровье (без маны).
  - При смерти (`OnDeath`) запускается система респауна / перезапуска уровня.

Архитектура остаётся прежней: `PlayerStats` отвечает за текущие ресурсы игрока и уведомляет мир об их изменении.

---

## 8. Проверка понимания

Проверь себя, ответив на следующие вопросы:

1. Чем `PlayerStats` отличается от `PlayerData`?
2. Почему поля `currentHealth` и `currentMana` сделаны приватными, а не публичными?
3. Как другие скрипты могут прочитать текущее здоровье игрока?
4. Почему важно изменять здоровье через методы `TakeDamage()` и `Heal()`, а не напрямую?
5. Как UI может узнать, что здоровье изменилось?
6. Что будет, если не проверять `playerData` на `null`?

Если ответы понятны и код работает — переходи к уроку 5.3 (`PlayerController`).

