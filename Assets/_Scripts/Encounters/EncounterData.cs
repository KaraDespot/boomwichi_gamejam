/*
 * EncounterData
 * Назначение: конфиг encounter, состоящего из нескольких волн.
 * Что делает: хранит список WaveData и общие правила выполнения encounter.
 * Связи: используется EncounterTrigger для запуска и контроля прохождения encounter.
 * Паттерны: Data-driven, ScriptableObject как конфиг.
 */

using UnityEngine;

/// <summary>
/// Конфигурация encounter-геймплея (набор волн + правила завершения).
/// </summary>
[CreateAssetMenu(
    fileName = "EncounterData",
    menuName = "Game Data/Encounter/Encounter Data",
    order = 1)]
public class EncounterData : ScriptableObject
{
    [Header("Идентификация")]
    [Tooltip("Человекочитаемый ID encounter для логов и отладки.")]
    [SerializeField] private string encounterId = "encounter_01";

    [Header("Волны")]
    [Tooltip("Список волн, которые будут запущены последовательно.")]
    [SerializeField] private WaveData[] waves;

    [Header("Тайминги")]
    [Tooltip("Пауза между волнами (если есть следующая волна).")]
    [Min(0f)]
    [SerializeField] private float delayBetweenWaves = 1f;

    [Header("Поведение")]
    [Tooltip("Если включено — encounter можно пройти только один раз за запуск сцены.")]
    [SerializeField] private bool oneShot = true;

    public string EncounterId => encounterId;
    public float DelayBetweenWaves => delayBetweenWaves;
    public bool OneShot => oneShot;
    public int WaveCount => waves != null ? waves.Length : 0;

    /// <summary>
    /// Возвращает волну по индексу или null при выходе за границы.
    /// </summary>
    public WaveData GetWave(int index)
    {
        if (waves == null || index < 0 || index >= waves.Length)
            return null;

        return waves[index];
    }

    /// <summary>
    /// Проверяет, содержит ли encounter хотя бы одну валидную волну.
    /// </summary>
    public bool HasAnyValidWave()
    {
        if (waves == null || waves.Length == 0)
            return false;

        for (int i = 0; i < waves.Length; i++)
        {
            WaveData wave = waves[i];
            if (wave != null && wave.IsValid)
                return true;
        }

        return false;
    }
}
