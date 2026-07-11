using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// GameLoopFlowController
/// Что делает: связывает lose/win UI с состояниями игры, checkpoint-сохранением конца уровня, restart через checkpoint и переходом дальше.
/// Зачем нужен в игре: уровень должен завершаться предсказуемо: смерть показывает lose, выход после encounter показывает win.
/// Связи: PlayerStats, EventBus, GameManager.RestartFromCheckpointOrScene, ExitWinTrigger и кнопки lose/win экранов.
/// Как используется: scene object подписывается на смерть игрока, принимает запрос победы от trigger выхода и перенаправляет Restart в GameManager.
/// Расширения: разные условия победы, отдельный экран выбора следующего уровня, расширенный win summary.
/// Совет: если win не срабатывает, проверить active state выхода, requiredEncounterIdForWin и ссылки UI в Inspector.
/// Совет: если checkpoint не пишется, проверить активный слот GameManager, fallback checkpointSlotIndex и логи CheckpointSaveSystem.
/// </summary>
public class GameLoopFlowController : MonoBehaviour
{
    [Header("UI поражения")]
    [Tooltip("Панель поражения в сцене или prefab-инстанс.")]
    [SerializeField] private GameObject losePanel;
    [Tooltip("Кнопка перезапуска текущей gameplay-сцены.")]
    [SerializeField] private Button loseRestartButton;
    [Tooltip("Кнопка выхода в главное меню.")]
    [SerializeField] private Button loseMenuButton;

    [Header("UI победы")]
    [Tooltip("Панель победы в сцене или prefab-инстанс.")]
    [SerializeField] private GameObject winPanel;
    [Tooltip("Кнопка выхода в главное меню.")]
    [SerializeField] private Button winMenuButton;

    [FormerlySerializedAs("winNextWaveButton")]
    [Tooltip("Кнопка перехода на следующий уровень.")]
    [SerializeField] private Button winNextLevelButton;

    [Header("Общий UI")]
    [Tooltip("Панель паузы, которую нужно скрыть при win/lose.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Условие победы")]
    [Tooltip("Объект выхода, который становится активным после завершения encounter.")]
    [SerializeField] private GameObject exitActivationObjectOverride;

    [Tooltip("ID обязательного encounter для победы через выход. Если пусто, фильтр по ID отключён.")]
    [SerializeField] private string requiredEncounterIdForWin = string.Empty;

    [Header("Checkpoint-сохранение")]
    [Tooltip("Слот checkpoint-сохранения для выхода уровня: 0, 1 или 2.")]
    [SerializeField] private int checkpointSlotIndex = 0;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private PlayerStats playerStats;
    private bool flowFinished;
    private bool isEncounterCompletedForWin;

    // Legacy-событие оставлено для обратной совместимости старых учебных подписок.
    public event Action OnNextWaveRequested;

    private void Awake()
    {
        ResolvePlayerStats();
        ValidateReferences();
        ValidateCheckpointSlotIndex();
        HideAllScreens();
    }

    /// <summary>
    /// Входные условия: объект flow активен, EventBus/GameManager уже созданы Bootstrap-сценой.
    /// Шаги: подписаться на смерть игрока, событие завершения encounter и кнопки UI.
    /// Типичные поломки: PlayerStats не найден, EventBus отсутствует, кнопки не назначены в Inspector.
    /// Что проверить: Console warnings и ссылки lose/win кнопок на объекте GameLoopFlowController.
    /// </summary>
    private void OnEnable()
    {
        SubscribeToPlayerDeath();
        SubscribeToEncounterCompleted();
        BindButtons();
    }

    /// <summary>
    /// Входные условия: объект выключается или сцена выгружается.
    /// Шаги: снять подписки со смерти игрока, EventBus и кнопок UI.
    /// Типичные поломки: двойные клики/двойные события после перезагрузки сцены означают пропущенную отписку.
    /// Что проверить: Console на повторные логи win/lose и количество GameLoopFlowController в сцене.
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        UnsubscribeFromEncounterCompleted();
        UnbindButtons();
    }

    /// <summary>
    /// Контракт: вызывать только из trigger завершения уровня после входа игрока.
    /// Метод проверяет win-условия, сохраняет checkpoint уровня через GameManager и только потом показывает win.
    /// Win не показывается, если save не прошёл: для урока 13 файл JSON является частью smoke-проверки.
    /// Не сохраняет активную волну: если encounter ещё не завершён, метод вернёт false.
    /// Почему так: конец уровня является безопасной точкой, а не случайным моментом внутри боя.
    /// Потенциальное применение: Continue сможет открыть следующий уровень по данным save.
    /// </summary>
    public bool RequestWinFromExit(Vector3 levelExitPosition)
    {
        if (!CanTriggerWin())
            return false;

        if (exitActivationObjectOverride != null && !exitActivationObjectOverride.activeInHierarchy)
            return false;

        if (!CanWinByEncounterRule())
            return false;

        if (GameManager.Instance != null)
        {
            bool checkpointSaved = GameManager.Instance.TrySaveLevelCheckpointProgress(checkpointSlotIndex, levelExitPosition);
            if (!checkpointSaved)
            {
                Debug.LogWarning(
                    $"{name}: win остановлен, потому что checkpoint не сохранён в слот {checkpointSlotIndex}. " +
                    "Проверить PlayerStats/PlayerProgression/WeaponManager на игроке, ошибки Save Game Free, " +
                    "а также лог Checkpoint JSON written с путём к Application.persistentDataPath.",
                    this);
                return false;
            }
        }

        TriggerWin();
        return true;
    }

    private void ResolvePlayerStats()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void ValidateReferences()
    {
        if (losePanel == null)
            Debug.LogError($"{name}: losePanel is not assigned.", this);

        if (loseRestartButton == null)
            Debug.LogError($"{name}: loseRestartButton is not assigned.", this);

        if (loseMenuButton == null)
            Debug.LogError($"{name}: loseMenuButton is not assigned.", this);

        if (winPanel == null)
            Debug.LogError($"{name}: winPanel is not assigned.", this);

        if (winMenuButton == null)
            Debug.LogError($"{name}: winMenuButton is not assigned.", this);

        if (winNextLevelButton == null)
            Debug.LogError($"{name}: winNextLevelButton is not assigned.", this);

        if (pausePanel == null && showDebugLogs)
            Debug.LogWarning($"{name}: pausePanel is not assigned. Pause UI will not be hidden on lose/win.", this);

        if (exitActivationObjectOverride == null && showDebugLogs)
            Debug.LogWarning($"{name}: exitActivationObjectOverride is not assigned. Win can still be requested by trigger.", this);
    }

    private void ValidateCheckpointSlotIndex()
    {
        if (checkpointSlotIndex >= 0 && checkpointSlotIndex < CheckpointSaveSystem.SlotCount)
            return;

        Debug.LogError(
            $"{name}: checkpointSlotIndex={checkpointSlotIndex} вне диапазона 0..{CheckpointSaveSystem.SlotCount - 1}. " +
            "Исправьте значение в Inspector. Для предсказуемого teacher repo в runtime будет использован слот 0.",
            this);

        checkpointSlotIndex = 0;
    }

    private void BindButtons()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.AddListener(HandleLoseRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.AddListener(HandleWinNextLevelClicked);
    }

    private void UnbindButtons()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.RemoveListener(HandleLoseRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.RemoveListener(HandleWinNextLevelClicked);
    }

    private void SubscribeToPlayerDeath()
    {
        if (playerStats == null)
            ResolvePlayerStats();

        if (playerStats == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: PlayerStats not found. Lose on death is disabled.", this);

            return;
        }

        playerStats.OnDeath += HandlePlayerDeath;
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (playerStats != null)
            playerStats.OnDeath -= HandlePlayerDeath;
    }

    private void SubscribeToEncounterCompleted()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnEncounterCompleted += HandleEncounterCompleted;
    }

    private void UnsubscribeFromEncounterCompleted()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnEncounterCompleted -= HandleEncounterCompleted;
    }

    /// <summary>
    /// Отмечает completion encounter, который разрешает победу через выход.
    /// </summary>
    private void HandleEncounterCompleted(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(requiredEncounterIdForWin))
        {
            isEncounterCompletedForWin = true;
            if (showDebugLogs)
                Debug.Log($"{name}: encounter '{encounterId}' завершён. Фильтр ID отключён, win-разрешение обновлено.", this);
            return;
        }

        if (string.Equals(requiredEncounterIdForWin, encounterId, StringComparison.Ordinal))
        {
            isEncounterCompletedForWin = true;
            if (showDebugLogs)
                Debug.Log($"{name}: encounter '{encounterId}' совпал с requiredEncounterIdForWin. Win-разрешение обновлено.", this);
        }
    }

    private bool CanTriggerWin()
    {
        if (flowFinished)
            return false;

        if (GameManager.Instance == null)
            return false;

        return GameManager.Instance.CurrentState == GameState.Playing;
    }

    private bool CanWinByEncounterRule()
    {
        if (string.IsNullOrWhiteSpace(requiredEncounterIdForWin))
            return true;

        if (isEncounterCompletedForWin)
            return true;

        if (showDebugLogs)
            Debug.Log($"{name}: win отклонён. Encounter '{requiredEncounterIdForWin}' ещё не завершён.", this);

        return false;
    }

    private void HandlePlayerDeath()
    {
        TriggerLose();
    }

    private void TriggerLose()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterLoseState();

        if (losePanel != null)
            losePanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: lose state triggered, but losePanel is not assigned.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: lose screen shown.", this);
    }

    private void TriggerWin()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterWinState();

        if (winPanel != null)
            winPanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: win state triggered, but winPanel is not assigned.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: win screen shown.", this);
    }

    private void HidePausePanelIfAssigned()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void HideAllScreens()
    {
        if (losePanel != null)
            losePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void HandleLoseRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartFromCheckpointOrScene();
    }

    private void HandleMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }

    /// <summary>
    /// Обработчик кнопки перехода на следующий уровень с win-экрана.
    /// </summary>
    private void HandleWinNextLevelClicked()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.TryLoadNextLevel())
                return;

            // Если следующего уровня нет, завершаем прогон и уходим в меню.
            GameManager.Instance.GoToMenu();
            return;
        }

        if (OnNextWaveRequested != null)
        {
            OnNextWaveRequested.Invoke();
            return;
        }

        Debug.LogWarning($"{name}: GameManager is missing. Next level flow cannot continue.", this);
    }
}
