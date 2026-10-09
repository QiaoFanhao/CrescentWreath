using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(fileName = "EffectSfxCatalog", menuName = "Crescent Wreath/Effect SFX Catalog")]
public sealed class EffectSfxCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string effectCategoryKey = EffectCategoryKeys.GenericEffect;
        public List<AudioClip> clips = new();
        [Range(0f, 1f)] public float volume = 0.75f;
        [Range(0.5f, 1.5f)] public float pitchMin = 0.96f;
        [Range(0.5f, 1.5f)] public float pitchMax = 1.04f;
        public bool randomizeClip = true;
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
            if (entry is null || entry.clips.Count <= 0)
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
