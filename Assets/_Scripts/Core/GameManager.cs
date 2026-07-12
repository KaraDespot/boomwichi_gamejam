/*
 * GameManager
 * Назначение: глобальное состояние приложения и flow игрового дня.
 * Что делает: переходы между сценами, пауза, состояния дня (Tutorial → DayFinished).
 * Связи: SceneLoader, EventBus, InputManager, DayTimer (через StartDay).
 * Паттерны: Singleton, State Machine (DayFlowState).
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Lost,
    Won,
}

/// <summary>
/// Состояния игрового дня внутри GameScene (см. Boomwichi_Development_Plan, раздел 17).
/// </summary>
public enum DayFlowState
{
    None,
    Tutorial,
    DayStarting,
    ShowingOrder,
    PlayingOrder,
    EvaluatingOrder,
    CustomerReaction,
    DayFinished,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Menu;
    public DayFlowState CurrentDayState { get; private set; } = DayFlowState.None;

    /// <summary> Смена состояния дня для UI и систем заказов. </summary>
    public event Action<DayFlowState> OnDayStateChanged;

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

    private void OnEnable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnLevelLoaded -= HandleLevelLoaded;
    }

    public void StartGame()
    {
        LoadGameplayScene(SceneNames.GameScene);
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        StartGame();
    }

    public bool TryContinueFromSlot(int slotIndex)
    {
        StartGame();
        return true;
    }

    public void GoToMenu()
    {
        CurrentState = GameState.Menu;
        SetDayState(DayFlowState.None);
        Time.timeScale = 1f;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.Load(SceneNames.MainMenu);
        else
            SceneManager.LoadScene(SceneNames.MainMenu);

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    public void Pause()
    {
        if (!CanPause())
            return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f;

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseGamePaused();
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseGameResumed();
    }

    public void RestartGameScene()
    {
        SetDayState(DayFlowState.None);
        LoadGameplayScene(SceneNames.GameScene);
    }

    public void RestartFromCheckpointOrScene()
    {
        RestartGameScene();
    }

    public bool TryLoadActiveCheckpoint()
    {
        return false;
    }

    public bool TrySaveLevelCheckpointProgress(int slotIndex, Vector3 levelExitPosition)
    {
        return false;
    }

    public bool TrySaveCheckpointProgress(int slotIndex, Vector3 checkpointPosition)
    {
        return false;
    }

    public bool TrySaveCheckpointProgress(Vector3 checkpointPosition)
    {
        return false;
    }

    public void EnterLoseState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Lost;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    public void EnterWinState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Won;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Вызывается при входе в GameScene: начинаем с туториала.
    /// </summary>
    public void BeginDayFlow()
    {
        if (CurrentState != GameState.Playing)
            return;

        SetDayState(DayFlowState.Tutorial);
    }

    /// <summary>
    /// Запускает игровой день после туториала: таймер + показ первого заказа.
    /// </summary>
    public void StartDay(DayTimer dayTimer)
    {
        if (CurrentDayState != DayFlowState.Tutorial)
        {
            Debug.LogWarning("GameManager: StartDay вызван не из Tutorial.");
            return;
        }

        SetDayState(DayFlowState.DayStarting);

        if (dayTimer != null)
            dayTimer.StartDay();
    }

    /// <summary>
    /// Переход к активной фазе заказа (после скрытия облака заказа).
    /// </summary>
    public void BeginPlayingOrder()
    {
        if (CurrentDayState != DayFlowState.ShowingOrder)
            return;

        SetDayState(DayFlowState.PlayingOrder);
    }

    /// <summary>
    /// Завершение смены: показ финальной статистики.
    /// </summary>
    public void FinishDay()
    {
        if (CurrentDayState == DayFlowState.DayFinished)
            return;

        SetDayState(DayFlowState.DayFinished);
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseDayFinished();
    }

    /// <summary>
    /// Можно ли открыть паузу в текущей фазе дня.
    /// </summary>
    public bool CanPause()
    {
        if (CurrentState != GameState.Playing)
            return false;

        return CurrentDayState != DayFlowState.EvaluatingOrder
            && CurrentDayState != DayFlowState.DayFinished;
    }

    public void SetDayState(DayFlowState newState)
    {
        if (CurrentDayState == newState)
            return;

        CurrentDayState = newState;
        OnDayStateChanged?.Invoke(newState);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseDayStateChanged(newState);
    }

    private void HandleLevelLoaded(string sceneName)
    {
        if (sceneName == SceneNames.MainMenu)
        {
            CurrentState = GameState.Menu;
            SetDayState(DayFlowState.None);
            return;
        }

        if (sceneName == SceneNames.GameScene)
            SetDayState(DayFlowState.None);
    }

    private void LoadGameplayScene(string sceneName)
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);

        if (InputManager.Instance != null)
            InputManager.Instance.EnableGameplayInput();
    }
}
