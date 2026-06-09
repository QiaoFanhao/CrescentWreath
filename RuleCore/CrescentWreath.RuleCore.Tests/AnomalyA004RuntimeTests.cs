using System;
using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Initialization;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public sealed class AnomalyA004RuntimeTests
{
    [Fact]
    public void TryResolveA004_WhenFriendlyPlayerCannotPaySkillPointCard_ShouldFailWithoutPayingMana()
    {
        var gameState = createA004ResolveState();
        var actorPlayerId = gameState.turnState!.currentPlayerId;
        var actorPlayer = gameState.players[actorPlayerId];
        var allyPlayerId = resolveAllyPlayerId(gameState, actorPlayerId);
        setFirstHandCardDefinition(gameState, actorPlayerId, "S001");
        actorPlayer.mana = 8;

        var events = new ActionRequestProcessor().processActionRequest(
            gameState,
            new TryResolveAnomalyActionRequest
            {
                requestId = 41001,
                actorPlayerId = actorPlayerId,
            });

        var attemptedEvent = Assert.Single(events.OfType<AnomalyResolveAttemptedEvent>());
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal(AnomalyValidationFailureKeys.FriendlyCannotPaySkillPointCard, attemptedEvent.failedReasonKey);
        Assert.Equal(8, actorPlayer.mana);
        Assert.Null(gameState.currentInputContext);
        Assert.Equal("A004", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.DoesNotContain(
            gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds,
            cardId => gameState.cardInstances[cardId].definitionId == "S001");
    }

    [Fact]
    public void TryResolveA004_WhenActorCannotPaySkillPointCard_ShouldReportActorFailureBeforeMana()
    {
        var gameState = createA004ResolveState();
        var actorPlayerId = gameState.turnState!.currentPlayerId;
        var allyPlayerId = resolveAllyPlayerId(gameState, actorPlayerId);
        setFirstHandCardDefinition(gameState, allyPlayerId, "T011");
        gameState.players[actorPlayerId].mana = 0;

        var events = new ActionRequestProcessor().processActionRequest(
            gameState,
            new TryResolveAnomalyActionRequest
            {
                requestId = 410011,
                actorPlayerId = actorPlayerId,
            });

        var attemptedEvent = Assert.Single(events.OfType<AnomalyResolveAttemptedEvent>());
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal(AnomalyValidationFailureKeys.ActorCannotPaySkillPointCard, attemptedEvent.failedReasonKey);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void TryResolveA004_WhenSkillPointCardsExistButManaIsInsufficient_ShouldNotOpenInput()
    {
        var gameState = createA004ResolveState();
        var actorPlayerId = gameState.turnState!.currentPlayerId;
        var allyPlayerId = resolveAllyPlayerId(gameState, actorPlayerId);
        setFirstHandCardDefinition(gameState, actorPlayerId, "S001");
        setFirstHandCardDefinition(gameState, allyPlayerId, "T018");
        gameState.players[actorPlayerId].mana = 7;

        var events = new ActionRequestProcessor().processActionRequest(
            gameState,
            new TryResolveAnomalyActionRequest
            {
                requestId = 410012,
                actorPlayerId = actorPlayerId,
            });

        var attemptedEvent = Assert.Single(events.OfType<AnomalyResolveAttemptedEvent>());
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal(AnomalyValidationFailureKeys.InsufficientMana, attemptedEvent.failedReasonKey);
        Assert.Equal(7, gameState.players[actorPlayerId].mana);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void TryResolveA004_WhenBothFriendlyPlayersCanPay_ShouldWaitForBothThenGrantExtraTurn()
    {
        var gameState = createA004ResolveState();
        var actorPlayerId = gameState.turnState!.currentPlayerId;
        var allyPlayerId = resolveAllyPlayerId(gameState, actorPlayerId);
        var actorPaymentCardId = setFirstHandCardDefinition(gameState, actorPlayerId, "S001");
        var allyPaymentCardId = setFirstHandCardDefinition(gameState, allyPlayerId, "T005");
        var actorPlayer = gameState.players[actorPlayerId];
        actorPlayer.mana = 8;
        var opponentTeam = gameState.teams.Values.Single(team => team.teamId != actorPlayer.teamId);
        opponentTeam.killScore = 3;
        var processor = new ActionRequestProcessor();

        processor.processActionRequest(
            gameState,
            new TryResolveAnomalyActionRequest
            {
                requestId = 41002,
                actorPlayerId = actorPlayerId,
            });

        Assert.Equal(0, actorPlayer.mana);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(2, gameState.currentInputContext!.requiredPlayerIds.Count);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA004ConditionDiscardSkillPointCards, gameState.currentActionChain!.pendingContinuationKey);

        processor.processActionRequest(
            gameState,
            new SubmitInputChoiceActionRequest
            {
                requestId = 41003,
                actorPlayerId = actorPlayerId,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = "skillPointCard:" + actorPaymentCardId.Value,
            });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(actorPlayerId, gameState.currentInputContext!.submittedPlayerIds);
        Assert.Equal(3, opponentTeam.killScore);
        Assert.Null(gameState.turnState.extraTurnFlags.pendingExtraTurnForPlayerId);

        var completionEvents = processor.processActionRequest(
            gameState,
            new SubmitInputChoiceActionRequest
            {
                requestId = 41004,
                actorPlayerId = allyPlayerId,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = "skillPointCard:" + allyPaymentCardId.Value,
            });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Contains(actorPaymentCardId, gameState.zones[actorPlayer.discardZoneId].cardInstanceIds);
        Assert.Contains(allyPaymentCardId, gameState.zones[gameState.players[allyPlayerId].discardZoneId].cardInstanceIds);
        Assert.Equal(2, opponentTeam.killScore);
        Assert.Equal(actorPlayerId, gameState.turnState.extraTurnFlags.pendingExtraTurnForPlayerId);
        Assert.True(gameState.turnState.extraTurnFlags.extraTurnGrantedThisTurn);
        Assert.Contains(completionEvents, gameEvent => gameEvent is AnomalyResolvedEvent resolved && resolved.anomalyDefinitionId == "A004");

        gameState.turnState.currentPhase = TurnPhase.end;
        processor.processActionRequest(
            gameState,
            new StartNextTurnActionRequest
            {
                requestId = 41005,
                actorPlayerId = actorPlayerId,
            });

        Assert.Equal(actorPlayerId, gameState.turnState.currentPlayerId);
        Assert.True(gameState.turnState.extraTurnFlags.isCurrentTurnExtraTurn);
    }

    [Fact]
    public void A004Arrival_ShouldWaitForEveryPlayerWithDefenseCardAndReturnEachSelectedCard()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(12345);
        var seatOrder = gameState.matchMeta!.seatOrder;
        var firstPlayerId = seatOrder[0];
        var secondPlayerId = seatOrder[1];
        var firstDefenseCardId = moveFirstHandCardToDefenseField(gameState, firstPlayerId);
        var secondDefenseCardId = moveFirstHandCardToDefenseField(gameState, secondPlayerId);
        var actionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(42001),
            actorPlayerId = gameState.turnState!.currentPlayerId,
        };
        var runtime = new AnomalyA004Runtime(new ZoneMovementService(), () => 42002);

        var suspended = runtime.tryOpenArrivalInputOrComplete(gameState, actionChain, 42001);

        Assert.True(suspended);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(firstPlayerId, gameState.currentInputContext!.requiredPlayerIds);
        Assert.Contains(secondPlayerId, gameState.currentInputContext.requiredPlayerIds);

        var firstCompleted = runtime.continueParallelChoice(
            gameState,
            actionChain,
            gameState.currentInputContext,
            new SubmitInputChoiceActionRequest
            {
                requestId = 42003,
                actorPlayerId = firstPlayerId,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = "defenseCard:" + firstDefenseCardId.Value,
            });

        Assert.False(firstCompleted);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(firstDefenseCardId, gameState.zones[gameState.players[firstPlayerId].fieldZoneId].cardInstanceIds);

        var secondCompleted = runtime.continueParallelChoice(
            gameState,
            actionChain,
            gameState.currentInputContext!,
            new SubmitInputChoiceActionRequest
            {
                requestId = 42004,
                actorPlayerId = secondPlayerId,
                inputContextId = gameState.currentInputContext!.inputContextId,
                choiceKey = "defenseCard:" + secondDefenseCardId.Value,
            });

        Assert.True(secondCompleted);
        Assert.Null(gameState.currentInputContext);
        Assert.Contains(firstDefenseCardId, gameState.zones[gameState.players[firstPlayerId].handZoneId].cardInstanceIds);
        Assert.Contains(secondDefenseCardId, gameState.zones[gameState.players[secondPlayerId].handZoneId].cardInstanceIds);
        Assert.False(gameState.cardInstances[firstDefenseCardId].isDefensePlacedOnField);
        Assert.False(gameState.cardInstances[secondDefenseCardId].isDefensePlacedOnField);
    }

    [Fact]
    public void A004Arrival_WhenKaguyaHasLessThanSixCards_ShouldDrawToSixAfterDefenseStep()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(54321);
        var kaguyaPlayerId = gameState.matchMeta!.seatOrder[1];
        var kaguyaPlayer = gameState.players[kaguyaPlayerId];
        var kaguyaCharacterId = kaguyaPlayer.activeCharacterInstanceId!.Value;
        gameState.characterInstances[kaguyaCharacterId].definitionId = "C020";
        var handZone = gameState.zones[kaguyaPlayer.handZoneId];
        var discardZone = gameState.zones[kaguyaPlayer.discardZoneId];
        while (handZone.cardInstanceIds.Count > 4)
        {
            var cardId = handZone.cardInstanceIds[^1];
            handZone.cardInstanceIds.RemoveAt(handZone.cardInstanceIds.Count - 1);
            discardZone.cardInstanceIds.Add(cardId);
            gameState.cardInstances[cardId].zoneId = kaguyaPlayer.discardZoneId;
            gameState.cardInstances[cardId].zoneKey = ZoneKey.discard;
        }

        var actionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(42005),
            actorPlayerId = gameState.turnState!.currentPlayerId,
        };
        var runtime = new AnomalyA004Runtime(new ZoneMovementService(), () => 42006);

        var suspended = runtime.tryOpenArrivalInputOrComplete(gameState, actionChain, 42005);

        Assert.False(suspended);
        Assert.Null(gameState.currentInputContext);
        Assert.Equal(6, handZone.cardInstanceIds.Count);
        Assert.Equal(2, actionChain.producedEvents.OfType<CardMovedEvent>().Count(
            moved => moved.moveReason == CardMoveReason.draw && moved.toZoneKey == ZoneKey.hand));
    }

    [Fact]
    public void ForceResolveA004_ShouldGrantRewardWithoutChargingConditionCosts()
    {
        var gameState = createA004ResolveState();
        var actorPlayerId = gameState.turnState!.currentPlayerId;
        var actorPlayer = gameState.players[actorPlayerId];
        actorPlayer.mana = 3;
        var opponentTeam = gameState.teams.Values.Single(team => team.teamId != actorPlayer.teamId);
        opponentTeam.killScore = 2;
        var actionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(43001),
            actorPlayerId = actorPlayerId,
            rootActionRequest = new PlayTreasureCardActionRequest
            {
                requestId = 43001,
                actorPlayerId = actorPlayerId,
            },
        };
        gameState.currentActionChain = actionChain;
        var processor = new AnomalyProcessor(new ZoneMovementService(), () => 43002);

        processor.forceResolveCurrentAnomalyFromExternalEffect(
            gameState,
            actionChain,
            actorPlayerId,
            43001);

        Assert.Equal(3, actorPlayer.mana);
        Assert.Equal(1, opponentTeam.killScore);
        Assert.Equal(actorPlayerId, gameState.turnState.extraTurnFlags.pendingExtraTurnForPlayerId);
        Assert.Null(gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Contains(actionChain.producedEvents, gameEvent =>
            gameEvent is AnomalyResolvedEvent resolved && resolved.anomalyDefinitionId == "A004");
    }

    private static RuleCore.GameState.GameState createA004ResolveState()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(24680);
        gameState.currentInputContext = null;
        gameState.currentResponseWindow = null;
        gameState.currentActionChain = null;
        gameState.turnState!.currentPhase = TurnPhase.action;
        gameState.turnState.hasResolvedAnomalyThisTurn = false;
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        return gameState;
    }

    private static PlayerId resolveAllyPlayerId(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        return gameState.players.Values
            .Where(player => player.teamId == actorTeamId && player.playerId != actorPlayerId)
            .Select(player => player.playerId)
            .Single();
    }

    private static CardInstanceId setFirstHandCardDefinition(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string definitionId)
    {
        var player = gameState.players[playerId];
        var cardInstanceId = gameState.zones[player.handZoneId].cardInstanceIds[0];
        gameState.cardInstances[cardInstanceId].definitionId = definitionId;
        return cardInstanceId;
    }

    private static CardInstanceId moveFirstHandCardToDefenseField(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        var player = gameState.players[playerId];
        var hand = gameState.zones[player.handZoneId];
        var field = gameState.zones[player.fieldZoneId];
        var cardInstanceId = hand.cardInstanceIds[0];
        hand.cardInstanceIds.RemoveAt(0);
        field.cardInstanceIds.Add(cardInstanceId);
        var card = gameState.cardInstances[cardInstanceId];
        card.zoneId = player.fieldZoneId;
        card.zoneKey = ZoneKey.field;
        card.isDefensePlacedOnField = true;
        return cardInstanceId;
    }
}
