using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Events;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TurnFlowAutoAdvanceService
{
    private const int MaxAutoAdvanceHops = 8;
    private const string AutoAdvanceSourceKey = "server:autoAdvance";

    public List<GameEvent> tryAutoAdvanceUntilPlayerActionRequired(
        RuleCore.GameState.GameState gameState,
        ActionRequestProcessor actionRequestProcessor,
        long requestId)
    {
        var producedEvents = new List<GameEvent>();
        var hopCount = 0;

        while (canAutoAdvance(gameState))
        {
            if (++hopCount > MaxAutoAdvanceHops)
            {
                throw new InvalidOperationException("TurnFlowAutoAdvanceService exceeded max auto-advance hops.");
            }

            var turnState = gameState.turnState!;
            if (turnState.currentPhase == RuleCore.GameState.TurnPhase.end)
            {
                var startNextTurnActionRequest = new StartNextTurnActionRequest
                {
                    requestId = requestId,
                    actorPlayerId = turnState.currentPlayerId,
                    sourceKey = AutoAdvanceSourceKey,
                };

                producedEvents.AddRange(actionRequestProcessor.processActionRequest(gameState, startNextTurnActionRequest));
                continue;
            }

            if (turnState.currentPhase == RuleCore.GameState.TurnPhase.start)
            {
                var enterActionPhaseActionRequest = new EnterActionPhaseActionRequest
                {
                    requestId = requestId,
                    actorPlayerId = turnState.currentPlayerId,
                    sourceKey = AutoAdvanceSourceKey,
                };

                producedEvents.AddRange(actionRequestProcessor.processActionRequest(gameState, enterActionPhaseActionRequest));
                continue;
            }

            break;
        }

        return producedEvents;
    }

    private static bool canAutoAdvance(RuleCore.GameState.GameState gameState)
    {
        if (gameState.matchState != RuleCore.GameState.MatchState.running || gameState.turnState is null)
        {
            return false;
        }

        if (gameState.currentInputContext is not null || gameState.currentResponseWindow is not null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(gameState.currentActionChain?.pendingContinuationKey))
        {
            return false;
        }

        return gameState.turnState.currentPhase == RuleCore.GameState.TurnPhase.end ||
               gameState.turnState.currentPhase == RuleCore.GameState.TurnPhase.start;
    }
}
