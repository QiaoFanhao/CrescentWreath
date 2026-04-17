using System;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class TemporaryTreasureDefinitionResolver
{
    public static TemporaryTreasureDefinition resolveDefinition(string definitionId)
    {
        var treasureDefinition = TreasureDefinitionRepository.resolveByDefinitionId(definitionId);
        return new TemporaryTreasureDefinition(
            manaGainOnEnterField: treasureDefinition.manaGainOnEnterField,
            sigilPreviewGainOnEnterField: treasureDefinition.sigilPreviewGainOnEnterField,
            summonSigilCost: treasureDefinition.summonSigilCost,
            persistOnFieldAcrossEnd: treasureDefinition.persistOnFieldAcrossEnd);
    }

    public static int resolveManaGainOnEnterField(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).manaGainOnEnterField;
    }

    public static int resolveSigilPreviewGainOnEnterField(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).sigilPreviewGainOnEnterField;
    }

    public static int resolveSummonSigilCost(string definitionId)
    {
        var summonSigilCost = TreasureDefinitionRepository.resolveByDefinitionId(definitionId).summonSigilCost;
        if (!summonSigilCost.HasValue)
        {
            throw new NotSupportedException("SummonTreasureCardActionRequest currently supports only definitionId test-summon-card for lockedSigil payment.");
        }

        return summonSigilCost.Value;
    }

    public static bool shouldPersistOnFieldAcrossEnd(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).persistOnFieldAcrossEnd;
    }

    public static int? resolveDefenseValue(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).defenseValue;
    }

    public static string? resolveDefenseTypeKey(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).defenseTypeKey;
    }
}

public sealed class TemporaryTreasureDefinition
{
    public TemporaryTreasureDefinition(
        int manaGainOnEnterField,
        int sigilPreviewGainOnEnterField,
        int? summonSigilCost,
        bool persistOnFieldAcrossEnd)
    {
        this.manaGainOnEnterField = manaGainOnEnterField;
        this.sigilPreviewGainOnEnterField = sigilPreviewGainOnEnterField;
        this.summonSigilCost = summonSigilCost;
        this.persistOnFieldAcrossEnd = persistOnFieldAcrossEnd;
    }

    public int manaGainOnEnterField { get; }

    public int sigilPreviewGainOnEnterField { get; }

    public int? summonSigilCost { get; }

    public bool persistOnFieldAcrossEnd { get; }
}
