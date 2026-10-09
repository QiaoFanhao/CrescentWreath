using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(fileName = "EffectVfxCatalog", menuName = "Crescent Wreath/Effect VFX Catalog")]
public sealed class EffectVfxCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string effectCategoryKey = EffectCategoryKeys.GenericEffect;
        public GameObject? prefab;
        [Min(0.05f)] public float duration = 0.8f;
        public EffectVfxAttachMode attachMode = EffectVfxAttachMode.cardSurface;
        public EffectVfxInstanceAttachMode instanceAttachMode = EffectVfxInstanceAttachMode.attachLocal;
        public Vector3 localOffset = new(0f, 0.03f, 0f);
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;
        public bool followTarget = true;
        public bool useBillboardToCamera;
        public EffectVfxSimulationSpaceMode simulationSpaceMode = EffectVfxSimulationSpaceMode.inherit;
        public bool autoDestroy = true;
        public Color debugColor = Color.cyan;
        public string debugName = string.Empty;
        public bool stopLoopOnComplete = true;
    }

    [SerializeField]
    private List<Entry> entries = new();

    private Dictionary<string, Entry>? entryByCategoryKey;

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetEntry(string effectCategoryKey, out Entry? entry)
    {
        ensureLookup();
        if (entryByCategoryKey!.TryGetValue(normalize(effectCategoryKey), out entry))
        {
            return true;
        }

        return entryByCategoryKey.TryGetValue(EffectCategoryKeys.GenericEffect, out entry);
    }

#if UNITY_EDITOR
    public void ReplaceEntriesForEditor(IEnumerable<Entry> newEntries)
    {
        entries = new List<Entry>(newEntries);
        entryByCategoryKey = null;
    }
#endif

    private void OnEnable()
    {
        entryByCategoryKey = null;
    }

    private void ensureLookup()
    {
        if (entryByCategoryKey is not null)
        {
            return;
        }

        entryByCategoryKey = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry is null || entry.prefab is null)
            {
                continue;
            }

            entryByCategoryKey[normalize(entry.effectCategoryKey)] = entry;
        }
    }

    private static string normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? EffectCategoryKeys.GenericEffect
            : value.Trim();
    }
}
}
