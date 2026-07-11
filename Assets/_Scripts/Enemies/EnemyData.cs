/*
 * EnemyData
 * Назначение: ScriptableObject-конфиг врага (баланс и ссылки на префаб).
 * Что делает: хранит “числа по умолчанию” для типа врага и префаб, из которого создаются экземпляры на сцене.
 * Связи:
 *  - EnemyBase читает из EnemyData скорости/дальности/урон и т.п.
 *  - EnemySpawner (и другие спавнеры) используют EnemyData.prefab, чтобы создавать врага через Instantiate.
 *  - EnemyStats может отдавать experienceReward наружу (например, в системе наград).
 * Паттерны: Data-driven (данные отдельно от логики), ScriptableObject как конфиг.
 */

using UnityEngine;

/// <summary>
/// Данные врага (конфиг).
/// Важно: EnemyData — это НЕ враг на сцене, а “паспорт” с числами и ссылками.
/// Экземпляр врага в рантайме появляется только после Instantiate префаба.
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Game Data/Enemy Data",
    order = 0)]
public class EnemyData : ScriptableObject
{
    public enum EnemyType
    {
        Melee,   // Ближний бой (гоблины, орки)
        Ranged,  // Дальний бой (лучники, маги)
        Boss     // Боссы (особые враги)
    }

    [Header("Общее")]
    [Tooltip("Читаемое название врага (для UI и логирования).")]
    public string enemyName = "New Enemy";

    [Tooltip("Тип врага (ближний, дальний, босс).")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Характеристики")]
    [Min(1f)]
    [Tooltip("Максимальное здоровье врага.")]
    public float maxHealth = 50f;

    [Min(0f)]
    [Tooltip("Скорость движения врага (единиц в секунду).")]
    public float moveSpeed = 3f;

    [Min(0f)]
    [Tooltip("Урон, который враг наносит за одну атаку.")]
    public float damage = 10f;

    [Header("Бой")]
    [Min(0f)]
    [Tooltip("Дальность атаки врага (радиус ближнего боя или дальность выстрела).")]
    public float attackRange = 2f;

    [Min(0f)]
    [Tooltip("Дальность обнаружения игрока (на каком расстоянии враг начинает преследовать).")]
    public float detectionRange = 10f;

    [Header("Награды")]
    [Min(0f)]
    [Tooltip("Опыт, который получает игрок за убийство этого врага.")]
    public float experienceReward = 10f;

    [Header("Префаб")]
    [Tooltip("Префаб врага, который будет использоваться для создания экземпляров.")]
    public GameObject prefab;
}