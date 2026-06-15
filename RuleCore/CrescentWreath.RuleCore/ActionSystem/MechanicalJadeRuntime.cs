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

public sealed class MechanicalJadeRuntime
{
    public const string DefinitionId = "T016";
    public const string InputTypeKeyOverlayAfterKillChoice = "mechanicalJadeOverlayAfterKillChoice";
    public const string ContextKeyOverlayAfterKill = "treasurePersistent:T016:overlayAfterKill";
    public const string ContinuationKeyOverlayAfterKill = "continuation:treasurePersistent:T016:overlayAfterKill";
    public const string LocalStateKeyContainerCardInstanceId = "treasurePersistent:T016:containerCardInstanceId";
    public const string LocalStateKeyPendingTriggerQueue = "treasurePersistent:T016:pendingTriggerQueue";

    private const int MaxOverlayCount = 2;
    private const int ManaGainPerOverlay = 2;
    private const int SigilGainPerOverlay = 2;

    private readonly Func<long> nextInputContextIdSupplier;
    private readonly ZoneMovementService zoneMovementService;

    public MechanicalJadeRuntime(
        Func<long> nextInputContextIdSupplier,
        ZoneMovementService zoneMovementService)
    {
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
        this.zoneMovementService = zoneMovementService;
    }

    public static bool isMechanicalJadeContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyOverlayAfterKill, StringComparison.Ordinal);
    }

    public static int resolveOverlayResourceBonus(GameState.GameState gameState, CardInstance cardInstance)
    {
        if (!string.Equals(cardInstance.definitionId, DefinitionId, StringComparison.Ordinal))
        {
            return 0;
        }

        return OverlayRuntime.getOverlayCardCount(gameState, cardInstance.cardInstanceId) * SigilGainPerOverlay;
    }

    public static void applyTurnStartResourceBonuses(
        GameState.GameState gameState,
        PlayerState playerState)
    {
        var overlayCount = countMechanicalJadeOverlaysOnField(gameState, playerState);
        if (overlayCount <= 0)
        {
            return;
        }

        playerState.mana += overlayCount * ManaGainPerOverlay;
        playerState.sigilPreview += overlayCount * SigilGainPerOverlay;
    }

    public bool tryOpenOverlayAfterKillInputContextFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (gameState.matchState != MatchState.running ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return false;
        }

        if (producedEventsStartIndex < 0 ||
            producedEventsStartIndex >= actionChainState.producedEvents.Count)
        {
            return false;
        }

        var pendingTriggers = new List<string>();
        for (var eventIndex = producedEventsStartIndex; eventIndex < actionChainState.producedEvents.Count; eventIndex++)
        {
            if (actionChainState.producedEvents[eventIndex] is not KillRecordedEvent killRecordedEvent ||
                !killRecordedEvent.killerPlayerId.HasValue)
            {
                continue;
            }

            var killerPlayerId = killRecordedEvent.killerPlayerId.Value;
            if (!gameState.players.TryGetValue(killerPlayerId, out var killerPlayerState))
            {
                continue;
            }

            var eligibleMechanicalJades = findEligibleMechanicalJades(gameState, killerPlayerState);
            foreach (var containerCardInstance in eligibleMechanicalJades)
            {
                pendingTriggers.Add(
                    killerPlayerId.Value + ":" + containerCardInstance.cardInstanceId.Value);
            }
        }

        if (pendingTriggers.Count == 0)
        {
            return false;
        }

        actionChainState.localState[LocalStateKeyPendingTriggerQueue] =
            string.Join(",", pendingTriggers);
        return tryOpenNextQueuedOverlayAfterKillInputContext(
            gameState,
            actionChainState,
            eventId);
    }

    public static void ensureValidOverlayAfterKillChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(inputContextState.contextKey, ContextKeyOverlayAfterKill, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires currentInputContext.contextKey to be treasurePersistent:T016:overlayAfterKill.");
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOverlay,
                StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKeys.Count != 0)
            {
                throw new InvalidOperationException("T016 overlay-after-kill continuation requires choiceKeys to be empty when choiceKey is overlay:decline.");
            }

            return;
        }

        if (submitInputChoiceActionRequest.choiceKeys.Count != 0)
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires exactly one selected overlayCard choiceKey.");
        }

        if (!submitInputChoiceActionRequest.choiceKey.StartsWith(
                TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires selected choiceKey to use overlayCard:{id} format.");
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires selected choiceKey to be one of currentInputContext.choiceKeys.");
        }
    }

    public bool tryContinueOnSubmitInputChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!isMechanicalJadeContinuationKey(actionChainState.pendingContinuationKey))
        {
            return false;
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOverlay,
                StringComparison.Ordinal))
        {
            actionChainState.localState.Remove(LocalStateKeyContainerCardInstanceId);
            actionChainState.pendingContinuationKey = null;
            tryOpenNextQueuedOverlayAfterKillInputContext(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);
            return true;
        }

        if (!actionChainState.localState.TryGetValue(LocalStateKeyContainerCardInstanceId, out var containerCardIdText) ||
            !long.TryParse(containerCardIdText, out var containerCardNumericId))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires container card instance id in actionChain.localState.");
        }

        var containerCardInstanceId = new CardInstanceId(containerCardNumericId);
        if (!gameState.cardInstances.TryGetValue(containerCardInstanceId, out var containerCardInstance))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires container card instance to exist.");
        }

        if (!gameState.players.TryGetValue(submitInputChoiceActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires actor player to exist.");
        }

        if (containerCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId ||
            containerCardInstance.zoneId != actorPlayerState.fieldZoneId ||
            !string.Equals(containerCardInstance.definitionId, DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires source Mechanical Jade to remain in actor field zone.");
        }

        if (OverlayRuntime.getOverlayCardCount(gameState, containerCardInstance.cardInstanceId) >= MaxOverlayCount)
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires source Mechanical Jade overlay count to be less than 2.");
        }

        var selectedCardInstanceId = parseOverlayCardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires selected card instance to exist.");
        }

        if (selectedCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires selected overlay card to be owned by actor player.");
        }

        if (!isSelectableOverlaySourceZone(gameState, actorPlayerState, selectedCardInstance))
        {
            throw new InvalidOperationException("T016 overlay-after-kill continuation requires selected overlay card to be in actor hand, actor discard, or gap zone.");
        }

        var overlayEvent = OverlayRuntime.overlayCardUnderContainer(
            gameState,
            zoneMovementService,
            selectedCardInstance,
            containerCardInstance,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(overlayEvent);
        applyOverlayDeltaResourceBonusForCurrentOwnerTurn(gameState, actorPlayerState);

        actionChainState.localState.Remove(LocalStateKeyContainerCardInstanceId);
        actionChainState.pendingContinuationKey = null;
        tryOpenNextQueuedOverlayAfterKillInputContext(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);
        return true;
    }

    private bool tryOpenNextQueuedOverlayAfterKillInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        while (tryDequeuePendingTrigger(
                   actionChainState,
                   out var requiredPlayerId,
                   out var containerCardInstanceId))
        {
            if (!gameState.players.TryGetValue(requiredPlayerId, out var playerState) ||
                !gameState.cardInstances.TryGetValue(containerCardInstanceId, out var containerCardInstance) ||
                containerCardInstance.ownerPlayerId != requiredPlayerId ||
                containerCardInstance.zoneId != playerState.fieldZoneId ||
                !string.Equals(containerCardInstance.definitionId, DefinitionId, StringComparison.Ordinal) ||
                OverlayRuntime.getOverlayCardCount(gameState, containerCardInstanceId) >= MaxOverlayCount)
            {
                continue;
            }

            var choiceKeys = collectOverlayChoiceKeys(gameState, playerState);
            if (choiceKeys.Count <= 1)
            {
                continue;
            }

            openOverlayAfterKillInputContext(
                gameState,
                actionChainState,
                eventId,
                requiredPlayerId,
                containerCardInstance,
                choiceKeys);
            return true;
        }

        actionChainState.localState.Remove(LocalStateKeyPendingTriggerQueue);
        return false;
    }

    private static bool tryDequeuePendingTrigger(
        ActionChainState actionChainState,
        out PlayerId requiredPlayerId,
        out CardInstanceId containerCardInstanceId)
    {
        requiredPlayerId = default;
        containerCardInstanceId = default;
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyPendingTriggerQueue,
                out var serializedQueue) ||
            string.IsNullOrWhiteSpace(serializedQueue))
        {
            return false;
        }

        var entries = serializedQueue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            actionChainState.localState.Remove(LocalStateKeyPendingTriggerQueue);
            return false;
        }

        actionChainState.localState[LocalStateKeyPendingTriggerQueue] =
            entries.Length == 1 ? string.Empty : string.Join(",", entries, 1, entries.Length - 1);

        var segments = entries[0].Split(':');
        if (segments.Length != 2 ||
            !long.TryParse(segments[0], out var playerNumericId) ||
            !long.TryParse(segments[1], out var cardNumericId))
        {
            throw new InvalidOperationException("T016 pending trigger queue contains an invalid player/card entry.");
        }

        requiredPlayerId = new PlayerId(playerNumericId);
        containerCardInstanceId = new CardInstanceId(cardNumericId);
        return true;
    }

    private void openOverlayAfterKillInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId requiredPlayerId,
        CardInstance containerCardInstance,
        List<string> choiceKeys)
    {
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = InputTypeKeyOverlayAfterKillChoice,
            contextKey = ContextKeyOverlayAfterKill,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyOverlayAfterKill;
        actionChainState.localState[LocalStateKeyContainerCardInstanceId] =
            containerCardInstance.cardInstanceId.Value.ToString();
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

    private static List<string> collectOverlayChoiceKeys(
        GameState.GameState gameState,
        PlayerState playerState)
    {
        var choiceKeys = new List<string>
        {
            TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOverlay,
        };

        appendOverlayChoiceKeysFromZone(gameState, playerState.handZoneId, choiceKeys);
        appendOverlayChoiceKeysFromZone(gameState, playerState.discardZoneId, choiceKeys);
        if (gameState.publicState is not null)
        {
            appendOverlayChoiceKeysFromZone(gameState, gameState.publicState.gapZoneId, choiceKeys, playerState.playerId);
        }

        return choiceKeys;
    }

    private static void appendOverlayChoiceKeysFromZone(
        GameState.GameState gameState,
        ZoneId zoneId,
        List<string> choiceKeys,
        PlayerId? requiredOwnerPlayerId = null)
    {
        if (!gameState.zones.TryGetValue(zoneId, out var zoneState))
        {
            return;
        }

        foreach (var cardInstanceId in zoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            if (requiredOwnerPlayerId.HasValue && cardInstance.ownerPlayerId != requiredOwnerPlayerId.Value)
            {
                continue;
            }

            choiceKeys.Add(TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix + cardInstanceId.Value);
        }
    }

    private static List<CardInstance> findEligibleMechanicalJades(
        GameState.GameState gameState,
        PlayerState playerState)
    {
        var result = new List<CardInstance>();
        if (!gameState.zones.TryGetValue(playerState.fieldZoneId, out var fieldZoneState))
        {
            return result;
        }

        foreach (var cardInstanceId in fieldZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            if (!string.Equals(cardInstance.definitionId, DefinitionId, StringComparison.Ordinal) ||
                cardInstance.ownerPlayerId != playerState.playerId ||
                OverlayRuntime.getOverlayCardCount(gameState, cardInstance.cardInstanceId) >= MaxOverlayCount)
            {
                continue;
            }

            result.Add(cardInstance);
        }

        return result;
    }

    private static int countMechanicalJadeOverlaysOnField(
        GameState.GameState gameState,
        PlayerState playerState)
    {
        if (!gameState.zones.TryGetValue(playerState.fieldZoneId, out var fieldZoneState))
        {
            return 0;
        }

        var overlayCount = 0;
        foreach (var cardInstanceId in fieldZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
                !string.Equals(cardInstance.definitionId, DefinitionId, StringComparison.Ordinal))
            {
                continue;
            }

            overlayCount += OverlayRuntime.getOverlayCardCount(gameState, cardInstance.cardInstanceId);
        }

        return overlayCount;
    }

    private static bool isSelectableOverlaySourceZone(
        GameState.GameState gameState,
        PlayerState actorPlayerState,
        CardInstance selectedCardInstance)
    {
        if (selectedCardInstance.zoneId == actorPlayerState.handZoneId ||
            selectedCardInstance.zoneId == actorPlayerState.discardZoneId)
        {
            return true;
        }

        return gameState.publicState is not null &&
               selectedCardInstance.zoneId == gameState.publicState.gapZoneId;
    }

    private static CardInstanceId parseOverlayCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T016 overlay-after-kill choice requires choiceKey to use overlayCard:{id} format.");
        }

        var cardIdSegment = choiceKey.Substring(TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T016 overlay-after-kill choice requires a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static void applyOverlayDeltaResourceBonusForCurrentOwnerTurn(
        GameState.GameState gameState,
        PlayerState ownerPlayerState)
    {
        if (gameState.turnState is null ||
            gameState.turnState.currentPlayerId != ownerPlayerState.playerId)
        {
            return;
        }

        if (gameState.turnState.currentPhase == TurnPhase.action ||
            gameState.turnState.currentPhase == TurnPhase.start)
        {
            ownerPlayerState.mana += ManaGainPerOverlay;
            ownerPlayerState.sigilPreview += SigilGainPerOverlay;
            return;
        }

        if (gameState.turnState.currentPhase == TurnPhase.summon)
        {
            ownerPlayerState.mana += ManaGainPerOverlay;
            ownerPlayerState.lockedSigil = (ownerPlayerState.lockedSigil ?? 0) + SigilGainPerOverlay;
            ownerPlayerState.isSigilLocked = true;
        }
    }
}
