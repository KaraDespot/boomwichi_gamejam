using System;
using UnityEngine;

/*
 * EventBus
 * Назначение: единая шина событий между core/UI/gameplay системами.
 * Зачем нужен: снижает связанность - отправитель события не зависит от конкретных получателей.
 * Как используется сейчас:
 *  - GameManager публикует pause/resume и смену DayFlowState.
 *  - SceneLoader публикует факт загрузки любой сцены.
 *  - DayTimer публикует прогресс дня.
 * Подписчики могут свободно добавляться без правок отправителей.
 */
public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    /// <summary>
    /// Игра поставлена на паузу.
    /// Подписчики обычно: UI-пауза, системы ввода, аудио.
    /// </summary>
    public event Action OnGamePaused;

    /// <summary>
    /// Игра продолжена после паузы.
    /// </summary>
    public event Action OnGameResumed;

    /// <summary>
    /// Unity-сцена завершила загрузку.
    /// Параметр: имя загруженной сцены.
    /// </summary>
    public event Action<string> OnLevelLoaded;

    /// <summary>
    /// Encounter завершён по правилам encounter-системы.
    /// Параметр: encounterId из EncounterData (или имя объекта как fallback).
    /// </summary>
    public event Action<string> OnEncounterCompleted;

    /// <summary>
    /// Смена состояния игрового дня (Tutorial, PlayingOrder и т.д.).
    /// </summary>
    public event Action<DayFlowState> OnDayStateChanged;

    /// <summary>
    /// Обновление таймера дня: прошло секунд / всего секунд.
    /// </summary>
    public event Action<float, float> OnDayTimeUpdated;

    /// <summary>
    /// Лимит 5 минут исчерпан; текущий заказ можно доделать.
    /// </summary>
    public event Action OnDayTimeExpired;

    /// <summary>
    /// Смена полностью завершена (все заказы обработаны или день закрыт).
    /// </summary>
    public event Action OnDayFinished;

    /// <summary>
    /// Первое действие игрока в заказе (скрыть облако заказа).
    /// </summary>
    public event Action OnFirstPlayerAction;

    /// <summary>
    /// Начался новый заказ: индекс и описание.
    /// </summary>
    public event Action<int, OrderDataAsset> OnOrderStarted;

    /// <summary>
    /// Заказ провален (таймер, грязь и т.д.).
    /// </summary>
    public event Action<int> OnOrderFailed;

    /// <summary>
    /// Заказ успешно сдан: индекс и начисленные чаевые.
    /// </summary>
    public event Action<int, int> OnOrderCompleted;

    /// <summary>
    /// Обновление таймера клиента: осталось / всего секунд.
    /// </summary>
    public event Action<float, float> OnCustomerTimeUpdated;

    /// <summary>
    /// Таймер ожидания клиента истёк.
    /// </summary>
    public event Action OnCustomerTimeExpired;

    /// <summary>
    /// Изменились накопленные чаевые за день.
    /// </summary>
    public event Action<int> OnTipsChanged;

    /// <summary>
    /// Инициализация singleton-экземпляра EventBus.
    /// Объект сохраняется между сценами, чтобы подписчики не теряли источник событий.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Публикует событие паузы.
    /// </summary>
    public void RaiseGamePaused()
    {
        OnGamePaused?.Invoke();
    }

    /// <summary>
    /// Публикует событие продолжения игры.
    /// </summary>
    public void RaiseGameResumed()
    {
        OnGameResumed?.Invoke();
    }

    /// <summary>
    /// Публикует событие "сцена загружена".
    /// </summary>
    public void RaiseLevelLoaded(string sceneName)
    {
        OnLevelLoaded?.Invoke(sceneName);
    }

    /// <summary>
    /// Публикует событие "encounter завершён".
    /// </summary>
    public void RaiseEncounterCompleted(string encounterId)
    {
        OnEncounterCompleted?.Invoke(encounterId);
    }

    public void RaiseDayStateChanged(DayFlowState state)
    {
        OnDayStateChanged?.Invoke(state);
    }

    public void RaiseDayTimeUpdated(float elapsed, float total)
    {
        OnDayTimeUpdated?.Invoke(elapsed, total);
    }

    public void RaiseDayTimeExpired()
    {
        OnDayTimeExpired?.Invoke();
    }

    public void RaiseDayFinished()
    {
        OnDayFinished?.Invoke();
    }

    public void RaiseFirstPlayerAction()
    {
        OnFirstPlayerAction?.Invoke();
    }

    public void RaiseOrderStarted(int orderIndex, OrderDataAsset order)
    {
        OnOrderStarted?.Invoke(orderIndex, order);
    }

    public void RaiseOrderFailed(int orderIndex)
    {
        OnOrderFailed?.Invoke(orderIndex);
    }

    public void RaiseOrderCompleted(int orderIndex, int tipsEarned)
    {
        OnOrderCompleted?.Invoke(orderIndex, tipsEarned);
    }

    public void RaiseCustomerTimeUpdated(float remaining, float total)
    {
        OnCustomerTimeUpdated?.Invoke(remaining, total);
    }

    public void RaiseCustomerTimeExpired()
    {
        OnCustomerTimeExpired?.Invoke();
    }

    public void RaiseTipsChanged(int totalTips)
    {
        OnTipsChanged?.Invoke(totalTips);
    }
}
