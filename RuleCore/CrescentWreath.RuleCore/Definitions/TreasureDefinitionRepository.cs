using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public static class TreasureDefinitionRepository
{
    private static readonly ITreasureDefinitionSource Source = new InMemoryTreasureDefinitionSource();
    private static readonly Dictionary<string, TreasureDefinition> DefinitionsById = buildDefinitionsById(Source);

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
            persistOnFieldAcrossEnd = false,
            defenseValue = null,
            defenseTypeKey = null,
        };
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
}
