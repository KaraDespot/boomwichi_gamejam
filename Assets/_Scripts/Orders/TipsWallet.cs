/*
 * TipsWallet
 * Назначение: простой счётчик чаевых за игровой день.
 * Что делает: принимает начисленные чаевые, хранит общий итог и публикует событие для будущего UI.
 * Связи: вызывается PackageZone после OrderEvaluator.
 * Паттерны: Scene Service, Event Publisher.
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TipsWallet : MonoBehaviour
{
    [Header("Tips")]
    [Tooltip("Стартовое количество чаевых для тестирования.")]
    [SerializeField] private int startingTips;

    public static TipsWallet Instance { get; private set; }

    public event Action<int, int> TipsChanged;

    public int TotalTips { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{name}: на сцене уже есть TipsWallet, этот экземпляр будет работать локально.", this);
        }
        else
        {
            Instance = this;
        }

        TotalTips = Mathf.Max(0, startingTips);
    }

    public void AddTips(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        TotalTips += safeAmount;
        TipsChanged?.Invoke(TotalTips, safeAmount);
    }

    public void ResetTips()
    {
        TotalTips = Mathf.Max(0, startingTips);
        TipsChanged?.Invoke(TotalTips, 0);
    }
}
