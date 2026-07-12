/*
 * TutorialUI
 * Назначение: экран туториала перед началом игрового дня.
 * Что делает: показывает краткие правила и запускает день по кнопке.
 * Связи: GameManager, DayTimer, EventBus.
 * Паттерны: UI Controller, Observer.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Панель")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("Текст")]
    [SerializeField] private TMP_Text tutorialBodyText;

    [Header("Кнопка")]
    [SerializeField] private Button startDayButton;

    [Header("Ссылки")]
    [SerializeField] private DayTimer dayTimer;

    [TextArea(4, 8)]
    [SerializeField] private string defaultTutorialText =
        "Собирай сендвич перетаскиванием ингредиентов.\n" +
        "Запомни заказ клиента — потом его не переспросить.\n" +
        "Стряхивай плесень, дави тараканов, жарь хлеб и сдавай заказ в пакет.";

    private void Awake()
    {
        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        EnsureRuntimePanelIfMissing();
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
        bool show = state == DayFlowState.Tutorial;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(show);
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

    private void EnsureRuntimePanelIfMissing()
    {
        if (tutorialPanel != null)
        {
            if (tutorialBodyText != null)
                tutorialBodyText.text = defaultTutorialText;

            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning($"{name}: Canvas не найден — TutorialUI не создан.", this);
            return;
        }

        tutorialPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
        tutorialPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = tutorialPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject textObject = new GameObject("TutorialText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(tutorialPanel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.85f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tutorialBodyText = textObject.GetComponent<TextMeshProUGUI>();
        tutorialBodyText.fontSize = 28f;
        tutorialBodyText.alignment = TextAlignmentOptions.Center;
        tutorialBodyText.text = defaultTutorialText;

        GameObject buttonObject = new GameObject("StartDayButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(tutorialPanel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.35f, 0.12f);
        buttonRect.anchorMax = new Vector2(0.65f, 0.22f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        startDayButton = buttonObject.GetComponent<Button>();

        GameObject buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonLabelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = buttonLabelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonLabel = buttonLabelObject.GetComponent<TextMeshProUGUI>();
        buttonLabel.fontSize = 24f;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.text = "Начать день";

        tutorialPanel.SetActive(false);
    }
}
