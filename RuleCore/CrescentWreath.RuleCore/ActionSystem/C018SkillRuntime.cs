using System;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class C018SkillRuntime
{
    private const string CharacterDefinitionId = "C018";

    public static void applySummonPhasePaymentDiscount(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorPlayerState = gameState.players[actorPlayerId];
        actorPlayerState.summonSigilDiscount = 0;

        if (!tryFindAliveInPlayCharacterInstanceByOwner(gameState, actorPlayerId, out var characterInstance))
        {
            return;
        }

        if (string.Equals(characterInstance.definitionId, CharacterDefinitionId, StringComparison.Ordinal))
        {
            actorPlayerState.summonSigilDiscount = 1;
        }
    }

    private static bool tryFindAliveInPlayCharacterInstanceByOwner(
        GameState.GameState gameState,
        PlayerId ownerId,
        out CharacterInstance characterInstance)
    {
        foreach (var instance in gameState.characterInstances.Values)
        {
            if (instance.ownerPlayerId == ownerId && instance.isAlive && instance.isInPlay)
            {
                characterInstance = instance;
                return true;
            }
        }

        characterInstance = null!;
        return false;
    }
}
