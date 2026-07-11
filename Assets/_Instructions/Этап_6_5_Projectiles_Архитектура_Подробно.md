# Урок 6.5: Projectiles — архитектура и расширение системы снарядов

---

## 0. Теория: инкапсуляция и разделение ответственности для снарядов

На Этапе 6.3 мы создали простой класс `Projectile`, который работает, но имеет ограничения:

- **Все параметры хранятся прямо в компоненте** — нельзя переиспользовать одни и те же данные для разных префабов.
- **Нет разделения на данные и логику** — как с `WeaponData` и `WeaponBase`.
- **Один класс для всех типов снарядов** — нельзя легко добавить взрывные, самонаводящиеся или снаряды с эффектами.

Теперь мы применим те же принципы, что и для оружия:

- **ProjectileData** (ScriptableObject) — данные снаряда (скорость, урон, эффекты).
- **ProjectileBase** (абстрактный класс) — базовая логика движения и попадания.
- **Конкретные типы снарядов** (SimpleProjectile, ExplosiveProjectile, HomingProjectile) — специализированное поведение.
- **Система эффектов** (IProjectileEffect) — нанесение эффектов при попадании (горение, замедление, яд).

**Ключевые идеи:**

- **Разделение данных и логики** — как с `WeaponData`/`WeaponBase`.
- **Наследование и полиморфизм** — разные типы снарядов через базовый класс.
- **Композиция для эффектов** — снаряд может иметь несколько эффектов через интерфейс.

---

## 1. Цели урока

- **Техническая цель**: создать архитектуру снарядов с разделением на данные и логику, поддержкой разных типов и системы эффектов.
- **Обучающая цель**: закрепить принципы инкапсуляции, наследования и композиции на примере снарядов.

После выполнения урока у тебя будет:

- `ProjectileData` — ScriptableObject с данными снаряда.
- `ProjectileBase` — абстрактный базовый класс.
- `SimpleProjectile` — простой снаряд (как текущий `Projectile`).
- `ExplosiveProjectile` — взрывной снаряд.
- `IProjectileEffect` — интерфейс для эффектов.
- Примеры эффектов: `BurnEffect`, `SlowEffect`, `PoisonEffect`.

---

## 2. Архитектура системы

### 2.1. Структура классов

```
ProjectileData (ScriptableObject)
    ↓ используется
ProjectileBase (abstract MonoBehaviour)
    ├─ SimpleProjectile
    ├─ ExplosiveProjectile
    └─ HomingProjectile

IProjectileEffect (interface)
    ├─ BurnEffect
    ├─ SlowEffect
    └─ PoisonEffect
```

### 2.2. Поток данных

1. **RangedWeapon** создаёт снаряд из префаба.
2. **Снаряд** получает `ProjectileData` и настраивается.
3. **Снаряд** летит и при попадании вызывает эффекты из `ProjectileData`.
4. **Эффекты** применяются к цели через интерфейс `IDamageable` (будет на Этапе 8).

---

## 3. ProjectileData — ScriptableObject с данными снаряда

### 3.1. Создание скрипта ProjectileData

1. В папке `Assets/_Scripts/Weapons/` создай скрипт `ProjectileData.cs`.
2. Замени содержимое на:

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Данные для снаряда (скорость, урон, эффекты и т.п.).
/// Используется разными типами снарядов.
/// </summary>
[CreateAssetMenu(
    fileName = "ProjectileData",
    menuName = "Game Data/Projectile Data",
    order = 1)]
public class ProjectileData : ScriptableObject
{
    [Header("Общее")]
    [Tooltip("Читаемое название снаряда (для отладки/UI).")]
    public string projectileName = "New Projectile";

    [Header("Движение")]
    [Min(0.1f)]
    [Tooltip("Скорость полёта снаряда (единиц в секунду).")]
    public float speed = 20f;

    [Min(0f)]
    [Tooltip("Максимальная дистанция, после которой снаряд уничтожается.")]
    public float maxDistance = 20f;

    [Header("Урон")]
    [Min(0f)]
    [Tooltip("Базовый урон снаряда при попадании.")]
    public float damage = 10f;

    [Tooltip("Слои, по которым может быть нанесён урон.")]
    public LayerMask hitLayers;

    [Header("Эффекты")]
    [Tooltip("Список эффектов, которые применяются при попадании.")]
    public List<ProjectileEffectData> effects = new List<ProjectileEffectData>();

    [Header("Визуальные эффекты (опционально)")]
    [Tooltip("Префаб эффекта взрыва/попадания.")]
    public GameObject hitEffectPrefab;

    [Tooltip("Звук попадания.")]
    public AudioClip hitSound;
}
```

### 3.2. ProjectileEffectData — данные эффекта

Создай скрипт `ProjectileEffectData.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Данные эффекта снаряда (горение, замедление и т.п.).
/// Используется в ProjectileData.
/// </summary>
[System.Serializable]
public class ProjectileEffectData
{
    public enum EffectType
    {
        None,
        Burn,      // Горение (урон со временем)
        Slow,      // Замедление
        Poison,    // Яд (урон со временем)
        Stun,      // Оглушение
        Knockback  // Отталкивание
    }

    [Tooltip("Тип эффекта.")]
    public EffectType effectType = EffectType.None;

    [Tooltip("Длительность эффекта в секундах.")]
    [Min(0f)]
    public float duration = 3f;

    [Tooltip("Интенсивность эффекта (урон в секунду для Burn/Poison, процент замедления для Slow и т.п.).")]
    [Min(0f)]
    public float intensity = 5f;

    [Tooltip("Сила отталкивания (только для Knockback).")]
    [Min(0f)]
    public float knockbackForce = 5f;
}
```

**Разбор:**

- `ProjectileData` хранит **все данные** снаряда, включая список эффектов.
- `ProjectileEffectData` — структура данных для одного эффекта (тип, длительность, интенсивность).
- Эффекты хранятся как `List<ProjectileEffectData>` — снаряд может иметь несколько эффектов.

---

## 4. ProjectileBase — абстрактный базовый класс

### 4.1. Создание скрипта ProjectileBase

1. В папке `Assets/_Scripts/Weapons/` создай скрипт `ProjectileBase.cs`.
2. Замени содержимое на:

```csharp
using UnityEngine;

/// <summary>
/// Базовый класс для всех снарядов.
/// Хранит ссылку на ProjectileData и управляет движением.
/// Наследники реализуют конкретное поведение при попадании.
/// </summary>
public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Данные снаряда")]
    [Tooltip("ScriptableObject с параметрами снаряда.")]
    public ProjectileData projectileData;

    [Header("Переопределения (опционально)")]
    [Tooltip("Урон снаряда. Если 0, используется значение из ProjectileData.")]
    public float damageOverride = 0f;

    [Tooltip("Максимальная дистанция. Если 0, используется значение из ProjectileData.")]
    public float maxDistanceOverride = 0f;

    protected Vector3 _startPosition;
    protected float _currentDamage;
    protected float _currentMaxDistance;

    protected virtual void Start()
    {
        _startPosition = transform.position;

        // Используем переопределения или значения из ProjectileData
        if (projectileData != null)
        {
            _currentDamage = damageOverride > 0f ? damageOverride : projectileData.damage;
            _currentMaxDistance = maxDistanceOverride > 0f ? maxDistanceOverride : projectileData.maxDistance;
        }
        else
        {
            Debug.LogWarning($"{name}: ProjectileData не назначен!", this);
            _currentDamage = damageOverride;
            _currentMaxDistance = maxDistanceOverride;
        }
    }

    protected virtual void Update()
    {
        if (projectileData == null)
            return;

        // Движемся вперёд
        Move();

        // Проверяем дистанцию
        float traveled = Vector3.Distance(_startPosition, transform.position);
        if (traveled >= _currentMaxDistance)
        {
            OnMaxDistanceReached();
        }
    }

    /// <summary>
    /// Движение снаряда (можно переопределить в наследниках).
    /// </summary>
    protected virtual void Move()
    {
        transform.position += transform.forward * (projectileData.speed * Time.deltaTime);
    }

    /// <summary>
    /// Вызывается при достижении максимальной дистанции.
    /// </summary>
    protected virtual void OnMaxDistanceReached()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Вызывается при попадании в цель.
    /// Наследники должны реализовать конкретное поведение (простое попадание, взрыв и т.п.).
    /// </summary>
    protected abstract void OnHit(Collider hitCollider);

    /// <summary>
    /// Применяет эффекты из ProjectileData к цели.
    /// </summary>
    protected virtual void ApplyEffects(Collider target)
    {
        if (projectileData == null || projectileData.effects == null || projectileData.effects.Count == 0)
            return;

        // Здесь позже, на Этапе 8, мы будем применять эффекты через IDamageable или систему эффектов.
        // Пока просто логируем.
        foreach (var effectData in projectileData.effects)
        {
            if (effectData.effectType != ProjectileEffectData.EffectType.None)
            {
                Debug.Log($"{name}: применяет эффект {effectData.effectType} к {target.name} " +
                         $"(длительность: {effectData.duration}с, интенсивность: {effectData.intensity})");
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (projectileData == null)
            return;

        // Проверяем слои
        if ((projectileData.hitLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Вызываем обработку попадания (реализуется в наследниках)
        OnHit(other);
    }
}
```

**Разбор:**

- `ProjectileBase` — абстрактный класс с общей логикой движения и проверки дистанции.
- `Move()` — виртуальный метод, можно переопределить (например, для самонаводящихся снарядов).
- `OnHit()` — абстрактный метод, каждый тип снаряда реализует по-своему.
- `ApplyEffects()` — применяет эффекты из `ProjectileData` (пока логирует, на Этапе 8 будет реальная логика).

---

## 5. SimpleProjectile — простой снаряд

### 5.1. Создание скрипта SimpleProjectile

1. В папке `Assets/_Scripts/Weapons/` создай скрипт `SimpleProjectile.cs`.
2. Замени содержимое на:

```csharp
using UnityEngine;

/// <summary>
/// Простой снаряд: летит вперёд и наносит урон одной цели при попадании.
/// </summary>
public class SimpleProjectile : ProjectileBase
{
    protected override void OnHit(Collider hitCollider)
    {
        Debug.Log($"{name}: попал в {hitCollider.name}, урон: {_currentDamage}");

        // Применяем эффекты
        ApplyEffects(hitCollider);

        // Здесь позже, на Этапе 8, мы будем наносить урон через IDamageable:
        // var damageable = hitCollider.GetComponent<IDamageable>();
        // if (damageable != null)
        // {
        //     damageable.TakeDamage(_currentDamage);
        // }

        // Визуальные эффекты
        if (projectileData != null && projectileData.hitEffectPrefab != null)
        {
            Instantiate(projectileData.hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Уничтожаем снаряд
        Destroy(gameObject);
    }
}
```

**Разбор:**

- `SimpleProjectile` наследуется от `ProjectileBase` и реализует `OnHit()`.
- При попадании применяет эффекты, наносит урон (пока логирует) и уничтожается.
- Это аналог текущего `Projectile`, но с архитектурой через `ProjectileData`.

---

## 6. ExplosiveProjectile — взрывной снаряд

### 6.1. Создание скрипта ExplosiveProjectile

1. В папке `Assets/_Scripts/Weapons/` создай скрипт `ExplosiveProjectile.cs`.
2. Замени содержимое на:

```csharp
using UnityEngine;

/// <summary>
/// Взрывной снаряд: при попадании наносит урон всем целям в радиусе взрыва.
/// </summary>
public class ExplosiveProjectile : ProjectileBase
{
    [Header("Параметры взрыва")]
    [Tooltip("Радиус взрыва.")]
    [Min(0f)]
    public float explosionRadius = 3f;

    protected override void OnHit(Collider hitCollider)
    {
        Vector3 explosionPosition = transform.position;

        Debug.Log($"{name}: взрыв в {explosionPosition}, радиус: {explosionRadius}");

        // Находим все цели в радиусе взрыва
        Collider[] targets = Physics.OverlapSphere(explosionPosition, explosionRadius, projectileData.hitLayers);

        foreach (Collider target in targets)
        {
            // Расстояние влияет на урон (ближе = больше урона)
            float distance = Vector3.Distance(explosionPosition, target.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius); // От 1.0 до 0.0
            damageMultiplier = Mathf.Clamp01(damageMultiplier);

            float finalDamage = _currentDamage * damageMultiplier;

            Debug.Log($"{name}: взрыв нанёс {finalDamage} урона {target.name} (множитель: {damageMultiplier:F2})");

            // Применяем эффекты к каждой цели
            ApplyEffects(target);

            // Здесь позже, на Этапе 8:
            // var damageable = target.GetComponent<IDamageable>();
            // if (damageable != null)
            // {
            //     damageable.TakeDamage(finalDamage);
            // }
        }

        // Визуальные эффекты
        if (projectileData != null && projectileData.hitEffectPrefab != null)
        {
            Instantiate(projectileData.hitEffectPrefab, explosionPosition, Quaternion.identity);
        }

        // Уничтожаем снаряд
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем радиус взрыва в редакторе
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
```

**Разбор:**

- `ExplosiveProjectile` использует `Physics.OverlapSphere` для поиска всех целей в радиусе.
- Урон уменьшается с расстоянием (ближе = больше урона).
- Применяет эффекты ко всем задетым целям.

---

## 7. Система эффектов (для будущего расширения)

### 7.1. Интерфейс IProjectileEffect

Создай скрипт `IProjectileEffect.cs`:

```csharp
/// <summary>
/// Интерфейс для эффектов снаряда.
/// Реализуется конкретными эффектами (BurnEffect, SlowEffect и т.п.).
/// </summary>
public interface IProjectileEffect
{
    /// <summary>
    /// Применяет эффект к цели.
    /// </summary>
    /// <param name="target">Цель (обычно компонент с IDamageable или системой эффектов).</param>
    /// <param name="effectData">Данные эффекта (длительность, интенсивность и т.п.).</param>
    void ApplyEffect(UnityEngine.Component target, ProjectileEffectData effectData);
}
```

### 7.2. Пример: BurnEffect (горение)

Создай скрипт `BurnEffect.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Эффект горения: наносит урон со временем.
/// </summary>
public class BurnEffect : MonoBehaviour, IProjectileEffect
{
    public void ApplyEffect(Component target, ProjectileEffectData effectData)
    {
        // Здесь позже, на Этапе 8, мы будем применять эффект через систему эффектов цели.
        // Пока просто логируем.
        Debug.Log($"{target.name}: получает эффект горения " +
                 $"(урон: {effectData.intensity}/сек, длительность: {effectData.duration}с)");

        // Псевдокод на будущее:
        // var statusEffectSystem = target.GetComponent<IStatusEffectSystem>();
        // if (statusEffectSystem != null)
        // {
        //     statusEffectSystem.ApplyBurn(effectData.duration, effectData.intensity);
        // }
    }
}
```

**Примечание:** Реальная реализация эффектов будет на Этапе 8, когда появится система урона и эффектов для врагов/игрока.

---

## 8. Использование новой архитектуры

### 8.1. Создание ProjectileData

1. В папке `Assets/_ScriptableObjects/Weapons/` создай ассет `ProjectileData_Arrow`.
2. Настрой:
   - `Speed` → `20`
   - `Max Distance` → `20`
   - `Damage` → `15`
   - `Hit Layers` → слой `Enemy`
   - `Effects` → можно добавить эффект горения (Burn, duration: 3s, intensity: 2)

### 8.2. Обновление префаба снаряда

1. Открой префаб `Projectile` (или создай новый `SimpleProjectile_Arrow`).
2. Замени компонент `Projectile` на `SimpleProjectile`.
3. В Inspector:
   - `Projectile Data` → назначь `ProjectileData_Arrow`
   - `Damage Override` → `0` (используется из данных)
   - `Max Distance Override` → `0`

### 8.3. Обновление RangedWeapon

В `RangedWeapon.cs` измени настройку снаряда:

```csharp
// Старый код:
Projectile projectile = projectileObject.GetComponent<Projectile>();
if (projectile != null)
{
    projectile.damage = Damage;
    projectile.maxDistance = Range;
    projectile.hitLayers = projectileHitLayers;
    // ...
}

// Новый код:
ProjectileBase projectile = projectileObject.GetComponent<ProjectileBase>();
if (projectile != null)
{
    // Если нужно переопределить урон/дальность из оружия:
    projectile.damageOverride = Damage;
    projectile.maxDistanceOverride = Range;
    
    // Если нужно переопределить слои (опционально):
    if (projectile.projectileData != null)
    {
        projectile.projectileData.hitLayers = projectileHitLayers;
    }
}
```

---

## 9. Анализ текущей реализации и возможные улучшения

### 9.1. Что сделано правильно

✅ **Разделение данных и логики** — `ProjectileData` отдельно от компонента.  
✅ **Наследование** — разные типы снарядов через `ProjectileBase`.  
✅ **Композиция для эффектов** — список эффектов в `ProjectileData`.  
✅ **Гибкость** — можно переопределить урон/дальность через `damageOverride`.

### 9.2. Возможные улучшения

#### 9.2.1. Переопределение слоёв в RangedWeapon

**Текущая проблема:** В `RangedWeapon` мы изменяем `projectileData.hitLayers` напрямую, что меняет ассет для всех снарядов этого типа.

**Решение:** Хранить `hitLayers` в `ProjectileBase` как отдельное поле с fallback на `projectileData.hitLayers`:

```csharp
// В ProjectileBase:
[Tooltip("Слои попадания. Если не задано, используется из ProjectileData.")]
public LayerMask hitLayersOverride;

protected LayerMask GetHitLayers()
{
    return hitLayersOverride.value != 0 ? hitLayersOverride : 
           (projectileData != null ? projectileData.hitLayers : 0);
}
```

#### 9.2.2. Система эффектов через ScriptableObject

**Текущая реализация:** Эффекты хранятся как `List<ProjectileEffectData>` (структура данных).

**Альтернатива:** Создать `ProjectileEffectData` как ScriptableObject для переиспользования:

```csharp
[CreateAssetMenu(menuName = "Game Data/Projectile Effect")]
public class ProjectileEffectData : ScriptableObject
{
    public EffectType effectType;
    public float duration;
    public float intensity;
    // ...
}
```

**Плюсы:** Можно переиспользовать одни и те же эффекты в разных снарядах.  
**Минусы:** Больше файлов, сложнее для простых случаев.

**Рекомендация:** Текущая реализация (структура) подходит для начала. Если эффектов станет много и их нужно переиспользовать — перейти на ScriptableObject.

#### 9.2.3. Object Pool для снарядов

**Текущая проблема:** Каждый снаряд создаётся через `Instantiate` и уничтожается через `Destroy` — это дорого при частых выстрелах.

**Решение:** Использовать Object Pool (будет на Этапе 10). Снаряды будут браться из пула и возвращаться туда вместо уничтожения.

---

## 10. Чеклист для самостоятельного добавления новых типов снарядов

### 10.1. Создание нового типа снаряда

- [ ] Создан класс, наследующийся от `ProjectileBase`.
- [ ] Реализован метод `OnHit()` с конкретной логикой.
- [ ] (Опционально) Переопределён `Move()` для особого движения (самонаведение, дуга и т.п.).
- [ ] Добавлены специфичные поля (радиус взрыва, скорость поворота и т.п.).

### 10.2. Создание нового эффекта

- [ ] Создан класс, реализующий `IProjectileEffect`.
- [ ] Реализован метод `ApplyEffect()`.
- [ ] Добавлен тип эффекта в `ProjectileEffectData.EffectType`.
- [ ] (На Этапе 8) Интегрирован с системой эффектов цели.

### 10.3. Тестирование

- [ ] Создан `ProjectileData` для нового типа снаряда.
- [ ] Создан префаб с компонентом нового типа.
- [ ] Настроено оружие для использования нового префаба.
- [ ] Проверено поведение в игре.

---

## 11. Примеры использования

### 11.1. Огненная стрела (SimpleProjectile + Burn)

1. Создай `ProjectileData_FireArrow`:
   - `Speed`: 20
   - `Damage`: 15
   - `Effects`: добавь `Burn` (duration: 5s, intensity: 3)

2. Создай префаб `SimpleProjectile_FireArrow` с компонентом `SimpleProjectile` и назначенным `ProjectileData_FireArrow`.

3. В `WeaponData` лука назначь этот префаб.

### 11.2. Взрывная бомба (ExplosiveProjectile)

1. Создай `ProjectileData_Bomb`:
   - `Speed`: 10 (медленнее)
   - `Damage`: 30
   - `Max Distance`: 15

2. Создай префаб `ExplosiveProjectile_Bomb` с компонентом `ExplosiveProjectile`:
   - `Explosion Radius`: 5
   - `Projectile Data`: `ProjectileData_Bomb`

3. Используй в оружии (например, гранатомёт).

---

## 12. Мини‑проверка

Ответь на вопросы:

1. Почему `ProjectileData` лучше, чем хранение параметров прямо в компоненте?
2. Как добавить новый тип снаряда (например, самонаводящийся)?
3. Как снаряд может иметь несколько эффектов одновременно?
4. В чём разница между `SimpleProjectile` и `ExplosiveProjectile`?

Проверь в проекте:

- Классы `ProjectileData`, `ProjectileBase`, `SimpleProjectile` созданы.
- Создан хотя бы один ассет `ProjectileData`.
- Префаб снаряда использует `SimpleProjectile` вместо старого `Projectile`.
- `RangedWeapon` обновлён для работы с `ProjectileBase`.

---

## 🔗 Связанные документы

- **[Этап 6.3: Melee/Ranged](Этап_6_3_Melee_Ranged_Подробно.md)** — создание базового Projectile
- **[Этап 8: IDamageable и система урона](Этап_8_...)** — интеграция эффектов с системой урона (будет позже)
- **[Этап 10: Object Pool](Этап_10_...)** — оптимизация создания снарядов (будет позже)
