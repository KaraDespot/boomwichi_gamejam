/*
 * IDamageable
 * Назначение: минимальный контракт для объектов, которые могут получать урон.
 * Что делает: задаёт единый API урона и статуса смерти для оружия, врагов и игрока.
 * Связи: используется боевой системой (MeleeWeapon, Projectile, EnemyBase и PlayerStats).
 * Паттерны: Interface Segregation (ISP), Polymorphism.
 */

/// <summary>
/// Минимальный контракт для получения урона.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Признак, что объект уже мёртв и не должен получать повторную боевую обработку.
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Применяет входящий урон к объекту.
    /// </summary>
    void TakeDamage(float damage);
}
