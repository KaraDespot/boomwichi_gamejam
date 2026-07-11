using System.Collections.Generic;
using System.Reflection;
using Dossamer.Dialogue;
using Dossamer.Dialogue.Schema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// DialogueRuntime
/// Что делает: создаёт минимальный runtime-UI для пакета Dead Simple Dialogue, если готовый DialogueManager не найден в сцене.
/// Зачем нужен в игре: позволяет подключить внешний dialogue-модуль без ручной сборки сложной UI-сцены для демонстрационного NPC.
/// Связи: работает с DialogueManager/TextIterator из Dead Simple Dialogue, TextMeshPro, Canvas и Input System.
/// Как используется: NPCDialogueTrigger вызывает EnsureReady перед запуском Cutscene, а этот класс подготавливает менеджер и кнопку продолжения.
/// Потенциальные расширения: заменить runtime-UI на prefab, добавить portraits для персонажей, вынести стиль окна в ScriptableObject.
/// Потенциальное применение: тот же паттерн EnsureReady можно использовать для bootstrap готовых inventory, loot или quest-пакетов.
/// Совет: если диалог не открывается, проверить наличие TextMeshPro Essentials и отсутствие второго DialogueManager в активной сцене.
/// Совет: если текст не листается, проверить Input System и Console на ошибки создания UI.
/// </summary>
public sealed class DialogueRuntime : MonoBehaviour
{
    private static DialogueRuntime instance;

    [Header("Runtime UI")]
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 190f);
    [SerializeField] private Vector2 panelOffset = new Vector2(0f, 70f);

    private DialogueManager dialogueManager;

    /// <summary>
    /// Контракт: метод можно вызывать перед стартом любого NPC-диалога.
    /// Гарантирует наличие DialogueManager, Canvas, TextIterator и CharacterBank.
    /// Не гарантирует художественный финальный UI: это учебный минимальный слой интеграции.
    /// Почему так: готовый пакет остаётся внешним модулем, а проект создаёт только недостающий glue-код.
    /// Риск: EnsureDialogueManager использует Reflection для private-полей пакета; при обновлении Dead Simple Dialogue имена полей могут измениться.
    /// Что проверить при поломке: имена _textPanel, _namePanel, _portraitPanel, _characterBank в DialogueManager или готовый prefab/UI из пакета.
    /// </summary>
    public static DialogueRuntime EnsureReady()
    {
        if (instance != null)
        {
            instance.EnsureDialogueManager();
            return instance;
        }

        GameObject runtimeObject = new GameObject("Dialogue Runtime");
        instance = runtimeObject.AddComponent<DialogueRuntime>();
        instance.EnsureDialogueManager();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Контракт: листает диалог только пока DialogueManager существует и GetIsDialogueActive() возвращает true.
    /// Почему Input System здесь: vendor-пакет не знает о локальном InputManager проекта, поэтому glue-слой даёт минимальное управление Space/Enter/Mouse.
    /// Риск: пакет не знает GameState, поэтому без дополнительной логики диалоговый ввод может конфликтовать с паузой или другим UI.
    /// Что проверить: активен ли диалог, нет ли второго DialogueManager в сцене, не перехватывает ли пауза тот же ввод.
    /// </summary>
    private void Update()
    {
        if (DialogueManager.Instance == null || !DialogueManager.Instance.GetIsDialogueActive())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool shouldProgress =
            keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) ||
            mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (shouldProgress)
        {
            DialogueManager.Instance.UpdateDialogue();
        }
    }

    /// <summary>
    /// Контракт: создаёт DialogueManager и минимальный Canvas, если готовый менеджер пакета не найден в сцене.
    /// Почему используется Reflection: это учебный bootstrap без готового prefab пакета, а нужные ссылки в DialogueManager закрыты private-полями.
    /// Риск: обновление Dead Simple Dialogue может переименовать private-поля _textPanel, _namePanel, _portraitPanel или _characterBank.
    /// Что делать при поломке: сверить имена полей в DialogueManager или заменить этот временный glue-слой на prefab/UI из пакета.
    /// Пометка: это удобный демонстрационный мост для урока, а не канон финальной production-архитектуры.
    /// </summary>
    private void EnsureDialogueManager()
    {
        if (DialogueManager.Instance != null)
        {
            dialogueManager = DialogueManager.Instance;
            return;
        }

        GameObject managerObject = new GameObject("DialogueManager");
        managerObject.transform.SetParent(transform);
        dialogueManager = managerObject.AddComponent<DialogueManager>();

        Canvas canvas = CreateDialogueCanvas(managerObject.transform, out TextIterator textIterator, out TextMeshProUGUI namePanel, out RawImage portraitPanel);
        CharacterBank characterBank = CreateCharacterBank();

        dialogueManager.DialogueCanvas = canvas.gameObject;
        SetPrivateField(dialogueManager, "_textPanel", textIterator);
        SetPrivateField(dialogueManager, "_namePanel", namePanel);
        SetPrivateField(dialogueManager, "_portraitPanel", portraitPanel);
        SetPrivateField(dialogueManager, "_characterBank", characterBank);

        characterBank.RefreshMap();
        canvas.gameObject.SetActive(false);
    }

    private Canvas CreateDialogueCanvas(Transform parent, out TextIterator textIterator, out TextMeshProUGUI namePanel, out RawImage portraitPanel)
    {
        GameObject canvasObject = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("DialoguePanel", typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = panelOffset;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.07f, 0.92f);

        GameObject portraitObject = new GameObject("Portrait", typeof(RawImage));
        portraitObject.transform.SetParent(panelObject.transform, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(24f, 0f);
        portraitRect.sizeDelta = new Vector2(128f, 128f);
        portraitPanel = portraitObject.GetComponent<RawImage>();
        portraitPanel.color = new Color(0.35f, 0.55f, 0.9f, 0.45f);

        GameObject nameObject = new GameObject("SpeakerName", typeof(TextMeshProUGUI));
        nameObject.transform.SetParent(panelObject.transform, false);
        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(176f, -20f);
        nameRect.sizeDelta = new Vector2(-210f, 38f);
        namePanel = nameObject.GetComponent<TextMeshProUGUI>();
        namePanel.fontSize = 28f;
        namePanel.color = new Color(0.95f, 0.82f, 0.44f, 1f);
        namePanel.text = string.Empty;

        GameObject textObject = new GameObject("DialogueText", typeof(TextMeshProUGUI), typeof(TextIterator));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(176f, -66f);
        textRect.sizeDelta = new Vector2(-210f, -90f);

        TextMeshProUGUI textPanel = textObject.GetComponent<TextMeshProUGUI>();
        textPanel.fontSize = 24f;
        textPanel.color = Color.white;
        textPanel.textWrappingMode = TextWrappingModes.Normal;
        textPanel.text = string.Empty;

        textIterator = textObject.GetComponent<TextIterator>();
        return canvas;
    }

    private static CharacterBank CreateCharacterBank()
    {
        CharacterBank bank = ScriptableObject.CreateInstance<CharacterBank>();
        bank.Initialize(new List<Character>
        {
            new Character("Проводник"),
            new Character("Игрок")
        });

        return bank;
    }

    /// <summary>
    /// Контракт: записывает ссылку в private-поле DialogueManager, если поле с таким именем существует.
    /// Почему используется Reflection: внешний пакет не открывает публичный API для runtime-подстановки UI-ссылок.
    /// Риск: обновление пакета может изменить имена private-полей и сломать интеграционный bootstrap.
    /// Что проверить: Console с сообщением DialogueRuntime, затем актуальный код DialogueManager или готовый prefab из пакета.
    /// </summary>
    private static void SetPrivateField<TValue>(DialogueManager manager, string fieldName, TValue value)
    {
        FieldInfo field = typeof(DialogueManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError($"DialogueRuntime: поле {fieldName} не найдено в DialogueManager.");
            return;
        }

        field.SetValue(manager, value);
    }
}
