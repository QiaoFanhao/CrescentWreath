using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyA007A008Runtime
{
    public const string A007ArrivalHandBanishContinuationKey = "continuation:anomalyA007ArrivalHandBanish";
    public const string A007ArrivalRinDiscardBanishContinuationKey = "continuation:anomalyA007ArrivalRinDiscardBanish";
    public const string A007ConditionOpponentOptionalDrawContinuationKey = "continuation:anomalyA007ConditionOpponentOptionalDraw";
    public const string A007RewardTargetCharmContinuationKey = "continuation:anomalyA007RewardTargetCharm";
    public const string A008ConditionOpponentOptionalDiscardReturnContinuationKey = "continuation:anomalyA008ConditionOpponentOptionalDiscardReturnParallel";
    public const string A008RewardTargetShackleContinuationKey = "continuation:anomalyA008RewardTargetShackle";

    private const string A007ArrivalHandBanishContextKey = "anomaly:A007:arrivalHandBanish";
    private const string A007ArrivalHandBanishInputTypeKey = "anomalyA007ArrivalHandBanishOne";
    private const string A007ArrivalRinDiscardBanishContextKey = "anomaly:A007:arrivalRinDiscardBanish";
    private const string A007ArrivalRinDiscardBanishInputTypeKey = "anomalyA007ArrivalRinDiscardBanishOptional";
    private const string A007ConditionOpponentOptionalDrawContextKey = "anomaly:A007:conditionOpponentOptionalDraw";
    private const string A007ConditionOpponentOptionalDrawInputTypeKey = "anomalyA007ConditionOpponentOptionalDraw";
    private const string A007RewardTargetCharmContextKey = "anomaly:A007:rewardTargetCharm";
    private const string A007RewardTargetCharmInputTypeKey = "anomalyA007RewardTargetCharm";
    private const string A008ConditionOpponentOptionalDiscardReturnContextKey = "anomaly:A008:conditionOpponentOptionalDiscardReturn";
    private const string A008ConditionOpponentOptionalDiscardReturnInputTypeKey = "anomalyA008ConditionOpponentOptionalDiscardReturn";
    private const string A008RewardTargetShackleContextKey = "anomaly:A008:rewardTargetShackle";
    private const string A008RewardTargetShackleInputTypeKey = "anomalyA008RewardTargetShackle";
    private const string HandCardChoicePrefix = "handCard:";
    private const string DiscardCardChoicePrefix = "discardCard:";
    private const string OpponentPlayerChoicePrefix = "opponentPlayer:";
    private const string DrawAcceptChoiceKey = "draw:accept";
    private const string DrawDeclineChoiceKey = "draw:decline";
    private const string DeclineChoiceKey = "decline";
    private const string SelectedChoiceStatePrefix = "anomaly:A007A008:selectedChoice:";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeyShackle = "Shackle";
    private const string RinDefinitionId = "C007";

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyA007A008Runtime(
        ZoneMovementService zoneMovementService,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool isParallelInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               inputContextState.requiredPlayerIds.Count > 0 &&
               (string.Equals(inputContextState.contextKey, A007ArrivalHandBanishContextKey, StringComparison.Ordinal) ||
                string.Equals(inputContextState.contextKey, A007ConditionOpponentOptionalDrawContextKey, StringComparison.Ordinal) ||
                string.Equals(inputContextState.contextKey, A008ConditionOpponentOptionalDiscardReturnContextKey, StringComparison.Ordinal));
    }

    public bool tryOpenA007ArrivalHandBanishInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var requiredPlayerIds = resolveSeatOrderPlayerIds(gameState)
            .Where(playerId => gameState.players.TryGetValue(playerId, out var playerState) &&
                               gameState.zones.TryGetValue(playerState.handZoneId, out var handZone) &&
                               handZone.cardInstanceIds.Count > 0)
            .ToList();
        if (requiredPlayerIds.Count == 0)
        {
            return tryOpenA007RinDiscardBanishInput(gameState, actionChainState, eventId);
        }

        var inputContextState = createParallelInputContext(
            actionChainState,
            A007ArrivalHandBanishInputTypeKey,
            A007ArrivalHandBanishContextKey);
        foreach (var playerId in requiredPlayerIds)
        {
            var playerState = gameState.players[playerId];
            var handZone = gameState.zones[playerState.handZoneId];
            inputContextState.requiredPlayerIds.Add(playerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[playerId.Value] =
                handZone.cardInstanceIds
                    .Select(cardInstanceId => HandCardChoicePrefix + cardInstanceId.Value)
                    .ToList();
        }

        openInputContext(gameState, actionChainState, inputContextState, A007ArrivalHandBanishContinuationKey, eventId);
        return true;
    }

    public bool continueA007ArrivalHandBanishChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureParallelSingleChoiceValid(inputContextState, request, A007ArrivalHandBanishContextKey);
        var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, HandCardChoicePrefix);
        var playerState = gameState.players[request.actorPlayerId];
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCard) ||
            selectedCard.ownerPlayerId != request.actorPlayerId ||
            selectedCard.zoneId != playerState.handZoneId)
        {
            throw new InvalidOperationException("A007 arrival hand banish requires selected card to remain in actor hand.");
        }

        recordSingleChoice(actionChainState, inputContextState, request);
        if (!allRequiredPlayersSubmitted(inputContextState))
        {
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            var choiceKey = resolveSelectedChoice(actionChainState, playerId);
            var cardInstanceId = parseCardChoiceKey(choiceKey, HandCardChoicePrefix);
            var cardInstance = gameState.cardInstances[cardInstanceId];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                cardInstance,
                gameState.publicState!.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                request.requestId));
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return !tryOpenA007RinDiscardBanishInput(gameState, actionChainState, request.requestId);
    }

    public bool tryOpenA007ConditionOpponentOptionalDrawInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long eventId)
    {
        var opponentPlayerIds = resolveOpponentPlayerIds(gameState, actorPlayerId);
        if (opponentPlayerIds.Count == 0)
        {
            return false;
        }

        var inputContextState = createParallelInputContext(
            actionChainState,
            A007ConditionOpponentOptionalDrawInputTypeKey,
            A007ConditionOpponentOptionalDrawContextKey);
        foreach (var opponentPlayerId in opponentPlayerIds)
        {
            inputContextState.requiredPlayerIds.Add(opponentPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[opponentPlayerId.Value] = new List<string>
            {
                DrawDeclineChoiceKey,
                DrawAcceptChoiceKey,
            };
        }

        openInputContext(gameState, actionChainState, inputContextState, A007ConditionOpponentOptionalDrawContinuationKey, eventId);
        return true;
    }

    public bool continueA007ConditionOpponentOptionalDrawChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureParallelSingleChoiceValid(inputContextState, request, A007ConditionOpponentOptionalDrawContextKey);
        recordSingleChoice(actionChainState, inputContextState, request);
        if (!allRequiredPlayersSubmitted(inputContextState))
        {
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            if (!string.Equals(resolveSelectedChoice(actionChainState, playerId), DrawAcceptChoiceKey, StringComparison.Ordinal))
            {
                continue;
            }

            drawOneCardIfPossible(gameState, actionChainState, playerId, request.requestId);
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    public bool tryOpenA008ConditionOpponentDiscardReturnInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long eventId)
    {
        var opponentPlayerIds = resolveOpponentPlayerIds(gameState, actorPlayerId);
        if (opponentPlayerIds.Count == 0)
        {
            return false;
        }

        var inputContextState = createParallelInputContext(
            actionChainState,
            A008ConditionOpponentOptionalDiscardReturnInputTypeKey,
            A008ConditionOpponentOptionalDiscardReturnContextKey);
        foreach (var opponentPlayerId in opponentPlayerIds)
        {
            var playerState = gameState.players[opponentPlayerId];
            var discardZone = gameState.zones[playerState.discardZoneId];
            var choiceKeys = new List<string> { DeclineChoiceKey };
            choiceKeys.AddRange(discardZone.cardInstanceIds.Select(cardInstanceId => DiscardCardChoicePrefix + cardInstanceId.Value));
            inputContextState.requiredPlayerIds.Add(opponentPlayerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[opponentPlayerId.Value] = choiceKeys;
        }

        openInputContext(gameState, actionChainState, inputContextState, A008ConditionOpponentOptionalDiscardReturnContinuationKey, eventId);
        return true;
    }

    public bool continueA008ConditionOpponentDiscardReturnChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureParallelSingleChoiceValid(inputContextState, request, A008ConditionOpponentOptionalDiscardReturnContextKey);
        if (!string.Equals(request.choiceKey, DeclineChoiceKey, StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, DiscardCardChoicePrefix);
            var playerState = gameState.players[request.actorPlayerId];
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCard) ||
                selectedCard.ownerPlayerId != request.actorPlayerId ||
                selectedCard.zoneId != playerState.discardZoneId)
            {
                throw new InvalidOperationException("A008 condition discard return requires selected card to remain in actor discard.");
            }
        }

        recordSingleChoice(actionChainState, inputContextState, request);
        if (!allRequiredPlayersSubmitted(inputContextState))
        {
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            var choiceKey = resolveSelectedChoice(actionChainState, playerId);
            if (string.Equals(choiceKey, DeclineChoiceKey, StringComparison.Ordinal))
            {
                continue;
            }

            var cardInstanceId = parseCardChoiceKey(choiceKey, DiscardCardChoicePrefix);
            var playerState = gameState.players[playerId];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                gameState.cardInstances[cardInstanceId],
                playerState.handZoneId,
                CardMoveReason.returnToSource,
                actionChainState.actionChainId,
                request.requestId));
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    public bool tryOpenA007RewardTargetCharmInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long eventId)
    {
        return tryOpenOpponentTargetInput(
            gameState,
            actionChainState,
            actorPlayerId,
            A007RewardTargetCharmInputTypeKey,
            A007RewardTargetCharmContextKey,
            A007RewardTargetCharmContinuationKey,
            eventId);
    }

    public void continueA007RewardTargetCharm(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var targetPlayerId = ensureSingleOpponentTargetChoice(
            gameState,
            inputContextState,
            request,
            A007RewardTargetCharmContextKey);
        applyStatusToActiveCharacter(gameState, request.actorPlayerId, targetPlayerId, StatusKeyCharm);
    }

    public bool tryOpenA008RewardTargetShackleInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long eventId)
    {
        return tryOpenOpponentTargetInput(
            gameState,
            actionChainState,
            actorPlayerId,
            A008RewardTargetShackleInputTypeKey,
            A008RewardTargetShackleContextKey,
            A008RewardTargetShackleContinuationKey,
            eventId);
    }

    public void continueA008RewardTargetShackle(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var targetPlayerId = ensureSingleOpponentTargetChoice(
            gameState,
            inputContextState,
            request,
            A008RewardTargetShackleContextKey);
        applyStatusToActiveCharacter(gameState, request.actorPlayerId, targetPlayerId, StatusKeyShackle);
    }

    public void applyOpponentTeamKillScoreMinusOne(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        var opponentTeamId = resolveSingleOpponentTeamId(gameState, actorTeamId);
        gameState.teams[opponentTeamId].killScore = Math.Max(0, gameState.teams[opponentTeamId].killScore - 1);
    }

    private bool tryOpenA007RinDiscardBanishInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        var rinPlayerId = tryResolveActiveCharacterOwner(gameState, RinDefinitionId);
        if (!rinPlayerId.HasValue)
        {
            return false;
        }

        var playerState = gameState.players[rinPlayerId.Value];
        var discardZone = gameState.zones[playerState.discardZoneId];
        if (discardZone.cardInstanceIds.Count == 0)
        {
            return false;
        }

        var inputContextState = new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = rinPlayerId.Value,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = A007ArrivalRinDiscardBanishInputTypeKey,
            contextKey = A007ArrivalRinDiscardBanishContextKey,
        };
        inputContextState.choiceKeys.Add(DeclineChoiceKey);
        inputContextState.choiceKeys.AddRange(discardZone.cardInstanceIds.Select(cardInstanceId => DiscardCardChoicePrefix + cardInstanceId.Value));
        openInputContext(gameState, actionChainState, inputContextState, A007ArrivalRinDiscardBanishContinuationKey, eventId);
        return true;
    }

    public void continueA007RinDiscardBanish(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureSinglePlayerChoiceValid(inputContextState, request, A007ArrivalRinDiscardBanishContextKey);
        if (!string.Equals(request.choiceKey, DeclineChoiceKey, StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, DiscardCardChoicePrefix);
            var playerState = gameState.players[request.actorPlayerId];
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCard) ||
                selectedCard.ownerPlayerId != request.actorPlayerId ||
                selectedCard.zoneId != playerState.discardZoneId)
            {
                throw new InvalidOperationException("A007 Rin discard banish requires selected card to remain in actor discard.");
            }

            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                selectedCard,
                gameState.publicState!.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                request.requestId));
        }

    }

    private bool tryOpenOpponentTargetInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        long eventId)
    {
        var choiceKeys = resolveOpponentPlayerIds(gameState, actorPlayerId)
            .Where(playerId => gameState.players[playerId].activeCharacterInstanceId.HasValue)
            .Select(playerId => OpponentPlayerChoicePrefix + playerId.Value)
            .ToList();
        if (choiceKeys.Count == 0)
        {
            return false;
        }

        var inputContextState = new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);
        openInputContext(gameState, actionChainState, inputContextState, continuationKey, eventId);
        return true;
    }

    private static PlayerId ensureSingleOpponentTargetChoice(
        RuleCore.GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string contextKey)
    {
        ensureSinglePlayerChoiceValid(inputContextState, request, contextKey);
        var targetPlayerId = parseOpponentPlayerChoiceKey(request.choiceKey);
        var actorTeamId = gameState.players[request.actorPlayerId].teamId;
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            targetPlayerState.teamId == actorTeamId ||
            !targetPlayerState.activeCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Anomaly reward target requires selected player to be an active opponent.");
        }

        return targetPlayerId;
    }

    private static void applyStatusToActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerId applierPlayerId,
        PlayerId targetPlayerId,
        string statusKey)
    {
        var targetCharacterInstanceId = gameState.players[targetPlayerId].activeCharacterInstanceId!.Value;
        StatusRuntime.applyStatus(gameState, new StatusInstance
        {
            statusKey = statusKey,
            applierPlayerId = applierPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            stackCount = 1,
        });
    }

    private InputContextState createParallelInputContext(
        ActionChainState actionChainState,
        string inputTypeKey,
        string contextKey)
    {
        return new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
    }

    private static void ensureParallelSingleChoiceValid(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string expectedContextKey)
    {
        if (!string.Equals(inputContextState.contextKey, expectedContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Anomaly parallel continuation requires matching currentInputContext.contextKey.");
        }

        if (!inputContextState.requiredPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("Anomaly parallel continuation requires actorPlayerId to be one of currentInputContext.requiredPlayerIds.");
        }

        if (inputContextState.submittedPlayerIds.Contains(request.actorPlayerId))
        {
            throw new InvalidOperationException("Anomaly parallel continuation does not allow duplicate submitInputChoice from the same actor.");
        }

        if (!inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(request.actorPlayerId.Value, out var choiceKeys) ||
            !choiceKeys.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("Anomaly parallel continuation requires choiceKey to be one of currentInputContext.choiceKeysByRequiredPlayerNumericId[actorPlayerId].");
        }
    }

    private static void ensureSinglePlayerChoiceValid(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string expectedContextKey)
    {
        if (!string.Equals(inputContextState.contextKey, expectedContextKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Anomaly continuation requires matching currentInputContext.contextKey.");
        }

        if (!inputContextState.requiredPlayerId.HasValue ||
            inputContextState.requiredPlayerId.Value != request.actorPlayerId)
        {
            throw new InvalidOperationException("Anomaly continuation requires actorPlayerId to equal currentInputContext.requiredPlayerId.");
        }

        if (!inputContextState.choiceKeys.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("Anomaly continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }
    }

    private static void recordSingleChoice(
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        inputContextState.submittedPlayerIds.Add(request.actorPlayerId);
        actionChainState.localState[SelectedChoiceStatePrefix + request.actorPlayerId.Value] = request.choiceKey;
    }

    private static bool allRequiredPlayersSubmitted(InputContextState inputContextState)
    {
        return inputContextState.submittedPlayerIds.Count >= inputContextState.requiredPlayerIds.Count;
    }

    private static string resolveSelectedChoice(ActionChainState actionChainState, PlayerId playerId)
    {
        if (!actionChainState.localState.TryGetValue(SelectedChoiceStatePrefix + playerId.Value, out var choiceKey))
        {
            throw new InvalidOperationException("Anomaly parallel continuation requires selected choice in actionChain.localState.");
        }

        return choiceKey;
    }

    private void drawOneCardIfPossible(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long eventId)
    {
        var playerState = gameState.players[playerId];
        var deckZone = gameState.zones[playerState.deckZoneId];
        var discardZone = gameState.zones[playerState.discardZoneId];
        if (deckZone.cardInstanceIds.Count == 0 && discardZone.cardInstanceIds.Count > 0)
        {
            foreach (var cardInstanceId in PlayerDeckRuntime.createShuffledCardInstanceIds(discardZone.cardInstanceIds))
            {
                actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                    gameState,
                    gameState.cardInstances[cardInstanceId],
                    playerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    eventId));
            }
        }

        if (deckZone.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topCard = gameState.cardInstances[deckZone.cardInstanceIds[0]];
        actionChainState.producedEvents.Add(zoneMovementService.moveCard(
            gameState,
            topCard,
            playerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            eventId));
    }

    private void closeInputContext(
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

    private static List<PlayerId> resolvePlayerOrder(IEnumerable<PlayerId> playerIds)
    {
        return playerIds.OrderBy(playerId => playerId.Value).ToList();
    }

    private static List<PlayerId> resolveSeatOrderPlayerIds(RuleCore.GameState.GameState gameState)
    {
        if (gameState.matchMeta is not null && gameState.matchMeta.seatOrder.Count > 0)
        {
            return gameState.matchMeta.seatOrder
                .Where(playerId => gameState.players.ContainsKey(playerId))
                .Distinct()
                .ToList();
        }

        return gameState.players.Keys.OrderBy(playerId => playerId.Value).ToList();
    }

    private static List<PlayerId> resolveOpponentPlayerIds(RuleCore.GameState.GameState gameState, PlayerId actorPlayerId)
    {
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        return gameState.players.Values
            .Where(player => player.teamId != actorTeamId)
            .Select(player => player.playerId)
            .OrderBy(playerId => playerId.Value)
            .ToList();
    }

    private static TeamId resolveSingleOpponentTeamId(RuleCore.GameState.GameState gameState, TeamId actorTeamId)
    {
        var opponentTeamIds = gameState.teams.Keys.Where(teamId => teamId != actorTeamId).ToList();
        if (opponentTeamIds.Count != 1)
        {
            throw new InvalidOperationException("Anomaly reward requires exactly one opponent team.");
        }

        return opponentTeamIds[0];
    }

    private static PlayerId? tryResolveActiveCharacterOwner(RuleCore.GameState.GameState gameState, string characterDefinitionId)
    {
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (!playerState.activeCharacterInstanceId.HasValue ||
                !gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var character))
            {
                continue;
            }

            if (string.Equals(character.definitionId, characterDefinitionId, StringComparison.Ordinal))
            {
                return playerState.playerId;
            }
        }

        return null;
    }

    private static CardInstanceId parseCardChoiceKey(string choiceKey, string prefix)
    {
        if (!choiceKey.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(prefix.Length), out var cardInstanceNumericId))
        {
            throw new InvalidOperationException("Anomaly choiceKey has invalid card choice format.");
        }

        return new CardInstanceId(cardInstanceNumericId);
    }

    private static PlayerId parseOpponentPlayerChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(OpponentPlayerChoicePrefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(OpponentPlayerChoicePrefix.Length), out var playerNumericId))
        {
            throw new InvalidOperationException("Anomaly reward target choiceKey has invalid opponent player format.");
        }

        return new PlayerId(playerNumericId);
    }

    private void openInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        string continuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("Anomaly input requires no active inputContext before opening.");
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
}
