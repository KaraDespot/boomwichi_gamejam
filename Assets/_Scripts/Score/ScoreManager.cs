/*
 * ScoreManager
 * Назначение: учёт чаевых и счётчиков дня.
 * Что делает: накапливает tips, считает заказы для финальной статистики.
 * Связи: OrderManager, GameplayHUDController, будущий ResultsUI.
 * Паттерны: Singleton, Observer через EventBus.
 */

using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int totalTips;
    private int completedOrders;
    private int failedOrders;

    public int TotalTips => totalTips;
    public int CompletedOrders => completedOrders;
    public int FailedOrders => failedOrders;

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
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnOrderCompleted -= HandleOrderCompleted;
            EventBus.Instance.OnOrderFailed -= HandleOrderFailed;
            EventBus.Instance.OnLevelLoaded -= HandleLevelLoaded;
        }
    }

    public void ResetDayScore()
    {
        totalTips = 0;
        completedOrders = 0;
        failedOrders = 0;
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
