/*
 * Cockroach
 * Назначение: один таракан на столе.
 * Что делает: хаотично бродит к еде на тарелке; при касании убегает или замирает на сендвиче.
 * Связи: CockroachSpawner, SandwichBoard, InputRaycaster, DragController, EventBus.
 * Паттерны: Entity Component, State Machine.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CockroachMovementConfig
{
    public SandwichBoard TargetPlate;
    public float PlateAttractionWeight;
    public float PlateApproachRadius;
    public LayerMask ObstacleLayerMask;
    public float AvoidanceProbeDistance;
    public float AvoidanceProbeRadius;
    public float AvoidanceStrength;
    public float SandwichTouchRadius;
    public float LandOnSandwichChance;
    public float LandApproachBias;
    public float FleeDuration;
    public float FleeSpeedMultiplier;
    public float LandAnimationDuration;
    public float LandDropHeight;
}

public enum CockroachBehaviorState
{
    Wandering,
    Fleeing,
    Landing,
    RestingOnSandwich
}

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
    private Vector3 wanderDirection;
    private Vector3 moveDirection;
    private float directionTimer;
    private bool isAlive = true;

    private SandwichBoard targetPlate;
    private float plateAttractionWeight = 0.72f;
    private float plateApproachRadius = 1.6f;
    private LayerMask obstacleLayerMask;
    private float avoidanceProbeDistance = 0.3f;
    private float avoidanceProbeRadius = 0.065f;
    private float avoidanceStrength = 2.2f;
    private float sandwichTouchRadius = 0.28f;
    private float landOnSandwichChance = 0.45f;
    private float landApproachBias = 0.72f;
    private float fleeDuration = 1.35f;
    private float fleeSpeedMultiplier = 1.35f;
    private float landAnimationDuration = 0.18f;
    private float landDropHeight = 0.14f;

    private CockroachBehaviorState behaviorState = CockroachBehaviorState.Wandering;
    private float fleeTimer;
    private Vector3 fleeDirection;
    private Coroutine landingRoutine;

    private const float DirectionSmoothing = 12f;
    private const float ProbeHeight = 0.05f;
    private const float FarChaosWeight = 0.28f;
    private const float NearChaosWeight = 0.08f;

    public bool IsAlive => isAlive;
    public CockroachBehaviorState BehaviorState => behaviorState;
    public bool IsRestingOnSandwich => behaviorState == CockroachBehaviorState.RestingOnSandwich;

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

    public void Initialize(
        Bounds spawnBounds,
        float surfaceHeight,
        float speed,
        Vector2 directionInterval,
        CockroachMovementConfig movementConfig)
    {
        moveSpeed = Mathf.Max(0.01f, speed);
        directionChangeInterval = directionInterval;
        wanderBounds = spawnBounds;
        tableHeight = surfaceHeight;

        targetPlate = movementConfig.TargetPlate;
        plateAttractionWeight = Mathf.Clamp01(movementConfig.PlateAttractionWeight);
        plateApproachRadius = Mathf.Max(0.05f, movementConfig.PlateApproachRadius);
        obstacleLayerMask = movementConfig.ObstacleLayerMask;
        avoidanceProbeDistance = Mathf.Max(0.05f, movementConfig.AvoidanceProbeDistance);
        avoidanceProbeRadius = Mathf.Max(0.01f, movementConfig.AvoidanceProbeRadius);
        avoidanceStrength = Mathf.Max(0.1f, movementConfig.AvoidanceStrength);
        sandwichTouchRadius = Mathf.Max(0.05f, movementConfig.SandwichTouchRadius);
        landOnSandwichChance = Mathf.Clamp01(movementConfig.LandOnSandwichChance);
        landApproachBias = Mathf.Clamp01(movementConfig.LandApproachBias);
        fleeDuration = Mathf.Max(0.1f, movementConfig.FleeDuration);
        fleeSpeedMultiplier = Mathf.Max(1f, movementConfig.FleeSpeedMultiplier);
        landAnimationDuration = Mathf.Max(0.05f, movementConfig.LandAnimationDuration);
        landDropHeight = Mathf.Max(0.01f, movementConfig.LandDropHeight);

        behaviorState = CockroachBehaviorState.Wandering;
        PickNewWanderDirection(biasTowardPlate: true);
        moveDirection = wanderDirection;
        SnapToTableHeight();
    }

    private void Update()
    {
        if (!isAlive)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            return;

        switch (behaviorState)
        {
            case CockroachBehaviorState.Wandering:
                UpdateWanderingMovement();
                TryTouchSandwich();
                break;

            case CockroachBehaviorState.Fleeing:
                UpdateFleeingMovement();
                break;
        }
    }

    public void Squash()
    {
        if (!isAlive)
            return;

        isAlive = false;
        enabled = false;

        if (landingRoutine != null)
            StopCoroutine(landingRoutine);

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

    private void UpdateWanderingMovement()
    {
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
            PickNewWanderDirection(biasTowardPlate: HasFoodTarget());

        Vector3 desiredDirection = ComputeDesiredDirection();
        desiredDirection = ApplyObstacleAvoidance(desiredDirection, GetHorizontalDistanceToPlateTarget());
        ApplyMovement(desiredDirection, moveSpeed);
    }

    private void UpdateFleeingMovement()
    {
        fleeTimer -= Time.deltaTime;
        if (fleeTimer <= 0f)
        {
            behaviorState = CockroachBehaviorState.Wandering;
            PickNewWanderDirection(biasTowardPlate: HasFoodTarget());
            return;
        }

        Vector3 desiredDirection = fleeDirection;
        desiredDirection = ApplyObstacleAvoidance(desiredDirection, float.MaxValue);
        ApplyMovement(desiredDirection, moveSpeed * fleeSpeedMultiplier);
    }

    private void ApplyMovement(Vector3 desiredDirection, float speed)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f)
            PickNewWanderDirection(biasTowardPlate: HasFoodTarget());
        else
            moveDirection = Vector3.Slerp(moveDirection, desiredDirection.normalized, Time.deltaTime * DirectionSmoothing).normalized;

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + moveDirection * (speed * Time.deltaTime);
        nextPosition = ResolveMovementCollision(currentPosition, nextPosition);
        nextPosition = ClampToBounds(nextPosition);
        nextPosition.y = tableHeight;
        transform.position = nextPosition;

        if (rotateToMovement && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
        }
    }

    private void TryTouchSandwich()
    {
        if (!HasFoodTarget() || targetPlate.CurrentSandwich == null)
            return;

        SandwichState sandwich = targetPlate.CurrentSandwich;
        if (!IsTouchingSandwich(sandwich))
            return;

        Vector3 approachPoint = transform.position;
        ApplySandwichContamination(sandwich);

        bool wantsToLand = Random.value < landOnSandwichChance;
        if (wantsToLand &&
            targetPlate.TryGetCockroachLandPosition(
                approachPoint,
                out Vector3 landPosition,
                out Quaternion landRotation,
                landApproachBias))
        {
            StartLandingOnSandwich(sandwich, landPosition, landRotation);
        }
        else
        {
            StartFleeing(sandwich);
        }
    }

    private void ApplySandwichContamination(SandwichState sandwich)
    {
        if (sandwich == null || sandwich.HasRoachContact)
            return;

        sandwich.MarkRoachContact();

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseSandwichCockroachContaminated(sandwich);
    }

    private void StartFleeing(SandwichState sandwich)
    {
        behaviorState = CockroachBehaviorState.Fleeing;
        fleeTimer = fleeDuration;

        Vector3 awayFromSandwich = transform.position - sandwich.transform.position;
        awayFromSandwich.y = 0f;

        if (awayFromSandwich.sqrMagnitude < 0.0001f)
            awayFromSandwich = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        fleeDirection = awayFromSandwich.normalized;
        moveDirection = fleeDirection;
        directionTimer = Random.Range(directionChangeInterval.x, directionChangeInterval.y);
    }

    private void StartLandingOnSandwich(SandwichState sandwich, Vector3 landPosition, Quaternion landRotation)
    {
        behaviorState = CockroachBehaviorState.Landing;

        if (landingRoutine != null)
            StopCoroutine(landingRoutine);

        landingRoutine = StartCoroutine(LandOnSandwichRoutine(sandwich.transform, landPosition, landRotation));
    }

    private IEnumerator LandOnSandwichRoutine(Transform sandwichRoot, Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = targetPosition + Vector3.up * landDropHeight;
        transform.SetParent(sandwichRoot, true);
        transform.position = startPosition;
        transform.rotation = targetRotation;

        float elapsed = 0f;
        while (elapsed < landAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / landAnimationDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        behaviorState = CockroachBehaviorState.RestingOnSandwich;
        landingRoutine = null;
    }

    private bool IsTouchingSandwich(SandwichState sandwich)
    {
        Vector3 delta = transform.position - sandwich.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= sandwichTouchRadius * sandwichTouchRadius;
    }

    private bool HasFoodTarget()
    {
        return targetPlate != null && targetPlate.HasBottomBread && !targetPlate.IsClosed;
    }

    private bool HasPlateTarget()
    {
        return targetPlate != null;
    }

    private Vector3 ComputeDesiredDirection()
    {
        if (!HasPlateTarget())
            return wanderDirection;

        Vector3 toPlate = GetDirectionToPlateTarget();
        if (toPlate.sqrMagnitude < 0.0001f)
            return wanderDirection;

        if (!HasFoodTarget())
        {
            float idleSeek = plateAttractionWeight * 0.35f;
            return Vector3.Slerp(wanderDirection, toPlate, idleSeek).normalized;
        }

        float distanceToPlate = GetHorizontalDistanceToPlateTarget();
        float approachFactor = 1f - Mathf.Clamp01(distanceToPlate / plateApproachRadius);
        float chaosWeight = Mathf.Lerp(FarChaosWeight, NearChaosWeight, approachFactor);
        float seekWeight = 1f - chaosWeight;
        float seekStrength = Mathf.Lerp(plateAttractionWeight * 0.65f, 0.98f, approachFactor);

        Vector3 chaoticSeek = wanderDirection * chaosWeight + toPlate * seekWeight;
        Vector3 directedSeek = Vector3.Slerp(wanderDirection, toPlate, seekStrength);
        Vector3 blended = Vector3.Slerp(chaoticSeek, directedSeek, approachFactor);

        return blended.sqrMagnitude > 0.0001f ? blended.normalized : toPlate;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredDirection, float distanceToPlate)
    {
        if (obstacleLayerMask.value == 0 || desiredDirection.sqrMagnitude < 0.0001f)
            return desiredDirection;

        Vector3 origin = GetProbeOrigin();
        Vector3 direction = desiredDirection.normalized;
        Vector3 avoidance = Vector3.zero;
        int hitCount = 0;

        if (TryGetObstacleHit(origin, direction, out RaycastHit centerHit))
        {
            avoidance += GetAvoidanceVector(centerHit, direction);
            hitCount++;
        }

        Vector3 leftDirection = Quaternion.Euler(0f, -35f, 0f) * direction;
        if (TryGetObstacleHit(origin, leftDirection, out RaycastHit leftHit))
        {
            avoidance += GetAvoidanceVector(leftHit, direction);
            hitCount++;
        }

        Vector3 rightDirection = Quaternion.Euler(0f, 35f, 0f) * direction;
        if (TryGetObstacleHit(origin, rightDirection, out RaycastHit rightHit))
        {
            avoidance += GetAvoidanceVector(rightHit, direction);
            hitCount++;
        }

        if (hitCount == 0)
            return desiredDirection;

        float avoidanceScale = distanceToPlate <= plateApproachRadius * 0.75f
            ? 0.35f
            : 1f;

        Vector3 adjusted = direction + avoidance / hitCount * avoidanceStrength * avoidanceScale;
        return adjusted.sqrMagnitude > 0.0001f ? adjusted.normalized : direction;
    }

    private Vector3 ResolveMovementCollision(Vector3 currentPosition, Vector3 targetPosition)
    {
        Vector3 delta = targetPosition - currentPosition;
        float distance = delta.magnitude;
        if (distance <= 0.0001f || obstacleLayerMask.value == 0)
            return targetPosition;

        Vector3 origin = GetProbeOrigin(currentPosition);
        Vector3 direction = delta / distance;

        if (!Physics.SphereCast(
                origin,
                avoidanceProbeRadius,
                direction,
                out RaycastHit hit,
                distance,
                obstacleLayerMask,
                QueryTriggerInteraction.Ignore) ||
            ShouldIgnoreObstacle(hit.collider))
        {
            return targetPosition;
        }

        Vector3 slideDelta = Vector3.ProjectOnPlane(delta, hit.normal);
        if (slideDelta.sqrMagnitude < 0.00001f)
            return currentPosition;

        return currentPosition + slideDelta.normalized * Mathf.Min(distance, slideDelta.magnitude);
    }

    private bool TryGetObstacleHit(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        if (Physics.SphereCast(
                origin,
                avoidanceProbeRadius,
                direction,
                out hit,
                avoidanceProbeDistance,
                obstacleLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return !ShouldIgnoreObstacle(hit.collider);
        }

        hit = default;
        return false;
    }

    private Vector3 GetAvoidanceVector(RaycastHit hit, Vector3 desiredDirection)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal);
        if (tangent.sqrMagnitude < 0.0001f)
            return -desiredDirection;

        tangent.Normalize();
        if (Vector3.Dot(tangent, desiredDirection) < 0f)
            tangent = -tangent;

        return tangent;
    }

    private bool ShouldIgnoreObstacle(Collider collider)
    {
        if (collider == null)
            return true;

        if (collider.transform.IsChildOf(transform))
            return true;

        if (targetPlate != null && collider.transform.IsChildOf(targetPlate.transform))
            return true;

        if (targetPlate != null && targetPlate.CurrentSandwich != null)
        {
            Transform sandwichRoot = targetPlate.CurrentSandwich.transform;
            if (collider.transform == sandwichRoot || collider.transform.IsChildOf(sandwichRoot))
                return true;
        }

        SandwichBoard board = collider.GetComponentInParent<SandwichBoard>();
        if (board != null && board == targetPlate)
            return true;

        return false;
    }

    private Vector3 GetDirectionToPlateTarget()
    {
        Vector3 target = GetPlateTargetPosition();
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.zero;
    }

    private float GetHorizontalDistanceToPlateTarget()
    {
        Vector3 target = GetPlateTargetPosition();
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private Vector3 GetPlateTargetPosition()
    {
        if (targetPlate == null)
            return transform.position;

        return targetPlate.GetRoachTargetWorldPoint();
    }

    private Vector3 GetProbeOrigin()
    {
        return GetProbeOrigin(transform.position);
    }

    private static Vector3 GetProbeOrigin(Vector3 position)
    {
        return position + Vector3.up * ProbeHeight;
    }

    private void PickNewWanderDirection(bool biasTowardPlate)
    {
        Vector2 flatDirection = Random.insideUnitCircle.normalized;
        if (flatDirection.sqrMagnitude < 0.01f)
            flatDirection = Vector2.right;

        wanderDirection = new Vector3(flatDirection.x, 0f, flatDirection.y);

        if (biasTowardPlate && HasPlateTarget())
        {
            Vector3 toPlate = GetDirectionToPlateTarget();
            if (toPlate.sqrMagnitude > 0.0001f)
            {
                float blend = HasFoodTarget() ? plateAttractionWeight * 0.55f : plateAttractionWeight * 0.25f;
                wanderDirection = Vector3.Slerp(wanderDirection, toPlate, blend).normalized;
            }
        }

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
