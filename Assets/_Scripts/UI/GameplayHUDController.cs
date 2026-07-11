/*
 * GameplayHUDController
 * Назначение: игровой HUD (часы дня, позже — заказ, клиент, чаевые).
 * Что делает: подписывается на EventBus и выводит игровое время (10:00 a.m. → 10:00 p.m.).
 * Связи: EventBus, GameManager, DayTimer.
 * Паттерны: Observer, UI Controller.
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHUDController : MonoBehaviour
{
    [Header("Таймер дня")]
    [Tooltip("Текст игровых часов. Перетащи сюда свой UI-объект — позиция задаётся в RectTransform.")]
    [SerializeField] private TMP_Text dayTimerText;

    [Tooltip("Резервный UI.Text, если TMP не назначен.")]
    [SerializeField] private Text dayTimerTextLegacy;

    [Tooltip("Источник настроек часов. Если пусто — ищется DayTimer на сцене.")]
    [SerializeField] private DayTimer dayTimer;

    [Header("Пауза")]
    [Tooltip("Кнопка открытия паузы.")]
    [SerializeField] private Button pauseButton;

    [Header("Состояние дня (отладка)")]
    [Tooltip("Опционально: показывает текущий DayFlowState для отладки.")]
    [SerializeField] private TMP_Text dayStateDebugText;

    private Coroutine bindingRoutine;
    private bool eventBusBound;

    private void Awake()
    {
        if (dayTimer == null)
            dayTimer = FindFirstObjectByType<DayTimer>();

        ValidateReferences();
    }

    private void OnEnable()
    {
        bindingRoutine = StartCoroutine(BindEventBusWhenReady());

        if (pauseButton != null)
            pauseButton.onClick.AddListener(HandlePauseClicked);
    }

    private void OnDisable()
    {
        if (bindingRoutine != null)
        {
            StopCoroutine(bindingRoutine);
            bindingRoutine = null;
        }

        UnbindEventBus();

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(HandlePauseClicked);
    }

    private IEnumerator BindEventBusWhenReady()
    {
        while (isActiveAndEnabled && !eventBusBound)
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.OnDayTimeUpdated += HandleDayTimeUpdated;
                EventBus.Instance.OnDayStateChanged += HandleDayStateChanged;
                EventBus.Instance.OnDayTimeExpired += HandleDayTimeExpired;
                eventBusBound = true;
                SyncFromCurrentState();
            }

            if (!eventBusBound)
                yield return null;
        }

        bindingRoutine = null;
    }

    private void UnbindEventBus()
    {
        if (!eventBusBound || EventBus.Instance == null)
        {
            eventBusBound = false;
            return;
        }

        EventBus.Instance.OnDayTimeUpdated -= HandleDayTimeUpdated;
        EventBus.Instance.OnDayStateChanged -= HandleDayStateChanged;
        EventBus.Instance.OnDayTimeExpired -= HandleDayTimeExpired;
        eventBusBound = false;
    }

    private void SyncFromCurrentState()
    {
        if (dayTimer != null && dayTimer.IsRunning)
        {
            HandleDayTimeUpdated(dayTimer.ElapsedDayTime, dayTimer.TotalDayDuration);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentDayState == DayFlowState.Tutorial)
            SetDayTimerLabel(GetStartClockLabel());
    }

    private void ValidateReferences()
    {
        if (dayTimerText == null && dayTimerTextLegacy == null)
        {
            Debug.LogWarning(
                $"{name}: назначьте dayTimerText (или dayTimerTextLegacy) в Inspector — " +
                "создай TextMeshPro на Canvas и перетащи сюда.", this);
        }
    }

    private void HandleDayTimeUpdated(float elapsed, float total)
    {
        SetDayTimerLabel(FormatClock(elapsed, total));
    }

    private void HandleDayStateChanged(DayFlowState state)
    {
        if (dayStateDebugText != null)
            dayStateDebugText.text = state.ToString();

        if (state == DayFlowState.Tutorial)
            SetDayTimerLabel(GetStartClockLabel());
    }

    private void HandleDayTimeExpired()
    {
        SetDayTimerLabel(GetEndClockLabel());
    }

    private void HandlePauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Pause();
    }

    private string FormatClock(float elapsed, float total)
    {
        if (dayTimer != null)
            return dayTimer.FormatClockFor(elapsed, total);

        return DayTimer.FormatGameClock(elapsed, total, 10, false, 10, true);
    }

    private string GetStartClockLabel()
    {
        if (dayTimer != null)
            return dayTimer.GetStartClockLabel();

        return DayTimer.FormatGameClock(0f, 1f, 10, false, 10, true);
    }

    private string GetEndClockLabel()
    {
        if (dayTimer != null)
            return dayTimer.GetEndClockLabel();

        return DayTimer.FormatGameClock(1f, 1f, 10, false, 10, true);
    }

    private void SetDayTimerLabel(string value)
    {
        if (dayTimerText != null)
            dayTimerText.text = value;

        if (dayTimerTextLegacy != null)
            dayTimerTextLegacy.text = value;
    }
}
