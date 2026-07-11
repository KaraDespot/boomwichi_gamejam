using System.Collections.Generic;
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

        // Важно для урока 7.2 (урон через IDamageable):
        // правило “игрок бьёт только врагов” обеспечивается настройкой hitLayers в инспекторе (обычно только слой Enemy).
        // Если не указан радиус, используем Range из WeaponData
        float radius = hitRadius > 0f ? hitRadius : Range;

        // Если attackOrigin не задан, используем позицию owner или самого оружия
        Vector3 origin = attackOrigin != null
            ? attackOrigin.position
            : (Owner != null ? Owner.position : transform.position);

        // Простой поиск попаданий.
        // OverlapSphere может вернуть несколько коллайдеров одного и того же врага,
        // поэтому HashSet защищает от нанесения урона несколько раз за одну атаку.
        Collider[] hits = Physics.OverlapSphere(origin, radius, hitLayers);

        if (hits.Length == 0)
        {
            Debug.Log($"{name}: ближняя атака — никого не задели.");
        }
        else
        {
            Debug.Log($"{name}: ближняя атака, задели {hits.Length} объект(ов).");
            HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

            foreach (Collider collider in hits)
            {
                Debug.Log($"Попали по объекту: {collider.name}");

                IDamageable damageable = collider.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = collider.GetComponentInParent<IDamageable>();

                if (damageable != null && damagedTargets.Add(damageable))
                    damageable.TakeDamage(Damage);
            }
        }

        // Здесь же в будущем можно запускать анимацию атаки и звук удара.
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
