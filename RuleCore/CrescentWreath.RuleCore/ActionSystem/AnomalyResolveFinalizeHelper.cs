using System;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyResolveFinalizeHelper
{
    public static void finalizeSuccessfulResolve(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        AnomalyDefinition currentAnomalyDefinition,
        Func<bool> flipNextAnomalyAction,
        string turnStateMissingErrorMessage,
        bool appendAttemptedSuccessEvent = true)
    {
        if (appendAttemptedSuccessEvent)
        {
            actionChainState.producedEvents.Add(new AnomalyResolveAttemptedEvent
            {
                eventId = requestId,
                eventTypeKey = "anomalyResolveAttempted",
                sourceActionChainId = actionChainState.actionChainId,
                anomalyDefinitionId = currentAnomalyDefinition.definitionId,
                isSucceeded = true,
                failedReasonKey = null,
            });
        }

        actionChainState.producedEvents.Add(new AnomalyResolvedEvent
        {
            eventId = requestId,
            eventTypeKey = "anomalyResolved",
            sourceActionChainId = actionChainState.actionChainId,
            anomalyDefinitionId = currentAnomalyDefinition.definitionId,
        });

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException(turnStateMissingErrorMessage);
        }

        gameState.turnState.hasResolvedAnomalyThisTurn = true;
        var isSuspendedByArrivalInput = flipNextAnomalyAction();
        if (isSuspendedByArrivalInput)
        {
            return;
        }

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }
}
