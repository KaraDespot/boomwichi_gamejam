using UnityEngine;

/// <summary>
/// Назначение: мост между Animation Event и боевой логикой игрока.
/// Что делает: принимает вызовы из клипа и передаёт их в PlayerCombatController.
/// Связи: висит рядом с Animator на visual-объекте и знает только о PlayerCombatController.
/// Паттерны: Adapter / Bridge для Animation Event.
/// </summary>
public class PlayerAttackAnimationEvents : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Боевой контроллер игрока, который обрабатывает фазы атаки.")]
    [SerializeField] private PlayerCombatController playerCombatController;

    private void Awake()
    {
        if (playerCombatController == null)
            playerCombatController = GetComponentInParent<PlayerCombatController>();
    }

    /// <summary>
    /// Вызывается из Animation Event в кадр действия атаки.
    /// Для melee это удар, для ranged — выстрел/спавн projectile.
    /// </summary>
    public void OnAttackActionAnimationEvent()
    {
        playerCombatController?.HandleAttackActionAnimationEvent();
    }

    /// <summary>
    /// Вызывается из Animation Event в конце анимации атаки.
    /// </summary>
    public void OnAttackFinishedAnimationEvent()
    {
        playerCombatController?.HandleAttackFinishedAnimationEvent();
    }
}
