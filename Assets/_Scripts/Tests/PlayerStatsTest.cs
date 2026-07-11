using UnityEngine;

/// <summary>
/// Временный тестовый скрипт для проверки работы PlayerStats.
/// Подписывается на события и позволяет наносить урон/лечение по горячим клавишам.
/// В реальной игре может быть удалён.
/// </summary>
public class PlayerStatsTest : MonoBehaviour
{
    [Tooltip("Ссылка на компонент PlayerStats. Если не назначена, будет найдена автоматически на этом объекте.")]
    public PlayerStats playerStats;

    /// <summary>
    /// Пытается найти PlayerStats на том же объекте, если ссылка не назначена в инспекторе.
    /// </summary>
    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    /// <summary>
    /// Подписывается на события PlayerStats при включении компонента.
    /// </summary>
    private void OnEnable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged += HandleHealthChanged;
        playerStats.OnDeath += HandleDeath;
    }

    /// <summary>
    /// Отписывается от событий PlayerStats при выключении компонента.
    /// </summary>
    private void OnDisable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged -= HandleHealthChanged;
        playerStats.OnDeath -= HandleDeath;
    }

    /// <summary>
    /// Обрабатывает тестовый ввод:
    /// H — нанести урон, J — вылечить игрока.
    /// </summary>
    private void Update()
    {
        if (playerStats == null) return;

        // Нанести урон по нажатию клавиши H.
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerStats.TakeDamage(10f);
        }

        // Вылечить по нажатию клавиши J.
        if (Input.GetKeyDown(KeyCode.J))
        {
            playerStats.Heal(10f);
        }
    }

    /// <summary>
    /// Обработчик события изменения здоровья — выводит значения в консоль.
    /// </summary>
    private void HandleHealthChanged(float current, float max)
    {
        Debug.Log($"Здоровье изменилось: {current} / {max}");
    }

    /// <summary>
    /// Обработчик события смерти игрока — выводит сообщение в консоль.
    /// </summary>
    private void HandleDeath()
    {
        Debug.Log("Игрок умер!");
    }
}