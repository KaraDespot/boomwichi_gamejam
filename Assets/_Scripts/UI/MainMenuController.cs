using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonContinue;
    [SerializeField] private Button buttonSettings;
    [SerializeField] private Button buttonExit;

    [Header("Settings")]
    [SerializeField] private SettingsPanelController settingsPanelController;

    private void Start()
    {
        if (buttonContinue != null)
            buttonContinue.interactable = false;
    }

    private void OnEnable()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.AddListener(HandleNewGameClicked);

        if (buttonContinue != null)
            buttonContinue.onClick.AddListener(HandleContinueClicked);

        if (buttonExit != null)
            buttonExit.onClick.AddListener(HandleExitClicked);

        if (buttonSettings != null)
            buttonSettings.onClick.AddListener(HandleSettingsClicked);
    }

    private void OnDisable()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.RemoveListener(HandleNewGameClicked);

        if (buttonContinue != null)
            buttonContinue.onClick.RemoveListener(HandleContinueClicked);

        if (buttonExit != null)
            buttonExit.onClick.RemoveListener(HandleExitClicked);

        if (buttonSettings != null)
            buttonSettings.onClick.RemoveListener(HandleSettingsClicked);
    }

    private static void HandleNewGameClicked()
    {
        AudioManager.Instance?.PlaySfx(AudioCue.Button);

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private static void HandleContinueClicked()
    {
        AudioManager.Instance?.PlaySfx(AudioCue.Button);

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private static void HandleExitClicked()
    {
        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        Application.Quit();
    }

    private void HandleSettingsClicked()
    {
        if (settingsPanelController != null)
            settingsPanelController.OpenPanel();
    }
}
