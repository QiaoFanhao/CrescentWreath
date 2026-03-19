using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class MinimalActionFlowProcessor
{
    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        if (actionRequest is PlayCardActionRequest playCardActionRequest)
        {
            return processPlayCardActionRequest(gameState, playCardActionRequest);
        }

        throw new NotSupportedException("Only PlayCardActionRequest is supported in M2 minimal flow.");
    }

    private List<GameEvent> processPlayCardActionRequest(
        GameState.GameState gameState,
        PlayCardActionRequest playCardActionRequest)
    {
        if (!gameState.cardInstances.TryGetValue(playCardActionRequest.cardInstanceId, out var cardInstance))
        {
            throw new InvalidOperationException("CardInstance not found for PlayCardActionRequest.");
        }

        var fromZoneState = getOrCreateZoneState(gameState, cardInstance.zoneKey);
        var toZoneState = getOrCreateZoneState(gameState, playCardActionRequest.targetZoneKey);

        var actionChainState = new ActionChainState
        {
            actionChainId = new Ids.ActionChainId(playCardActionRequest.requestId),
            actorPlayerId = playCardActionRequest.actorPlayerId,
            rootActionRequest = playCardActionRequest,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "moveCard",
            movingCardInstanceId = playCardActionRequest.cardInstanceId,
            fromZoneKey = cardInstance.zoneKey,
            toZoneKey = playCardActionRequest.targetZoneKey,
            moveReason = CardMoveReason.play,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        fromZoneState.cardInstanceIds.Remove(playCardActionRequest.cardInstanceId);
        if (!toZoneState.cardInstanceIds.Contains(playCardActionRequest.cardInstanceId))
        {
            toZoneState.cardInstanceIds.Add(playCardActionRequest.cardInstanceId);
        }

        cardInstance.zoneKey = playCardActionRequest.targetZoneKey;
        actionChainState.currentFrameIndex = 1;

        var cardMovedEvent = new CardMovedEvent
        {
            eventId = playCardActionRequest.requestId,
            eventTypeKey = "cardMoved",
            sourceActionChainId = actionChainState.actionChainId,
            cardInstanceId = playCardActionRequest.cardInstanceId,
            fromZoneKey = effectFrame.fromZoneKey ?? cardInstance.zoneKey,
            toZoneKey = effectFrame.toZoneKey ?? cardInstance.zoneKey,
            moveReason = effectFrame.moveReason ?? CardMoveReason.play,
        };

        actionChainState.producedEvents.Add(cardMovedEvent);
        actionChainState.producedEventKeys.Add(cardMovedEvent.eventTypeKey);

        return actionChainState.producedEvents;
    }

    private ZoneState getOrCreateZoneState(GameState.GameState gameState, ZoneKey zoneKey)
    {
        if (gameState.zones.TryGetValue(zoneKey, out var existingZoneState))
        {
            return existingZoneState;
        }

        var zoneState = new ZoneState
        {
            zoneKey = zoneKey,
        };
        gameState.zones.Add(zoneKey, zoneState);
        return zoneState;
    }
}
