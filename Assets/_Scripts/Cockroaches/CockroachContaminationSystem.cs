/*
 * CockroachContaminationSystem
 * Назначение: визуал заражения сендвича после контакта с тараканом.
 * Что делает: включает toxic-FX, когда таракан коснулся еды на тарелке.
 * Связи: SandwichBoard, SandwichState, EventBus.
 * Паттерны: Manager, Observer через EventBus.
 */

using System.Collections.Generic;
using UnityEngine;

public class CockroachContaminationSystem : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Доска/тарелка со сборкой сендвича. Если пусто — ищется SandwichBoard на сцене.")]
    [SerializeField] private SandwichBoard sandwichBoard;

    [Header("FX")]
    [Tooltip("Постоянный toxic-эффект на заражённом сендвиче.")]
    [SerializeField] private GameObject sandwichToxicFxPrefab;

    [Tooltip("Смещение FX над центром сендвича.")]
    [SerializeField] private Vector3 toxicFxLocalOffset = new Vector3(0f, 0.08f, 0f);

    [Tooltip("Заставить toxic-частицы зациклиться в рантайме.")]
    [SerializeField] private bool forceToxicFxLoop = true;

    [Tooltip("Множитель масштаба toxic-FX на сендвиче.")]
    [SerializeField] private float toxicFxScaleMultiplier = 0.88f;

    [Tooltip("Размер частиц toxic-FX.")]
    [SerializeField] private float toxicFxParticleSize = 0.34f;

    private readonly Dictionary<SandwichState, GameObject> toxicFxBySandwich = new();

    private void Awake()
    {
        if (sandwichBoard == null)
            sandwichBoard = FindFirstObjectByType<SandwichBoard>();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnSandwichCockroachContaminated += HandleSandwichContaminated;
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnSandwichCockroachContaminated -= HandleSandwichContaminated;

        ClearAllFx();
    }

    private void Update()
    {
        CleanupDestroyedSandwiches();
    }

    private void HandleSandwichContaminated(SandwichState sandwichState)
    {
        EnsureToxicFx(sandwichState);
    }

    private void EnsureToxicFx(SandwichState sandwichState)
    {
        if (sandwichToxicFxPrefab == null || sandwichState == null)
            return;

        if (toxicFxBySandwich.ContainsKey(sandwichState))
            return;

        GameObject fxInstance = IngredientFxUtility.Attach(
            sandwichToxicFxPrefab,
            sandwichState.transform,
            toxicFxLocalOffset,
            toxicFxScaleMultiplier,
            loop: forceToxicFxLoop,
            playOnAttach: true,
            particleStartSize: toxicFxParticleSize);
        toxicFxBySandwich[sandwichState] = fxInstance;
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
        Gizmos.DrawWireSphere(center, 0.28f);
    }
}
