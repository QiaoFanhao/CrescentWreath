using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyA010Runtime
{
    public const string DefinitionId = "A010";
    public const string ArrivalStepKey = "applyA010FateStayNightSetAsideInput";

    public const string ContinuationKeyArrivalSetAside = "continuation:anomalyA010ArrivalSetAside";
    public const string ContinuationKeyKillBanishSetAside = "continuation:anomalyA010KillBanishSetAside";
    public const string ContinuationKeyRewardChooseTwo = "continuation:anomalyA010RewardChooseTwo";
    public const string ContinuationKeyRewardSelectSummonToHand = "continuation:anomalyA010RewardSelectSummonToHand";

    public const string InputTypeKeyArrivalSetAside = "anomalyA010ArrivalSetAside";
    public const string InputTypeKeyKillBanishSetAside = "anomalyA010KillBanishSetAside";
    public const string InputTypeKeyRewardChooseTwo = "anomalyA010RewardChooseTwo";
    public const string InputTypeKeyRewardSelectSummonToHand = "anomalyA010RewardSelectSummonToHand";

    public const string ContextKeyArrivalSetAside = "anomaly:A010:arrivalSetAside";
    public const string ContextKeyKillBanishSetAside = "anomaly:A010:killBanishSetAside";
    public const string ContextKeyRewardChooseTwo = "anomaly:A010:rewardChooseTwo";
    public const string ContextKeyRewardSelectSummonToHand = "anomaly:A010:rewardSelectSummonToHand";

    public const string ChoicePrefixSetAsideCard = "setAsideCard:";
    public const string ChoicePrefixFriendlySetAside = "friendlySetAside:";
    public const string ChoiceRewardLeyline = "reward:leyline3";
    public const string ChoiceRewardKillScore = "reward:killScorePlus1";
    public const string ChoiceRewardSummonToHand = "reward:summonToHand";
    public const string ChoicePrefixSummonCard = "summonCard:";

    private const string LocalStateKeyArrivalNextSeatIndex = "anomaly:A010:arrivalNextSeatIndex";
    private const string LocalStateKeyRewardResolverPlayerId = "anomaly:A010:rewardResolverPlayerId";
    private const string LocalStateKeyRewardSelectedChoices = "anomaly:A010:rewardSelectedChoices";
    private const string LocalStateKeySetAsideByPlayerPrefix = "anomaly:A010:setAsideCardByPlayer:";
    private const int LeylineMaxValue = 5;

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyA010Runtime(
        ZoneMovementService zoneMovementService,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public static bool isFateStayNightActiveAndUnresolved(GameState.GameState gameState)
    {
        return gameState.currentAnomalyState is not null &&
               string.Equals(gameState.currentAnomalyState.currentAnomalyDefinitionId, DefinitionId, StringComparison.Ordinal) &&
               !gameState.resolvedAnomalyDefinitionIds.Contains(DefinitionId);
    }

    public static bool shouldBlockAnomalyDeckMutation(GameState.GameState gameState)
    {
        return isFateStayNightActiveAndUnresolved(gameState);
    }

    public static bool shouldSkipT017ForcedFateStayNight(GameState.GameState gameState)
    {
        return shouldBlockAnomalyDeckMutation(gameState) ||
               gameState.resolvedAnomalyDefinitionIds.Contains(DefinitionId);
    }

    public bool tryOpenArrivalSetAsideInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var nextPlayerId = findNextArrivalPlayerWithChoices(gameState, 0, out var nextSeatIndex, out var choiceKeys);
        if (!nextPlayerId.HasValue)
        {
            completeActionChain(actionChainState);
            return false;
        }

        actionChainState.localState[LocalStateKeyArrivalNextSeatIndex] = (nextSeatIndex + 1).ToString();
        openInputContext(
            gameState,
            actionChainState,
            eventId,
            nextPlayerId.Value,
            InputTypeKeyArrivalSetAside,
            ContextKeyArrivalSetAside,
            ContinuationKeyArrivalSetAside,
            choiceKeys);
        return true;
    }

    public void ensureValidArrivalSetAsideChoice(
        GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureContext(inputContextState, ContextKeyArrivalSetAside);
        if (!inputContextState.choiceKeys.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("A010 arrival set-aside requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        var cardInstanceId = parseCardChoice(request.choiceKey, ChoicePrefixSetAsideCard);
        if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
        {
            throw new InvalidOperationException("A010 arrival set-aside requires selected card to exist.");
        }

        if (!inputContextState.requiredPlayerId.HasValue ||
            cardInstance.ownerPlayerId != inputContextState.requiredPlayerId.Value)
        {
            throw new InvalidOperationException("A010 arrival set-aside requires selected card to be owned by required player.");
        }

        if (!isArrivalSelectableCard(gameState, gameState.players[inputContextState.requiredPlayerId.Value], cardInstance))
        {
            throw new InvalidOperationException("A010 arrival set-aside requires selected card to remain in required player's hand, discard, field, or gap.");
        }
    }

    public void continueArrivalSetAside(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var requiredPlayerId = inputContextState.requiredPlayerId ??
                               throw new InvalidOperationException("A010 arrival set-aside requires requiredPlayerId.");
        var selectedCardInstanceId = parseCardChoice(request.choiceKey, ChoicePrefixSetAsideCard);
        var selectedCardInstance = gameState.cardInstances[selectedCardInstanceId];
        var activeCharacter = resolveActiveCharacter(gameState, requiredPlayerId);

        var moveEvent = CharacterSetAsideRuntime.setAsideCardUnderCharacter(
            gameState,
            zoneMovementService,
            selectedCardInstance,
            activeCharacter,
            actionChainState.actionChainId,
            request.requestId);
        actionChainState.producedEvents.Add(moveEvent);
        setA010SetAsideCardForPlayer(gameState, requiredPlayerId, selectedCardInstanceId);

        var startSeatIndex = tryReadInt(actionChainState.localState, LocalStateKeyArrivalNextSeatIndex, 0);
        var nextPlayerId = findNextArrivalPlayerWithChoices(gameState, startSeatIndex, out var nextSeatIndex, out var choiceKeys);
        if (!nextPlayerId.HasValue)
        {
            actionChainState.pendingContinuationKey = null;
            completeActionChain(actionChainState);
            return;
        }

        actionChainState.localState[LocalStateKeyArrivalNextSeatIndex] = (nextSeatIndex + 1).ToString();
        openInputContext(
            gameState,
            actionChainState,
            request.requestId,
            nextPlayerId.Value,
            InputTypeKeyArrivalSetAside,
            ContextKeyArrivalSetAside,
            ContinuationKeyArrivalSetAside,
            choiceKeys);
    }

    public bool tryOpenKillBanishSetAsideInputFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (!isFateStayNightActiveAndUnresolved(gameState) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return false;
        }

        for (var index = producedEventsStartIndex; index < actionChainState.producedEvents.Count; index++)
        {
            if (actionChainState.producedEvents[index] is not KillRecordedEvent killRecordedEvent ||
                !killRecordedEvent.killerPlayerId.HasValue)
            {
                continue;
            }

            var killerPlayerId = killRecordedEvent.killerPlayerId.Value;
            var choiceKeys = collectFriendlySetAsideChoiceKeys(gameState, killerPlayerId);
            if (choiceKeys.Count == 0)
            {
                continue;
            }

            openInputContext(
                gameState,
                actionChainState,
                eventId,
                killerPlayerId,
                InputTypeKeyKillBanishSetAside,
                ContextKeyKillBanishSetAside,
                ContinuationKeyKillBanishSetAside,
                choiceKeys);
            return true;
        }

        return false;
    }

    public bool tryForceResolveFromExternalEffect(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId resolverPlayerId)
    {
        if (!isFateStayNightActiveAndUnresolved(gameState))
        {
            return false;
        }

        banishAllRemainingA010SetAsideCards(gameState, actionChainState, eventId);
        openRewardChooseTwoInput(
            gameState,
            actionChainState,
            eventId,
            resolverPlayerId);
        return true;
    }

    public void ensureValidKillBanishSetAsideChoice(
        GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureContext(inputContextState, ContextKeyKillBanishSetAside);
        if (!inputContextState.choiceKeys.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("A010 kill set-aside banish requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        var (_, cardInstanceId) = parseFriendlySetAsideChoice(request.choiceKey);
        if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
            cardInstance.zoneKey != ZoneKey.characterSetAside ||
            !cardInstance.isSetAside)
        {
            throw new InvalidOperationException("A010 kill set-aside banish requires selected card to remain set aside under a character.");
        }
    }

    public void continueKillBanishSetAside(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var killerPlayerId = inputContextState.requiredPlayerId ??
                             throw new InvalidOperationException("A010 kill set-aside banish requires requiredPlayerId.");
        var (friendlyPlayerId, cardInstanceId) = parseFriendlySetAsideChoice(request.choiceKey);
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var moveEvent = CharacterSetAsideRuntime.banishSetAsideCard(
            gameState,
            zoneMovementService,
            cardInstance,
            gameState.publicState!.gapZoneId,
            actionChainState.actionChainId,
            request.requestId);
        actionChainState.producedEvents.Add(moveEvent);
        clearA010SetAsideCardForPlayer(gameState, friendlyPlayerId);

        if (!killerTeamHasA010SetAsideCards(gameState, killerPlayerId))
        {
            banishAllRemainingA010SetAsideCards(gameState, actionChainState, request.requestId);
            openRewardChooseTwoInput(
                gameState,
                actionChainState,
                request.requestId,
                killerPlayerId);
            return;
        }

        actionChainState.pendingContinuationKey = null;
        completeActionChain(actionChainState);
    }

    public static void ensureValidRewardChooseTwoChoice(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureContext(inputContextState, ContextKeyRewardChooseTwo);
        if (request.choiceKeys.Count != 2 ||
            request.choiceKeys.Distinct(StringComparer.Ordinal).Count() != 2 ||
            request.choiceKeys.Any(choiceKey => !inputContextState.choiceKeys.Contains(choiceKey)))
        {
            throw new InvalidOperationException("A010 reward requires exactly two unique choices from currentInputContext.choiceKeys.");
        }
    }

    public void continueRewardChooseTwo(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        Func<bool> flipNextAnomalyIgnoringLock)
    {
        var resolverPlayerId = inputContextState.requiredPlayerId ??
                               throw new InvalidOperationException("A010 reward choose-two requires requiredPlayerId.");
        actionChainState.localState[LocalStateKeyRewardResolverPlayerId] = resolverPlayerId.Value.ToString();
        actionChainState.localState[LocalStateKeyRewardSelectedChoices] = string.Join("|", request.choiceKeys);

        var selectedChoices = request.choiceKeys.ToHashSet(StringComparer.Ordinal);
        if (selectedChoices.Contains(ChoiceRewardLeyline))
        {
            var teamState = gameState.teams[gameState.players[resolverPlayerId].teamId];
            teamState.leyline = Math.Min(LeylineMaxValue, teamState.leyline + 3);
        }

        if (selectedChoices.Contains(ChoiceRewardKillScore))
        {
            var teamState = gameState.teams[gameState.players[resolverPlayerId].teamId];
            teamState.killScore += 1;
        }

        if (selectedChoices.Contains(ChoiceRewardSummonToHand) && collectSummonCardChoiceKeys(gameState).Count > 0)
        {
            openInputContext(
                gameState,
                actionChainState,
                request.requestId,
                resolverPlayerId,
                InputTypeKeyRewardSelectSummonToHand,
                ContextKeyRewardSelectSummonToHand,
                ContinuationKeyRewardSelectSummonToHand,
                collectSummonCardChoiceKeys(gameState));
            return;
        }

        finalizeA010Resolve(gameState, actionChainState, request.requestId, flipNextAnomalyIgnoringLock);
    }

    public void ensureValidRewardSelectSummonToHandChoice(
        GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureContext(inputContextState, ContextKeyRewardSelectSummonToHand);
        if (!inputContextState.choiceKeys.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("A010 reward summon-to-hand requires choiceKey to be one of currentInputContext.choiceKeys.");
        }

        var cardInstanceId = parseCardChoice(request.choiceKey, ChoicePrefixSummonCard);
        if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
            gameState.publicState is null ||
            cardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException("A010 reward summon-to-hand requires selected card to remain in summonZone.");
        }
    }

    public void continueRewardSelectSummonToHand(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        Func<bool> flipNextAnomalyIgnoringLock)
    {
        var resolverPlayerId = inputContextState.requiredPlayerId ??
                               throw new InvalidOperationException("A010 reward summon-to-hand requires requiredPlayerId.");
        var cardInstanceId = parseCardChoice(request.choiceKey, ChoicePrefixSummonCard);
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var playerState = gameState.players[resolverPlayerId];
        var moveEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            playerState.handZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            request.requestId);
        actionChainState.producedEvents.Add(moveEvent);
        cardInstance.ownerPlayerId = resolverPlayerId;
        refillSummonZoneFromPublicDeck(gameState, actionChainState, request.requestId);
        finalizeA010Resolve(gameState, actionChainState, request.requestId, flipNextAnomalyIgnoringLock);
    }

    private void openRewardChooseTwoInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId resolverPlayerId)
    {
        openInputContext(
            gameState,
            actionChainState,
            eventId,
            resolverPlayerId,
            InputTypeKeyRewardChooseTwo,
            ContextKeyRewardChooseTwo,
            ContinuationKeyRewardChooseTwo,
            new List<string>
            {
                ChoiceRewardLeyline,
                ChoiceRewardKillScore,
                ChoiceRewardSummonToHand,
            });
    }

    private void finalizeA010Resolve(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        Func<bool> flipNextAnomalyIgnoringLock)
    {
        var definition = Definitions.AnomalyDefinitionRepository.resolveByDefinitionId(DefinitionId);
        AnomalyResolveFinalizeHelper.finalizeSuccessfulResolve(
            gameState,
            actionChainState,
            requestId,
            definition,
            flipNextAnomalyIgnoringLock,
            "A010 reward continuation requires turnState.");
    }

    private void openInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        List<string> choiceKeys)
    {
        var inputContextId = new InputContextId(nextInputContextIdSupplier());
        gameState.currentInputContext = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
        gameState.currentInputContext.choiceKeys.AddRange(choiceKeys);
        actionChainState.pendingContinuationKey = continuationKey;
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

    private PlayerId? findNextArrivalPlayerWithChoices(
        GameState.GameState gameState,
        int startSeatIndex,
        out int foundSeatIndex,
        out List<string> choiceKeys)
    {
        var seatOrder = resolveSeatOrder(gameState);
        for (var offset = 0; offset < seatOrder.Count; offset++)
        {
            var seatIndex = startSeatIndex + offset;
            if (seatIndex >= seatOrder.Count)
            {
                break;
            }

            var playerId = seatOrder[seatIndex];
            if (hasA010SetAsideCardForPlayer(gameState, playerId))
            {
                continue;
            }

            choiceKeys = collectArrivalSetAsideChoiceKeys(gameState, playerId);
            if (choiceKeys.Count > 0)
            {
                foundSeatIndex = seatIndex;
                return playerId;
            }
        }

        foundSeatIndex = -1;
        choiceKeys = new List<string>();
        return null;
    }

    private List<string> collectArrivalSetAsideChoiceKeys(GameState.GameState gameState, PlayerId playerId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            return new List<string>();
        }

        var choiceKeys = new List<string>();
        appendSetAsideChoicesFromZone(gameState, playerState.handZoneId, playerState, choiceKeys);
        appendSetAsideChoicesFromZone(gameState, playerState.discardZoneId, playerState, choiceKeys);
        appendSetAsideChoicesFromZone(gameState, playerState.fieldZoneId, playerState, choiceKeys);
        if (gameState.publicState is not null)
        {
            appendSetAsideChoicesFromZone(gameState, gameState.publicState.gapZoneId, playerState, choiceKeys);
        }

        return choiceKeys;
    }

    private static void appendSetAsideChoicesFromZone(
        GameState.GameState gameState,
        ZoneId zoneId,
        PlayerState playerState,
        List<string> choiceKeys)
    {
        if (!gameState.zones.TryGetValue(zoneId, out var zoneState))
        {
            return;
        }

        foreach (var cardInstanceId in zoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
                cardInstance.ownerPlayerId != playerState.playerId)
            {
                continue;
            }

            if (!isArrivalSelectableCard(gameState, playerState, cardInstance))
            {
                continue;
            }

            choiceKeys.Add(ChoicePrefixSetAsideCard + cardInstanceId.Value);
        }
    }

    private static bool isArrivalSelectableCard(
        GameState.GameState gameState,
        PlayerState playerState,
        CardInstance cardInstance)
    {
        if (cardInstance.ownerPlayerId != playerState.playerId)
        {
            return false;
        }

        var publicState = gameState.publicState;
        return cardInstance.zoneId == playerState.handZoneId ||
               cardInstance.zoneId == playerState.discardZoneId ||
               cardInstance.zoneId == playerState.fieldZoneId ||
               (publicState is not null && cardInstance.zoneId == publicState.gapZoneId);
    }

    private List<string> collectFriendlySetAsideChoiceKeys(GameState.GameState gameState, PlayerId killerPlayerId)
    {
        var killerTeamId = gameState.players[killerPlayerId].teamId;
        var choiceKeys = new List<string>();
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (playerState.teamId != killerTeamId ||
                !tryGetA010SetAsideCardForPlayer(gameState, playerState.playerId, out var cardInstanceId))
            {
                continue;
            }

            choiceKeys.Add($"{ChoicePrefixFriendlySetAside}{playerState.playerId.Value}:{cardInstanceId.Value}");
        }

        return choiceKeys;
    }

    private bool killerTeamHasA010SetAsideCards(GameState.GameState gameState, PlayerId killerPlayerId)
    {
        var teamId = gameState.players[killerPlayerId].teamId;
        return gameState.players.Values.Any(player =>
            player.teamId == teamId &&
            tryGetA010SetAsideCardForPlayer(gameState, player.playerId, out _));
    }

    private void banishAllRemainingA010SetAsideCards(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        foreach (var playerState in gameState.players.Values.ToList())
        {
            if (!tryGetA010SetAsideCardForPlayer(gameState, playerState.playerId, out var cardInstanceId))
            {
                continue;
            }

            if (gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) &&
                gameState.publicState is not null &&
                cardInstance.zoneKey == ZoneKey.characterSetAside)
            {
                actionChainState.producedEvents.Add(CharacterSetAsideRuntime.banishSetAsideCard(
                    gameState,
                    zoneMovementService,
                    cardInstance,
                    gameState.publicState.gapZoneId,
                    actionChainState.actionChainId,
                    eventId));
            }

            clearA010SetAsideCardForPlayer(gameState, playerState.playerId);
        }
    }

    private void refillSummonZoneFromPublicDeck(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.publicState is null)
        {
            return;
        }

        var summonZone = gameState.zones[gameState.publicState.summonZoneId];
        var publicDeck = gameState.zones[gameState.publicState.publicTreasureDeckZoneId];
        if (publicDeck.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topCardId = publicDeck.cardInstanceIds[0];
        var topCard = gameState.cardInstances[topCardId];
        actionChainState.producedEvents.Add(zoneMovementService.moveCard(
            gameState,
            topCard,
            summonZone.zoneId,
            CardMoveReason.reveal,
            actionChainState.actionChainId,
            eventId));
    }

    private List<string> collectSummonCardChoiceKeys(GameState.GameState gameState)
    {
        if (gameState.publicState is null)
        {
            return new List<string>();
        }

        return gameState.zones[gameState.publicState.summonZoneId].cardInstanceIds
            .Select(cardId => ChoicePrefixSummonCard + cardId.Value)
            .ToList();
    }

    private List<PlayerId> resolveSeatOrder(GameState.GameState gameState)
    {
        var ordered = gameState.players.Keys.OrderBy(playerId => playerId.Value).ToList();
        if (gameState.turnState is null)
        {
            return ordered;
        }

        var currentIndex = ordered.FindIndex(playerId => playerId == gameState.turnState.currentPlayerId);
        if (currentIndex <= 0)
        {
            return ordered;
        }

        return ordered.Skip(currentIndex).Concat(ordered.Take(currentIndex)).ToList();
    }

    private static CharacterInstance resolveActiveCharacter(GameState.GameState gameState, PlayerId playerId)
    {
        var characterInstanceId = gameState.players[playerId].activeCharacterInstanceId ??
                                  throw new InvalidOperationException("A010 requires player active character.");
        return gameState.characterInstances[characterInstanceId];
    }

    private static CardInstanceId parseCardChoice(string choiceKey, string prefix)
    {
        if (!choiceKey.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey[prefix.Length..], out var cardInstanceNumericId))
        {
            throw new InvalidOperationException($"A010 choiceKey must use {prefix}{{cardInstanceNumericId}} format.");
        }

        return new CardInstanceId(cardInstanceNumericId);
    }

    private static (PlayerId PlayerId, CardInstanceId CardInstanceId) parseFriendlySetAsideChoice(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoicePrefixFriendlySetAside, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A010 friendly set-aside choiceKey must use friendlySetAside:{playerId}:{cardInstanceId} format.");
        }

        var parts = choiceKey[ChoicePrefixFriendlySetAside.Length..].Split(':');
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], out var playerNumericId) ||
            !long.TryParse(parts[1], out var cardInstanceNumericId))
        {
            throw new InvalidOperationException("A010 friendly set-aside choiceKey must use friendlySetAside:{playerId}:{cardInstanceId} format.");
        }

        return (new PlayerId(playerNumericId), new CardInstanceId(cardInstanceNumericId));
    }

    private static string setAsideLocalStateKey(PlayerId playerId)
    {
        return LocalStateKeySetAsideByPlayerPrefix + playerId.Value;
    }

    private static bool hasA010SetAsideCardForPlayer(GameState.GameState gameState, PlayerId playerId)
    {
        return tryGetA010SetAsideCardForPlayer(gameState, playerId, out _);
    }

    private static bool tryGetA010SetAsideCardForPlayer(
        GameState.GameState gameState,
        PlayerId playerId,
        out CardInstanceId cardInstanceId)
    {
        cardInstanceId = default;
        if (gameState.currentAnomalyState is null ||
            !gameState.currentAnomalyState.localState.TryGetValue(setAsideLocalStateKey(playerId), out var text) ||
            !long.TryParse(text, out var cardInstanceNumericId))
        {
            return false;
        }

        var candidateId = new CardInstanceId(cardInstanceNumericId);
        if (!gameState.cardInstances.TryGetValue(candidateId, out var cardInstance) ||
            cardInstance.zoneKey != ZoneKey.characterSetAside ||
            !cardInstance.isSetAside)
        {
            return false;
        }

        cardInstanceId = candidateId;
        return true;
    }

    private static void setA010SetAsideCardForPlayer(
        GameState.GameState gameState,
        PlayerId playerId,
        CardInstanceId cardInstanceId)
    {
        gameState.currentAnomalyState!.localState[setAsideLocalStateKey(playerId)] = cardInstanceId.Value.ToString();
    }

    private static void clearA010SetAsideCardForPlayer(GameState.GameState gameState, PlayerId playerId)
    {
        gameState.currentAnomalyState?.localState.Remove(setAsideLocalStateKey(playerId));
    }

    private static void ensureContext(InputContextState inputContextState, string expectedContextKey)
    {
        if (!string.Equals(inputContextState.contextKey, expectedContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"A010 continuation requires currentInputContext.contextKey to be {expectedContextKey}.");
        }
    }

    private static int tryReadInt(Dictionary<string, string> localState, string key, int fallback)
    {
        return localState.TryGetValue(key, out var text) && int.TryParse(text, out var value)
            ? value
            : fallback;
    }

    private static void completeActionChain(ActionChainState actionChainState)
    {
        actionChainState.pendingContinuationKey = null;
        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
    }
}
