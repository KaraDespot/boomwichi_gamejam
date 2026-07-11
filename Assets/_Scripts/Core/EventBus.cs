using System;
using UnityEngine;

/*
 * EventBus
 * Назначение: единая шина событий между core/UI/gameplay системами.
 * Зачем нужен: снижает связанность - отправитель события не зависит от конкретных получателей.
 * Как используется сейчас:
 *  - GameManager публикует pause/resume.
 *  - SceneLoader публикует факт загрузки любой сцены.
 *  - EncounterTrigger публикует факт завершения encounter.
 * Подписчики могут свободно добавляться в будущих уроках без правок отправителей.
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
        OnGamePaused?.Invoke(); // вызываем событие, если есть подписчики
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
}
