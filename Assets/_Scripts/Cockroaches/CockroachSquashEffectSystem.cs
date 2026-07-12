/*
 * CockroachSquashEffectSystem
 * Назначение: визуальная обратная связь при раздавливании таракана.
 * Что делает: спавнит burst-FX и оставляет пятно на столе, как клякса соуса.
 * Связи: EventBus.OnCockroachKilled, Cockroach, Table.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockroachSquashEffectSystem : MonoBehaviour
{
    [Header("FX")]
    [Tooltip("Кратковременный эффект в момент раздавливания.")]
    [SerializeField] private GameObject squashFxPrefab;

    [Tooltip("Масштаб burst-FX относительно префаба.")]
    [SerializeField] private float squashFxScale = 1f;

    [Tooltip("Сколько секунд держать burst-FX перед уничтожением.")]
    [SerializeField] private float squashFxLifetime = 2f;

    [Header("Пятно на столе")]
    [Tooltip("Плоское пятно, остающееся на поверхности стола.")]
    [SerializeField] private GameObject tableStainPrefab;

    [Tooltip("Задержка перед появлением пятна после раздавливания (сек).")]
    [SerializeField] private float stainSpawnDelay;

    [Tooltip("Базовый масштаб пятна относительно префаба.")]
    [SerializeField] private float stainScale = 1f;

    [Tooltip("Случайный множитель масштаба пятна (мин–макс). 1–1 = без разброса.")]
    [SerializeField] private Vector2 stainScaleRandomRange = new Vector2(0.85f, 1.2f);

    [Tooltip("Родитель для пятен. Если пусто — создаётся контейнер под Table или Managers.")]
    [SerializeField] private Transform stainParent;

    [Tooltip("Подъём пятна над столом, чтобы не мерцало с мешем.")]
    [SerializeField] private float stainSurfaceOffset = 0.003f;

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
        StopAllCoroutines();

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

        if (stainSpawnDelay > 0f)
            StartCoroutine(SpawnTableStainDelayed(squashPoint, stainSpawnDelay));
        else
            SpawnTableStain(squashPoint);
    }

    private void HandleDayFinished()
    {
        StopAllCoroutines();
        ClearAllStains();
    }

    private IEnumerator SpawnTableStainDelayed(Vector3 worldPoint, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnTableStain(worldPoint);
    }

    private void SpawnSquashFx(Vector3 worldPoint)
    {
        if (squashFxPrefab == null)
            return;

        Vector3 fxPosition = worldPoint + Vector3.up * 0.02f;
        GameObject fxInstance = Instantiate(squashFxPrefab, fxPosition, Quaternion.identity);
        fxInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, squashFxScale);
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
        TableStain.Configure(stainInstance);

        float randomMultiplier = Random.Range(
            Mathf.Min(stainScaleRandomRange.x, stainScaleRandomRange.y),
            Mathf.Max(stainScaleRandomRange.x, stainScaleRandomRange.y));
        float finalScale = Mathf.Max(0.01f, stainScale) * randomMultiplier;
        stainInstance.transform.localScale = Vector3.one * finalScale;
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
