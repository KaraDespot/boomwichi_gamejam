/*
 * BreadCookVisual
 * Назначение: визуальное состояние прожарки и плесени хлеба.
 * Что делает: меняет материалы хлеба при переходах Raw/Toasted/Burnt и учитывает clean/moldy-варианты.
 * Связи: вызывается SandwichState при смене SandwichCookState и MoldSystem при появлении/очистке плесени.
 * Паттерны: View Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class BreadCookVisual : MonoBehaviour
{
    [Header("Рендереры")]
    [Tooltip("Рендеры хлеба, которым нужно менять материал. Если пусто, компонент найдет Renderer в детях.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Чистые материалы")]
    [Tooltip("Материал сырого хлеба.")]
    [SerializeField] private Material rawMaterial;

    [Tooltip("Материал поджаренного хлеба.")]
    [SerializeField] private Material toastedMaterial;

    [Tooltip("Материал пережаренного хлеба.")]
    [SerializeField] private Material burntMaterial;

    [Header("Материалы с плесенью")]
    [Tooltip("Материал сырого хлеба с плесенью.")]
    [SerializeField] private Material moldyRawMaterial;

    [Tooltip("Материал поджаренного хлеба с плесенью.")]
    [SerializeField] private Material moldyToastedMaterial;

    [Tooltip("Материал пережаренного хлеба с плесенью.")]
    [SerializeField] private Material moldyBurntMaterial;

    private IngredientInstance ingredient;
    private SandwichCookState currentCookState;
    private bool isMoldy;

    private void Awake()
    {
        ingredient = GetComponent<IngredientInstance>();
        isMoldy = ingredient != null && ingredient.IsMoldy;
        EnsureRenderers();
        ApplyMaterial(GetMaterial(currentCookState, isMoldy));
    }

    private void OnValidate()
    {
        EnsureRenderers();
        ApplyMaterial(GetMaterial(currentCookState, isMoldy));
    }

    public void Apply(SandwichCookState cookState)
    {
        currentCookState = cookState;
        isMoldy = ingredient != null && ingredient.IsMoldy;
        EnsureRenderers();
        ApplyMaterial(GetMaterial(currentCookState, isMoldy));
    }

    public void SetMoldyVisual(bool hasMold)
    {
        isMoldy = hasMold;
        EnsureRenderers();
        ApplyMaterial(GetMaterial(currentCookState, isMoldy));
    }

    private void EnsureRenderers()
    {
        if (HasValidTargetRenderers())
            return;

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    private bool HasValidTargetRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return false;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                return true;
        }

        return false;
    }

    private void ApplyMaterial(Material material)
    {
        if (material == null || targetRenderers == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].sharedMaterial = material;
        }
    }

    private Material GetMaterial(SandwichCookState cookState, bool hasMold)
    {
        switch (cookState)
        {
            case SandwichCookState.Toasted:
                if (hasMold && moldyToastedMaterial != null)
                    return moldyToastedMaterial;

                return toastedMaterial != null ? toastedMaterial : rawMaterial;

            case SandwichCookState.Burnt:
                if (hasMold && moldyBurntMaterial != null)
                    return moldyBurntMaterial;

                return burntMaterial != null ? burntMaterial : toastedMaterial != null ? toastedMaterial : rawMaterial;

            default:
                if (hasMold && moldyRawMaterial != null)
                    return moldyRawMaterial;

                return rawMaterial;
        }
    }
}
