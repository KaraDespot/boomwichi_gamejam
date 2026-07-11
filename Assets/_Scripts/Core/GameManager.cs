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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Menu;

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
        if (CurrentState != GameState.Playing)
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
