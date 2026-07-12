/*
 * CockroachSquashEffectSystem
 * Назначение: визуальная обратная связь при раздавливании таракана.
 * Что делает: спавнит burst-FX и оставляет пятно на столе, как клякса соуса.
 * Связи: EventBus.OnCockroachKilled, Cockroach, Table.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections.Generic;
using UnityEngine;

public class CockroachSquashEffectSystem : MonoBehaviour
{
    [Header("FX")]
    [Tooltip("Кратковременный эффект в момент раздавливания.")]
    [SerializeField] private GameObject squashFxPrefab;

    [Tooltip("Сколько секунд держать burst-FX перед уничтожением.")]
    [SerializeField] private float squashFxLifetime = 2f;

    [Header("Пятно на столе")]
    [Tooltip("Плоское пятно, остающееся на поверхности стола.")]
    [SerializeField] private GameObject tableStainPrefab;

    [Tooltip("Родитель для пятен. Если пусто — создаётся контейнер под Table или Managers.")]
    [SerializeField] private Transform stainParent;

    [Tooltip("Подъём пятна над столом, чтобы не мерцало с мешем.")]
    [SerializeField] private float stainSurfaceOffset = 0.003f;

    [Tooltip("Случайный масштаб пятна (мин–макс).")]
    [SerializeField] private Vector2 stainScaleRange = new Vector2(0.85f, 1.2f);

    [Header("Очистка")]
    [Tooltip("Убирать пятна при завершении дня.")]
    [SerializeField] private bool clearStainsOnDayFinished = true;

    private readonly List<GameObject> activeStains = new();
    private Transform runtimeStainParent;

    private void OnEnable()
    {
        if (EventBus.Instance == null)
            return;

        EventBus.Instance.OnCockroachKilled += HandleCockroachKilled;

        if (clearStainsOnDayFinished)
            EventBus.Instance.OnDayFinished += HandleDayFinished;
    }

    private void OnDisable()
    {
        if (EventBus.Instance == null)
            return;

        EventBus.Instance.OnCockroachKilled -= HandleCockroachKilled;

        if (clearStainsOnDayFinished)
            EventBus.Instance.OnDayFinished -= HandleDayFinished;
    }

    private void HandleCockroachKilled(Cockroach cockroach)
    {
        if (cockroach == null)
            return;

        Vector3 squashPoint = cockroach.SquashWorldPoint;
        SpawnSquashFx(squashPoint);
        SpawnTableStain(squashPoint);
    }

    private void HandleDayFinished()
    {
        ClearAllStains();
    }

    private void SpawnSquashFx(Vector3 worldPoint)
    {
        if (squashFxPrefab == null)
            return;

        Vector3 fxPosition = worldPoint + Vector3.up * 0.02f;
        GameObject fxInstance = Instantiate(squashFxPrefab, fxPosition, Quaternion.identity);
        ConfigureOneShotParticles(fxInstance);
        Destroy(fxInstance, Mathf.Max(0.1f, squashFxLifetime));
    }

    private void SpawnTableStain(Vector3 worldPoint)
    {
        if (tableStainPrefab == null)
            return;

        Transform parent = GetStainParent();
        Vector3 stainPosition = worldPoint + Vector3.up * stainSurfaceOffset;
        Quaternion stainRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject stainInstance = Instantiate(tableStainPrefab, stainPosition, stainRotation, parent);

        float scale = Random.Range(stainScaleRange.x, stainScaleRange.y);
        stainInstance.transform.localScale = Vector3.one * scale;
        activeStains.Add(stainInstance);
    }

    private Transform GetStainParent()
    {
        if (stainParent != null)
            return stainParent;

        if (runtimeStainParent != null)
            return runtimeStainParent;

        runtimeStainParent = new GameObject("RoachStains_Runtime").transform;
        runtimeStainParent.SetParent(transform, false);
        return runtimeStainParent;
    }

    private static void ConfigureOneShotParticles(GameObject fxRoot)
    {
        if (fxRoot == null)
            return;

        ParticleSystem[] particleSystems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ClearAllStains()
    {
        for (int i = activeStains.Count - 1; i >= 0; i--)
        {
            if (activeStains[i] != null)
                Destroy(activeStains[i]);
        }

        activeStains.Clear();
    }
}
