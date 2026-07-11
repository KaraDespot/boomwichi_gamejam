/*
 * PlayerProgression
 * Назначение: хранить и обновлять прогрессию игрока (уровень + опыт).
 * Что делает: принимает опыт, выполняет level up, уведомляет UI событиями;
 *             применяет runtime-состояние прогрессии после перехода на следующий уровень.
 * Связи: PlayerStats (бонусы при level up), GameplayHUDController (подписка на события).
 * Паттерны: локальный state-компонент игрока.
 */
using System;
using UnityEngine;

/// <summary>
/// Отвечает за прогрессию игрока: уровень, опыт и повышение уровня.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Ссылка на PlayerStats для усиления характеристик при повышении уровня.")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Уровень")]
    [SerializeField]
    [Tooltip("Текущий уровень игрока.")]
    private int currentLevel = 1;

    [Header("Опыт")]
    [SerializeField]
    [Tooltip("Текущее количество опыта.")]
    private float currentExperience;

    [Header("Кривая прогрессии")]
    [SerializeField]
    [Tooltip("Базовое количество опыта для перехода с 1 на 2 уровень.")]
    private float baseExperienceToNextLevel = 100f;

    [SerializeField]
    [Tooltip("Множитель роста требуемого опыта на каждый следующий уровень.")]
    private float experienceGrowthFactor = 1.5f;

    /// <summary>
    /// Текущий уровень игрока (только чтение).
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// Текущее количество опыта игрока (только чтение).
    /// </summary>
    public float CurrentExperience => currentExperience;

    /// <summary>
    /// Требуемый опыт до следующего уровня.
    /// </summary>
    public float RequiredExperienceForNextLevel => GetRequiredExperienceForNextLevel();

    /// <summary>
    /// Вызывается при фактическом повышении уровня.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Вызывается при любом изменении отображаемого уровня (включая runtime-применение).
    /// </summary>
    public event Action<int> OnLevelChanged;

    /// <summary>
    /// Вызывается при изменении опыта.
    /// Параметры: текущий опыт, требуемый опыт до следующего уровня.
    /// </summary>
    public event Action<float, float> OnExperienceChanged;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        OnExperienceChanged?.Invoke(currentExperience, GetRequiredExperienceForNextLevel());
    }

    /// <summary>
    /// Добавляет опыт и обрабатывает возможные повышения уровня.
    /// </summary>
    public void AddExperience(float amount)
    {
        if (amount <= 0f)
            return;

        currentExperience += amount;
        bool leveledUpAtLeastOnce = false;

        while (true)
        {
            float required = GetRequiredExperienceForNextLevel();
            if (currentExperience < required)
                break;

            currentExperience -= required;
            LevelUpInternal();
            leveledUpAtLeastOnce = true;
        }

        float nextRequired = GetRequiredExperienceForNextLevel();
        OnExperienceChanged?.Invoke(currentExperience, nextRequired);

        if (leveledUpAtLeastOnce)
            Debug.Log($"Новый уровень: {currentLevel}, опыт: {currentExperience}/{nextRequired}");
    }

    /// <summary>
    /// Применяет runtime-прогресс после перехода на следующий уровень.
    /// Это не save/load на диск, только перенос в памяти.
    /// </summary>
    public void ApplyRuntimeState(int level, float experience)
    {
        int sanitizedLevel = Mathf.Max(1, level);
        if (sanitizedLevel != level)
        {
            Debug.LogWarning(
                $"PlayerProgression.ApplyRuntimeState: получен некорректный уровень {level}. " +
                $"Используем безопасное значение {sanitizedLevel}.",
                this);
        }

        currentLevel = sanitizedLevel;

        float required = GetRequiredExperienceForNextLevel();
        float maxExperience = Mathf.Max(0f, required - 0.0001f);
        float clampedExperience = Mathf.Clamp(experience, 0f, maxExperience);

        if (!Mathf.Approximately(clampedExperience, experience))
        {
            Debug.LogWarning(
                $"PlayerProgression.ApplyRuntimeState: опыт {experience} выходит за границы для уровня {currentLevel}. " +
                $"Применено значение {clampedExperience}.",
                this);
        }

        currentExperience = clampedExperience;

        OnLevelChanged?.Invoke(currentLevel);
        OnExperienceChanged?.Invoke(currentExperience, required);
    }

    /// <summary>
    /// Возвращает требуемый опыт до следующего уровня.
    /// </summary>
    private float GetRequiredExperienceForNextLevel()
    {
        float required = baseExperienceToNextLevel;
        int power = Mathf.Max(0, currentLevel - 1);
        required *= Mathf.Pow(experienceGrowthFactor, power);
        return required;
    }

    /// <summary>
    /// Внутренняя логика повышения уровня.
    /// </summary>
    private void LevelUpInternal()
    {
        currentLevel++;
        OnLevelUp?.Invoke(currentLevel);
        OnLevelChanged?.Invoke(currentLevel);

        if (playerStats != null)
            playerStats.ApplyLevelUpBonuses(10f, 5f);
    }
}
