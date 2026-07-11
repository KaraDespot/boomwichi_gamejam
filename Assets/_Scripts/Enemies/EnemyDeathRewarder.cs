using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Начисляет опыт игроку при смерти врагов.
/// </summary>
public class EnemyDeathRewarder : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент прогрессии игрока, куда добавляется опыт.")]
    [SerializeField] private PlayerProgression playerProgression;

    public PlayerProgression PlayerProgression => playerProgression;

    private readonly HashSet<EnemyStats> registeredEnemies = new HashSet<EnemyStats>();

    private void Awake()
    {
        ResolvePlayerProgressionIfNeeded();
    }

    private void Start()
    {
        RegisterExistingEnemiesInScene();
    }

    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null || registeredEnemies.Contains(stats))
            return;

        ResolvePlayerProgressionIfNeeded();
        stats.OnDied += HandleEnemyDied;
        registeredEnemies.Add(stats);
    }

    private void HandleEnemyDied(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Критично отписаться сразу, чтобы избежать дублирующихся наград и утечек подписок.
        stats.OnDied -= HandleEnemyDied;
        registeredEnemies.Remove(stats);

        ResolvePlayerProgressionIfNeeded();
        if (playerProgression == null)
        {
            Debug.LogWarning("EnemyDeathRewarder: PlayerProgression не назначен.", this);
            return;
        }

        float reward = stats.ExperienceReward;
        if (reward > 0f)
            playerProgression.AddExperience(reward);
    }

    private void OnDisable()
    {
        foreach (EnemyStats stats in registeredEnemies)
        {
            if (stats != null)
                stats.OnDied -= HandleEnemyDied;
        }

        registeredEnemies.Clear();
    }

    private void ResolvePlayerProgressionIfNeeded()
    {
        if (playerProgression != null)
            return;

        // Нужен fallback для сцен, где ссылка не проставлена вручную в инспекторе.
        playerProgression = FindFirstObjectByType<PlayerProgression>();
    }

    private void RegisterExistingEnemiesInScene()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null)
                continue;

            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats == null)
                stats = enemy.gameObject.AddComponent<EnemyStats>();

            RegisterEnemy(stats);
        }
    }
}
