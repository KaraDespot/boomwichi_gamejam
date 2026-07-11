/*
 * WaveData
 * Назначение: конфиг одной волны врагов для encounter-системы.
 * Что делает: хранит, кого и как часто спавнить в рамках одной волны.
 * Связи: используется EncounterData и EncounterTrigger.
 * Паттерны: Data-driven, ScriptableObject как конфиг.
 */

using UnityEngine;

/// <summary>
/// Конфигурация одной волны encounter.
/// </summary>
[CreateAssetMenu(
    fileName = "WaveData",
    menuName = "Game Data/Encounter/Wave Data",
    order = 0)]
public class WaveData : ScriptableObject
{
    [Header("Состав волны")]
    [Tooltip("Тип врага, который будет заспавнен в этой волне.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Сколько врагов этого типа создать в волне.")]
    [Min(1)]
    [SerializeField] private int enemyCount = 3;

    [Header("Тайминги")]
    [Tooltip("Задержка перед стартом волны.")]
    [Min(0f)]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("Интервал между спавнами внутри одной волны.")]
    [Min(0f)]
    [SerializeField] private float spawnInterval = 0.5f;

    [Tooltip("Ожидать ли полного добивания волны перед переходом к следующей.")]
    [SerializeField] private bool waitUntilWaveDefeated = true;

    public EnemyData EnemyData => enemyData;
    public int EnemyCount => enemyCount;
    public float StartDelay => startDelay;
    public float SpawnInterval => spawnInterval;
    public bool WaitUntilWaveDefeated => waitUntilWaveDefeated;

    /// <summary>
    /// Валидация конфига волны до запуска encounter.
    /// </summary>
    public bool IsValid => enemyData != null && enemyData.prefab != null && enemyCount > 0;
}
