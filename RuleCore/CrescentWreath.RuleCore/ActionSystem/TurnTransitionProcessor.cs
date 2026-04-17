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

public sealed class TurnTransitionProcessor
{
    public const string ContinuationKeyTurnStartShackleDiscard = "continuation:turnStartShackleDiscard";

    private const int TurnStartSkillPointBaseline = 1;
    private const int ShackleDiscardRequiredCount = 4;
    private const string StatusKeyShackle = "Shackle";
    private const string TurnStartShackleDiscardInputTypeKey = "turnStartShackleDiscardChoice";
    private const string TurnStartShackleDiscardContextKey = "turnStart:shackleDiscard";
    private const string TurnStartShackleDiscardChoiceKeyPrefix = "discardCard:";

    private readonly ZoneMovementService zoneMovementService;
    private readonly EndPhaseProcessor endPhaseProcessor;
    private readonly Func<long> nextInputContextIdSupplier;

    public TurnTransitionProcessor(
        ZoneMovementService zoneMovementService,
        EndPhaseProcessor endPhaseProcessor,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.endPhaseProcessor = endPhaseProcessor;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public List<GameEvent> processEnterActionPhaseActionRequest(
        RuleCore.GameState.GameState gameState,
        EnterActionPhaseActionRequest enterActionPhaseActionRequest)
    {
        if (gameState.matchState != MatchState.running)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest requires gameState.turnState to be initialized.");
        }

        if (enterActionPhaseActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.turnState.currentPhase != TurnPhase.start)
        {
            throw new InvalidOperationException("EnterActionPhaseActionRequest requires gameState.turnState.currentPhase to be start.");
        }

        var actionChainState = createPhaseTransitionActionChain(
            gameState,
            enterActionPhaseActionRequest,
            "enterActionPhase",
            enterActionPhaseActionRequest.actorPlayerId,
            "phaseTransition:action");

        var actorPlayerState = gameState.players[enterActionPhaseActionRequest.actorPlayerId];
        actorPlayerState.skillPoint = TurnStartSkillPointBaseline;
        gameState.turnState.currentPhase = TurnPhase.action;
        gameState.turnState.phaseStepIndex = 0;

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    public List<GameEvent> processEnterSummonPhaseActionRequest(
        RuleCore.GameState.GameState gameState,
        EnterSummonPhaseActionRequest enterSummonPhaseActionRequest)
    {
        if (gameState.matchState != MatchState.running)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest requires gameState.turnState to be initialized.");
        }

        if (enterSummonPhaseActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.turnState.currentPhase != TurnPhase.action)
        {
            throw new InvalidOperationException("EnterSummonPhaseActionRequest requires gameState.turnState.currentPhase to be action.");
        }

        var actorPlayerState = gameState.players[enterSummonPhaseActionRequest.actorPlayerId];

        var actionChainState = createPhaseTransitionActionChain(
            gameState,
            enterSummonPhaseActionRequest,
            "enterSummonPhase",
            enterSummonPhaseActionRequest.actorPlayerId,
            "phaseTransition:summon");

        var recomputedSigil = SigilSnapshotCalculator.recomputeSigilPreviewFromCurrentFieldState(gameState, actorPlayerState);
        actorPlayerState.lockedSigil = recomputedSigil;
        actorPlayerState.isSigilLocked = true;
        actorPlayerState.sigilPreview = 0;
        gameState.turnState.currentPhase = TurnPhase.summon;
        gameState.turnState.phaseStepIndex = 0;

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    public List<GameEvent> processStartNextTurnActionRequest(
        RuleCore.GameState.GameState gameState,
        StartNextTurnActionRequest startNextTurnActionRequest)
    {
        if (gameState.matchState != MatchState.running)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.turnState to be initialized.");
        }

        if (startNextTurnActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.turnState.currentPhase != TurnPhase.end)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.turnState.currentPhase to be end.");
        }

        if (gameState.matchMeta is null)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.matchMeta to be initialized.");
        }

        var seatOrder = gameState.matchMeta.seatOrder;
        if (seatOrder.Count == 0)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires gameState.matchMeta.seatOrder to be non-empty.");
        }

        var currentSeatIndex = seatOrder.IndexOf(gameState.turnState.currentPlayerId);
        if (currentSeatIndex < 0)
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires currentPlayerId to exist in gameState.matchMeta.seatOrder.");
        }

        var nextSeatIndex = (currentSeatIndex + 1) % seatOrder.Count;
        var nextPlayerId = seatOrder[nextSeatIndex];
        if (!gameState.matchMeta.teamAssignments.TryGetValue(nextPlayerId, out var nextTeamId))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires nextPlayerId to exist in gameState.matchMeta.teamAssignments.");
        }

        if (!gameState.players.TryGetValue(nextPlayerId, out var nextPlayerState))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires nextPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(nextPlayerState.deckZoneId, out var nextPlayerDeckZoneState))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires next player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(nextPlayerState.handZoneId, out var nextPlayerHandZoneState))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires next player's handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(nextPlayerState.discardZoneId, out var nextPlayerDiscardZoneState))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires next player's discardZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(nextPlayerState.fieldZoneId, out var nextPlayerFieldZoneState))
        {
            throw new InvalidOperationException("StartNextTurnActionRequest requires next player's fieldZoneId to exist in gameState.zones.");
        }

        foreach (var cardInstanceId in nextPlayerDeckZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("StartNextTurnActionRequest requires all cardInstanceIds in next player's deck zone to exist in gameState.cardInstances.");
            }
        }

        foreach (var cardInstanceId in nextPlayerHandZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("StartNextTurnActionRequest requires all cardInstanceIds in next player's hand zone to exist in gameState.cardInstances.");
            }
        }

        foreach (var cardInstanceId in nextPlayerDiscardZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("StartNextTurnActionRequest requires all cardInstanceIds in next player's discard zone to exist in gameState.cardInstances.");
            }
        }

        foreach (var cardInstanceId in nextPlayerFieldZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(cardInstanceId))
            {
                throw new InvalidOperationException("StartNextTurnActionRequest requires all cardInstanceIds in next player's field zone to exist in gameState.cardInstances.");
            }
        }

        var actionChainState = createPhaseTransitionActionChain(
            gameState,
            startNextTurnActionRequest,
            "startNextTurn",
            startNextTurnActionRequest.actorPlayerId,
            "phaseTransition:nextTurn");

        gameState.turnState.turnNumber += 1;
        gameState.turnState.currentPlayerId = nextPlayerId;
        gameState.turnState.currentTeamId = nextTeamId;
        gameState.turnState.currentPhase = TurnPhase.start;
        gameState.turnState.phaseStepIndex = 0;
        gameState.turnState.hasResolvedAnomalyThisTurn = false;
        var removedSealStatuses = removeSealOnActiveCharacterAtTurnStart(gameState, nextPlayerState);
        appendReturnDefensePlacedCardsToHand(
            gameState,
            actionChainState,
            nextPlayerState,
            startNextTurnActionRequest.requestId);
        appendStatusRemovedEvents(actionChainState, removedSealStatuses, startNextTurnActionRequest.requestId);

        if (tryHandleShackleAtTurnStart(
                gameState,
                actionChainState,
                nextPlayerState,
                startNextTurnActionRequest.requestId))
        {
            return actionChainState.producedEvents;
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    private void appendReturnDefensePlacedCardsToHand(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState playerState,
        long eventId)
    {
        var fieldZoneState = gameState.zones[playerState.fieldZoneId];
        var fieldCardIdsInCurrentOrder = new List<CardInstanceId>(fieldZoneState.cardInstanceIds);
        foreach (var cardInstanceId in fieldCardIdsInCurrentOrder)
        {
            var fieldCardInstance = gameState.cardInstances[cardInstanceId];
            if (!fieldCardInstance.isDefensePlacedOnField)
            {
                continue;
            }

            var returnEvent = zoneMovementService.moveCard(
                gameState,
                fieldCardInstance,
                playerState.handZoneId,
                CardMoveReason.returnToSource,
                actionChainState.actionChainId,
                eventId);
            actionChainState.producedEvents.Add(returnEvent);
            fieldCardInstance.isDefensePlacedOnField = false;
        }
    }

    private static List<StatusInstance> removeSealOnActiveCharacterAtTurnStart(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState)
    {
        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return new List<StatusInstance>();
        }

        return StatusRuntime.removeStatusesOnCharacter(
            gameState,
            playerState.activeCharacterInstanceId.Value,
            "Seal");
    }

    public void continueTurnStartShackleDiscardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != TurnStartShackleDiscardContextKey)
        {
            throw new InvalidOperationException("Turn-start shackle discard continuation requires currentInputContext.contextKey to be turnStart:shackleDiscard.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("Turn-start shackle discard continuation requires currentInputContext.requiredPlayerId.");
        }

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("Turn-start shackle discard continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!actorPlayerState.activeCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Turn-start shackle discard continuation requires actor player activeCharacterInstanceId.");
        }

        var selectedCardInstanceIds = parseShackleDiscardChoiceKeys(submitInputChoiceActionRequest.choiceKeys);
        foreach (var selectedCardInstanceId in selectedCardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("Turn-start shackle discard continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
            }

            if (selectedCardInstance.ownerPlayerId != actorPlayerId)
            {
                throw new InvalidOperationException("Turn-start shackle discard continuation requires selected cards to be owned by currentInputContext.requiredPlayerId.");
            }

            if (selectedCardInstance.zoneId != actorPlayerState.handZoneId)
            {
                throw new InvalidOperationException("Turn-start shackle discard continuation requires selected cards to be in actor hand zone.");
            }
        }

        foreach (var selectedCardInstanceId in selectedCardInstanceIds)
        {
            var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
            var discardEvent = zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                actorPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(discardEvent);
        }

        var removedShackleStatuses = StatusRuntime.removeStatusesOnCharacter(
            gameState,
            actorPlayerState.activeCharacterInstanceId.Value,
            StatusKeyShackle);
        appendStatusRemovedEvents(
            actionChainState,
            removedShackleStatuses,
            submitInputChoiceActionRequest.requestId);

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public static bool isValidTurnStartShackleDiscardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (submitInputChoiceActionRequest.choiceKeys.Count != ShackleDiscardRequiredCount)
        {
            return false;
        }

        var uniqueChoiceKeys = new HashSet<string>(submitInputChoiceActionRequest.choiceKeys, StringComparer.Ordinal);
        if (uniqueChoiceKeys.Count != ShackleDiscardRequiredCount)
        {
            return false;
        }

        foreach (var choiceKey in submitInputChoiceActionRequest.choiceKeys)
        {
            if (!inputContextState.choiceKeys.Contains(choiceKey))
            {
                return false;
            }
        }

        return true;
    }

    private bool tryHandleShackleAtTurnStart(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState nextPlayerState,
        long eventId)
    {
        if (!nextPlayerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        var activeCharacterInstanceId = nextPlayerState.activeCharacterInstanceId.Value;
        if (!StatusRuntime.hasStatusOnCharacter(gameState, activeCharacterInstanceId, StatusKeyShackle))
        {
            return false;
        }

        var handZoneState = gameState.zones[nextPlayerState.handZoneId];
        if (handZoneState.cardInstanceIds.Count < ShackleDiscardRequiredCount)
        {
            endPhaseProcessor.forceCompleteEndSettlementWithoutHandCapInput(
                gameState,
                actionChainState,
                nextPlayerState,
                eventId);
            var removedShackleStatuses = StatusRuntime.removeStatusesOnCharacter(
                gameState,
                activeCharacterInstanceId,
                StatusKeyShackle);
            appendStatusRemovedEvents(actionChainState, removedShackleStatuses, eventId);
            actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
            actionChainState.isCompleted = true;
            return true;
        }

        openTurnStartShackleDiscardInputContext(gameState, actionChainState, nextPlayerState, eventId);
        return true;
    }

    private void openTurnStartShackleDiscardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState nextPlayerState,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("Turn-start shackle discard input requires gameState.currentInputContext to be null before opening.");
        }

        var handZoneState = gameState.zones[nextPlayerState.handZoneId];
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = nextPlayerState.playerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = TurnStartShackleDiscardInputTypeKey,
            contextKey = TurnStartShackleDiscardContextKey,
        };

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            inputContextState.choiceKeys.Add(createTurnStartShackleDiscardChoiceKey(cardInstanceId));
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyTurnStartShackleDiscard;
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

    private static string createTurnStartShackleDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return TurnStartShackleDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static List<CardInstanceId> parseShackleDiscardChoiceKeys(List<string> choiceKeys)
    {
        var cardInstanceIds = new List<CardInstanceId>(choiceKeys.Count);
        foreach (var choiceKey in choiceKeys)
        {
            if (!choiceKey.StartsWith(TurnStartShackleDiscardChoiceKeyPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Turn-start shackle discard choiceKey must start with discardCard: prefix.");
            }

            var cardIdSegment = choiceKey.Substring(TurnStartShackleDiscardChoiceKeyPrefix.Length);
            if (!long.TryParse(cardIdSegment, out var cardNumericId))
            {
                throw new InvalidOperationException("Turn-start shackle discard choiceKey must encode a valid CardInstanceId numeric value.");
            }

            cardInstanceIds.Add(new CardInstanceId(cardNumericId));
        }

        return cardInstanceIds;
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

    private static void appendStatusRemovedEvents(
        ActionChainState actionChainState,
        List<StatusInstance> removedStatuses,
        long eventId)
    {
        foreach (var removedStatus in removedStatuses)
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
    }
}
