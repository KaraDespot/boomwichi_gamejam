/*
 * BreadCookVisual
 * Назначение: визуальное состояние прожарки хлеба.
 * Что делает: меняет материалы хлеба при переходах Raw/Toasted/Burnt без постоянной работы в Update.
 * Связи: вызывается SandwichState при смене SandwichCookState.
 * Паттерны: View Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class BreadCookVisual : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("Рендеры хлеба, которым нужно менять материал. Если пусто, компонент найдет Renderer в детях.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Materials")]
    [Tooltip("Материал сырого хлеба.")]
    [SerializeField] private Material rawMaterial;

    [Tooltip("Материал поджаренного хлеба.")]
    [SerializeField] private Material toastedMaterial;

    [Tooltip("Материал пережаренного хлеба.")]
    [SerializeField] private Material burntMaterial;

    private void Awake()
    {
        EnsureRenderers();
    }

    public void Apply(SandwichCookState cookState)
    {
        EnsureRenderers();

        Material material = GetMaterial(cookState);
        if (material == null || targetRenderers == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].sharedMaterial = material;
        }
    }

    private void EnsureRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
            return;

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    private Material GetMaterial(SandwichCookState cookState)
    {
        switch (cookState)
        {
            case SandwichCookState.Toasted:
                return toastedMaterial != null ? toastedMaterial : rawMaterial;

            case SandwichCookState.Burnt:
                return burntMaterial != null ? burntMaterial : toastedMaterial != null ? toastedMaterial : rawMaterial;

            default:
                return rawMaterial;
        }
    }
}
