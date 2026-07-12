/*
 * DayTimer
 * Назначение: отсчёт 5-минутного игрового дня и игровые часы (10:00 a.m. → 10:00 p.m.).
 * Что делает: накапливает elapsedDayTime, публикует прогресс для UI/плесени, фиксирует окончание дня.
 * Связи: GameManager (пауза), EventBus (события времени), GameplayHUDController (отображение).
 * Паттерны: компонент сцены GameScene, Observer через EventBus.
 */

using UnityEngine;

public class DayTimer : MonoBehaviour
{
    [Header("Длительность дня")]
    [Tooltip("Полная длительность игрового дня в секундах (5 минут = 300).")]
    [SerializeField] private float totalDayDuration = 300f;

    [Header("Игровые часы")]
    [Tooltip("Час начала смены (12-часовой формат).")]
    [SerializeField] private int clockStartHour = 10;

    [Tooltip("true = p.m., false = a.m. для начала смены.")]
    [SerializeField] private bool clockStartIsPm;

    [Tooltip("Час конца смены (12-часовой формат).")]
    [SerializeField] private int clockEndHour = 10;

    [Tooltip("true = p.m., false = a.m. для конца смены.")]
    [SerializeField] private bool clockEndIsPm = true;

    private float elapsedDayTime;
    private bool isRunning;
    private bool isDayTimeExpired;

    /// <summary> Сколько секунд прошло с начала дня. </summary>
    public float ElapsedDayTime => elapsedDayTime;

    /// <summary> Полная длительность дня в секундах. </summary>
    public float TotalDayDuration => totalDayDuration;

    /// <summary> Оставшееся время дня в секундах. </summary>
    public float RemainingTime => Mathf.Max(0f, totalDayDuration - elapsedDayTime);

    /// <summary> Прогресс дня от 0 до 1 (для плесени и баланса). </summary>
    public float DayProgress => totalDayDuration > 0f
        ? Mathf.Clamp01(elapsedDayTime / totalDayDuration)
        : 0f;

    /// <summary> Таймер активно тикает. </summary>
    public bool IsRunning => isRunning;

    /// <summary> Лимит дня исчерпан; текущий заказ можно доделать. </summary>
    public bool IsDayTimeExpired => isDayTimeExpired;

    private void Update()
    {
        if (!isRunning)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            return;

        elapsedDayTime += Time.deltaTime;
        PublishTimeUpdated();

        if (!isDayTimeExpired && elapsedDayTime >= totalDayDuration)
        {
            isDayTimeExpired = true;
            elapsedDayTime = totalDayDuration;

            if (EventBus.Instance != null)
                EventBus.Instance.RaiseDayTimeExpired();
        }
    }

    /// <summary>
    /// Запускает отсчёт дня с нуля. Вызывается при переходе в DayStarting.
    /// </summary>
    public void StartDay()
    {
        elapsedDayTime = 0f;
        isDayTimeExpired = false;
        isRunning = true;
        PublishTimeUpdated();
    }

    /// <summary>
    /// Останавливает таймер (конец смены или рестарт сцены).
    /// </summary>
    public void StopDay()
    {
        isRunning = false;
    }

    /// <summary>
    /// Сбрасывает состояние без запуска (перед новым днём).
    /// </summary>
    public void ResetTimer()
    {
        elapsedDayTime = 0f;
        isDayTimeExpired = false;
        isRunning = false;
        PublishTimeUpdated();
    }

    /// <summary>
    /// Форматирует игровое время для произвольного elapsed (для UI-событий).
    /// </summary>
    public string FormatClockFor(float elapsed, float total)
    {
        return FormatGameClock(
            elapsed,
            total,
            clockStartHour,
            clockStartIsPm,
            clockEndHour,
            clockEndIsPm);
    }

    /// <summary>
    /// Форматирует текущее игровое время с настройками этого компонента.
    /// </summary>
    public string GetFormattedClockTime()
    {
        return FormatGameClock(
            elapsedDayTime,
            totalDayDuration,
            clockStartHour,
            clockStartIsPm,
            clockEndHour,
            clockEndIsPm);
    }

    /// <summary>
    /// Форматирует игровое время по прогрессу дня (10:00 a.m. → 10:00 p.m. по умолчанию).
    /// </summary>
    public static string FormatGameClock(
        float elapsed,
        float total,
        int startHour12,
        bool startIsPm,
        int endHour12,
        bool endIsPm)
    {
        int startMinutes = ToMinutesSinceMidnight(startHour12, startIsPm);
        int endMinutes = ToMinutesSinceMidnight(endHour12, endIsPm);

        if (endMinutes <= startMinutes)
            endMinutes += 12 * 60; // Обработка перехода через полдень/полночь

        float progress = total > 0f ? Mathf.Clamp01(elapsed / total) : 0f;
        float currentMinutes = Mathf.Lerp(startMinutes, endMinutes, progress);

        // --- ИЗМЕНЕНИЕ ТУТ ---
        // Округляем до ближайших 10 минут
        int minutesRounded = Mathf.RoundToInt(currentMinutes / 10f) * 10;
        
        return FormatMinutesAsClock(minutesRounded);
    }

    /// <summary>
    /// Время начала смены (до старта дня).
    /// </summary>
    public string GetStartClockLabel()
    {
        return FormatMinutesAsClock(ToMinutesSinceMidnight(clockStartHour, clockStartIsPm));
    }

    /// <summary>
    /// Время конца смены.
    /// </summary>
    public string GetEndClockLabel()
    {
        return FormatMinutesAsClock(ToMinutesSinceMidnight(clockEndHour, clockEndIsPm));
    }

    private static int ToMinutesSinceMidnight(int hour12, bool isPm)
    {
        hour12 = Mathf.Clamp(hour12, 1, 12);
        int hour24 = hour12 % 12;
        if (isPm)
            hour24 += 12;

        return hour24 * 60;
    }

    private static string FormatMinutesAsClock(int totalMinutes)
    {
        totalMinutes = Mathf.Max(0, totalMinutes);
        int hour24 = (totalMinutes / 60) % 24;
        int minute = totalMinutes % 60;

        int hour12 = hour24 % 12;
        if (hour12 == 0)
            hour12 = 12;

        string period = hour24 < 12 ? "a.m." : "p.m.";
        return $"{hour12}:{minute:00} {period}";
    }

    private void PublishTimeUpdated()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseDayTimeUpdated(elapsedDayTime, totalDayDuration);
    }
}
