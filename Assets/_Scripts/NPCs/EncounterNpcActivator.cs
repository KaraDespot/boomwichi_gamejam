using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EncounterNpcActivator
/// Что делает: слушает завершение encounter и активирует демонстрационного NPC для урока интеграции модуля.
/// Зачем нужен в игре: связывает новый dialogue-модуль с уже собранным core loop без изменений в EncounterTrigger.
/// Связи: EventBus.OnEncounterCompleted, активная gameplay-сцена и объект NPCBase.
/// Как используется: создаётся автоматически при запуске игры через RuntimeInitializeOnLoadMethod.
/// Потенциальные расширения: активировать разных NPC по encounterId, включать quest marker, сохранять состояние NPC в checkpoint.
/// Совет: если NPC не появляется, проверить событие OnEncounterCompleted в Console и наличие NPCBase в gameplay-сцене.
/// Совет: если NPC появляется слишком рано, проверить имя сцены и место вызова RaiseEncounterCompleted.
/// </summary>
public sealed class EncounterNpcActivator : MonoBehaviour
{
    private const string NpcName = "NPCBase";
    private static EncounterNpcActivator instance;

    [Header("NPC")]
    [Tooltip("Опциональная явная ссылка на NPC. Если поле пустое, используется учебный fallback-поиск объекта NPCBase по имени.")]
    [SerializeField] private GameObject npcOverride;

    private GameObject sceneNpc;
    private bool encounterCompleted;
    private bool eventBusBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeActivator()
    {
        if (instance != null)
        {
            return;
        }

        GameObject activatorObject = new GameObject("Encounter NPC Activator");
        instance = activatorObject.AddComponent<EncounterNpcActivator>();
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        StartCoroutine(BindEventBusWhenReady());
        ResolveNpcForCurrentScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (eventBusBound && EventBus.Instance != null)
        {
            EventBus.Instance.OnEncounterCompleted -= HandleEncounterCompleted;
        }
    }

    /// <summary>
    /// Контракт: ждёт EventBus, потому что Bootstrap может создать singleton раньше или позже этого runtime-компонента.
    /// Гарантирует единственную подписку на событие завершения encounter.
    /// Почему так: интеграционный слой не требует ручной сцены-обвязки и не зависит от порядка загрузки Bootstrap/GameScene.
    /// Потенциальное применение: похожее ожидание полезно для пакетов, которые должны подключаться к GameManager или SceneLoader.
    /// </summary>
    private IEnumerator BindEventBusWhenReady()
    {
        while (EventBus.Instance == null)
        {
            yield return null;
        }

        if (eventBusBound)
        {
            yield break;
        }

        EventBus.Instance.OnEncounterCompleted += HandleEncounterCompleted;
        eventBusBound = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        encounterCompleted = false;
        ResolveNpcForCurrentScene();
    }

    private void HandleEncounterCompleted(string encounterId)
    {
        encounterCompleted = true;
        ActivateNpcIfPossible();
    }

    /// <summary>
    /// Контракт: находит NPC для текущей gameplay-сцены и синхронизирует его active state с encounterCompleted.
    /// Сначала используется npcOverride, если ссылка назначена и объект принадлежит активной сцене.
    /// Fallback ищет объект по имени NPCBase только в сцене, имя которой начинается с GameScene.
    /// Риск: переименование объекта NPCBase в сцене ломает fallback-активацию.
    /// Что проверить: объект NPCBase находится в сцене inactive до encounter, а EventBus.OnEncounterCompleted появляется в Console/логике EncounterTrigger.
    /// </summary>
    private void ResolveNpcForCurrentScene()
    {
        sceneNpc = null;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.StartsWith("GameScene"))
        {
            return;
        }

        if (npcOverride != null && npcOverride.scene == activeScene)
        {
            sceneNpc = npcOverride;
            sceneNpc.SetActive(encounterCompleted);
            return;
        }

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject candidate in allObjects)
        {
            if (candidate.scene == activeScene && candidate.name == NpcName)
            {
                sceneNpc = candidate;
                sceneNpc.SetActive(encounterCompleted);
                break;
            }
        }
    }

    private void ActivateNpcIfPossible()
    {
        if (sceneNpc == null)
        {
            ResolveNpcForCurrentScene();
        }

        if (sceneNpc == null)
        {
            Debug.LogWarning("EncounterNpcActivator: NPCBase не найден в активной gameplay-сцене.");
            return;
        }

        sceneNpc.SetActive(true);
        DialogueRuntime.EnsureReady();
        Debug.Log("EncounterNpcActivator: NPCBase активирован после завершения encounter.");
    }
}
