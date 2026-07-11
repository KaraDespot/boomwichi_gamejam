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
            projectile.Setup(Damage, Range, WeaponData.projectileSpeed, projectileHitLayers);
        }

        Debug.Log($"{name}: дальняя атака, выпущен снаряд с уроном {Damage} и дальностью {Range}.");
    }
}