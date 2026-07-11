/*
 * GameplayHUDController
 * Назначение: отображать боевой HUD игрока (HP, Mana, XP/Level, иконка оружия).
 * Что делает: подписывается на события PlayerStats/PlayerProgression/WeaponManager и обновляет UI;
 *             при включении выполняет RefreshAll(), чтобы HUD сразу показал актуальные значения.
 * Связи: PlayerStats, PlayerProgression, WeaponManager, Unity UI (Image/Text).
 * Паттерны: Presenter для UI.
 */
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD-презентер: связывает runtime-данные игрока с UI-элементами.
/// </summary>
public class GameplayHUDController : MonoBehaviour
{
    [Header("Источники данных")]
    [Tooltip("Ссылка на PlayerStats. Если не назначена, будет найден первый объект на сцене.")]
    [SerializeField] private PlayerStats playerStats;

    [Tooltip("Ссылка на PlayerProgression. Если не назначена, будет найден первый объект на сцене.")]
    [SerializeField] private PlayerProgression playerProgression;

    [Tooltip("Ссылка на WeaponManager. Если не назначена, будет найден первый объект на сцене.")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Text hpValueText;

    [Header("Мана")]
    [SerializeField] private Image manaFillImage;

    [Header("XP и уровень")]
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Text levelValueText;

    [Header("Оружие")]
    [SerializeField] private Image weaponIconImage;

    private bool isBound;
    private bool warnedAboutUiRefs;
    private bool warnedAboutMissingSources;
    private bool warnedAboutMissingPlayerData;

    private void Awake()
    {
        ValidateHudMultiplicity();
        ResolveSourcesIfNeeded();
        ValidateUiReferencesOnce();
    }

    private void OnEnable()
    {
        Bind();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unbind();
    }

    /// <summary>
    /// Пытается найти источники данных, если ссылки не назначены в инспекторе.
    /// </summary>
    private void ResolveSourcesIfNeeded()
    {
        if (playerStats == null)
            playerStats = FindSingleOrFirst<PlayerStats>("PlayerStats");

        if (playerProgression == null && playerStats != null)
            playerProgression = playerStats.GetComponent<PlayerProgression>();

        if (playerProgression == null)
            playerProgression = FindSingleOrFirst<PlayerProgression>("PlayerProgression");

        if (weaponManager == null && playerStats != null)
            weaponManager = playerStats.GetComponent<WeaponManager>();

        if (weaponManager == null)
            weaponManager = FindSingleOrFirst<WeaponManager>("WeaponManager");
    }

    /// <summary>
    /// Подписывает HUD на события источников данных.
    /// </summary>
    private void Bind()
    {
        if (isBound)
            Unbind();

        ResolveSourcesIfNeeded();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += HandleHealthChanged;
            playerStats.OnManaChanged += HandleManaChanged;
        }

        if (playerProgression != null)
        {
            playerProgression.OnExperienceChanged += HandleExperienceChanged;
            playerProgression.OnLevelUp += HandleLevelChanged;
            playerProgression.OnLevelChanged += HandleLevelChanged;
        }

        if (weaponManager != null)
            weaponManager.OnWeaponChanged += HandleWeaponChanged;

        isBound = true;
    }

    /// <summary>
    /// Отписывает HUD от всех событий.
    /// </summary>
    private void Unbind()
    {
        if (!isBound)
            return;

        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= HandleHealthChanged;
            playerStats.OnManaChanged -= HandleManaChanged;
        }

        if (playerProgression != null)
        {
            playerProgression.OnExperienceChanged -= HandleExperienceChanged;
            playerProgression.OnLevelUp -= HandleLevelChanged;
            playerProgression.OnLevelChanged -= HandleLevelChanged;
        }

        if (weaponManager != null)
            weaponManager.OnWeaponChanged -= HandleWeaponChanged;

        isBound = false;
    }

    /// <summary>
    /// Мгновенно синхронизирует HUD с текущими runtime-значениями.
    /// Нужен, чтобы не ждать следующего события после загрузки сцены.
    /// </summary>
    private void RefreshAll()
    {
        if (playerStats == null || playerProgression == null || weaponManager == null)
        {
            WarnMissingSourcesOnce();
            return;
        }

        if (playerStats.playerData == null)
        {
            if (!warnedAboutMissingPlayerData)
            {
                warnedAboutMissingPlayerData = true;
                Debug.LogWarning(
                    "GameplayHUDController: у PlayerStats не назначен PlayerData. " +
                    "Проверьте ссылку PlayerData на объекте игрока.",
                    this);
            }

            return;
        }

        HandleHealthChanged(playerStats.CurrentHealth, playerStats.playerData.maxHealth);
        HandleManaChanged(playerStats.CurrentMana, playerStats.playerData.maxMana);
        HandleLevelChanged(playerProgression.CurrentLevel);
        HandleExperienceChanged(playerProgression.CurrentExperience, playerProgression.RequiredExperienceForNextLevel);
        HandleWeaponChanged(weaponManager.CurrentWeapon);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (hpFillImage != null)
            hpFillImage.fillAmount = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;

        if (hpValueText != null)
            hpValueText.text = Mathf.CeilToInt(current).ToString();
    }

    private void HandleManaChanged(float current, float max)
    {
        if (manaFillImage != null)
            manaFillImage.fillAmount = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void HandleExperienceChanged(float current, float required)
    {
        if (xpFillImage != null)
            xpFillImage.fillAmount = required > 0.01f ? Mathf.Clamp01(current / required) : 0f;
    }

    private void HandleLevelChanged(int level)
    {
        if (levelValueText != null)
            levelValueText.text = level.ToString();
    }

    private void HandleWeaponChanged(WeaponBase weapon)
    {
        if (weaponIconImage == null)
            return;

        Sprite icon = weapon != null && weapon.WeaponData != null
            ? weapon.WeaponData.icon
            : null;

        weaponIconImage.enabled = icon != null;
        weaponIconImage.sprite = icon;
    }

    /// <summary>
    /// Проверяет, что на сцене активен только один HUD-контроллер.
    /// Это частая учебная ошибка: случайно оставить второй HUDCanvas и получить дублирование UI.
    /// </summary>
    private void ValidateHudMultiplicity()
    {
        GameplayHUDController[] hudControllers = FindObjectsByType<GameplayHUDController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        if (hudControllers.Length > 1)
        {
            Debug.LogWarning(
                "GameplayHUDController: на сцене найдено несколько HUD-контроллеров. " +
                "Проверьте, что активен только один HUDCanvas для gameplay-сцены.",
                this);
        }
    }

    /// <summary>
    /// Один раз проверяет, что все ключевые UI-ссылки назначены в инспекторе.
    /// Не спамит консоль каждый кадр.
    /// </summary>
    private void ValidateUiReferencesOnce()
    {
        if (warnedAboutUiRefs)
            return;

        warnedAboutUiRefs = true;

        if (hpFillImage == null || hpValueText == null || manaFillImage == null ||
            xpFillImage == null || levelValueText == null || weaponIconImage == null)
        {
            Debug.LogWarning(
                "GameplayHUDController: не все UI-ссылки назначены. " +
                "Проверьте поля HP/Mana/XP/Level/Weapon в инспекторе HUDCanvas.",
                this);
        }
    }

    /// <summary>
    /// Один раз предупреждает, если не найдены источники данных игрока.
    /// Нужен для безопасной диагностики, когда HUD есть, а Player-компоненты на сцене отсутствуют.
    /// </summary>
    private void WarnMissingSourcesOnce()
    {
        if (warnedAboutMissingSources)
            return;

        warnedAboutMissingSources = true;
        Debug.LogWarning(
            "GameplayHUDController: не найдены все источники данных игрока (PlayerStats / PlayerProgression / WeaponManager). " +
            "Назначьте ссылки вручную в инспекторе HUDCanvas или проверьте, что на сцене есть объект Player с этими компонентами.",
            this);
    }

    /// <summary>
    /// Находит компонент типа T на сцене.
    /// Если компонентов несколько, предупреждает и выбирает первый для предсказуемого поведения.
    /// </summary>
    private T FindSingleOrFirst<T>(string componentName) where T : Object
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (components == null || components.Length == 0)
            return null;

        if (components.Length > 1)
        {
            Debug.LogWarning(
                $"GameplayHUDController: найдено несколько компонентов {componentName}. " +
                $"Будет использован первый: {components[0].name}. " +
                "Для предсказуемого поведения назначьте ссылку вручную в инспекторе.",
                this);
        }

        return components[0];
    }
}
