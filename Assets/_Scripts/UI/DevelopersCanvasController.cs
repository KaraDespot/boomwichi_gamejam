using System;
using UnityEngine;
using UnityEngine.UI;

/*
 * DevelopersCanvasController
 * Назначение: экран «Разработчики» в главном меню.
 * Что делает: открывает Canvas разработчиков и возвращает в меню по кнопке «Назад».
 * Связи: MainMenuController.
 * Паттерны: UI Controller (тот же контракт, что у SettingsPanelController).
 *
 * Настройка в сцене:
 * - Повесь компонент на MainMenuController (рядом с SettingsPanelController).
 * - Назначь developersCanvas и backButton в Inspector.
 * - Canvas разработчиков по умолчанию должен быть выключен в иерархии.
 * - В MainMenuController укажи кнопку и ссылку на этот компонент.
 */
[DisallowMultipleComponent]
public class DevelopersCanvasController : MonoBehaviour
{
    [Header("Canvas разработчиков")]
    [Tooltip("Отдельный Canvas с титрами. Перетащи сюда компонент Canvas.")]
    [SerializeField] private Canvas developersCanvas;

    [Header("Кнопки")]
    [Tooltip("Кнопка «Назад в меню» на Canvas разработчиков.")]
    [SerializeField] private Button backButton;

    public event Action OnDevelopersClosed;

    public bool IsOpen => developersCanvas != null && developersCanvas.gameObject.activeSelf;

    private void Awake()
    {
        ValidateReferences();
        HideDevelopersCanvas();
    }

    private void Start()
    {
        HideDevelopersCanvas();
    }

    private void OnEnable()
    {
        BindUiHandlers();
    }

    private void OnDisable()
    {
        UnbindUiHandlers();
    }

    public void OpenPanel()
    {
        if (developersCanvas == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        developersCanvas.gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        if (developersCanvas == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        HideDevelopersCanvas();
        OnDevelopersClosed?.Invoke();
    }

    private void BindUiHandlers()
    {
        if (backButton != null)
            backButton.onClick.AddListener(HandleBackClicked);
    }

    private void UnbindUiHandlers()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(HandleBackClicked);
    }

    private void HandleBackClicked()
    {
        ClosePanel();
    }

    private void HideDevelopersCanvas()
    {
        if (developersCanvas != null)
            developersCanvas.gameObject.SetActive(false);
    }

    private void ValidateReferences()
    {
        if (developersCanvas == null)
            Debug.LogWarning($"{name}: назначь developersCanvas (компонент Canvas) в Inspector.", this);

        if (backButton == null)
            Debug.LogWarning($"{name}: назначь backButton в Inspector.", this);
    }
}
