/*
 * EnemyStats
 * Назначение: совместимый адаптер над EnemyBase для систем, ожидающих EnemyStats.
 * Что делает: проксирует характеристики/урон/смерть и ретранслирует событие OnDied.
 * Связи: используется EnemyDeathRewarder и другими подсистемами, где ожидается EnemyStats.
 * Паттерны: Adapter, Facade.
 */

using System;
using UnityEngine;

/// <summary>
/// Адаптер к EnemyBase для совместимости с системами наград/подписок.
/// Источник истины по здоровью/смерти находится в EnemyBase.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент EnemyBase, который хранит runtime-состояние врага.")]
    [SerializeField] private EnemyBase enemyBase;

    public EnemyData EnemyData => enemyBase != null ? enemyBase.Data : null;
    public float MaxHealth => enemyBase != null ? enemyBase.MaxHealth : 0f;
    public float MoveSpeed => enemyBase != null ? enemyBase.MoveSpeed : 0f;
    public float Damage => enemyBase != null ? enemyBase.Damage : 0f;
    public float AttackRange => enemyBase != null ? enemyBase.AttackRange : 0f;
    public float DetectionRange => enemyBase != null ? enemyBase.DetectionRange : 0f;
    public float ExperienceReward => EnemyData != null ? EnemyData.experienceReward : 0f;

    /// <summary>
    /// Событие смерти в формате EnemyStats для обратной совместимости.
    /// </summary>
    public event Action<EnemyStats> OnDied;

    private void Awake()
    {
        // EnemyStats — компонент “обвязки”.
        // Чтобы префабы были устойчивыми, стараемся автоматически найти EnemyBase на том же объекте.
        if (enemyBase == null)
            enemyBase = GetComponent<EnemyBase>();

        if (enemyBase == null)
            Debug.LogError("EnemyStats: отсутствует EnemyBase на объекте.", this);
    }

    private void OnEnable()
    {
        // Важно: подписки на события делаем в OnEnable,
        // чтобы при Disable/Enable компонента не оставались “висячие” подписки.
        if (enemyBase != null)
            enemyBase.OnDied += HandleEnemyBaseDied;
    }

    private void OnDisable()
    {
        // И симметрично отписываемся в OnDisable.
        if (enemyBase != null)
            enemyBase.OnDied -= HandleEnemyBaseDied;
    }

    /// <summary>
    /// Совместимый метод инициализации из EnemyData.
    /// </summary>
    public void Setup(EnemyData data)
    {
        // В большинстве случаев EnemyData назначается не в инспекторе, а “снаружи”:
        // спавнер создаёт префаб и передаёт конфиг врага методом Setup(data).
        if (enemyBase == null)
        {
            Debug.LogWarning("EnemyStats.Setup: EnemyBase не назначен.", this);
            return;
        }

        enemyBase.Setup(data);
    }

    /// <summary>
    /// Совместимый метод получения урона.
    /// </summary>
    public void TakeDamage(float damage)
    {
        // EnemyStats не хранит здоровье сам — он только прокидывает вызов в EnemyBase.
        if (enemyBase == null)
        {
            Debug.LogWarning("EnemyStats.TakeDamage: EnemyBase не назначен.", this);
            return;
        }

        enemyBase.TakeDamage(damage);
    }

    /// <summary>
    /// Совместимый метод смерти.
    /// </summary>
    public virtual void Die()
    {
        // Аналогично: смерть хранится в EnemyBase, а EnemyStats оставлен для совместимости API.
        if (enemyBase == null)
        {
            Debug.LogWarning("EnemyStats.Die: EnemyBase не назначен.", this);
            return;
        }

        enemyBase.Die();
    }

    private void HandleEnemyBaseDied()
    {
        // Переводим событие “умер EnemyBase” в событие “умер EnemyStats”.
        // Так системам наград/спавна/пула удобнее работать с одним типом события.
        OnDied?.Invoke(this);
    }
}