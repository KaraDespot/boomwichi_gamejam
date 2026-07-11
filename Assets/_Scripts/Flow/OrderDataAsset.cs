/*
 * OrderDataAsset
 * Назначение: ScriptableObject с описанием одного заказа клиента.
 * Что делает: хранит фразу, соус, ингредиенты и прожарку — создаётся через Create > Boomwichi > Order Data.
 * Связи: OrderManager, CustomerUI, будущий OrderEvaluator (Dev 1).
 * Паттерны: ScriptableObject, Data Asset.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewOrder", menuName = "Boomwichi/Order Data")]
public class OrderDataAsset : ScriptableObject
{
    [Header("Клиент")]
    [TextArea(2, 5)]
    [Tooltip("Фраза клиента, показывается один раз в облаке заказа.")]
    [SerializeField] private string customerPhrase;

    [Header("Соус")]
    [Tooltip("Нужен ли соус в заказе.")]
    [SerializeField] private bool requiresSauce;

    [Header("Ингредиенты")]
    [Tooltip("Список ингредиентов и их количество. Хлеб (нижний/верхний) добавляется автоматически.")]
    [SerializeField] private OrderIngredientRequirement[] ingredients;

    [Header("Прожарка")]
    [Tooltip("Каким должен быть верхний хлеб после гриля.")]
    [SerializeField] private ToastState requiredTopToast = ToastState.Toasted;

    public string CustomerPhrase => customerPhrase;
    public bool RequiresSauce => requiresSauce;
    public OrderIngredientRequirement[] Ingredients => ingredients;
    public ToastState RequiredTopToast => requiredTopToast;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ingredients == null)
            return;

        for (int i = 0; i < ingredients.Length; i++)
        {
            if (ingredients[i].count < 1)
            {
                OrderIngredientRequirement fixedEntry = ingredients[i];
                fixedEntry.count = 1;
                ingredients[i] = fixedEntry;
            }
        }
    }
#endif
}
