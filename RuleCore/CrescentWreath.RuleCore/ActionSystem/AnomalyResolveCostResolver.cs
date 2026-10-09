using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

internal static class AnomalyResolveCostResolver
{
    private const string A001DefinitionId = "A001";
    private const string RemiliaDefinitionId = "C003";
    private const int A001RemiliaManaSurcharge = 2;

    public static int? resolveManaCost(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (!anomalyDefinition.resolveManaCost.HasValue)
        {
            return null;
        }

        var manaCost = anomalyDefinition.resolveManaCost.Value;
        if (anomalyDefinition.definitionId == A001DefinitionId &&
            opponentTeamHasActiveCharacter(gameState, actorPlayerId, RemiliaDefinitionId))
        {
            manaCost += A001RemiliaManaSurcharge;
        }

        return manaCost;
    }

    private static bool opponentTeamHasActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        string characterDefinitionId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            return false;
        }

        foreach (var playerEntry in gameState.players)
        {
            var playerState = playerEntry.Value;
            if (playerState.teamId == actorPlayerState.teamId ||
                !playerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            if (gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var character) &&
                character.isAlive &&
                character.isInPlay &&
                character.definitionId == characterDefinitionId)
            {
                return true;
            }
        }

        return false;
    }
}
