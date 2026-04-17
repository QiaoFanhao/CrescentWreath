using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public static class AnomalyDefinitionRepository
{
    private static readonly IAnomalyDefinitionSource Source = new InMemoryAnomalyDefinitionSource();
    private static readonly Dictionary<string, AnomalyDefinition> DefinitionsById = buildDefinitionsById(Source);
    private static readonly IReadOnlyList<string> InitialDeckDefinitionIds = buildInitialDeckDefinitionIds(Source);

    public static AnomalyDefinition resolveByDefinitionId(string definitionId)
    {
        if (DefinitionsById.TryGetValue(definitionId, out var anomalyDefinition))
        {
            return anomalyDefinition;
        }

        return new AnomalyDefinition
        {
            definitionId = definitionId,
            name = string.Empty,
            oncePerTurnHint = string.Empty,
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = string.Empty,
            resolveRewardKey = string.Empty,
            resolveManaCost = null,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
        };
    }

    public static IReadOnlyList<string> getInitialDeckDefinitionIds()
    {
        return InitialDeckDefinitionIds;
    }

    internal static Dictionary<string, AnomalyDefinition> buildDefinitionsById(IAnomalyDefinitionSource source)
    {
        var definitionsById = new Dictionary<string, AnomalyDefinition>();
        foreach (var anomalyDefinition in source.getAnomalyDefinitions())
        {
            definitionsById[anomalyDefinition.definitionId] = anomalyDefinition;
        }

        return definitionsById;
    }

    internal static IReadOnlyList<string> buildInitialDeckDefinitionIds(IAnomalyDefinitionSource source)
    {
        var definitionIds = new List<string>();
        foreach (var anomalyDefinition in source.getAnomalyDefinitions())
        {
            definitionIds.Add(anomalyDefinition.definitionId);
        }

        return definitionIds;
    }
}
