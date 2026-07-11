using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameLoopFlowController : MonoBehaviour
{
    [Header("Lose UI")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMenuButton;

    [Header("Win UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMenuButton;
    [SerializeField] private Button winNextLevelButton;

    [Header("Common UI")]
    [SerializeField] private GameObject pausePanel;

    public event UnityAction OnNextWaveRequested;

    private bool flowFinished;

    private void Awake()
    {
        HideAllScreens();
    }

    private void OnEnable()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.AddListener(HandleRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.AddListener(HandleMenuClicked);
    }

    private void OnDisable()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.RemoveListener(HandleRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.RemoveListener(HandleMenuClicked);
    }

    public bool RequestWinFromExit(Vector3 levelExitPosition)
    {
        TriggerWin();
        return true;
    }

    public void TriggerLose()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterLoseState();

        if (losePanel != null)
            losePanel.SetActive(true);
    }

    public void TriggerWin()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterWinState();

        if (winPanel != null)
            winPanel.SetActive(true);
    }

    private void HidePausePanelIfAssigned()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void HideAllScreens()
    {
        if (losePanel != null)
            losePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void HandleRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGameScene();
    }

    private void HandleMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
        else
            OnNextWaveRequested?.Invoke();
    }
}
