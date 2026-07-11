/*
 * RangedEnemy
 * Назначение: специализация EnemyBase для врага дальнего боя.
 * Что делает: переопределяет атаку и выпускает Projectile в сторону цели.
 * Связи: использует EnemyData/EnemyBase для урона и дистанции, Projectile для доставки урона.
 * Паттерны: Inheritance (расширение базового поведения), Fail Fast (валидация конфигурации перед выстрелом).
 */

using UnityEngine;

/// <summary>
/// Враг дальнего боя, который атакует снарядом.
/// Контракт: создаёт снаряд только при валидной конфигурации и наличии цели.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Дальняя атака")]
    [Tooltip("Префаб снаряда. На объекте должен быть компонент Projectile.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Точка выстрела. Если не задана, используется позиция врага.")]
    [SerializeField] private Transform shootOrigin;

    [Tooltip("Слои, по которым снаряд может наносить урон (обычно Player).")]
    [SerializeField] private LayerMask projectileHitLayers;

    [Min(0.1f)]
    [Tooltip("Скорость полёта снаряда.")]
    [SerializeField] private float projectileSpeed = 12f;

    [Min(0f)]
    [Tooltip("Максимальная дистанция полёта снаряда. Если <= 0, используется AttackRange из EnemyData.")]
    [SerializeField] private float projectileDistance = 12f;

    [Tooltip("Смещение точки прицеливания по высоте относительно цели.")]
    [SerializeField] private float aimOffsetY = 1f;

    /// <summary>
    /// Дальняя атака: выпускает Projectile в сторону текущей цели.
    /// </summary>
    public override void Attack()
    {
        if (CurrentTarget == null)
            return;

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: RangedEnemy — не назначен projectilePrefab.", this);
            return;
        }

        if (projectileHitLayers.value == 0)
        {
            Debug.LogWarning($"{name}: RangedEnemy — projectileHitLayers пустой, снаряд никого не заденет.", this);
        }

        Vector3 spawnPosition = shootOrigin != null ? shootOrigin.position : transform.position;

        Vector3 aimPoint = CurrentTarget.position + Vector3.up * aimOffsetY;
        Vector3 direction = aimPoint - spawnPosition;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        Quaternion spawnRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError($"{name}: на projectilePrefab отсутствует компонент Projectile.", projectileObject);
            Destroy(projectileObject);
            return;
        }

        float distance = projectileDistance > 0f ? projectileDistance : AttackRange;
        if (distance <= 0f)
        {
            Debug.LogWarning($"{name}: RangedEnemy — дистанция снаряда <= 0, использовано значение 1.", this);
            distance = 1f;
        }

        projectile.Setup(Damage, distance, projectileSpeed, projectileHitLayers);
    }
}
