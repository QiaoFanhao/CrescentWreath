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

public sealed class AnomalyRewardInputRuntime
{
    private const string A002RewardOptionalBanishDecisionInputTypeKey = "anomalyA002RewardOptionalBanishDecision";
    private const string A002RewardSelectBanishCardSingleInputTypeKey = "anomalyA002RewardSelectBanishCardSingle";
    private const string A002RewardSelectBanishCardFirstOfTwoInputTypeKey = "anomalyA002RewardSelectBanishCardFirstOfTwo";
    private const string A002RewardSelectBanishCardSecondOfTwoInputTypeKey = "anomalyA002RewardSelectBanishCardSecondOfTwo";
    private const string A002RewardOptionalSakuraReplacementAfterOneInputTypeKey = "anomalyA002RewardOptionalSakuraReplacementAfterOne";
    private const string A002RewardOptionalSakuraReplacementAfterTwoInputTypeKey = "anomalyA002RewardOptionalSakuraReplacementAfterTwo";
    private const string A002RewardContextKey = "anomaly:A002:rewardOptionalBanishAndSakuraReplacement";
    private const string A002RewardDecisionChoiceKeyDecline = "decline";
    private const string A002RewardDecisionChoiceKeyBanishOne = "banish1";
    private const string A002RewardDecisionChoiceKeyBanishTwo = "banish2";
    private const string A002RewardSakuraReplacementChoiceKeyAccept = "accept";
    private const string A002RewardSakuraReplacementChoiceKeyDecline = "decline";
    private const string A002RewardBanishCardChoiceKeyPrefix = "banishCard:";
    private const string RyougiCharacterDefinitionId = "C009";
    private const string A008RewardRyougiOptionalDrawInputTypeKey = "anomalyA008RewardRyougiOptionalDrawOne";
    private const string A008RewardRyougiOptionalDrawContextKey = "anomaly:A008:rewardRyougiOptionalDrawOne";
    private const string A008RewardRyougiOptionalDrawChoiceKeyAccept = "accept";
    private const string A008RewardRyougiOptionalDrawChoiceKeyDecline = "decline";
    private const int A005RewardSummonSigilCostMax = 7;
    private const string A005RewardSelectSummonCardInputTypeKey = "anomalyA005SelectSummonCardToHand";
    private const string A005RewardSelectSummonCardContextKey = "anomaly:A005:selectSummonCardToHand";
    private const string A005RewardSelectSummonCardChoiceKeyPrefix = "summonCard:";
    private const int A009RewardGapSummonSigilCostMax = 7;
    private const string A009RewardSelectGapTreasureInputTypeKey = "anomalyA009SelectGapTreasureToHand";
    private const string A009RewardSelectGapTreasureContextKey = "anomaly:A009:selectGapTreasureToHand";
    private const string A009RewardSelectGapTreasureChoiceKeyPrefix = "gapCard:";

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyRewardInputRuntime(ZoneMovementService zoneMovementService, Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool tryOpenA008RewardRyougiOptionalDrawInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly reward input requires actorPlayerId to exist in gameState.players.");
        }

        var ryougiOwnerPlayerId = tryResolveFriendlyRyougiOwnerPlayerId(gameState, actorPlayerState.teamId);
        if (!ryougiOwnerPlayerId.HasValue)
        {
            return false;
        }

        openA008RewardRyougiOptionalDrawInputContext(
            gameState,
            actionChainState,
            ryougiOwnerPlayerId.Value,
            pendingContinuationKey,
            eventId);
        return true;
    }

    public static bool isValidA008RewardRyougiOptionalDrawChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey))
        {
            return false;
        }

        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public void ensureValidA008RewardRyougiOptionalDrawChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A008RewardRyougiOptionalDrawContextKey)
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires currentInputContext.contextKey to be anomaly:A008:rewardRyougiOptionalDrawOne.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (submitInputChoiceActionRequest.actorPlayerId != inputContextState.requiredPlayerId.Value)
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        }

        if (!isValidA008RewardRyougiOptionalDrawChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        if (!gameState.players.TryGetValue(inputContextState.requiredPlayerId.Value, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.deckZoneId))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's discardZoneId to exist in gameState.zones.");
        }
    }

    public void executeA008RewardRyougiOptionalDrawOneByChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId ryougiOwnerPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (submitInputChoiceActionRequest.choiceKey == A008RewardRyougiOptionalDrawChoiceKeyDecline)
        {
            return;
        }

        if (!gameState.players.TryGetValue(ryougiOwnerPlayerId, out var ryougiOwnerPlayerState))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(ryougiOwnerPlayerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(ryougiOwnerPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(ryougiOwnerPlayerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires required player's discardZoneId to exist in gameState.zones.");
        }

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardIdsInCurrentOrder = new List<CardInstanceId>(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
            {
                if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var discardedCardInstance))
                {
                    throw new InvalidOperationException("A008 anomaly reward continuation requires discard zone cardInstanceIds to exist in gameState.cardInstances.");
                }

                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    ryougiOwnerPlayerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    submitInputChoiceActionRequest.requestId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topCardInstanceId = deckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topCardInstanceId, out var topCardInstance))
        {
            throw new InvalidOperationException("A008 anomaly reward continuation requires deck zone cardInstanceIds to exist in gameState.cardInstances.");
        }

        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topCardInstance,
            ryougiOwnerPlayerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(drawEvent);
    }

    public List<string> createA005RewardSelectSummonCardChoiceKeys(RuleCore.GameState.GameState gameState)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A005 anomaly reward selection requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("A005 anomaly reward selection requires gameState.publicState.summonZoneId to exist in gameState.zones.");
        }

        var choiceKeys = new List<string>();
        foreach (var summonCardInstanceId in summonZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(summonCardInstanceId, out var summonCardInstance))
            {
                throw new InvalidOperationException("A005 anomaly reward selection requires summon zone cardInstanceIds to exist in gameState.cardInstances.");
            }

            if (!isA005RewardSummonCardEligible(summonCardInstance))
            {
                continue;
            }

            choiceKeys.Add(createA005RewardSelectSummonCardChoiceKey(summonCardInstanceId));
        }

        return choiceKeys;
    }

    public void openA005RewardSelectSummonCardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId,
        List<string> choiceKeys)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A005 anomaly reward input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A005RewardSelectSummonCardInputTypeKey,
            contextKey = A005RewardSelectSummonCardContextKey,
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

    public void ensureValidA005SummonCardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A005RewardSelectSummonCardContextKey)
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires currentInputContext.contextKey to be anomaly:A005:selectSummonCardToHand.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out _))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires gameState.publicState.summonZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA005RewardSelectSummonCardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires selected card to still be in summon zone.");
        }

        if (!isA005RewardSummonCardEligible(selectedCardInstance))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires selected card summon cost to be less than or equal to 7.");
        }
    }

    public void executeA005SelectedSummonCardToActorHand(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation requires actor player's handZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA005RewardSelectSummonCardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];

        var moveEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.handZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(moveEvent);
    }

    public List<string> createA009RewardSelectGapTreasureChoiceKeys(RuleCore.GameState.GameState gameState)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A009 anomaly reward selection requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.gapZoneId, out var gapZoneState))
        {
            throw new InvalidOperationException("A009 anomaly reward selection requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var choiceKeys = new List<string>();
        foreach (var gapCardInstanceId in gapZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(gapCardInstanceId, out var gapCardInstance))
            {
                throw new InvalidOperationException("A009 anomaly reward selection requires gap zone cardInstanceIds to exist in gameState.cardInstances.");
            }

            if (!isA009RewardGapTreasureEligible(gapCardInstance))
            {
                continue;
            }

            choiceKeys.Add(createA009RewardSelectGapTreasureChoiceKey(gapCardInstanceId));
        }

        return choiceKeys;
    }

    public void openA009RewardSelectGapTreasureInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId,
        List<string> choiceKeys)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A009 anomaly reward input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A009RewardSelectGapTreasureInputTypeKey,
            contextKey = A009RewardSelectGapTreasureContextKey,
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

    public void ensureValidA009GapTreasureChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A009RewardSelectGapTreasureContextKey)
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires currentInputContext.contextKey to be anomaly:A009:selectGapTreasureToHand.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.gapZoneId, out _))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA009RewardSelectGapTreasureChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.gapZoneId)
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires selected card to still be in gap zone.");
        }

        if (!isA009RewardGapTreasureEligible(selectedCardInstance))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires selected card summon cost to be less than or equal to 7.");
        }
    }

    public void executeA009SelectedGapTreasureToActorHand(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation requires actor player's handZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA009RewardSelectGapTreasureChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];

        var moveEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.handZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(moveEvent);
    }

    public List<string> createA002RewardOptionalBanishDecisionChoiceKeys(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var banishEligibleCardInstanceIds = collectA002RewardBanishEligibleCardInstanceIds(gameState, actorPlayerId);
        if (banishEligibleCardInstanceIds.Count == 0)
        {
            return new List<string>();
        }

        var choiceKeys = new List<string>
        {
            A002RewardDecisionChoiceKeyDecline,
            A002RewardDecisionChoiceKeyBanishOne,
        };
        if (banishEligibleCardInstanceIds.Count >= 2)
        {
            choiceKeys.Add(A002RewardDecisionChoiceKeyBanishTwo);
        }

        return choiceKeys;
    }

    public void openA002RewardOptionalBanishDecisionInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId,
        List<string> choiceKeys)
    {
        openA002RewardInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            A002RewardOptionalBanishDecisionInputTypeKey,
            pendingContinuationKey,
            eventId,
            choiceKeys);
    }

    public void openA002RewardSelectBanishCardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId,
        int stage)
    {
        var inputTypeKey = stage switch
        {
            1 => A002RewardSelectBanishCardSingleInputTypeKey,
            2 => A002RewardSelectBanishCardFirstOfTwoInputTypeKey,
            3 => A002RewardSelectBanishCardSecondOfTwoInputTypeKey,
            _ => throw new InvalidOperationException("A002 anomaly reward input stage must be 1(single), 2(first-of-two), or 3(second-of-two)."),
        };

        var banishEligibleCardInstanceIds = collectA002RewardBanishEligibleCardInstanceIds(gameState, actorPlayerId);
        if (banishEligibleCardInstanceIds.Count == 0)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires at least one banish-eligible card in actor hand or discard.");
        }

        openA002RewardInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            inputTypeKey,
            pendingContinuationKey,
            eventId,
            createA002RewardBanishCardChoiceKeys(banishEligibleCardInstanceIds));
    }

    public void openA002RewardOptionalSakuraReplacementInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId,
        int banishedCount)
    {
        if (banishedCount <= 0)
        {
            throw new InvalidOperationException("A002 anomaly reward optional sakura replacement input requires banishedCount to be greater than 0.");
        }

        var inputTypeKey = banishedCount switch
        {
            1 => A002RewardOptionalSakuraReplacementAfterOneInputTypeKey,
            2 => A002RewardOptionalSakuraReplacementAfterTwoInputTypeKey,
            _ => throw new InvalidOperationException("A002 anomaly reward optional sakura replacement input supports only banishedCount 1 or 2."),
        };

        openA002RewardInputContext(
            gameState,
            actionChainState,
            actorPlayerId,
            inputTypeKey,
            pendingContinuationKey,
            eventId,
            new List<string>
            {
                A002RewardSakuraReplacementChoiceKeyAccept,
                A002RewardSakuraReplacementChoiceKeyDecline,
            });
    }

    public static bool isValidA002RewardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey))
        {
            return false;
        }

        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public static bool shouldA002RewardContinueToSecondBanishSelection(string? inputTypeKey)
    {
        return string.Equals(
            inputTypeKey,
            A002RewardSelectBanishCardFirstOfTwoInputTypeKey,
            StringComparison.Ordinal);
    }

    public static int resolveA002RewardBanishCountFromSakuraReplacementInputType(string? inputTypeKey)
    {
        if (string.Equals(
                inputTypeKey,
                A002RewardOptionalSakuraReplacementAfterOneInputTypeKey,
                StringComparison.Ordinal))
        {
            return 1;
        }

        if (string.Equals(
                inputTypeKey,
                A002RewardOptionalSakuraReplacementAfterTwoInputTypeKey,
                StringComparison.Ordinal))
        {
            return 2;
        }

        throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.inputTypeKey to be anomalyA002RewardOptionalSakuraReplacementAfterOne or anomalyA002RewardOptionalSakuraReplacementAfterTwo.");
    }

    public static bool isA002RewardOptionalBanishDecisionDeclineChoice(string choiceKey)
    {
        return string.Equals(choiceKey, A002RewardDecisionChoiceKeyDecline, StringComparison.Ordinal);
    }

    public static bool isA002RewardOptionalBanishDecisionBanishOneChoice(string choiceKey)
    {
        return string.Equals(choiceKey, A002RewardDecisionChoiceKeyBanishOne, StringComparison.Ordinal);
    }

    public static bool isA002RewardOptionalBanishDecisionBanishTwoChoice(string choiceKey)
    {
        return string.Equals(choiceKey, A002RewardDecisionChoiceKeyBanishTwo, StringComparison.Ordinal);
    }

    public static bool isA002RewardOptionalSakuraReplacementDeclineChoice(string choiceKey)
    {
        return string.Equals(choiceKey, A002RewardSakuraReplacementChoiceKeyDecline, StringComparison.Ordinal);
    }

    public void ensureValidA002RewardOptionalBanishDecisionChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredPlayerId = ensureValidA002RewardContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest,
            A002RewardOptionalBanishDecisionInputTypeKey,
            "A002 anomaly reward continuation requires currentInputContext.inputTypeKey to be anomalyA002RewardOptionalBanishDecision.");

        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.handZoneId) ||
            !gameState.zones.ContainsKey(requiredPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires actor hand/discard zones to exist in gameState.zones.");
        }
    }

    public void ensureValidA002RewardSelectBanishCardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredPlayerId = ensureValidA002RewardContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest,
            expectedInputTypeKey: null,
            expectedInputTypeMessage: null);

        if (!isA002RewardSelectBanishInputType(inputContextState.inputTypeKey))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.inputTypeKey to be a supported A002 reward banish selection stage.");
        }

        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.handZoneId) ||
            !gameState.zones.ContainsKey(requiredPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires actor hand/discard zones to exist in gameState.zones.");
        }

        if (gameState.publicState is null ||
            !gameState.zones.ContainsKey(gameState.publicState.gapZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA002RewardBanishCardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires selected card to be owned by currentInputContext.requiredPlayerId.");
        }

        if (selectedCardInstance.zoneId != requiredPlayerState.handZoneId &&
            selectedCardInstance.zoneId != requiredPlayerState.discardZoneId)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires selected card to still be in actor hand or discard zone.");
        }
    }

    public void ensureValidA002RewardOptionalSakuraReplacementChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var requiredPlayerId = ensureValidA002RewardContinuationEnvironment(
            inputContextState,
            submitInputChoiceActionRequest,
            expectedInputTypeKey: null,
            expectedInputTypeMessage: null);

        if (!isA002RewardOptionalSakuraReplacementInputType(inputContextState.inputTypeKey))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.inputTypeKey to be anomalyA002RewardOptionalSakuraReplacementAfterOne or anomalyA002RewardOptionalSakuraReplacementAfterTwo.");
        }

        if (gameState.publicState is null ||
            !gameState.zones.ContainsKey(gameState.publicState.sakuraCakeDeckZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.publicState.sakuraCakeDeckZoneId to exist in gameState.zones.");
        }

        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires actor discardZoneId to exist in gameState.zones.");
        }
    }

    public void executeA002SelectedCardToGap(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.publicState.");
        }

        if (!gameState.zones.ContainsKey(gameState.publicState.gapZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA002RewardBanishCardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
        var moveEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            gameState.publicState.gapZoneId,
            CardMoveReason.banish,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(moveEvent);
    }

    public void executeA002OptionalSakuraReplacementByChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        int replacementSourceCount)
    {
        if (isA002RewardOptionalSakuraReplacementDeclineChoice(submitInputChoiceActionRequest.choiceKey))
        {
            return;
        }

        if (replacementSourceCount <= 0)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires replacement source count to be greater than 0 for accept choice.");
        }

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires actor discardZoneId to exist in gameState.zones.");
        }

        if (gameState.publicState is null ||
            !gameState.zones.TryGetValue(gameState.publicState.sakuraCakeDeckZoneId, out var sakuraCakeDeckZoneState))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires gameState.publicState.sakuraCakeDeckZoneId to exist in gameState.zones.");
        }

        var summonCount = Math.Min(replacementSourceCount, sakuraCakeDeckZoneState.cardInstanceIds.Count);
        for (var summonIndex = 0; summonIndex < summonCount; summonIndex++)
        {
            var topSakuraCakeCardInstanceId = sakuraCakeDeckZoneState.cardInstanceIds[0];
            if (!gameState.cardInstances.TryGetValue(topSakuraCakeCardInstanceId, out var topSakuraCakeCardInstance))
            {
                throw new InvalidOperationException("A002 anomaly reward continuation requires sakura cake deck cardInstanceIds to exist in gameState.cardInstances.");
            }

            var summonEvent = zoneMovementService.moveCard(
                gameState,
                topSakuraCakeCardInstance,
                actorPlayerState.discardZoneId,
                CardMoveReason.summon,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(summonEvent);
        }
    }

    private void openA008RewardRyougiOptionalDrawInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId ryougiOwnerPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A008 anomaly reward input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = ryougiOwnerPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A008RewardRyougiOptionalDrawInputTypeKey,
            contextKey = A008RewardRyougiOptionalDrawContextKey,
        };
        inputContextState.choiceKeys.Add(A008RewardRyougiOptionalDrawChoiceKeyAccept);
        inputContextState.choiceKeys.Add(A008RewardRyougiOptionalDrawChoiceKeyDecline);

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

    private void openA002RewardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string inputTypeKey,
        string pendingContinuationKey,
        long eventId,
        List<string> choiceKeys)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A002 anomaly reward input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = A002RewardContextKey,
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

    private static PlayerId ensureValidA002RewardContinuationEnvironment(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? expectedInputTypeKey,
        string? expectedInputTypeMessage)
    {
        if (!string.Equals(inputContextState.contextKey, A002RewardContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.contextKey to be anomaly:A002:rewardOptionalBanishAndSakuraReplacement.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires currentInputContext.requiredPlayerId.");
        }

        var requiredPlayerId = inputContextState.requiredPlayerId.Value;
        if (submitInputChoiceActionRequest.actorPlayerId != requiredPlayerId)
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        }

        if (!isValidA002RewardChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        if (!string.IsNullOrWhiteSpace(expectedInputTypeKey) &&
            !string.Equals(inputContextState.inputTypeKey, expectedInputTypeKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(expectedInputTypeMessage);
        }

        return requiredPlayerId;
    }

    private static List<CardInstanceId> collectA002RewardBanishEligibleCardInstanceIds(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A002 anomaly reward input requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.handZoneId, out var actorHandZoneState))
        {
            throw new InvalidOperationException("A002 anomaly reward input requires actor handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var actorDiscardZoneState))
        {
            throw new InvalidOperationException("A002 anomaly reward input requires actor discardZoneId to exist in gameState.zones.");
        }

        var banishEligibleCardInstanceIds = new List<CardInstanceId>(
            actorHandZoneState.cardInstanceIds.Count + actorDiscardZoneState.cardInstanceIds.Count);

        foreach (var handCardInstanceId in actorHandZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(handCardInstanceId))
            {
                throw new InvalidOperationException("A002 anomaly reward input requires actor hand zone cardInstanceIds to exist in gameState.cardInstances.");
            }

            banishEligibleCardInstanceIds.Add(handCardInstanceId);
        }

        foreach (var discardCardInstanceId in actorDiscardZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(discardCardInstanceId))
            {
                throw new InvalidOperationException("A002 anomaly reward input requires actor discard zone cardInstanceIds to exist in gameState.cardInstances.");
            }

            banishEligibleCardInstanceIds.Add(discardCardInstanceId);
        }

        return banishEligibleCardInstanceIds;
    }

    private static bool isA002RewardSelectBanishInputType(string? inputTypeKey)
    {
        return string.Equals(inputTypeKey, A002RewardSelectBanishCardSingleInputTypeKey, StringComparison.Ordinal) ||
               string.Equals(inputTypeKey, A002RewardSelectBanishCardFirstOfTwoInputTypeKey, StringComparison.Ordinal) ||
               string.Equals(inputTypeKey, A002RewardSelectBanishCardSecondOfTwoInputTypeKey, StringComparison.Ordinal);
    }

    private static bool isA002RewardOptionalSakuraReplacementInputType(string? inputTypeKey)
    {
        return string.Equals(inputTypeKey, A002RewardOptionalSakuraReplacementAfterOneInputTypeKey, StringComparison.Ordinal) ||
               string.Equals(inputTypeKey, A002RewardOptionalSakuraReplacementAfterTwoInputTypeKey, StringComparison.Ordinal);
    }

    private static List<string> createA002RewardBanishCardChoiceKeys(List<CardInstanceId> cardInstanceIds)
    {
        var choiceKeys = new List<string>(cardInstanceIds.Count);
        foreach (var cardInstanceId in cardInstanceIds)
        {
            choiceKeys.Add(createA002RewardBanishCardChoiceKey(cardInstanceId));
        }

        return choiceKeys;
    }

    private static string createA002RewardBanishCardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A002RewardBanishCardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static CardInstanceId parseA002RewardBanishCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A002RewardBanishCardChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation choiceKey must start with banishCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A002RewardBanishCardChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A002 anomaly reward continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static PlayerId? tryResolveFriendlyRyougiOwnerPlayerId(
        RuleCore.GameState.GameState gameState,
        TeamId actorTeamId)
    {
        IReadOnlyList<PlayerId> friendlyTeamPlayerIds;
        if (gameState.teams.TryGetValue(actorTeamId, out var actorTeamState) &&
            actorTeamState.memberPlayerIds.Count > 0)
        {
            friendlyTeamPlayerIds = actorTeamState.memberPlayerIds;
        }
        else
        {
            var inferredFriendlyPlayerIds = new List<PlayerId>();
            foreach (var playerStateEntry in gameState.players)
            {
                if (playerStateEntry.Value.teamId == actorTeamId)
                {
                    inferredFriendlyPlayerIds.Add(playerStateEntry.Key);
                }
            }

            friendlyTeamPlayerIds = inferredFriendlyPlayerIds;
        }

        foreach (var friendlyPlayerId in friendlyTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState))
            {
                throw new InvalidOperationException("A008 anomaly reward input requires friendly team players to exist in gameState.players.");
            }

            if (!friendlyPlayerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            var activeCharacterInstanceId = friendlyPlayerState.activeCharacterInstanceId.Value;
            if (!gameState.characterInstances.TryGetValue(activeCharacterInstanceId, out var activeCharacterInstance))
            {
                throw new InvalidOperationException("A008 anomaly reward input requires activeCharacterInstanceId to exist in gameState.characterInstances.");
            }

            if (!activeCharacterInstance.isInPlay || !activeCharacterInstance.isAlive)
            {
                continue;
            }

            if (string.Equals(activeCharacterInstance.definitionId, RyougiCharacterDefinitionId, StringComparison.Ordinal))
            {
                return friendlyPlayerId;
            }
        }

        return null;
    }

    private static bool isA005RewardSummonCardEligible(CardInstance cardInstance)
    {
        var treasureDefinition = TreasureDefinitionRepository.resolveByDefinitionId(cardInstance.definitionId);
        return treasureDefinition.summonSigilCost.HasValue &&
               treasureDefinition.summonSigilCost.Value <= A005RewardSummonSigilCostMax;
    }

    private static bool isA009RewardGapTreasureEligible(CardInstance cardInstance)
    {
        var treasureDefinition = TreasureDefinitionRepository.resolveByDefinitionId(cardInstance.definitionId);
        return treasureDefinition.summonSigilCost.HasValue &&
               treasureDefinition.summonSigilCost.Value <= A009RewardGapSummonSigilCostMax;
    }

    private static string createA005RewardSelectSummonCardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A005RewardSelectSummonCardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static CardInstanceId parseA005RewardSelectSummonCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A005RewardSelectSummonCardChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation choiceKey must start with summonCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A005RewardSelectSummonCardChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A005 anomaly reward continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static string createA009RewardSelectGapTreasureChoiceKey(CardInstanceId cardInstanceId)
    {
        return A009RewardSelectGapTreasureChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static CardInstanceId parseA009RewardSelectGapTreasureChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A009RewardSelectGapTreasureChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation choiceKey must start with gapCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A009RewardSelectGapTreasureChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A009 anomaly reward continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }
}
