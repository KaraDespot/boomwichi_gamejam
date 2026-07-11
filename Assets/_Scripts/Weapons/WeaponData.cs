using UnityEngine;

/// <summary>
/// Данные для оружия (урон, скорость, дальность и т.п.).
/// Используется разными системами: игрок, инвентарь, лут.
/// </summary>
[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Game Data/Weapon Data",
    order = 0)]
public class WeaponData : ScriptableObject
{
    public enum WeaponType
    {
        Melee,   // Ближний бой (мечи, топоры)
        Ranged,  // Дальний бой (луки, арбалеты)
        Magic    // Магическое оружие (посохи и т.п.)
    }

    [Header("Общее")]
    [Tooltip("Читаемое название оружия (для UI).")]
    public string weaponName = "New Weapon";

    [Tooltip("Тип оружия (ближнее, дальнее, магическое).")]
    public WeaponType weaponType = WeaponType.Melee;

    [Tooltip("Иконка оружия для инвентаря/интерфейса.")]
    public Sprite icon;

    [Header("Бой")]
    [Min(0f)]
    [Tooltip("Базовый урон за одну атаку.")]
    public float damage = 10f;

    [Min(0.1f)]
    [Tooltip("Скорость атаки (атак в секунду). 1 = 1 удар в секунду.")]
    public float attackSpeed = 1f;

    [Min(0f)]
    [Tooltip("Дальность действия оружия (радиус удара или дальность выстрела).")]
    public float range = 2f;

    [Min(0f)]
    [Tooltip("Сила отталкивания цели при попадании (опционально).")]
    public float knockbackForce = 0f;

    [Header("Анимация и эффекты (по желанию)")]
    [Tooltip("Имя анимации атаки в Animator (опционально).")]
    public string attackAnimationName;

    [Tooltip("Звук атаки (удар/выстрел).")]
    public AudioClip attackSound;

    [Header("Снаряды (для дальнего боя)")]
    [Tooltip("Префаб снаряда (для лука/магии). Может быть пустым для ближнего боя.")]
    public GameObject projectilePrefab;

    [Tooltip("Скорость снаряда (единиц в секунду).")]
    [Min(0f)]
    public float projectileSpeed = 20f;
}
