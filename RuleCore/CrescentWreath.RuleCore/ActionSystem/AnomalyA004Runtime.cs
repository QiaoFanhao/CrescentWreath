using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyA004Runtime
{
    public const string ArrivalContinuationKey = "continuation:anomalyA004ArrivalReturnDefenseCards";
    public const string ConditionContinuationKey = "continuation:anomalyA004ConditionDiscardSkillPointCards";

    private const string ArrivalContextKey = "anomaly:A004:arrivalReturnDefenseCards";
    private const string ArrivalInputTypeKey = "anomalyA004ArrivalReturnDefenseCards";
    private const string ConditionContextKey = "anomaly:A004:conditionDiscardSkillPointCards";
    private const string ConditionInputTypeKey = "anomalyA004ConditionDiscardSkillPointCards";
    private const string DefenseCardChoicePrefix = "defenseCard:";
    private const string SkillPointCardChoicePrefix = "skillPointCard:";
    private const string KaguyaDefinitionId = "C020";
    private const int KaguyaHandTargetCount = 6;

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyA004Runtime(
        ZoneMovementService zoneMovementService,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool isA004ParallelInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               inputContextState.requiredPlayerIds.Count > 0 &&
               (string.Equals(inputContextState.contextKey, ArrivalContextKey, StringComparison.Ordinal) ||
                string.Equals(inputContextState.contextKey, ConditionContextKey, StringComparison.Ordinal));
    }

    public bool tryOpenArrivalInputOrComplete(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var requiredPlayerIds = resolvePlayersWithDefensePlacedCards(gameState);
        if (requiredPlayerIds.Count == 0)
        {
            drawKaguyaToSix(gameState, actionChainState, eventId);
            return false;
        }

        openParallelInputContext(
            gameState,
            actionChainState,
            requiredPlayerIds,
            ArrivalInputTypeKey,
            ArrivalContextKey,
            ArrivalContinuationKey,
            eventId,
            createDefenseCardChoicesForPlayer);
        return true;
    }

    public bool tryPrepareConditionPlayers(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        out List<PlayerId> requiredPlayerIds,
        out string? failedReasonKey)
    {
        requiredPlayerIds = new List<PlayerId>();
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
            return false;
        }

        foreach (var playerId in resolveSeatOrderPlayers(gameState))
        {
            if (!gameState.players.TryGetValue(playerId, out var playerState) ||
                playerState.teamId != actorPlayerState.teamId)
            {
                continue;
            }

            var choices = createSkillPointCardChoicesForPlayer(gameState, playerId);
            if (choices.Count == 0)
            {
                failedReasonKey = playerId == actorPlayerId
                    ? AnomalyValidationFailureKeys.ActorCannotPaySkillPointCard
                    : AnomalyValidationFailureKeys.FriendlyCannotPaySkillPointCard;
                requiredPlayerIds.Clear();
                return false;
            }

            requiredPlayerIds.Add(playerId);
        }

        if (requiredPlayerIds.Count == 0)
        {
            failedReasonKey = AnomalyValidationFailureKeys.FriendlyTeamPlayerMissing;
            return false;
        }

        failedReasonKey = null;
        return true;
    }

    public void openConditionInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        long eventId)
    {
        openParallelInputContext(
            gameState,
            actionChainState,
            requiredPlayerIds,
            ConditionInputTypeKey,
            ConditionContextKey,
            ConditionContinuationKey,
            eventId,
            createSkillPointCardChoicesForPlayer);
    }

    public bool continueParallelChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureParallelChoiceValid(gameState, inputContextState, request);
        inputContextState.submittedPlayerIds.Add(request.actorPlayerId);
        actionChainState.localState[createSelectedChoiceStateKey(request.actorPlayerId)] = request.choiceKey;

        if (inputContextState.submittedPlayerIds.Count < inputContextState.requiredPlayerIds.Count)
        {
            actionChainState.isCompleted = false;
            return false;
        }

        if (string.Equals(inputContextState.contextKey, ArrivalContextKey, StringComparison.Ordinal))
        {
            resolveArrivalChoices(gameState, actionChainState, inputContextState, request.requestId);
            drawKaguyaToSix(gameState, actionChainState, request.requestId);
        }
        else
        {
            resolveConditionChoices(gameState, actionChainState, inputContextState, request.requestId);
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    private void resolveArrivalChoices(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        long eventId)
    {
        foreach (var playerId in resolveSeatOrderPlayers(gameState))
        {
            if (!inputContextState.requiredPlayerIds.Contains(playerId))
            {
                continue;
            }

            var cardInstanceId = parseCardChoice(
                resolveSelectedChoice(actionChainState, playerId),
                DefenseCardChoicePrefix,
                "A004 arrival");
            var playerState = gameState.players[playerId];
            var cardInstance = gameState.cardInstances[cardInstanceId];
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                cardInstance,
                playerState.handZoneId,
                CardMoveReason.returnToSource,
                actionChainState.actionChainId,
                eventId);
            actionChainState.producedEvents.Add(movedEvent);
            cardInstance.isDefensePlacedOnField = false;
        }
    }

    private void resolveConditionChoices(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        long eventId)
    {
        foreach (var playerId in resolveSeatOrderPlayers(gameState))
        {
            if (!inputContextState.requiredPlayerIds.Contains(playerId))
            {
                continue;
            }

            var cardInstanceId = parseCardChoice(
                resolveSelectedChoice(actionChainState, playerId),
                SkillPointCardChoicePrefix,
                "A004 condition");
            var playerState = gameState.players[playerId];
            var cardInstance = gameState.cardInstances[cardInstanceId];
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                cardInstance,
                playerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                eventId);
            actionChainState.producedEvents.Add(movedEvent);
        }
    }

    private void ensureParallelChoiceValid(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        if (!inputContextState.requiredPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("A004 parallel input requires actorPlayerId to be a required player.");
        }

        if (inputContextState.submittedPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("A004 parallel input does not allow duplicate submission.");
        }

        if (!inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(
                request.actorPlayerId.Value,
                out var allowedChoices) ||
            !allowedChoices.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("A004 parallel input requires choiceKey to be allowed for actorPlayerId.");
        }

        var expectedPrefix = string.Equals(inputContextState.contextKey, ArrivalContextKey, StringComparison.Ordinal)
            ? DefenseCardChoicePrefix
            : SkillPointCardChoicePrefix;
        var cardInstanceId = parseCardChoice(request.choiceKey, expectedPrefix, "A004 parallel input");
        if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
            cardInstance.ownerPlayerId != request.actorPlayerId)
        {
            throw new InvalidOperationException("A004 parallel input requires selected card to exist and be owned by actorPlayerId.");
        }

        var playerState = gameState.players[request.actorPlayerId];
        if (string.Equals(inputContextState.contextKey, ArrivalContextKey, StringComparison.Ordinal))
        {
            if (cardInstance.zoneId != playerState.fieldZoneId || !cardInstance.isDefensePlacedOnField)
            {
                throw new InvalidOperationException("A004 arrival requires selected card to remain defense-placed in actor field.");
            }
        }
        else if (cardInstance.zoneId != playerState.handZoneId || !hasSkillPointGain(cardInstance.definitionId))
        {
            throw new InvalidOperationException("A004 condition requires selected card to remain a skill-point card in actor hand.");
        }
    }

    private void openParallelInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        long eventId,
        Func<RuleCore.GameState.GameState, PlayerId, List<string>> choiceFactory)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A004 input requires gameState.currentInputContext to be null.");
        }

        var inputContextState = new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };

        foreach (var playerId in requiredPlayerIds)
        {
            var choices = choiceFactory(gameState, playerId);
            if (choices.Count == 0)
            {
                continue;
            }

            inputContextState.requiredPlayerIds.Add(playerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[playerId.Value] = choices;
        }

        if (inputContextState.requiredPlayerIds.Count == 0)
        {
            throw new InvalidOperationException("A004 input requires at least one required player with choices.");
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = continuationKey;
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

    private static List<PlayerId> resolvePlayersWithDefensePlacedCards(RuleCore.GameState.GameState gameState)
    {
        var result = new List<PlayerId>();
        foreach (var playerId in resolveSeatOrderPlayers(gameState))
        {
            if (createDefenseCardChoicesForPlayer(gameState, playerId).Count > 0)
            {
                result.Add(playerId);
            }
        }

        return result;
    }

    private static List<string> createDefenseCardChoicesForPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        var choices = new List<string>();
        if (!gameState.players.TryGetValue(playerId, out var playerState) ||
            !gameState.zones.TryGetValue(playerState.fieldZoneId, out var fieldZoneState))
        {
            return choices;
        }

        foreach (var cardInstanceId in fieldZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            if (cardInstance.isDefensePlacedOnField)
            {
                choices.Add(DefenseCardChoicePrefix + cardInstanceId.Value);
            }
        }

        return choices;
    }

    private static List<string> createSkillPointCardChoicesForPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        var choices = new List<string>();
        if (!gameState.players.TryGetValue(playerId, out var playerState) ||
            !gameState.zones.TryGetValue(playerState.handZoneId, out var handZoneState))
        {
            return choices;
        }

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            if (hasSkillPointGain(cardInstance.definitionId))
            {
                choices.Add(SkillPointCardChoicePrefix + cardInstanceId.Value);
            }
        }

        return choices;
    }

    private static bool hasSkillPointGain(string definitionId)
    {
        return TreasureDefinitionRepository.resolveByDefinitionId(definitionId).skillPointGainOnPlay > 0;
    }

    private void drawKaguyaToSix(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        foreach (var playerState in gameState.players.Values)
        {
            if (!playerState.activeCharacterInstanceId.HasValue ||
                !gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var character) ||
                !string.Equals(character.definitionId, KaguyaDefinitionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!gameState.zones.TryGetValue(playerState.handZoneId, out var handZoneState))
            {
                continue;
            }

            while (handZoneState.cardInstanceIds.Count < KaguyaHandTargetCount)
            {
                if (!tryDrawOne(gameState, actionChainState, playerState, eventId))
                {
                    break;
                }
            }
        }
    }

    private bool tryDrawOne(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerState playerState,
        long eventId)
    {
        if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState) ||
            !gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState) ||
            !gameState.zones.ContainsKey(playerState.handZoneId))
        {
            return false;
        }
        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            foreach (var cardInstanceId in PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds))
            {
                var recoverEvent = zoneMovementService.moveCard(
                    gameState,
                    gameState.cardInstances[cardInstanceId],
                    playerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    eventId);
                actionChainState.producedEvents.Add(recoverEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count == 0)
        {
            return false;
        }

        var topCard = gameState.cardInstances[deckZoneState.cardInstanceIds[0]];
        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topCard,
            playerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(drawEvent);
        return true;
    }

    private static List<PlayerId> resolveSeatOrderPlayers(RuleCore.GameState.GameState gameState)
    {
        if (gameState.matchMeta is not null && gameState.matchMeta.seatOrder.Count > 0)
        {
            return new List<PlayerId>(gameState.matchMeta.seatOrder);
        }

        var playerIds = new List<PlayerId>(gameState.players.Keys);
        playerIds.Sort((left, right) => left.Value.CompareTo(right.Value));
        return playerIds;
    }

    private static string resolveSelectedChoice(ActionChainState actionChainState, PlayerId playerId)
    {
        if (!actionChainState.localState.TryGetValue(createSelectedChoiceStateKey(playerId), out var choiceKey))
        {
            throw new InvalidOperationException("A004 parallel input requires every required player choice to be recorded.");
        }

        return choiceKey;
    }

    private static string createSelectedChoiceStateKey(PlayerId playerId)
    {
        return "anomaly:A004:selectedChoice:" + playerId.Value;
    }

    private static CardInstanceId parseCardChoice(string choiceKey, string prefix, string context)
    {
        if (!choiceKey.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(prefix.Length), out var numericId))
        {
            throw new InvalidOperationException($"{context} requires a valid {prefix} choiceKey.");
        }

        return new CardInstanceId(numericId);
    }
}
