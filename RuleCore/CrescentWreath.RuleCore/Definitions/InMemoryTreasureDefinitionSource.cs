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
            definitionId = "T001",
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T002",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 2,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "T003",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "T004",
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "T005",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 5,
            persistOnFieldAcrossEnd = false,
            defenseValue = 5,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "T006",
            manaGainOnEnterField = 4,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 1,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T007",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T008",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 2,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T009",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "T010",
            manaGainOnEnterField = 4,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T011",
            manaGainOnEnterField = 4,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 7,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "T012",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T013",
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 5,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "T014",
            manaGainOnEnterField = 3,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 7,
            persistOnFieldAcrossEnd = false,
            defenseValue = 7,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T015",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T016",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = true,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T017",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 9,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T018",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = false,
            defenseValue = 5,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "T019",
            manaGainOnEnterField = 4,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "spell",
        },
        new()
        {
            definitionId = "T020",
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 5,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T021",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 6,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "physical",
        },
        new()
        {
            definitionId = "T022",
            manaGainOnEnterField = 5,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T023",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 7,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T024",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 1,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T025",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 5,
            persistOnFieldAcrossEnd = false,
            defenseValue = 4,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T026",
            manaGainOnEnterField = 3,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 5,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T027",
            manaGainOnEnterField = 2,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = 4,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T028",
            manaGainOnEnterField = 3,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 5,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "T029",
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 2,
            persistOnFieldAcrossEnd = false,
            defenseValue = 0,
            defenseTypeKey = "dual",
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
