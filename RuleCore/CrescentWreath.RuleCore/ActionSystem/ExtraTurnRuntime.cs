using System;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class ExtraTurnRuntime
{
    public static bool grantExtraTurn(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("ExtraTurnRuntime requires gameState.turnState.");
        }

        if (!gameState.players.ContainsKey(playerId))
        {
            throw new InvalidOperationException("ExtraTurnRuntime requires playerId to exist in gameState.players.");
        }

        var extraTurnFlags = gameState.turnState.extraTurnFlags;
        if (extraTurnFlags.extraTurnGrantedThisTurn)
        {
            return false;
        }

        extraTurnFlags.pendingExtraTurnForPlayerId = playerId;
        extraTurnFlags.extraTurnGrantedThisTurn = true;
        return true;
    }

    public static PlayerId resolveNextTurnPlayer(
        TurnState turnState,
        PlayerId normalNextPlayerId)
    {
        var extraTurnFlags = turnState.extraTurnFlags;
        if (extraTurnFlags.pendingExtraTurnForPlayerId.HasValue)
        {
            var extraTurnPlayerId = extraTurnFlags.pendingExtraTurnForPlayerId.Value;
            extraTurnFlags.pendingExtraTurnForPlayerId = null;
            extraTurnFlags.extraTurnGrantedThisTurn = false;
            extraTurnFlags.isCurrentTurnExtraTurn = true;
            return extraTurnPlayerId;
        }

        extraTurnFlags.extraTurnGrantedThisTurn = false;
        extraTurnFlags.isCurrentTurnExtraTurn = false;
        return normalNextPlayerId;
    }
}
