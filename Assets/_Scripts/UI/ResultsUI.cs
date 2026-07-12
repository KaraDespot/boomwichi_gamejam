/*
 * ResultsUI
 * Назначение: финальный экран статистики после завершения смены.
 * Что делает: показывает чаевые, заказы и хаос-метрики; даёт Restart Day и Main Menu.
 * Связи: EventBus.OnDayFinished, ScoreManager, OrderManager, GameManager.
 * Паттерны: UI Controller, Observer.
 *
 * Настройка в сцене:
 * - Создай панель ResultsPanel на Canvas (по умолчанию выключена).
 * - Назначь resultsPanel, titleText, statsText, restartDayButton, mainMenuButton.
 * - Компонент ResultsUI может висеть на UIController или на самой панели.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ResultsUI : MonoBehaviour
{
    [Header("Панель")]
    [Tooltip("Корневой объект финального экрана. Должен быть выключен в сцене до конца смены.")]
    [SerializeField] private GameObject resultsPanel;

    [Header("Текст")]
    [Tooltip("Заголовок экрана. Если пусто — подставится titleLabel.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Многострочная статистика смены.")]
    [SerializeField] private TMP_Text statsText;

    [Header("Кнопки")]
    [Tooltip("Перезапуск GameScene / новая смена.")]
    [SerializeField] private Button restartDayButton;

    [Tooltip("Возврат в главное меню.")]
    [SerializeField] private Button mainMenuButton;

    [Header("Заголовок")]
    [Tooltip("Текст заголовка, если titleText не задан отдельно в сцене.")]
    [SerializeField] private string titleLabel = "Смена окончена";

    private bool isVisible;

    private void Awake()
    {
        ValidateReferences();
        HidePanel();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayFinished += HandleDayFinished;
            EventBus.Instance.OnDayStateChanged += HandleDayStateChanged;
        }

        if (restartDayButton != null)
            restartDayButton.onClick.AddListener(HandleRestartDayClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentDayState == DayFlowState.DayFinished)
        {
            ShowResults();
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDayFinished -= HandleDayFinished;
            EventBus.Instance.OnDayStateChanged -= HandleDayStateChanged;
        }

        if (restartDayButton != null)
            restartDayButton.onClick.RemoveListener(HandleRestartDayClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateReferences();
    }
#endif

    private void HandleDayFinished()
    {
        ShowResults();
    }

    private void HandleDayStateChanged(DayFlowState state)
    {
        if (state == DayFlowState.DayFinished)
            ShowResults();
    }

    private void ShowResults()
    {
        if (isVisible)
            return;

        if (resultsPanel == null)
        {
            Debug.LogError($"{name}: resultsPanel не назначен — финальный экран не показан.", this);
            return;
        }

        isVisible = true;
        RefreshStats();
        resultsPanel.SetActive(true);
    }

    private void HidePanel()
    {
        isVisible = false;

        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    private void RefreshStats()
    {
        if (titleText != null)
            titleText.text = titleLabel;

        if (statsText == null)
            return;

        ScoreManager scoreManager = ScoreManager.Instance;
        OrderManager orderManager = FindFirstObjectByType<OrderManager>();

        int totalTips = scoreManager != null ? scoreManager.TotalTips : 0;
        int completedOrders = scoreManager != null ? scoreManager.CompletedOrders : 0;
        int failedOrders = scoreManager != null ? scoreManager.FailedOrders : 0;
        int totalOrders = orderManager != null ? orderManager.TotalOrders : completedOrders + failedOrders;
        int cockroachesSquashed = scoreManager != null ? scoreManager.CockroachesSquashed : 0;
        int moldCleaned = scoreManager != null ? scoreManager.MoldCleaned : 0;
        int dirtyIncidents = scoreManager != null ? scoreManager.DirtyIncidents : 0;

        statsText.text =
            $"Чаевые: ${totalTips}\n\n" +
            $"Заказы: {completedOrders} из {totalOrders} сданы\n" +
            $"Провалено: {failedOrders}\n\n" +
            $"Тараканов раздавлено: {cockroachesSquashed}\n" +
            $"Плесень очищена: {moldCleaned}\n" +
            $"Грязных сендвичей: {dirtyIncidents}";
    }

    private void HandleRestartDayClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGameScene();
    }

    private void HandleMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }

    private void ValidateReferences()
    {
        if (resultsPanel == null)
            Debug.LogWarning($"{name}: назначь resultsPanel в Inspector.", this);

        if (statsText == null)
            Debug.LogWarning($"{name}: назначь statsText (TextMeshPro) в Inspector.", this);

        if (restartDayButton == null)
            Debug.LogWarning($"{name}: назначь restartDayButton в Inspector.", this);

        if (mainMenuButton == null)
            Debug.LogWarning($"{name}: назначь mainMenuButton в Inspector.", this);
    }
}
