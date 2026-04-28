using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public static class TreasureDefinitionRepository
{
    private static readonly ITreasureDefinitionSource Source = new InMemoryTreasureDefinitionSource();
    private static readonly Dictionary<string, TreasureDefinition> DefinitionsById = buildDefinitionsById(Source);
    private static readonly IReadOnlyList<string> InitialPublicDeckDefinitionIds = buildInitialPublicDeckDefinitionIds(Source);

    public static TreasureDefinition resolveByDefinitionId(string definitionId)
    {
        if (DefinitionsById.TryGetValue(definitionId, out var treasureDefinition))
        {
            return treasureDefinition;
        }

        return new TreasureDefinition
        {
            definitionId = definitionId,
            manaGainOnEnterField = 0,
            sigilPreviewGainOnEnterField = 0,
            summonSigilCost = null,
            initialPublicDeckCopies = 0,
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        };
    }

    public static IReadOnlyList<string> getInitialPublicDeckDefinitionIds()
    {
        return InitialPublicDeckDefinitionIds;
    }

    internal static Dictionary<string, TreasureDefinition> buildDefinitionsById(ITreasureDefinitionSource source)
    {
        var definitionsById = new Dictionary<string, TreasureDefinition>();
        foreach (var treasureDefinition in source.getTreasureDefinitions())
        {
            definitionsById[treasureDefinition.definitionId] = treasureDefinition;
        }

        return definitionsById;
    }

    internal static IReadOnlyList<string> buildInitialPublicDeckDefinitionIds(ITreasureDefinitionSource source)
    {
        var initialPublicDeckDefinitionIds = new List<string>();
        foreach (var treasureDefinition in source.getTreasureDefinitions())
        {
            for (var copyIndex = 0; copyIndex < treasureDefinition.initialPublicDeckCopies; copyIndex++)
            {
                initialPublicDeckDefinitionIds.Add(treasureDefinition.definitionId);
            }
        }

        return initialPublicDeckDefinitionIds;
    }
}
