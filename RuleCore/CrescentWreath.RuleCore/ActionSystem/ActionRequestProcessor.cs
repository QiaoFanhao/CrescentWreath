using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class ActionRequestProcessor
{
    private readonly ZoneMovementService zoneMovementService;

    public ActionRequestProcessor()
        : this(new ZoneMovementService())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService)
    {
        this.zoneMovementService = zoneMovementService;
    }

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
        var cardInstance = gameState.cardInstances[playCardActionRequest.cardInstanceId];

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(playCardActionRequest.requestId),
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

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            playCardActionRequest.targetZoneKey,
            CardMoveReason.play,
            actionChainState.actionChainId,
            playCardActionRequest.requestId);

        actionChainState.currentFrameIndex = 1;
        actionChainState.producedEvents.Add(cardMovedEvent);

        return actionChainState.producedEvents;
    }
}
