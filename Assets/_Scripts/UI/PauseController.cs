using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
 * PauseController
 * Назначение: управляет показом/скрытием паузы и действиями Resume/Main Menu.
 * Роль в игре: обслуживает только pause-flow, без логики панели настроек.
 * Связи: EventBus (события паузы), InputManager (клавиши), GameManager (смена состояния), UI-кнопки.
 * Как используется: висит в игровой сцене, ссылки на pausePanel и кнопки назначаются в Inspector.
 * Порядок Bootstrap: EventBus и InputManager могут появиться после OnEnable этого компонента, поэтому подписка выполняется отложенно.
 * Риск интеграции: после импорта внешнего пакета порядок загрузки может измениться, и пауза будет "молчать", если подписка была сделана слишком рано.
 * Идеи расширения:
 * - Добавить установку фокуса на Resume при открытии паузы.
 * - Добавить анимацию появления/скрытия панели.
 * Практические советы:
 * - Если пауза не открывается с клавиши, проверить InputManager.Instance, EventBus.Instance и action Player/Pause в Console/Inspector.
 * - Если кнопки молчат, проверить назначение buttonResume/buttonMainMenu в Inspector.
 */
public class PauseController : MonoBehaviour
{
    [Header("UI паузы")]
    [Tooltip("Корневой объект панели паузы.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Кнопка продолжения игры.")]
    [SerializeField] private Button buttonResume;

    [Tooltip("Кнопка возврата в главное меню.")]
    [SerializeField] private Button buttonMainMenu;

    private Coroutine bindingRoutine;
    private bool eventBusBound;
    private bool inputManagerBound;

    /// <summary>
    /// Входные условия: gameplay-сцена только создала UI-объекты.
    /// Шаги: сразу скрыть pausePanel, чтобы active state из prefab или scene не переносил паузу между уровнями.
    /// Типичные поломки: панель была сохранена активной в prefab или scene, поэтому появлялась при загрузке уровня.
    /// Что проверить: active state pausePanel в Inspector и Console на ошибки ссылок.
    /// </summary>
    private void Awake()
    {
        HidePausePanel();
    }

    private void Start()
    {
        ValidateReferences();
        HidePausePanel();
    }

    private void OnEnable()
    {
        bindingRoutine = StartCoroutine(BindDependenciesWhenReady());

        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);

        if (buttonMainMenu != null)
            buttonMainMenu.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDisable()
    {
        if (bindingRoutine != null)
        {
            StopCoroutine(bindingRoutine);
            bindingRoutine = null;
        }

        UnbindDependencies();

        if (buttonResume != null)
            buttonResume.onClick.RemoveListener(OnResumeClicked);

        if (buttonMainMenu != null)
            buttonMainMenu.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void ValidateReferences()
    {
        if (pausePanel == null)
            Debug.LogError($"{name}: pausePanel не назначен.", this);

        if (buttonResume == null)
            Debug.LogWarning($"{name}: buttonResume не назначен.", this);

        if (buttonMainMenu == null)
            Debug.LogWarning($"{name}: buttonMainMenu не назначен.", this);
    }

    /// <summary>
    /// Контракт: ждёт появления EventBus и InputManager, затем один раз подписывает pause UI на runtime-события.
    /// Входные условия: компонент активен, Bootstrap может уже создать singleton'ы или создать их через несколько кадров.
    /// Гарантии: подписки не дублируются, а OnDisable корректно снимает их и сбрасывает флаги.
    /// Типичные поломки: EventBus/InputManager не созданы Bootstrap-сценой, action Player/Pause не найден или не назначен Input Actions Asset.
    /// Что проверить: Console на ошибки InputManager, Inspector у BootstrapManager/InputManager и ссылки pausePanel/buttonResume/buttonMainMenu.
    /// </summary>
    private IEnumerator BindDependenciesWhenReady()
    {
        while (isActiveAndEnabled && (!eventBusBound || !inputManagerBound))
        {
            if (!eventBusBound && EventBus.Instance != null)
            {
                EventBus.Instance.OnGamePaused += ShowPausePanel;
                EventBus.Instance.OnGameResumed += HidePausePanel;
                eventBusBound = true;
            }

            if (!inputManagerBound && InputManager.Instance != null)
            {
                InputManager.Instance.OnPausePressed += HandlePausePressed;
                InputManager.Instance.OnCancelPressed += HandleCancelPressed;
                inputManagerBound = true;
            }

            if (!eventBusBound || !inputManagerBound)
                yield return null;
        }

        bindingRoutine = null;
    }

    private void UnbindDependencies()
    {
        if (eventBusBound && EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= ShowPausePanel;
            EventBus.Instance.OnGameResumed -= HidePausePanel;
        }

        if (inputManagerBound && InputManager.Instance != null)
        {
            InputManager.Instance.OnPausePressed -= HandlePausePressed;
            InputManager.Instance.OnCancelPressed -= HandleCancelPressed;
        }

        eventBusBound = false;
        inputManagerBound = false;
    }

    private void ShowPausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void HidePausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }

    private void HandlePausePressed()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.Pause();
    }

    private void HandleCancelPressed()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            GameManager.Instance.Resume();
    }
}
