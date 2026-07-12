/*
 * InputRaycaster
 * Назначение: переводит экранную позицию курсора в понятные игровой логике попадания.
 * Что делает: ищет переносимые объекты, drop zone и точку на плоскости стола.
 * Связи: используется DragController; ожидает коллайдеры на столе, DraggableObject и DropZone.
 * Паттерны: Service/Adapter между Camera.PhysicsRaycast и gameplay-кодом.
 */

using UnityEngine;

public class InputRaycaster : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Камера, из которой строится луч курсора. Если поле пустое, берётся Camera.main.")]
    [SerializeField] private Camera rayCamera;

    [Header("Слои")]
    [Tooltip("Слой поверхности стола. Если луч не попал в коллайдер, можно использовать fallback-плоскость.")]
    [SerializeField] private LayerMask tableLayerMask = ~0;

    [Tooltip("Слой объектов, которые можно тащить мышью.")]
    [SerializeField] private LayerMask draggableLayerMask = ~0;

    [Tooltip("Слой drop zone: доска, мусорка и будущие зоны.")]
    [SerializeField] private LayerMask dropZoneLayerMask = ~0;

    [Tooltip("Слой контейнеров ингредиентов, из которых создаются новые ингредиенты.")]
    [SerializeField] private LayerMask ingredientContainerLayerMask = ~0;

    [Header("Плоскость стола")]
    [Tooltip("Максимальная дистанция raycast от камеры.")]
    [SerializeField] private float maxRayDistance = 100f;

    [Tooltip("Если у стола ещё нет коллайдера, курсор будет проецироваться на горизонтальную плоскость с этой высотой.")]
    [SerializeField] private bool useFallbackTablePlane = true;

    [Tooltip("Y-координата fallback-плоскости стола.")]
    [SerializeField] private float fallbackTableHeight = 0f;

    private void Awake()
    {
        if (rayCamera == null)
            rayCamera = Camera.main;
    }

    public bool TryGetTablePoint(Vector2 screenPosition, out Vector3 tablePoint)
    {
        tablePoint = Vector3.zero;

        if (!TryCreateRay(screenPosition, out Ray ray))
            return false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, tableLayerMask, QueryTriggerInteraction.Ignore))
        {
            tablePoint = hit.point;
            return true;
        }

        if (!useFallbackTablePlane)
            return false;

        Plane tablePlane = new Plane(Vector3.up, new Vector3(0f, fallbackTableHeight, 0f));
        if (!tablePlane.Raycast(ray, out float enter))
            return false;

        tablePoint = ray.GetPoint(enter);
        return true;
    }

    public bool TryGetDraggable(Vector2 screenPosition, out DraggableObject draggableObject)
    {
        draggableObject = null;

        if (!TryCreateRay(screenPosition, out Ray ray))
            return false;

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, draggableLayerMask, QueryTriggerInteraction.Collide);
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            DraggableObject candidate = hits[i].collider.GetComponentInParent<DraggableObject>();
            if (candidate == null || !candidate.CanDrag || hits[i].distance >= closestDistance)
                continue;

            draggableObject = candidate;
            closestDistance = hits[i].distance;
        }

        return draggableObject != null;
    }

    public bool TryGetDropZone(Vector2 screenPosition, out DropZone dropZone)
    {
        return TryGetDropZone(screenPosition, out dropZone, out _);
    }

    public bool TryGetDropZone(Vector2 screenPosition, out DropZone dropZone, out Vector3 dropPoint)
    {
        dropZone = null;
        dropPoint = Vector3.zero;

        if (!TryCreateRay(screenPosition, out Ray ray))
            return false;

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, dropZoneLayerMask, QueryTriggerInteraction.Collide);
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            DropZone candidate = hits[i].collider.GetComponentInParent<DropZone>();
            if (candidate == null || hits[i].distance >= closestDistance)
                continue;

            dropZone = candidate;
            dropPoint = hits[i].point;
            closestDistance = hits[i].distance;
        }

        return dropZone != null;
    }

    public bool TryGetIngredientContainer(Vector2 screenPosition, out IngredientContainer ingredientContainer)
    {
        ingredientContainer = null;

        if (!TryCreateRay(screenPosition, out Ray ray))
            return false;

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, ingredientContainerLayerMask, QueryTriggerInteraction.Collide);
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            IngredientContainer candidate = hits[i].collider.GetComponentInParent<IngredientContainer>();
            if (candidate == null || !candidate.isActiveAndEnabled || hits[i].distance >= closestDistance)
                continue;

            ingredientContainer = candidate;
            closestDistance = hits[i].distance;
        }

        return ingredientContainer != null;
    }

    private bool TryCreateRay(Vector2 screenPosition, out Ray ray)
    {
        if (rayCamera == null)
            rayCamera = Camera.main;

        if (rayCamera == null)
        {
            Debug.LogWarning("InputRaycaster: камера для raycast не назначена и Camera.main не найдена.");
            ray = default;
            return false;
        }

        ray = rayCamera.ScreenPointToRay(screenPosition);
        return true;
    }
}
