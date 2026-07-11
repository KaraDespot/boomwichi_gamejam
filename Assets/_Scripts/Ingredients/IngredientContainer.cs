/*
 * IngredientContainer
 * Назначение: интерактивный контейнер ингредиентов на столе.
 * Что делает: по клику создаёт prefab ингредиента, настраивает его тип и отдаёт DragController для немедленного переноса.
 * Связи: находится InputRaycaster по коллайдеру контейнера, создаёт IngredientInstance и DraggableObject.
 * Паттерны: Factory Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class IngredientContainer : MonoBehaviour
{
    [Header("Ingredient")]
    [Tooltip("Prefab ингредиента. Желательно заранее добавить Collider, DraggableObject и IngredientInstance.")]
    [SerializeField] private GameObject ingredientPrefab;

    [Tooltip("Тип ингредиента, который создаёт этот контейнер.")]
    [SerializeField] private IngredientType ingredientType = IngredientType.Bread;

    [Tooltip("Роль хлеба для специальных prefab. Обычный контейнер хлеба оставляет None: доска сама назначит Bottom или Top.")]
    [SerializeField] private BreadRole breadRole = BreadRole.None;

    [Header("Spawn")]
    [Tooltip("Точка появления ингредиента. Если пусто, используется позиция контейнера.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Родитель для созданных ингредиентов. Если пусто, объект создаётся в корне сцены.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("Что делать с созданным ингредиентом, если игрок отпустил его не над валидной зоной.")]
    [SerializeField] private DragFailedDropAction failedDropAction = DragFailedDropAction.DeactivateObject;

    public IngredientType IngredientType => ingredientType;
    public BreadRole BreadRole => breadRole;

    public bool TrySpawnIngredient(out DraggableObject draggableObject)
    {
        draggableObject = null;

        if (ingredientPrefab == null)
        {
            Debug.LogWarning($"{name}: ingredientPrefab не назначен.", this);
            return false;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        GameObject instance = Instantiate(ingredientPrefab, point.position, point.rotation, spawnParent);
        instance.name = $"{ingredientType}_Ingredient";

        draggableObject = instance.GetComponent<DraggableObject>();
        if (draggableObject == null)
            draggableObject = instance.AddComponent<DraggableObject>();

        IngredientInstance ingredientInstance = instance.GetComponent<IngredientInstance>();
        if (ingredientInstance == null)
            ingredientInstance = instance.AddComponent<IngredientInstance>();

        BreadRole spawnedBreadRole = ingredientType == IngredientType.Bread
            ? BreadRole.None
            : breadRole;

        ingredientInstance.Initialize(ingredientType, spawnedBreadRole);
        draggableObject.SetFailedDropAction(failedDropAction);
        draggableObject.SetCanDrag(true);

        if (instance.GetComponentInChildren<Collider>() == null)
            Debug.LogWarning($"{name}: у созданного ингредиента нет Collider. Его можно будет тащить после spawn, но нельзя будет выбрать повторно.", instance);

        return true;
    }
}
