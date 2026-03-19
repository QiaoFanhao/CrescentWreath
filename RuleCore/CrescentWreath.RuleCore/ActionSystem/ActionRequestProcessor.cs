using System;
using System.Collections.Generic;
using System.Threading;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class ActionRequestProcessor
{
    private readonly ZoneMovementService zoneMovementService;
    private readonly DamageProcessor damageProcessor;
    private long nextResponseWindowNumericId;

    public ActionRequestProcessor()
        : this(new ZoneMovementService(), new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService)
        : this(zoneMovementService, new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService, DamageProcessor damageProcessor)
    {
        this.zoneMovementService = zoneMovementService;
        this.damageProcessor = damageProcessor;
    }

    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        if (actionRequest is PlayCardActionRequest playCardActionRequest)
        {
            return processPlayCardActionRequest(gameState, playCardActionRequest);
        }

        if (actionRequest is DealDamageActionRequest dealDamageActionRequest)
        {
            return processDealDamageActionRequest(gameState, dealDamageActionRequest);
        }

        throw new NotSupportedException("Only PlayCardActionRequest and DealDamageActionRequest are supported in M2 minimal flow.");
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

    private List<GameEvent> processDealDamageActionRequest(
        GameState.GameState gameState,
        DealDamageActionRequest dealDamageActionRequest)
    {
        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(dealDamageActionRequest.requestId),
            actorPlayerId = dealDamageActionRequest.actorPlayerId,
            rootActionRequest = dealDamageActionRequest,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "resolveDamage",
            sourcePlayerId = dealDamageActionRequest.actorPlayerId,
            sourceCardInstanceId = dealDamageActionRequest.sourceCardInstanceId,
            sourceCharacterInstanceId = dealDamageActionRequest.sourceCharacterInstanceId,
            targetCharacterInstanceId = dealDamageActionRequest.targetCharacterInstanceId,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        var responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId));

        gameState.currentResponseWindow = new ResponseWindowState
        {
            responseWindowId = responseWindowId,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
        };

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = dealDamageActionRequest.requestId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowId,
            isOpened = true,
        });

        gameState.currentResponseWindow = null;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = dealDamageActionRequest.requestId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowId,
            isOpened = false,
        });

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(dealDamageActionRequest.requestId),
            sourcePlayerId = dealDamageActionRequest.actorPlayerId,
            sourceCardInstanceId = dealDamageActionRequest.sourceCardInstanceId,
            sourceCharacterInstanceId = dealDamageActionRequest.sourceCharacterInstanceId,
            targetCharacterInstanceId = dealDamageActionRequest.targetCharacterInstanceId,
            baseDamageValue = dealDamageActionRequest.baseDamageValue,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;

        return actionChainState.producedEvents;
    }
}
