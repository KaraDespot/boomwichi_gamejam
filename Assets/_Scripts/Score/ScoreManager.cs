/*
 * ScoreManager
 * Назначение: учёт чаевых и счётчиков дня.
 * Что делает: накапливает tips, считает заказы для финальной статистики.
 * Связи: OrderManager, GameplayHUDController, ResultsUI.
 * Паттерны: Singleton, Observer через EventBus.
 */

using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int totalTips;
    private int completedOrders;
    private int failedOrders;
    private int cockroachesSquashed;
    private int moldCleaned;
    private int dirtyIncidents;

    public int TotalTips => totalTips;
    public int CompletedOrders => completedOrders;
    public int FailedOrders => failedOrders;
    public int CockroachesSquashed => cockroachesSquashed;
    public int MoldCleaned => moldCleaned;
    public int DirtyIncidents => dirtyIncidents;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnOrderCompleted += HandleOrderCompleted;
            EventBus.Instance.OnOrderFailed += HandleOrderFailed;
            EventBus.Instance.OnLevelLoaded += HandleLevelLoaded;
            EventBus.Instance.OnCockroachKilled += HandleCockroachKilled;
            EventBus.Instance.OnMoldCleaned += HandleMoldCleaned;
            EventBus.Instance.OnMoldFallenOnSandwich += HandleDirtySandwich;
            EventBus.Instance.OnSandwichCockroachContaminated += HandleDirtySandwich;
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnOrderCompleted -= HandleOrderCompleted;
            EventBus.Instance.OnOrderFailed -= HandleOrderFailed;
            EventBus.Instance.OnLevelLoaded -= HandleLevelLoaded;
            EventBus.Instance.OnCockroachKilled -= HandleCockroachKilled;
            EventBus.Instance.OnMoldCleaned -= HandleMoldCleaned;
            EventBus.Instance.OnMoldFallenOnSandwich -= HandleDirtySandwich;
            EventBus.Instance.OnSandwichCockroachContaminated -= HandleDirtySandwich;
        }
    }

    public void ResetDayScore()
    {
        totalTips = 0;
        completedOrders = 0;
        failedOrders = 0;
        cockroachesSquashed = 0;
        moldCleaned = 0;
        dirtyIncidents = 0;
        PublishTipsChanged();
    }

    public void AddTips(int amount)
    {
        if (amount <= 0)
            return;

        totalTips += amount;
        PublishTipsChanged();
    }

    private void HandleOrderCompleted(int orderIndex, int tipsEarned)
    {
        completedOrders++;
    }

    private void HandleOrderFailed(int orderIndex)
    {
        failedOrders++;
    }

    private void HandleCockroachKilled(Cockroach cockroach)
    {
        cockroachesSquashed++;
    }

    private void HandleMoldCleaned(IngredientInstance ingredient)
    {
        moldCleaned++;
    }

    private void HandleDirtySandwich(SandwichState sandwichState)
    {
        dirtyIncidents++;
    }

    private void HandleLevelLoaded(string sceneName)
    {
        if (sceneName == SceneNames.GameScene)
            ResetDayScore();
    }

    private void PublishTipsChanged()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseTipsChanged(totalTips);
    }
}
