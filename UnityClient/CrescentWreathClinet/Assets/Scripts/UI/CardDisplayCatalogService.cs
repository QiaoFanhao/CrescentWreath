using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class CardDisplayCatalogService
{
    private const string CatalogResourcePath = "CardDisplay/CardDisplayCatalog";
    private static readonly Dictionary<string, CardDisplayEntry> EntriesByDefinitionId =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool loaded;

    public static CardDisplayEntry? TryGetEntry(string definitionId)
    {
        ensureLoaded();
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return null;
        }

        return EntriesByDefinitionId.TryGetValue(definitionId.Trim(), out var entry) ? entry : null;
    }

    public static string BuildAttributeText(string definitionId)
    {
        var entry = TryGetEntry(definitionId);
        if (entry is null)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(entry.summonCost))
        {
            lines.Add($"召唤费用：{entry.summonCost}");
        }

        if (!string.IsNullOrWhiteSpace(entry.mana))
        {
            lines.Add($"魔力：{entry.mana}");
        }

        if (!string.IsNullOrWhiteSpace(entry.sigil))
        {
            lines.Add($"灵符：{entry.sigil}");
        }

        if (!string.IsNullOrWhiteSpace(entry.defense))
        {
            lines.Add($"防御：{entry.defense}");
        }

        return string.Join("　", lines);
    }

    public static string BuildRulesText(string definitionId)
    {
        var entry = TryGetEntry(definitionId);
        if (entry is null)
        {
            return string.Empty;
        }

        if (string.Equals(entry.kind, "character", StringComparison.OrdinalIgnoreCase))
        {
            return buildCharacterSkillText(entry);
        }

        var builder = new StringBuilder();
        appendLabeledLine(builder, "效果", entry.effectText);
        appendLabeledLine(builder, "降临", entry.arrivalText);
        appendLabeledLine(builder, "解决", entry.resolveText);
        return builder.ToString().Trim();
    }

    private static string buildCharacterSkillText(CardDisplayEntry entry)
    {
        var builder = new StringBuilder();
        foreach (var skill in entry.skills ?? Array.Empty<CardDisplaySkillEntry>())
        {
            if (string.IsNullOrWhiteSpace(skill.name))
            {
                continue;
            }

            var header = new StringBuilder();
            header.Append('【');
            header.Append(string.IsNullOrWhiteSpace(skill.type) ? "技能" : skill.type);
            header.Append('】');
            header.Append(skill.name);
            if (!string.IsNullOrWhiteSpace(skill.cost))
            {
                header.Append('（');
                header.Append(skill.cost);
                header.Append('）');
            }

            builder.AppendLine(header.ToString());
            if (!string.IsNullOrWhiteSpace(skill.effectText))
            {
                builder.AppendLine(skill.effectText);
            }
        }

        return builder.ToString().Trim();
    }

    private static void appendLabeledLine(StringBuilder builder, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append('【');
        builder.Append(label);
        builder.Append('】');
        builder.AppendLine(value.Trim());
    }

    private static void ensureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        var textAsset = Resources.Load<TextAsset>(CatalogResourcePath);
        if (textAsset is null || string.IsNullOrWhiteSpace(textAsset.text))
        {
            Debug.LogWarning($"[CardDisplayCatalogService] Missing Resources/{CatalogResourcePath}.json.");
            return;
        }

        var catalog = JsonUtility.FromJson<CardDisplayCatalog>(textAsset.text);
        if (catalog?.cards is null)
        {
            return;
        }

        foreach (var entry in catalog.cards)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.definitionId))
            {
                continue;
            }

            EntriesByDefinitionId[entry.definitionId.Trim()] = entry;
        }
    }

    [Serializable]
    private sealed class CardDisplayCatalog
    {
        public CardDisplayEntry[] cards = Array.Empty<CardDisplayEntry>();
    }
}

[Serializable]
public sealed class CardDisplayEntry
{
    public string definitionId = string.Empty;
    public string kind = string.Empty;
    public string name = string.Empty;
    public string effectText = string.Empty;
    public string arrivalText = string.Empty;
    public string resolveText = string.Empty;
    public string summonCost = string.Empty;
    public string mana = string.Empty;
    public string sigil = string.Empty;
    public string defense = string.Empty;
    public CardDisplaySkillEntry[] skills = Array.Empty<CardDisplaySkillEntry>();
}

[Serializable]
public sealed class CardDisplaySkillEntry
{
    public string skillKey = string.Empty;
    public string name = string.Empty;
    public string type = string.Empty;
    public string cost = string.Empty;
    public string effectText = string.Empty;
}
}
