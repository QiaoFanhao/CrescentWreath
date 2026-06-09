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
        bool appendAttemptedSuccessEvent = true,
        bool appendRewardPlaceholderEvent = false)
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
        gameState.resolvedAnomalyDefinitionIds.Add(currentAnomalyDefinition.definitionId);

        if (appendRewardPlaceholderEvent)
        {
            var anomalyName = !string.IsNullOrWhiteSpace(currentAnomalyDefinition.name)
                ? currentAnomalyDefinition.name
                : currentAnomalyDefinition.definitionId;
            actionChainState.producedEvents.Add(new AnomalyRewardPlaceholderEvent
            {
                eventId = requestId,
                eventTypeKey = "anomalyRewardPlaceholder",
                sourceActionChainId = actionChainState.actionChainId,
                anomalyDefinitionId = currentAnomalyDefinition.definitionId,
                anomalyName = anomalyName,
                message = $"{anomalyName}已解决，结算奖励",
            });
        }

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
