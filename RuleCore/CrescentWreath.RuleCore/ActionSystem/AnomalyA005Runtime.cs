using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyA005Runtime
{
    public const string ConditionContinuationKey = "continuation:anomalyA005ConditionDefenseLikePlace";

    private const string ConditionContextKey = "anomaly:A005:conditionDefenseLikePlace";
    private const string ConditionInputTypeKey = "anomalyA005ConditionDefenseLikePlace";
    private const string HandCardChoicePrefix = "handCard:";
    private const string SelectedChoicesLocalStatePrefix = "anomaly:A005:condition:selectedChoices:";
    private const int RequiredCardCountPerPlayer = 2;

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyA005Runtime(
        ZoneMovementService zoneMovementService,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool isParallelConditionInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               inputContextState.requiredPlayerIds.Count > 0 &&
               string.Equals(inputContextState.contextKey, ConditionContextKey, StringComparison.Ordinal);
    }

    public void openConditionInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A005 condition input requires gameState.currentInputContext to be null.");
        }

        var inputContextState = new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = ConditionInputTypeKey,
            contextKey = ConditionContextKey,
        };

        foreach (var playerId in requiredPlayerIds)
        {
            if (!gameState.players.TryGetValue(playerId, out var playerState) ||
                !gameState.zones.TryGetValue(playerState.handZoneId, out var handZoneState))
            {
                throw new InvalidOperationException("A005 condition input requires every friendly player hand zone.");
            }

            if (handZoneState.cardInstanceIds.Count < RequiredCardCountPerPlayer)
            {
                throw new InvalidOperationException("A005 condition input requires every friendly player to have at least two hand cards.");
            }

            var choiceKeys = new List<string>(handZoneState.cardInstanceIds.Count);
            foreach (var cardInstanceId in handZoneState.cardInstanceIds)
            {
                choiceKeys.Add(createHandCardChoiceKey(cardInstanceId));
            }

            inputContextState.requiredPlayerIds.Add(playerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[playerId.Value] = choiceKeys;
        }

        if (inputContextState.requiredPlayerIds.Count == 0)
        {
            throw new InvalidOperationException("A005 condition input requires at least one friendly player.");
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ConditionContinuationKey;
        actionChainState.isCompleted = false;
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = true,
        });
    }

    public bool continueParallelChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureChoiceValid(gameState, inputContextState, request);

        inputContextState.submittedPlayerIds.Add(request.actorPlayerId);
        actionChainState.localState[createSelectedChoicesStateKey(request.actorPlayerId)] =
            string.Join(",", request.choiceKeys);

        if (inputContextState.submittedPlayerIds.Count < inputContextState.requiredPlayerIds.Count)
        {
            actionChainState.isCompleted = false;
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            var selectedChoiceKeys = resolveSelectedChoices(actionChainState, playerId);
            var playerState = gameState.players[playerId];
            foreach (var selectedChoiceKey in selectedChoiceKeys)
            {
                var cardInstanceId = parseHandCardChoiceKey(selectedChoiceKey);
                var cardInstance = gameState.cardInstances[cardInstanceId];
                var movedEvent = zoneMovementService.moveCard(
                    gameState,
                    cardInstance,
                    playerState.fieldZoneId,
                    CardMoveReason.defensePlace,
                    actionChainState.actionChainId,
                    request.requestId);
                actionChainState.producedEvents.Add(movedEvent);
                cardInstance.isDefensePlacedOnField = true;
            }
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    private static void ensureChoiceValid(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        if (!string.Equals(inputContextState.contextKey, ConditionContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 parallel condition requires the A005 condition InputContext.");
        }

        if (!inputContextState.requiredPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("A005 parallel condition requires actorPlayerId to be a required player.");
        }

        if (inputContextState.submittedPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("A005 parallel condition does not allow duplicate submission.");
        }

        if (request.choiceKeys.Count != RequiredCardCountPerPlayer)
        {
            throw new InvalidOperationException("A005 parallel condition requires exactly two selected hand cards.");
        }

        var uniqueChoiceKeys = new HashSet<string>(request.choiceKeys, StringComparer.Ordinal);
        if (uniqueChoiceKeys.Count != RequiredCardCountPerPlayer)
        {
            throw new InvalidOperationException("A005 parallel condition requires two unique selected hand cards.");
        }

        if (!inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(
                request.actorPlayerId.Value,
                out var allowedChoiceKeys))
        {
            throw new InvalidOperationException("A005 parallel condition requires choices for actorPlayerId.");
        }

        if (!gameState.players.TryGetValue(request.actorPlayerId, out var playerState))
        {
            throw new InvalidOperationException("A005 parallel condition requires actor player state.");
        }

        foreach (var choiceKey in request.choiceKeys)
        {
            if (!allowedChoiceKeys.Contains(choiceKey))
            {
                throw new InvalidOperationException("A005 parallel condition requires every choiceKey to be allowed for actorPlayerId.");
            }

            var cardInstanceId = parseHandCardChoiceKey(choiceKey);
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
                cardInstance.ownerPlayerId != request.actorPlayerId)
            {
                throw new InvalidOperationException("A005 parallel condition requires selected cards to exist and be owned by actorPlayerId.");
            }

            if (cardInstance.zoneId != playerState.handZoneId)
            {
                throw new InvalidOperationException("A005 parallel condition requires selected cards to remain in actor hand.");
            }
        }
    }

    private static List<PlayerId> resolvePlayerOrder(IReadOnlyList<PlayerId> playerIds)
    {
        var result = new List<PlayerId>(playerIds);
        result.Sort((left, right) => left.Value.CompareTo(right.Value));
        return result;
    }

    private static List<string> resolveSelectedChoices(ActionChainState actionChainState, PlayerId playerId)
    {
        if (!actionChainState.localState.TryGetValue(
                createSelectedChoicesStateKey(playerId),
                out var serializedChoices) ||
            string.IsNullOrWhiteSpace(serializedChoices))
        {
            throw new InvalidOperationException("A005 parallel condition requires every player selection to be recorded.");
        }

        return new List<string>(serializedChoices.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void closeInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        long eventId)
    {
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = false,
        });
        gameState.currentInputContext = null;
        actionChainState.pendingContinuationKey = null;
    }

    private static string createSelectedChoicesStateKey(PlayerId playerId)
    {
        return SelectedChoicesLocalStatePrefix + playerId.Value;
    }

    private static string createHandCardChoiceKey(CardInstanceId cardInstanceId)
    {
        return HandCardChoicePrefix + cardInstanceId.Value;
    }

    private static CardInstanceId parseHandCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(HandCardChoicePrefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(HandCardChoicePrefix.Length), out var numericId))
        {
            throw new InvalidOperationException("A005 parallel condition choiceKey must encode handCard:{cardInstanceNumericId}.");
        }

        return new CardInstanceId(numericId);
    }
}
