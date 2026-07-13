using System;
using UnityEngine;
using UnityEngine.UI;

/*
 * DevelopersCanvasController
 * Назначение: экран «Разработчики» в главном меню.
 * Что делает: включает отдельный Canvas разработчиков и возвращает назад по кнопке.
 * Связи: MainMenuController.
 * Паттерны: UI Controller.
 *
 * Настройка в сцене:
 * - Повесь компонент на MainMenuController или другой постоянный объект меню.
 * - Назначь developersCanvas и backButton в Inspector.
 * - Canvas разработчиков по умолчанию должен быть выключен в иерархии.
 * - В MainMenuController укажи кнопку и ссылку на этот компонент.
 */
[DisallowMultipleComponent]
public class DevelopersCanvasController : MonoBehaviour
{
    [Header("Canvas разработчиков")]
    [Tooltip("Отдельный Canvas с титрами. Перетащи сюда компонент Canvas, не Panel.")]
    [SerializeField] private Canvas developersCanvas;

    [Header("Кнопки")]
    [Tooltip("Кнопка «Назад» на Canvas разработчиков — закрывает экран.")]
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
        if (backButton != null)
            backButton.onClick.AddListener(HandleBackClicked);
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(HandleBackClicked);
    }

    public void OpenCanvas()
    {
        if (developersCanvas == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        developersCanvas.gameObject.SetActive(true);
    }

    public void CloseCanvas()
    {
        if (developersCanvas == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        HideDevelopersCanvas();
        OnDevelopersClosed?.Invoke();
    }

    private void HandleBackClicked()
    {
        CloseCanvas();
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
