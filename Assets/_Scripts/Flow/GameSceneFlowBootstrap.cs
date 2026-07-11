/*
 * GameSceneFlowBootstrap
 * Назначение: инициализация игрового дня при загрузке GameScene.
 * Что делает: гарантирует наличие DayTimer, запускает flow GameManager, опционально пропускает туториал.
 * Связи: GameManager, DayTimer, EventBus.
 * Паттерны: Bootstrap компонент сцены, композиция менеджеров дня.
 */

using System.Collections;
using UnityEngine;

public class GameSceneFlowBootstrap : MonoBehaviour
{
    [Header("Отладка")]
    [Tooltip("Если включено, день стартует сразу без ожидания TutorialUI (для тестов до готовности туториала).")]
    [SerializeField] private bool autoStartDayForTesting;

    private DayTimer dayTimer;
    private bool sceneFlowInitialized;

    private void Awake()
    {
        dayTimer = GetComponent<DayTimer>();
        if (dayTimer == null)
            dayTimer = gameObject.AddComponent<DayTimer>();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void Start()
    {
        TryInitializeGameSceneFlow();
    }

    private void HandleLevelLoaded(string sceneName)
    {
        if (sceneName != SceneNames.GameScene)
            return;

        sceneFlowInitialized = false;
        TryInitializeGameSceneFlow();
    }

    private void TryInitializeGameSceneFlow()
    {
        if (sceneFlowInitialized)
            return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneNames.GameScene)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{name}: GameManager ещё не создан, flow GameScene отложен.", this);
            return;
        }

        sceneFlowInitialized = true;
        dayTimer.ResetTimer();
        GameManager.Instance.BeginDayFlow();

        if (autoStartDayForTesting)
            StartCoroutine(DeferredStartDay());
    }

    private IEnumerator DeferredStartDay()
    {
        yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.StartDay(dayTimer);
    }
}
