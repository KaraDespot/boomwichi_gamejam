using UnityEngine;
using UnityEngine.UI;

/*
 * MainMenuController
 * Назначение: связывает кнопки главного меню с игровым flow, настройками и выбором save-слота.
 * Роль в игре: открывает новую игру, продолжение, настройки и завершает приложение.
 * Связи: GameManager, SettingsPanelController, SaveSlotSelectionController, кнопки UI главного меню.
 * Как используется: компонент размещается в MainMenu-сцене, все ссылки назначаются вручную в Inspector.
 * Идеи расширения:
 * - Добавить подтверждение перезаписи занятого save-слота.
 * - Добавить звуковые эффекты на клики кнопок.
 * Практические советы:
 * - Для teacher repo лучше явные инспекторные связи, без автопоиска и автосоздания UI.
 * - Если панель не открывается, сначала проверить ссылку saveSlotSelectionController или settingsPanelController.
 */
public class MainMenuController : MonoBehaviour
{
    [Header("Кнопки главного меню")]
    [Tooltip("Кнопка начала новой игры. Открывает выбор save-слота.")]
    [SerializeField] private Button buttonNewGame;

    [Tooltip("Кнопка продолжения игры. Открывает выбор существующего save-слота.")]
    [SerializeField] private Button buttonContinue;

    [Tooltip("Кнопка открытия окна настроек.")]
    [SerializeField] private Button buttonSettings;

    [Tooltip("Кнопка выхода из приложения.")]
    [SerializeField] private Button buttonExit;

    [Header("Окно настроек")]
    [Tooltip("Контроллер отдельной панели настроек в MainMenu.")]
    [SerializeField] private SettingsPanelController settingsPanelController;

    [Header("Слоты сохранения")]
    [Tooltip("Панель выбора одного из трёх save-слотов для New Game и Continue.")]
    [SerializeField] private SaveSlotSelectionController saveSlotSelectionController;

    private void Start()
    {
        ValidateReferences();
        RefreshContinueButtonState();
    }

    /// <summary>
    /// Входные условия: объект меню активен, кнопки созданы вручную и назначены в Inspector.
    /// Шаги: подписать кнопки New Game, Continue, Settings и Exit на локальные обработчики.
    /// Типичные поломки: ссылка на кнопку не назначена, обработчик добавлен вручную и дублирует вызов.
    /// Что проверить: Inspector объекта MainMenuController и Console warnings после запуска MainMenu.
    /// </summary>
    private void OnEnable()
    {
        RefreshContinueButtonState();

        if (buttonNewGame != null)
            buttonNewGame.onClick.AddListener(HandleNewGameClicked);

        if (buttonContinue != null)
            buttonContinue.onClick.AddListener(HandleContinueClicked);

        if (buttonExit != null)
            buttonExit.onClick.AddListener(HandleExitClicked);

        if (buttonSettings != null)
            buttonSettings.onClick.AddListener(HandleSettingsClicked);
    }

    /// <summary>
    /// Входные условия: объект меню выключается или сцена выгружается.
    /// Шаги: снять подписки с кнопок, чтобы повторный вход в меню не создавал двойные клики.
    /// Типичные поломки: двойное открытие панели после возвращения в меню указывает на пропущенную отписку.
    /// Что проверить: повторный переход MainMenu -> Game -> MainMenu и количество логов на один клик.
    /// </summary>
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

    private void ValidateReferences()
    {
        if (buttonNewGame == null)
            Debug.LogError($"{name}: buttonNewGame не назначен.", this);

        if (buttonContinue == null)
            Debug.LogWarning($"{name}: buttonContinue не назначен. Continue из главного меню будет недоступен.", this);

        if (buttonExit == null)
            Debug.LogError($"{name}: buttonExit не назначен.", this);

        if (buttonSettings == null)
            Debug.LogWarning($"{name}: buttonSettings не назначен.", this);

        if (settingsPanelController == null)
            Debug.LogError($"{name}: settingsPanelController не назначен в Inspector.", this);

        if (saveSlotSelectionController == null)
            Debug.LogError($"{name}: saveSlotSelectionController не назначен. Выбор save-слота не откроется.", this);
    }

    /// <summary>
    /// Контракт: обновляет доступность Continue по фактическому наличию save-слотов.
    /// Метод вызывается при входе в меню, читает CheckpointSaveSystem.HasAnySave() и выставляет buttonContinue.interactable.
    /// Почему так: Button.interactable использует стандартное затемнение Unity и не требует отдельной логики цветов.
    /// Потенциальное применение: обновление кнопки после будущей очистки слота.
    /// </summary>
    private void RefreshContinueButtonState()
    {
        if (buttonContinue == null)
            return;

        buttonContinue.interactable = CheckpointSaveSystem.HasAnySave();
    }

    /// <summary>
    /// Контракт: открывает вручную собранную панель выбора слота для новой игры.
    /// Метод не создаёт UI и не очищает слот самостоятельно: после клика по слоту это сделает GameManager.
    /// Почему так: MainMenu отвечает за кнопки, а GameManager отвечает за игровой flow и active save slot.
    /// Потенциальное применение: позже добавить подтверждение перезаписи перед вызовом StartNewGameInSlot.
    /// </summary>
    private void HandleNewGameClicked()
    {
        if (saveSlotSelectionController == null)
        {
            Debug.LogError($"{name}: saveSlotSelectionController не назначен. Нельзя открыть выбор слота.", this);
            return;
        }

        saveSlotSelectionController.OpenForNewGame();
    }

    /// <summary>
    /// Контракт: открывает ту же панель выбора слота в режиме Continue.
    /// Пустые слоты остаются видимыми как New, но не запускают загрузку.
    /// Почему так: один экран выбора слотов проще поддерживать и объяснять, чем две разные панели.
    /// Потенциальное применение: добавить подсветку последнего выбранного профиля без автоматической загрузки.
    /// </summary>
    private void HandleContinueClicked()
    {
        if (saveSlotSelectionController == null)
        {
            Debug.LogError($"{name}: saveSlotSelectionController не назначен. Нельзя открыть Continue.", this);
            return;
        }

        saveSlotSelectionController.OpenForContinue();
    }

    private static void HandleExitClicked()
    {
        Application.Quit();
    }

    /// <summary>
    /// Контракт: открывает отдельную панель настроек из главного меню.
    /// Почему так: настройки остаются отдельным контроллером, без общего UIManager.
    /// Как дебажить: если окно не открылось, проверить settingsPanelController и ссылку settingsPanel внутри него.
    /// </summary>
    private void HandleSettingsClicked()
    {
        if (settingsPanelController == null)
        {
            Debug.LogError($"{name}: settingsPanelController отсутствует. Требуется назначить ссылку в Inspector.", this);
            return;
        }

        settingsPanelController.OpenPanel();
    }
}
