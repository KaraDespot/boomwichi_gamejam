/*
 * CockroachSpawner
 * Назначение: появление тараканов во время активного заказа.
 * Что делает: спавнит тараканов на столе с нарастающей частотой к концу дня.
 * Связи: DayTimer, GameManager, EventBus, InputRaycaster, Cockroach.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections.Generic;
using UnityEngine;

public class CockroachSpawner : MonoBehaviour
{
    [Header("Префаб")]
    [Tooltip("Визуал таракана. Если пусто — создаётся простая капсула для теста.")]
    [SerializeField] private GameObject cockroachPrefab;

    [Tooltip("Масштаб инстанса префаба.")]
    [SerializeField] private float spawnScale = 0.12f;

    [Header("Поведение")]
    [Tooltip("Скорость бега тараканов по столу.")]
    [SerializeField] private float moveSpeed = 0.55f;

    [Tooltip("Интервал смены направления (мин–макс, сек).")]
    [SerializeField] private Vector2 directionChangeInterval = new Vector2(0.6f, 1.4f);

    [Tooltip("Радиус зоны раздавливания в мировых единицах.")]
    [SerializeField] private float clickWorldRadius = 0.42f;

    [Header("Частота")]
    [Tooltip("Интервал между спавнами в начале дня (сек).")]
    [SerializeField] private float minSpawnInterval = 18f;

    [Tooltip("Интервал между спавнами к концу дня (сек).")]
    [SerializeField] private float maxSpawnInterval = 7f;

    [Tooltip("Максимум живых тараканов одновременно.")]
    [SerializeField] private int maxAliveCockroaches = 3;

    [Header("Зона спавна")]
    [Tooltip("Центр области на столе, где появляются тараканы.")]
    [SerializeField] private Transform spawnAreaCenter;

    [Tooltip("Половина размера зоны по X и Z от центра.")]
    [SerializeField] private Vector2 spawnHalfExtents = new Vector2(1.6f, 0.9f);

    [Tooltip("Дополнительный отступ от края зоны, чтобы не появлялись за пределами стола.")]
    [SerializeField] private float spawnEdgePadding = 0.15f;

    [Tooltip("Спавнить только по краям стола, а не в центре.")]
    [SerializeField] private bool spawnAtTableEdges = true;

    [Tooltip("Ширина полосы у края стола, где появляются тараканы.")]
    [SerializeField] private float spawnBorderWidth = 0.35f;

    [Header("Связи")]
    [SerializeField] private DayTimer dayTimer;
    [SerializeField] private InputRaycaster inputRaycaster;

    [Tooltip("Высота поверхности стола, если raycast не попал.")]
    [SerializeField] private float fallbackTableHeight = 0.52f;

    [Header("Отладка")]
    [Tooltip("Сразу спавнить одного таракана при старте PlayingOrder.")]
    [SerializeField] private bool spawnOneImmediatelyForTesting;

    private readonly List<Cockroach> aliveCockroaches = new();
    private Transform spawnParent;
    private float spawnCooldown;
    private bool spawningEnabled;

    private void Awake()
    {
        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        if (inputRaycaster == null)
            inputRaycaster = FindFirstObjectByType<InputRaycaster>();

        spawnParent = new GameObject("Cockroaches_Runtime").transform;
        spawnParent.SetParent(transform, false);
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayStateChanged += HandleDayStateChanged;
            EventBus.Instance.OnDayFinished += HandleDayFinished;
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayStateChanged -= HandleDayStateChanged;
            EventBus.Instance.OnDayFinished -= HandleDayFinished;
        }

        ClearAllCockroaches();
    }

    private void Update()
    {
        if (!spawningEnabled)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            return;

        CleanupDeadReferences();

        spawnCooldown -= Time.deltaTime;
        if (spawnCooldown > 0f)
            return;

        if (aliveCockroaches.Count >= maxAliveCockroaches)
            return;

        SpawnCockroach();
        spawnCooldown = GetCurrentSpawnInterval();
    }

    private void HandleDayStateChanged(DayFlowState state)
    {
        bool shouldSpawn = state == DayFlowState.PlayingOrder;
        spawningEnabled = shouldSpawn;

        if (!shouldSpawn)
        {
            spawnCooldown = 0f;
            return;
        }

        spawnCooldown = spawnOneImmediatelyForTesting ? 0f : GetCurrentSpawnInterval() * 0.5f;
    }

    private void HandleDayFinished()
    {
        spawningEnabled = false;
        ClearAllCockroaches();
    }

    private float GetCurrentSpawnInterval()
    {
        float dayProgress = dayTimer != null ? dayTimer.DayProgress : 0f;
        return Mathf.Lerp(minSpawnInterval, maxSpawnInterval, dayProgress);
    }

    private void SpawnCockroach()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        float tableHeight = GetTableHeight(spawnPosition);

        GameObject instance = CreateCockroachInstance(spawnPosition);
        if (instance == null)
            return;

        instance.transform.SetParent(spawnParent, true);
        instance.transform.position = new Vector3(spawnPosition.x, tableHeight, spawnPosition.z);
        instance.transform.localScale = Vector3.one * spawnScale;
        SetLayerRecursively(instance, LayerMask.NameToLayer("Cockroach"));

        SetupClickTarget(instance, clickWorldRadius);

        Cockroach cockroach = instance.GetComponent<Cockroach>();
        if (cockroach == null)
            cockroach = instance.AddComponent<Cockroach>();

        Bounds wanderBounds = BuildWanderBounds(tableHeight);
        cockroach.Initialize(wanderBounds, tableHeight, moveSpeed, directionChangeInterval);
        aliveCockroaches.Add(cockroach);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseCockroachSpawned(cockroach);
    }

    private GameObject CreateCockroachInstance(Vector3 spawnPosition)
    {
        if (cockroachPrefab != null)
            return Instantiate(cockroachPrefab, spawnPosition, Quaternion.identity);

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.name = "Cockroach_Fallback";
        fallback.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            renderer.material = new Material(shader);
            renderer.material.color = new Color(0.35f, 0.22f, 0.08f, 1f);
        }

        return fallback;
    }

    private void SetupClickTarget(GameObject instance, float worldClickRadius)
    {
        int cockroachLayer = LayerMask.NameToLayer("Cockroach");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        float radius = Mathf.Max(0.05f, worldClickRadius);

        Collider[] existingColliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < existingColliders.Length; i++)
        {
            Collider collider = existingColliders[i];
            if (collider == null || collider.gameObject.name == "ClickTarget")
                continue;

            collider.gameObject.layer = ignoreRaycastLayer;
        }

        Transform clickTransform = instance.transform.Find("ClickTarget");
        GameObject clickObject = clickTransform != null
            ? clickTransform.gameObject
            : new GameObject("ClickTarget");

        if (clickTransform == null)
            clickObject.transform.SetParent(instance.transform, false);

        clickObject.layer = cockroachLayer >= 0 ? cockroachLayer : instance.layer;
        clickObject.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        clickObject.transform.localRotation = Quaternion.identity;
        clickObject.transform.localScale = Vector3.one;

        SphereCollider clickCollider = clickObject.GetComponent<SphereCollider>();
        if (clickCollider == null)
            clickCollider = clickObject.AddComponent<SphereCollider>();

        clickCollider.isTrigger = true;
        float uniformScale = Mathf.Max(instance.transform.lossyScale.x, 0.01f);
        clickCollider.radius = radius / uniformScale;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnAtTableEdges)
            return GetRandomEdgeSpawnPosition();

        return GetRandomAreaSpawnPosition();
    }

    private Vector3 GetRandomAreaSpawnPosition()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
        Vector2 innerHalfExtents = GetInnerHalfExtents();

        float x = Random.Range(-innerHalfExtents.x, innerHalfExtents.x);
        float z = Random.Range(-innerHalfExtents.y, innerHalfExtents.y);
        return center + new Vector3(x, 0f, z);
    }

    private Vector3 GetRandomEdgeSpawnPosition()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
        Vector2 innerHalfExtents = GetInnerHalfExtents();
        float border = Mathf.Clamp(
            spawnBorderWidth,
            0.05f,
            Mathf.Min(innerHalfExtents.x, innerHalfExtents.y) * 0.5f);

        int side = Random.Range(0, 4);
        float along = Random.Range(-1f, 1f);

        Vector3 offset = side switch
        {
            0 => new Vector3(Random.Range(-innerHalfExtents.x, -innerHalfExtents.x + border), 0f, along * innerHalfExtents.y),
            1 => new Vector3(Random.Range(innerHalfExtents.x - border, innerHalfExtents.x), 0f, along * innerHalfExtents.y),
            2 => new Vector3(along * innerHalfExtents.x, 0f, Random.Range(-innerHalfExtents.y, -innerHalfExtents.y + border)),
            _ => new Vector3(along * innerHalfExtents.x, 0f, Random.Range(innerHalfExtents.y - border, innerHalfExtents.y)),
        };

        return center + offset;
    }

    private Vector2 GetInnerHalfExtents()
    {
        return new Vector2(
            Mathf.Max(0.1f, spawnHalfExtents.x - spawnEdgePadding),
            Mathf.Max(0.1f, spawnHalfExtents.y - spawnEdgePadding));
    }

    private Bounds BuildWanderBounds(float tableHeight)
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
        Vector3 boundsCenter = new Vector3(center.x, tableHeight, center.z);
        Vector3 boundsSize = new Vector3(spawnHalfExtents.x * 2f, 0.2f, spawnHalfExtents.y * 2f);
        return new Bounds(boundsCenter, boundsSize);
    }

    private float GetTableHeight(Vector3 worldPosition)
    {
        Camera camera = Camera.main;
        if (inputRaycaster != null && camera != null)
        {
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z > 0f &&
                inputRaycaster.TryGetTablePoint(screenPosition, out Vector3 tablePoint))
            {
                return tablePoint.y;
            }
        }

        return fallbackTableHeight;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0)
            return;

        root.layer = layer;
        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
    }

    private void CleanupDeadReferences()
    {
        for (int i = aliveCockroaches.Count - 1; i >= 0; i--)
        {
            if (aliveCockroaches[i] == null)
                aliveCockroaches.RemoveAt(i);
        }
    }

    private void ClearAllCockroaches()
    {
        for (int i = aliveCockroaches.Count - 1; i >= 0; i--)
        {
            if (aliveCockroaches[i] != null)
                Destroy(aliveCockroaches[i].gameObject);
        }

        aliveCockroaches.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
        Vector2 innerHalfExtents = GetInnerHalfExtents();

        Gizmos.color = new Color(0.6f, 0.35f, 0.1f, 0.2f);
        Gizmos.DrawCube(center, new Vector3(innerHalfExtents.x * 2f, 0.05f, innerHalfExtents.y * 2f));

        if (!spawnAtTableEdges)
            return;

        Gizmos.color = new Color(0.85f, 0.45f, 0.1f, 0.85f);
        float halfX = innerHalfExtents.x;
        float halfZ = innerHalfExtents.y;
        Vector3 y = Vector3.up * 0.03f;

        Gizmos.DrawLine(center + new Vector3(-halfX, 0f, -halfZ) + y, center + new Vector3(-halfX, 0f, halfZ) + y);
        Gizmos.DrawLine(center + new Vector3(halfX, 0f, -halfZ) + y, center + new Vector3(halfX, 0f, halfZ) + y);
        Gizmos.DrawLine(center + new Vector3(-halfX, 0f, -halfZ) + y, center + new Vector3(halfX, 0f, -halfZ) + y);
        Gizmos.DrawLine(center + new Vector3(-halfX, 0f, halfZ) + y, center + new Vector3(halfX, 0f, halfZ) + y);
    }
}
