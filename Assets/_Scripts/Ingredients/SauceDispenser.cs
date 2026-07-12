/*
 * SauceDispenser
 * Назначение: переносимый контейнер соуса, который наносит кляксу на сендвич.
 * Что делает: при drop на доску создаёт prefab соуса, а сам контейнер возвращает на старт drag.
 * Связи: перетаскивается через DraggableObject, принимается SandwichBoard, создаёт IngredientInstance типа Sauce.
 * Паттерны: Factory Component, Source Object.
 */

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DraggableObject))]
public class SauceDispenser : MonoBehaviour
{
    [Header("Sauce")]
    [Tooltip("Prefab кляксы соуса, которая появится на хлебе после drop контейнера на сендвич.")]
    [SerializeField] private GameObject saucePrefab;

    [Tooltip("Должен ли контейнер автоматически возвращаться на место при неудачном drop.")]
    [SerializeField] private bool returnToStartOnFailedDrop = true;

    public GameObject SaucePrefab => saucePrefab;

    private void Awake()
    {
        if (!returnToStartOnFailedDrop)
            return;

        DraggableObject draggableObject = GetComponent<DraggableObject>();
        if (draggableObject != null)
            draggableObject.SetFailedDropAction(DragFailedDropAction.ReturnToStart);
    }

    public bool CanDispense(SandwichState sandwichState)
    {
        return isActiveAndEnabled &&
            saucePrefab != null &&
            sandwichState != null &&
            sandwichState.HasBottomBread &&
            !sandwichState.IsClosed &&
            !sandwichState.HasSauce;
    }

    public bool TryCreateSauce(out DraggableObject sauceDraggable, out IngredientInstance sauceIngredient)
    {
        sauceDraggable = null;
        sauceIngredient = null;

        if (saucePrefab == null)
        {
            Debug.LogWarning($"{name}: saucePrefab не назначен.", this);
            return false;
        }

        GameObject sauceInstance = Instantiate(saucePrefab);
        sauceInstance.name = $"{IngredientType.Sauce}_Ingredient";

        sauceDraggable = sauceInstance.GetComponent<DraggableObject>();
        if (sauceDraggable == null)
            sauceDraggable = sauceInstance.AddComponent<DraggableObject>();

        sauceIngredient = sauceInstance.GetComponent<IngredientInstance>();
        if (sauceIngredient == null)
            sauceIngredient = sauceInstance.AddComponent<IngredientInstance>();

        sauceIngredient.Initialize(IngredientType.Sauce, BreadRole.None);
        sauceDraggable.SetFailedDropAction(DragFailedDropAction.DeactivateObject);
        sauceDraggable.SetCanDrag(true);

        if (sauceInstance.GetComponentInChildren<Collider>() == null)
            Debug.LogWarning($"{name}: у созданной кляксы соуса нет Collider.", sauceInstance);

        return true;
    }
}
