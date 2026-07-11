/*
 * SimpleEnemySpawner
 * Назначение: максимально упрощённый спавнер для ранних шагов обучения (Instantiate + лимит активных врагов).
 * Что делает: спавнит один выбранный тип врага в случайных точках и управляет простым циклом auto-spawn.
 * Связи: использует EnemyData/EnemyBase; может работать как fallback для EncounterTrigger в учебном режиме.
 * Паттерны: Composition, Fail Fast, Local Validation.
 *
 * Контракт для уроков:
 *  - Это облегчённый вариант, чтобы ученики быстрее освоили основы спавна.
 *  - Основной канон для encounter/wave слоя в teacher repo — EnemySpawner.
 *  - EncounterTrigger может использовать этот компонент как fallback, чтобы ученик не получал "молчаливую" поломку.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Упрощённый спавнер врагов для базовых уроков без pooling/factory.
/// </summary>
public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Тип врага")]
    [Tooltip("Данные врага, которого будем спавнить в упрощённом режиме.")]
    [SerializeField] private EnemyData enemyData;

    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    [SerializeField] private float spawnInterval = 5f;

    [Min(0)]
    [Tooltip("Максимальное количество одновременно активных врагов.")]
    [SerializeField] private int maxEnemies = 10;

    [Tooltip("Запускать ли спавн автоматически при старте.")]
    [SerializeField] private bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать подробные логи спавнера.")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Награды")]
    [Tooltip("Система выдачи опыта за убийство врагов.")]
    [SerializeField] private EnemyDeathRewarder enemyDeathRewarder;

    private bool isSpawning;
    private Coroutine spawnCoroutine;
    private Transform playerTarget;
    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

    /// <summary>
    /// Точки спавна (read-only) для внешних систем, например EncounterTrigger fallback.
    /// </summary>
    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

    private void Start()
    {
        ResolvePlayerTarget();
        ResolveRewarderIfNeeded();

        if (!ValidateSetup())
            return;

        if (spawnOnStart)
            StartSpawning();
    }

    /// <summary>
    /// Запускает периодический auto-spawn.
    /// Это учебный базовый цикл, не wave/encounter оркестратор.
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн уже запущен.", this);
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCoroutine());

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов запущен.", this);
    }

    /// <summary>
    /// Останавливает периодический auto-spawn.
    /// </summary>
    public void StopSpawning()
    {
        if (!isSpawning)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн не был запущен.", this);
            return;
        }

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов остановлен.", this);
    }

    /// <summary>
    /// Спавнит врага из локального enemyData в случайной точке.
    /// </summary>
    public EnemyBase SpawnEnemy()
    {
        if (!ValidateSetup())
            return null;

        if (playerTarget == null)
            ResolvePlayerTarget();

        return SpawnInternal(enemyData, GetRandomSpawnPoint(), playerTarget);
    }

    /// <summary>
    /// Fallback-метод для encounter-системы.
    /// Позволяет EncounterTrigger заспавнить конкретный EnemyData, если в сцене нет EnemySpawner.
    /// </summary>
    public EnemyBase SpawnEnemyForEncounter(EnemyData overrideData, Transform spawnPointOverride, Transform targetOverride)
    {
        EnemyData dataToSpawn = overrideData != null ? overrideData : enemyData;
        if (dataToSpawn == null || dataToSpawn.prefab == null)
        {
            Debug.LogError($"{name}: encounter fallback не может заспавнить врага — невалидный EnemyData.", this);
            return null;
        }

        if (showDebugLogs)
        {
            Debug.LogWarning(
                $"{name}: encounter использует SimpleEnemySpawner как fallback. " +
                "Для каноничного сценария урока 7.4 рекомендуется EnemySpawner.", this);
        }

        Transform spawnPoint = spawnPointOverride != null ? spawnPointOverride : GetRandomSpawnPoint();
        Transform target = targetOverride != null ? targetOverride : playerTarget;

        if (target == null)
        {
            ResolvePlayerTarget();
            target = playerTarget;
        }

        return SpawnInternal(dataToSpawn, spawnPoint, target);
    }

    private EnemyBase SpawnInternal(EnemyData data, Transform spawnPoint, Transform target)
    {
        if (data == null || data.prefab == null)
            return null;

        CleanupInactiveEnemies();
        if (activeEnemies.Count >= maxEnemies)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: достигнут лимит врагов. Пропускаем спавн.", this);
            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning($"{name}: не найдена валидная точка спавна.", this);
            return null;
        }

        GameObject enemyObject = Instantiate(data.prefab, spawnPoint.position, spawnPoint.rotation);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogError($"{name}: на префабе {data.prefab.name} отсутствует EnemyBase.", this);
            Destroy(enemyObject);
            return null;
        }

        enemy.Setup(data);

        EnemyStats enemyStats = enemyObject.GetComponent<EnemyStats>();
        if (enemyStats == null)
            enemyStats = enemyObject.AddComponent<EnemyStats>();

        if (enemyStats != null && enemyDeathRewarder != null)
            enemyDeathRewarder.RegisterEnemy(enemyStats);

        if (target != null)
            enemy.SetTarget(target);

        activeEnemies.Add(enemy);

        if (showDebugLogs)
            Debug.Log($"{name}: создан враг {data.enemyName} в точке {spawnPoint.name}.", this);

        return enemy;
    }

    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    private bool ValidateSetup()
    {
        if (enemyData == null || enemyData.prefab == null)
        {
            Debug.LogWarning($"{name}: не назначены EnemyData или prefab.", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна.", this);
            return false;
        }

        return true;
    }

    private void ResolvePlayerTarget()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }

    private void ResolveRewarderIfNeeded()
    {
        if (enemyDeathRewarder != null)
            return;

        enemyDeathRewarder = FindFirstObjectByType<EnemyDeathRewarder>();
        if (enemyDeathRewarder != null)
            return;

        GameObject rewarderObject = new GameObject("EnemyDeathRewarder");
        enemyDeathRewarder = rewarderObject.AddComponent<EnemyDeathRewarder>();

        if (showDebugLogs)
            Debug.LogWarning($"{name}: EnemyDeathRewarder не найден, создан автоматически.", this);
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        List<Transform> validPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                validPoints.Add(spawnPoints[i]);
        }

        if (validPoints.Count == 0)
            return null;

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    private void CleanupInactiveEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
