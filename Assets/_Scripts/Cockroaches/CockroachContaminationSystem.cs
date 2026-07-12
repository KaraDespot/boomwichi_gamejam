/*
 * CockroachContaminationSystem
 * Назначение: заражение сендвича на тарелке при контакте с тараканом.
 * Что делает: отслеживает дистанцию таракан ↔ сендвич, включает toxic-FX и флаг грязи.
 * Связи: SandwichBoard, SandwichState, Cockroach, EventBus.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections.Generic;
using UnityEngine;

public class CockroachContaminationSystem : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Доска/тарелка со сборкой сендвича. Если пусто — ищется SandwichBoard на сцене.")]
    [SerializeField] private SandwichBoard sandwichBoard;

    [Header("Контакт")]
    [Tooltip("Горизонтальная дистанция, на которой таракан заражает сендвич.")]
    [SerializeField] private float contaminationRadius = 0.38f;

    [Header("FX")]
    [Tooltip("Постоянный toxic-эффект на заражённом сендвиче.")]
    [SerializeField] private GameObject sandwichToxicFxPrefab;

    [Tooltip("Смещение FX над центром сендвича.")]
    [SerializeField] private Vector3 toxicFxLocalOffset = new Vector3(0f, 0.08f, 0f);

    [Tooltip("Заставить toxic-частицы зациклиться в рантайме.")]
    [SerializeField] private bool forceToxicFxLoop = true;

    private readonly Dictionary<SandwichState, GameObject> toxicFxBySandwich = new();

    private void Awake()
    {
        if (sandwichBoard == null)
            sandwichBoard = FindFirstObjectByType<SandwichBoard>();
    }

    private void OnDisable()
    {
        ClearAllFx();
    }

    private void Update()
    {
        CleanupDestroyedSandwiches();

        if (!IsGameplayActive())
            return;

        SandwichState sandwichState = GetActiveSandwich();
        if (sandwichState == null || sandwichState.HasRoachContact)
            return;

        if (IsCockroachTouchingSandwich(sandwichState))
            ApplyContamination(sandwichState);
    }

    private bool IsGameplayActive()
    {
        if (GameManager.Instance == null)
            return true;

        if (GameManager.Instance.CurrentState == GameState.Paused)
            return false;

        DayFlowState dayState = GameManager.Instance.CurrentDayState;
        return dayState == DayFlowState.PlayingOrder || dayState == DayFlowState.ShowingOrder;
    }

    private SandwichState GetActiveSandwich()
    {
        if (sandwichBoard == null || !sandwichBoard.HasBottomBread)
            return null;

        return sandwichBoard.CurrentSandwich;
    }

    private bool IsCockroachTouchingSandwich(SandwichState sandwichState)
    {
        Vector3 sandwichPosition = sandwichState.transform.position;
        float radiusSq = contaminationRadius * contaminationRadius;

        IReadOnlyList<Cockroach> cockroaches = Cockroach.ActiveInstances;
        for (int i = 0; i < cockroaches.Count; i++)
        {
            Cockroach cockroach = cockroaches[i];
            if (cockroach == null || !cockroach.IsAlive)
                continue;

            Vector3 delta = cockroach.transform.position - sandwichPosition;
            delta.y = 0f;

            if (delta.sqrMagnitude <= radiusSq)
                return true;
        }

        return false;
    }

    private void ApplyContamination(SandwichState sandwichState)
    {
        if (sandwichState == null || sandwichState.HasRoachContact)
            return;

        sandwichState.MarkRoachContact();
        EnsureToxicFx(sandwichState);

        if (EventBus.Instance != null)
            EventBus.Instance.RaiseSandwichCockroachContaminated(sandwichState);
    }

    private void EnsureToxicFx(SandwichState sandwichState)
    {
        if (sandwichToxicFxPrefab == null || sandwichState == null)
            return;

        if (toxicFxBySandwich.ContainsKey(sandwichState))
            return;

        GameObject fxInstance = Instantiate(sandwichToxicFxPrefab, sandwichState.transform);
        fxInstance.transform.localPosition = toxicFxLocalOffset;
        fxInstance.transform.localRotation = Quaternion.identity;
        ConfigureLoopingParticles(fxInstance);
        toxicFxBySandwich[sandwichState] = fxInstance;
    }

    private void ConfigureLoopingParticles(GameObject fxRoot)
    {
        if (fxRoot == null)
            return;

        ParticleSystem[] particleSystems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            if (forceToxicFxLoop)
                main.loop = true;

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ClearAllFx()
    {
        foreach (KeyValuePair<SandwichState, GameObject> pair in toxicFxBySandwich)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        toxicFxBySandwich.Clear();
    }

    private void CleanupDestroyedSandwiches()
    {
        if (toxicFxBySandwich.Count == 0)
            return;

        List<SandwichState> keys = new List<SandwichState>(toxicFxBySandwich.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            SandwichState sandwich = keys[i];
            if (sandwich != null)
                continue;

            if (toxicFxBySandwich.TryGetValue(sandwich, out GameObject fx) && fx != null)
                Destroy(fx);

            toxicFxBySandwich.Remove(sandwich);
        }
    }

    private void OnDrawGizmosSelected()
    {
        SandwichState sandwichState = sandwichBoard != null ? sandwichBoard.CurrentSandwich : null;
        if (sandwichState == null)
            return;

        Gizmos.color = new Color(0.45f, 0.85f, 0.2f, 0.35f);
        Vector3 center = sandwichState.transform.position + Vector3.up * 0.04f;
        Gizmos.DrawWireSphere(center, contaminationRadius);
    }
}
