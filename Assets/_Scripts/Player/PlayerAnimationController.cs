using UnityEngine;

/// <summary>
/// Назначение: отдельный слой связи между игровым кодом игрока и Animator.
/// Что делает: обновляет параметры движения, запускает атаку и смерть, но не принимает боевых решений.
/// Связи: читает состояние из PlayerStats и InputManager, анимационные события приходят через PlayerAttackAnimationEvents.
/// Паттерны: Single Responsibility, Adapter между gameplay и Animator.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    /// <summary>
    /// Тип анимации атаки, который будет выставлен в параметр AttackType у Animator.
    /// Значения enum специально совпадают с числами, которые удобно использовать в переходах Animator:
    /// 0 = ближняя атака, 1 = дальняя атака.
    /// </summary>
    public enum AttackAnimationType
    {
        Melee = 0,
        Ranged = 1
    }

    [Header("Связи")]
    [Tooltip("Animator на визуальной модели игрока.")]
    [SerializeField] private Animator animator;

    [Tooltip("Статы игрока. Нужны, чтобы корректно реагировать на смерть и не обновлять лишние параметры после неё.")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Параметры Animator")]
    [Tooltip("Имя float-параметра скорости движения в Animator Controller.")]
    [SerializeField] private string moveSpeedParameter = "MoveSpeed";

    [Tooltip("Имя trigger-параметра атаки в Animator Controller.")]
    [SerializeField] private string attackTriggerParameter = "Attack";

    [Tooltip("Имя int-параметра типа атаки в Animator Controller. Через него Animator решает, какой attack-state запускать: melee или ranged.")]
    [SerializeField] private string attackTypeParameter = "AttackType";

    [Tooltip("Имя bool-параметра смерти в Animator Controller.")]
    [SerializeField] private string isDeadParameter = "IsDead";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (playerStats != null)
            playerStats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (animator == null)
            return;

        if (playerStats != null && playerStats.IsDead)
        {
            animator.SetFloat(moveSpeedParameter, 0f);
            return;
        }

        if (InputManager.Instance == null)
            return;

        // Для урока берём самую простую и наблюдаемую метрику:
        // если игрок двигает стик/клавиши сильнее, MoveSpeed становится больше,
        // и Animator сам переключает Idle <-> Move по условиям переходов.
        float moveSpeed = InputManager.Instance.MoveInput.magnitude;
        animator.SetFloat(moveSpeedParameter, moveSpeed);
    }

    /// <summary>
    /// Запускает анимацию атаки выбранного типа.
    /// Сначала выставляет параметр AttackType, чтобы Animator выбрал правильный state,
    /// и только потом дёргает общий trigger Attack.
    /// </summary>
    public void PlayAttack(AttackAnimationType attackAnimationType)
    {
        if (animator == null)
            return;

        // Важно для вашей схемы Animator:
        // AttackType решает, в какой state идти (Attack_Melee или Attack_Ranged),
        // а trigger Attack отвечает только за сам факт запуска атаки.
        animator.SetInteger(attackTypeParameter, (int)attackAnimationType);
        animator.ResetTrigger(attackTriggerParameter);
        animator.SetTrigger(attackTriggerParameter);
    }

    /// <summary>
    /// Запускает состояние смерти в Animator.
    /// </summary>
    public void PlayDeath()
    {
        if (animator == null)
            return;

        animator.SetBool(isDeadParameter, true);
    }

    private void HandleDeath()
    {
        PlayDeath();
    }
}
