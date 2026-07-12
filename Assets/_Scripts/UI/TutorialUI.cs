/*
 * TutorialUI
 * Назначение: экран туториала перед началом игрового дня.
 * Что делает: показывает краткие правила и запускает день по кнопке.
 * Связи: GameManager, DayTimer, EventBus.
 * Паттерны: UI Controller, Observer.
 *
 * Настройка в сцене:
 * - Создай панель TutorialPanel на Canvas (по умолчанию выключена).
 * - Назначь tutorialPanel, tutorialBodyText, startDayButton в Inspector.
 * - Компонент TutorialUI может висеть на UIController или на самой панели.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialUI : MonoBehaviour
{
    [Header("Панель")]
    [Tooltip("Корневой объект экрана туториала. Должен быть выключен в сцене до старта дня.")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("Текст")]
    [Tooltip("Текст правил. Если в Inspector пусто — подставится defaultTutorialText при старте.")]
    [SerializeField] private TMP_Text tutorialBodyText;

    [Header("Кнопка")]
    [Tooltip("Кнопка «Начать день».")]
    [SerializeField] private Button startDayButton;

    [Header("Ссылки")]
    [Tooltip("Таймер смены на Managers. Если пусто — ищется на сцене.")]
    [SerializeField] private DayTimer dayTimer;

    [Header("Текст по умолчанию")]
    [TextArea(4, 8)]
    [SerializeField] private string defaultTutorialText =
        "Собирай сендвич перетаскиванием ингредиентов.\n" +
        "Запомни заказ клиента — потом его не переспросить.\n" +
        "Стряхивай плесень, дави тараканов, жарь хлеб и сдавай заказ в пакет.";

    private void Awake()
    {
        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        ApplyDefaultTutorialText();
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnDayStateChanged += HandleDayStateChanged;

        if (startDayButton != null)
            startDayButton.onClick.AddListener(HandleStartDayClicked);

        SyncVisibility();
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnDayStateChanged -= HandleDayStateChanged;

        if (startDayButton != null)
            startDayButton.onClick.RemoveListener(HandleStartDayClicked);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateReferences();
    }
#endif

    private void HandleDayStateChanged(DayFlowState state)
    {
        SyncVisibility(state);
    }

    private void SyncVisibility()
    {
        DayFlowState state = GameManager.Instance != null
            ? GameManager.Instance.CurrentDayState
            : DayFlowState.None;

        SyncVisibility(state);
    }

    private void SyncVisibility(DayFlowState state)
    {
        if (tutorialPanel == null)
            return;

        tutorialPanel.SetActive(state == DayFlowState.Tutorial);
    }

    private void HandleStartDayClicked()
    {
        if (GameManager.Instance == null)
            return;

        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        GameManager.Instance.StartDay(dayTimer);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    private void ApplyDefaultTutorialText()
    {
        if (tutorialBodyText == null)
            return;

        if (string.IsNullOrWhiteSpace(tutorialBodyText.text))
            tutorialBodyText.text = defaultTutorialText;
    }

    private void ValidateReferences()
    {
        if (tutorialPanel == null)
            Debug.LogWarning($"{name}: назначь tutorialPanel в Inspector.", this);

        if (tutorialBodyText == null)
            Debug.LogWarning($"{name}: назначь tutorialBodyText (TextMeshPro) в Inspector.", this);

        if (startDayButton == null)
            Debug.LogWarning($"{name}: назначь startDayButton в Inspector.", this);
    }
}
