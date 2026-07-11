# Урок 6.3: MeleeWeapon и RangedWeapon — конкретные виды оружия

---

## 0. Теория: специализация наследников

Сейчас у нас уже есть:

- `WeaponData` — ScriptableObject с данными оружия;
- `WeaponBase` — абстрактный базовый класс с общей логикой (данные + перезарядка).

Следующий шаг — создать **конкретные реализации** оружия:

- `MeleeWeapon` — оружие ближнего боя (мечи, топоры и т.п.);
- `RangedWeapon` — оружие дальнего боя (луки, арбалеты, магические посохи со снарядами).

Это пример **наследования и полиморфизма**:

- оба класса **наследуются** от `WeaponBase`;
- оба реализуют метод `Attack()`, но делают это **по-разному**;
- код, который вызывает `Attack()` (например, `WeaponManager`), не знает о внутренних отличиях.

---

## 1. Цели урока

- **Техническая цель**:
  - создать классы `MeleeWeapon` и `RangedWeapon`, наследующиеся от `WeaponBase`;
  - реализовать в них метод `Attack()` с использованием общих методов `CanAttack()` и `StartAttackCooldown()`;
  - подготовить простые варианты ближней и дальней атаки (пока можно ограничиться логами и базовой физикой).
- **Обучающая цель**:
  - показать, как наследники переопределяют абстрактный метод из базового класса;
  - закрепить понимание того, как разделяются общая и специфическая логика.

---

## 2. Подготовка

Перед началом убедись, что:

1. Выполнены уроки 6.1 и 6.2:
   - есть `WeaponData` и несколько ассетов оружия;
   - есть абстрактный класс `WeaponBase`.
2. В сцене есть хотя бы тестовый `GameObject` (например, `Player` или пустой объект), на который можно будет повесить оружие для проверки.

---

## 3. MeleeWeapon — оружие ближнего боя

### 3.1. Идея реализации

Самая простая модель ближней атаки:

1. Берём точку удара (`attackOrigin`) — обычно это:
   - точка возле меча;
   - или центр коллайдера, где происходит удар.
2. Определяем радиус удара (`hitRadius`), либо берём `Range` из `WeaponData`.
3. Через `Physics.OverlapSphere` находим все коллайдеры в этой сфере.
4. Логируем попадания, а в будущем — наносим урон врагам (`EnemyStats` / `IDamageable`).

### 3.2. Создание скрипта MeleeWeapon

1. В окне `Project` перейди в `Assets/Scripts/Weapons/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`MeleeWeapon`**.
4. Открой его в редакторе.

Замените содержимое на следующий код.  
Здесь для полей используется `[SerializeField] private`, чтобы показать инкапсуляцию, но при этом сохранить удобную настройку в инспекторе:

```csharp
using UnityEngine;

/// <summary>
/// Оружие ближнего боя.
/// Реализует атаку через сферу вокруг точки удара.
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Параметры ближней атаки")]
    [Tooltip("Точка, откуда считается удар (обычно у меча/руки).")]
    [SerializeField]
    private Transform attackOrigin;

    [Tooltip("Радиус удара. Если 0, можно использовать Range из WeaponData.")]
    [SerializeField]
    private float hitRadius = 1.5f;

    [Tooltip("Слои, по которым можно наносить урон (враги, разрушаемые объекты).")]
    [SerializeField]
    private LayerMask hitLayers;

    public override void Attack()
    {
        if (!CanAttack())
            return;

        StartAttackCooldown();

        if (WeaponData == null)
        {
            Debug.LogWarning($"{name}: WeaponData не назначен, ближняя атака невозможна.", this);
            return;
        }

        // Если не указан радиус, используем Range из WeaponData
        float radius = hitRadius > 0f ? hitRadius : Range;

        // Если attackOrigin не задан, используем позицию Owner или самого оружия
        Vector3 origin = attackOrigin != null
            ? attackOrigin.position
            : (Owner != null ? Owner.position : transform.position);

        // Простой поиск попаданий
        Collider[] hits = Physics.OverlapSphere(origin, radius, hitLayers);

        if (hits.Length == 0)
        {
            Debug.Log($"{name}: ближняя атака — никого не задели.");
        }
        else
        {
            Debug.Log($"{name}: ближняя атака, задели {hits.Length} объект(ов).");

            foreach (Collider collider in hits)
            {
                // Здесь позже, на Этапе 8, мы будем вызывать метод получения урона
                // у врагов (например, через EnemyStats или интерфейс IDamageable).
                Debug.Log($"Попали по объекту: {collider.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем сферу удара в редакторе, чтобы видеть радиус
        Gizmos.color = Color.red;

        float radius = hitRadius > 0f ? hitRadius : (WeaponData != null ? WeaponData.range : 1.5f);
        Vector3 origin = attackOrigin != null
            ? attackOrigin.position
            : (Owner != null ? Owner.position : transform.position);

        Gizmos.DrawWireSphere(origin, radius);
    }
}
```

### 3.3. Настройка префаба ближнего оружия (меч)

1. В сцене создай пустой объект, назови его, например, `Sword_Melee`.
2. Добавь на него компонент `MeleeWeapon`.
3. В Inspector:
   - Поле `Weapon Data` — укажи ассет `Weapon_Sword_Default`.
   - Создай пустой дочерний объект `AttackOrigin` у места, где будет меч:
     - перетащи его в поле `Attack Origin`.
   - `Hit Radius` — поставь, например, `1.5`.
   - `Hit Layers` — выбери слои, по которым планируешь попадать (например, слой `Enemy`).
4. Сохрани `Sword_Melee` как префаб в `Assets/Prefabs/Weapons/` (если папки нет — создай).

Позже этот префаб будет использоваться `WeaponManager` в качестве стартового оружия игрока.

---

## 4. Projectile — простой снаряд для дальнего боя

Перед созданием `RangedWeapon` нужно определить поведение самого снаряда.

### 4.1. Идея реализации снаряда

Простой снаряд:

1. Запоминает стартовую позицию.
2. Каждый кадр движется вперёд (`transform.forward * speed * Time.deltaTime`).
3. Если пройдено больше `maxDistance` — уничтожается.
4. При входе в триггер (`OnTriggerEnter`) проверяет слой объекта:
   - если слой подходит (`hitLayers`) — логирует попадание и уничтожает себя;
   - позже здесь будет вызываться система урона врагов.

### 4.2. Создание скрипта Projectile

1. В `Assets/Scripts/Weapons/`:
   - ПКМ → `Create` → `C# Script`;
   - назови скрипт **`Projectile`**.
2. Открой и замени содержимое.  
Мы используем приватные поля с `[SerializeField]` и свойства, чтобы не нарушать инкапсуляцию:

```csharp
using UnityEngine;

/// <summary>
/// Простой снаряд: летит вперёд и уничтожается при столкновении или по достижении дальности.
/// Пока просто логирует попадания.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Скорость полёта снаряда (единиц в секунду).")]
    [SerializeField]
    private float speed = 20f;

    [Tooltip("Максимальная дистанция, после которой снаряд уничтожается.")]
    [SerializeField]
    private float maxDistance = 20f;

    [Tooltip("Урон, который этот снаряд должен нанести при попадании.")]
    [SerializeField]
    private float damage = 10f;

    [Tooltip("Слои, по которым может быть нанесён урон.")]
    [SerializeField]
    private LayerMask hitLayers;

    /// <summary>Скорость полёта снаряда.</summary>
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    /// <summary>Максимальная дистанция полёта.</summary>
    public float MaxDistance
    {
        get => maxDistance;
        set => maxDistance = value;
    }

    /// <summary>Урон снаряда.</summary>
    public float Damage
    {
        get => damage;
        set => damage = value;
    }

    /// <summary>Маска слоёв, по которым снаряд может наносить урон.</summary>
    public LayerMask HitLayers
    {
        get => hitLayers;
        set => hitLayers = value;
    }

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Движемся вперёд по локальному forward
        transform.position += transform.forward * (speed * Time.deltaTime);

        // Проверяем пройденную дистанцию
        float traveled = Vector3.Distance(_startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверка: слой объекта есть в маске hitLayers?
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        Debug.Log($"Снаряд попал в {other.name}, потенциальный урон: {damage}");

        // Здесь позже можно вызывать систему урона врагов.
        // Пример на будущее (НЕ реализуем сейчас):
        // var damageable = other.GetComponent<IDamageable>();
        // if (damageable != null)
        // {
        //     damageable.TakeDamage(damage);
        // }

        Destroy(gameObject);
    }
}
```

### 4.3. Создание префаба Projectile

1. Создай пустой `GameObject` в сцене, назови его `Arrow_Projectile` (или любое другое имя).
2. Добавь компонент:
   - `Projectile`;
   - `SphereCollider` (или другой `Collider`) с галочкой **Is Trigger**;
   - (опционально) `Rigidbody` с `Is Kinematic = true`.
3. В Inspector:
   - `Speed` — например, `20`;
   - `Max Distance` — например, `20`;
   - `Damage` — `10` (будет переопределяться из оружия);
   - `Hit Layers` — слой врагов.
4. Сохрани объект как префаб `Projectile_Arrow` в `Assets/Prefabs/Weapons/`.
5. Удали его из сцены (он будет создаваться скриптом `RangedWeapon`).

---

## 5. RangedWeapon — оружие дальнего боя

### 5.1. Идея реализации

Для дальнего боя:

1. Нужна точка выстрела (`shootOrigin`) — конец ствола/лука.
2. При атаке создаём экземпляр префаба снаряда (`WeaponData.projectilePrefab`).
3. Задаём ему:
   - направление (`shootOrigin.forward`);
   - урон (`Damage`);
   - максимальную дистанцию (`Range`);
   - слои, по которым он может попадать.
4. Запускаем перезарядку через `StartAttackCooldown()`.

### 5.2. Создание скрипта RangedWeapon

1. В `Assets/Scripts/Weapons/`:
   - ПКМ → `Create` → `C# Script`;
   - назови скрипт **`RangedWeapon`**.
2. Открой и замени содержимое.  
Как и в остальных скриптах, используем приватные поля + `[SerializeField]`, а снаряд настраиваем через его свойства, не лезя в поля напрямую:

```csharp
using UnityEngine;

/// <summary>
/// Оружие дальнего боя.
/// Создаёт снаряд, который летит вперёд.
/// </summary>
public class RangedWeapon : WeaponBase
{
    [Header("Параметры дальнего боя")]
    [Tooltip("Точка, из которой вылетают снаряды (конец ствола/лука).")]
    [SerializeField]
    private Transform shootOrigin;

    [Tooltip("Скорость снаряда. Если 0, используется значение по умолчанию в префабе.")]
    [SerializeField]
    private float projectileSpeedOverride = 0f;

    [Tooltip("Слои, по которым может быть нанесён урон.")]
    [SerializeField]
    private LayerMask projectileHitLayers;

    public override void Attack()
    {
        if (!CanAttack())
            return;

        StartAttackCooldown();

        if (WeaponData == null)
        {
            Debug.LogWarning($"{name}: WeaponData не назначен, дальняя атака невозможна.", this);
            return;
        }

        if (WeaponData.projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: projectilePrefab в WeaponData не назначен, нечего стрелять.", this);
            return;
        }

        // Определяем точку выстрела
        Vector3 spawnPosition = shootOrigin != null
            ? shootOrigin.position
            : (Owner != null ? Owner.position : transform.position);

        Quaternion spawnRotation = shootOrigin != null
            ? shootOrigin.rotation
            : (Owner != null ? Owner.rotation : transform.rotation);

        // Создаём снаряд
        GameObject projectileObject = Instantiate(
            WeaponData.projectilePrefab,
            spawnPosition,
            spawnRotation
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            // Настраиваем снаряд через свойства, не трогая его приватные поля.
            projectile.Damage = Damage;
            projectile.MaxDistance = Range;
            projectile.HitLayers = projectileHitLayers;

            if (projectileSpeedOverride > 0f)
            {
                projectile.Speed = projectileSpeedOverride;
            }
        }

        Debug.Log($"{name}: дальняя атака, выпущен снаряд с уроном {Damage} и дальностью {Range}.");
    }
}
```

### 5.3. Настройка префаба дальнего оружия (лук)

1. В сцене создай пустой объект `Bow_Ranged`.
2. Добавь на него компонент `RangedWeapon`.
3. Создай дочерний объект `ShootOrigin` на кончике лука и перетащи его в поле `Shoot Origin`.
4. В Inspector:
   - `Weapon Data` — укажи ассет `Weapon_Bow_Default`.
   - `Projectile Hit Layers` — слой врагов.
   - `Projectile Speed Override` можно оставить `0`, если скорость задана в `Projectile`.
5. Сохрани `Bow_Ranged` как префаб в `Assets/Prefabs/Weapons/`.

---

## 6. Полиморфизм в действии

Сейчас у нас:

- `WeaponBase` — абстрактный базовый класс;
- `MeleeWeapon` и `RangedWeapon` — конкретные наследники;
- оба реализуют один и тот же метод `Attack()`, но поведение внутри разное.

Позже, в уроке 6.4:

- `WeaponManager` будет хранить ссылку `WeaponBase currentWeapon`;
- при нажатии кнопки атаки от `InputManager` он будет вызывать:
  - `currentWeapon.Attack();`
- при этом:
  - если `currentWeapon` — `MeleeWeapon`, выполнится ближняя атака;
  - если `currentWeapon` — `RangedWeapon`, выполнится дальняя атака;
  - сам код `WeaponManager` при этом не меняется.

Это и есть **полиморфизм**: разные объекты ведут себя по‑разному при вызове одного и того же метода базового типа.

---

## 7. Мини‑проверка

Ответь на вопросы:

1. Какие поля и методы унаследованы обоими классами (`MeleeWeapon`, `RangedWeapon`) от `WeaponBase`?
2. В чём главное отличие реализации `Attack()` в `MeleeWeapon` и `RangedWeapon`?
3. Почему выгодно, что логика перезарядки (`CanAttack`, `StartAttackCooldown`) вынесена в базовый класс?

Проверь в проекте:

- Скрипты `MeleeWeapon`, `RangedWeapon`, `Projectile` находятся в `Assets/Scripts/Weapons/`.
- Префабы `Sword_Melee`, `Bow_Ranged` и `Projectile_Arrow` созданы и лежат в `Assets/Prefabs/Weapons/`.
- В Inspector у `MeleeWeapon` и `RangedWeapon` корректно назначены `WeaponData`, точки `attackOrigin`/`shootOrigin` и маски слоёв.

Если всё готово и понятно — переходи к уроку 6.4 (`WeaponManager` — управление оружием игрока и атакой).

