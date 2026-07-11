using Dossamer.Dialogue;
using Dossamer.Dialogue.Schema;
using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable CS0649

/// <summary>
/// NPCDialogueTrigger
/// Что делает: превращает NPC в точку запуска Cutscene из пакета Dead Simple Dialogue.
/// Зачем нужен в игре: NPC появляется после encounter и запускает отдельный dialogue asset, не храня текст в коде.
/// Связи: Collider trigger, DialogueManager/Cutscene из Dead Simple Dialogue, PlayerProgression для опциональной XP-награды.
/// Как используется: компонент висит на NPCBase; конкретный диалог назначается в поле dialogueAsset через Inspector.
/// Потенциальные расширения: разные диалоги до/после quest, несколько rewards, связь выбора с inventory или quest-флагами.
/// Совет: если NPC не реагирует, проверить tag Player, радиус trigger collider и активность объекта после encounter.
/// Совет: если диалог не запускается, проверить поле dialogueAsset и наличие DialogueRuntime/DialogueManager в сцене.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class NPCDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Cutscene asset из Dead Simple Dialogue. У каждого NPC может быть свой asset с собственным текстом.")]
    [SerializeField] private Cutscene dialogueAsset;

    [Tooltip("Если включено, NPC запускает назначенный диалог только один раз за runtime-сессию.")]
    [SerializeField] private bool playOnlyOnce = true;

    [Header("Reward")]
    [Tooltip("Опыт, который выдаётся после завершения назначенного диалога. Значение 0 отключает XP-награду.")]
    [SerializeField] private float experienceReward = 50f;

    private bool playerInside;
    private bool dialogueStarted;
    private bool rewardGranted;

    public Cutscene DialogueAsset => dialogueAsset;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    /// <summary>
    /// Контракт: проверяет локальное взаимодействие с NPC каждый кадр, пока игрок находится в trigger.
    /// Polling клавиши E в Update — осознанное упрощение для урока, чтобы не смешивать InputManager проекта и API внешнего пакета.
    /// Гарантирует только запуск назначенного dialogueAsset; выдача награды происходит отдельно после события окончания Cutscene.
    /// Не гарантирует блокировку ввода во время паузы или другого UI: для production-версии нужна проверка GameState/UI state.
    /// Типичные поломки: у игрока нет tag Player, collider не isTrigger, dialogueAsset == null, playOnlyOnce уже заблокировал повторный старт.
    /// Почему так: старт, проигрывание и gameplay-эффект разделены, чтобы внешний пакет не знал о PlayerProgression.
    /// Потенциальное применение: тот же шаблон подходит для сундука с loot-пакетом или quest giver.
    /// </summary>
    private void Update()
    {
        if (!playerInside || DialogueManager.Instance != null && DialogueManager.Instance.GetIsDialogueActive())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (playOnlyOnce && dialogueStarted)
        {
            return;
        }

        StartDialogue();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void StartDialogue()
    {
        if (dialogueAsset == null)
        {
            Debug.LogWarning($"{name}: dialogueAsset не назначен. Диалог NPC не может быть запущен.", this);
            return;
        }

        DialogueRuntime.EnsureReady();

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"{name}: DialogueManager не найден и не был создан DialogueRuntime.", this);
            return;
        }

        DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        DialogueManager.Instance.StartNewDialogue(dialogueAsset);
        dialogueStarted = true;
    }

    private void HandleDialogueEnded(Cutscene endedCutscene)
    {
        if (endedCutscene != dialogueAsset || rewardGranted)
        {
            return;
        }

        rewardGranted = true;

        if (experienceReward <= 0f)
        {
            return;
        }

        PlayerProgression progression = FindFirstObjectByType<PlayerProgression>();
        if (progression == null)
        {
            Debug.LogWarning($"{name}: PlayerProgression не найден, награда за диалог не выдана.", this);
            return;
        }

        progression.AddExperience(experienceReward);
        Debug.Log($"{name}: диалог завершён, игрок получил {experienceReward} XP через интеграционный адаптер.", this);
    }
}

#pragma warning restore CS0649
