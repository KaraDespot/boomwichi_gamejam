/*
 * EncounterTrigger
 * Назначение: запуск и управление encounter при входе игрока в trigger.
 * Что делает: запускает волны из EncounterData, отслеживает живых врагов и завершает encounter только после их добивания.
 * Связи: использует EncounterData/WaveData и спавнер (EnemySpawner, либо SimpleEnemySpawner как учебный fallback).
 * Паттерны: Trigger-driven flow, локальная state-машина, event-based декуплинг.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Триггер encounter: стартует волны и завершает encounter по правилам из EncounterData.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterTrigger : MonoBehaviour
{
    [Header("Данные encounter")]
    [Tooltip("ScriptableObject с волнами и правилами текущего encounter.")]
    [SerializeField] private EncounterData encounterData;

    [Header("Связи")]
    [Tooltip("Основной спавнер для encounter/wave-системы (каноничный вариант урока 7.4).")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("Упрощённый fallback-спавнер. Используется только если EnemySpawner не найден/не назначен.")]
    [SerializeField] private SimpleEnemySpawner simpleEnemySpawner;

    [Tooltip("Опциональный override цели врагов. Если пусто, ищется PlayerController на сцене.")]
    [SerializeField] private Transform playerTargetOverride;

    [Tooltip("Опциональные точки спавна конкретно для этого encounter. Если пусто, используются точки спавнера.")]
    [SerializeField] private Transform[] encounterSpawnPoints;

    [Header("Триггер")]
    [Tooltip("Какой tag должен войти в trigger для старта encounter.")]
    [SerializeField] private string requiredTag = "Player";

    [Tooltip("Отключать ли trigger-коллайдер сразу после запуска encounter.")]
    [SerializeField] private bool disableTriggerAfterStart = true;

    [Header("Поведение")]
    [Tooltip("Какие объекты включить после завершения encounter (например, выход).")]
    [SerializeField] private GameObject[] activateOnCompleted;

    [Tooltip("Какие объекты выключить при старте encounter (например, временный блокер).")]
    [SerializeField] private GameObject[] deactivateOnStarted;

    [Header("Отладка")]
    [Tooltip("Показывать ли подробные логи encounter.")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isEncounterRunning;
    private bool isEncounterCompleted;
    private Coroutine encounterCoroutine;
    private Collider triggerCollider;

    private readonly List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    private readonly Dictionary<EnemyBase, Action> deathHandlers = new Dictionary<EnemyBase, Action>();

    /// <summary>
    /// Событие старта encounter.
    /// </summary>
    public event Action<EncounterTrigger> OnEncounterStarted;

    /// <summary>
    /// Событие завершения encounter.
    /// </summary>
    public event Action<EncounterTrigger> OnEncounterCompleted;

    public bool IsEncounterRunning => isEncounterRunning;
    public bool IsEncounterCompleted => isEncounterCompleted;

    private bool HasEnemySpawner => enemySpawner != null;
    private bool HasSimpleSpawner => simpleEnemySpawner != null;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
            Debug.LogWarning($"{name}: EncounterTrigger ожидает Collider c IsTrigger = true.", this);

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        // Friendly fallback для учеников: если основной спавнер не найден,
        // пробуем упрощённый SimpleEnemySpawner вместо немой поломки encounter.
        if (enemySpawner == null && simpleEnemySpawner == null)
            simpleEnemySpawner = FindFirstObjectByType<SimpleEnemySpawner>();

        if (enemySpawner == null && simpleEnemySpawner != null)
        {
            Debug.LogWarning(
                $"{name}: EnemySpawner не найден. Encounter будет использовать SimpleEnemySpawner как fallback. " +
                "Для каноничного варианта урока 7.4 рекомендуется назначить EnemySpawner.", this);
        }
    }

    private void OnDisable()
    {
        StopEncounterRoutine();
        UnregisterAllTrackedEnemies();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        if (isEncounterRunning)
            return;

        if (encounterData != null && encounterData.OneShot && isEncounterCompleted)
            return;

        if (!IsPlayerTrigger(other))
            return;

        StartEncounter();
    }

    /// <summary>
    /// Публичный запуск encounter (можно вызвать из кнопки/скрипта).
    /// </summary>
    public void StartEncounter()
    {
        if (isEncounterRunning)
            return;

        if (encounterData != null && encounterData.OneShot && isEncounterCompleted)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: encounter уже завершён и помечен one-shot.", this);
            return;
        }

        if (!ValidateSetup())
            return;

        SetObjectsActive(deactivateOnStarted, false);

        isEncounterRunning = true;

        if (disableTriggerAfterStart && triggerCollider != null)
            triggerCollider.enabled = false;

        OnEncounterStarted?.Invoke(this);

        if (showDebugLogs)
            Debug.Log($"{name}: encounter '{encounterData.EncounterId}' запущен.", this);

        encounterCoroutine = StartCoroutine(RunEncounterRoutine());
    }

    [ContextMenu("Debug/Запустить encounter")]
    private void DebugStartEncounter()
    {
        StartEncounter();
    }

    [ContextMenu("Debug/Сбросить encounter")]
    private void DebugResetEncounter()
    {
        StopEncounterRoutine();
        UnregisterAllTrackedEnemies();

        isEncounterRunning = false;
        isEncounterCompleted = false;

        if (triggerCollider != null)
            triggerCollider.enabled = true;

        if (showDebugLogs)
            Debug.Log($"{name}: encounter сброшен (debug).", this);
    }

    private IEnumerator RunEncounterRoutine()
    {
        int waveCount = encounterData.WaveCount;

        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            WaveData wave = encounterData.GetWave(waveIndex);
            if (wave == null || !wave.IsValid)
            {
                Debug.LogWarning($"{name}: wave #{waveIndex} пропущена (невалидный конфиг).", this);
                continue;
            }

            if (wave.StartDelay > 0f)
                yield return new WaitForSeconds(wave.StartDelay);

            for (int spawnIndex = 0; spawnIndex < wave.EnemyCount; spawnIndex++)
            {
                EnemyBase enemy = SpawnWaveEnemy(wave);
                if (enemy != null)
                    RegisterAliveEnemy(enemy);

                if (spawnIndex < wave.EnemyCount - 1 && wave.SpawnInterval > 0f)
                    yield return new WaitForSeconds(wave.SpawnInterval);
            }

            if (wave.WaitUntilWaveDefeated)
                yield return WaitUntilAllAliveEnemiesDefeated();

            bool hasNextWave = waveIndex < waveCount - 1;
            if (hasNextWave && encounterData.DelayBetweenWaves > 0f)
                yield return new WaitForSeconds(encounterData.DelayBetweenWaves);
        }

        // Канон из спецификации: завершение только после добивания остатка врагов.
        yield return WaitUntilAllAliveEnemiesDefeated();

        CompleteEncounter();
    }

    private EnemyBase SpawnWaveEnemy(WaveData wave)
    {
        if (wave == null || !wave.IsValid)
            return null;

        Transform spawnPoint = ResolveSpawnPoint();
        Transform target = ResolvePlayerTarget();

        if (HasEnemySpawner)
            return enemySpawner.SpawnEnemy(wave.EnemyData, spawnPoint, target);

        if (HasSimpleSpawner)
            return simpleEnemySpawner.SpawnEnemyForEncounter(wave.EnemyData, spawnPoint, target);

        return null;
    }

    private Transform ResolveSpawnPoint()
    {
        if (encounterSpawnPoints != null && encounterSpawnPoints.Length > 0)
            return PickRandomValidPoint(encounterSpawnPoints);

        List<Transform> spawnerPoints = GetSpawnerPoints();
        if (spawnerPoints == null || spawnerPoints.Count == 0)
            return null;

        return spawnerPoints[UnityEngine.Random.Range(0, spawnerPoints.Count)];
    }

    private List<Transform> GetSpawnerPoints()
    {
        List<Transform> validPoints = new List<Transform>();

        if (HasEnemySpawner)
        {
            IReadOnlyList<Transform> points = enemySpawner.SpawnPoints;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null)
                    validPoints.Add(points[i]);
            }

            return validPoints;
        }

        if (HasSimpleSpawner)
        {
            IReadOnlyList<Transform> points = simpleEnemySpawner.SpawnPoints;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null)
                    validPoints.Add(points[i]);
            }
        }

        return validPoints;
    }

    private static Transform PickRandomValidPoint(Transform[] points)
    {
        if (points == null || points.Length == 0)
            return null;

        List<Transform> validPoints = new List<Transform>();
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
                validPoints.Add(points[i]);
        }

        if (validPoints.Count == 0)
            return null;

        return validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
    }

    private Transform ResolvePlayerTarget()
    {
        if (playerTargetOverride != null)
            return playerTargetOverride;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform : null;
    }

    private void RegisterAliveEnemy(EnemyBase enemy)
    {
        if (enemy == null || aliveEnemies.Contains(enemy))
            return;

        aliveEnemies.Add(enemy);

        Action deathHandler = null;
        deathHandler = () =>
        {
            if (enemy != null)
                enemy.OnDied -= deathHandler;

            UnregisterAliveEnemy(enemy);
        };

        enemy.OnDied += deathHandler;
        deathHandlers[enemy] = deathHandler;
    }

    private void UnregisterAliveEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        aliveEnemies.Remove(enemy);

        if (deathHandlers.TryGetValue(enemy, out Action deathHandler))
        {
            enemy.OnDied -= deathHandler;
            deathHandlers.Remove(enemy);
        }
    }

    private IEnumerator WaitUntilAllAliveEnemiesDefeated()
    {
        while (true)
        {
            CleanupTrackedEnemies();
            if (aliveEnemies.Count == 0)
                yield break;

            yield return null;
        }
    }

    private void CleanupTrackedEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = aliveEnemies[i];
            bool shouldRemove = enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy;
            if (!shouldRemove)
                continue;

            if (enemy != null && deathHandlers.TryGetValue(enemy, out Action deathHandler))
            {
                enemy.OnDied -= deathHandler;
                deathHandlers.Remove(enemy);
            }

            aliveEnemies.RemoveAt(i);
        }
    }

    private void UnregisterAllTrackedEnemies()
    {
        foreach (KeyValuePair<EnemyBase, Action> pair in deathHandlers)
        {
            EnemyBase enemy = pair.Key;
            if (enemy != null)
                enemy.OnDied -= pair.Value;
        }

        deathHandlers.Clear();
        aliveEnemies.Clear();
    }

    private void CompleteEncounter()
    {
        if (!isEncounterRunning)
            return;

        isEncounterRunning = false;
        isEncounterCompleted = true;
        encounterCoroutine = null;

        SetObjectsActive(activateOnCompleted, true);

        // Локальное событие для прямых подписчиков конкретного trigger-объекта.
        OnEncounterCompleted?.Invoke(this);

        // Глобальное событие через EventBus:
        // нужно, чтобы внешние системы (HUD/аналитика/квесты/прогрессия уровня)
        // могли реагировать на завершение encounter без прямой ссылки на этот компонент.
        if (EventBus.Instance != null)
        {
            // Берём стабильный encounterId из данных.
            // Если данные вдруг отсутствуют - используем имя объекта как безопасный fallback.
            string encounterId = encounterData != null ? encounterData.EncounterId : name;
            EventBus.Instance.RaiseEncounterCompleted(encounterId);
        }

        if (showDebugLogs)
            Debug.Log($"{name}: encounter '{encounterData.EncounterId}' завершён.", this);

        if (encounterData != null && !encounterData.OneShot && triggerCollider != null)
            triggerCollider.enabled = true;
    }

    private void StopEncounterRoutine()
    {
        if (encounterCoroutine != null)
        {
            StopCoroutine(encounterCoroutine);
            encounterCoroutine = null;
        }

        isEncounterRunning = false;
    }

    private bool ValidateSetup()
    {
        if (encounterData == null)
        {
            Debug.LogError($"{name}: EncounterData не назначен.", this);
            return false;
        }

        if (!encounterData.HasAnyValidWave())
        {
            Debug.LogError($"{name}: EncounterData не содержит валидных волн.", this);
            return false;
        }

        if (!HasEnemySpawner && !HasSimpleSpawner)
        {
            Debug.LogError(
                $"{name}: не найден спавнер для encounter. " +
                "Назначьте EnemySpawner (канонично) или добавьте SimpleEnemySpawner как fallback.", this);
            return false;
        }

        bool hasOwnPoints = encounterSpawnPoints != null && encounterSpawnPoints.Length > 0;
        bool hasSpawnerPoints = GetSpawnerPoints().Count > 0;
        if (!hasOwnPoints && !hasSpawnerPoints)
        {
            Debug.LogError($"{name}: нет ни encounterSpawnPoints, ни валидных точек в спавнере.", this);
            return false;
        }

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogError($"{name}: Collider должен быть trigger для запуска encounter.", this);
            return false;
        }

        return true;
    }

    private bool IsPlayerTrigger(Collider other)
    {
        if (other == null)
            return false;

        bool matchedByTag = !string.IsNullOrWhiteSpace(requiredTag) && other.CompareTag(requiredTag);
        if (matchedByTag)
            return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }

    private static void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null || objects.Length == 0)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject targetObject = objects[i];
            if (targetObject != null)
                targetObject.SetActive(isActive);
        }
    }
}
