/*
 * Cockroach
 * Назначение: один таракан на столе.
 * Что делает: бродит в заданной зоне, реагирует на клик — раздавливается.
 * Связи: CockroachSpawner, InputRaycaster, DragController, EventBus.
 * Паттерны: Entity Component.
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Cockroach : MonoBehaviour
{
    private static readonly List<Cockroach> activeInstances = new();

    public static IReadOnlyList<Cockroach> ActiveInstances => activeInstances;

    [Header("Движение (задаётся CockroachSpawner)")]
    [SerializeField] private float moveSpeed = 0.55f;

    [SerializeField] private Vector2 directionChangeInterval = new Vector2(0.6f, 1.4f);

    [Header("Клик")]
    [SerializeField] private float clickTargetHeight = 0.04f;

    [Header("Визуал")]
    [Tooltip("Поворачивать модель по направлению движения.")]
    [SerializeField] private bool rotateToMovement = true;

    [Tooltip("Задержка перед уничтожением после клика.")]
    [SerializeField] private float squashDestroyDelay = 0.15f;

    private Bounds wanderBounds;
    private float tableHeight;
    private Vector3 moveDirection;
    private float directionTimer;
    private bool isAlive = true;

    public bool IsAlive => isAlive;

    /// <summary> Точка для screen-space клика (центр зоны раздавливания). </summary>
    public Vector3 ClickWorldPoint => transform.position + Vector3.up * clickTargetHeight;

    /// <summary> Точка на столе, где остаётся пятно после раздавливания. </summary>
    public Vector3 SquashWorldPoint
    {
        get
        {
            Vector3 point = transform.position;
            point.y = tableHeight;
            return point;
        }
    }

    private void OnEnable()
    {
        if (!activeInstances.Contains(this))
            activeInstances.Add(this);
    }

    private void OnDisable()
    {
        activeInstances.Remove(this);
    }

    public void Initialize(Bounds spawnBounds, float surfaceHeight, float speed, Vector2 directionInterval)
    {
        moveSpeed = Mathf.Max(0.01f, speed);
        directionChangeInterval = directionInterval;
        wanderBounds = spawnBounds;
        tableHeight = surfaceHeight;
        PickNewDirection();
        SnapToTableHeight();
    }

    private void Update()
    {
        if (!isAlive)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            return;

        UpdateMovement();
    }

    public void Squash()
    {
        if (!isAlive)
            return;

        isAlive = false;
        enabled = false;
        HideVisuals();

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseCockroachKilled(this);

        Destroy(gameObject, squashDestroyDelay);
    }

    private void HideVisuals()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }
    }

    private void UpdateMovement()
    {
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
            PickNewDirection();

        Vector3 nextPosition = transform.position + moveDirection * (moveSpeed * Time.deltaTime);
        nextPosition = ClampToBounds(nextPosition);
        nextPosition.y = tableHeight;
        transform.position = nextPosition;

        if (rotateToMovement && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
        }
    }

    private void PickNewDirection()
    {
        Vector2 flatDirection = Random.insideUnitCircle.normalized;
        if (flatDirection.sqrMagnitude < 0.01f)
            flatDirection = Vector2.right;

        moveDirection = new Vector3(flatDirection.x, 0f, flatDirection.y);
        directionTimer = Random.Range(directionChangeInterval.x, directionChangeInterval.y);
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, wanderBounds.min.x, wanderBounds.max.x);
        position.z = Mathf.Clamp(position.z, wanderBounds.min.z, wanderBounds.max.z);
        return position;
    }

    private void SnapToTableHeight()
    {
        Vector3 position = transform.position;
        position.y = tableHeight;
        transform.position = position;
    }
}
