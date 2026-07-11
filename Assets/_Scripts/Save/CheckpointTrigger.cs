using UnityEngine;

/// <summary>
/// CheckpointTrigger
/// Что делает: сохраняет прогресс при входе игрока в отдельную safe-point зону.
/// Зачем нужен в игре: даёт второй вариант checkpoint-модели кроме сохранения на выходе уровня.
/// Связи: Collider trigger, tag игрока, опциональный EncounterTrigger, GameManager и CheckpointSaveSystem.
/// Как используется: объект с Collider(Is Trigger) ставится в сцену там, где сохранение считается безопасным.
/// Расширения: визуальный эффект активации, одноразовые checkpoint, разные слоты для разных safe-point зон.
/// Совет: для коридорного уровня обычно достаточно save на выходе; для хаба или длинной карты удобнее отдельная точка.
/// Совет: чаще всего ломаются выключенный объект, неверный tag, снятый Is Trigger или отключённые collision matrix слои.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Фильтр игрока")]
    [Tooltip("Tag, который считается игроком для активации checkpoint.")]
    [SerializeField] private string requiredTag = "Player";

    [Header("Правило safe-point")]
    [Tooltip("Если включено, сохранение разрешено только после завершения указанного encounter.")]
    [SerializeField] private bool requireEncounterCompleted;

    [Tooltip("Encounter, завершение которого делает checkpoint безопасным. Если поле пустое, проверяется отсутствие активных encounter.")]
    [SerializeField] private EncounterTrigger requiredEncounter;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private void Reset()
    {
        Collider checkpointCollider = GetComponent<Collider>();
        checkpointCollider.isTrigger = true;
    }

    /// <summary>
    /// Контракт: Unity вызывает метод при входе collider в trigger.
    /// Входные условия: объект активен, collider настроен как Is Trigger, игрок имеет нужный tag.
    /// Шаги: проверить игрока, проверить безопасное состояние encounter, вызвать GameManager save.
    /// Типичные поломки: не тот tag, слои не сталкиваются, requiredEncounter не завершён, GameManager отсутствует.
    /// Что проверить: Inspector объекта checkpoint, tag Player, Collider/Is Trigger, Console-логи слота.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (!CanSaveByEncounterRule())
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{name}: GameManager.Instance не найден. Checkpoint не может сохранить прогресс.", this);
            return;
        }

        bool saved = GameManager.Instance.TrySaveCheckpointProgress(transform.position);
        if (showDebugLogs && saved)
            Debug.Log("Checkpoint saved.", this);
    }

    /// <summary>
    /// Контракт: возвращает true, если checkpoint-зона считается безопасной для записи прогресса.
    /// Почему так: сам save не должен знать правила конкретной комнаты, эти правила живут на trigger-объекте.
    /// Потенциальное применение: checkpoint после arena encounter или safe-room без active encounter.
    /// </summary>
    private bool CanSaveByEncounterRule()
    {
        if (!requireEncounterCompleted)
            return true;

        if (requiredEncounter != null)
        {
            if (requiredEncounter.IsEncounterCompleted)
                return true;

            if (showDebugLogs)
                Debug.Log($"{name}: checkpoint ожидает завершения encounter '{requiredEncounter.name}'.", this);

            return false;
        }

        EncounterTrigger[] encounters = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < encounters.Length; i++)
        {
            if (encounters[i] != null && encounters[i].IsEncounterRunning)
            {
                if (showDebugLogs)
                    Debug.Log($"{name}: checkpoint пропущен, потому что encounter ещё активен.", this);

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Контракт: проверяет, что collider принадлежит игроку по tag на самом collider, Rigidbody или root.
    /// Почему так: в Unity collider часто находится на дочернем объекте, а tag может быть на корне player prefab.
    /// Потенциальное применение: та же проверка подходит для выходов, heal-зон и pickup-trigger.
    /// </summary>
    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (string.IsNullOrWhiteSpace(requiredTag))
            return true;

        if (other.CompareTag(requiredTag))
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(requiredTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(requiredTag);
    }
}
