/*
 * EnemyBase
 * Назначение: базовое поведение врага в runtime (цель, движение, атака, получение урона).
 * Что делает: хранит runtime-состояние врага, ведёт chase/attack/dead логику и перемещается через NavMeshAgent.
 * Связи: читает баланс из EnemyData, используется EnemySpawner/EncounterTrigger и адаптером EnemyStats.
 * Паттерны: Single Responsibility (поведение врага), Data + Runtime State, Enum-based State Machine.
 */

using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Базовый класс поведения врага для simple-ветки.
/// </summary>
public class EnemyBase : MonoBehaviour, IDamageable
{
    private enum EnemyState
    {
        Chase,
        Attack,
        Dead
    }

    [Header("Данные врага")]
    [Tooltip("ScriptableObject с базовыми параметрами врага.")]
    [SerializeField] private EnemyData enemyData;

    [Header("Состояние")]
    [Tooltip("Текущее здоровье врага в рантайме.")]
    [SerializeField] private float currentHealth;

    [Header("Цель")]
    [Tooltip("Текущая цель врага (обычно игрок).")]
    [SerializeField] private Transform target;

    [Tooltip("Пробовать ли автоматически найти цель на старте, если она не задана.")]
    [SerializeField] private bool autoResolveTargetOnStart = true;

    [Header("Тайминги")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Смерть")]
    [Tooltip("Нужно ли уничтожать объект при смерти. Для pooled-врагов обычно выключается.")]
    [SerializeField] private bool destroyOnDeath = true;

    [Tooltip("Задержка перед уничтожением после смерти. Нужна для эффектов/анимаций.")]
    [Min(0f)]
    [SerializeField] private float destroyDelayAfterDeath = 0.15f;

    [Header("Навигация")]
    [Tooltip("Использовать ли NavMeshAgent для движения к цели.")]
    [SerializeField] private bool useNavMeshAgent = true;

    [Tooltip("Отступ, который вычитается из AttackRange для stoppingDistance NavMeshAgent.")]
    [Min(0f)]
    [SerializeField] private float attackStoppingOffset = 0.1f;

    [Tooltip("Скорость поворота в fallback-режиме ручного движения.")]
    [Min(0f)]
    [SerializeField] private float manualRotationSpeed = 5f;

    private float nextAttackTime;
    private bool isDead;
    private EnemyState currentState = EnemyState.Chase;
    private NavMeshAgent navMeshAgent;
    private bool hasLoggedNavMeshFallback;
    private bool isNavMeshAgentAutoAdded;

    public EnemyData Data => enemyData;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => enemyData != null ? enemyData.maxHealth : 0f;
    public float MoveSpeed => enemyData != null ? enemyData.moveSpeed : 0f;
    public float Damage => enemyData != null ? enemyData.damage : 0f;
    public float AttackRange => enemyData != null ? enemyData.attackRange : 0f;
    public float DetectionRange => enemyData != null ? enemyData.detectionRange : 0f;
    public bool IsDead => isDead;
    protected Transform CurrentTarget => target;

    /// <summary>
    /// Событие смерти врага.
    /// Нужен как канал декуплинга для адаптеров и систем наград.
    /// </summary>
    public event Action OnDied;

    private void Awake()
    {
        // Важно для учеников:
        // Awake вызывается при создании объекта (в том числе сразу после Instantiate).
        // Здесь мы можем подготовить runtime-состояние (например, здоровье),
        // если EnemyData уже назначен в инспекторе на префабе.
        if (enemyData != null)
            currentHealth = enemyData.maxHealth;

        TryInitializeNavMeshAgent();
        ApplyNavigationSettings();
    }

    private void Start()
    {
        // Start вызывается после Awake (обычно на первом кадре).
        // Здесь удобно делать “поиск внешнего мира”: найти игрока и выставить цель.
        if (target == null && autoResolveTargetOnStart)
            ResolveTargetOnce();

        ApplyNavigationSettings();
    }

    private void Update()
    {
        // Update — “мозг” врага. Здесь мы НЕ должны делать тяжёлые операции,
        // поэтому логика максимально простая: проверили дистанцию → решили действие.
        if (enemyData == null || isDead || target == null)
            return;

        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable == null)
            targetDamageable = target.GetComponentInParent<IDamageable>();

        // Минимальный guard для завершённого боевого цикла:
        // если цель уже мертва, враг прекращает преследование и атаку.
        if (targetDamageable != null && targetDamageable.IsDead)
        {
            StopMovement();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > DetectionRange)
        {
            StopMovement();
            return;
        }

        if (distanceToTarget <= AttackRange)
            currentState = EnemyState.Attack;
        else
            currentState = EnemyState.Chase;

        switch (currentState)
        {
            case EnemyState.Attack:
                StopMovement();
                FaceTarget();
                TryAttack();
                break;

            case EnemyState.Chase:
                MoveTowardsTarget();
                break;
        }
    }

    /// <summary>
    /// Инициализирует врага данными и сбрасывает runtime-состояние.
    /// Обычно вызывается спавнером сразу после создания врага.
    /// </summary>
    public void Setup(EnemyData data)
    {
        enemyData = data;
        currentHealth = enemyData != null ? enemyData.maxHealth : 0f;
        isDead = false;
        nextAttackTime = 0f;
        currentState = EnemyState.Chase;
        hasLoggedNavMeshFallback = false;

        TryInitializeNavMeshAgent();
        ApplyNavigationSettings();
    }

    /// <summary>
    /// Получение урона врагом.
    /// virtual — чтобы в будущем можно было переопределить правила урона у наследников
    /// (например, броня/щит/иммунитеты), не меняя код спавнера и оружия.
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (enemyData == null || damage <= 0f || isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        Debug.Log($"{name}: получил {damage} урона. Здоровье: {currentHealth}/{MaxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Смерть врага (базовая реализация).
    /// Здесь мы:
    ///  - защищаемся от двойной смерти (isDead);
    ///  - отправляем событие OnDied как “сигнал” для других систем;
    ///  - удаляем объект со сцены.
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentState = EnemyState.Dead;
        StopMovement();
        Debug.Log($"{name}: умер.");
        OnDied?.Invoke();

        // Для объектов из пула уничтожение не выполняем:
        // смерть обрабатывается подписчиками события OnDied (Release в пул).
        // Проверяем "пуловость" по имени компонента, чтобы не иметь жёсткой зависимости
        // от класса PooledEnemy (его может не быть в simple-ветке проекта).
        bool isPooledEnemy = GetComponent("PooledEnemy") != null;
        if (!destroyOnDeath || isPooledEnemy)
            return;

        if (destroyDelayAfterDeath > 0f)
            Destroy(gameObject, destroyDelayAfterDeath);
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Внешняя установка цели (предпочтительный путь для спавнера/encounter-системы).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ResolveTargetOnce()
    {
        // Учебное упрощение:
        // ищем первый PlayerController на сцене и используем его transform как цель.
        // Это проще, чем слои/рейкасты/“зрение” с препятствиями.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            target = player.transform;
    }

    /// <summary>
    /// Движение к цели.
    /// Основной путь — через NavMeshAgent; ручное движение используется как fallback.
    /// </summary>
    public void MoveTowardsTarget()
    {
        if (target == null)
            return;

        if (TryMoveWithNavMesh())
            return;

        MoveTowardsTargetManually();
    }

    private void MoveTowardsTargetManually()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * manualRotationSpeed);
        }
    }

    /// <summary>
    /// Атака врага (базовая реализация).
    /// В этом файле атака пока “символическая” (лог в консоль) — так проще отделить поведение
    /// (подошёл и атакует) от боевой модели (кто кому наносит урон), которая разбирается отдельно.
    /// </summary>
    public virtual void Attack()
    {
        if (target == null)
            return;

        // Важно для урока 7.2 (урон через IDamageable):
        // враг не делает “поиск целей по слоям” и не бьёт всех вокруг.
        // Он наносит урон строго своей цели target (обычно игрок), которую нужно корректно назначить
        // через авто-резолв на старте или методом SetTarget() при спавне/инициализации.
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(Damage);

        Debug.Log($"{name}: атакует {target.name} с уроном {Damage}");
    }

    private void TryAttack()
    {
        // Кулдаун — защита от атаки “каждый кадр”.
        // Без него враг атаковал бы 60+ раз в секунду, что ломает геймплей.
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        Attack();
    }

    private void TryInitializeNavMeshAgent()
    {
        if (!useNavMeshAgent)
            return;

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        // Для существующих префабов в simple-ветке добавляем NavMeshAgent автоматически,
        // чтобы не ломать урок и не требовать массового ручного перевешивания компонентов.
        if (navMeshAgent == null)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
            isNavMeshAgentAutoAdded = true;
            ApplyAutoAddedAgentDefaults();
        }
    }

    private void ApplyNavigationSettings()
    {
        if (!useNavMeshAgent || navMeshAgent == null || enemyData == null)
            return;

        navMeshAgent.speed = Mathf.Max(0.01f, MoveSpeed);
        navMeshAgent.stoppingDistance = Mathf.Max(0.1f, AttackRange - attackStoppingOffset);
        navMeshAgent.acceleration = Mathf.Max(8f, navMeshAgent.acceleration);
        navMeshAgent.angularSpeed = Mathf.Max(360f, navMeshAgent.angularSpeed);
        navMeshAgent.autoBraking = true;
    }

    private bool TryMoveWithNavMesh()
    {
        if (!useNavMeshAgent || navMeshAgent == null || target == null)
            return false;

        // Если агент не на baked NavMesh (или поверхность не построена), не падаем:
        // мягко переключаемся в fallback-движение.
        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            if (!hasLoggedNavMeshFallback)
            {
                Debug.LogWarning(
                    $"{name}: NavMeshAgent недоступен/вне NavMesh. " +
                    "Используется fallback-движение. Проверьте Bake NavMesh, активность NavMeshSurface и позицию врага на NavMesh.",
                    this);
                hasLoggedNavMeshFallback = true;
            }

            return false;
        }

        hasLoggedNavMeshFallback = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(target.position);
        return true;
    }

    private void StopMovement()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
            return;

        if (navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            if (navMeshAgent.hasPath)
                navMeshAgent.ResetPath();
        }

        navMeshAgent.velocity = Vector3.zero;
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * manualRotationSpeed);
    }

    private void ApplyAutoAddedAgentDefaults()
    {
        if (navMeshAgent == null || !isNavMeshAgentAutoAdded)
            return;

        // Учебные безопасные дефолты, чтобы автодобавленный агент не вел себя хаотично.
        navMeshAgent.radius = 0.35f;
        navMeshAgent.height = 1.8f;
        navMeshAgent.baseOffset = 0f;
        navMeshAgent.acceleration = 16f;
        navMeshAgent.angularSpeed = 540f;
        navMeshAgent.autoBraking = true;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            navMeshAgent.radius = Mathf.Max(0.2f, capsule.radius * 0.6f);
            navMeshAgent.height = Mathf.Max(1f, capsule.height);
            navMeshAgent.baseOffset = capsule.center.y;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
