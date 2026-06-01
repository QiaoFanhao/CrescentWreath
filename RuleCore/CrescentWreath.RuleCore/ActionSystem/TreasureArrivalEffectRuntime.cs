using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TreasureArrivalEffectRuntime
{
    public const string InputTypeKeyTreasureArrivalDiscardCardChoice = "treasureArrivalDiscardCardChoice";
    public const string InputTypeKeyTreasureArrivalOptionalBanishChoice = "treasureArrivalOptionalBanishChoice";
    public const string ChoiceKeyDiscardCardPrefix = "discardCard:";
    public const string ChoiceKeyDiscardNoCard = "discard:noCard";
    public const string ChoiceKeyBanishCardPrefix = "banishCard:";
    public const string ChoiceKeyDeclineOptionalBanish = "banish:decline";
    public const string ContinuationKeyT005ArrivalAllPlayersDiscard1 = "continuation:treasureArrival:T005:arrivalAllPlayersDiscard1";
    public const string ContinuationKeyT018ArrivalAllPlayersOptionalBanish1 = "continuation:treasureArrival:T018:arrivalAllPlayersOptionalBanish1";

    private const string ContextKeyT005ArrivalAllPlayersDiscard1 = "treasureArrival:T005:arrivalAllPlayersDiscard1";
    private const string ContextKeyT018ArrivalAllPlayersOptionalBanish1 = "treasureArrival:T018:arrivalAllPlayersOptionalBanish1";
    private const string LocalStateKeyArrivalQueue = "treasureArrival.queueCardIds";
    private const string LocalStateKeyArrivalQueueIndex = "treasureArrival.queueIndex";

    private readonly Func<long> nextInputContextIdSupplier;
    private readonly ZoneMovementService zoneMovementService;

    private enum ArrivalEffectKind
    {
        none = 0,
        t005AllPlayersDiscard1 = 1,
        t010AllPlayersDraw1 = 2,
        t018AllPlayersOptionalBanish1 = 3,
        t014AllPlayersHeal2 = 4,
    }

    public TreasureArrivalEffectRuntime(
        Func<long> nextInputContextIdSupplier,
        ZoneMovementService zoneMovementService)
    {
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
        this.zoneMovementService = zoneMovementService;
    }

    public static bool isTreasureArrivalContinuationKey(string? continuationKey)
    {
        return string.Equals(
            continuationKey,
            ContinuationKeyT005ArrivalAllPlayersDiscard1,
            StringComparison.Ordinal) ||
            string.Equals(
                continuationKey,
                ContinuationKeyT018ArrivalAllPlayersOptionalBanish1,
                StringComparison.Ordinal);
    }

    public bool tryStartArrivalEffectsForEnteredSummonZoneCards(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        IReadOnlyList<CardInstanceId> enteredSummonZoneCardInstanceIds)
    {
        var arrivalQueue = collectArrivalQueue(gameState, enteredSummonZoneCardInstanceIds);
        if (arrivalQueue.Count == 0)
        {
            return false;
        }

        saveArrivalQueue(actionChainState, arrivalQueue);
        saveArrivalQueueIndex(actionChainState, 0);
        continueArrivalQueue(gameState, actionChainState, eventId);
        return true;
    }

    public bool isParallelTreasureArrivalInputContext(InputContextState? inputContextState)
    {
        if (inputContextState is null)
        {
            return false;
        }

        var isParallelContext =
            string.Equals(inputContextState.contextKey, ContextKeyT005ArrivalAllPlayersDiscard1, StringComparison.Ordinal) ||
            string.Equals(inputContextState.contextKey, ContextKeyT018ArrivalAllPlayersOptionalBanish1, StringComparison.Ordinal);
        if (!isParallelContext)
        {
            return false;
        }

        return inputContextState.requiredPlayerIds.Count > 0;
    }

    public bool tryContinueOnSubmitInputChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!isTreasureArrivalContinuationKey(actionChainState.pendingContinuationKey))
        {
            return false;
        }

        var isT005ArrivalContext = string.Equals(
            inputContextState.contextKey,
            ContextKeyT005ArrivalAllPlayersDiscard1,
            StringComparison.Ordinal);
        var isT018ArrivalContext = string.Equals(
            inputContextState.contextKey,
            ContextKeyT018ArrivalAllPlayersOptionalBanish1,
            StringComparison.Ordinal);
        if (!isT005ArrivalContext && !isT018ArrivalContext)
        {
            throw new InvalidOperationException("Treasure arrival continuation requires currentInputContext.contextKey to be a supported treasureArrival context.");
        }

        var continuationName = isT005ArrivalContext
            ? "T005 arrival continuation"
            : "T018 arrival continuation";

        if (inputContextState.requiredPlayerIds.Count <= 0)
        {
            throw new InvalidOperationException($"{continuationName} requires currentInputContext.requiredPlayerIds.");
        }

        var requiredPlayerId = submitInputChoiceActionRequest.actorPlayerId;
        if (!containsPlayerId(inputContextState.requiredPlayerIds, requiredPlayerId))
        {
            throw new InvalidOperationException($"{continuationName} requires submitInputChoiceActionRequest.actorPlayerId to be one of currentInputContext.requiredPlayerIds.");
        }

        if (containsPlayerId(inputContextState.submittedPlayerIds, requiredPlayerId))
        {
            throw new InvalidOperationException($"{continuationName} does not allow duplicate submission by the same required player.");
        }

        if (!inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(requiredPlayerId.Value, out var availableChoiceKeys) ||
            availableChoiceKeys.Count <= 0)
        {
            throw new InvalidOperationException($"{continuationName} requires currentInputContext.choiceKeysByRequiredPlayerNumericId for actorPlayerId.");
        }

        if (!containsChoiceKey(availableChoiceKeys, submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException($"{continuationName} requires choiceKey to be one of currentInputContext.choiceKeysByRequiredPlayerNumericId[actorPlayerId].");
        }

        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException($"{continuationName} requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredPlayerState.handZoneId, out var requiredPlayerHandZoneState) ||
            !gameState.zones.TryGetValue(requiredPlayerState.discardZoneId, out var requiredPlayerDiscardZoneState))
        {
            throw new InvalidOperationException($"{continuationName} requires required player hand/discard zones to exist in gameState.zones.");
        }

        if (isT005ArrivalContext)
        {
            var selectedCardInstanceId = parseDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
            if (!selectedCardInstanceId.HasValue)
            {
                if (requiredPlayerHandZoneState.cardInstanceIds.Count > 0)
                {
                    throw new InvalidOperationException("T005 arrival continuation requires discard:noCard to be used only when required player has no hand cards.");
                }
            }
            else
            {
                if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId.Value, out var selectedCardInstance))
                {
                    throw new InvalidOperationException("T005 arrival continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
                }

                if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
                {
                    throw new InvalidOperationException("T005 arrival continuation requires selected discard card to be owned by currentInputContext.requiredPlayerId.");
                }

                if (selectedCardInstance.zoneId != requiredPlayerState.handZoneId ||
                    !requiredPlayerHandZoneState.cardInstanceIds.Contains(selectedCardInstanceId.Value))
                {
                    throw new InvalidOperationException("T005 arrival continuation requires selected discard card to still be in required player hand zone.");
                }

                var movedEvent = zoneMovementService.moveCard(
                    gameState,
                    selectedCardInstance,
                    requiredPlayerState.discardZoneId,
                    CardMoveReason.discard,
                    actionChainState.actionChainId,
                    submitInputChoiceActionRequest.requestId);
                actionChainState.producedEvents.Add(movedEvent);
            }
        }
        else
        {
            if (!string.Equals(
                    submitInputChoiceActionRequest.choiceKey,
                    ChoiceKeyDeclineOptionalBanish,
                    StringComparison.Ordinal))
            {
                if (gameState.publicState is null)
                {
                    throw new InvalidOperationException("T018 arrival continuation requires gameState.publicState to be initialized.");
                }

                var selectedCardInstanceId = parseBanishChoiceKey(submitInputChoiceActionRequest.choiceKey);
                if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
                {
                    throw new InvalidOperationException("T018 arrival continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
                }

                if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
                {
                    throw new InvalidOperationException("T018 arrival continuation requires selected banish card to be owned by currentInputContext.requiredPlayerId.");
                }

                var inHand = selectedCardInstance.zoneId == requiredPlayerState.handZoneId &&
                             requiredPlayerHandZoneState.cardInstanceIds.Contains(selectedCardInstanceId);
                var inDiscard = selectedCardInstance.zoneId == requiredPlayerState.discardZoneId &&
                                requiredPlayerDiscardZoneState.cardInstanceIds.Contains(selectedCardInstanceId);
                if (!inHand && !inDiscard)
                {
                    throw new InvalidOperationException("T018 arrival continuation requires selected banish card to still be in required player hand or discard zone.");
                }

                var movedEvent = zoneMovementService.moveCard(
                    gameState,
                    selectedCardInstance,
                    gameState.publicState.gapZoneId,
                    CardMoveReason.banish,
                    actionChainState.actionChainId,
                    submitInputChoiceActionRequest.requestId);
                actionChainState.producedEvents.Add(movedEvent);
            }
        }

        inputContextState.submittedPlayerIds.Add(requiredPlayerId);
        inputContextState.selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;

        if (inputContextState.submittedPlayerIds.Count < inputContextState.requiredPlayerIds.Count)
        {
            actionChainState.pendingContinuationKey = isT005ArrivalContext
                ? ContinuationKeyT005ArrivalAllPlayersDiscard1
                : ContinuationKeyT018ArrivalAllPlayersOptionalBanish1;
            actionChainState.isCompleted = false;
            return true;
        }

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = submitInputChoiceActionRequest.requestId,
            eventTypeKey = "inputContextClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = false,
        });
        gameState.currentInputContext = null;

        var arrivalQueueIndex = loadArrivalQueueIndexOrThrow(actionChainState);
        saveArrivalQueueIndex(actionChainState, arrivalQueueIndex + 1);
        continueArrivalQueue(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);
        return true;
    }

    private static List<CardInstanceId> collectArrivalQueue(
        GameState.GameState gameState,
        IReadOnlyList<CardInstanceId> enteredSummonZoneCardInstanceIds)
    {
        var arrivalQueue = new List<CardInstanceId>();
        for (var index = 0; index < enteredSummonZoneCardInstanceIds.Count; index++)
        {
            var enteredCardInstanceId = enteredSummonZoneCardInstanceIds[index];
            if (!gameState.cardInstances.TryGetValue(enteredCardInstanceId, out var enteredCardInstance))
            {
                continue;
            }

            var effectKind = resolveArrivalEffectKind(enteredCardInstance.definitionId);
            if (effectKind == ArrivalEffectKind.none)
            {
                continue;
            }

            arrivalQueue.Add(enteredCardInstanceId);
        }

        return arrivalQueue;
    }

    private static ArrivalEffectKind resolveArrivalEffectKind(string? definitionId)
    {
        return definitionId switch
        {
            "T005" => ArrivalEffectKind.t005AllPlayersDiscard1,
            "T010" => ArrivalEffectKind.t010AllPlayersDraw1,
            "T018" => ArrivalEffectKind.t018AllPlayersOptionalBanish1,
            "T014" => ArrivalEffectKind.t014AllPlayersHeal2,
            _ => ArrivalEffectKind.none,
        };
    }

    private void continueArrivalQueue(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var arrivalQueue = loadArrivalQueueOrThrow(actionChainState);
        var arrivalQueueIndex = loadArrivalQueueIndexOrThrow(actionChainState);
        if (arrivalQueueIndex < 0)
        {
            throw new InvalidOperationException("Treasure arrival continuation requires queueIndex to be non-negative.");
        }

        while (arrivalQueueIndex < arrivalQueue.Count)
        {
            var arrivalCardInstanceId = arrivalQueue[arrivalQueueIndex];
            if (!gameState.cardInstances.TryGetValue(arrivalCardInstanceId, out var arrivalCardInstance))
            {
                arrivalQueueIndex += 1;
                saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
                continue;
            }

            var effectKind = resolveArrivalEffectKind(arrivalCardInstance.definitionId);
            if (effectKind == ArrivalEffectKind.t005AllPlayersDiscard1)
            {
                if (tryOpenT005ArrivalDiscardInputContext(
                        gameState,
                        actionChainState,
                        eventId))
                {
                    return;
                }

                arrivalQueueIndex += 1;
                saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
                continue;
            }

            if (effectKind == ArrivalEffectKind.t010AllPlayersDraw1)
            {
                applyT010ArrivalAllPlayersDraw1(
                    gameState,
                    actionChainState,
                    eventId);
                arrivalQueueIndex += 1;
                saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
                continue;
            }

            if (effectKind == ArrivalEffectKind.t018AllPlayersOptionalBanish1)
            {
                if (tryOpenT018ArrivalOptionalBanishInputContext(
                        gameState,
                        actionChainState,
                        eventId))
                {
                    return;
                }

                arrivalQueueIndex += 1;
                saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
                continue;
            }

            if (effectKind == ArrivalEffectKind.t014AllPlayersHeal2)
            {
                applyT014ArrivalAllPlayersHeal2(
                    gameState,
                    actionChainState,
                    eventId);
                arrivalQueueIndex += 1;
                saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
                continue;
            }

            arrivalQueueIndex += 1;
            saveArrivalQueueIndex(actionChainState, arrivalQueueIndex);
        }

        clearArrivalQueueState(actionChainState);
        actionChainState.pendingContinuationKey = null;
    }

    private bool tryOpenT005ArrivalDiscardInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("T005 arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var seatOrderPlayerIds = resolveSeatOrderPlayerIds(gameState);
        if (seatOrderPlayerIds.Count <= 0)
        {
            return false;
        }

        var opened = openInputContext(
            gameState,
            actionChainState,
            eventId,
            seatOrderPlayerIds);
        return opened;
    }

    private bool tryOpenT018ArrivalOptionalBanishInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("T018 arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var seatOrderPlayerIds = resolveSeatOrderPlayerIds(gameState);
        if (seatOrderPlayerIds.Count <= 0)
        {
            return false;
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = InputTypeKeyTreasureArrivalOptionalBanishChoice,
            contextKey = ContextKeyT018ArrivalAllPlayersOptionalBanish1,
        };

        for (var index = 0; index < seatOrderPlayerIds.Count; index++)
        {
            var requiredPlayerId = seatOrderPlayerIds[index];
            if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
            {
                continue;
            }

            if (!gameState.zones.TryGetValue(requiredPlayerState.handZoneId, out var handZoneState) ||
                !gameState.zones.TryGetValue(requiredPlayerState.discardZoneId, out var discardZoneState))
            {
                throw new InvalidOperationException("T018 arrival input requires required player hand/discard zones to exist in gameState.zones.");
            }

            var choiceKeys = createOptionalBanishChoiceKeys(handZoneState.cardInstanceIds, discardZoneState.cardInstanceIds);
            inputContextState.requiredPlayerIds.Add(requiredPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[requiredPlayerId.Value] = choiceKeys;
        }

        if (inputContextState.requiredPlayerIds.Count <= 0)
        {
            return false;
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyT018ArrivalAllPlayersOptionalBanish1;
        actionChainState.isCompleted = false;
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });

        return true;
    }

    private void applyT010ArrivalAllPlayersDraw1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var seatOrderPlayerIds = resolveSeatOrderPlayerIds(gameState);
        for (var index = 0; index < seatOrderPlayerIds.Count; index++)
        {
            drawCardsForPlayer(
                gameState,
                actionChainState,
                seatOrderPlayerIds[index],
                eventId,
                drawCount: 1);
        }
    }

    private static void applyT014ArrivalAllPlayersHeal2(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var seatOrderPlayerIds = resolveSeatOrderPlayerIds(gameState);
        for (var index = 0; index < seatOrderPlayerIds.Count; index++)
        {
            var targetPlayerId = seatOrderPlayerIds[index];
            if (!tryResolveAliveInPlayActiveCharacter(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                continue;
            }

            var hpBefore = targetCharacterInstance.currentHp;
            var hpAfter = Math.Min(targetCharacterInstance.maxHp, hpBefore + 2);
            if (hpAfter == hpBefore)
            {
                continue;
            }

            targetCharacterInstance.currentHp = hpAfter;
            actionChainState.producedEvents.Add(new HpChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "hpChanged",
                sourceActionChainId = actionChainState.actionChainId,
                targetPlayerId = targetCharacterInstance.ownerPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                hpBefore = hpBefore,
                hpAfter = hpAfter,
                delta = hpAfter - hpBefore,
            });
        }
    }

    private bool openInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        IReadOnlyList<PlayerId> requiredPlayerIds)
    {
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = InputTypeKeyTreasureArrivalDiscardCardChoice,
            contextKey = ContextKeyT005ArrivalAllPlayersDiscard1,
        };
        for (var index = 0; index < requiredPlayerIds.Count; index++)
        {
            var requiredPlayerId = requiredPlayerIds[index];
            if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
            {
                continue;
            }

            if (!gameState.zones.TryGetValue(requiredPlayerState.handZoneId, out var handZoneState))
            {
                throw new InvalidOperationException("T005 arrival input requires required player handZoneId to exist in gameState.zones.");
            }

            var choiceKeys = createDiscardChoiceKeys(handZoneState.cardInstanceIds);
            if (choiceKeys.Count <= 0)
            {
                choiceKeys.Add(ChoiceKeyDiscardNoCard);
            }

            inputContextState.requiredPlayerIds.Add(requiredPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[requiredPlayerId.Value] = choiceKeys;
        }

        if (inputContextState.requiredPlayerIds.Count <= 0)
        {
            return false;
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyT005ArrivalAllPlayersDiscard1;
        actionChainState.isCompleted = false;
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });
        return true;
    }

    private static List<string> createDiscardChoiceKeys(IReadOnlyList<CardInstanceId> handCardInstanceIds)
    {
        var choiceKeys = new List<string>(handCardInstanceIds.Count);
        for (var index = 0; index < handCardInstanceIds.Count; index++)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + handCardInstanceIds[index].Value);
        }

        return choiceKeys;
    }

    private static List<string> createOptionalBanishChoiceKeys(
        IReadOnlyList<CardInstanceId> handCardInstanceIds,
        IReadOnlyList<CardInstanceId> discardCardInstanceIds)
    {
        var choiceKeys = new List<string>(1 + handCardInstanceIds.Count + discardCardInstanceIds.Count)
        {
            ChoiceKeyDeclineOptionalBanish,
        };

        for (var index = 0; index < handCardInstanceIds.Count; index++)
        {
            choiceKeys.Add(ChoiceKeyBanishCardPrefix + handCardInstanceIds[index].Value);
        }

        for (var index = 0; index < discardCardInstanceIds.Count; index++)
        {
            choiceKeys.Add(ChoiceKeyBanishCardPrefix + discardCardInstanceIds[index].Value);
        }

        return choiceKeys;
    }

    private static CardInstanceId? parseDiscardChoiceKey(string choiceKey)
    {
        if (string.Equals(choiceKey, ChoiceKeyDiscardNoCard, StringComparison.Ordinal))
        {
            return null;
        }

        if (!choiceKey.StartsWith(ChoiceKeyDiscardCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T005 arrival discard choice requires choiceKey prefix discardCard:.");
        }

        var cardIdSegment = choiceKey.Substring(ChoiceKeyDiscardCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T005 arrival discard choice requires numeric cardInstanceId segment.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseBanishChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyBanishCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T018 arrival banish choice requires choiceKey prefix banishCard:.");
        }

        var cardIdSegment = choiceKey.Substring(ChoiceKeyBanishCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T018 arrival banish choice requires numeric cardInstanceId segment.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static List<PlayerId> resolveSeatOrderPlayerIds(GameState.GameState gameState)
    {
        if (gameState.matchMeta is not null && gameState.matchMeta.seatOrder.Count > 0)
        {
            return new List<PlayerId>(gameState.matchMeta.seatOrder);
        }

        var playerIds = new List<PlayerId>(gameState.players.Keys);
        playerIds.Sort((leftPlayerId, rightPlayerId) => leftPlayerId.Value.CompareTo(rightPlayerId.Value));
        return playerIds;
    }

    private static bool tryResolveAliveInPlayActiveCharacter(
        GameState.GameState gameState,
        PlayerId playerId,
        out CharacterInstance targetCharacterInstance)
    {
        targetCharacterInstance = null!;
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            return false;
        }

        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var characterInstance))
        {
            return false;
        }

        if (!characterInstance.isAlive || !characterInstance.isInPlay)
        {
            return false;
        }

        targetCharacterInstance = characterInstance;
        return true;
    }

    private void drawCardsForPlayer(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long requestId,
        int drawCount)
    {
        if (drawCount <= 0)
        {
            return;
        }

        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            throw new InvalidOperationException("Treasure arrival draw requires playerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState) ||
            !gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("Treasure arrival draw requires player deck/discard zones to exist in gameState.zones.");
        }

        for (var drawIndex = 0; drawIndex < drawCount; drawIndex++)
        {
            if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
            {
                var discardCardIdsInCurrentOrder = new List<CardInstanceId>(discardZoneState.cardInstanceIds);
                for (var discardIndex = 0; discardIndex < discardCardIdsInCurrentOrder.Count; discardIndex++)
                {
                    var cardInstanceId = discardCardIdsInCurrentOrder[discardIndex];
                    var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                    var rebuildEvent = zoneMovementService.moveCard(
                        gameState,
                        discardedCardInstance,
                        playerState.deckZoneId,
                        CardMoveReason.returnToSource,
                        actionChainState.actionChainId,
                        requestId);
                    actionChainState.producedEvents.Add(rebuildEvent);
                }
            }

            if (deckZoneState.cardInstanceIds.Count <= 0)
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
                requestId);
            actionChainState.producedEvents.Add(drawEvent);
        }
    }

    private static bool containsPlayerId(IReadOnlyList<PlayerId> playerIds, PlayerId targetPlayerId)
    {
        for (var index = 0; index < playerIds.Count; index++)
        {
            if (playerIds[index] == targetPlayerId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool containsChoiceKey(IReadOnlyList<string> choiceKeys, string choiceKey)
    {
        for (var index = 0; index < choiceKeys.Count; index++)
        {
            if (string.Equals(choiceKeys[index], choiceKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void saveArrivalQueue(ActionChainState actionChainState, IReadOnlyList<CardInstanceId> arrivalQueue)
    {
        var serializedValue = string.Empty;
        for (var index = 0; index < arrivalQueue.Count; index++)
        {
            if (index > 0)
            {
                serializedValue += ",";
            }

            serializedValue += arrivalQueue[index].Value.ToString();
        }

        actionChainState.localState[LocalStateKeyArrivalQueue] = serializedValue;
    }

    private static List<CardInstanceId> loadArrivalQueueOrThrow(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyArrivalQueue, out var serializedQueue) ||
            string.IsNullOrWhiteSpace(serializedQueue))
        {
            throw new InvalidOperationException("Treasure arrival continuation requires queueCardIds to exist in currentActionChain.localState.");
        }

        var segments = serializedQueue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var queue = new List<CardInstanceId>(segments.Length);
        foreach (var segment in segments)
        {
            if (!long.TryParse(segment, out var numericCardInstanceId) || numericCardInstanceId <= 0)
            {
                throw new InvalidOperationException("Treasure arrival continuation queueCardIds contains a non-positive cardInstanceId.");
            }

            queue.Add(new CardInstanceId(numericCardInstanceId));
        }

        return queue;
    }

    private static void saveArrivalQueueIndex(ActionChainState actionChainState, int arrivalQueueIndex)
    {
        actionChainState.localState[LocalStateKeyArrivalQueueIndex] = arrivalQueueIndex.ToString();
    }

    private static int loadArrivalQueueIndexOrThrow(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyArrivalQueueIndex, out var serializedValue) ||
            !int.TryParse(serializedValue, out var arrivalQueueIndex))
        {
            throw new InvalidOperationException("Treasure arrival continuation requires queueIndex to exist in currentActionChain.localState.");
        }

        return arrivalQueueIndex;
    }

    private static void clearArrivalQueueState(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyArrivalQueue);
        actionChainState.localState.Remove(LocalStateKeyArrivalQueueIndex);
    }
}
