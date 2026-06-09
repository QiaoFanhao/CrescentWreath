using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyArrivalInputRuntime
{
    private const string A003ArrivalSelectOpponentShackleInputTypeKey = "anomalyA003ArrivalSelectOpponentShackle";
    private const string A003ArrivalSelectOpponentShackleContextKey = "anomaly:A003:arrivalSelectOpponentShackle";
    private const string A003ArrivalSelectOpponentShackleChoiceKeyPrefix = "opponentPlayer:";
    private const string A001ArrivalHumanDiscardInputTypeKey = "anomalyA001ArrivalHumanDiscardOne";
    private const string A001ArrivalHumanDiscardContextKey = "anomaly:A001:arrivalHumanDiscardFlow";
    private const string A001ArrivalHumanDiscardChoiceKeyPrefix = "handCard:";
    private const string A001RemiliaDefinitionId = "C003";
    private const string A002YuyukoDefinitionId = "C018";
    private const string A002ArrivalInputTypeKey = "anomalyA002ArrivalParallelDirectSummonChoice";
    private const string A002ArrivalContextKey = "anomaly:A002:arrivalParallelDirectSummonChoice";
    private const string A002ArrivalChoiceKeySakuraAccept = "sakuraCake:accept";
    private const string A002ArrivalChoiceKeySakuraDecline = "sakuraCake:decline";
    private const string A002ArrivalChoiceKeySummonCardPrefix = "summonCard:";
    private const string A002ArrivalSelectedChoiceLocalStatePrefix = "anomaly:A002:arrival:selectedChoice:";
    private const int A002YuyukoSummonCostMax = 5;
    private const string A006ArrivalHumanDefenseDiscardInputTypeKey = "anomalyA006ArrivalHumanDefenseDiscardOne";
    private const string A006ArrivalHumanDefenseDiscardContextKey = "anomaly:A006:arrivalHumanDefenseDiscardFlow";
    private const string A006ArrivalHumanDefenseDiscardChoiceKeyPrefix = "fieldCard:";
    private const string A005ArrivalDirectSummonInputTypeKey = "anomalyA005ArrivalDirectSummonFromSummonZone";
    private const string A005ArrivalDirectSummonContextKey = "anomaly:A005:arrivalDirectSummonFromSummonZone";
    private const string A005ArrivalDirectSummonChoiceKeyPrefix = "summonCard:";
    private const string A005ArrivalDirectSummonChoiceKeyDecline = "summon:decline";
    private const string A007RinDefinitionId = "C007";
    private const string A007ArrivalInputTypeKeyOptionalHandBanish = "anomalyA007ArrivalOptionalHandBanish";
    private const string A007ArrivalInputTypeKeyOptionalDiscardBanishDecision = "anomalyA007ArrivalOptionalDiscardBanishDecision";
    private const string A007ArrivalInputTypeKeyOptionalDiscardBanishSelect = "anomalyA007ArrivalOptionalDiscardBanishSelect";
    private const string A007ArrivalContextKey = "anomaly:A007:arrivalOptionalBanishFlow";
    private const string A007ArrivalChoiceKeyHandCardPrefix = "handCard:";
    private const string A007ArrivalChoiceKeyDiscardCardPrefix = "discardCard:";
    private const string A007ArrivalChoiceKeyAccept = "accept";
    private const string A007ArrivalChoiceKeyDecline = "decline";

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyArrivalInputRuntime(ZoneMovementService zoneMovementService, Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool executeA001ArrivalNonHumanDrawAndMaybeOpenHumanDiscardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        string pendingContinuationKey,
        long eventId)
    {
        executeDrawOneForNonHumanActiveCharacterPlayers(
            gameState,
            actionChainState,
            eventId);

        var humanDiscardPlayerIds = resolveA001HumanDiscardPlayerIds(gameState);
        if (humanDiscardPlayerIds.Count == 0)
        {
            executeA001RemiliaFullHeal(gameState);
            return false;
        }

        openA001ArrivalHumanDiscardInputContext(
            gameState,
            actionChainState,
            humanDiscardPlayerIds[0],
            pendingContinuationKey,
            eventId);
        return true;
    }

    public void ensureValidA001ArrivalHumanDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A001ArrivalHumanDiscardContextKey)
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A001:arrivalHumanDiscardFlow.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        var requiredPlayerId = inputContextState.requiredPlayerId.Value;
        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredPlayerState.handZoneId, out var requiredPlayerHandZoneState))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires required player handZoneId to exist in gameState.zones.");
        }

        var selectedCardInstanceId = parseA001ArrivalHumanDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires selected card to be owned by currentInputContext.requiredPlayerId.");
        }

        if (selectedCardInstance.zoneId != requiredPlayerState.handZoneId ||
            !requiredPlayerHandZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires selected card to still be in required player hand zone.");
        }
    }

    public AnomalyArrivalInputAdvanceResult continueA001ArrivalHumanDiscardFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string pendingContinuationKey)
    {
        ensureValidA001ArrivalHumanDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        var requiredPlayerId = inputContextState.requiredPlayerId!.Value;
        var requiredPlayerState = gameState.players[requiredPlayerId];
        var selectedCardInstanceId = parseA001ArrivalHumanDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            requiredPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(movedEvent);

        var humanDiscardPlayerIds = resolveA001HumanDiscardPlayerIds(gameState);
        var requiredPlayerIndex = indexOfPlayerId(humanDiscardPlayerIds, requiredPlayerId);
        if (requiredPlayerIndex < 0)
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation requires requiredPlayerId to exist in current human discard player list.");
        }

        if (requiredPlayerIndex + 1 < humanDiscardPlayerIds.Count)
        {
            openA001ArrivalHumanDiscardInputContext(
                gameState,
                actionChainState,
                humanDiscardPlayerIds[requiredPlayerIndex + 1],
                pendingContinuationKey,
                submitInputChoiceActionRequest.requestId);
            return AnomalyArrivalInputAdvanceResult.createPending();
        }

        executeA001RemiliaFullHeal(gameState);
        return AnomalyArrivalInputAdvanceResult.createCompleted();
    }

    public bool tryOpenA002ArrivalParallelDirectSummonInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        string pendingContinuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A002 anomaly arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        if (seatOrderPlayerIds.Count == 0)
        {
            return false;
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A002ArrivalInputTypeKey,
            contextKey = A002ArrivalContextKey,
        };

        foreach (var requiredPlayerId in seatOrderPlayerIds)
        {
            if (!gameState.players.ContainsKey(requiredPlayerId))
            {
                continue;
            }

            var choiceKeys = createA002ArrivalChoiceKeysForPlayer(gameState, requiredPlayerId);
            inputContextState.requiredPlayerIds.Add(requiredPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[requiredPlayerId.Value] = choiceKeys;
        }

        if (inputContextState.requiredPlayerIds.Count == 0)
        {
            return false;
        }

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
        return true;
    }

    public bool isA002ArrivalParallelInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               string.Equals(inputContextState.contextKey, A002ArrivalContextKey, StringComparison.Ordinal) &&
               inputContextState.requiredPlayerIds.Count > 0;
    }

    public void continueA002ArrivalParallelDirectSummonChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string pendingContinuationKey)
    {
        ensureValidA002ArrivalParallelDirectSummonChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        var requiredPlayerId = submitInputChoiceActionRequest.actorPlayerId;
        inputContextState.submittedPlayerIds.Add(requiredPlayerId);
        actionChainState.localState[createA002ArrivalSelectedChoiceLocalStateKey(requiredPlayerId)] =
            submitInputChoiceActionRequest.choiceKey;

        if (inputContextState.submittedPlayerIds.Count < inputContextState.requiredPlayerIds.Count)
        {
            actionChainState.pendingContinuationKey = pendingContinuationKey;
            actionChainState.isCompleted = false;
            return;
        }

        executeA002ArrivalChoicesInTurnOrder(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest.requestId);

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
        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }

    public bool executeA006ArrivalMaybeOpenHumanDefenseDiscardInputContextAndMaybeDrawNonHuman(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        string pendingContinuationKey,
        long eventId)
    {
        var humanDefenseDiscardPlayerIds = resolveA006HumanDefenseDiscardPlayerIds(gameState);
        if (humanDefenseDiscardPlayerIds.Count == 0)
        {
            executeDrawOneForNonHumanActiveCharacterPlayers(
                gameState,
                actionChainState,
                eventId);
            return false;
        }

        openA006ArrivalHumanDefenseDiscardInputContext(
            gameState,
            actionChainState,
            humanDefenseDiscardPlayerIds,
            pendingContinuationKey,
            eventId);
        return true;
    }

    public bool isA006ArrivalParallelInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               inputContextState.requiredPlayerIds.Count > 0 &&
               string.Equals(
                   inputContextState.contextKey,
                   A006ArrivalHumanDefenseDiscardContextKey,
                   StringComparison.Ordinal);
    }

    private List<string> createA002ArrivalChoiceKeysForPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId requiredPlayerId)
    {
        var choiceKeys = new List<string>
        {
            A002ArrivalChoiceKeySakuraDecline,
            A002ArrivalChoiceKeySakuraAccept,
        };

        if (!isA002YuyukoPlayer(gameState, requiredPlayerId))
        {
            return choiceKeys;
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly arrival input requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("A002 anomaly arrival input requires summonZone to exist.");
        }

        foreach (var summonZoneCardInstanceId in summonZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(summonZoneCardInstanceId, out var summonZoneCardInstance))
            {
                throw new InvalidOperationException("A002 anomaly arrival input requires summonZone cardInstanceIds to exist in gameState.cardInstances.");
            }

            var treasureDefinition = TreasureDefinitionRepository.resolveByDefinitionId(summonZoneCardInstance.definitionId);
            if (treasureDefinition.summonSigilCost.HasValue &&
                treasureDefinition.summonSigilCost.Value <= A002YuyukoSummonCostMax)
            {
                choiceKeys.Add(createA002ArrivalSummonCardChoiceKey(summonZoneCardInstanceId));
            }
        }

        return choiceKeys;
    }

    private void ensureValidA002ArrivalParallelDirectSummonChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(inputContextState.contextKey, A002ArrivalContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A002:arrivalParallelDirectSummonChoice.");
        }

        var requiredPlayerId = submitInputChoiceActionRequest.actorPlayerId;
        if (!inputContextState.requiredPlayerIds.Contains(requiredPlayerId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires actorPlayerId to be in currentInputContext.requiredPlayerIds.");
        }

        if (inputContextState.submittedPlayerIds.Contains(requiredPlayerId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires actorPlayerId to not have already submitted.");
        }

        if (!inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(
                requiredPlayerId.Value,
                out var choiceKeysForPlayer) ||
            !choiceKeysForPlayer.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires choiceKey to be one of currentInputContext choices for actorPlayerId.");
        }

        if (submitInputChoiceActionRequest.choiceKey.StartsWith(
                A002ArrivalChoiceKeySummonCardPrefix,
                StringComparison.Ordinal))
        {
            ensureA002YuyukoSummonCardChoiceStillLegal(
                gameState,
                requiredPlayerId,
                submitInputChoiceActionRequest.choiceKey);
        }
    }

    private void executeA002ArrivalChoicesInTurnOrder(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        long eventId)
    {
        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        foreach (var playerId in seatOrderPlayerIds)
        {
            if (!inputContextState.requiredPlayerIds.Contains(playerId))
            {
                continue;
            }

            if (!actionChainState.localState.TryGetValue(
                    createA002ArrivalSelectedChoiceLocalStateKey(playerId),
                    out var selectedChoiceKey))
            {
                throw new InvalidOperationException("A002 anomaly arrival continuation requires every required player selected choice to be recorded before resolving.");
            }

            if (string.Equals(selectedChoiceKey, A002ArrivalChoiceKeySakuraDecline, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(selectedChoiceKey, A002ArrivalChoiceKeySakuraAccept, StringComparison.Ordinal))
            {
                summonTopSakuraCakeToPlayerDiscardIfAvailable(
                    gameState,
                    actionChainState,
                    playerId,
                    eventId);
                continue;
            }

            if (selectedChoiceKey.StartsWith(A002ArrivalChoiceKeySummonCardPrefix, StringComparison.Ordinal))
            {
                summonSelectedSummonZoneCardToPlayerDiscardAndRefillIfStillAvailable(
                    gameState,
                    actionChainState,
                    playerId,
                    selectedChoiceKey,
                    eventId);
                continue;
            }

            throw new InvalidOperationException("A002 anomaly arrival continuation selected choice is not supported.");
        }
    }

    private void summonTopSakuraCakeToPlayerDiscardIfAvailable(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long eventId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected player to exist.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.sakuraCakeDeckZoneId, out var sakuraCakeDeckZoneState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires sakuraCakeDeck to exist.");
        }

        if (!gameState.zones.ContainsKey(playerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires player discardZone to exist.");
        }

        if (sakuraCakeDeckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topSakuraCakeCardInstanceId = sakuraCakeDeckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topSakuraCakeCardInstanceId, out var topSakuraCakeCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires sakuraCakeDeck cardInstanceIds to exist in gameState.cardInstances.");
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            topSakuraCakeCardInstance,
            playerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(movedEvent);
    }

    private void summonSelectedSummonZoneCardToPlayerDiscardAndRefillIfStillAvailable(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        string selectedChoiceKey,
        long eventId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected player to exist.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires summonZone to exist.");
        }

        if (!gameState.zones.ContainsKey(playerState.discardZoneId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires player discardZone to exist.");
        }

        var selectedCardInstanceId = parseA002ArrivalSummonCardChoiceKey(selectedChoiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected summonZone cardInstanceId to exist.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId ||
            !summonZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
        {
            return;
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            playerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(movedEvent);

        refillSummonZoneFromPublicTreasureDeckIfAvailable(
            gameState,
            actionChainState,
            eventId);
    }

    private void refillSummonZoneFromPublicTreasureDeckIfAvailable(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires publicTreasureDeck to exist.");
        }

        if (!gameState.zones.ContainsKey(gameState.publicState.summonZoneId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires summonZone to exist.");
        }

        if (publicTreasureDeckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topPublicTreasureCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topPublicTreasureCardInstanceId, out var topPublicTreasureCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires publicTreasureDeck cardInstanceIds to exist in gameState.cardInstances.");
        }

        var refillEvent = zoneMovementService.moveCard(
            gameState,
            topPublicTreasureCardInstance,
            gameState.publicState.summonZoneId,
            CardMoveReason.reveal,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(refillEvent);
    }

    private void ensureA002YuyukoSummonCardChoiceStillLegal(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string choiceKey)
    {
        if (!isA002YuyukoPlayer(gameState, playerId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires summonCard choice to be submitted by C018 Yuyuko player.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires summonZone to exist.");
        }

        var selectedCardInstanceId = parseA002ArrivalSummonCardChoiceKey(choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected summonZone cardInstanceId to exist.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId ||
            !summonZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected card to still be in summonZone.");
        }

        var treasureDefinition = TreasureDefinitionRepository.resolveByDefinitionId(selectedCardInstance.definitionId);
        if (!treasureDefinition.summonSigilCost.HasValue ||
            treasureDefinition.summonSigilCost.Value > A002YuyukoSummonCostMax)
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation requires selected summonZone card summonSigilCost to be no more than 5.");
        }
    }

    private static bool isA002YuyukoPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState) ||
            !playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(
                playerState.activeCharacterInstanceId.Value,
                out var activeCharacterInstance))
        {
            return false;
        }

        return string.Equals(activeCharacterInstance.definitionId, A002YuyukoDefinitionId, StringComparison.Ordinal);
    }

    public void ensureValidA006ArrivalHumanDefenseDiscardChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A006ArrivalHumanDefenseDiscardContextKey)
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A006:arrivalHumanDefenseDiscardFlow.");
        }

        if (!inputContextState.requiredPlayerIds.Contains(submitInputChoiceActionRequest.actorPlayerId))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires actorPlayerId to be a required player.");
        }

        if (inputContextState.submittedPlayerIds.Contains(submitInputChoiceActionRequest.actorPlayerId))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation does not accept duplicate player submissions.");
        }

        var requiredPlayerId = submitInputChoiceActionRequest.actorPlayerId;
        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredPlayerState.fieldZoneId, out var requiredPlayerFieldZoneState))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires required player fieldZoneId to exist in gameState.zones.");
        }

        if (submitInputChoiceActionRequest.choiceKeys.Count != 0 ||
            !inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(
                requiredPlayerId.Value,
                out var allowedChoiceKeys) ||
            !allowedChoiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires one allowed fieldCard choice.");
        }

        var selectedCardInstanceId = parseA006ArrivalHumanDefenseDiscardChoiceKey(
            submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.ownerPlayerId != requiredPlayerId)
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires selected card to be owned by currentInputContext.requiredPlayerId.");
        }

        if (selectedCardInstance.zoneId != requiredPlayerState.fieldZoneId ||
            !requiredPlayerFieldZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires selected card to still be in required player field zone.");
        }

        if (!selectedCardInstance.isDefensePlacedOnField)
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation requires selected card to be defense-placed on field.");
        }
    }

    public AnomalyArrivalInputAdvanceResult continueA006ArrivalHumanDefenseDiscardFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string pendingContinuationKey)
    {
        ensureValidA006ArrivalHumanDefenseDiscardChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        inputContextState.submittedPlayerIds.Add(submitInputChoiceActionRequest.actorPlayerId);
        actionChainState.localState[
            A006ArrivalHumanDefenseDiscardContextKey + ":selected:" +
            submitInputChoiceActionRequest.actorPlayerId.Value] = submitInputChoiceActionRequest.choiceKey;

        if (inputContextState.submittedPlayerIds.Count < inputContextState.requiredPlayerIds.Count)
        {
            return AnomalyArrivalInputAdvanceResult.createPending();
        }

        foreach (var requiredPlayerId in inputContextState.requiredPlayerIds.OrderBy(playerId => playerId.Value))
        {
            if (!actionChainState.localState.TryGetValue(
                    A006ArrivalHumanDefenseDiscardContextKey + ":selected:" + requiredPlayerId.Value,
                    out var selectedChoiceKey))
            {
                throw new InvalidOperationException("A006 anomaly arrival requires every required player's choice.");
            }

            var requiredPlayerState = gameState.players[requiredPlayerId];
            var selectedCardInstanceId = parseA006ArrivalHumanDefenseDiscardChoiceKey(selectedChoiceKey);
            var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                requiredPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId));
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
        actionChainState.pendingContinuationKey = null;

        executeDrawOneForNonHumanActiveCharacterPlayers(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);
        return AnomalyArrivalInputAdvanceResult.createCompleted();
    }

    public List<string> createA003ArrivalSelectOpponentShackleChoiceKeys(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A003 anomaly arrival input requires actorPlayerId to exist in gameState.players.");
        }

        var choiceKeys = new List<string>();
        if (gameState.matchMeta is not null && gameState.matchMeta.seatOrder.Count > 0)
        {
            foreach (var seatPlayerId in gameState.matchMeta.seatOrder)
            {
                if (!gameState.players.TryGetValue(seatPlayerId, out var seatPlayerState))
                {
                    continue;
                }

                if (seatPlayerState.teamId == actorPlayerState.teamId)
                {
                    continue;
                }

                if (!seatPlayerState.activeCharacterInstanceId.HasValue)
                {
                    continue;
                }

                if (!gameState.characterInstances.ContainsKey(seatPlayerState.activeCharacterInstanceId.Value))
                {
                    continue;
                }

                choiceKeys.Add(createA003ArrivalSelectOpponentShackleChoiceKey(seatPlayerId));
            }

            return choiceKeys;
        }

        foreach (var playerStateEntry in gameState.players)
        {
            var candidatePlayerId = playerStateEntry.Key;
            var candidatePlayerState = playerStateEntry.Value;
            if (candidatePlayerState.teamId == actorPlayerState.teamId)
            {
                continue;
            }

            if (!candidatePlayerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            if (!gameState.characterInstances.ContainsKey(candidatePlayerState.activeCharacterInstanceId.Value))
            {
                continue;
            }

            choiceKeys.Add(createA003ArrivalSelectOpponentShackleChoiceKey(candidatePlayerId));
        }

        return choiceKeys;
    }

    public bool tryOpenA003ArrivalSelectOpponentShackleInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        var choiceKeys = createA003ArrivalSelectOpponentShackleChoiceKeys(gameState, actorPlayerId);
        if (choiceKeys.Count == 0)
        {
            return false;
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A003 anomaly arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A003ArrivalSelectOpponentShackleInputTypeKey,
            contextKey = A003ArrivalSelectOpponentShackleContextKey,
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
        return true;
    }

    public void ensureValidA003ArrivalSelectOpponentShackleChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A003ArrivalSelectOpponentShackleContextKey)
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A003:arrivalSelectOpponentShackle.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (!gameState.players.TryGetValue(inputContextState.requiredPlayerId.Value, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires requiredPlayerId to exist in gameState.players.");
        }

        var selectedOpponentPlayerId = parseA003ArrivalSelectOpponentShackleChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.players.TryGetValue(selectedOpponentPlayerId, out var selectedOpponentPlayerState))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires selected opponent playerId to exist in gameState.players.");
        }

        if (selectedOpponentPlayerState.teamId == requiredPlayerState.teamId)
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires selected opponent to be on a different team.");
        }

        if (!selectedOpponentPlayerState.activeCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires selected opponent activeCharacterInstanceId.");
        }

        if (!gameState.characterInstances.ContainsKey(selectedOpponentPlayerState.activeCharacterInstanceId.Value))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation requires selected opponent activeCharacterInstanceId to exist in gameState.characterInstances.");
        }
    }

    public void executeA003ArrivalApplyShackleToSelectedOpponent(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var selectedOpponentPlayerId = parseA003ArrivalSelectOpponentShackleChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedOpponentPlayerState = gameState.players[selectedOpponentPlayerId];
        var selectedOpponentCharacterInstanceId = selectedOpponentPlayerState.activeCharacterInstanceId!.Value;

        StatusRuntime.applyStatus(gameState, new StatusInstance
        {
            statusKey = "Shackle",
            applierPlayerId = actorPlayerId,
            targetCharacterInstanceId = selectedOpponentCharacterInstanceId,
            stackCount = 1,
        });
    }

    public List<string> createA005ArrivalDirectSummonChoiceKeys(RuleCore.GameState.GameState gameState)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A005 anomaly arrival input requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("A005 anomaly arrival input requires gameState.publicState.summonZoneId to exist in gameState.zones.");
        }

        var choiceKeys = new List<string>(summonZoneState.cardInstanceIds.Count + 1)
        {
            A005ArrivalDirectSummonChoiceKeyDecline,
        };
        foreach (var summonCardInstanceId in summonZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.ContainsKey(summonCardInstanceId))
            {
                throw new InvalidOperationException("A005 anomaly arrival input requires summon zone cardInstanceIds to exist in gameState.cardInstances.");
            }

            choiceKeys.Add(createA005ArrivalDirectSummonChoiceKey(summonCardInstanceId));
        }

        return choiceKeys;
    }

    public bool tryOpenA005ArrivalDirectSummonInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        var choiceKeys = createA005ArrivalDirectSummonChoiceKeys(gameState);
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A005 anomaly arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A005ArrivalDirectSummonInputTypeKey,
            contextKey = A005ArrivalDirectSummonContextKey,
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
        return true;
    }

    public void ensureValidA005ArrivalDirectSummonChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A005ArrivalDirectSummonContextKey)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A005:arrivalDirectSummonFromSummonZone.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out _))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.publicState.summonZoneId to exist in gameState.zones.");
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                A005ArrivalDirectSummonChoiceKeyDecline,
                StringComparison.Ordinal))
        {
            return;
        }

        var selectedCardInstanceId = parseA005ArrivalDirectSummonChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires selected cardInstanceId to exist in gameState.cardInstances.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires selected card to still be in summon zone.");
        }
    }

    public void executeA005ArrivalSelectedSummonCardToActorDiscardAndRefill(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires actor player's discardZoneId to exist in gameState.zones.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.publicState.publicTreasureDeckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(gameState.publicState.summonZoneId))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires gameState.publicState.summonZoneId to exist in gameState.zones.");
        }

        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                A005ArrivalDirectSummonChoiceKeyDecline,
                StringComparison.Ordinal))
        {
            return;
        }

        var selectedCardInstanceId = parseA005ArrivalDirectSummonChoiceKey(submitInputChoiceActionRequest.choiceKey);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
        var summonedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(summonedEvent);

        if (publicTreasureDeckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topPublicTreasureCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topPublicTreasureCardInstanceId, out var topPublicTreasureCardInstance))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation requires public treasure deck cardInstanceIds to exist in gameState.cardInstances.");
        }

        var refillEvent = zoneMovementService.moveCard(
            gameState,
            topPublicTreasureCardInstance,
            gameState.publicState.summonZoneId,
            CardMoveReason.reveal,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(refillEvent);
    }

    public bool tryOpenA007ArrivalOptionalBanishInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        string pendingContinuationKey,
        long eventId)
    {
        if (gameState.publicState is null || !gameState.zones.ContainsKey(gameState.publicState.gapZoneId))
        {
            throw new InvalidOperationException("A007 anomaly arrival input requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        return tryOpenA007ArrivalNextPlayerStageInputContext(
            gameState,
            actionChainState,
            seatOrderPlayerIds,
            0,
            pendingContinuationKey,
            eventId);
    }

    public static bool isValidA007ArrivalOptionalBanishChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        return inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    public void ensureValidA007ArrivalOptionalBanishChoiceForContinuation(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (inputContextState.contextKey != A007ArrivalContextKey)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.contextKey to be anomaly:A007:arrivalOptionalBanishFlow.");
        }

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (submitInputChoiceActionRequest.actorPlayerId != inputContextState.requiredPlayerId.Value)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires submitInputChoiceActionRequest.actorPlayerId to equal currentInputContext.requiredPlayerId.");
        }

        if (!isA007ArrivalStageInputType(inputContextState.inputTypeKey))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.inputTypeKey to be a supported A007 arrival stage.");
        }

        if (!isValidA007ArrivalOptionalBanishChoiceRequest(inputContextState, submitInputChoiceActionRequest))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        if (!gameState.players.TryGetValue(inputContextState.requiredPlayerId.Value, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.handZoneId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires required player handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(requiredPlayerState.discardZoneId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        if (gameState.publicState is null || !gameState.zones.ContainsKey(gameState.publicState.gapZoneId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires gameState.publicState.gapZoneId to exist in gameState.zones.");
        }

        var stageDefinition = resolveA007ArrivalOptionalStepDefinition(inputContextState.inputTypeKey);
        if (string.Equals(stageDefinition.stepKey, "optionalHandBanish", StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKey == A007ArrivalChoiceKeyDecline)
            {
                return;
            }

            var selectedCardInstanceId = parseA007ArrivalHandChoiceKey(submitInputChoiceActionRequest.choiceKey);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires selected hand cardInstanceId to exist in gameState.cardInstances.");
            }

            if (selectedCardInstance.zoneId != requiredPlayerState.handZoneId)
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires selected hand card to still be in required player hand zone.");
            }

            return;
        }

        if (string.Equals(stageDefinition.stepKey, "optionalDiscardBanishDecision", StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKey != A007ArrivalChoiceKeyAccept &&
                submitInputChoiceActionRequest.choiceKey != A007ArrivalChoiceKeyDecline)
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires discard optional decision to be accept or decline.");
            }

            if (submitInputChoiceActionRequest.choiceKey == A007ArrivalChoiceKeyAccept &&
                gameState.zones[requiredPlayerState.discardZoneId].cardInstanceIds.Count == 0)
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires required player discard zone to contain at least one card when accepting optional discard banish.");
            }

            return;
        }

        if (string.Equals(stageDefinition.stepKey, "optionalDiscardBanishSelect", StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseA007ArrivalDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires selected discard cardInstanceId to exist in gameState.cardInstances.");
            }

            if (selectedCardInstance.zoneId != requiredPlayerState.discardZoneId)
            {
                throw new InvalidOperationException("A007 anomaly arrival continuation requires selected discard card to still be in required player discard zone.");
            }

            return;
        }

        throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.inputTypeKey to be a supported A007 arrival stage.");
    }

    public AnomalyArrivalInputAdvanceResult continueA007ArrivalOptionalBanishFlowContinuation(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string pendingContinuationKey)
    {
        ensureValidA007ArrivalOptionalBanishChoiceForContinuation(
            gameState,
            inputContextState,
            submitInputChoiceActionRequest);

        if (!inputContextState.requiredPlayerId.HasValue)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.requiredPlayerId.");
        }

        if (!gameState.players.TryGetValue(inputContextState.requiredPlayerId.Value, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires requiredPlayerId to exist in gameState.players.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires gameState.publicState.");
        }

        var requiredPlayerId = inputContextState.requiredPlayerId.Value;
        var stageDefinition = resolveA007ArrivalOptionalStepDefinition(inputContextState.inputTypeKey);
        if (string.Equals(stageDefinition.stepKey, "optionalHandBanish", StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKey != A007ArrivalChoiceKeyDecline)
            {
                var selectedHandCardInstanceId = parseA007ArrivalHandChoiceKey(submitInputChoiceActionRequest.choiceKey);
                var selectedHandCardInstance = gameState.cardInstances[selectedHandCardInstanceId];
                var movedEvent = zoneMovementService.moveCard(
                    gameState,
                    selectedHandCardInstance,
                    gameState.publicState.gapZoneId,
                    CardMoveReason.banish,
                    actionChainState.actionChainId,
                    submitInputChoiceActionRequest.requestId);
                actionChainState.producedEvents.Add(movedEvent);
            }

            if (tryOpenA007ExtraOptionalDiscardDecisionInputContextForPlayer(
                    gameState,
                    actionChainState,
                    requiredPlayerId,
                    pendingContinuationKey,
                    submitInputChoiceActionRequest.requestId))
            {
                return AnomalyArrivalInputAdvanceResult.createPending();
            }
        }
        else if (string.Equals(stageDefinition.stepKey, "optionalDiscardBanishDecision", StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKey == A007ArrivalChoiceKeyAccept)
            {
                openA007ArrivalInputContext(
                    gameState,
                    actionChainState,
                    requiredPlayerId,
                    A007ArrivalInputTypeKeyOptionalDiscardBanishSelect,
                    createA007ArrivalDiscardChoiceKeys(gameState, requiredPlayerState.discardZoneId),
                    pendingContinuationKey,
                    submitInputChoiceActionRequest.requestId);
                return AnomalyArrivalInputAdvanceResult.createPending();
            }
        }
        else if (string.Equals(stageDefinition.stepKey, "optionalDiscardBanishSelect", StringComparison.Ordinal))
        {
            var selectedDiscardCardInstanceId = parseA007ArrivalDiscardChoiceKey(submitInputChoiceActionRequest.choiceKey);
            var selectedDiscardCardInstance = gameState.cardInstances[selectedDiscardCardInstanceId];
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                selectedDiscardCardInstance,
                gameState.publicState.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(movedEvent);
        }
        else
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.inputTypeKey to be a supported A007 arrival stage.");
        }

        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        var requiredPlayerIndex = indexOfPlayerId(seatOrderPlayerIds, requiredPlayerId);
        if (requiredPlayerIndex < 0)
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires requiredPlayerId to exist in seatOrder player list.");
        }

        if (tryOpenA007ArrivalNextPlayerStageInputContext(
                gameState,
                actionChainState,
                seatOrderPlayerIds,
                requiredPlayerIndex + 1,
                pendingContinuationKey,
                submitInputChoiceActionRequest.requestId))
        {
            return AnomalyArrivalInputAdvanceResult.createPending();
        }

        return AnomalyArrivalInputAdvanceResult.createCompleted();
    }

    private bool tryOpenA007ArrivalNextPlayerStageInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> seatOrderPlayerIds,
        int startPlayerIndex,
        string pendingContinuationKey,
        long eventId)
    {
        for (var playerIndex = startPlayerIndex; playerIndex < seatOrderPlayerIds.Count; playerIndex++)
        {
            var seatPlayerId = seatOrderPlayerIds[playerIndex];
            if (!gameState.players.TryGetValue(seatPlayerId, out var seatPlayerState))
            {
                throw new InvalidOperationException("A007 anomaly arrival input requires seatOrder players to exist in gameState.players.");
            }

            if (tryOpenA007HandOptionalInputContextForPlayer(
                    gameState,
                    actionChainState,
                    seatPlayerId,
                    seatPlayerState.handZoneId,
                    pendingContinuationKey,
                    eventId))
            {
                return true;
            }

            if (tryOpenA007ExtraOptionalDiscardDecisionInputContextForPlayer(
                    gameState,
                    actionChainState,
                    seatPlayerId,
                    pendingContinuationKey,
                    eventId))
            {
                return true;
            }
        }

        return false;
    }

    private bool tryOpenA007HandOptionalInputContextForPlayer(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        ZoneId requiredPlayerHandZoneId,
        string pendingContinuationKey,
        long eventId)
    {
        if (!gameState.zones.TryGetValue(requiredPlayerHandZoneId, out var requiredPlayerHandZoneState))
        {
            throw new InvalidOperationException("A007 anomaly arrival input requires required player handZoneId to exist in gameState.zones.");
        }

        if (requiredPlayerHandZoneState.cardInstanceIds.Count == 0)
        {
            return false;
        }

        openA007ArrivalInputContext(
            gameState,
            actionChainState,
            requiredPlayerId,
            A007ArrivalInputTypeKeyOptionalHandBanish,
            createA007ArrivalHandChoiceKeys(requiredPlayerHandZoneState.cardInstanceIds),
            pendingContinuationKey,
            eventId);
        return true;
    }

    private bool tryOpenA007ExtraOptionalDiscardDecisionInputContextForPlayer(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        if (!isA007ActiveCharacter(gameState, requiredPlayerId))
        {
            return false;
        }

        var requiredPlayerState = gameState.players[requiredPlayerId];
        if (!gameState.zones.TryGetValue(requiredPlayerState.discardZoneId, out var requiredPlayerDiscardZoneState))
        {
            throw new InvalidOperationException("A007 anomaly arrival input requires required player discardZoneId to exist in gameState.zones.");
        }

        if (requiredPlayerDiscardZoneState.cardInstanceIds.Count == 0)
        {
            return false;
        }

        openA007ArrivalInputContext(
            gameState,
            actionChainState,
            requiredPlayerId,
            A007ArrivalInputTypeKeyOptionalDiscardBanishDecision,
            new List<string>
            {
                A007ArrivalChoiceKeyAccept,
                A007ArrivalChoiceKeyDecline,
            },
            pendingContinuationKey,
            eventId);
        return true;
    }

    private static bool isA007ActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            return false;
        }

        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
        {
            return false;
        }

        return string.Equals(activeCharacterInstance.definitionId, A007RinDefinitionId, StringComparison.Ordinal);
    }

    private void openA007ArrivalInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        List<string> choiceKeys,
        string pendingContinuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A007 anomaly arrival input requires gameState.currentInputContext to be null before opening.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = A007ArrivalContextKey,
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

    private static bool isA007ArrivalStageInputType(string? inputTypeKey)
    {
        return tryResolveA007ArrivalOptionalStepDefinition(inputTypeKey, out _);
    }

    private static bool tryResolveA007ArrivalOptionalStepDefinition(
        string? inputTypeKey,
        out A007ArrivalOptionalStepDefinition? stepDefinition)
    {
        if (string.Equals(inputTypeKey, A007ArrivalInputTypeKeyOptionalHandBanish, StringComparison.Ordinal))
        {
            stepDefinition = new A007ArrivalOptionalStepDefinition("optionalHandBanish", A007ArrivalInputTypeKeyOptionalHandBanish);
            return true;
        }

        if (string.Equals(inputTypeKey, A007ArrivalInputTypeKeyOptionalDiscardBanishDecision, StringComparison.Ordinal))
        {
            stepDefinition = new A007ArrivalOptionalStepDefinition("optionalDiscardBanishDecision", A007ArrivalInputTypeKeyOptionalDiscardBanishDecision);
            return true;
        }

        if (string.Equals(inputTypeKey, A007ArrivalInputTypeKeyOptionalDiscardBanishSelect, StringComparison.Ordinal))
        {
            stepDefinition = new A007ArrivalOptionalStepDefinition("optionalDiscardBanishSelect", A007ArrivalInputTypeKeyOptionalDiscardBanishSelect);
            return true;
        }

        stepDefinition = null;
        return false;
    }

    private static A007ArrivalOptionalStepDefinition resolveA007ArrivalOptionalStepDefinition(string? inputTypeKey)
    {
        if (!tryResolveA007ArrivalOptionalStepDefinition(inputTypeKey, out var stepDefinition))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires currentInputContext.inputTypeKey to be a supported A007 arrival stage.");
        }

        return stepDefinition!;
    }

    private static List<PlayerId> resolveA007ArrivalSeatOrderPlayerIds(RuleCore.GameState.GameState gameState)
    {
        var seatOrderPlayerIds = new List<PlayerId>();
        var addedPlayerIds = new HashSet<PlayerId>();
        if (gameState.matchMeta is not null)
        {
            foreach (var seatPlayerId in gameState.matchMeta.seatOrder)
            {
                if (!gameState.players.ContainsKey(seatPlayerId))
                {
                    continue;
                }

                if (addedPlayerIds.Add(seatPlayerId))
                {
                    seatOrderPlayerIds.Add(seatPlayerId);
                }
            }
        }

        if (seatOrderPlayerIds.Count > 0)
        {
            return seatOrderPlayerIds;
        }

        foreach (var playerId in gameState.players.Keys)
        {
            if (addedPlayerIds.Add(playerId))
            {
                seatOrderPlayerIds.Add(playerId);
            }
        }

        seatOrderPlayerIds.Sort((leftPlayerId, rightPlayerId) => leftPlayerId.Value.CompareTo(rightPlayerId.Value));
        return seatOrderPlayerIds;
    }

    private static int indexOfPlayerId(IReadOnlyList<PlayerId> playerIds, PlayerId targetPlayerId)
    {
        for (var playerIndex = 0; playerIndex < playerIds.Count; playerIndex++)
        {
            if (playerIds[playerIndex] == targetPlayerId)
            {
                return playerIndex;
            }
        }

        return -1;
    }

    private void openA001ArrivalHumanDiscardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string pendingContinuationKey,
        long eventId)
    {
        if (!gameState.players.TryGetValue(requiredPlayerId, out var requiredPlayerState))
        {
            throw new InvalidOperationException("A001 anomaly arrival input requires requiredPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(requiredPlayerState.handZoneId, out var requiredPlayerHandZoneState))
        {
            throw new InvalidOperationException("A001 anomaly arrival input requires required player handZoneId to exist in gameState.zones.");
        }

        var choiceKeys = createA001ArrivalHumanDiscardChoiceKeys(requiredPlayerHandZoneState.cardInstanceIds);
        if (choiceKeys.Count == 0)
        {
            throw new InvalidOperationException("A001 anomaly arrival input requires at least one hand card choice for required player.");
        }

        openArrivalInputContext(
            gameState,
            actionChainState,
            requiredPlayerId,
            A001ArrivalHumanDiscardInputTypeKey,
            A001ArrivalHumanDiscardContextKey,
            choiceKeys,
            pendingContinuationKey,
            eventId,
            "A001");
    }

    private void openA006ArrivalHumanDefenseDiscardInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        string pendingContinuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("A006 anomaly arrival input requires no active inputContext.");
        }

        var inputContextState = new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A006ArrivalHumanDefenseDiscardInputTypeKey,
            contextKey = A006ArrivalHumanDefenseDiscardContextKey,
        };
        foreach (var requiredPlayerId in requiredPlayerIds)
        {
            var requiredPlayerState = gameState.players[requiredPlayerId];
            var requiredPlayerFieldZoneState = gameState.zones[requiredPlayerState.fieldZoneId];
            var choiceKeys = createA006ArrivalHumanDefenseDiscardChoiceKeys(
                gameState,
                requiredPlayerFieldZoneState.cardInstanceIds);
            if (choiceKeys.Count == 0)
            {
                throw new InvalidOperationException("A006 anomaly arrival input requires a defense card for every required player.");
            }

            inputContextState.requiredPlayerIds.Add(requiredPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[requiredPlayerId.Value] = choiceKeys;
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = pendingContinuationKey;
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

    private void openArrivalInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        List<string> choiceKeys,
        string pendingContinuationKey,
        long eventId,
        string anomalyLabel)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException(anomalyLabel + " anomaly arrival input requires gameState.currentInputContext to be null before opening.");
        }

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

    private static List<PlayerId> resolveA001HumanDiscardPlayerIds(RuleCore.GameState.GameState gameState)
    {
        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        var playerIds = new List<PlayerId>();
        foreach (var seatPlayerId in seatOrderPlayerIds)
        {
            if (!isActiveCharacterWithRaceTag(gameState, seatPlayerId, "human"))
            {
                continue;
            }

            var playerState = gameState.players[seatPlayerId];
            if (!gameState.zones.TryGetValue(playerState.handZoneId, out var handZoneState))
            {
                throw new InvalidOperationException("A001 anomaly arrival input requires human player handZoneId to exist in gameState.zones.");
            }

            if (handZoneState.cardInstanceIds.Count > 0)
            {
                playerIds.Add(seatPlayerId);
            }
        }

        return playerIds;
    }

    private static List<PlayerId> resolveA006HumanDefenseDiscardPlayerIds(RuleCore.GameState.GameState gameState)
    {
        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        var playerIds = new List<PlayerId>();
        foreach (var seatPlayerId in seatOrderPlayerIds)
        {
            if (!isActiveCharacterWithRaceTag(gameState, seatPlayerId, "human"))
            {
                continue;
            }

            var playerState = gameState.players[seatPlayerId];
            if (!gameState.zones.TryGetValue(playerState.fieldZoneId, out var fieldZoneState))
            {
                throw new InvalidOperationException("A006 anomaly arrival input requires human player fieldZoneId to exist in gameState.zones.");
            }

            var hasDefensePlacedCard = false;
            foreach (var fieldCardInstanceId in fieldZoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(fieldCardInstanceId, out var fieldCardInstance))
                {
                    throw new InvalidOperationException("A006 anomaly arrival input requires fieldZone cardInstanceIds to exist in gameState.cardInstances.");
                }

                if (fieldCardInstance.isDefensePlacedOnField)
                {
                    hasDefensePlacedCard = true;
                    break;
                }
            }

            if (hasDefensePlacedCard)
            {
                playerIds.Add(seatPlayerId);
            }
        }

        return playerIds;
    }

    private static bool isActiveCharacterWithRaceTag(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string expectedRaceTag)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            return false;
        }

        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
        {
            return false;
        }

        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(activeCharacterInstance.definitionId);
        foreach (var raceTag in characterDefinition.raceTags)
        {
            if (string.Equals(raceTag, expectedRaceTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void executeDrawOneForNonHumanActiveCharacterPlayers(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId)
    {
        var seatOrderPlayerIds = resolveA007ArrivalSeatOrderPlayerIds(gameState);
        foreach (var seatPlayerId in seatOrderPlayerIds)
        {
            if (!isActiveCharacterWithRaceTag(gameState, seatPlayerId, "nonHuman"))
            {
                continue;
            }

            tryDrawOneForPlayerWithDeckRecoveryIfNeeded(
                gameState,
                actionChainState,
                seatPlayerId,
                requestId);
        }
    }

    private void tryDrawOneForPlayerWithDeckRecoveryIfNeeded(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long requestId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            throw new InvalidOperationException("Anomaly arrival draw helper requires playerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("Anomaly arrival draw helper requires player deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("Anomaly arrival draw helper requires player discardZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(playerState.handZoneId))
        {
            throw new InvalidOperationException("Anomaly arrival draw helper requires player handZoneId to exist in gameState.zones.");
        }

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardInstanceIds = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
            foreach (var discardCardInstanceId in discardCardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(discardCardInstanceId, out var discardedCardInstance))
                {
                    throw new InvalidOperationException("Anomaly arrival draw helper requires discard cardInstanceIds to exist in gameState.cardInstances.");
                }

                var recoverEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    playerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    requestId);
                actionChainState.producedEvents.Add(recoverEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topDeckCardInstanceId = deckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topDeckCardInstanceId, out var topDeckCardInstance))
        {
            throw new InvalidOperationException("Anomaly arrival draw helper requires deck cardInstanceIds to exist in gameState.cardInstances.");
        }

        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topDeckCardInstance,
            playerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(drawEvent);
    }

    private static void executeA001RemiliaFullHeal(RuleCore.GameState.GameState gameState)
    {
        foreach (var playerStateEntry in gameState.players)
        {
            var playerState = playerStateEntry.Value;
            if (!playerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
            {
                continue;
            }

            if (string.Equals(activeCharacterInstance.definitionId, A001RemiliaDefinitionId, StringComparison.Ordinal))
            {
                activeCharacterInstance.currentHp = activeCharacterInstance.maxHp;
            }
        }
    }

    private static List<string> createA001ArrivalHumanDiscardChoiceKeys(List<CardInstanceId> handCardInstanceIds)
    {
        var choiceKeys = new List<string>(handCardInstanceIds.Count);
        foreach (var handCardInstanceId in handCardInstanceIds)
        {
            choiceKeys.Add(createA001ArrivalHumanDiscardChoiceKey(handCardInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createA006ArrivalHumanDefenseDiscardChoiceKeys(
        RuleCore.GameState.GameState gameState,
        List<CardInstanceId> fieldCardInstanceIds)
    {
        var choiceKeys = new List<string>();
        foreach (var fieldCardInstanceId in fieldCardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(fieldCardInstanceId, out var fieldCardInstance))
            {
                throw new InvalidOperationException("A006 anomaly arrival input requires fieldZone cardInstanceIds to exist in gameState.cardInstances.");
            }

            if (!fieldCardInstance.isDefensePlacedOnField)
            {
                continue;
            }

            choiceKeys.Add(createA006ArrivalHumanDefenseDiscardChoiceKey(fieldCardInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createA007ArrivalHandChoiceKeys(List<CardInstanceId> handCardInstanceIds)
    {
        var choiceKeys = new List<string>(handCardInstanceIds.Count + 1)
        {
            A007ArrivalChoiceKeyDecline,
        };
        foreach (var handCardInstanceId in handCardInstanceIds)
        {
            choiceKeys.Add(createA007ArrivalHandChoiceKey(handCardInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createA007ArrivalDiscardChoiceKeys(
        RuleCore.GameState.GameState gameState,
        ZoneId discardZoneId)
    {
        if (!gameState.zones.TryGetValue(discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation requires required player discardZoneId to exist in gameState.zones.");
        }

        var choiceKeys = new List<string>(discardZoneState.cardInstanceIds.Count);
        foreach (var discardCardInstanceId in discardZoneState.cardInstanceIds)
        {
            choiceKeys.Add(createA007ArrivalDiscardChoiceKey(discardCardInstanceId));
        }

        return choiceKeys;
    }

    private static string createA005ArrivalDirectSummonChoiceKey(CardInstanceId cardInstanceId)
    {
        return A005ArrivalDirectSummonChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createA002ArrivalSummonCardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A002ArrivalChoiceKeySummonCardPrefix + cardInstanceId.Value;
    }

    private static string createA002ArrivalSelectedChoiceLocalStateKey(PlayerId playerId)
    {
        return A002ArrivalSelectedChoiceLocalStatePrefix + playerId.Value;
    }

    private static string createA001ArrivalHumanDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A001ArrivalHumanDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createA006ArrivalHumanDefenseDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A006ArrivalHumanDefenseDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createA007ArrivalHandChoiceKey(CardInstanceId cardInstanceId)
    {
        return A007ArrivalChoiceKeyHandCardPrefix + cardInstanceId.Value;
    }

    private static string createA007ArrivalDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return A007ArrivalChoiceKeyDiscardCardPrefix + cardInstanceId.Value;
    }

    private static string createA003ArrivalSelectOpponentShackleChoiceKey(PlayerId playerId)
    {
        return A003ArrivalSelectOpponentShackleChoiceKeyPrefix + playerId.Value;
    }

    private static PlayerId parseA003ArrivalSelectOpponentShackleChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A003ArrivalSelectOpponentShackleChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation choiceKey must start with opponentPlayer: prefix.");
        }

        var playerIdSegment = choiceKey.Substring(A003ArrivalSelectOpponentShackleChoiceKeyPrefix.Length);
        if (!long.TryParse(playerIdSegment, out var playerNumericId))
        {
            throw new InvalidOperationException("A003 anomaly arrival continuation choiceKey must encode a valid PlayerId numeric value.");
        }

        return new PlayerId(playerNumericId);
    }

    private static CardInstanceId parseA005ArrivalDirectSummonChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A005ArrivalDirectSummonChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation choiceKey must start with summonCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A005ArrivalDirectSummonChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A005 anomaly arrival continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA002ArrivalSummonCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A002ArrivalChoiceKeySummonCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation choiceKey must start with summonCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A002ArrivalChoiceKeySummonCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A002 anomaly arrival continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA001ArrivalHumanDiscardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A001ArrivalHumanDiscardChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation choiceKey must start with handCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A001ArrivalHumanDiscardChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A001 anomaly arrival continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA006ArrivalHumanDefenseDiscardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A006ArrivalHumanDefenseDiscardChoiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation choiceKey must start with fieldCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A006ArrivalHumanDefenseDiscardChoiceKeyPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A006 anomaly arrival continuation choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA007ArrivalHandChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A007ArrivalChoiceKeyHandCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation hand choiceKey must start with handCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A007ArrivalChoiceKeyHandCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation hand choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static CardInstanceId parseA007ArrivalDiscardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(A007ArrivalChoiceKeyDiscardCardPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation discard choiceKey must start with discardCard: prefix.");
        }

        var cardIdSegment = choiceKey.Substring(A007ArrivalChoiceKeyDiscardCardPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId))
        {
            throw new InvalidOperationException("A007 anomaly arrival continuation discard choiceKey must encode a valid CardInstanceId numeric value.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private sealed class A007ArrivalOptionalStepDefinition
    {
        public A007ArrivalOptionalStepDefinition(string stepKey, string inputTypeKey)
        {
            this.stepKey = stepKey;
            this.inputTypeKey = inputTypeKey;
        }

        public string stepKey { get; }
        public string inputTypeKey { get; }
    }
}

public sealed class AnomalyArrivalInputAdvanceResult
{
    public bool isCompleted { get; private set; }

    public static AnomalyArrivalInputAdvanceResult createPending()
    {
        return new AnomalyArrivalInputAdvanceResult
        {
            isCompleted = false,
        };
    }

    public static AnomalyArrivalInputAdvanceResult createCompleted()
    {
        return new AnomalyArrivalInputAdvanceResult
        {
            isCompleted = true,
        };
    }
}
