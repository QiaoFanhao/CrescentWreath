using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(fileName = "EffectPresentationCatalog", menuName = "Crescent Wreath/Effect Presentation Catalog")]
public sealed class EffectPresentationCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string effectCategoryKey = EffectCategoryKeys.GenericEffect;
        public string vfxKey = EffectCategoryKeys.GenericEffect;
        public string sfxKey = EffectCategoryKeys.GenericEffect;
        public string labelText = "????";
        [Min(0f)] public float totalDelayBeforeInteraction = 0.18f;
    }

    [SerializeField]
    private EffectVfxCatalog? vfxCatalog;

    [SerializeField]
    private EffectSfxCatalog? sfxCatalog;

    [SerializeField]
    private List<Entry> entries = new();

    private Dictionary<string, Entry>? entryByCategoryKey;

    public EffectVfxCatalog? VfxCatalog => vfxCatalog;
    public EffectSfxCatalog? SfxCatalog => sfxCatalog;
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
    public void ReplaceForEditor(EffectVfxCatalog? newVfxCatalog, EffectSfxCatalog? newSfxCatalog, IEnumerable<Entry> newEntries)
    {
        vfxCatalog = newVfxCatalog;
        sfxCatalog = newSfxCatalog;
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
            if (entry is null)
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
