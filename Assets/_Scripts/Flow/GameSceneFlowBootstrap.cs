/*
 * GameSceneFlowBootstrap
 * Назначение: инициализация игрового дня при загрузке GameScene.
 * Что делает: гарантирует наличие DayTimer, запускает flow GameManager, сбрасывает заказы.
 * Связи: GameManager, DayTimer, OrderManager, ScoreManager, TutorialUI, EventBus.
 * Паттерны: Bootstrap компонент сцены, композиция менеджеров дня.
 */

using System.Collections;
using UnityEngine;

public class GameSceneFlowBootstrap : MonoBehaviour
{
    [Header("Отладка")]
    [Tooltip("Пропустить туториал и сразу начать день. Игнорируется, если на сцене есть TutorialUI.")]
    [SerializeField] private bool autoStartDayForTesting;

    private DayTimer dayTimer;
    private OrderManager orderManager;
    private ScoreManager scoreManager;
    private bool sceneFlowInitialized;

    private void Awake()
    {
        dayTimer = GetComponent<DayTimer>();
        if (dayTimer == null)
            dayTimer = gameObject.AddComponent<DayTimer>();

        orderManager = GetComponent<OrderManager>();
        if (orderManager == null)
            orderManager = gameObject.AddComponent<OrderManager>();

        scoreManager = GetComponent<ScoreManager>();
        if (scoreManager == null)
            scoreManager = gameObject.AddComponent<ScoreManager>();
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

        if (orderManager != null)
            orderManager.ResetDayOrders();

        if (scoreManager != null)
            scoreManager.ResetDayScore();

        GameManager.Instance.BeginDayFlow();

        TutorialUI tutorialUi = FindFirstObjectByType<TutorialUI>();
        if (autoStartDayForTesting && tutorialUi == null)
            StartCoroutine(DeferredStartDay());
    }

    private IEnumerator DeferredStartDay()
    {
        yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.StartDay(dayTimer);
    }
}
