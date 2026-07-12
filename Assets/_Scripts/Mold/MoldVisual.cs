/*
 * MoldVisual
 * Назначение: визуал плесени через смену текстуры/материала самого продукта.
 * Что делает: кэширует чистый вид при спавне, переключает на moldy-вариант и обратно.
 * Связи: IngredientInstance, MoldSystem.
 * Паттерны: View Component.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class MoldVisual : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    [Header("Рендереры")]
    [Tooltip("Какие MeshRenderer менять. Если пусто — все дочерние Renderer на этом объекте.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Плесневой вид")]
    [Tooltip("Полный материал плесневого продукта. Приоритетнее текстуры.")]
    [SerializeField] private Material moldyMaterial;

    [Tooltip("Только текстура плесени — подставляется в копию текущего материала.")]
    [SerializeField] private Texture2D moldyTexture;

    private Renderer[] resolvedRenderers;
    private Material[] cleanMaterialInstances;
    private Material[] moldyMaterialInstances;
    private bool isInitialized;

    public bool HasMoldyVisualConfigured => moldyMaterial != null || moldyTexture != null;

    public void SetMoldyVisual(bool isMoldy)
    {
        if (!EnsureInitialized())
            return;

        if (isMoldy && !HasMoldyVisualConfigured)
        {
            Debug.LogWarning(
                $"{name}: плесень назначена логикой, но на MoldVisual не задан moldyMaterial или moldyTexture.",
                this);
            return;
        }

        for (int i = 0; i < resolvedRenderers.Length; i++)
        {
            Renderer renderer = resolvedRenderers[i];
            if (renderer == null)
                continue;

            if (isMoldy)
                renderer.material = moldyMaterialInstances[i];
            else
                renderer.material = cleanMaterialInstances[i];
        }
    }

    public static MoldVisual GetOrCreate(IngredientInstance ingredient)
    {
        if (ingredient == null)
            return null;

        MoldVisual existing = ingredient.GetComponent<MoldVisual>();
        if (existing != null)
            return existing;

        Debug.LogWarning(
            $"{ingredient.name}: на префабе нет MoldVisual. Добавь компонент и назначь moldy-текстуру/материал.",
            ingredient);
        return ingredient.gameObject.AddComponent<MoldVisual>();
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return resolvedRenderers != null && resolvedRenderers.Length > 0;

        resolvedRenderers = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : GetComponentsInChildren<Renderer>(true);

        if (resolvedRenderers == null || resolvedRenderers.Length == 0)
        {
            Debug.LogWarning($"{name}: не найдены Renderer для смены текстуры плесени.", this);
            return false;
        }

        cleanMaterialInstances = new Material[resolvedRenderers.Length];
        moldyMaterialInstances = new Material[resolvedRenderers.Length];

        for (int i = 0; i < resolvedRenderers.Length; i++)
        {
            Renderer renderer = resolvedRenderers[i];
            if (renderer == null)
                continue;

            cleanMaterialInstances[i] = renderer.material;
            moldyMaterialInstances[i] = CreateMoldyMaterialInstance(cleanMaterialInstances[i]);
        }

        isInitialized = true;
        return true;
    }

    private Material CreateMoldyMaterialInstance(Material cleanMaterial)
    {
        if (moldyMaterial != null)
            return new Material(moldyMaterial);

        if (moldyTexture == null || cleanMaterial == null)
            return null;

        Material instance = new Material(cleanMaterial);
        if (instance.HasProperty(BaseMapId))
            instance.SetTexture(BaseMapId, moldyTexture);
        else if (instance.HasProperty(MainTexId))
            instance.SetTexture(MainTexId, moldyTexture);

        return instance;
    }

    private void OnDestroy()
    {
        DestroyMaterialInstances(cleanMaterialInstances);
        DestroyMaterialInstances(moldyMaterialInstances);
    }

    private static void DestroyMaterialInstances(Material[] materials)
    {
        if (materials == null)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
                Destroy(materials[i]);
        }
    }
}
