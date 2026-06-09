using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public static class TreasureDefinitionRepository
{
    private static readonly ITreasureDefinitionSource Source = new InMemoryTreasureDefinitionSource();
    private static readonly Dictionary<string, TreasureDefinition> DefinitionsById = buildDefinitionsById(Source);
    private static readonly IReadOnlyList<string> InitialPublicDeckDefinitionIds = buildInitialPublicDeckDefinitionIds(Source);
    private static readonly IReadOnlyList<string> DeclarableTreasureDefinitionIds = buildDeclarableTreasureDefinitionIds(Source);

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

    public static IReadOnlyList<string> getDeclarableTreasureDefinitionIds()
    {
        return DeclarableTreasureDefinitionIds;
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

    internal static IReadOnlyList<string> buildDeclarableTreasureDefinitionIds(ITreasureDefinitionSource source)
    {
        var declarableDefinitionIds = new List<string>();
        foreach (var treasureDefinition in source.getTreasureDefinitions())
        {
            if (isDeclarableTreasureDefinitionId(treasureDefinition.definitionId))
            {
                declarableDefinitionIds.Add(treasureDefinition.definitionId);
            }
        }

        return declarableDefinitionIds;
    }

    private static bool isDeclarableTreasureDefinitionId(string definitionId)
    {
        if (definitionId == "starter:magicCircuit" ||
            definitionId == "starter:kourindouCoupon" ||
            definitionId == "S001")
        {
            return true;
        }

        if (definitionId.Length != 4 || definitionId[0] != 'T')
        {
            return false;
        }

        return int.TryParse(definitionId.Substring(1), out var treasureNumericId) &&
               treasureNumericId >= 1 &&
               treasureNumericId <= 29;
    }
}
