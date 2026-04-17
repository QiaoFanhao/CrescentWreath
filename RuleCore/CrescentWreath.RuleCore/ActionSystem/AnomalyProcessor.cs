using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyProcessor
{
    public const string ContinuationKeyA002ConditionFriendlyDiscardFromHand = "continuation:anomalyA002ConditionFriendlyDiscardFromHand";
    public const string ContinuationKeyA005ConditionDefenseLikePlace = "continuation:anomalyA005ConditionDefenseLikePlace";
    public const string ContinuationKeyA005SelectSummonCardToHand = "continuation:anomalyA005SelectSummonCardToHand";
    public const string ContinuationKeyA005ArrivalDirectSummonFromSummonZone = "continuation:anomalyA005ArrivalDirectSummonFromSummonZone";
    public const string ContinuationKeyA003ArrivalSelectOpponentShackle = "continuation:anomalyA003ArrivalSelectOpponentShackle";
    public const string ContinuationKeyA007ArrivalOptionalBanishFlow = "continuation:anomalyA007ArrivalOptionalBanishFlow";
    public const string ContinuationKeyA001ArrivalHumanDiscardFlow = "continuation:anomalyA001ArrivalHumanDiscardFlow";
    public const string ContinuationKeyA006ArrivalHumanDefenseDiscardFlow = "continuation:anomalyA006ArrivalHumanDefenseDiscardFlow";
    public const string ContinuationKeyA008ConditionOpponentOptionalDiscardReturn = "continuation:anomalyA008ConditionOpponentOptionalDiscardReturn";
    public const string ContinuationKeyA008RewardRyougiOptionalDrawOne = "continuation:anomalyA008RewardRyougiOptionalDrawOne";
    public const string ContinuationKeyA009ConditionOpponentOptionalBenefit = "continuation:anomalyA009ConditionOpponentOptionalBenefit";
    public const string ContinuationKeyA009SelectGapTreasureToHand = "continuation:anomalyA009SelectGapTreasureToHand";
    public const string ContinuationKeyA002RewardOptionalBanishDecision = "continuation:anomalyA002RewardOptionalBanishDecision";
    public const string ContinuationKeyA002RewardSelectFirstBanishCard = "continuation:anomalyA002RewardSelectFirstBanishCard";
    public const string ContinuationKeyA002RewardSelectSecondBanishCard = "continuation:anomalyA002RewardSelectSecondBanishCard";
    public const string ContinuationKeyA002RewardOptionalSakuraReplacement = "continuation:anomalyA002RewardOptionalSakuraReplacement";

    private const string A001DefinitionId = "A001";
    private const string A002DefinitionId = "A002";
    private const string A003DefinitionId = "A003";
    private const string A005DefinitionId = "A005";
    private const string A006DefinitionId = "A006";
    private const string A007DefinitionId = "A007";
    private const string A008DefinitionId = "A008";
    private const string A009DefinitionId = "A009";

    private readonly ZoneMovementService zoneMovementService;
    private readonly AnomalyArrivalInputRuntime anomalyArrivalInputRuntime;
    private readonly AnomalyConditionInputRuntime anomalyConditionInputRuntime;
    private readonly AnomalyRewardInputRuntime anomalyRewardInputRuntime;

    public AnomalyProcessor(ZoneMovementService zoneMovementService, Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        anomalyArrivalInputRuntime = new AnomalyArrivalInputRuntime(zoneMovementService, nextInputContextIdSupplier);
        anomalyConditionInputRuntime = new AnomalyConditionInputRuntime(zoneMovementService, nextInputContextIdSupplier);
        anomalyRewardInputRuntime = new AnomalyRewardInputRuntime(zoneMovementService, nextInputContextIdSupplier);
    }

    public List<GameEvent> processTryResolveAnomalyActionRequest(
        RuleCore.GameState.GameState gameState,
        TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
    {
        if (gameState.matchState != MatchState.running)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.turnState to be initialized.");
        }

        if (tryResolveAnomalyActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.currentAnomalyState is null)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.currentAnomalyState to be initialized.");
        }

        if (string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires gameState.currentAnomalyState.currentAnomalyDefinitionId to be initialized.");
        }

        if (gameState.turnState.hasResolvedAnomalyThisTurn)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest can only be accepted once per turn.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);

        var conditionValidationResult = AnomalyResolveConditionRuntime.evaluateCondition(
            gameState,
            tryResolveAnomalyActionRequest.actorPlayerId,
            tryResolveAnomalyActionRequest.targetPlayerId,
            currentAnomalyDefinition);

        var rewardContextValidationResult = AnomalyValidationResult.passed();
        if (conditionValidationResult.isPassed)
        {
            rewardContextValidationResult = AnomalyResolveRewardRuntime.evaluateRewardContext(
                gameState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                tryResolveAnomalyActionRequest.targetPlayerId,
                currentAnomalyDefinition);
        }

        var isResolveSucceeded = conditionValidationResult.isPassed && rewardContextValidationResult.isPassed;
        var failedReasonKey = conditionValidationResult.isPassed
            ? rewardContextValidationResult.failedReasonKey
            : conditionValidationResult.failedReasonKey;
        List<PlayerId>? a002ConditionFriendlyPlayerIds = null;
        List<PlayerId>? a005ConditionFriendlyPlayerIds = null;
        List<PlayerId>? a008ConditionOpponentPlayerIds = null;
        List<PlayerId>? a009ConditionOpponentPlayerIds = null;

        if (isResolveSucceeded &&
            string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            if (!anomalyConditionInputRuntime.tryPrepareA002FriendlyTeamPlayerIdsForConditionInput(
                    gameState,
                    tryResolveAnomalyActionRequest.actorPlayerId,
                    out a002ConditionFriendlyPlayerIds,
                    out var a002FailedReasonKey))
            {
                isResolveSucceeded = false;
                failedReasonKey = a002FailedReasonKey;
                a002ConditionFriendlyPlayerIds = null;
            }
            else if (a002ConditionFriendlyPlayerIds.Count > 0)
            {
                isResolveSucceeded = false;
                failedReasonKey = AnomalyValidationFailureKeys.AnomalyConditionInputRequired;
            }
        }

        if (isResolveSucceeded &&
            string.Equals(currentAnomalyDefinition.definitionId, A005DefinitionId, StringComparison.Ordinal))
        {
            if (!anomalyConditionInputRuntime.tryPrepareA005FriendlyTeamPlayerIdsForConditionInput(
                    gameState,
                    tryResolveAnomalyActionRequest.actorPlayerId,
                    out a005ConditionFriendlyPlayerIds,
                    out var a005FailedReasonKey))
            {
                isResolveSucceeded = false;
                failedReasonKey = a005FailedReasonKey;
                a005ConditionFriendlyPlayerIds = null;
            }
            else
            {
                isResolveSucceeded = false;
                failedReasonKey = AnomalyValidationFailureKeys.AnomalyConditionInputRequired;
            }
        }

        if (isResolveSucceeded &&
            string.Equals(currentAnomalyDefinition.definitionId, A008DefinitionId, StringComparison.Ordinal))
        {
            if (!anomalyConditionInputRuntime.tryPrepareA008OpponentPlayerIdsForConditionInput(
                    gameState,
                    tryResolveAnomalyActionRequest.actorPlayerId,
                    out a008ConditionOpponentPlayerIds,
                    out var a008FailedReasonKey))
            {
                isResolveSucceeded = false;
                failedReasonKey = a008FailedReasonKey;
                a008ConditionOpponentPlayerIds = null;
            }
            else if (a008ConditionOpponentPlayerIds.Count > 0)
            {
                isResolveSucceeded = false;
                failedReasonKey = AnomalyValidationFailureKeys.AnomalyConditionInputRequired;
            }
        }

        if (isResolveSucceeded &&
            string.Equals(currentAnomalyDefinition.definitionId, A009DefinitionId, StringComparison.Ordinal))
        {
            if (!anomalyConditionInputRuntime.tryPrepareA009OpponentPlayerIdsForConditionInput(
                    gameState,
                    tryResolveAnomalyActionRequest.actorPlayerId,
                    out a009ConditionOpponentPlayerIds,
                    out var a009FailedReasonKey))
            {
                isResolveSucceeded = false;
                failedReasonKey = a009FailedReasonKey;
                a009ConditionOpponentPlayerIds = null;
            }
            else if (a009ConditionOpponentPlayerIds.Count > 0)
            {
                isResolveSucceeded = false;
                failedReasonKey = AnomalyValidationFailureKeys.AnomalyConditionInputRequired;
            }
        }

        var actionChainState = createAnomalyActionChain(
            gameState,
            tryResolveAnomalyActionRequest,
            "tryResolveAnomaly",
            tryResolveAnomalyActionRequest.actorPlayerId,
            "anomaly:tryResolve");

        actionChainState.producedEvents.Add(new AnomalyResolveAttemptedEvent
        {
            eventId = tryResolveAnomalyActionRequest.requestId,
            eventTypeKey = "anomalyResolveAttempted",
            sourceActionChainId = actionChainState.actionChainId,
            anomalyDefinitionId = currentAnomalyDefinition.definitionId,
            isSucceeded = isResolveSucceeded,
            failedReasonKey = failedReasonKey,
        });

        if (a002ConditionFriendlyPlayerIds is not null &&
            a002ConditionFriendlyPlayerIds.Count > 0)
        {
            anomalyConditionInputRuntime.openA002ConditionInputContext(
                gameState,
                actionChainState,
                a002ConditionFriendlyPlayerIds[0],
                tryResolveAnomalyActionRequest.requestId);
            return actionChainState.producedEvents;
        }

        if (a005ConditionFriendlyPlayerIds is not null)
        {
            anomalyConditionInputRuntime.openA005ConditionInputContext(
                gameState,
                actionChainState,
                a005ConditionFriendlyPlayerIds[0],
                tryResolveAnomalyActionRequest.requestId);
            return actionChainState.producedEvents;
        }

        if (a008ConditionOpponentPlayerIds is not null &&
            a008ConditionOpponentPlayerIds.Count > 0)
        {
            anomalyConditionInputRuntime.openA008ConditionInputContext(
                gameState,
                actionChainState,
                a008ConditionOpponentPlayerIds[0],
                tryResolveAnomalyActionRequest.requestId);
            return actionChainState.producedEvents;
        }

        if (a009ConditionOpponentPlayerIds is not null &&
            a009ConditionOpponentPlayerIds.Count > 0)
        {
            anomalyConditionInputRuntime.openA009ConditionInputContext(
                gameState,
                actionChainState,
                a009ConditionOpponentPlayerIds[0],
                tryResolveAnomalyActionRequest.requestId);
            return actionChainState.producedEvents;
        }

        if (!isResolveSucceeded)
        {
            actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
            actionChainState.isCompleted = true;
            return actionChainState.producedEvents;
        }

        var hasAppliedResolveCostsAndRewards = false;
        if (string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            applyResolveCostsAndRewardsOrThrow(
                gameState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                targetPlayerId: null,
                currentAnomalyDefinition);
            hasAppliedResolveCostsAndRewards = true;

            var isSuspendedByRewardInput = tryOpenA002RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                tryResolveAnomalyActionRequest.requestId);
            if (isSuspendedByRewardInput)
            {
                return actionChainState.producedEvents;
            }
        }

        if (string.Equals(currentAnomalyDefinition.definitionId, A008DefinitionId, StringComparison.Ordinal))
        {
            applyResolveCostsAndRewardsOrThrow(
                gameState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                tryResolveAnomalyActionRequest.targetPlayerId,
                currentAnomalyDefinition);
            hasAppliedResolveCostsAndRewards = true;

            var isSuspendedByRewardInput = tryOpenA008RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                tryResolveAnomalyActionRequest.requestId);
            if (isSuspendedByRewardInput)
            {
                return actionChainState.producedEvents;
            }
        }

        if (!hasAppliedResolveCostsAndRewards)
        {
            deductResolveCostsIfNeeded(
                gameState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                currentAnomalyDefinition);

            AnomalyResolveRewardRuntime.applyRewardOrThrow(
                gameState,
                tryResolveAnomalyActionRequest.actorPlayerId,
                tryResolveAnomalyActionRequest.targetPlayerId,
                currentAnomalyDefinition);
        }

        actionChainState.producedEvents.Add(new AnomalyResolvedEvent
        {
            eventId = tryResolveAnomalyActionRequest.requestId,
            eventTypeKey = "anomalyResolved",
            sourceActionChainId = actionChainState.actionChainId,
            anomalyDefinitionId = currentAnomalyDefinition.definitionId,
        });

        gameState.turnState.hasResolvedAnomalyThisTurn = true;
        var isSuspendedByArrivalInput = flipNextAnomaly(gameState, actionChainState, tryResolveAnomalyActionRequest.requestId);
        if (isSuspendedByArrivalInput)
        {
            return actionChainState.producedEvents;
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    public static bool isAnomalyContinuationKey(string? pendingContinuationKey)
    {
        return AnomalyContinuationDispatcher.isAnomalyContinuationKey(pendingContinuationKey);
    }

    public static bool shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation(string? pendingContinuationKey)
    {
        return AnomalyContinuationDispatcher.tryResolve(pendingContinuationKey, out var continuationKind) &&
               continuationKind == AnomalyContinuationKind.a005ConditionDefenseLikePlace;
    }

    public static void ensureValidAnomalyContinuationChoiceRequestShape(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? pendingContinuationKey)
    {
        var continuationKind = AnomalyContinuationDispatcher.resolveOrThrow(pendingContinuationKey);
        switch (continuationKind)
        {
            case AnomalyContinuationKind.a002ConditionFriendlyDiscardFromHand:
                if (!isValidA002ConditionFriendlyDiscardChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA002ConditionFriendlyDiscardFromHand.");
                }

                return;
            case AnomalyContinuationKind.a002RewardOptionalBanishDecision:
                if (!isValidA002RewardChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA002RewardOptionalBanishDecision.");
                }

                return;
            case AnomalyContinuationKind.a002RewardSelectFirstBanishCard:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a002RewardSelectSecondBanishCard:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a002RewardOptionalSakuraReplacement:
                if (!isValidA002RewardChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA002RewardOptionalSakuraReplacement.");
                }

                return;
            case AnomalyContinuationKind.a005ConditionDefenseLikePlace:
                if (!isValidA005ConditionDefenseLikePlaceChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKeys to contain exactly two unique values from currentInputContext.choiceKeys for continuation:anomalyA005ConditionDefenseLikePlace.");
                }

                return;
            case AnomalyContinuationKind.a005SelectSummonCardToHand:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a005ArrivalDirectSummonFromSummonZone:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a003ArrivalSelectOpponentShackle:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a007ArrivalOptionalBanishFlow:
                if (!isValidA007ArrivalOptionalBanishChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA007ArrivalOptionalBanishFlow.");
                }

                return;
            case AnomalyContinuationKind.a001ArrivalHumanDiscardFlow:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a006ArrivalHumanDefenseDiscardFlow:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a008ConditionOpponentOptionalDiscardReturn:
                if (!isValidA008ConditionOpponentOptionalDiscardReturnChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA008ConditionOpponentOptionalDiscardReturn.");
                }

                return;
            case AnomalyContinuationKind.a009ConditionOpponentOptionalBenefit:
                if (!isValidA009ConditionOpponentOptionalBenefitChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA009ConditionOpponentOptionalBenefit.");
                }

                return;
            case AnomalyContinuationKind.a009SelectGapTreasureToHand:
                if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
                }

                return;
            case AnomalyContinuationKind.a008RewardRyougiOptionalDrawOne:
                if (!isValidA008RewardRyougiOptionalDrawChoiceRequest(inputContextState, submitInputChoiceActionRequest))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA008RewardRyougiOptionalDrawOne.");
                }

                return;
            default:
                throw new InvalidOperationException("SubmitInputChoiceActionRequest pendingContinuationKey is not a supported anomaly continuation.");
        }
    }

    public bool ensureValidAnomalyContinuationBeforeClosingInputContext(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? pendingContinuationKey)
    {
        ensureValidAnomalyContinuationChoiceRequestShape(
            inputContextState,
            submitInputChoiceActionRequest,
            pendingContinuationKey);
        ensureValidAnomalyContinuationForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest,
            pendingContinuationKey);
        return shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation(pendingContinuationKey);
    }

    public void ensureValidAnomalyContinuationForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? pendingContinuationKey)
    {
        var continuationKind = AnomalyContinuationDispatcher.resolveOrThrow(pendingContinuationKey);
        switch (continuationKind)
        {
            case AnomalyContinuationKind.a002ConditionFriendlyDiscardFromHand:
                ensureValidA002ConditionFriendlyDiscardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardOptionalBanishDecision:
                ensureValidA002RewardOptionalBanishDecisionChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardSelectFirstBanishCard:
                ensureValidA002RewardSelectBanishCardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardSelectSecondBanishCard:
                ensureValidA002RewardSelectBanishCardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardOptionalSakuraReplacement:
                ensureValidA002RewardOptionalSakuraReplacementChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005ConditionDefenseLikePlace:
                ensureValidA005ConditionDefenseLikeChoicesForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005SelectSummonCardToHand:
                ensureValidA005SummonCardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005ArrivalDirectSummonFromSummonZone:
                ensureValidA005ArrivalDirectSummonChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a003ArrivalSelectOpponentShackle:
                ensureValidA003ArrivalSelectOpponentShackleChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a007ArrivalOptionalBanishFlow:
                ensureValidA007ArrivalOptionalBanishChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a001ArrivalHumanDiscardFlow:
                ensureValidA001ArrivalHumanDiscardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a006ArrivalHumanDefenseDiscardFlow:
                ensureValidA006ArrivalHumanDefenseDiscardChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a008ConditionOpponentOptionalDiscardReturn:
                ensureValidA008ConditionOpponentOptionalDiscardReturnChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a009ConditionOpponentOptionalBenefit:
                ensureValidA009ConditionOpponentOptionalBenefitChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a009SelectGapTreasureToHand:
                ensureValidA009GapTreasureChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a008RewardRyougiOptionalDrawOne:
                ensureValidA008RewardRyougiOptionalDrawChoiceForContinuation(
                    gameState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            default:
                throw new InvalidOperationException("SubmitInputChoiceActionRequest pendingContinuationKey is not a supported anomaly continuation.");
        }
    }

    public void continueAnomalyContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? pendingContinuationKey)
    {
        var continuationKind = AnomalyContinuationDispatcher.resolveOrThrow(pendingContinuationKey);
        switch (continuationKind)
        {
            case AnomalyContinuationKind.a002ConditionFriendlyDiscardFromHand:
                continueA002ConditionFriendlyDiscardContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardOptionalBanishDecision:
                continueA002RewardOptionalBanishDecisionContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardSelectFirstBanishCard:
                continueA002RewardSelectFirstBanishCardContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardSelectSecondBanishCard:
                continueA002RewardSelectSecondBanishCardContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a002RewardOptionalSakuraReplacement:
                continueA002RewardOptionalSakuraReplacementContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005ConditionDefenseLikePlace:
                continueA005ConditionDefenseLikePlaceContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005SelectSummonCardToHand:
                continueA005SelectSummonCardToHandContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a005ArrivalDirectSummonFromSummonZone:
                continueA005ArrivalDirectSummonFromSummonZoneContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a003ArrivalSelectOpponentShackle:
                continueA003ArrivalSelectOpponentShackleContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a007ArrivalOptionalBanishFlow:
                continueA007ArrivalOptionalBanishFlowContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a001ArrivalHumanDiscardFlow:
                continueA001ArrivalHumanDiscardFlowContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a006ArrivalHumanDefenseDiscardFlow:
                continueA006ArrivalHumanDefenseDiscardFlowContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a008ConditionOpponentOptionalDiscardReturn:
                continueA008ConditionOpponentOptionalDiscardReturnContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a009ConditionOpponentOptionalBenefit:
                continueA009ConditionOpponentOptionalBenefitContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a009SelectGapTreasureToHand:
                continueA009SelectGapTreasureToHandContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            case AnomalyContinuationKind.a008RewardRyougiOptionalDrawOne:
                continueA008RewardRyougiOptionalDrawOneContinuation(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest);
                return;
            default:
                throw new InvalidOperationException("SubmitInputChoiceActionRequest pendingContinuationKey is not a supported anomaly continuation.");
        }
    }

    public static bool isValidA005ConditionDefenseLikePlaceChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyConditionInputRuntime.isValidA005ConditionDefenseLikePlaceChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA002ConditionFriendlyDiscardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyConditionInputRuntime.isValidA002ConditionFriendlyDiscardChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA002RewardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyRewardInputRuntime.isValidA002RewardChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA007ArrivalOptionalBanishChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyArrivalInputRuntime.isValidA007ArrivalOptionalBanishChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA008ConditionOpponentOptionalDiscardReturnChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyConditionInputRuntime.isValidA008ConditionOpponentOptionalDiscardReturnChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA009ConditionOpponentOptionalBenefitChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyConditionInputRuntime.isValidA009ConditionOpponentOptionalBenefitChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public static bool isValidA008RewardRyougiOptionalDrawChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return AnomalyRewardInputRuntime.isValidA008RewardRyougiOptionalDrawChoiceRequest(
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA005ConditionDefenseLikeChoicesForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyConditionInputRuntime.ensureValidA005ConditionDefenseLikeChoicesForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA002ConditionFriendlyDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyConditionInputRuntime.ensureValidA002ConditionFriendlyDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA002RewardOptionalBanishDecisionChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA002RewardOptionalBanishDecisionChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA002RewardSelectBanishCardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA002RewardSelectBanishCardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA002RewardOptionalSakuraReplacementChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA002RewardOptionalSakuraReplacementChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA008ConditionOpponentOptionalDiscardReturnChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyConditionInputRuntime.ensureValidA008ConditionOpponentOptionalDiscardReturnChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA009ConditionOpponentOptionalBenefitChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyConditionInputRuntime.ensureValidA009ConditionOpponentOptionalBenefitChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA009GapTreasureChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA009GapTreasureChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA008RewardRyougiOptionalDrawChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA008RewardRyougiOptionalDrawChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void continueA005ConditionDefenseLikePlaceContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var conditionInputProgressResult = anomalyConditionInputRuntime.continueA005ConditionDefenseLikePlaceContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!conditionInputProgressResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A005DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires current anomaly to remain A005.");
        }

        if (!actionChainState.actorPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly condition continuation requires actionChain.actorPlayerId.");
        }

        var actorPlayerId = actionChainState.actorPlayerId.Value;
        AnomalyResolveOrchestrator.executePostConditionFlow(
            () => applyResolveCostsAndRewardsOrThrow(
                gameState,
                actorPlayerId,
                targetPlayerId: null,
                currentAnomalyDefinition),
            () => tryOpenA005RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                actorPlayerId,
                submitInputChoiceActionRequest.requestId),
            () => AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                currentAnomalyDefinition,
                () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
                "A005 anomaly continuation requires gameState.turnState."));
    }

    public void continueA002ConditionFriendlyDiscardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var conditionInputProgressResult = anomalyConditionInputRuntime.continueA002ConditionFriendlyDiscardContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!conditionInputProgressResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires current anomaly to remain A002.");
        }

        if (!actionChainState.actorPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly condition continuation requires actionChain.actorPlayerId.");
        }

        var actorPlayerId = actionChainState.actorPlayerId.Value;
        AnomalyResolveOrchestrator.executePostConditionFlow(
            () => applyResolveCostsAndRewardsOrThrow(
                gameState,
                actorPlayerId,
                targetPlayerId: null,
                currentAnomalyDefinition),
            () => tryOpenA002RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                actorPlayerId,
                submitInputChoiceActionRequest.requestId),
            () => AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                currentAnomalyDefinition,
                () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
                "A002 anomaly continuation requires gameState.turnState."));
    }

    public void continueA002RewardOptionalBanishDecisionContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA002RewardOptionalBanishDecisionChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires current anomaly to remain A002.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        if (AnomalyRewardInputRuntime.isA002RewardOptionalBanishDecisionDeclineChoice(submitInputChoiceActionRequest.choiceKey))
        {
            var appendAttemptedSuccessEvent = !hasSuccessfulAttemptedEvent(
                actionChainState,
                currentAnomalyDefinition.definitionId);
            AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                currentAnomalyDefinition,
                () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
                "A002 anomaly continuation requires gameState.turnState.",
                appendAttemptedSuccessEvent);
            return;
        }

        var stage = AnomalyRewardInputRuntime.isA002RewardOptionalBanishDecisionBanishTwoChoice(submitInputChoiceActionRequest.choiceKey)
            ? 2
            : 1;
        anomalyRewardInputRuntime.openA002RewardSelectBanishCardInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA002RewardSelectFirstBanishCard,
            submitInputChoiceActionRequest.requestId,
            stage);
    }

    public void continueA002RewardSelectFirstBanishCardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA002RewardSelectBanishCardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires current anomaly to remain A002.");
        }

        anomalyRewardInputRuntime.executeA002SelectedCardToGap(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest);

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        if (AnomalyRewardInputRuntime.shouldA002RewardContinueToSecondBanishSelection(inputContextState.inputTypeKey))
        {
            anomalyRewardInputRuntime.openA002RewardSelectBanishCardInputContext(
                gameState,
                actionChainState,
                actorPlayerId,
                ContinuationKeyA002RewardSelectSecondBanishCard,
                submitInputChoiceActionRequest.requestId,
                stage: 3);
            return;
        }

        anomalyRewardInputRuntime.openA002RewardOptionalSakuraReplacementInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA002RewardOptionalSakuraReplacement,
            submitInputChoiceActionRequest.requestId,
            banishedCount: 1);
    }

    public void continueA002RewardSelectSecondBanishCardContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA002RewardSelectBanishCardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires current anomaly to remain A002.");
        }

        anomalyRewardInputRuntime.executeA002SelectedCardToGap(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest);

        anomalyRewardInputRuntime.openA002RewardOptionalSakuraReplacementInputContext(
            gameState,
            actionChainState,
            inputContextState.requiredPlayerId.Value,
            ContinuationKeyA002RewardOptionalSakuraReplacement,
            submitInputChoiceActionRequest.requestId,
            banishedCount: 2);
    }

    public void continueA002RewardOptionalSakuraReplacementContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA002RewardOptionalSakuraReplacementChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A002DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires current anomaly to remain A002.");
        }

        var replacementSourceCount = AnomalyRewardInputRuntime.resolveA002RewardBanishCountFromSakuraReplacementInputType(
            inputContextState.inputTypeKey);
        anomalyRewardInputRuntime.executeA002OptionalSakuraReplacementByChoice(
            gameState,
            actionChainState,
            inputContextState.requiredPlayerId.Value,
            submitInputChoiceActionRequest,
            replacementSourceCount);

        var appendAttemptedSuccessEvent = !hasSuccessfulAttemptedEvent(
            actionChainState,
            currentAnomalyDefinition.definitionId);
        AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            currentAnomalyDefinition,
            () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
            "A002 anomaly continuation requires gameState.turnState.",
            appendAttemptedSuccessEvent);
    }

    public void continueA008ConditionOpponentOptionalDiscardReturnContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var conditionInputProgressResult = anomalyConditionInputRuntime.continueA008ConditionOpponentOptionalDiscardReturnContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!conditionInputProgressResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A008DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires current anomaly to remain A008.");
        }

        if (!actionChainState.actorPlayerId.HasValue)
        {
            throw new InvalidOperationException("A008 anomaly condition continuation requires actionChain.actorPlayerId.");
        }

        var targetPlayerId = tryResolveTargetPlayerIdFromTryResolveAnomalyRootActionRequest(actionChainState);
        var actorPlayerId = actionChainState.actorPlayerId.Value;
        AnomalyResolveOrchestrator.executePostConditionFlow(
            () => applyResolveCostsAndRewardsOrThrow(
                gameState,
                actorPlayerId,
                targetPlayerId,
                currentAnomalyDefinition),
            () => tryOpenA008RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                actorPlayerId,
                submitInputChoiceActionRequest.requestId),
            () => AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                currentAnomalyDefinition,
                () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
                "A008 anomaly continuation requires gameState.turnState."));
    }

    public void continueA009ConditionOpponentOptionalBenefitContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var conditionInputProgressResult = anomalyConditionInputRuntime.continueA009ConditionOpponentOptionalBenefitContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!conditionInputProgressResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A009DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires current anomaly to remain A009.");
        }

        if (!actionChainState.actorPlayerId.HasValue)
        {
            throw new InvalidOperationException("A009 anomaly condition continuation requires actionChain.actorPlayerId.");
        }

        var actorPlayerId = actionChainState.actorPlayerId.Value;
        AnomalyResolveOrchestrator.executePostConditionFlow(
            () => applyResolveCostsAndRewardsOrThrow(
                gameState,
                actorPlayerId,
                targetPlayerId: null,
                currentAnomalyDefinition),
            () => tryOpenA009RewardInputAfterCostsAndReward(
                gameState,
                actionChainState,
                actorPlayerId,
                submitInputChoiceActionRequest.requestId),
            () => AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                currentAnomalyDefinition,
                () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
                "A009 anomaly continuation requires gameState.turnState."));
    }

    public void ensureValidA005ArrivalDirectSummonChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyArrivalInputRuntime.ensureValidA005ArrivalDirectSummonChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA003ArrivalSelectOpponentShackleChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyArrivalInputRuntime.ensureValidA003ArrivalSelectOpponentShackleChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA007ArrivalOptionalBanishChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyArrivalInputRuntime.ensureValidA007ArrivalOptionalBanishChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA001ArrivalHumanDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyArrivalInputRuntime.ensureValidA001ArrivalHumanDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void ensureValidA006ArrivalHumanDefenseDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyArrivalInputRuntime.ensureValidA006ArrivalHumanDefenseDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void continueA003ArrivalSelectOpponentShackleContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA003ArrivalSelectOpponentShackleChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A003DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires current anomaly to remain A003.");
        }

        anomalyArrivalInputRuntime.executeA003ArrivalApplyShackleToSelectedOpponent(
            gameState,
            actionChainState,
            inputContextState.requiredPlayerId.Value,
            submitInputChoiceActionRequest);

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public void continueA007ArrivalOptionalBanishFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var advanceResult = anomalyArrivalInputRuntime.continueA007ArrivalOptionalBanishFlowContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest,
            ContinuationKeyA007ArrivalOptionalBanishFlow);

        if (!advanceResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A007DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires current anomaly to remain A007.");
        }

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public void continueA001ArrivalHumanDiscardFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var advanceResult = anomalyArrivalInputRuntime.continueA001ArrivalHumanDiscardFlowContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest,
            ContinuationKeyA001ArrivalHumanDiscardFlow);

        if (!advanceResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A001DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires current anomaly to remain A001.");
        }

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public void continueA006ArrivalHumanDefenseDiscardFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var advanceResult = anomalyArrivalInputRuntime.continueA006ArrivalHumanDefenseDiscardFlowContinuation(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest,
            ContinuationKeyA006ArrivalHumanDefenseDiscardFlow);

        if (!advanceResult.isCompleted)
        {
            return;
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A006DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires current anomaly to remain A006.");
        }

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public void continueA005ArrivalDirectSummonFromSummonZoneContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA005ArrivalDirectSummonChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A005DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires current anomaly to remain A005.");
        }

        anomalyArrivalInputRuntime.executeA005ArrivalSelectedSummonCardToActorDiscardAndRefill(
            gameState,
            actionChainState,
            inputContextState.requiredPlayerId.Value,
            submitInputChoiceActionRequest);

        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public void ensureValidA005SummonCardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        anomalyRewardInputRuntime.ensureValidA005SummonCardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);
    }

    public void continueA005SelectSummonCardToHandContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA005SummonCardChoiceForContinuation(gameState, inputContextState, submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A005DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires current anomaly to remain A005.");
        }

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        anomalyRewardInputRuntime.executeA005SelectedSummonCardToActorHand(
            gameState,
            actionChainState,
            actorPlayerId,
            submitInputChoiceActionRequest);

        AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            currentAnomalyDefinition,
            () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
            "A005 anomaly continuation requires gameState.turnState.");
    }

    public void continueA009SelectGapTreasureToHandContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA009GapTreasureChoiceForContinuation(gameState, inputContextState, submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A009DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires current anomaly to remain A009.");
        }

        var actorPlayerId = inputContextState.requiredPlayerId.Value;
        anomalyRewardInputRuntime.executeA009SelectedGapTreasureToActorHand(
            gameState,
            actionChainState,
            actorPlayerId,
            submitInputChoiceActionRequest);

        AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            currentAnomalyDefinition,
            () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
            "A009 anomaly continuation requires gameState.turnState.");
    }

    public void continueA008RewardRyougiOptionalDrawOneContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        ensureValidA008RewardRyougiOptionalDrawChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires gameState.currentAnomalyState.currentAnomalyDefinitionId.");
        }

        var currentAnomalyDefinitionId = gameState.currentAnomalyState.currentAnomalyDefinitionId!;
        var currentAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(currentAnomalyDefinitionId);
        if (!string.Equals(currentAnomalyDefinition.definitionId, A008DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires current anomaly to remain A008.");
        }

        anomalyRewardInputRuntime.executeA008RewardRyougiOptionalDrawOneByChoice(
            gameState,
            actionChainState,
            inputContextState.requiredPlayerId.Value,
            submitInputChoiceActionRequest);

        var appendAttemptedSuccessEvent = !hasSuccessfulAttemptedEvent(
            actionChainState,
            currentAnomalyDefinition.definitionId);
        AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            currentAnomalyDefinition,
            () => flipNextAnomaly(gameState, actionChainState, submitInputChoiceActionRequest.requestId),
            "A008 anomaly continuation requires gameState.turnState.",
            appendAttemptedSuccessEvent);
    }

    private static PlayerId? tryResolveTargetPlayerIdFromTryResolveAnomalyRootActionRequest(ActionChainState actionChainState)
    {
        if (actionChainState.rootActionRequest is not TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
        {
            throw new InvalidOperationException("Anomaly continuation requires actionChain.rootActionRequest to be TryResolveAnomalyActionRequest.");
        }

        return tryResolveAnomalyActionRequest.targetPlayerId;
    }

    private void applyResolveCostsAndRewardsOrThrow(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        deductResolveCostsIfNeeded(gameState, actorPlayerId, anomalyDefinition);
        AnomalyResolveRewardRuntime.applyRewardOrThrow(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
    }

    private bool tryOpenA005RewardInputAfterCostsAndReward(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long requestId)
    {
        var rewardChoiceKeys = anomalyRewardInputRuntime.createA005RewardSelectSummonCardChoiceKeys(gameState);
        if (rewardChoiceKeys.Count == 0)
        {
            return false;
        }

        anomalyRewardInputRuntime.openA005RewardSelectSummonCardInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA005SelectSummonCardToHand,
            requestId,
            rewardChoiceKeys);
        return true;
    }

    private bool tryOpenA002RewardInputAfterCostsAndReward(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long requestId)
    {
        var rewardDecisionChoiceKeys = anomalyRewardInputRuntime.createA002RewardOptionalBanishDecisionChoiceKeys(
            gameState,
            actorPlayerId);
        if (rewardDecisionChoiceKeys.Count == 0)
        {
            return false;
        }

        anomalyRewardInputRuntime.openA002RewardOptionalBanishDecisionInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA002RewardOptionalBanishDecision,
            requestId,
            rewardDecisionChoiceKeys);
        return true;
    }

    private bool tryOpenA008RewardInputAfterCostsAndReward(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long requestId)
    {
        return anomalyRewardInputRuntime.tryOpenA008RewardRyougiOptionalDrawInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA008RewardRyougiOptionalDrawOne,
            requestId);
    }

    private bool tryOpenA009RewardInputAfterCostsAndReward(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long requestId)
    {
        var rewardChoiceKeys = anomalyRewardInputRuntime.createA009RewardSelectGapTreasureChoiceKeys(gameState);
        if (rewardChoiceKeys.Count == 0)
        {
            return false;
        }

        anomalyRewardInputRuntime.openA009RewardSelectGapTreasureInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            ContinuationKeyA009SelectGapTreasureToHand,
            requestId,
            rewardChoiceKeys);
        return true;
    }

    private static void deductResolveCostsIfNeeded(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor player state for resolve cost payment.");
        }

        if (anomalyDefinition.resolveManaCost.HasValue)
        {
            if (actorPlayerState.mana < anomalyDefinition.resolveManaCost.Value)
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor mana to remain sufficient for resolve cost payment.");
            }

            actorPlayerState.mana -= anomalyDefinition.resolveManaCost.Value;
        }

        if (!anomalyDefinition.resolveFriendlyTeamHpCostPerPlayer.HasValue)
        {
            return;
        }

        var hpCostPerFriendlyPlayer = anomalyDefinition.resolveFriendlyTeamHpCostPerPlayer.Value;
        var friendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorPlayerState.teamId);
        if (friendlyTeamPlayerIds.Count == 0)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires at least one friendly team player for resolve hp cost payment.");
        }

        var payableCharacterInstanceIds = new List<CharacterInstanceId>(friendlyTeamPlayerIds.Count);
        foreach (var friendlyPlayerId in friendlyTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState))
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest requires friendly player state for resolve hp cost payment.");
            }

            if (!friendlyPlayerState.activeCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest requires active character for resolve hp cost payment.");
            }

            var activeCharacterInstanceId = friendlyPlayerState.activeCharacterInstanceId.Value;
            if (!gameState.characterInstances.TryGetValue(activeCharacterInstanceId, out var activeCharacterInstance))
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest requires active character for resolve hp cost payment.");
            }

            if (activeCharacterInstance.currentHp <= hpCostPerFriendlyPlayer)
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest requires friendly active character hp to remain above resolve hp cost payment.");
            }

            payableCharacterInstanceIds.Add(activeCharacterInstanceId);
        }

        foreach (var characterInstanceId in payableCharacterInstanceIds)
        {
            gameState.characterInstances[characterInstanceId].currentHp -= hpCostPerFriendlyPlayer;
        }
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

    private static bool hasSuccessfulAttemptedEvent(
        ActionChainState actionChainState,
        string anomalyDefinitionId)
    {
        foreach (var gameEvent in actionChainState.producedEvents)
        {
            if (gameEvent is not AnomalyResolveAttemptedEvent attemptedEvent)
            {
                continue;
            }

            if (!attemptedEvent.isSucceeded)
            {
                continue;
            }

            if (string.Equals(attemptedEvent.anomalyDefinitionId, anomalyDefinitionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool flipNextAnomaly(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId)
    {
        var currentAnomalyState = gameState.currentAnomalyState!;
        if (currentAnomalyState.anomalyDeckDefinitionIds.Count == 0)
        {
            currentAnomalyState.currentAnomalyDefinitionId = null;
            currentAnomalyState.localState.Clear();
            return false;
        }

        var nextAnomalyDefinitionId = currentAnomalyState.anomalyDeckDefinitionIds[0];
        currentAnomalyState.anomalyDeckDefinitionIds.RemoveAt(0);
        currentAnomalyState.currentAnomalyDefinitionId = nextAnomalyDefinitionId;
        currentAnomalyState.localState.Clear();

        actionChainState.producedEvents.Add(new AnomalyFlippedEvent
        {
            eventId = requestId,
            eventTypeKey = "anomalyFlipped",
            sourceActionChainId = actionChainState.actionChainId,
            anomalyDefinitionId = nextAnomalyDefinitionId,
        });

        var nextAnomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(nextAnomalyDefinitionId);
        return AnomalyArrivalRuntime.executeOnFlip(
            gameState,
            actionChainState,
            requestId,
            zoneMovementService,
            nextAnomalyDefinition,
            anomalyArrivalInputRuntime,
            ContinuationKeyA003ArrivalSelectOpponentShackle,
            ContinuationKeyA005ArrivalDirectSummonFromSummonZone,
            ContinuationKeyA007ArrivalOptionalBanishFlow,
            ContinuationKeyA001ArrivalHumanDiscardFlow,
            ContinuationKeyA006ArrivalHumanDefenseDiscardFlow);
    }

    private static ActionChainState createAnomalyActionChain(
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
