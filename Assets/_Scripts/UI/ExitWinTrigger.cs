using UnityEngine;

/*
 * ExitWinTrigger
 * Назначение: trigger-точка, через которую игрок завершает уровень (win path).
 * Что делает: при входе игрока запрашивает победу у GameLoopFlowController и передаёт позицию выхода
 * для checkpoint-сохранения завершённого уровня.
 * Связи: GameLoopFlowController (RequestWinFromExit), коллайдер игрока и tag-фильтр.
 * Как используется: объект выхода активируется после encounter и ждёт входа игрока в trigger.
 * Расширения: эффект портала, разные типы выходов, подсказка UI перед переходом.
 * Совет: если сохранение не происходит, проверить active state выхода и что winAccepted возвращает true.
 */
public class ExitWinTrigger : MonoBehaviour
{
    [Tooltip("Контроллер игрового flow, который принимает решение о win.")]
    [SerializeField] private GameLoopFlowController flowController;

    [Tooltip("Tag, который считается игроком для активации выхода.")]
    [SerializeField] private string requiredTag = "Player";

    [Tooltip("Показывать ли отладочные сообщения в консоли.")]
    [SerializeField] private bool showDebugLogs = true;

    /// <summary>
    /// Автопоиск контроллера при добавлении компонента в сцену.
    /// </summary>
    private void Reset()
    {
        if (flowController == null)
            flowController = FindFirstObjectByType<GameLoopFlowController>();
    }

    /// <summary>
    /// Контракт: вызывается Unity при входе collider в trigger завершения уровня.
    /// Входные условия: объект выхода активен, collider принадлежит игроку, flowController назначен.
    /// Шаги: отфильтровать игрока, передать позицию выхода, дождаться ответа winAccepted.
    /// Типичные поломки: неверный tag, Collider не Is Trigger, flowController не назначен, выход ещё выключен.
    /// Что проверить: Inspector выхода, tag Player, active state ExitUnlockedMarker, Console-лог winAccepted.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (!IsPlayerCollider(other))
            return;

        if (flowController == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: flowController is not assigned.", this);

            return;
        }

        bool winAccepted = flowController.RequestWinFromExit(transform.position);

        if (showDebugLogs && winAccepted)
            Debug.Log($"{name}: player entered exit trigger, win requested.", this);
    }

    /// <summary>
    /// Проверка, что вошедший collider принадлежит игроку.
    /// Поддерживает проверку самого collider, его rigidbody и root-transform.
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
