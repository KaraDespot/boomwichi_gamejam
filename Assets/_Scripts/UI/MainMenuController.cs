using UnityEngine;
using UnityEngine.UI;

/*
 * MainMenuController
 * Назначение: кнопки главного меню и открытие экранов настроек и разработчиков.
 * Связи: GameManager, SettingsPanelController, DevelopersCanvasController.
 */
public class MainMenuController : MonoBehaviour
{
    [Header("Кнопки главного меню")]
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonContinue;
    [SerializeField] private Button buttonSettings;
    [SerializeField] private Button buttonDevelopers;
    [SerializeField] private Button buttonExit;

    [Header("Экран настроек")]
    [Tooltip("Компонент на Canvas настроек со слайдерами и кнопкой «Назад».")]
    [SerializeField] private SettingsPanelController settingsPanelController;

    [Header("Canvas разработчиков")]
    [Tooltip("Контроллер отдельного Canvas разработчиков с кнопкой «Назад».")]
    [SerializeField] private DevelopersCanvasController developersCanvasController;

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

        if (buttonDevelopers != null)
            buttonDevelopers.onClick.AddListener(HandleDevelopersClicked);
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

        if (buttonDevelopers != null)
            buttonDevelopers.onClick.RemoveListener(HandleDevelopersClicked);
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

    private void HandleDevelopersClicked()
    {
        if (developersCanvasController != null)
            developersCanvasController.OpenCanvas();
    }
}
