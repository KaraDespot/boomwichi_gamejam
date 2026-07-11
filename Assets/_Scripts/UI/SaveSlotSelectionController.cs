using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// SaveSlotSelectionController
/// Что делает: управляет вручную собранной панелью выбора одного из трёх save-слотов.
/// Зачем нужен в игре: New Game и Continue должны явно выбирать профиль, чтобы прогресс не смешивался между прохождениями.
/// Связи: читает CheckpointSaveSystem, вызывает GameManager, обновляет Button/Image/Text, назначенные в Inspector.
/// Как используется: MainMenuController открывает панель в режиме новой игры или продолжения, кнопки слотов вызывают выбор слота.
/// Возможные расширения: подтверждение перезаписи, кнопка удаления слота, дата последнего сохранения.
/// Совет: если слот не обновляется, проверить ссылки Slot Views, active state occupied/new блоков и наличие save в persistentDataPath.
/// Совет: панель не создаёт кнопки кодом; все визуальные элементы должны быть заранее собраны в сцене или prefab.
/// </summary>
public class SaveSlotSelectionController : MonoBehaviour
{
    private enum SlotSelectionMode
    {
        NewGame,
        Continue
    }

    [Serializable]
#pragma warning disable 0649
    private sealed class SaveSlotView
    {
        [Header("Кнопка слота")]
        [Tooltip("Кнопка всей ячейки слота. Создаётся вручную в UI и назначается в Inspector.")]
        public Button slotButton;

        [Header("Состояния ячейки")]
        [Tooltip("Блок занятого слота: иконка персонажа, уровень, ОЗ и сцена.")]
        public GameObject occupiedState;

        [Tooltip("Блок пустого слота. Внутри вручную размещается надпись New.")]
        public GameObject emptyState;

        [Header("Занятый слот")]
        [Tooltip("Иконка персонажа. Спрайт назначается вручную на Image или через поле defaultCharacterSprite.")]
        public Image characterIcon;

        [Tooltip("Текст уровня персонажа.")]
        public TMP_Text levelText;

        [Tooltip("Текст здоровья персонажа.")]
        public TMP_Text healthText;

        [Tooltip("Текст игровой сцены или уровня.")]
        public TMP_Text sceneText;
    }
#pragma warning restore 0649

    [Header("Панель")]
    [Tooltip("Корневой объект панели выбора слота.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Кнопка закрытия панели. Визуально это может быть крестик, созданный вручную.")]
    [SerializeField] private Button closeButton;

    [Header("Слоты")]
    [Tooltip("Три вручную собранные ячейки save-слотов.")]
    [SerializeField] private SaveSlotView[] slotViews = new SaveSlotView[CheckpointSaveSystem.SlotCount];

    [Header("Визуал")]
    [Tooltip("Иконка персонажа для занятого слота, если на самой Image ещё не назначен sprite.")]
    [SerializeField] private Sprite defaultCharacterSprite;

    private SlotSelectionMode currentMode = SlotSelectionMode.NewGame;
    private UnityAction[] slotClickHandlers;

    /// <summary>
    /// Контракт: подписывает только уже назначенные кнопки.
    /// Входные условия: UI-ячейки созданы вручную, ссылки назначены в Inspector.
    /// Шаги: подписать close, подписать три slotButton с их индексами.
    /// Типичные поломки: массив короче трёх элементов, slotButton не назначен, объект панели выключен не тем root-объектом.
    /// Что проверить: Inspector на объекте SaveSlotSelectionController и Console warnings при старте MainMenu.
    /// </summary>
    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        BindSlotButtons();
    }

    /// <summary>
    /// Контракт: снимает подписки при выключении панели или выгрузке сцены.
    /// Почему так: повторное открытие панели не должно добавлять повторные обработчики клика.
    /// Потенциальное применение: такой же подход подходит для pause/settings панелей.
    /// </summary>
    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        UnbindSlotButtons();
    }

    /// <summary>
    /// Контракт: открывает панель в режиме New Game.
    /// Все слоты доступны: пустой слот начинает новый прогон, занятый слот будет очищен без подтверждения на уровне этого урока.
    /// Почему так: подтверждение перезаписи остаётся отдельным UI-сценарием, чтобы не усложнять load/restart flow.
    /// Потенциальное применение: выбор профиля перед стартом новой кампании.
    /// </summary>
    public void OpenForNewGame()
    {
        currentMode = SlotSelectionMode.NewGame;
        OpenAndRefresh();
    }

    /// <summary>
    /// Контракт: открывает панель в режиме Continue.
    /// Пустые слоты показываются как New, но не запускают загрузку, потому что save ещё нет.
    /// Почему так: явный выбор существующего слота проще объяснить, чем автоматический поиск последнего сохранения.
    /// Потенциальное применение: экран загрузки профиля из главного меню.
    /// </summary>
    public void OpenForContinue()
    {
        currentMode = SlotSelectionMode.Continue;
        OpenAndRefresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OpenAndRefresh()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        RefreshAllSlots();
    }

    private void BindSlotButtons()
    {
        if (slotViews == null)
            return;

        slotClickHandlers = new UnityAction[slotViews.Length];

        for (int i = 0; i < slotViews.Length; i++)
        {
            SaveSlotView view = slotViews[i];
            if (view == null || view.slotButton == null)
                continue;

            int slotIndex = i;
            slotClickHandlers[i] = () => HandleSlotClicked(slotIndex);
            view.slotButton.onClick.AddListener(slotClickHandlers[i]);
        }
    }

    private void UnbindSlotButtons()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            SaveSlotView view = slotViews[i];
            if (view != null && view.slotButton != null && slotClickHandlers != null && i < slotClickHandlers.Length && slotClickHandlers[i] != null)
                view.slotButton.onClick.RemoveListener(slotClickHandlers[i]);
        }

        slotClickHandlers = null;
    }

    private void RefreshAllSlots()
    {
        int viewCount = slotViews != null ? slotViews.Length : 0;
        if (viewCount != CheckpointSaveSystem.SlotCount)
        {
            Debug.LogWarning(
                $"{name}: ожидается {CheckpointSaveSystem.SlotCount} slot views, сейчас назначено {viewCount}.",
                this);
        }

        for (int i = 0; i < viewCount; i++)
        {
            RefreshSlot(i, slotViews[i]);
        }
    }

    private void RefreshSlot(int slotIndex, SaveSlotView view)
    {
        if (view == null)
            return;

        bool hasSave = CheckpointSaveSystem.TryLoad(slotIndex, out CheckpointSaveData data);
        bool canClick = currentMode == SlotSelectionMode.NewGame || hasSave;

        if (view.slotButton != null)
            view.slotButton.interactable = canClick;

        if (view.occupiedState != null)
            view.occupiedState.SetActive(hasSave);

        if (view.emptyState != null)
            view.emptyState.SetActive(!hasSave);

        if (!hasSave)
            return;

        if (view.characterIcon != null && view.characterIcon.sprite == null && defaultCharacterSprite != null)
            view.characterIcon.sprite = defaultCharacterSprite;

        if (view.levelText != null)
            view.levelText.text = $"Уровень {Mathf.Max(1, data.playerLevel)}";

        if (view.healthText != null)
            view.healthText.text = $"ОЗ {Mathf.CeilToInt(Mathf.Max(0f, data.health))}";

        if (view.sceneText != null)
            view.sceneText.text = ResolveSceneLabel(data);
    }

    private static string ResolveSceneLabel(CheckpointSaveData data)
    {
        if (data == null)
            return "Неизвестно";

        if (data.savedFromLevelExit && !string.IsNullOrWhiteSpace(data.nextSceneName))
            return data.nextSceneName;

        if (!string.IsNullOrWhiteSpace(data.completedSceneName))
            return data.completedSceneName;

        return "Неизвестно";
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError($"{name}: GameManager.Instance == null. Выбор save-слота невозможен.", this);
            return;
        }

        if (currentMode == SlotSelectionMode.NewGame)
        {
            GameManager.Instance.StartNewGameInSlot(slotIndex);
            return;
        }

        GameManager.Instance.TryContinueFromSlot(slotIndex);
    }
}
