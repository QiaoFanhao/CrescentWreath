using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class EffectVfxPlayer : MonoBehaviour
{
    private const float LargeBoundsWarningThreshold = 4.5f;
    private readonly Dictionary<GameObject, Queue<GameObject>> poolByPrefab = new();
    private readonly Dictionary<GameObject, GameObject> prefabByInstance = new();

    public void Play(
        EffectVfxCatalog.Entry? entry,
        Transform? target,
        Transform? tableCenter,
        WorldCardView? sourceCard = null,
        string categoryKey = "",
        bool usedFallback = false)
    {
        if (entry is null || entry.prefab is null)
        {
            Debug.LogWarning("[EffectVfxPlayer] Missing VFX entry or prefab; using no-op fallback.");
            return;
        }

        var shouldAttachLocal = entry.instanceAttachMode == EffectVfxInstanceAttachMode.attachLocal &&
                                entry.followTarget &&
                                target is not null;
        var parent = shouldAttachLocal
            ? target
            : null;
        var instance = rentInstance(entry.prefab, parent);
        if (parent is not null)
        {
            instance.transform.localPosition = entry.localOffset;
            instance.transform.localRotation = Quaternion.Euler(entry.localEulerAngles);
            instance.transform.localScale = entry.localScale;
        }
        else
        {
            var origin = target is not null
                ? target.position
                : tableCenter is not null
                    ? tableCenter.position
                    : Vector3.zero;
            var baseRotation = target is not null ? target.rotation : Quaternion.identity;
            instance.transform.position = target is not null
                ? target.TransformPoint(entry.localOffset)
                : origin + entry.localOffset;
            instance.transform.rotation = baseRotation * Quaternion.Euler(entry.localEulerAngles);
            instance.transform.localScale = entry.localScale;
        }

        if (entry.useBillboardToCamera && Camera.main != null)
        {
            instance.transform.rotation = Quaternion.LookRotation(instance.transform.position - Camera.main.transform.position, Vector3.up);
        }

        applySimulationSpace(instance, entry);
        warnIfPrefabAppearsTooLarge(instance, entry, categoryKey);
        Debug.Log(
            $"[EffectVfxPlayer] play category={categoryKey} effect={entry.effectCategoryKey} debugName={entry.debugName} prefab={entry.prefab.name} " +
            $"sourceCard={(sourceCard is null ? "(none)" : $"{sourceCard.DefinitionId}#{sourceCard.CardInstanceNumericId}")} " +
            $"visualKey={(sourceCard is null ? "(none)" : sourceCard.VisualKey)} zone={(sourceCard is null ? "(none)" : sourceCard.ZoneViewKey)} " +
            $"anchor={(target is null ? "(none)" : target.position.ToString("F3"))} anchorRot={(target is null ? "(none)" : target.rotation.eulerAngles.ToString("F1"))} " +
            $"fallback={usedFallback} instanceAttach={entry.instanceAttachMode} placement={entry.attachMode} " +
            $"offset={entry.localOffset.ToString("F3")} euler={entry.localEulerAngles.ToString("F1")} " +
            $"scale={entry.localScale.ToString("F2")} debugColor={entry.debugColor}");
        restartParticleSystems(instance);
        if (entry.autoDestroy)
        {
            StartCoroutine(returnAfterDuration(instance, entry.duration, entry.stopLoopOnComplete));
        }
    }

    private GameObject rentInstance(GameObject prefab, Transform? parent)
    {
        if (!poolByPrefab.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            poolByPrefab[prefab] = pool;
        }

        while (pool.Count > 0)
        {
            var pooledInstance = pool.Dequeue();
            if (pooledInstance != null)
            {
                pooledInstance.transform.SetParent(parent, false);
                pooledInstance.SetActive(true);
                prefabByInstance[pooledInstance] = prefab;
                return pooledInstance;
            }
        }

        var instance = Instantiate(prefab, parent);
        prefabByInstance[instance] = prefab;
        return instance;
    }

    private IEnumerator returnAfterDuration(GameObject instance, float duration, bool stopLoopOnComplete)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        if (instance == null)
        {
            yield break;
        }

        if (stopLoopOnComplete)
        {
            foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        yield return new WaitForSeconds(0.35f);
        returnInstance(instance);
    }

    private void returnInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        stopAndClearParticleSystems(instance);
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        if (!prefabByInstance.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Destroy(instance);
            return;
        }

        if (!poolByPrefab.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            poolByPrefab[prefab] = pool;
        }

        pool.Enqueue(instance);
    }

    private static void restartParticleSystems(GameObject instance)
    {
        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private static void stopAndClearParticleSystems(GameObject instance)
    {
        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private static void warnIfPrefabAppearsTooLarge(GameObject instance, EffectVfxCatalog.Entry entry, string categoryKey)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (var index = 1; index < renderers.Length; index += 1)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        var maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize <= LargeBoundsWarningThreshold)
        {
            return;
        }

        Debug.LogWarning(
            $"[EffectVfxPlayer] VFX prefab appears too large. category={categoryKey} " +
            $"prefab={(entry.prefab == null ? "(none)" : entry.prefab.name)} bounds={bounds.size.ToString("F2")} " +
            $"profileScale={entry.localScale.ToString("F2")}");
    }

    private static void applySimulationSpace(GameObject instance, EffectVfxCatalog.Entry entry)
    {
        var resolvedMode = entry.simulationSpaceMode == EffectVfxSimulationSpaceMode.inherit
            ? entry.followTarget
                ? EffectVfxSimulationSpaceMode.local
                : EffectVfxSimulationSpaceMode.world
            : entry.simulationSpaceMode;
        var unityMode = resolvedMode == EffectVfxSimulationSpaceMode.local
            ? ParticleSystemSimulationSpace.Local
            : ParticleSystemSimulationSpace.World;

        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.simulationSpace = unityMode;
        }
    }
}
}
