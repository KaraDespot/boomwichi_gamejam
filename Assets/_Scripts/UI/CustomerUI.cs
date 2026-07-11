/*
 * CustomerUI
 * Назначение: облако заказа клиента.
 * Что делает: показывает фразу один раз, скрывает после первого действия или кнопки «Понял».
 * Связи: OrderManager, EventBus, GameManager.
 * Паттерны: UI Controller, Observer.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomerUI : MonoBehaviour
{
    [Header("Облако заказа")]
    [SerializeField] private GameObject orderBubblePanel;

    [SerializeField] private TMP_Text orderPhraseText;

    [Header("Кнопка")]
    [SerializeField] private Button hideOrderButton;

    private bool orderHiddenForCurrentOrder;

    private void Awake()
    {
        EnsureRuntimeBubbleIfMissing();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnOrderStarted += HandleOrderStarted;
            EventBus.Instance.OnFirstPlayerAction += HandleFirstPlayerAction;
        }

        if (hideOrderButton != null)
            hideOrderButton.onClick.AddListener(HandleHideOrderClicked);
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnOrderStarted -= HandleOrderStarted;
            EventBus.Instance.OnFirstPlayerAction -= HandleFirstPlayerAction;
        }

        if (hideOrderButton != null)
            hideOrderButton.onClick.RemoveListener(HandleHideOrderClicked);
    }

    private void HandleOrderStarted(int orderIndex, OrderDataAsset order)
    {
        orderHiddenForCurrentOrder = false;

        if (orderPhraseText != null && order != null)
            orderPhraseText.text = order.CustomerPhrase;

        if (orderBubblePanel != null)
            orderBubblePanel.SetActive(true);
    }

    private void HandleFirstPlayerAction()
    {
        if (orderHiddenForCurrentOrder)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentDayState == DayFlowState.ShowingOrder)
        {
            HideOrderBubble();
        }
    }

    private void HandleHideOrderClicked()
    {
        HideOrderBubble();
    }

    private void HideOrderBubble()
    {
        if (orderHiddenForCurrentOrder)
            return;

        orderHiddenForCurrentOrder = true;

        if (orderBubblePanel != null)
            orderBubblePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.BeginPlayingOrder();
    }

    private void EnsureRuntimeBubbleIfMissing()
    {
        if (orderBubblePanel != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning($"{name}: Canvas не найден — CustomerUI не создан.", this);
            return;
        }

        orderBubblePanel = new GameObject("OrderBubblePanel", typeof(RectTransform), typeof(Image));
        orderBubblePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = orderBubblePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.55f);
        panelRect.anchorMax = new Vector2(0.55f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = orderBubblePanel.GetComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject textObject = new GameObject("OrderPhraseText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(orderBubblePanel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.2f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        orderPhraseText = textObject.GetComponent<TextMeshProUGUI>();
        orderPhraseText.fontSize = 24f;
        orderPhraseText.color = Color.black;
        orderPhraseText.alignment = TextAlignmentOptions.TopLeft;
        orderPhraseText.text = string.Empty;

        GameObject buttonObject = new GameObject("HideOrderButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(orderBubblePanel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.65f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.18f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        hideOrderButton = buttonObject.GetComponent<Button>();

        GameObject buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonLabelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = buttonLabelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonLabel = buttonLabelObject.GetComponent<TextMeshProUGUI>();
        buttonLabel.fontSize = 20f;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = Color.black;
        buttonLabel.text = "Понял";

        orderBubblePanel.SetActive(false);
    }
}
