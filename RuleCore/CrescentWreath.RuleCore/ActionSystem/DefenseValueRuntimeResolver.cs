using System;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class DefenseValueRuntimeResolver
{
    private const string StatusKeySeal = "Seal";

    public static int? resolveEffectiveDefenseValue(
        GameState.GameState gameState,
        CardInstance cardInstance)
    {
        var definitionDefenseValue = TreasureResourceValueResolver.resolveDefenseValue(cardInstance.definitionId);
        if (!definitionDefenseValue.HasValue)
        {
            return null;
        }

        var effectiveDefenseValue = definitionDefenseValue.Value;
        if (hasSealOnOwnerActiveCharacter(gameState, cardInstance.ownerPlayerId))
        {
            effectiveDefenseValue = Math.Max(0, effectiveDefenseValue - 1);
        }

        return effectiveDefenseValue;
    }

    private static bool hasSealOnOwnerActiveCharacter(
        GameState.GameState gameState,
        PlayerId ownerPlayerId)
    {
        if (!gameState.players.TryGetValue(ownerPlayerId, out var ownerPlayerState))
        {
            return false;
        }

        if (!ownerPlayerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        var ownerStatuses = StatusRuntime.queryStatusesForCharacter(
            gameState,
            ownerPlayerState.activeCharacterInstanceId.Value);
        foreach (var ownerStatus in ownerStatuses)
        {
            var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(ownerStatus.statusKey);
            if (string.Equals(normalizedStatusKey, StatusKeySeal, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
