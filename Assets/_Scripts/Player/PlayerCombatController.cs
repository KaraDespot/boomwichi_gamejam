using UnityEngine;

/// <summary>
/// Назначение: отдельная точка боевой логики игрока.
/// Что делает: принимает запрос на атаку, проверяет условия, запускает анимацию и обрабатывает Animation Event.
/// Связи: использует PlayerStats, WeaponManager и PlayerAnimationController. Через этот класс позже удобно подключать звук, VFX и shake камеры.
/// Паттерны: Single Responsibility, Orchestrator для боевого цикла игрока.
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Статы игрока. Нужны для проверки смерти и блокировки атаки.")]
    [SerializeField] private PlayerStats playerStats;

    [Tooltip("Менеджер оружия игрока. Через него берётся текущее оружие и выполняется его Attack().")]
    [SerializeField] private WeaponManager weaponManager;

    [Tooltip("Контроллер анимации игрока. Он только запускает анимационные состояния.")]
    [SerializeField] private PlayerAnimationController playerAnimationController;

    [Header("Будущие звук и эффекты")]
    [Tooltip("Источник звука для будущих SFX атаки. Пока это болванка под дальнейшее расширение уроков.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Точка, из которой удобно выпускать визуальные эффекты атаки: вспышку, след, частицы, muzzle flash.")]
    [SerializeField] private Transform attackEffectPoint;

    [Tooltip("Заготовка эффекта старта атаки. Например, лёгкая вспышка, след меча или эффект натяжения тетивы.")]
    [SerializeField] private GameObject attackStartEffectPrefab;

    [Tooltip("Заготовка эффекта в момент действия атаки. Для melee это может быть вспышка удара, для ranged — вспышка выстрела.")]
    [SerializeField] private GameObject attackActionEffectPrefab;

    [Header("Отладка")]
    [Tooltip("Если включено, контроллер будет писать в Console подробные сообщения о фазах атаки.")]
    [SerializeField] private bool enableDebugLogs;

    private bool isAttackInProgress;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (weaponManager == null)
            weaponManager = GetComponent<WeaponManager>();

        if (playerAnimationController == null)
            playerAnimationController = GetComponentInChildren<PlayerAnimationController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Пытается начать атаку игрока.
    /// На этом шаге мы не наносим урон и не спавним projectile:
    /// мы только запускаем красивую и предсказуемую анимацию.
    /// </summary>
    public bool TryStartAttack()
    {
        if (!CanStartAttack())
            return false;

        isAttackInProgress = true;

        PlayerAnimationController.AttackAnimationType attackAnimationType = ResolveAttackAnimationType();

        if (enableDebugLogs)
            Debug.Log($"{name}: атака запущена, тип анимации = {attackAnimationType}, ждём Animation Event.", this);

        PlayAttackStartEffects();
        playerAnimationController.PlayAttack(attackAnimationType);
        return true;
    }

    /// <summary>
    /// Вызывается из Animation Event в тот кадр, когда attack-действие должно реально произойти.
    /// Для melee это обычно момент удара.
    /// Для ranged это обычно момент выстрела или спавна projectile.
    /// </summary>
    public void HandleAttackActionAnimationEvent()
    {
        if (!isAttackInProgress)
            return;

        if (weaponManager == null)
            return;

        weaponManager.PerformCurrentWeaponAttack();
        PlayAttackActionEffects();

        if (enableDebugLogs)
            Debug.Log($"{name}: сработал Animation Event действия атаки.", this);
    }

    /// <summary>
    /// Вызывается из Animation Event в конце клипа атаки.
    /// Снимает внутреннюю блокировку и позволяет начать следующую атаку.
    /// </summary>
    public void HandleAttackFinishedAnimationEvent()
    {
        isAttackInProgress = false;

        if (enableDebugLogs)
            Debug.Log($"{name}: анимация атаки завершена.", this);
    }

    private bool CanStartAttack()
    {
        if (playerStats == null || weaponManager == null || playerAnimationController == null)
            return false;

        if (playerStats.IsDead)
            return false;

        if (isAttackInProgress)
            return false;

        if (weaponManager.CurrentWeapon == null)
            return false;

        return weaponManager.CurrentWeapon.CanAttack();
    }

    /// <summary>
    /// Определяет, какую анимацию атаки нужно запускать для текущего оружия.
    /// Сейчас правило очень простое и хорошо читается на уроке:
    /// если текущее оружие дальнее, играем ranged-анимацию,
    /// во всех остальных случаях — melee-анимацию.
    /// </summary>
    private PlayerAnimationController.AttackAnimationType ResolveAttackAnimationType()
    {
        if (weaponManager == null || weaponManager.CurrentWeapon == null)
            return PlayerAnimationController.AttackAnimationType.Melee;

        // Это удобная учебная точка расширения.
        // Позже, если появятся:
        // - тяжёлая атака,
        // - магия,
        // - комбо,
        // - специальный выстрел,
        // здесь можно будет сделать более богатое правило выбора анимации,
        // не трогая WeaponManager и не засоряя PlayerAnimationController боевой логикой.
        if (weaponManager.CurrentWeapon is RangedWeapon)
            return PlayerAnimationController.AttackAnimationType.Ranged;

        return PlayerAnimationController.AttackAnimationType.Melee;
    }

    /// <summary>
    /// Болванка под стартовые эффекты атаки.
    /// Сюда позже удобно добавлять:
    /// - звук замаха мечом;
    /// - натяжение лука;
    /// - включение trail renderer;
    /// - лёгкую вспышку/частицы у оружия.
    /// </summary>
    private void PlayAttackStartEffects()
    {
        if (attackStartEffectPrefab != null && attackEffectPoint != null)
            Instantiate(attackStartEffectPrefab, attackEffectPoint.position, attackEffectPoint.rotation);

        if (audioSource != null)
        {
            // Здесь позже можно вызывать:
            // audioSource.PlayOneShot(attackStartClip);
            // Пока оставляем только точку расширения, чтобы не перегружать урок.
        }
    }

    /// <summary>
    /// Болванка под эффекты в кадр реального действия атаки.
    /// Самая удобная точка для будущего расширения:
    /// - звук попадания/выстрела;
    /// - muzzle flash;
    /// - частицы удара;
    /// - shake камеры;
    /// - вспышка на оружии;
    /// - spawn дополнительных VFX, если они должны совпасть с gameplay.
    /// </summary>
    private void PlayAttackActionEffects()
    {
        if (attackActionEffectPrefab != null && attackEffectPoint != null)
            Instantiate(attackActionEffectPrefab, attackEffectPoint.position, attackEffectPoint.rotation);

        if (audioSource != null)
        {
            // Здесь позже можно вызывать:
            // audioSource.PlayOneShot(attackActionClip);
            // Для ranged сюда обычно кладут звук выстрела.
            // Для melee — звук контакта или "whoosh", если он должен совпасть именно с кадром удара.
        }
    }
}
