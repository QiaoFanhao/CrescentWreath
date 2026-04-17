using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class InMemoryTreasureDefinitionSource : ITreasureDefinitionSource
{
    private static readonly IReadOnlyList<TreasureDefinition> TreasureDefinitions = new List<TreasureDefinition>
    {
        new()
        {
            definitionId = "starter:magicCircuit",
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        },
        new()
        {
            definitionId = "starter:kourindouCoupon",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        },
        new()
        {
            definitionId = "test-summon-card",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 1,
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        },
        new()
        {
            definitionId = "test:summon-cost-8",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 8,
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        },
        new()
        {
            definitionId = "T016",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = true,
            defenseValue = null,
            defenseTypeKey = null,
        },
        new()
        {
            definitionId = "test:defensePhysical2",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "test:defensePhysical1",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = 1,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "test:defenseSpell2",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "test:defenseDual2",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "dual",
        },
    };

    public IReadOnlyList<TreasureDefinition> getTreasureDefinitions()
    {
        return TreasureDefinitions;
    }
}
