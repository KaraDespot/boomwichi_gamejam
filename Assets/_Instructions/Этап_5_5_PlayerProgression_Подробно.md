# Урок 5.5: PlayerProgression — система прокачки игрока

## 1. Цели урока

- **Техническая цель**: создать компонент `PlayerProgression`, который:
  - хранит уровень и опыт игрока;
  - считает, сколько опыта нужно до следующего уровня;
  - обрабатывает получение опыта и повышение уровня.
- **Обучающая цель**: показать, как расширять функциональность игрока через **новый компонент** (композиция), не переписывая существующие `PlayerStats` и `PlayerController`.

---

## 2. Роль PlayerProgression в системе

Теперь у нас:

- `PlayerData` — базовые параметры (здоровье, скорость и т.п.).
- `PlayerStats` — текущее здоровье/мана и события.
- `PlayerController` — движение и обработка ввода.

`PlayerProgression` будет:

- отвечать за **уровень** игрока;
- считать **опыт** и порог для повышения уровня;
- при повышении уровня (LevelUp) вызывать события и, при желании, усиливать характеристики.

**Принцип ООП:** расширение поведения через **композицию** — добавляем новый компонент вместо того, чтобы раздувать `PlayerStats`.

---

## 3. Продумываем модель прогрессии

Перед кодом продумай:

1. **Как будет расти уровень?**
   - С 1 уровня до 2, 3, 4 и т.д.
2. **Сколько опыта нужно на каждый уровень?**
   - Простой вариант — линейный:
     - 1 → 2: 100 опыта,
     - 2 → 3: 200,
     - 3 → 4: 300 и т.д.
   - Другой вариант — с множителем:
     - базовое значение `baseExperienceToNextLevel = 100`;
     - каждый следующий уровень требует `previous * multiplier` (например, `x1.5`).
3. **Что происходит при повышении уровня?**
   - Увеличиваем уровень.
   - Обнуляем или пересчитываем текущий опыт.
   - (Опционально) увеличиваем параметры в `PlayerData`/`PlayerStats`:
     - макс. здоровье;
     - мана;
     - скорость и т.п.
   - Вызываем событие `OnLevelUp`, на которое может подписаться UI (показать анимацию, текст “Level Up!”).

В примере ниже используются:

- текущий уровень;
- текущее количество опыта;
- базовое количество опыта для уровня с множителем.

---

## 4. Создание скрипта PlayerProgression

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/Scripts/Player/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`PlayerProgression`**.
4. Открой его в редакторе.

### 4.2. Реализация PlayerProgression

Пример кода:

```csharp
using System;
using UnityEngine;

/// <summary>
/// Отвечает за прогрессию игрока:
/// уровень, опыт и повышение уровня.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Ссылка на PlayerStats для возможного усиления характеристик при уровне.")]
    public PlayerStats playerStats;

    [Header("Уровень")]
    [SerializeField]
    [Tooltip("Текущий уровень игрока.")]
    private int currentLevel = 1;

    [Header("Опыт")]
    [SerializeField]
    [Tooltip("Текущее количество опыта.")]
    private float currentExperience = 0f;

    /// <summary>
    /// Текущий уровень игрока (только для чтения).
    /// Для изменения уровня используйте метод AddExperience().
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// Текущее количество опыта игрока (только для чтения).
    /// Для добавления опыта используйте метод AddExperience().
    /// </summary>
    public float CurrentExperience => currentExperience;

    [Tooltip("Базовое количество опыта для перехода с 1 на 2 уровень.")]
    public float baseExperienceToNextLevel = 100f;

    [Tooltip("Множитель роста требуемого опыта на каждый следующий уровень.")]
    public float experienceGrowthFactor = 1.5f;

    // Событие, вызываемое при повышении уровня
    public event Action<int> OnLevelUp;

    // Событие для обновления UI опыта: (текущий опыт, опыт до следующего уровня)
    public event Action<float, float> OnExperienceChanged;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        // Инициализируем подписчиков начальными значениями
        float required = GetRequiredExperienceForNextLevel();
        OnExperienceChanged?.Invoke(currentExperience, required);
    }

    /// <summary>
    /// Сколько опыта нужно для перехода на следующий уровень.
    /// </summary>
    private float GetRequiredExperienceForNextLevel()
    {
        // Например: baseExp * factor^(level-1)
        float required = baseExperienceToNextLevel;

        // Для 1 уровня (currentLevel = 1) степень будет 0 → множитель = 1
        int power = Mathf.Max(0, currentLevel - 1);
        required *= Mathf.Pow(experienceGrowthFactor, power);

        return required;
    }

    /// <summary>
    /// Добавление опыта. Можно вызывать из других систем
    /// (убийство врага, выполнение квеста и т.д.).
    /// </summary>
    public void AddExperience(float amount)
    {
        if (amount <= 0f)
            return;

        currentExperience += amount;

        // Проверяем, хватает ли опыта для повышения уровня (возможно, несколько раз подряд)
        bool leveledUpAtLeastOnce = false;

        while (true)
        {
            float required = GetRequiredExperienceForNextLevel();

            if (currentExperience < required)
                break;

            currentExperience -= required;
            LevelUpInternal();
            leveledUpAtLeastOnce = true;
        }

        float nextRequired = GetRequiredExperienceForNextLevel();
        OnExperienceChanged?.Invoke(currentExperience, nextRequired);

        if (leveledUpAtLeastOnce)
        {
            Debug.Log($"Новый уровень: {currentLevel}, опыт: {currentExperience}/{nextRequired}");
        }
    }

    /// <summary>
    /// Внутренняя логика повышения уровня.
    /// </summary>
    private void LevelUpInternal()
    {
        currentLevel++;

        // Уведомляем подписчиков
        OnLevelUp?.Invoke(currentLevel);

        // Пример: усиливаем характеристики игрока при каждом уровне
        if (playerStats != null)
        {
            // Все изменения здоровья/маны и вызовы событий
            // выполняем через отдельный метод в PlayerStats.
            // Так события OnHealthChanged/OnManaChanged остаются
            // инкапсулированы внутри PlayerStats.
            playerStats.ApplyLevelUpBonuses(10f, 5f);
        }
    }
}
```

Комментарий:

- Поля `currentLevel` и `currentExperience` сделаны **приватными** (`private`) с атрибутом `[SerializeField]`, чтобы они были видны в инспекторе Unity, но не доступны напрямую из других скриптов.
- Для чтения значений добавлены публичные свойства `CurrentLevel` и `CurrentExperience` (только для чтения).
- Это обеспечивает **инкапсуляцию** — все изменения уровня и опыта проходят через метод `AddExperience()`, что гарантирует:
  - правильный расчёт повышения уровня;
  - вызов событий (`OnLevelUp`, `OnExperienceChanged`);
  - единую точку контроля логики прогрессии.
- Опыт и уровень живут **внутри** `PlayerProgression`, чтобы не раздувать `PlayerStats`.
- Метод `AddExperience` можно вызывать из:
  - врагов (при смерти);
  - квестов;
  - предметов (свитки опыта и т.п.).
- В примере при повышении уровня:
  - немного увеличивается `maxHealth` и `maxMana` в `PlayerData`;
  - здоровье игрока восстанавливается до нового максимума;
  - вызывается метод `PlayerStats.ApplyLevelUpBonuses`, который изнутри `PlayerStats`
    поднимает события `OnHealthChanged`/`OnManaChanged`. Так мы не пытаемся вызывать
    события `OnHealthChanged`/`OnManaChanged` напрямую из `PlayerProgression`
    (в C# событие можно вызывать только внутри того класса, где оно объявлено).

> В реальном проекте можно вместо изменения `PlayerData` хранить “бонусы за уровни” отдельно, чтобы не менять ScriptableObject на лету. Для учебного проекта такой простой вариант подойдёт, чтобы показать идею.

### 4.3. Изменение PlayerStats: метод ApplyLevelUpBonuses

Чтобы корректно вызывать события `OnHealthChanged` и `OnManaChanged` (и не получать ошибку компиляции про события,
которые можно вызывать только внутри класса-источника), в `PlayerStats` добавляется отдельный метод:

```csharp
public void ApplyLevelUpBonuses(float healthBonus, float manaBonus)
{
    if (playerData == null)
    {
        Debug.LogWarning("PlayerStats.ApplyLevelUpBonuses: PlayerData не назначен.", this);
        return;
    }

    // Увеличиваем максимальные значения
    playerData.maxHealth += healthBonus;
    playerData.maxMana += manaBonus;

    // Синхронизируем текущие значения с новыми максимумами
    currentHealth = playerData.maxHealth;
    currentMana = Mathf.Clamp(currentMana, 0f, playerData.maxMana);

    // События вызываем здесь, внутри PlayerStats
    OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
    OnManaChanged?.Invoke(currentMana, playerData.maxMana);
}
```

`PlayerProgression` при этом ничего не знает о том, *как именно* меняются статы и поднимаются события —
он просто вызывает `playerStats.ApplyLevelUpBonuses(10f, 5f);` при повышении уровня.

---

## 5. Добавление PlayerProgression на Player

1. Открой сцену `GameScene` или префаб `Player`.
2. Выбери объект `Player` в `Hierarchy` или в окне префаба.
3. Нажми `Add Component` → добавь `PlayerProgression`.
4. В Inspector:
   - Поле `Player Stats` можно оставить пустым — компонент попытается найти `PlayerStats` на том же объекте.
   - Настрой параметры:
     - `Current Level` = `1`.
     - `Current Experience` = `0`.
     - `Base Experience To Next Level` = `100`.
     - `Experience Growth Factor` = `1.5` (по умолчанию).

Сохрани префаб `Player`.

---

## 6. Пример: начисление опыта и отображение в UI

### 6.1. Временный тест начисления опыта

Создай временный скрипт `PlayerExperienceTest.cs` (например, в `Assets/Scripts/Player/`) для проверки:

```csharp
using UnityEngine;

/// <summary>
/// Временный скрипт для проверки работы PlayerProgression.
/// </summary>
public class PlayerExperienceTest : MonoBehaviour
{
    public PlayerProgression progression;

    private void Awake()
    {
        if (progression == null)
            progression = FindFirstObjectByType<PlayerProgression>();
    }

    private void Update()
    {
        if (progression == null)
            return;

        // Добавить 50 опыта по нажатию клавиши X
        if (Input.GetKeyDown(KeyCode.X))
        {
            progression.AddExperience(50f);
        }
    }
}
```

Повесь этот скрипт на любой объект в сцене (например, на `Player` или отдельный `GameObject Debug`).

Шаги проверки:

1. Запусти сцену.
2. Нажимай `X` и следи за логами в консоли:
   - при накоплении достаточного опыта появляется сообщение о новом уровне.

### 6.2. Пример упрощённого UI для опыта (по желанию)

Если хочешь, можешь добавить простой вывод опыта через `OnGUI` (аналогично `PlayerDebugHUD`):

```csharp
using UnityEngine;

public class PlayerExperienceHUD : MonoBehaviour
{
    public PlayerProgression progression;

    private float current;
    private float required;

    private void Awake()
    {
        if (progression == null)
            progression = FindFirstObjectByType<PlayerProgression>();
    }

    private void OnEnable()
    {
        if (progression == null) return;

        progression.OnExperienceChanged += HandleExperienceChanged;
        progression.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (progression == null) return;

        progression.OnExperienceChanged -= HandleExperienceChanged;
        progression.OnLevelUp -= HandleLevelUp;
    }

    private void HandleExperienceChanged(float currentExp, float requiredExp)
    {
        current = currentExp;
        required = requiredExp;
    }

    private void HandleLevelUp(int newLevel)
    {
        Debug.Log($"LEVEL UP! Новый уровень: {newLevel}");
    }

    private void OnGUI()
    {
        if (progression == null)
            return;

        GUI.Label(new Rect(10, 30, 300, 20),
            $"Уровень: {progression.CurrentLevel}, XP: {current}/{required}");
    }
}
```

Этот UI можно заменить полноценным Canvas‑интерфейсом позже.

---

## 7. Как прогрессия отличается в разных играх

Обсуди (или запиши для себя), как `PlayerProgression` может меняться:

- **Action‑RPG (Diablo‑подобная)**:
  - много уровней;
  - сложные формулы опыта;
  - при уровне выдаются очки характеристик и навыков;
  - возможно — несколько веток развития.
- **Простой платформер**:
  - может вообще не иметь уровней;
  - прогресс выражается в собранных монетах/жизнях;
  - компонент `PlayerProgression` может быть очень простым или отсутствовать.
- **Рогалик**:
  - прогресс внутри забега может сбрасываться при смерти;
  - есть мета‑прогрессия между забегами (отдельная система).

Везде идея одна: логика прогресса вынесена в отдельный компонент, который можно включать/отключать и настраивать под жанр.

---

## 8. Проверка понимания

Ответь устно (или попроси ученика):

1. Чем `PlayerProgression` отличается от `PlayerStats`?
2. Почему поля `currentLevel` и `currentExperience` сделаны приватными?
3. Как другие скрипты могут прочитать текущий уровень игрока?
4. Какие плюсы даёт то, что опыт и уровни вынесены в отдельный компонент?
5. Почему важно изменять опыт только через метод `AddExperience()`, а не напрямую?
6. Как бы ты изменила формулу опыта для:
   - казуальной игры;
   - хардкорной RPG?
7. Что происходит при повышении уровня в текущем коде и как можно это расширить (например, добавлением очков навыков)?

Если код собирается, опыт начисляется и уровни растут — система игрока (на уровне этого этапа) готова, можно переходить к оружию (Этап 6).

