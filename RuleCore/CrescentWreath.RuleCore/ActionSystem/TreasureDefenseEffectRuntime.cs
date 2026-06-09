using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TreasureDefenseEffectRuntime
{
    public const string DefinitionIdT025 = "T025";
    public const string InputTypeKeyT025ExtraDiscardChoice = "treasureDefenseT025ExtraDiscardChoice";
    public const string ContextKeyT025ExtraDiscard = "treasureDefense:T025:extraDiscard";
    public const string ContinuationKeyT025ExtraDiscard = "continuation:treasureDefense:T025:extraDiscard";

    public const string LocalStateKeyT025DefenseCardInstanceId = "treasureDefense:T025:defenseCardInstanceId";

    private readonly Func<long> nextInputContextIdSupplier;
    private readonly ZoneMovementService zoneMovementService;

    public TreasureDefenseEffectRuntime(
        Func<long> nextInputContextIdSupplier,
        ZoneMovementService zoneMovementService)
    {
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
        this.zoneMovementService = zoneMovementService;
    }

    public static bool isTreasureDefenseContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyT025ExtraDiscard, StringComparison.Ordinal);
    }

    public static void ensureValidT025ExtraDiscardChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(inputContextState.contextKey, ContextKeyT025ExtraDiscard, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires currentInputContext.contextKey to be treasureDefense:T025:extraDiscard.");
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                TreasureOnPlayEffectRuntime.ChoiceKeyDeclineDiscard,
                StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKeys.Count != 0)
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires choiceKeys to be empty when choiceKey is discard:decline.");
            }

            return;
        }

        if (submitInputChoiceActionRequest.choiceKeys.Count == 0)
        {
            if (submitInputChoiceActionRequest.choiceKey.StartsWith(
                    TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix,
                    StringComparison.Ordinal) &&
                inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                return;
            }

            throw new InvalidOperationException("T025 extra-discard defense continuation requires choiceKey=discard:decline, choiceKey=discardCard:{id}, or choiceKeys to contain selected discardCard choices.");
        }

        var selectedChoiceKeySet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selectedChoiceKey in submitInputChoiceActionRequest.choiceKeys)
        {
            if (!selectedChoiceKey.StartsWith(
                    TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires selected choiceKeys to use discardCard:{id} format.");
            }

            if (!inputContextState.choiceKeys.Contains(selectedChoiceKey))
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires every selected choiceKey to be one of currentInputContext.choiceKeys.");
            }

            if (!selectedChoiceKeySet.Add(selectedChoiceKey))
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires selected choiceKeys to be unique.");
            }
        }
    }

    public void openT025ExtraDiscardInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerState defenderPlayerState,
        CardInstanceId defenseCardInstanceId)
    {
        var choiceKeys = collectExtraDiscardChoiceKeys(gameState, defenderPlayerState);
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = defenderPlayerState.playerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = InputTypeKeyT025ExtraDiscardChoice,
            contextKey = ContextKeyT025ExtraDiscard,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyT025ExtraDiscard;
        actionChainState.localState[LocalStateKeyT025DefenseCardInstanceId] =
            defenseCardInstanceId.Value.ToString();
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
    }

    public int continueT025ExtraDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!isTreasureDefenseContinuationKey(actionChainState.pendingContinuationKey))
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires currentActionChain.pendingContinuationKey to be continuation:treasureDefense:T025:extraDiscard.");
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                TreasureOnPlayEffectRuntime.ChoiceKeyDeclineDiscard,
                StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = ActionRequestProcessor.ContinuationKeyStagedResponseDamage;
            return 0;
        }

        if (!gameState.players.TryGetValue(submitInputChoiceActionRequest.actorPlayerId, out var defenderPlayerState))
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires actor player to exist.");
        }

        var selectedChoiceKeys = submitInputChoiceActionRequest.choiceKeys.Count > 0
            ? submitInputChoiceActionRequest.choiceKeys
            : new List<string> { submitInputChoiceActionRequest.choiceKey };
        var extraDiscardCount = 0;
        foreach (var selectedChoiceKey in selectedChoiceKeys)
        {
            var selectedCardInstanceId = parseDiscardCardChoiceKey(selectedChoiceKey);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires selected card instance to exist.");
            }

            if (selectedCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId ||
                selectedCardInstance.zoneId != defenderPlayerState.handZoneId)
            {
                throw new InvalidOperationException("T025 extra-discard defense continuation requires selected card to remain in actor hand.");
            }

            var discardEvent = zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                defenderPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(discardEvent);
            extraDiscardCount++;
        }

        actionChainState.pendingContinuationKey = ActionRequestProcessor.ContinuationKeyStagedResponseDamage;
        return extraDiscardCount;
    }

    private static List<string> collectExtraDiscardChoiceKeys(
        GameState.GameState gameState,
        PlayerState defenderPlayerState)
    {
        var choiceKeys = new List<string>
        {
            TreasureOnPlayEffectRuntime.ChoiceKeyDeclineDiscard,
        };

        if (!gameState.zones.TryGetValue(defenderPlayerState.handZoneId, out var handZoneState))
        {
            return choiceKeys;
        }

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static CardInstanceId parseDiscardCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T025 extra-discard choice requires choiceKey to use discardCard:{id} format.");
        }

        var cardIdSegment = choiceKey.Substring(TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T025 extra-discard choice requires a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }
}
