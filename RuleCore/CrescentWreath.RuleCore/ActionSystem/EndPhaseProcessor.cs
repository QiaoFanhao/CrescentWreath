using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class EndPhaseProcessor
{
    public const string ContinuationKeyEndPhaseHandDiscard = "continuation:endPhaseHandDiscard";

    private const string EndPhaseDiscardInputTypeKey = "endPhaseDiscardChoice";
    private const string EndPhaseDiscardContextKey = "endPhase:discardToHandLimit";
    private const string EndPhaseDiscardChoiceKeyPrefix = "discardCard:";
    private const int TurnStartHandTargetSize = 6;

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public EndPhaseProcessor(ZoneMovementService zoneMovementService, Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public List<GameEvent> processEnterEndPhaseActionRequest(
        RuleCore.GameState.GameState gameState,
        EnterEndPhaseActionRequest enterEndPhaseActionRequest)
    {
        if (gameState.matchState != MatchState.running)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires gameState.turnState to be initialized.");
        }

        if (enterEndPhaseActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.turnState.currentPhase != TurnPhase.action &&
            gameState.turnState.currentPhase != TurnPhase.summon)
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires gameState.turnState.currentPhase to be action or summon.");
        }

        var actorPlayerState = gameState.players[enterEndPhaseActionRequest.actorPlayerId];
        if (!gameState.zones.TryGetValue(actorPlayerState.fieldZoneId, out var fieldZoneState))
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires actor player's fieldZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires actor player's discardZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires actor player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.handZoneId, out var handZoneState))
        {
            throw new InvalidOperationException("EnterEndPhaseActionRequest requires actor player's handZoneId to exist in gameState.zones.");
        }

        var fieldCardIdsInCurrentOrder = new List<CardInstanceId>(fieldZoneState.cardInstanceIds);
        foreach (var cardInstanceId in fieldCardIdsInCurrentOrder)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("EnterEndPhaseActionRequest requires all cardInstanceIds in actor field zone to exist in gameState.cardInstances.");
            }
        }

        foreach (var cardInstanceId in deckZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("EnterEndPhaseActionRequest requires all cardInstanceIds in actor deck zone to exist in gameState.cardInstances.");
            }
        }

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("EnterEndPhaseActionRequest requires all cardInstanceIds in actor hand zone to exist in gameState.cardInstances.");
            }
        }

        var discardZoneState = gameState.zones[actorPlayerState.discardZoneId];
        foreach (var cardInstanceId in discardZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("EnterEndPhaseActionRequest requires all cardInstanceIds in actor discard zone to exist in gameState.cardInstances.");
            }
        }

        var actionChainState = createPhaseTransitionActionChain(
            gameState,
            enterEndPhaseActionRequest,
            "enterEndPhase",
            enterEndPhaseActionRequest.actorPlayerId,
            "phaseTransition:end");

        appendFieldCardsToDiscardAtEnd(
            gameState,
            actionChainState,
            actorPlayerState,
            enterEndPhaseActionRequest.requestId);

        if (handZoneState.cardInstanceIds.Count > TurnStartHandTargetSize)
        {
            openEndPhaseHandDiscardInputContext(
                gameState,
                actionChainState,
                actorPlayerState,
                enterEndPhaseActionRequest.requestId);
            return actionChainState.producedEvents;
        }

        finalizeEnterEndPhaseAfterHandCap(
            gameState,
            actionChainState,
            actorPlayerState,
            enterEndPhaseActionRequest.requestId);

        return actionChainState.producedEvents;
    }

    public void continueEndPhaseHandDiscardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != EndPhaseDiscardContextKey)
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires currentInputContext.contextKey to be endPhase:discardToHandLimit.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires currentInputContext.requiredPlayerId.");
        }

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires requiredPlayerId to exist in gameState.players.");
        }

        var selectedCardInstanceId = parseEndPhaseDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.ownerPlayerId != actorPlayerId)
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires selected card to be owned by currentInputContext.requiredPlayerId.");
        }

        if (selectedCardInstance.zoneId != actorPlayerState.handZoneId)
        {
            throw new InvalidOperationException("End-phase hand-cap discard continuation requires selected card to be in actor hand zone.");
        }

        var discardEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(discardEvent);

        var handZoneState = gameState.zones[actorPlayerState.handZoneId];
        if (handZoneState.cardInstanceIds.Count > TurnStartHandTargetSize)
        {
            openEndPhaseHandDiscardInputContext(
                gameState,
                actionChainState,
                actorPlayerState,
                submitInputChoiceActionRequest.requestId);
            return;
        }

        finalizeEnterEndPhaseAfterHandCap(
            gameState,
            actionChainState,
            actorPlayerState,
            submitInputChoiceActionRequest.requestId);
    }

    public void forceCompleteEndSettlementWithoutHandCapInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState actorPlayerState,
        long eventId)
    {
        appendFieldCardsToDiscardAtEnd(
            gameState,
            actionChainState,
            actorPlayerState,
            eventId);
        finalizeEnterEndPhaseAfterHandCap(
            gameState,
            actionChainState,
            actorPlayerState,
            eventId);
    }

    private void openEndPhaseHandDiscardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState actorPlayerState,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("End-phase hand-cap discard input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerState.playerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = EndPhaseDiscardInputTypeKey,
            contextKey = EndPhaseDiscardContextKey,
        };

        inputContextState.choiceKeys.AddRange(createEndPhaseDiscardChoiceKeys(gameState, actorPlayerState));

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyEndPhaseHandDiscard;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = false;
    }

    private static List<string> createEndPhaseDiscardChoiceKeys(
        RuleCore.GameState.GameState gameState,
        PlayerState actorPlayerState)
    {
        var handZoneState = gameState.zones[actorPlayerState.handZoneId];
        var choiceKeys = new List<string>(handZoneState.cardInstanceIds.Count);
        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(createEndPhaseDiscardChoiceKey(cardInstanceId));
        }

        return choiceKeys;
    }

    private static string createEndPhaseDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return EndPhaseDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private void appendFieldCardsToDiscardAtEnd(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState actorPlayerState,
        long eventId)
    {
        var fieldZoneState = gameState.zones[actorPlayerState.fieldZoneId];
        var fieldCardIdsInCurrentOrder = new List<CardInstanceId>(fieldZoneState.cardInstanceIds);
        foreach (var cardInstanceId in fieldCardIdsInCurrentOrder)
        {
            var fieldCardInstance = gameState.cardInstances[cardInstanceId];
            if (fieldCardInstance.isDefensePlacedOnField ||
                TreasureResourceValueResolver.shouldPersistOnFieldAcrossEnd(fieldCardInstance.definitionId))
            {
                continue;
            }

            var discardEvent = zoneMovementService.moveCard(
                gameState,
                fieldCardInstance,
                actorPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                eventId);
            actionChainState.producedEvents.Add(discardEvent);
            fieldCardInstance.isDefensePlacedOnField = false;
        }
    }

    private static CardInstanceId parseEndPhaseDiscardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(EndPhaseDiscardChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("End-phase hand-cap discard choiceKey must start with discardCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(EndPhaseDiscardChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("End-phase hand-cap discard choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private void finalizeEnterEndPhaseAfterHandCap(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState actorPlayerState,
        long eventId)
    {
        appendDrawEventsUntilTargetHandSize(
            gameState,
            actionChainState,
            actorPlayerState,
            TurnStartHandTargetSize,
            eventId);

        var removedShortStatuses = StatusRuntime.clearShortStatusesAtTurnEnd(gameState);
        foreach (var removedStatus in removedShortStatuses)
        {
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = removedStatus.statusKey,
                targetCardInstanceId = removedStatus.targetCardInstanceId,
                targetCharacterInstanceId = removedStatus.targetCharacterInstanceId,
                targetPlayerId = removedStatus.targetPlayerId,
                isApplied = false,
            });
        }

        actorPlayerState.mana = 0;
        actorPlayerState.lockedSigil = null;
        actorPlayerState.isSigilLocked = false;
        gameState.turnState!.currentPhase = TurnPhase.end;
        gameState.turnState.phaseStepIndex = 0;

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    private void appendDrawEventsUntilTargetHandSize(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState playerState,
        int targetHandSize,
        long eventId)
    {
        var deckZoneState = gameState.zones[playerState.deckZoneId];
        var handZoneState = gameState.zones[playerState.handZoneId];
        var discardZoneState = gameState.zones[playerState.discardZoneId];

        while (handZoneState.cardInstanceIds.Count < targetHandSize)
        {
            if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
            {
                var discardCardIdsInCurrentOrder = new List<CardInstanceId>(discardZoneState.cardInstanceIds);
                foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
                {
                    var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                    var rebuildEvent = zoneMovementService.moveCard(
                        gameState,
                        discardedCardInstance,
                        playerState.deckZoneId,
                        CardMoveReason.returnToSource,
                        actionChainState.actionChainId,
                        eventId);
                    actionChainState.producedEvents.Add(rebuildEvent);
                }
            }

            if (deckZoneState.cardInstanceIds.Count == 0)
            {
                break;
            }

            var topCardInstanceId = deckZoneState.cardInstanceIds[0];
            var topCardInstance = gameState.cardInstances[topCardInstanceId];
            var drawEvent = zoneMovementService.moveCard(
                gameState,
                topCardInstance,
                playerState.handZoneId,
                CardMoveReason.draw,
                actionChainState.actionChainId,
                eventId);
            actionChainState.producedEvents.Add(drawEvent);
        }
    }

    private static ActionChainState createPhaseTransitionActionChain(
        RuleCore.GameState.GameState gameState,
        ActionRequest actionRequest,
        string effectKey,
        PlayerId actorPlayerId,
        string contextKey)
    {
        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(actionRequest.requestId),
            actorPlayerId = actorPlayerId,
            rootActionRequest = actionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        actionChainState.effectFrames.Add(new EffectFrame
        {
            effectKey = effectKey,
            sourcePlayerId = actorPlayerId,
            contextKey = contextKey,
        });

        gameState.currentActionChain = actionChainState;
        return actionChainState;
    }
}
