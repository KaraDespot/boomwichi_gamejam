/*
 * IngredientFxUtility
 * Назначение: безопасная привязка VFX к ингредиенту или сендвичу.
 * Что делает: сохраняет масштаб префаба, переводит частицы в Local space и при необходимости сжимает shape.
 * Связи: MoldSystem, CockroachContaminationSystem.
 * Паттерны: Static Utility.
 */

using UnityEngine;

public static class IngredientFxUtility
{
    private const float MaxShapeLength = 0.28f;
    private const float MaxShapeRadius = 0.12f;
    private const float MaxStartSpeed = 0.35f;

    public static GameObject Attach(
        GameObject prefab,
        Transform parent,
        Vector3 localOffset,
        float extraScaleMultiplier = 1f,
        bool loop = true,
        bool playOnAttach = true,
        float? particleStartSize = null)
    {
        if (prefab == null || parent == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.transform.localPosition = localOffset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, extraScaleMultiplier);
        ConfigureAttachedParticles(instance, loop, playOnAttach, particleStartSize);
        return instance;
    }

    public static void ConfigureAttachedParticles(
        GameObject fxRoot,
        bool loop,
        bool playOnAttach,
        float? particleStartSize = null)
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
            main.scalingMode = ParticleSystemScalingMode.Local;

            if (loop)
                main.loop = true;

            ApplyParticleTuning(particleSystem, particleStartSize);

            if (playOnAttach)
            {
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }
    }

    public static void StopEmitting(GameObject fxRoot)
    {
        if (fxRoot == null)
            return;

        ParticleSystem[] particleSystems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem != null)
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private static void ApplyParticleTuning(ParticleSystem particleSystem, float? particleStartSize)
    {
        ParticleSystem.MainModule main = particleSystem.main;

        if (particleStartSize.HasValue &&
            main.startSize.mode == ParticleSystemCurveMode.Constant)
        {
            main.startSize = Mathf.Max(0.05f, particleStartSize.Value);
        }

        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant && main.startSpeed.constant > MaxStartSpeed)
            main.startSpeed = MaxStartSpeed;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        if (!shape.enabled)
            return;

        if (shape.length > MaxShapeLength)
            shape.length = MaxShapeLength;

        if (shape.radius > MaxShapeRadius)
            shape.radius = MaxShapeRadius;
    }
}
