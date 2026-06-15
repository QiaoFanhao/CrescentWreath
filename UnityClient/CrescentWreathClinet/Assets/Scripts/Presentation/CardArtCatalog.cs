using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.Presentation
{
[CreateAssetMenu(fileName = "CardArtCatalog", menuName = "Crescent Wreath/Card Art Catalog")]
public sealed class CardArtCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string definitionId = string.Empty;
        public Sprite? sprite;
    }

    [SerializeField]
    private Sprite? cardBack;

    [SerializeField]
    private List<Entry> entries = new();

    private Dictionary<string, Sprite>? spriteByDefinitionId;

    public Sprite? CardBack => cardBack;
    public int EntryCount => entries.Count;
    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetSprite(string definitionId, out Sprite? sprite)
    {
        ensureLookup();
        return spriteByDefinitionId!.TryGetValue(normalizeDefinitionId(definitionId), out sprite);
    }

    public Sprite? GetSpriteOrCardBack(string definitionId)
    {
        return TryGetSprite(definitionId, out var sprite) && sprite is not null
            ? sprite
            : cardBack;
    }

#if UNITY_EDITOR
    public void ReplaceEntriesForEditor(Sprite? newCardBack, IEnumerable<Entry> newEntries)
    {
        cardBack = newCardBack;
        entries = new List<Entry>(newEntries);
        spriteByDefinitionId = null;
    }
#endif

    private void OnEnable()
    {
        spriteByDefinitionId = null;
    }

    private void ensureLookup()
    {
        if (spriteByDefinitionId is not null)
        {
            return;
        }

        spriteByDefinitionId = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry is null || entry.sprite is null)
            {
                continue;
            }

            var normalizedDefinitionId = normalizeDefinitionId(entry.definitionId);
            if (normalizedDefinitionId.Length == 0)
            {
                continue;
            }

            spriteByDefinitionId[normalizedDefinitionId] = entry.sprite;
        }
    }

    private static string normalizeDefinitionId(string definitionId)
    {
        var normalizedDefinitionId = string.IsNullOrWhiteSpace(definitionId)
            ? string.Empty
            : definitionId.Trim().ToUpperInvariant();

        return normalizedDefinitionId switch
        {
            "STARTER:KOURINDOUCOUPON" => "T001B",
            "STARTER:MAGICCIRCUIT" => "T002B",
            _ => normalizedDefinitionId,
        };
    }
}
}
