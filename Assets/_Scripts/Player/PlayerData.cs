using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "Game Data/Player Data", order = 0)]
/// <summary>
/// ScriptableObject с базовыми параметрами игрока.
/// Хранит стартовые значения здоровья, маны и настроек движения,
/// которые затем читают PlayerStats и PlayerController.
/// </summary>
public class PlayerData : ScriptableObject
{
    [Header("Основные характеристики")]
    [Min(1f)]
    [Tooltip("Максимальное здоровье игрока по умолчанию.")]
    public float maxHealth = 100f;

    [Min(0f)]
    [Tooltip("Максимальная мана / энергия по умолчанию. Может быть 0, если мана не используется.")]
    public float maxMana = 0f;

    [Header("Движение")]
    [Min(0f)]
    [Tooltip("Базовая скорость движения, которую использует PlayerController.")]
    public float moveSpeed = 5f;

    [Min(0f)]
    [Tooltip("Базовая сила прыжка, влияет на начальную вертикальную скорость.")]
    public float jumpForce = 5f;

    [Header("Дополнительные параметры движения")]
    [Min(0f)]
    [Tooltip("Ускорение при начале движения (может использоваться в более сложных контроллерах).")]
    public float acceleration = 10f;

    [Min(0f)]
    [Tooltip("Скорость поворота персонажа (градусы в секунду).")]
    public float rotationSpeed = 720f;
}