/*
 * TableStain
 * Назначение: декоративное пятно на столе после раздавливания таракана.
 * Что делает: отключает drag, физику и коллайдеры — пятно только визуальное.
 * Связи: создаётся CockroachSquashEffectSystem.
 * Паттерны: Marker Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class TableStain : MonoBehaviour
{
    private void Awake()
    {
        Configure(gameObject);
    }

    /// <summary>
    /// Делает объект чисто визуальным: без drag, физики и raycast-хитов.
    /// </summary>
    public static void Configure(GameObject stainRoot)
    {
        if (stainRoot == null)
            return;

        DraggableObject[] draggables = stainRoot.GetComponentsInChildren<DraggableObject>(true);
        for (int i = 0; i < draggables.Length; i++)
        {
            draggables[i].SetCanDrag(false);
            draggables[i].enabled = false;
            Destroy(draggables[i]);
        }

        IngredientInstance[] ingredients = stainRoot.GetComponentsInChildren<IngredientInstance>(true);
        for (int i = 0; i < ingredients.Length; i++)
            Destroy(ingredients[i]);

        SauceDispenser[] dispensers = stainRoot.GetComponentsInChildren<SauceDispenser>(true);
        for (int i = 0; i < dispensers.Length; i++)
            Destroy(dispensers[i]);

        Rigidbody[] rigidbodies = stainRoot.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
            Destroy(rigidbodies[i]);

        Collider[] colliders = stainRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
            Destroy(colliders[i]);
        }

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer < 0)
            ignoreRaycastLayer = stainRoot.layer;

        SetLayerRecursively(stainRoot, ignoreRaycastLayer);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
    }
}
