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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 1,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 4,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 3,
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
            initialPublicDeckCopies = 2,
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
            initialPublicDeckCopies = 4,
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 1,
            summonSigilCost = 2,
            persistOnFieldAcrossEnd = false,
            defenseValue = 0,
            defenseTypeKey = "dual",
        },
        new()
        {
            definitionId = "S001",
            initialPublicDeckCopies = 0,
            manaGainOnEnterField = 1,
            sigilPreviewGainOnEnterField = 2,
            summonSigilCost = 3,
            persistOnFieldAcrossEnd = false,
            defenseValue = 3,
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
