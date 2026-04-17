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

public sealed class AnomalyConditionInputRuntime
{
    private const string A002YoumuDefinitionId = "C005";
    private const int A002ConditionRequiredCardCountPerFriendlyPlayer = 1;
    private const string A002ConditionInputTypeKey = "anomalyA002ConditionFriendlyDiscardFromHand";
    private const string A002ConditionContextKey = "anomaly:A002:conditionFriendlyDiscardFromHand";
    private const string A002ConditionChoiceKeyPrefix = "handCard:";
    private const int A005ConditionRequiredCardCountPerFriendlyPlayer = 2;
    private const string A005ConditionInputTypeKey = "anomalyA005ConditionDefenseLikePlace";
    private const string A005ConditionContextKey = "anomaly:A005:conditionDefenseLikePlace";
    private const string A005ConditionChoiceKeyPrefix = "handCard:";
    private const string A008ConditionInputTypeKeyOpponentOptionalDiscardReturn = "anomalyA008ConditionOpponentOptionalDiscardReturn";
    private const string A008ConditionContextKey = "anomaly:A008:conditionOpponentOptionalDiscardReturn";
    private const string A008ConditionChoiceKeyPrefix = "discardCard:";
    private const string A008ConditionChoiceKeyDecline = "decline";
    private const string A009ConditionInputTypeKeyBarrierOptional = "anomalyA009ConditionOpponentOptionalBenefit";
    private const string A009ConditionInputTypeKeySakuraOptional = "anomalyA009ConditionOpponentOptionalSakura";
    private const string A009ConditionContextKey = "anomaly:A009:conditionOpponentOptionalBenefit";
    private const string A009ConditionChoiceKeyAccept = "accept";
    private const string A009ConditionChoiceKeyDecline = "decline";

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;
    private static readonly IReadOnlyList<A009ConditionOptionalStepDefinition> A009ConditionOptionalSteps =
        new List<A009ConditionOptionalStepDefinition>
        {
            new(
                "barrierOptional",
                A009ConditionInputTypeKeyBarrierOptional),
            new(
                "sakuraOptional",
                A009ConditionInputTypeKeySakuraOptional),
        };

    public AnomalyConditionInputRuntime(ZoneMovementService zoneMovementService, Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool tryPrepareA002FriendlyTeamPlayerIdsForConditionInput(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        out List<PlayerId> friendlyTeamPlayerIds,
        out string? failedReasonKey)
    {
        friendlyTeamPlayerIds = new List<PlayerId>();

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
            return false;
        }

        var allFriendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorPlayerState.teamId);
        if (allFriendlyTeamPlayerIds.Count == 0)
        {
            failedReasonKey = AnomalyValidationFailureKeys.FriendlyTeamPlayerMissing;
            return false;
        }

        friendlyTeamPlayerIds = new List<PlayerId>(allFriendlyTeamPlayerIds.Count);
        foreach (var friendlyPlayerId in allFriendlyTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.FriendlyPlayerStateMissing;
                return false;
            }

            if (isA002YoumuExemptFromDiscard(gameState, friendlyPlayerState))
            {
                continue;
            }

            friendlyTeamPlayerIds.Add(friendlyPlayerId);

            if (!gameState.zones.TryGetValue(friendlyPlayerState.handZoneId, out var friendlyHandZoneState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
                return false;
            }

            if (friendlyHandZoneState.cardInstanceIds.Count < A002ConditionRequiredCardCountPerFriendlyPlayer)
            {
                failedReasonKey = AnomalyValidationFailureKeys.InsufficientFriendlyHandCards;
                return false;
            }
        }

        failedReasonKey = null;
        return true;
    }

    public static bool isValidA005ConditionDefenseLikePlaceChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (submitInputChoiceActionRequest.choiceKeys.Count != A005ConditionRequiredCardCountPerFriendlyPlayer)
        {
            return false;
        }

        var uniqueChoiceKeys = new HashSet<string>(submitInputChoiceActionRequest.choiceKeys, StringComparer.Ordinal);
        if (uniqueChoiceKeys.Count != A005ConditionRequiredCardCountPerFriendlyPlayer)
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

    public bool tryPrepareA005FriendlyTeamPlayerIdsForConditionInput(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        out List<PlayerId> friendlyTeamPlayerIds,
        out string? failedReasonKey)
    {
        friendlyTeamPlayerIds = new List<PlayerId>();

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
            return false;
        }

        friendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorPlayerState.teamId);
        if (friendlyTeamPlayerIds.Count == 0)
        {
            failedReasonKey = AnomalyValidationFailureKeys.FriendlyTeamPlayerMissing;
            return false;
        }

        foreach (var friendlyPlayerId in friendlyTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.FriendlyPlayerStateMissing;
                return false;
            }

            if (!gameState.zones.TryGetValue(friendlyPlayerState.handZoneId, out var friendlyHandZoneState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
                return false;
            }

            if (friendlyHandZoneState.cardInstanceIds.Count < A005ConditionRequiredCardCountPerFriendlyPlayer)
            {
                failedReasonKey = AnomalyValidationFailureKeys.InsufficientFriendlyHandCards;
                return false;
            }
        }

        failedReasonKey = null;
        return true;
    }

    public bool tryPrepareA008OpponentPlayerIdsForConditionInput(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        out List<PlayerId> opponentPlayerIdsWithDiscardCards,
        out string? failedReasonKey)
    {
        opponentPlayerIdsWithDiscardCards = new List<PlayerId>();

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
            return false;
        }

        var opponentTeamPlayerIds = resolveOpponentTeamPlayerIds(gameState, actorPlayerState.teamId);
        foreach (var opponentPlayerId in opponentTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(opponentPlayerId, out var opponentPlayerState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.TargetPlayerMissing;
                return false;
            }

            if (!gameState.zones.TryGetValue(opponentPlayerState.discardZoneId, out var opponentDiscardZoneState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
                return false;
            }

            foreach (var discardCardInstanceId in opponentDiscardZoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.ContainsKey(discardCardInstanceId))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardSourceCardMissing;
                    return false;
                }
            }

            if (opponentDiscardZoneState.cardInstanceIds.Count > 0)
            {
                opponentPlayerIdsWithDiscardCards.Add(opponentPlayerId);
            }
        }

        failedReasonKey = null;
        return true;
    }

    public bool tryPrepareA009OpponentPlayerIdsForConditionInput(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        out List<PlayerId> opponentPlayerIds,
        out string? failedReasonKey)
    {
        opponentPlayerIds = new List<PlayerId>();

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
            return false;
        }

        if (gameState.publicState is null)
        {
            failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
            return false;
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.sakuraCakeDeckZoneId, out var sakuraCakeDeckZoneState))
        {
            failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
            return false;
        }

        foreach (var sakuraCakeCardInstanceId in sakuraCakeDeckZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(sakuraCakeCardInstanceId))
            {
                failedReasonKey = AnomalyValidationFailureKeys.RewardSourceCardMissing;
                return false;
            }
        }

        opponentPlayerIds = resolveOpponentTeamPlayerIds(gameState, actorPlayerState.teamId);
        foreach (var opponentPlayerId in opponentPlayerIds)
        {
            if (!gameState.players.TryGetValue(opponentPlayerId, out var opponentPlayerState))
            {
                failedReasonKey = AnomalyValidationFailureKeys.TargetPlayerMissing;
                return false;
            }

            if (!gameState.zones.ContainsKey(opponentPlayerState.discardZoneId))
            {
                failedReasonKey = AnomalyValidationFailureKeys.RewardTargetZoneMissing;
                return false;
            }
        }

        failedReasonKey = null;
        return true;
    }

    public void openA005ConditionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredFriendlyPlayerId,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A005 anomaly condition input requires gameState.currentInputContext to be null before opening.");
        }

        if (!gameState.players.TryGetValue(requiredFriendlyPlayerId, out var requiredFriendlyPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly condition input requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredFriendlyPlayerState.handZoneId, out var requiredFriendlyHandZoneState))
        {
            throw new InvalidOperationException("A005 anomaly condition input requires required player handZoneId to exist in gameState.zones.");
        }

        openConditionInputContext(
            gameState,
            actionChainState,
            requiredFriendlyPlayerId,
            A005ConditionInputTypeKey,
            A005ConditionContextKey,
            AnomalyProcessor.ContinuationKeyA005ConditionDefenseLikePlace,
            createA005ConditionChoiceKeys(requiredFriendlyHandZoneState.cardInstanceIds),
            eventId);
    }

    public void openA002ConditionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredFriendlyPlayerId,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A002 anomaly condition input requires gameState.currentInputContext to be null before opening.");
        }

        if (!gameState.players.TryGetValue(requiredFriendlyPlayerId, out var requiredFriendlyPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly condition input requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredFriendlyPlayerState.handZoneId, out var requiredFriendlyHandZoneState))
        {
            throw new InvalidOperationException("A002 anomaly condition input requires required player handZoneId to exist in gameState.zones.");
        }

        openConditionInputContext(
            gameState,
            actionChainState,
            requiredFriendlyPlayerId,
            A002ConditionInputTypeKey,
            A002ConditionContextKey,
            AnomalyProcessor.ContinuationKeyA002ConditionFriendlyDiscardFromHand,
            createA002ConditionChoiceKeys(requiredFriendlyHandZoneState.cardInstanceIds),
            eventId);
    }

    public void openA008ConditionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A008 anomaly condition input requires gameState.currentInputContext to be null before opening.");
        }

        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var requiredOpponentPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly condition input requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredOpponentPlayerState.discardZoneId, out var requiredOpponentDiscardZoneState))
        {
            throw new InvalidOperationException("A008 anomaly condition input requires required player discardZoneId to exist in gameState.zones.");
        }

        openConditionInputContext(
            gameState,
            actionChainState,
            requiredOpponentPlayerId,
            A008ConditionInputTypeKeyOpponentOptionalDiscardReturn,
            A008ConditionContextKey,
            AnomalyProcessor.ContinuationKeyA008ConditionOpponentOptionalDiscardReturn,
            createA008ConditionChoiceKeys(requiredOpponentDiscardZoneState.cardInstanceIds),
            eventId);
    }

    public void openA009ConditionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A009 anomaly condition input requires gameState.currentInputContext to be null before opening.");
        }

        if (!gameState.players.ContainsKey(requiredOpponentPlayerId))
        {
            throw new InvalidOperationException("A009 anomaly condition input requires requiredPlayerId to exist in gameState.players.");
        }

        openA009ConditionInputContextForStep(
            gameState,
            actionChainState,
            requiredOpponentPlayerId,
            A009ConditionOptionalSteps[0],
            eventId);
    }

    private void openA009ConditionInputContextForStep(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId,
        A009ConditionOptionalStepDefinition stepDefinition,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A009 anomaly condition input requires gameState.currentInputContext to be null before opening.");
        }

        if (!gameState.players.ContainsKey(requiredOpponentPlayerId))
        {
            throw new InvalidOperationException("A009 anomaly condition input requires requiredPlayerId to exist in gameState.players.");
        }

        openConditionInputContext(
            gameState,
            actionChainState,
            requiredOpponentPlayerId,
            stepDefinition.inputTypeKey,
            A009ConditionContextKey,
            AnomalyProcessor.ContinuationKeyA009ConditionOpponentOptionalBenefit,
            new List<string>
            {
                A009ConditionChoiceKeyAccept,
                A009ConditionChoiceKeyDecline,
            },
            eventId);
    }

    public void ensureValidA005ConditionDefenseLikeChoicesForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredPlayerId = ensureValidConditionContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest.actorPlayerId,
            A005ConditionContextKey,
            "A005 anomaly condition continuation requires currentInputContext.contextKey to be anomaly:A005:conditionDefenseLikePlace.",
            "A005 anomaly condition continuation requires currentInputContext.requiredPlayerId.",
            "A005 anomaly condition continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires required player handZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceIds = parseA005ConditionChoiceKeys(submitInputChoiceActionRequest.choiceKeys);
        foreach (var selectedCardInstanceId in selectedCardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("A005 anomaly condition continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
            }

            if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
            {
                throw new InvalidOperationException("A005 anomaly condition continuation requires selected cards to be owned by currentInputContext.requiredPlayerId.");
            }

            if (selectedCardInstance.zoneId != requiredPlayerState.handZoneId)
            {
                throw new InvalidOperationException("A005 anomaly condition continuation requires selected cards to still be in required player hand zone.");
            }
        }
    }

    public static bool isValidA008ConditionOpponentOptionalDiscardReturnChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey))
        {
            return false;
        }

        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public static bool isValidA002ConditionFriendlyDiscardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey))
        {
            return false;
        }

        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public void ensureValidA002ConditionFriendlyDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredFriendlyPlayerId = ensureValidConditionContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest.actorPlayerId,
            A002ConditionContextKey,
            "A002 anomaly condition continuation requires currentInputContext.contextKey to be anomaly:A002:conditionFriendlyDiscardFromHand.",
            "A002 anomaly condition continuation requires currentInputContext.requiredPlayerId.",
            "A002 anomaly condition continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        if (!string.Equals(inputContextState.inputTypeKey, A002ConditionInputTypeKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires currentInputContext.inputTypeKey to be anomalyA002ConditionFriendlyDiscardFromHand.");
        }

        if (!gameState.players.TryGetValue(requiredFriendlyPlayerId, out var requiredFriendlyPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredFriendlyPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires required player handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(requiredFriendlyPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        if (!isValidA002ConditionFriendlyDiscardChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        var selectedCardInstanceId = parseA002ConditionChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.ownerPlayerId != requiredFriendlyPlayerId)
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires selected card to be owned by currentInputContext.requiredPlayerId.");
        }

        if (selectedCardInstance.zoneId != requiredFriendlyPlayerState.handZoneId)
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires selected card to still be in required player hand zone.");
        }
    }

    public void ensureValidA008ConditionOpponentOptionalDiscardReturnChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredOpponentPlayerId = ensureValidConditionContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest.actorPlayerId,
            A008ConditionContextKey,
            "A008 anomaly condition continuation requires currentInputContext.contextKey to be anomaly:A008:conditionOpponentOptionalDiscardReturn.",
            "A008 anomaly condition continuation requires currentInputContext.requiredPlayerId.",
            "A008 anomaly condition continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        if (!string.Equals(inputContextState.inputTypeKey, A008ConditionInputTypeKeyOpponentOptionalDiscardReturn, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires currentInputContext.inputTypeKey to be anomalyA008ConditionOpponentOptionalDiscardReturn.");
        }

        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var requiredOpponentPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredOpponentPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(requiredOpponentPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires required player handZoneId to exist in gameState.zones.");
        }

        if (!isValidA008ConditionOpponentOptionalDiscardReturnChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        if (submitInputChoiceActionRequest.choiceKey == A008ConditionChoiceKeyDecline)
        {
            return;
        }

        var selectedCardInstanceId = parseA008ConditionChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.zoneId != requiredOpponentPlayerState.discardZoneId)
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires selected card to still be in required player discard zone.");
        }
    }

    public static bool isValidA009ConditionOpponentOptionalBenefitChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey))
        {
            return false;
        }

        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public void ensureValidA009ConditionOpponentOptionalBenefitChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredOpponentPlayerId = ensureValidConditionContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest.actorPlayerId,
            A009ConditionContextKey,
            "A009 anomaly condition continuation requires currentInputContext.contextKey to be anomaly:A009:conditionOpponentOptionalBenefit.",
            "A009 anomaly condition continuation requires currentInputContext.requiredPlayerId.",
            "A009 anomaly condition continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        if (!isA009ConditionStageInputType(inputContextState.inputTypeKey))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires currentInputContext.inputTypeKey to be a supported A009 condition stage.");
        }

        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var requiredOpponentPlayerState))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredOpponentPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        if (!isValidA009ConditionOpponentOptionalBenefitChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }
    }

    public AnomalyConditionInputAdvanceResult continueA005ConditionDefenseLikePlaceContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA005ConditionDefenseLikeChoicesForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires currentInputContext.requiredPlayerId.");
        }

        var payingFriendlyPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(payingFriendlyPlayerId, out var payingFriendlyPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(payingFriendlyPlayerState.fieldZoneId))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires required player fieldZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceIds = parseA005ConditionChoiceKeys(submitInputChoiceActionRequest.choiceKeys);
        foreach (var selectedCardInstanceId in selectedCardInstanceIds)
        {
            var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                payingFriendlyPlayerState.fieldZoneId,
                CardMoveReason.defensePlace,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(movedEvent);
            selectedCardInstance.isDefensePlacedOnField = true;
        }

        if (!actionChainState.actorPlayerId.HasValue ||
            !gameState.players.TryGetValue(actionChainState.actorPlayerId.Value, out var actorPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires actionChain.actorPlayerId to exist in gameState.players.");
        }

        var friendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorPlayerState.teamId);

        return advanceConditionInputParticipantOrComplete(
            friendlyTeamPlayerIds,
            payingFriendlyPlayerId,
            "A005 anomaly condition continuation requires requiredPlayerId to exist in friendly team payment order.",
            nextFriendlyPlayerId => openA005ConditionInputContext(
                gameState,
                actionChainState,
                nextFriendlyPlayerId,
                submitInputChoiceActionRequest.requestId));
    }

    public AnomalyConditionInputAdvanceResult continueA002ConditionFriendlyDiscardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA002ConditionFriendlyDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires currentInputContext.requiredPlayerId.");
        }

        var payingFriendlyPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(payingFriendlyPlayerId, out var payingFriendlyPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        var selectedCardInstanceId = parseA002ConditionChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            payingFriendlyPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(movedEvent);

        if (!actionChainState.actorPlayerId.HasValue ||
            !gameState.players.TryGetValue(actionChainState.actorPlayerId.Value, out var actorPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires actionChain.actorPlayerId to exist in gameState.players.");
        }

        var friendlyTeamPlayerIds = resolveA002FriendlyTeamPaymentPlayerIds(gameState, actorPlayerState.teamId);
        return advanceConditionInputParticipantOrComplete(
            friendlyTeamPlayerIds,
            payingFriendlyPlayerId,
            "A002 anomaly condition continuation requires requiredPlayerId to exist in friendly team payment order.",
            nextFriendlyPlayerId => openA002ConditionInputContext(
                gameState,
                actionChainState,
                nextFriendlyPlayerId,
                submitInputChoiceActionRequest.requestId));
    }

    public AnomalyConditionInputAdvanceResult continueA008ConditionOpponentOptionalDiscardReturnContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA008ConditionOpponentOptionalDiscardReturnChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires currentInputContext.requiredPlayerId.");
        }

        var requiredOpponentPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var requiredOpponentPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (submitInputChoiceActionRequest.choiceKey != A008ConditionChoiceKeyDecline)
        {
            var selectedCardInstanceId = parseA008ConditionChoiceKey(submitInputChoiceActionRequest.choiceKey);
            var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];

            var movedEvent = zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                requiredOpponentPlayerState.handZoneId,
                CardMoveReason.returnToSource,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(movedEvent);
        }

        if (!actionChainState.actorPlayerId.HasValue ||
            !gameState.players.TryGetValue(actionChainState.actorPlayerId.Value, out var actorPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires actionChain.actorPlayerId to exist in gameState.players.");
        }

        var opponentTeamPlayerIds = resolveOpponentTeamPlayerIds(gameState, actorPlayerState.teamId);
        var currentOpponentIndex = indexOfPlayerId(opponentTeamPlayerIds, requiredOpponentPlayerId);
        if (currentOpponentIndex < 0)
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires requiredPlayerId to exist in opponent team payment order.");
        }

        var nextOpponentPlayerId = findNextA008OpponentPlayerWithDiscardCards(
            gameState,
            opponentTeamPlayerIds,
            currentOpponentIndex);
        if (!nextOpponentPlayerId.HasValue)
        {
            return AnomalyConditionInputAdvanceResult.createCompleted();
        }

        openA008ConditionInputContext(
            gameState,
            actionChainState,
            nextOpponentPlayerId.Value,
            submitInputChoiceActionRequest.requestId);
        return AnomalyConditionInputAdvanceResult.createPending();
    }

    public AnomalyConditionInputAdvanceResult continueA009ConditionOpponentOptionalBenefitContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA009ConditionOpponentOptionalBenefitChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires currentInputContext.requiredPlayerId.");
        }

        var selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;
        var requiredOpponentPlayerId = inputContextState.requiredPlayerId.Value;
        var inputTypeKey = inputContextState.inputTypeKey;
        var stepDefinition = resolveA009ConditionOptionalStepDefinition(inputTypeKey);

        if (!actionChainState.actorPlayerId.HasValue ||
            !gameState.players.TryGetValue(actionChainState.actorPlayerId.Value, out var actorPlayerState))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires actionChain.actorPlayerId to exist in gameState.players.");
        }

        if (selectedChoiceKey == A009ConditionChoiceKeyAccept)
        {
            applyA009AcceptedOpponentOptionalStepBenefit(
                gameState,
                actionChainState,
                requiredOpponentPlayerId,
                stepDefinition,
                submitInputChoiceActionRequest.requestId);
        }

        if (tryResolveNextA009ConditionOptionalStepDefinition(stepDefinition, out var nextStepDefinition))
        {
            openA009ConditionInputContextForStep(
                gameState,
                actionChainState,
                requiredOpponentPlayerId,
                nextStepDefinition,
                submitInputChoiceActionRequest.requestId);
            return AnomalyConditionInputAdvanceResult.createPending();
        }

        var opponentTeamPlayerIds = resolveOpponentTeamPlayerIds(gameState, actorPlayerState.teamId);

        return advanceConditionInputParticipantOrComplete(
            opponentTeamPlayerIds,
            requiredOpponentPlayerId,
            "A009 anomaly condition continuation requires requiredPlayerId to exist in opponent team payment order.",
            nextOpponentPlayerId => openA009ConditionInputContext(
                gameState,
                actionChainState,
                nextOpponentPlayerId,
                submitInputChoiceActionRequest.requestId));
    }

    private void openConditionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        string pendingContinuationKey,
        List<string> choiceKeys,
        long eventId)
    {
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = pendingContinuationKey;
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

    private static PlayerId ensureValidConditionContinuationEnvironment(
        InputContextState inputContextState,
        PlayerId actorPlayerId,
        string expectedContextKey,
        string invalidContextMessage,
        string requiredPlayerMissingMessage,
        string actorMismatchMessage)
    {
        if (inputContextState.contextKey != expectedContextKey)
        {
            throw new InvalidOperationException(invalidContextMessage);
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException(requiredPlayerMissingMessage);
        }

        var requiredPlayerId = inputContextState.requiredPlayerId.Value;
        if (requiredPlayerId != actorPlayerId)
        {
            throw new InvalidOperationException(actorMismatchMessage);
        }

        return requiredPlayerId;
    }

    private static AnomalyConditionInputAdvanceResult advanceConditionInputParticipantOrComplete(
        IReadOnlyList<PlayerId> participantPlayerIds,
        PlayerId currentParticipantPlayerId,
        string participantMissingMessage,
        Action<PlayerId> openNextParticipantInputContext)
    {
        var currentParticipantIndex = indexOfPlayerId(participantPlayerIds, currentParticipantPlayerId);
        if (currentParticipantIndex < 0)
        {
            throw new InvalidOperationException(participantMissingMessage);
        }

        var nextParticipantIndex = currentParticipantIndex + 1;
        if (nextParticipantIndex < participantPlayerIds.Count)
        {
            openNextParticipantInputContext(participantPlayerIds[nextParticipantIndex]);
            return AnomalyConditionInputAdvanceResult.createPending();
        }

        return AnomalyConditionInputAdvanceResult.createCompleted();
    }

    private static int indexOfPlayerId(IReadOnlyList<PlayerId> participantPlayerIds, PlayerId targetPlayerId)
    {
        for (var participantIndex = 0; participantIndex < participantPlayerIds.Count; participantIndex++)
        {
            if (participantPlayerIds[participantIndex] == targetPlayerId)
            {
                return participantIndex;
            }
        }

        return -1;
    }

    private static List<string> createA005ConditionChoiceKeys(List<CardInstanceId> handCardInstanceIds)
    {
        var choiceKeys = new List<string>(handCardInstanceIds.Count);
        foreach (var handCardInstanceId in handCardInstanceIds)
        {
            choiceKeys.Add(createA005ConditionChoiceKey(handCardInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createA002ConditionChoiceKeys(List<CardInstanceId> handCardInstanceIds)
    {
        var choiceKeys = new List<string>(handCardInstanceIds.Count);
        foreach (var handCardInstanceId in handCardInstanceIds)
        {
            choiceKeys.Add(createA002ConditionChoiceKey(handCardInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createA008ConditionChoiceKeys(List<CardInstanceId> discardCardInstanceIds)
    {
        var choiceKeys = new List<string>(discardCardInstanceIds.Count + 1)
        {
            A008ConditionChoiceKeyDecline,
        };

        foreach (var discardCardInstanceId in discardCardInstanceIds)
        {
            choiceKeys.Add(createA008ConditionChoiceKey(discardCardInstanceId));
        }

        return choiceKeys;
    }

    private static string createA005ConditionChoiceKey(CardInstanceId cardInstanceId)
    {
        return A005ConditionChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createA002ConditionChoiceKey(CardInstanceId cardInstanceId)
    {
        return A002ConditionChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createA008ConditionChoiceKey(CardInstanceId cardInstanceId)
    {
        return A008ConditionChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static List<CardInstanceId> parseA005ConditionChoiceKeys(List<string> choiceKeys)
    {
        var cardInstanceIds = new List<CardInstanceId>(choiceKeys.Count);
        foreach (var choiceKey in choiceKeys)
        {
            if (!choiceKey.StartsWith(A005ConditionChoiceKeyPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A005 anomaly condition continuation choiceKey must start with handCard: prefix.");
            }

            var cardIdSegment = choiceKey.Substring(A005ConditionChoiceKeyPrefix.Length);
            if (!long.TryParse(cardIdSegment, out var cardNumericId))
            {
                throw new InvalidOperationException("A005 anomaly condition continuation choiceKey must encode a valid CardInstanceId numeric value.");
            }

            cardInstanceIds.Add(new CardInstanceId(cardNumericId));
        }

        return cardInstanceIds;
    }

    private static CardInstanceId parseA008ConditionChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A008ConditionChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation choiceKey must start with discardCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A008ConditionChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA002ConditionChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A002ConditionChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation choiceKey must start with handCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A002ConditionChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static List<PlayerId> resolveFriendlyTeamPlayerIds(
        RuleCore.GameState.GameState gameState,
        TeamId actorTeamId)
    {
        if (gameState.teams.TryGetValue(actorTeamId, out var actorTeamState) &&
            actorTeamState.memberPlayerIds.Count > 0)
        {
            return new List<PlayerId>(actorTeamState.memberPlayerIds);
        }

        var friendlyTeamPlayerIds = new List<PlayerId>();
        foreach (var playerStateEntry in gameState.players)
        {
            if (playerStateEntry.Value.teamId == actorTeamId)
            {
                friendlyTeamPlayerIds.Add(playerStateEntry.Key);
            }
        }

        return friendlyTeamPlayerIds;
    }

    private static List<PlayerId> resolveA002FriendlyTeamPaymentPlayerIds(
        RuleCore.GameState.GameState gameState,
        TeamId actorTeamId)
    {
        var allFriendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorTeamId);
        var paymentFriendlyTeamPlayerIds = new List<PlayerId>(allFriendlyTeamPlayerIds.Count);
        foreach (var friendlyPlayerId in allFriendlyTeamPlayerIds)
        {
            if (gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState) &&
                isA002YoumuExemptFromDiscard(gameState, friendlyPlayerState))
            {
                continue;
            }

            paymentFriendlyTeamPlayerIds.Add(friendlyPlayerId);
        }

        return paymentFriendlyTeamPlayerIds;
    }

    private static bool isA002YoumuExemptFromDiscard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState)
    {
        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
        {
            return false;
        }

        return string.Equals(activeCharacterInstance.definitionId, A002YoumuDefinitionId, StringComparison.Ordinal);
    }

    private static List<PlayerId> resolveOpponentTeamPlayerIds(
        RuleCore.GameState.GameState gameState,
        TeamId actorTeamId)
    {
        var opponentTeamPlayerIds = new List<PlayerId>();
        foreach (var teamStateEntry in gameState.teams)
        {
            if (teamStateEntry.Key == actorTeamId)
            {
                continue;
            }

            if (teamStateEntry.Value.memberPlayerIds.Count > 0)
            {
                opponentTeamPlayerIds.AddRange(teamStateEntry.Value.memberPlayerIds);
            }
        }

        if (opponentTeamPlayerIds.Count > 0)
        {
            return opponentTeamPlayerIds;
        }

        foreach (var playerStateEntry in gameState.players)
        {
            if (playerStateEntry.Value.teamId != actorTeamId)
            {
                opponentTeamPlayerIds.Add(playerStateEntry.Key);
            }
        }

        return opponentTeamPlayerIds;
    }

    private static PlayerId? findNextA008OpponentPlayerWithDiscardCards(
        RuleCore.GameState.GameState gameState,
        IReadOnlyList<PlayerId> opponentTeamPlayerIds,
        int currentOpponentIndex)
    {
        for (var nextOpponentIndex = currentOpponentIndex + 1; nextOpponentIndex < opponentTeamPlayerIds.Count; nextOpponentIndex++)
        {
            var candidateOpponentPlayerId = opponentTeamPlayerIds[nextOpponentIndex];
            if (!gameState.players.TryGetValue(candidateOpponentPlayerId, out var candidateOpponentPlayerState))
            {
                throw new InvalidOperationException("A008 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
            }

            if (!gameState.zones.TryGetValue(candidateOpponentPlayerState.discardZoneId, out var candidateOpponentDiscardZoneState))
            {
                throw new InvalidOperationException("A008 anomaly condition continuation requires required player discardZoneId to exist in gameState.zones.");
            }

            if (candidateOpponentDiscardZoneState.cardInstanceIds.Count > 0)
            {
                return candidateOpponentPlayerId;
            }
        }

        return null;
    }

    private static bool isA009ConditionStageInputType(string? inputTypeKey)
    {
        return tryResolveA009ConditionOptionalStepDefinition(inputTypeKey, out _);
    }

    private static bool tryResolveA009ConditionOptionalStepDefinition(
        string? inputTypeKey,
        out A009ConditionOptionalStepDefinition? stepDefinition)
    {
        foreach (var candidateStep in A009ConditionOptionalSteps)
        {
            if (string.Equals(candidateStep.inputTypeKey, inputTypeKey, StringComparison.Ordinal))
            {
                stepDefinition = candidateStep;
                return true;
            }
        }

        stepDefinition = null;
        return false;
    }

    private static A009ConditionOptionalStepDefinition resolveA009ConditionOptionalStepDefinition(string? inputTypeKey)
    {
        if (!tryResolveA009ConditionOptionalStepDefinition(inputTypeKey, out var stepDefinition))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires currentInputContext.inputTypeKey to be a supported A009 condition stage.");
        }

        return stepDefinition!;
    }

    private static bool tryResolveNextA009ConditionOptionalStepDefinition(
        A009ConditionOptionalStepDefinition currentStepDefinition,
        out A009ConditionOptionalStepDefinition nextStepDefinition)
    {
        for (var stepIndex = 0; stepIndex < A009ConditionOptionalSteps.Count; stepIndex++)
        {
            if (!string.Equals(A009ConditionOptionalSteps[stepIndex].stepKey, currentStepDefinition.stepKey, StringComparison.Ordinal))
            {
                continue;
            }

            var nextStepIndex = stepIndex + 1;
            if (nextStepIndex < A009ConditionOptionalSteps.Count)
            {
                nextStepDefinition = A009ConditionOptionalSteps[nextStepIndex];
                return true;
            }

            break;
        }

        nextStepDefinition = null!;
        return false;
    }

    private void applyA009AcceptedOpponentOptionalStepBenefit(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId,
        A009ConditionOptionalStepDefinition stepDefinition,
        long eventId)
    {
        if (string.Equals(stepDefinition.stepKey, "barrierOptional", StringComparison.Ordinal))
        {
            applyA009AcceptedOpponentBarrierOptionalBenefit(
                gameState,
                actionChainState,
                requiredOpponentPlayerId);
            return;
        }

        if (string.Equals(stepDefinition.stepKey, "sakuraOptional", StringComparison.Ordinal))
        {
            applyA009AcceptedOpponentSakuraOptionalBenefit(
                gameState,
                actionChainState,
                requiredOpponentPlayerId,
                eventId);
            return;
        }

        throw new InvalidOperationException("A009 anomaly condition continuation requires currentInputContext.inputTypeKey to be a supported A009 condition stage.");
    }

    private static void applyA009AcceptedOpponentBarrierOptionalBenefit(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId)
    {
        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var opponentPlayerState))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (opponentPlayerState.activeCharacterInstanceId.HasValue &&
            gameState.characterInstances.ContainsKey(opponentPlayerState.activeCharacterInstanceId.Value))
        {
            StatusRuntime.applyStatus(gameState, new StatusInstance
            {
                statusKey = "Barrier",
                applierPlayerId = actionChainState.actorPlayerId,
                targetCharacterInstanceId = opponentPlayerState.activeCharacterInstanceId.Value,
                stackCount = 1,
            });
        }
    }

    private void applyA009AcceptedOpponentSakuraOptionalBenefit(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredOpponentPlayerId,
        long eventId)
    {
        if (!gameState.players.TryGetValue(requiredOpponentPlayerId, out var opponentPlayerState))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (gameState.publicState is null ||
            !gameState.zones.TryGetValue(gameState.publicState.sakuraCakeDeckZoneId, out var sakuraCakeDeckZoneState))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires gameState.publicState.sakuraCakeDeckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(opponentPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        if (sakuraCakeDeckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topSakuraCakeCardInstanceId = sakuraCakeDeckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topSakuraCakeCardInstanceId, out var topSakuraCakeCardInstance))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires sakura cake deck cardInstanceIds to exist in gameState.cardInstances.");
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            topSakuraCakeCardInstance,
            opponentPlayerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(movedEvent);
    }

    private sealed class A009ConditionOptionalStepDefinition
    {
        public A009ConditionOptionalStepDefinition(string stepKey, string inputTypeKey)
        {
            this.stepKey = stepKey;
            this.inputTypeKey = inputTypeKey;
        }

        public string stepKey { get; }
        public string inputTypeKey { get; }
    }
}

public sealed class AnomalyConditionInputAdvanceResult
{
    public bool isCompleted { get; private set; }

    public static AnomalyConditionInputAdvanceResult createPending()
    {
        return new AnomalyConditionInputAdvanceResult
        {
            isCompleted = false,
        };
    }

    public static AnomalyConditionInputAdvanceResult createCompleted()
    {
        return new AnomalyConditionInputAdvanceResult
        {
            isCompleted = true,
        };
    }
}
