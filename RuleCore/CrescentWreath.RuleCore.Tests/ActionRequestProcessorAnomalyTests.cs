using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorAnomalyTests
{
    [Fact]
    public void TryResolveAnomaly_HappyPath_ShouldResolveCurrentAndFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);

        var actorCharacterId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91001,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("anomalyResolveAttempted", attemptedEvent.eventTypeKey);
        Assert.Equal("A001", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);

        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("anomalyResolved", resolvedEvent.eventTypeKey);
        Assert.Equal("A001", resolvedEvent.anomalyDefinitionId);

        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("anomalyFlipped", flippedEvent.eventTypeKey);
        Assert.Equal("A002", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(2, gameState.characterInstances[actorCharacterId].currentHp);
        Assert.Equal(2, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.Equal(0, gameState.teams[actorTeamId].leyline);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.NotNull(gameState.currentAnomalyState);
        Assert.Equal("A002", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A003", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void TryResolveAnomaly_WhenCalledTwiceInSameTurn_ShouldThrowOnSecondAttempt()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);
        var processor = new ActionRequestProcessor();

        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91011,
            actorPlayerId = actorPlayerId,
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
            {
                requestId = 91012,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("TryResolveAnomalyActionRequest can only be accepted once per turn.", exception.Message);
        Assert.Equal("A002", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
    }

    [Fact]
    public void TryResolveAnomaly_WhenResolveConditionFails_ShouldOnlyProduceAttemptedFailure()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A999";

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91014,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A999", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("unsupportedResolveCondition", attemptedEvent.failedReasonKey);

        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A999", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Equal(2, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA001AndManaInsufficient_ShouldFailWithoutConsumingGateOrChangingState()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA001SampleGameState(
            actorPlayerId,
            allyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 7,
            actorCharacterHp: 3,
            allyCharacterHp: 3,
            hasAllyActiveCharacter: true);

        var actorCharacterId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91018,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A001", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientMana", attemptedEvent.failedReasonKey);

        Assert.Equal(7, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.characterInstances[actorCharacterId].currentHp);
        Assert.Equal(3, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(10, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A001", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(2, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A002", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A003", gameState.currentAnomalyState.anomalyDeckDefinitionIds[1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA001AndFriendlyHpInsufficient_ShouldFailWithoutConsumingGateOrChangingState()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA001SampleGameState(
            actorPlayerId,
            allyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorCharacterHp: 3,
            allyCharacterHp: 1,
            hasAllyActiveCharacter: true);

        var actorCharacterId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91019,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientFriendlyHp", attemptedEvent.failedReasonKey);

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.characterInstances[actorCharacterId].currentHp);
        Assert.Equal(1, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(10, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA001AndFriendlyActiveCharacterMissing_ShouldFailWithoutConsumingGateOrChangingState()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA001SampleGameState(
            actorPlayerId,
            allyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorCharacterHp: 3,
            allyCharacterHp: 3,
            hasAllyActiveCharacter: false);

        var actorCharacterId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91020,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("activeCharacterMissing", attemptedEvent.failedReasonKey);

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.characterInstances[actorCharacterId].currentHp);
        Assert.Equal(10, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002ConditionInputCompleted_ShouldDeductManaAndFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1);

        var processor = new ActionRequestProcessor();
        var openEvents = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910171,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, openEvents.Count);
        var attemptedOpenEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(openEvents[0]);
        Assert.Equal("A002", attemptedOpenEvent.anomalyDefinitionId);
        Assert.False(attemptedOpenEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedOpenEvent.failedReasonKey);
        Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A002:conditionFriendlyDiscardFromHand", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002ConditionFriendlyDiscardFromHand, gameState.currentActionChain!.pendingContinuationKey);

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var actorSelectedCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910172,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorSelectedCardId),
        });

        Assert.Contains(actorSelectedCardId, gameState.zones[actorDiscardZoneId].cardInstanceIds);
        Assert.Equal(actorDiscardZoneId, gameState.cardInstances[actorSelectedCardId].zoneId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002ConditionFriendlyDiscardFromHand, gameState.currentActionChain!.pendingContinuationKey);

        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        var allyDiscardZoneId = gameState.players[allyPlayerId].discardZoneId;
        var allySelectedCardId = gameState.zones[allyHandZoneId].cardInstanceIds[0];
        var rewardOpenEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910173,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allySelectedCardId),
        });

        Assert.Contains(allySelectedCardId, gameState.zones[allyDiscardZoneId].cardInstanceIds);
        Assert.Equal(allyDiscardZoneId, gameState.cardInstances[allySelectedCardId].zoneId);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A002", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002RewardOptionalBanishDecision, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);

        var openedRewardWindowEvent = Assert.IsType<InteractionWindowEvent>(rewardOpenEvents[^1]);
        Assert.Equal("inputContextOpened", openedRewardWindowEvent.eventTypeKey);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A002:rewardOptionalBanishAndSakuraReplacement", gameState.currentInputContext!.contextKey);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Contains("banish1", gameState.currentInputContext.choiceKeys);
        Assert.Equal(actorPlayerId, gameState.currentInputContext.requiredPlayerId);

        var finalizeEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910174,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "decline",
        });

        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        var attemptedSuccessEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(finalizeEvents[^4]);
        Assert.Equal("A002", attemptedSuccessEvent.anomalyDefinitionId);
        Assert.True(attemptedSuccessEvent.isSucceeded);
        Assert.IsType<AnomalyResolvedEvent>(finalizeEvents[^3]);
        Assert.IsType<AnomalyFlippedEvent>(finalizeEvents[^2]);
        Assert.IsType<InteractionWindowEvent>(finalizeEvents[^1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002FriendlyHandCardsInsufficient_ShouldAttemptFailedWithoutInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 0);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910174,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A002", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientFriendlyHandCards", attemptedEvent.failedReasonKey);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002ActorIsYoumu_ShouldSkipActorAndRequireAllyDiscard()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 1);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C005";

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910181,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A002", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedEvent.failedReasonKey);
        Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002ConditionFriendlyDiscardFromHand, gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002AllFriendlyAreYoumu_ShouldResolveWithoutConditionInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 0);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C005";
        gameState.characterInstances[allyCharacterInstanceId].definitionId = "C005";

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910182,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A002", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.IsType<AnomalyFlippedEvent>(events[2]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[3]);
        Assert.Equal("inputContextOpened", openedEvent.eventTypeKey);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002OnlyYoumuAllyHasNoHand_ShouldRequireActorThenResolve()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 0);

        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[allyCharacterInstanceId].definitionId = "C005";

        var processor = new ActionRequestProcessor();
        var openEvents = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910183,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, openEvents.Count);
        var attemptedOpenEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(openEvents[0]);
        Assert.Equal("A002", attemptedOpenEvent.anomalyDefinitionId);
        Assert.False(attemptedOpenEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedOpenEvent.failedReasonKey);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var actorSelectedCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        var rewardOpenEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910184,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorSelectedCardId),
        });

        Assert.Contains(actorSelectedCardId, gameState.zones[actorDiscardZoneId].cardInstanceIds);
        Assert.Equal(actorDiscardZoneId, gameState.cardInstances[actorSelectedCardId].zoneId);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A002", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002RewardOptionalBanishDecision, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.IsType<InteractionWindowEvent>(rewardOpenEvents[^1]);
        Assert.NotNull(gameState.currentInputContext);

        var finalizeEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910185,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "decline",
        });

        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        var attemptedSuccessEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(finalizeEvents[^4]);
        Assert.Equal("A002", attemptedSuccessEvent.anomalyDefinitionId);
        Assert.True(attemptedSuccessEvent.isSucceeded);
        Assert.IsType<AnomalyResolvedEvent>(finalizeEvents[^3]);
        Assert.IsType<AnomalyFlippedEvent>(finalizeEvents[^2]);
        Assert.IsType<InteractionWindowEvent>(finalizeEvents[^1]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002ConditionActorMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910175,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorSelectedCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 910176,
                actorPlayerId = allyPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = createA002ConditionChoiceKey(actorSelectedCardId),
            }));

        Assert.Equal("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002ConditionChoiceKeyIsInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910177,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 910178,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = "invalid",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA002ConditionFriendlyDiscardFromHand.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002ConditionSelectedCardNoLongerInHand_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910179,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var selectedCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        gameState.zones[actorHandZoneId].cardInstanceIds.Remove(selectedCardId);
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(selectedCardId);
        gameState.cardInstances[selectedCardId].zoneId = actorDiscardZoneId;
        gameState.cardInstances[selectedCardId].zoneKey = ZoneKey.discard;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 910180,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = createA002ConditionChoiceKey(selectedCardId),
            }));

        Assert.Equal("A002 anomaly condition continuation requires selected card to still be in required player hand zone.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002RewardBanishOneThenDeclineReplacement_ShouldFinalizeWithBanishOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 2);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910190,
            actorPlayerId = actorPlayerId,
        });

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorSelectedCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910191,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorSelectedCardId),
        });

        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        var allySelectedCardId = gameState.zones[allyHandZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910192,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allySelectedCardId),
        });

        Assert.Equal("anomaly:A002:rewardOptionalBanishAndSakuraReplacement", gameState.currentInputContext!.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA002RewardOptionalBanishDecision, gameState.currentActionChain!.pendingContinuationKey);

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910193,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "banish1",
        });

        Assert.Equal(AnomalyProcessor.ContinuationKeyA002RewardSelectFirstBanishCard, gameState.currentActionChain!.pendingContinuationKey);
        var banishSelectedCardId = gameState.zones[gameState.players[actorPlayerId].discardZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910194,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002RewardBanishChoiceKey(banishSelectedCardId),
        });

        Assert.Equal(AnomalyProcessor.ContinuationKeyA002RewardOptionalSakuraReplacement, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("accept", gameState.currentInputContext!.choiceKeys);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);

        var finalizeEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910195,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        var gapZoneId = gameState.publicState!.gapZoneId;
        Assert.Contains(banishSelectedCardId, gameState.zones[gapZoneId].cardInstanceIds);
        Assert.Equal(gapZoneId, gameState.cardInstances[banishSelectedCardId].zoneId);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        var attemptedSuccessEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(finalizeEvents[^4]);
        Assert.True(attemptedSuccessEvent.isSucceeded);
        Assert.IsType<AnomalyResolvedEvent>(finalizeEvents[^3]);
        Assert.IsType<AnomalyFlippedEvent>(finalizeEvents[^2]);
        Assert.IsType<InteractionWindowEvent>(finalizeEvents[^1]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002RewardBanishOneThenAcceptReplacement_ShouldSummonSakuraToActorDiscard()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910196,
            actorPlayerId = actorPlayerId,
        });

        var actorSelectedCardId = gameState.zones[gameState.players[actorPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910197,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorSelectedCardId),
        });

        var allySelectedCardId = gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910198,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allySelectedCardId),
        });

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910199,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "banish1",
        });

        var banishSelectedCardId = gameState.zones[gameState.players[actorPlayerId].discardZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910200,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002RewardBanishChoiceKey(banishSelectedCardId),
        });

        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var discardCountBeforeReplacement = gameState.zones[actorDiscardZoneId].cardInstanceIds.Count;
        var finalizeEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910201,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(discardCountBeforeReplacement + 1, gameState.zones[actorDiscardZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[gameState.publicState!.sakuraCakeDeckZoneId].cardInstanceIds);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.IsType<AnomalyResolveAttemptedEvent>(finalizeEvents[^4]);
        Assert.IsType<AnomalyResolvedEvent>(finalizeEvents[^3]);
        Assert.IsType<AnomalyFlippedEvent>(finalizeEvents[^2]);
        Assert.IsType<InteractionWindowEvent>(finalizeEvents[^1]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002RewardBanishTwoThenAcceptReplacement_ShouldSummonByMinBanishAndDeckRemaining()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910202,
            actorPlayerId = actorPlayerId,
        });

        var actorFirstPaymentCardId = gameState.zones[gameState.players[actorPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910203,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorFirstPaymentCardId),
        });

        var allyPaymentCardId = gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910204,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allyPaymentCardId),
        });

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910205,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "banish2",
        });

        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var firstBanishCardId = gameState.zones[actorDiscardZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910206,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002RewardBanishChoiceKey(firstBanishCardId),
        });

        var secondBanishCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910207,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002RewardBanishChoiceKey(secondBanishCardId),
        });

        var actorDiscardCountBeforeReplacement = gameState.zones[actorDiscardZoneId].cardInstanceIds.Count;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910208,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var gapZoneId = gameState.publicState!.gapZoneId;
        Assert.Contains(firstBanishCardId, gameState.zones[gapZoneId].cardInstanceIds);
        Assert.Contains(secondBanishCardId, gameState.zones[gapZoneId].cardInstanceIds);
        Assert.Equal(actorDiscardCountBeforeReplacement + 1, gameState.zones[actorDiscardZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[gameState.publicState.sakuraCakeDeckZoneId].cardInstanceIds);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA002ConditionCompletedButActorHasNoBanishEligibleCards_ShouldFinalizeWithoutRewardInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 2);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C005";

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910209,
            actorPlayerId = actorPlayerId,
        });

        var allyPaymentCardId = gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds[0];
        var completeEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910210,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allyPaymentCardId),
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.Equal("A003", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.IsType<AnomalyResolveAttemptedEvent>(completeEvents[^4]);
        Assert.IsType<AnomalyResolvedEvent>(completeEvents[^3]);
        Assert.IsType<AnomalyFlippedEvent>(completeEvents[^2]);
        Assert.IsType<InteractionWindowEvent>(completeEvents[^1]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002RewardSelectedCardNoLongerInHandOrDiscard_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 1,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910211,
            actorPlayerId = actorPlayerId,
        });

        var actorPaymentCardId = gameState.zones[gameState.players[actorPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910212,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorPaymentCardId),
        });

        var allyPaymentCardId = gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910213,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allyPaymentCardId),
        });

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910214,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "banish1",
        });

        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var actorFieldZoneId = gameState.players[actorPlayerId].fieldZoneId;
        var selectedCardId = gameState.zones[actorDiscardZoneId].cardInstanceIds[0];
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Remove(selectedCardId);
        gameState.zones[actorFieldZoneId].cardInstanceIds.Add(selectedCardId);
        gameState.cardInstances[selectedCardId].zoneId = actorFieldZoneId;
        gameState.cardInstances[selectedCardId].zoneKey = ZoneKey.field;

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 910215,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = createA002RewardBanishChoiceKey(selectedCardId),
            }));

        Assert.Equal("A002 anomaly reward continuation requires selected card to still be in actor hand or discard zone.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void SubmitInputChoice_WhenA002RewardSecondBanishRepeatsFirstCard_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA002ConditionFlowGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 1,
            sakuraCakeDeckCardCount: 1);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910216,
            actorPlayerId = actorPlayerId,
        });

        var actorPaymentCardId = gameState.zones[gameState.players[actorPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910217,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(actorPaymentCardId),
        });

        var allyPaymentCardId = gameState.zones[gameState.players[allyPlayerId].handZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910218,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002ConditionChoiceKey(allyPaymentCardId),
        });

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910219,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "banish2",
        });

        var firstBanishCardId = gameState.zones[gameState.players[actorPlayerId].discardZoneId].cardInstanceIds[0];
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910220,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createA002RewardBanishChoiceKey(firstBanishCardId),
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 910221,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = createA002RewardBanishChoiceKey(firstBanishCardId),
            }));

        Assert.Equal("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA003AndManaSufficient_ShouldDeductManaApplyRewardAndFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA003SampleGameState(
            actorPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorTeamLeyline: 3,
            enemyTeamKillScore: 1);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91017,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);

        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A003", resolvedEvent.anomalyDefinitionId);

        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A004", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(5, gameState.teams[actorTeamId].leyline);
        Assert.Equal(0, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A004", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Empty(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA003AndManaInsufficient_ShouldFailWithoutConsumingGateOrChangingState()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA003SampleGameState(
            actorPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 7,
            actorTeamLeyline: 4,
            enemyTeamKillScore: 9);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91023,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A003", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientMana", attemptedEvent.failedReasonKey);

        Assert.Equal(7, gameState.players[actorPlayerId].mana);
        Assert.Equal(4, gameState.teams[actorTeamId].leyline);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A003", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A004", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA007WithOpponentTarget_ShouldApplyCharmAndFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA007SampleGameState(
            actorPlayerId,
            allyPlayerId,
            targetOpponentPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91024,
            actorPlayerId = actorPlayerId,
            targetPlayerId = targetOpponentPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A007", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);

        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A007", resolvedEvent.anomalyDefinitionId);

        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A008", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, targetOpponentPlayerId, "Charm"));
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, allyPlayerId, "Charm"));
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A008", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A009", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA007FlipsToA008AndLeadingTeamContainsC009_ShouldSkipC009ButSealOtherLeadingCharacters()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA007SampleGameState(
            actorPlayerId,
            allyPlayerId,
            targetOpponentPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8);

        gameState.players[allyPlayerId].teamId = enemyTeamId;
        gameState.teams[actorTeamId].memberPlayerIds.Clear();
        gameState.teams[actorTeamId].memberPlayerIds.Add(actorPlayerId);
        gameState.teams[enemyTeamId].memberPlayerIds.Add(allyPlayerId);

        gameState.characterInstances[gameState.players[targetOpponentPlayerId].activeCharacterInstanceId!.Value].definitionId = "C009";

        gameState.teams[actorTeamId].killScore = 4;
        gameState.teams[enemyTeamId].killScore = 7;

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        var targetOpponentCharacterInstanceId = gameState.players[targetOpponentPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910241,
            actorPlayerId = actorPlayerId,
            targetPlayerId = targetOpponentPlayerId,
        });

        Assert.Equal(3, events.Count);
        Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A008", flippedEvent.anomalyDefinitionId);

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, targetOpponentCharacterInstanceId, "Seal"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, actorCharacterInstanceId, "Seal"));
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, allyCharacterInstanceId, "Seal"));
    }

    [Fact]
    public void TryResolveAnomaly_WhenA007WithoutTarget_ShouldFailWithAttemptedOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA007SampleGameState(
            actorPlayerId,
            allyPlayerId,
            targetOpponentPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91025,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A007", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("targetPlayerRequired", attemptedEvent.failedReasonKey);

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, targetOpponentPlayerId, "Charm"));
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A007", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(2, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A008", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A009", gameState.currentAnomalyState.anomalyDeckDefinitionIds[1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA007TargetIsFriendly_ShouldFailWithAttemptedOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA007SampleGameState(
            actorPlayerId,
            allyPlayerId,
            targetOpponentPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91026,
            actorPlayerId = actorPlayerId,
            targetPlayerId = allyPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A007", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("targetPlayerMustBeOpponent", attemptedEvent.failedReasonKey);

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, allyPlayerId, "Charm"));
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A007", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(2, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A008", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A009", gameState.currentAnomalyState.anomalyDeckDefinitionIds[1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008WithOpponentTarget_ShouldApplyTeamDeltaAndShackleAndFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            targetOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A010");
        gameState.teams[enemyTeamId].killScore = 3;

        var targetCharacterInstanceId = gameState.players[targetOpponentPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91027,
            actorPlayerId = actorPlayerId,
            targetPlayerId = targetOpponentPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);

        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A008", resolvedEvent.anomalyDefinitionId);

        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(2, gameState.teams[enemyTeamId].killScore);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Shackle"));
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A009", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A010", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008WithoutTarget_ShouldSucceedWithTeamDeltaOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            targetOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A010");
        gameState.teams[enemyTeamId].killScore = 4;

        var targetCharacterInstanceId = gameState.players[targetOpponentPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91028,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A008", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.teams[enemyTeamId].killScore);
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Shackle"));
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A009", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A010", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008TargetIsFriendly_ShouldSucceedWithTeamDeltaOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            targetOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A010");

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91029,
            actorPlayerId = actorPlayerId,
            targetPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A008", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, actorCharacterInstanceId, "Shackle"));
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A009", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A010", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008FlipsToA009_ShouldExecuteA009ArrivalBanishSummonZoneToGapAfterFlipped()
    {
        var actorPlayerId = new PlayerId(1);
        var targetOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            targetOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            sakuraCakeCount: 0,
            summonZoneDefinitionIds: new[] { "starter:magicCircuit", "test-summon-card" });

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A010");

        var summonZoneId = gameState.publicState!.summonZoneId;
        var gapZoneId = gameState.publicState.gapZoneId;
        var summonZoneCardsBeforeFlip = new List<CardInstanceId>(gameState.zones[summonZoneId].cardInstanceIds);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910291,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(5, events.Count);
        Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);

        var movedEventA = Assert.IsType<CardMovedEvent>(events[3]);
        var movedEventB = Assert.IsType<CardMovedEvent>(events[4]);
        Assert.Equal(CardMoveReason.banish, movedEventA.moveReason);
        Assert.Equal(CardMoveReason.banish, movedEventB.moveReason);
        Assert.Equal(ZoneKey.summonZone, movedEventA.fromZoneKey);
        Assert.Equal(ZoneKey.gapZone, movedEventA.toZoneKey);
        Assert.Equal(ZoneKey.summonZone, movedEventB.fromZoneKey);
        Assert.Equal(ZoneKey.gapZone, movedEventB.toZoneKey);

        Assert.Empty(gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(2, gameState.zones[gapZoneId].cardInstanceIds.Count);
        foreach (var movedCardId in summonZoneCardsBeforeFlip)
        {
            Assert.Contains(movedCardId, gameState.zones[gapZoneId].cardInstanceIds);
            Assert.Equal(gapZoneId, gameState.cardInstances[movedCardId].zoneId);
            Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[movedCardId].zoneKey);
        }
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008AndOpponentDiscardHasCards_ShouldOpenConditionInputAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");

        var discardCardId = addCardToPlayerDiscardZone(gameState, firstOpponentPlayerId, "test-summon-card", 999801);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91201,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedEvent.failedReasonKey);

        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.Equal("inputContextOpened", openedEvent.eventTypeKey);
        Assert.True(openedEvent.isOpened);

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A008:conditionOpponentOptionalDiscardReturn", gameState.currentInputContext.contextKey);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Contains(createA008ConditionDiscardChoiceKey(discardCardId), gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA008ConditionOpponentOptionalDiscardReturn, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008ConditionSelectsDiscardCard_ShouldMoveDiscardToHandThenResolveAndFlip()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.teams[opponentTeamId].killScore = 4;

        var discardCardId = addCardToPlayerDiscardZone(gameState, firstOpponentPlayerId, "test-summon-card", 999811);
        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91202,
            actorPlayerId = actorPlayerId,
            targetPlayerId = firstOpponentPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91203,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = inputContextId,
            choiceKey = createA008ConditionDiscardChoiceKey(discardCardId),
        });

        var firstOpponentDiscardZoneId = gameState.players[firstOpponentPlayerId].discardZoneId;
        var firstOpponentHandZoneId = gameState.players[firstOpponentPlayerId].handZoneId;
        Assert.DoesNotContain(discardCardId, gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds);
        Assert.Contains(discardCardId, gameState.zones[firstOpponentHandZoneId].cardInstanceIds);
        Assert.Equal(firstOpponentHandZoneId, gameState.cardInstances[discardCardId].zoneId);
        Assert.Equal(ZoneKey.hand, gameState.cardInstances[discardCardId].zoneKey);

        Assert.Contains(events, gameEvent =>
            gameEvent is CardMovedEvent movedEvent &&
            movedEvent.cardInstanceId == discardCardId &&
            movedEvent.fromZoneKey == ZoneKey.discard &&
            movedEvent.toZoneKey == ZoneKey.hand);

        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[^3]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[^2]);
        Assert.Equal("A008", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[^1]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.teams[opponentTeamId].killScore);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Shackle"));
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008ConditionDeclines_ShouldResolveWithoutDiscardMove()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");

        var discardCardId = addCardToPlayerDiscardZone(gameState, firstOpponentPlayerId, "test-summon-card", 999821);
        var firstOpponentDiscardZoneId = gameState.players[firstOpponentPlayerId].discardZoneId;
        var firstOpponentHandZoneId = gameState.players[firstOpponentPlayerId].handZoneId;

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91204,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91205,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = inputContextId,
            choiceKey = "decline",
        });

        Assert.Contains(discardCardId, gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds);
        Assert.DoesNotContain(discardCardId, gameState.zones[firstOpponentHandZoneId].cardInstanceIds);
        Assert.Equal(firstOpponentDiscardZoneId, gameState.cardInstances[discardCardId].zoneId);
        Assert.Equal(ZoneKey.discard, gameState.cardInstances[discardCardId].zoneKey);
        Assert.DoesNotContain(events, gameEvent =>
            gameEvent is CardMovedEvent movedEvent &&
            movedEvent.cardInstanceId == discardCardId &&
            movedEvent.fromZoneKey == ZoneKey.discard &&
            movedEvent.toZoneKey == ZoneKey.hand);

        Assert.IsType<AnomalyResolveAttemptedEvent>(events[^3]);
        Assert.IsType<AnomalyResolvedEvent>(events[^2]);
        Assert.IsType<AnomalyFlippedEvent>(events[^1]);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008FirstOpponentProcessed_ShouldOpenSecondOpponentInput()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");

        var firstOpponentDiscardCardId = addCardToPlayerDiscardZone(gameState, firstOpponentPlayerId, "test-summon-card", 999831);
        var secondOpponentDiscardCardId = addCardToPlayerDiscardZone(gameState, secondOpponentPlayerId, "test-summon-card", 999832);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91206,
            actorPlayerId = actorPlayerId,
        });

        var firstInputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91207,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = firstInputContextId,
            choiceKey = "decline",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A008:conditionOpponentOptionalDiscardReturn", gameState.currentInputContext.contextKey);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Contains(createA008ConditionDiscardChoiceKey(secondOpponentDiscardCardId), gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain(events, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(events, gameEvent => gameEvent is AnomalyFlippedEvent);
        Assert.Contains(firstOpponentDiscardCardId, gameState.zones[gameState.players[firstOpponentPlayerId].discardZoneId].cardInstanceIds);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008ConditionChoiceInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");

        _ = addCardToPlayerDiscardZone(gameState, firstOpponentPlayerId, "test-summon-card", 999841);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91208,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var inputContextId = inputContextBefore.inputContextId;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91209,
                actorPlayerId = firstOpponentPlayerId,
                inputContextId = inputContextId,
                choiceKey = "discardCard:123456789",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA008ConditionOpponentOptionalDiscardReturn.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008AllOpponentDiscardEmpty_ShouldResolveDirectlyWithoutConditionInput()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.teams[opponentTeamId].killScore = 5;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91210,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(4, gameState.teams[opponentTeamId].killScore);
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA008AllOpponentDiscardEmptyAndFriendlyC009Exists_ShouldOpenRyougiRewardInputAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        gameState.teams[opponentTeamId].killScore = 5;
        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C009";

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91211,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A008", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.Equal("inputContextOpened", openedEvent.eventTypeKey);
        Assert.True(openedEvent.isOpened);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(4, gameState.teams[opponentTeamId].killScore);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A008:rewardRyougiOptionalDrawOne", gameState.currentInputContext.contextKey);
        Assert.Contains("accept", gameState.currentInputContext.choiceKeys);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA008RewardRyougiOptionalDrawOne, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008RyougiRewardAccept_ShouldDrawOneThenResolvedAndFlipped()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C009";
        var drawCardId = addCardToPlayerDiscardZone(gameState, actorPlayerId, "starter:magicCircuit", 999851);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91212,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91213,
            actorPlayerId = actorPlayerId,
            inputContextId = inputContextId,
            choiceKey = "accept",
        });

        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        Assert.DoesNotContain(drawCardId, gameState.zones[actorDiscardZoneId].cardInstanceIds);
        Assert.Contains(drawCardId, gameState.zones[actorHandZoneId].cardInstanceIds);
        Assert.Equal(actorHandZoneId, gameState.cardInstances[drawCardId].zoneId);
        Assert.Equal(ZoneKey.hand, gameState.cardInstances[drawCardId].zoneKey);

        Assert.Contains(events, gameEvent =>
            gameEvent is CardMovedEvent movedEvent &&
            movedEvent.cardInstanceId == drawCardId &&
            movedEvent.fromZoneKey == ZoneKey.discard &&
            movedEvent.toZoneKey == ZoneKey.deck &&
            movedEvent.moveReason == CardMoveReason.returnToSource);
        Assert.Contains(events, gameEvent =>
            gameEvent is CardMovedEvent movedEvent &&
            movedEvent.cardInstanceId == drawCardId &&
            movedEvent.fromZoneKey == ZoneKey.deck &&
            movedEvent.toZoneKey == ZoneKey.hand &&
            movedEvent.moveReason == CardMoveReason.draw);

        var successfulAttemptedEventCount = 0;
        foreach (var gameEvent in events)
        {
            if (gameEvent is AnomalyResolveAttemptedEvent anomalyResolveAttemptedEvent &&
                anomalyResolveAttemptedEvent.anomalyDefinitionId == "A008" &&
                anomalyResolveAttemptedEvent.isSucceeded)
            {
                successfulAttemptedEventCount++;
            }
        }

        Assert.Equal(1, successfulAttemptedEventCount);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[^2]);
        Assert.Equal("A008", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[^1]);
        Assert.Equal("A009", flippedEvent.anomalyDefinitionId);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008RyougiRewardDecline_ShouldFinalizeWithoutDraw()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C009";
        var drawCardId = addCardToPlayerDiscardZone(gameState, actorPlayerId, "starter:magicCircuit", 999861);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91214,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91215,
            actorPlayerId = actorPlayerId,
            inputContextId = inputContextId,
            choiceKey = "decline",
        });

        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        Assert.Contains(drawCardId, gameState.zones[actorDiscardZoneId].cardInstanceIds);
        Assert.Equal(actorDiscardZoneId, gameState.cardInstances[drawCardId].zoneId);
        Assert.Equal(ZoneKey.discard, gameState.cardInstances[drawCardId].zoneKey);
        Assert.DoesNotContain(events, gameEvent =>
            gameEvent is CardMovedEvent movedEvent &&
            movedEvent.moveReason == CardMoveReason.draw);

        Assert.IsType<AnomalyResolvedEvent>(events[^2]);
        Assert.IsType<AnomalyFlippedEvent>(events[^1]);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008RyougiRewardRequiredPlayerMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C009";

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91216,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var inputContextId = inputContextBefore.inputContextId;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91217,
                actorPlayerId = firstOpponentPlayerId,
                inputContextId = inputContextId,
                choiceKey = "accept",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA008RyougiRewardContextKeyMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A008";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A009");
        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[actorCharacterInstanceId].definitionId = "C009";

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91218,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        gameState.currentInputContext!.contextKey = "anomaly:A008:invalid";
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91219,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = "accept",
            }));

        Assert.Equal("A008 anomaly reward continuation requires currentInputContext.contextKey to be anomaly:A008:rewardRyougiOptionalDrawOne.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A008:invalid", gameState.currentInputContext!.contextKey);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA009AndManaSufficient_ShouldOpenOpponentConditionInputAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91034,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A009", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedEvent.failedReasonKey);

        var inputOpenedEvent = Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.Equal("inputContextOpened", inputOpenedEvent.eventTypeKey);
        Assert.True(inputOpenedEvent.isOpened);

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A009:conditionOpponentOptionalBenefit", gameState.currentInputContext.contextKey);
        Assert.Contains("accept", gameState.currentInputContext.choiceKeys);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA009ConditionOpponentOptionalBenefit, gameState.currentActionChain!.pendingContinuationKey);

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A009", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Single(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal("A010", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009OpponentAccepts_ShouldApplyBarrierAndAdvanceToSameOpponentSakuraStage()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91035,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91036,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(4, events.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[2]);
        Assert.False(closedEvent.isOpened);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[3]);
        Assert.True(openedEvent.isOpened);

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(sakuraCakeDeckCountBefore, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA009ConditionOpponentOptionalSakura", gameState.currentInputContext.inputTypeKey);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009OpponentDeclines_ShouldAdvanceToSameOpponentSakuraStageWithoutSideEffects()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91037,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91038,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(4, events.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[2]);
        Assert.False(closedEvent.isOpened);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[3]);
        Assert.True(openedEvent.isOpened);

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(sakuraCakeDeckCountBefore, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA009ConditionOpponentOptionalSakura", gameState.currentInputContext.inputTypeKey);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009FirstOpponentSakuraStepCompletes_ShouldAdvanceToNextOpponentBarrierStep()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 9103891,
            actorPlayerId = actorPlayerId,
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9103892,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        var firstSakuraEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9103893,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(6, firstSakuraEvents.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(firstSakuraEvents[4]);
        Assert.False(closedEvent.isOpened);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(firstSakuraEvents[5]);
        Assert.True(openedEvent.isOpened);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA009ConditionOpponentOptionalBenefit", gameState.currentInputContext.inputTypeKey);
        Assert.DoesNotContain(firstSakuraEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(firstSakuraEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009SecondOpponentBarrierStepCompletes_ShouldAdvanceToSecondOpponentSakuraStep()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 9103894,
            actorPlayerId = actorPlayerId,
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9103895,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9103896,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        var secondBarrierEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9103897,
            actorPlayerId = secondOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(8, secondBarrierEvents.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(secondBarrierEvents[6]);
        Assert.False(closedEvent.isOpened);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(secondBarrierEvents[7]);
        Assert.True(openedEvent.isOpened);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA009ConditionOpponentOptionalSakura", gameState.currentInputContext.inputTypeKey);
        Assert.DoesNotContain(secondBarrierEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(secondBarrierEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
    }

    [Fact]
    public void A009_WhenSingleOpponentBarrierAcceptSakuraDecline_ShouldApplyOnlyBarrierAndFinalize()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var firstOpponentDiscardZoneId = gameState.players[firstOpponentPlayerId].discardZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;
        var firstOpponentDiscardCountBefore = gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910381,
            actorPlayerId = actorPlayerId,
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910382,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var secondStageEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910383,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(8, secondStageEvents.Count);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(sakuraCakeDeckCountBefore, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.Equal(firstOpponentDiscardCountBefore, gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void A009_WhenSingleOpponentBarrierDeclineSakuraAccept_ShouldMoveOnlySakuraAndFinalize()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var firstOpponentDiscardZoneId = gameState.players[firstOpponentPlayerId].discardZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;
        var firstOpponentDiscardCountBefore = gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910384,
            actorPlayerId = actorPlayerId,
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910385,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        var secondStageEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910386,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(9, secondStageEvents.Count);
        var sakuraMovedEvent = Assert.IsType<CardMovedEvent>(secondStageEvents[5]);
        Assert.Equal(ZoneKey.sakuraCakeDeck, sakuraMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, sakuraMovedEvent.toZoneKey);
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(sakuraCakeDeckCountBefore - 1, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.Equal(firstOpponentDiscardCountBefore + 1, gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void A009_WhenSingleOpponentBarrierDeclineSakuraDecline_ShouldFinalizeWithoutOptionalBenefits()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var firstOpponentDiscardZoneId = gameState.players[firstOpponentPlayerId].discardZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;
        var firstOpponentDiscardCountBefore = gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 910387,
            actorPlayerId = actorPlayerId,
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910388,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        var secondStageEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910389,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(8, secondStageEvents.Count);
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(sakuraCakeDeckCountBefore, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.Equal(firstOpponentDiscardCountBefore, gameState.zones[firstOpponentDiscardZoneId].cardInstanceIds.Count);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA009AndManaInsufficient_ShouldFailWithoutInputAndWithoutGateConsume()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 7,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91039,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A009", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientMana", attemptedEvent.failedReasonKey);

        Assert.Null(gameState.currentInputContext);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(7, gameState.players[actorPlayerId].mana);
        Assert.Equal("A009", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009ChoiceInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91040,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        var sakuraCakeDeckZoneId = gameState.publicState!.sakuraCakeDeckZoneId;
        var sakuraCakeDeckCountBefore = gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91041,
                actorPlayerId = firstOpponentPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = "invalid-choice",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA009ConditionOpponentOptionalBenefit.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(sakuraCakeDeckCountBefore, gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenAnomalyContinuationKeyUnknown_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91140,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        gameState.currentActionChain!.pendingContinuationKey = "continuation:anomaly:unknown";
        var pendingContinuationBefore = gameState.currentActionChain.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91141,
                actorPlayerId = firstOpponentPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = "accept",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest pendingContinuationKey is not a supported anomaly continuation.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009RequiredPlayerMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91095,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91096,
                actorPlayerId = secondOpponentPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = "accept",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009CurrentInputContextMissing_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91097,
            actorPlayerId = actorPlayerId,
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;
        gameState.currentInputContext = null;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91098,
                actorPlayerId = firstOpponentPlayerId,
                inputContextId = inputContextId,
                choiceKey = "accept",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires an active currentInputContext.", exception.Message);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void A009_WhenSakuraCakeDeckEmpty_OpponentAccepts_ShouldStillProceedWithoutFailure()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);

        var firstOpponentCharacterId = gameState.players[firstOpponentPlayerId].activeCharacterInstanceId!.Value;
        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91042,
            actorPlayerId = actorPlayerId,
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91043,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 910431,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(8, events.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[4]);
        Assert.False(closedEvent.isOpened);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[5]);
        Assert.True(attemptedEvent.isSucceeded);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[6]);
        Assert.Equal("A009", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[7]);
        Assert.Equal("A010", flippedEvent.anomalyDefinitionId);

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, firstOpponentCharacterId, "Barrier"));
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A010", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Empty(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void A009_WhenAllOpponentsProcessed_ShouldDeductManaThenAttemptedSuccessResolvedFlippedAndConsumeGate()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 2);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91044,
            actorPlayerId = actorPlayerId,
        });

        var firstInputContextId = gameState.currentInputContext!.inputContextId;
        var firstBarrierEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91045,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = firstInputContextId,
            choiceKey = "decline",
        });
        Assert.Equal(4, firstBarrierEvents.Count);
        Assert.DoesNotContain(firstBarrierEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(firstBarrierEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);

        var firstSakuraEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91046,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });
        Assert.Equal(6, firstSakuraEvents.Count);
        Assert.DoesNotContain(firstSakuraEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(firstSakuraEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);

        var secondBarrierEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91047,
            actorPlayerId = secondOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });
        Assert.Equal(8, secondBarrierEvents.Count);
        Assert.DoesNotContain(secondBarrierEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(secondBarrierEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);

        var secondSakuraEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91048,
            actorPlayerId = secondOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "decline",
        });

        Assert.Equal(12, secondSakuraEvents.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(secondSakuraEvents[8]);
        Assert.False(closedEvent.isOpened);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(secondSakuraEvents[9]);
        Assert.True(attemptedEvent.isSucceeded);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(secondSakuraEvents[10]);
        Assert.Equal("A009", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(secondSakuraEvents[11]);
        Assert.Equal("A010", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A010", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Empty(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void A009_WhenGapTreasureRewardTargetExists_ShouldOpenRewardInputThenMoveSelectedCardAndFinalize()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1,
            gapZoneDefinitionIds: new[] { "test-summon-card", "test:summon-cost-8" });

        var selectedGapCardId = gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds[0];
        var selectedChoiceKey = createA009RewardSelectGapChoiceKey(selectedGapCardId);
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91142,
            actorPlayerId = actorPlayerId,
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91143,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var conditionSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91144,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(7, conditionSubmitEvents.Count);
        var conditionClosedEvent = Assert.IsType<InteractionWindowEvent>(conditionSubmitEvents[4]);
        Assert.False(conditionClosedEvent.isOpened);
        var sakuraMovedEvent = Assert.IsType<CardMovedEvent>(conditionSubmitEvents[5]);
        Assert.Equal(ZoneKey.sakuraCakeDeck, sakuraMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, sakuraMovedEvent.toZoneKey);
        var rewardOpenedEvent = Assert.IsType<InteractionWindowEvent>(conditionSubmitEvents[6]);
        Assert.True(rewardOpenedEvent.isOpened);

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A009:selectGapTreasureToHand", gameState.currentInputContext.contextKey);
        Assert.Single(gameState.currentInputContext.choiceKeys);
        Assert.Contains(selectedChoiceKey, gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA009SelectGapTreasureToHand, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);

        var rewardInputContextId = gameState.currentInputContext.inputContextId;
        var rewardSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91145,
            actorPlayerId = actorPlayerId,
            inputContextId = rewardInputContextId,
            choiceKey = selectedChoiceKey,
        });

        Assert.Equal(12, rewardSubmitEvents.Count);
        var rewardClosedEvent = Assert.IsType<InteractionWindowEvent>(rewardSubmitEvents[7]);
        Assert.False(rewardClosedEvent.isOpened);
        var gapMovedEvent = Assert.IsType<CardMovedEvent>(rewardSubmitEvents[8]);
        Assert.Equal(ZoneKey.gapZone, gapMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, gapMovedEvent.toZoneKey);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(rewardSubmitEvents[9]);
        Assert.True(attemptedEvent.isSucceeded);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(rewardSubmitEvents[10]);
        Assert.Equal("A009", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(rewardSubmitEvents[11]);
        Assert.Equal("A010", flippedEvent.anomalyDefinitionId);

        Assert.Equal(actorHandZoneId, gameState.cardInstances[selectedGapCardId].zoneId);
        Assert.Contains(selectedGapCardId, gameState.zones[actorHandZoneId].cardInstanceIds);
        Assert.DoesNotContain(selectedGapCardId, gameState.zones[gameState.publicState.gapZoneId].cardInstanceIds);
        Assert.True(gameState.turnState.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009RewardSelectedCardNotInGapZone_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1,
            gapZoneDefinitionIds: new[] { "test-summon-card" });

        var selectedGapCardId = gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds[0];
        var selectedChoiceKey = createA009RewardSelectGapChoiceKey(selectedGapCardId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91145,
            actorPlayerId = actorPlayerId,
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91146,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 911461,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var gapZoneId = gameState.publicState.gapZoneId;
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        gameState.zones[gapZoneId].cardInstanceIds.Remove(selectedGapCardId);
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(selectedGapCardId);
        gameState.cardInstances[selectedGapCardId].zoneId = actorDiscardZoneId;
        gameState.cardInstances[selectedGapCardId].zoneKey = ZoneKey.discard;

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91147,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = selectedChoiceKey,
            }));

        Assert.Equal("A009 anomaly reward continuation requires selected card to still be in gap zone.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA009RewardSelectedCardSummonCostGreaterThanSeven_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1,
            gapZoneDefinitionIds: new[] { "test-summon-card" });

        var selectedGapCardId = gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds[0];
        var selectedChoiceKey = createA009RewardSelectGapChoiceKey(selectedGapCardId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91148,
            actorPlayerId = actorPlayerId,
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91149,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 911491,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        gameState.cardInstances[selectedGapCardId].definitionId = "test:summon-cost-8";

        var inputContextBefore = gameState.currentInputContext!;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91150,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKey = selectedChoiceKey,
            }));

        Assert.Equal("A009 anomaly reward continuation requires selected card summon cost to be less than or equal to 7.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextBefore.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void A009_WhenGapZoneHasNoEligibleTreasureTarget_ShouldSkipRewardInputAndFinalize()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId: null,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 1,
            gapZoneDefinitionIds: new[] { "starter:magicCircuit", "test:summon-cost-8" });

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91151,
            actorPlayerId = actorPlayerId,
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 911511,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91152,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "accept",
        });

        Assert.Equal(9, events.Count);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[4]);
        Assert.False(closedEvent.isOpened);
        var movedSakuraEvent = Assert.IsType<CardMovedEvent>(events[5]);
        Assert.Equal(ZoneKey.sakuraCakeDeck, movedSakuraEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, movedSakuraEvent.toZoneKey);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[6]);
        Assert.True(attemptedEvent.isSucceeded);
        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[7]);
        Assert.Equal("A009", resolvedEvent.anomalyDefinitionId);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[8]);
        Assert.Equal("A010", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[opponentTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA006WithManaSufficient_ShouldApplyTeamDeltaAndHealFriendlyNonHumanThenFlipNext()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA006SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorCharacterHp: 2,
            allyCharacterHp: 1,
            enemyCharacterHp: 1,
            enemyTeamKillScore: 6);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        var enemyCharacterInstanceId = gameState.players[enemyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91030,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A006", attemptedEvent.anomalyDefinitionId);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);

        var resolvedEvent = Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.Equal("A006", resolvedEvent.anomalyDefinitionId);

        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A007", flippedEvent.anomalyDefinitionId);

        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(5, gameState.teams[enemyTeamId].killScore);
        Assert.Equal(2, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Equal(4, gameState.characterInstances[allyCharacterInstanceId].currentHp);
        Assert.Equal(1, gameState.characterInstances[enemyCharacterInstanceId].currentHp);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A007", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(2, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A008", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A009", gameState.currentAnomalyState.anomalyDeckDefinitionIds[1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA006WithManaInsufficient_ShouldFailWithAttemptedOnlyAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA006SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 7,
            actorCharacterHp: 2,
            allyCharacterHp: 1,
            enemyCharacterHp: 1,
            enemyTeamKillScore: 6);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        var enemyCharacterInstanceId = gameState.players[enemyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91031,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A006", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientMana", attemptedEvent.failedReasonKey);

        Assert.Equal(7, gameState.players[actorPlayerId].mana);
        Assert.Equal(6, gameState.teams[enemyTeamId].killScore);
        Assert.Equal(2, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Equal(1, gameState.characterInstances[allyCharacterInstanceId].currentHp);
        Assert.Equal(1, gameState.characterInstances[enemyCharacterInstanceId].currentHp);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A006", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(3, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A007", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A008", gameState.currentAnomalyState.anomalyDeckDefinitionIds[1]);
        Assert.Equal("A009", gameState.currentAnomalyState.anomalyDeckDefinitionIds[2]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA005PrecheckPasses_ShouldOpenConditionInputForFirstFriendlyPlayer()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorFirstHandCardId = gameState.zones[actorHandZoneId].cardInstanceIds[0];

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91032,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A005", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("anomalyConditionInputRequired", attemptedEvent.failedReasonKey);

        var inputOpenedEvent = Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.Equal("inputContextOpened", inputOpenedEvent.eventTypeKey);
        Assert.True(inputOpenedEvent.isOpened);

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A005:conditionDefenseLikePlace", gameState.currentInputContext.contextKey);
        Assert.Contains(createA005ConditionChoiceKey(actorFirstHandCardId), gameState.currentInputContext.choiceKeys);
        Assert.NotNull(gameState.currentActionChain);
        Assert.False(gameState.currentActionChain!.isCompleted);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA005ConditionDefenseLikePlace, gameState.currentActionChain.pendingContinuationKey);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(10, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionPaymentsCompletedAndEligibleSummonCardsExist_ShouldOpenRewardInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card", "test:a005-summon-ineligible" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91033,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorSelectedCardA = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        var actorSelectedCardB = gameState.zones[actorHandZoneId].cardInstanceIds[1];
        var actorChoiceA = createA005ConditionChoiceKey(actorSelectedCardA);
        var actorChoiceB = createA005ConditionChoiceKey(actorSelectedCardB);
        var actorSigilPreviewBefore = gameState.players[actorPlayerId].sigilPreview;
        var allySigilPreviewBefore = gameState.players[allyPlayerId].sigilPreview;

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91034,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys = { actorChoiceA, actorChoiceB },
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A005:conditionDefenseLikePlace", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA005ConditionDefenseLikePlace, gameState.currentActionChain!.pendingContinuationKey);
        Assert.DoesNotContain(actorSelectedCardA, gameState.zones[actorHandZoneId].cardInstanceIds);
        Assert.DoesNotContain(actorSelectedCardB, gameState.zones[actorHandZoneId].cardInstanceIds);
        var actorFieldZoneId = gameState.players[actorPlayerId].fieldZoneId;
        Assert.Contains(actorSelectedCardA, gameState.zones[actorFieldZoneId].cardInstanceIds);
        Assert.Contains(actorSelectedCardB, gameState.zones[actorFieldZoneId].cardInstanceIds);
        Assert.True(gameState.cardInstances[actorSelectedCardA].isDefensePlacedOnField);
        Assert.True(gameState.cardInstances[actorSelectedCardB].isDefensePlacedOnField);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(actorSigilPreviewBefore, gameState.players[actorPlayerId].sigilPreview);
        Assert.Equal(allySigilPreviewBefore, gameState.players[allyPlayerId].sigilPreview);

        var allyInputContextId = gameState.currentInputContext.inputContextId;
        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        var allySelectedCardA = gameState.zones[allyHandZoneId].cardInstanceIds[0];
        var allySelectedCardB = gameState.zones[allyHandZoneId].cardInstanceIds[1];
        var allyChoiceA = createA005ConditionChoiceKey(allySelectedCardA);
        var allyChoiceB = createA005ConditionChoiceKey(allySelectedCardB);

        var allyConditionCompleteEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91035,
            actorPlayerId = allyPlayerId,
            inputContextId = allyInputContextId,
            choiceKeys = { allyChoiceA, allyChoiceB },
        });

        Assert.DoesNotContain(allyConditionCompleteEvents, gameEvent => gameEvent is AnomalyResolvedEvent);
        Assert.DoesNotContain(allyConditionCompleteEvents, gameEvent => gameEvent is AnomalyFlippedEvent);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A005:selectSummonCardToHand", gameState.currentInputContext.contextKey);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA005SelectSummonCardToHand, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Single(gameState.currentInputContext.choiceKeys);
        Assert.Equal(
            createA005RewardSelectSummonChoiceKey(gameState.zones[gameState.publicState!.summonZoneId].cardInstanceIds[0]),
            gameState.currentInputContext.choiceKeys[0]);
        var allyFieldZoneId = gameState.players[allyPlayerId].fieldZoneId;
        Assert.Contains(allySelectedCardA, gameState.zones[allyFieldZoneId].cardInstanceIds);
        Assert.Contains(allySelectedCardB, gameState.zones[allyFieldZoneId].cardInstanceIds);
        Assert.True(gameState.cardInstances[allySelectedCardA].isDefensePlacedOnField);
        Assert.True(gameState.cardInstances[allySelectedCardB].isDefensePlacedOnField);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(actorSigilPreviewBefore, gameState.players[actorPlayerId].sigilPreview);
        Assert.Equal(allySigilPreviewBefore, gameState.players[allyPlayerId].sigilPreview);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionContinuation_ShouldKeepClosedInputContextSelectedChoiceKeyNull()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91071,
            actorPlayerId = actorPlayerId,
        });

        var actorConditionInputContext = gameState.currentInputContext!;
        var actorInputContextId = actorConditionInputContext.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var actorChoiceA = createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]);
        var actorChoiceB = createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]);

        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91072,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys = { actorChoiceA, actorChoiceB },
        });

        Assert.Null(actorConditionInputContext.selectedChoiceKey);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA005ConditionDefenseLikePlace, gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005RewardChoiceSubmitted_ShouldMoveSelectedSummonCardToActorHandThenResolvedAndFlipped()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card", "test:a005-summon-ineligible" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91036,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91037,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]),
            },
        });

        var allyInputContextId = gameState.currentInputContext!.inputContextId;
        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91038,
            actorPlayerId = allyPlayerId,
            inputContextId = allyInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[1]),
            },
        });

        var rewardInputContextId = gameState.currentInputContext!.inputContextId;
        var summonZoneId = gameState.publicState!.summonZoneId;
        var selectedSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[0];
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91039,
            actorPlayerId = actorPlayerId,
            inputContextId = rewardInputContextId,
            choiceKey = createA005RewardSelectSummonChoiceKey(selectedSummonCardId),
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A006:arrivalHumanDefenseDiscardFlow", gameState.currentInputContext!.contextKey);
        Assert.Equal("anomalyA006ArrivalHumanDefenseDiscardOne", gameState.currentInputContext.inputTypeKey);
        Assert.Equal(actorPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.DoesNotContain(selectedSummonCardId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Contains(selectedSummonCardId, gameState.zones[actorHandZoneId].cardInstanceIds);
        Assert.Equal(actorHandZoneId, gameState.cardInstances[selectedSummonCardId].zoneId);
        Assert.Equal(ZoneKey.hand, gameState.cardInstances[selectedSummonCardId].zoneKey);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A006", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA006ArrivalHumanDefenseDiscardFlow, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);

        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[^4]);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        Assert.IsType<AnomalyResolvedEvent>(events[^3]);
        Assert.IsType<AnomalyFlippedEvent>(events[^2]);
        Assert.IsType<InteractionWindowEvent>(events[^1]);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionPaymentsCompletedWithoutEligibleSummonCard_ShouldResolveWithoutRewardInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test:a005-summon-ineligible" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91040,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91041,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]),
            },
        });

        var allyInputContextId = gameState.currentInputContext!.inputContextId;
        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91042,
            actorPlayerId = allyPlayerId,
            inputContextId = allyInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[1]),
            },
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A006:arrivalHumanDefenseDiscardFlow", gameState.currentInputContext!.contextKey);
        Assert.Equal("anomalyA006ArrivalHumanDefenseDiscardOne", gameState.currentInputContext.inputTypeKey);
        Assert.Equal(actorPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.Equal(0, gameState.players[actorPlayerId].mana);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Equal("A006", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA006ArrivalHumanDefenseDiscardFlow, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);

        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[^4]);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        Assert.IsType<AnomalyResolvedEvent>(events[^3]);
        Assert.IsType<AnomalyFlippedEvent>(events[^2]);
        Assert.IsType<InteractionWindowEvent>(events[^1]);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA005FriendlyHandCardsInsufficient_ShouldAttemptFailedWithoutInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 1,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91043,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(events);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.Equal("A005", attemptedEvent.anomalyDefinitionId);
        Assert.False(attemptedEvent.isSucceeded);
        Assert.Equal("insufficientFriendlyHandCards", attemptedEvent.failedReasonKey);

        Assert.Null(gameState.currentInputContext);
        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(10, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionChoiceIsInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91044,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorFieldCountBefore = gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91045,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore!.inputContextId,
                choiceKeys = { inputContextBefore.choiceKeys[0], inputContextBefore.choiceKeys[0] },
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKeys to contain exactly two unique values from currentInputContext.choiceKeys for continuation:anomalyA005ConditionDefenseLikePlace.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorFieldCountBefore, gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionContextKeyMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 3,
            allyHandCardCount: 3,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91100,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext!;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorFieldCountBefore = gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count;
        gameState.currentInputContext!.contextKey = "anomaly:invalidContext";
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91101,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore.inputContextId,
                choiceKeys =
                {
                    createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]),
                    createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]),
                },
            }));

        Assert.Equal("A005 anomaly condition continuation requires currentInputContext.contextKey to be anomaly:A005:conditionDefenseLikePlace.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorFieldCountBefore, gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionSelectedCardNotOwnedByRequiredPlayer_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91050,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorFieldCountBefore = gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count;

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var selectedCardA = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        var selectedCardB = gameState.zones[actorHandZoneId].cardInstanceIds[1];
        gameState.cardInstances[selectedCardA].ownerPlayerId = allyPlayerId;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91051,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore!.inputContextId,
                choiceKeys =
                {
                    createA005ConditionChoiceKey(selectedCardA),
                    createA005ConditionChoiceKey(selectedCardB),
                },
            }));

        Assert.Equal("A005 anomaly condition continuation requires selected cards to be owned by currentInputContext.requiredPlayerId.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorFieldCountBefore, gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ConditionSelectedCardNotInHand_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91052,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorFieldCountBefore = gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count;

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var selectedCardA = gameState.zones[actorHandZoneId].cardInstanceIds[0];
        var selectedCardB = gameState.zones[actorHandZoneId].cardInstanceIds[1];
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;
        gameState.zones[actorHandZoneId].cardInstanceIds.Remove(selectedCardA);
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(selectedCardA);
        gameState.cardInstances[selectedCardA].zoneId = actorDiscardZoneId;
        gameState.cardInstances[selectedCardA].zoneKey = ZoneKey.discard;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91053,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore!.inputContextId,
                choiceKeys =
                {
                    createA005ConditionChoiceKey(selectedCardA),
                    createA005ConditionChoiceKey(selectedCardB),
                },
            }));

        Assert.Equal("A005 anomaly condition continuation requires selected cards to still be in required player hand zone.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorFieldCountBefore, gameState.zones[gameState.players[actorPlayerId].fieldZoneId].cardInstanceIds.Count);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005RewardSelectedCardNotInCurrentSummonZone_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91046,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91047,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]),
            },
        });

        var allyInputContextId = gameState.currentInputContext!.inputContextId;
        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91048,
            actorPlayerId = allyPlayerId,
            inputContextId = allyInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[1]),
            },
        });

        var rewardInputContext = gameState.currentInputContext!;
        var rewardInputContextId = rewardInputContext.inputContextId;
        var summonZoneId = gameState.publicState!.summonZoneId;
        var selectedSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[0];
        var selectedChoiceKey = createA005RewardSelectSummonChoiceKey(selectedSummonCardId);
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;

        gameState.zones[summonZoneId].cardInstanceIds.Remove(selectedSummonCardId);
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(selectedSummonCardId);
        gameState.cardInstances[selectedSummonCardId].zoneId = actorDiscardZoneId;
        gameState.cardInstances[selectedSummonCardId].zoneKey = ZoneKey.discard;

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorManaBefore = gameState.players[actorPlayerId].mana;
        var enemyKillScoreBefore = gameState.teams[enemyTeamId].killScore;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91049,
                actorPlayerId = actorPlayerId,
                inputContextId = rewardInputContextId,
                choiceKey = selectedChoiceKey,
            }));

        Assert.Equal("A005 anomaly reward continuation requires selected card to still be in summon zone.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorManaBefore, gameState.players[actorPlayerId].mana);
        Assert.Equal(enemyKillScoreBefore, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005RewardSelectedCardSummonCostGreaterThanSeven_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 2,
            allyHandCardCount: 2,
            summonZoneDefinitionIds: new[] { "test-summon-card", "test:summon-cost-8" });

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91070,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91071,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[actorHandZoneId].cardInstanceIds[1]),
            },
        });

        var allyInputContextId = gameState.currentInputContext!.inputContextId;
        var allyHandZoneId = gameState.players[allyPlayerId].handZoneId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91072,
            actorPlayerId = allyPlayerId,
            inputContextId = allyInputContextId,
            choiceKeys =
            {
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[0]),
                createA005ConditionChoiceKey(gameState.zones[allyHandZoneId].cardInstanceIds[1]),
            },
        });

        var rewardInputContext = gameState.currentInputContext!;
        var rewardInputContextId = rewardInputContext.inputContextId;
        var summonZoneId = gameState.publicState!.summonZoneId;
        var ineligibleSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[1];
        var ineligibleChoiceKey = createA005RewardSelectSummonChoiceKey(ineligibleSummonCardId);

        rewardInputContext.choiceKeys.Add(ineligibleChoiceKey);

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;
        var actorManaBefore = gameState.players[actorPlayerId].mana;
        var enemyKillScoreBefore = gameState.teams[enemyTeamId].killScore;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91073,
                actorPlayerId = actorPlayerId,
                inputContextId = rewardInputContextId,
                choiceKey = ineligibleChoiceKey,
            }));

        Assert.Equal("A005 anomaly reward continuation requires selected card summon cost to be less than or equal to 7.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(actorManaBefore, gameState.players[actorPlayerId].mana);
        Assert.Equal(enemyKillScoreBefore, gameState.teams[enemyTeamId].killScore);
        Assert.False(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA004FlipsToA005AndSummonZoneHasCards_ShouldOpenArrivalInputAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 0,
            summonZoneDefinitionIds: new[] { "test-summon-card" });
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A005");

        var publicTreasureDeckZoneId = new ZoneId(8960);
        gameState.publicState!.publicTreasureDeckZoneId = publicTreasureDeckZoneId;
        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };


        var summonZoneId = gameState.publicState.summonZoneId;
        var selectedSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[0];

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91140,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, events.Count);
        var attemptedEvent = Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.True(attemptedEvent.isSucceeded);
        Assert.Null(attemptedEvent.failedReasonKey);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A005", flippedEvent.anomalyDefinitionId);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[3]);
        Assert.Equal("inputContextOpened", openedEvent.eventTypeKey);

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A005:arrivalDirectSummonFromSummonZone", gameState.currentInputContext.contextKey);
        Assert.Contains(createA005RewardSelectSummonChoiceKey(selectedSummonCardId), gameState.currentInputContext.choiceKeys);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(
            AnomalyProcessor.ContinuationKeyA005ArrivalDirectSummonFromSummonZone,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ArrivalSummonCardSelected_ShouldMoveToActorDiscardAndRefillSummonThenComplete()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 0,
            summonZoneDefinitionIds: new[] { "test-summon-card" });
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A005");

        var publicTreasureDeckZoneId = new ZoneId(8961);
        gameState.publicState!.publicTreasureDeckZoneId = publicTreasureDeckZoneId;
        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        var refillCardId = new CardInstanceId(89611);
        gameState.cardInstances[refillCardId] = new CardInstance
        {
            cardInstanceId = refillCardId,
            definitionId = "test:a005-arrival-refill",
            ownerPlayerId = actorPlayerId,
            zoneId = publicTreasureDeckZoneId,
            zoneKey = ZoneKey.publicTreasureDeck,
            isFaceUp = true,
        };
        gameState.zones[publicTreasureDeckZoneId].cardInstanceIds.Add(refillCardId);

        var summonZoneId = gameState.publicState.summonZoneId;
        var selectedSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[0];
        var actorDiscardZoneId = gameState.players[actorPlayerId].discardZoneId;

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91141,
            actorPlayerId = actorPlayerId,
        });

        var arrivalInputContextId = gameState.currentInputContext!.inputContextId;
        var events = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91142,
            actorPlayerId = actorPlayerId,
            inputContextId = arrivalInputContextId,
            choiceKey = createA005RewardSelectSummonChoiceKey(selectedSummonCardId),
        });

        Assert.True(events.Count >= 3);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[^3]);
        Assert.Equal("inputContextClosed", closedEvent.eventTypeKey);
        var summonMovedEvent = Assert.IsType<CardMovedEvent>(events[^2]);
        Assert.Equal(CardMoveReason.summon, summonMovedEvent.moveReason);
        var refillMovedEvent = Assert.IsType<CardMovedEvent>(events[^1]);
        Assert.Equal(CardMoveReason.reveal, refillMovedEvent.moveReason);

        Assert.Null(gameState.currentInputContext);
        Assert.Contains(selectedSummonCardId, gameState.zones[actorDiscardZoneId].cardInstanceIds);
        Assert.Equal(actorDiscardZoneId, gameState.cardInstances[selectedSummonCardId].zoneId);
        Assert.Equal(ZoneKey.discard, gameState.cardInstances[selectedSummonCardId].zoneKey);
        Assert.Contains(refillCardId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(summonZoneId, gameState.cardInstances[refillCardId].zoneId);
        Assert.Equal(ZoneKey.summonZone, gameState.cardInstances[refillCardId].zoneKey);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void SubmitInputChoice_WhenA005ArrivalSelectedCardNotInSummonZone_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 0,
            summonZoneDefinitionIds: new[] { "test-summon-card" });
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A005");

        var publicTreasureDeckZoneId = new ZoneId(8962);
        gameState.publicState!.publicTreasureDeckZoneId = publicTreasureDeckZoneId;
        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };


        var summonZoneId = gameState.publicState.summonZoneId;
        var selectedSummonCardId = gameState.zones[summonZoneId].cardInstanceIds[0];
        var selectedChoiceKey = createA005RewardSelectSummonChoiceKey(selectedSummonCardId);
        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91143,
            actorPlayerId = actorPlayerId,
        });

        gameState.zones[summonZoneId].cardInstanceIds.Remove(selectedSummonCardId);
        gameState.zones[actorHandZoneId].cardInstanceIds.Add(selectedSummonCardId);
        gameState.cardInstances[selectedSummonCardId].zoneId = actorHandZoneId;
        gameState.cardInstances[selectedSummonCardId].zoneKey = ZoneKey.hand;

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91144,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore!.inputContextId,
                choiceKey = selectedChoiceKey,
            }));

        Assert.Equal("A005 anomaly arrival continuation requires selected card to still be in summon zone.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA004FlipsToA005AndSummonZoneEmpty_ShouldResolveWithoutArrivalInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorHandCardCount: 0,
            allyHandCardCount: 0,
            summonZoneDefinitionIds: Array.Empty<string>());
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A005");

        var publicTreasureDeckZoneId = new ZoneId(8963);
        gameState.publicState!.publicTreasureDeckZoneId = publicTreasureDeckZoneId;
        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };


        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91145,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal("A005", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
    }

    [Fact]
    public void TryResolveAnomaly_WhenA006FlipsToA007AndSeatPlayersCanInteract_ShouldOpenA007ArrivalInputAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A006";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A007");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A008");

        var actorHandCardId = addCardToPlayerHandZone(gameState, actorPlayerId, "starter:magicCircuit", 999901);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91160,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, events.Count);
        Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A007", flippedEvent.anomalyDefinitionId);
        var inputOpenedEvent = Assert.IsType<InteractionWindowEvent>(events[3]);
        Assert.Equal("inputContextOpened", inputOpenedEvent.eventTypeKey);

        Assert.Equal("A007", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomaly:A007:arrivalOptionalBanishFlow", gameState.currentInputContext.contextKey);
        Assert.Equal("anomalyA007ArrivalOptionalHandBanish", gameState.currentInputContext.inputTypeKey);
        Assert.Contains("decline", gameState.currentInputContext.choiceKeys);
        Assert.Contains(createA007ArrivalHandChoiceKey(actorHandCardId), gameState.currentInputContext.choiceKeys);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA007ArrivalOptionalBanishFlow, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void SubmitInputChoice_WhenA007ArrivalFlowProgressesThroughC007ExtraOptionalStages_ShouldMoveSelectedCardsToGapAndComplete()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A006";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A007");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A008");

        var actorHandCardId = addCardToPlayerHandZone(gameState, actorPlayerId, "starter:magicCircuit", 999902);
        var firstOpponentHandCardId = addCardToPlayerHandZone(gameState, firstOpponentPlayerId, "starter:magicCircuit", 999903);
        var secondOpponentDiscardCardId = addCardToPlayerDiscardZone(gameState, secondOpponentPlayerId, "starter:magicCircuit", 999904);
        var secondOpponentCharacterInstanceId = gameState.players[secondOpponentPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[secondOpponentCharacterInstanceId].definitionId = "C007";

        var actorHandZoneId = gameState.players[actorPlayerId].handZoneId;
        var firstOpponentHandZoneId = gameState.players[firstOpponentPlayerId].handZoneId;
        var secondOpponentDiscardZoneId = gameState.players[secondOpponentPlayerId].discardZoneId;
        var gapZoneId = gameState.publicState!.gapZoneId;

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91161,
            actorPlayerId = actorPlayerId,
        });

        var actorInputContextId = gameState.currentInputContext!.inputContextId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91162,
            actorPlayerId = actorPlayerId,
            inputContextId = actorInputContextId,
            choiceKey = "decline",
        });
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(firstOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA007ArrivalOptionalHandBanish", gameState.currentInputContext.inputTypeKey);

        var firstOpponentInputContextId = gameState.currentInputContext.inputContextId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91163,
            actorPlayerId = firstOpponentPlayerId,
            inputContextId = firstOpponentInputContextId,
            choiceKey = createA007ArrivalHandChoiceKey(firstOpponentHandCardId),
        });
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA007ArrivalOptionalDiscardBanishDecision", gameState.currentInputContext.inputTypeKey);

        var secondOpponentDecisionContextId = gameState.currentInputContext.inputContextId;
        _ = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91164,
            actorPlayerId = secondOpponentPlayerId,
            inputContextId = secondOpponentDecisionContextId,
            choiceKey = "accept",
        });
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(secondOpponentPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal("anomalyA007ArrivalOptionalDiscardBanishSelect", gameState.currentInputContext.inputTypeKey);
        Assert.Contains(createA007ArrivalDiscardChoiceKey(secondOpponentDiscardCardId), gameState.currentInputContext.choiceKeys);

        var secondOpponentSelectContextId = gameState.currentInputContext.inputContextId;
        var finalEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91165,
            actorPlayerId = secondOpponentPlayerId,
            inputContextId = secondOpponentSelectContextId,
            choiceKey = createA007ArrivalDiscardChoiceKey(secondOpponentDiscardCardId),
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.DoesNotContain(firstOpponentHandCardId, gameState.zones[firstOpponentHandZoneId].cardInstanceIds);
        Assert.DoesNotContain(secondOpponentDiscardCardId, gameState.zones[secondOpponentDiscardZoneId].cardInstanceIds);
        Assert.Contains(firstOpponentHandCardId, gameState.zones[gapZoneId].cardInstanceIds);
        Assert.Contains(secondOpponentDiscardCardId, gameState.zones[gapZoneId].cardInstanceIds);
        Assert.Equal(gapZoneId, gameState.cardInstances[firstOpponentHandCardId].zoneId);
        Assert.Equal(gapZoneId, gameState.cardInstances[secondOpponentDiscardCardId].zoneId);
        Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[firstOpponentHandCardId].zoneKey);
        Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[secondOpponentDiscardCardId].zoneKey);
        Assert.Equal(actorHandZoneId, gameState.cardInstances[actorHandCardId].zoneId);
        Assert.Contains(firstOpponentHandCardId, finalEvents.OfType<CardMovedEvent>().Select(cardMovedEvent => cardMovedEvent.cardInstanceId));
        Assert.Contains(secondOpponentDiscardCardId, finalEvents.OfType<CardMovedEvent>().Select(cardMovedEvent => cardMovedEvent.cardInstanceId));
    }

    [Fact]
    public void TryResolveAnomaly_WhenA006FlipsToA007AndAllPlayersHaveNoArrivalTargets_ShouldResolveWithoutArrivalInput()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A006";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A007");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A008");
        var secondOpponentCharacterInstanceId = gameState.players[secondOpponentPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[secondOpponentCharacterInstanceId].definitionId = "C007";

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91166,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, events.Count);
        Assert.IsType<AnomalyResolveAttemptedEvent>(events[0]);
        Assert.IsType<AnomalyResolvedEvent>(events[1]);
        var flippedEvent = Assert.IsType<AnomalyFlippedEvent>(events[2]);
        Assert.Equal("A007", flippedEvent.anomalyDefinitionId);
        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void SubmitInputChoice_WhenA007ArrivalChoiceInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var firstOpponentPlayerId = new PlayerId(2);
        var secondOpponentPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var gameState = createA009ConditionFlowGameState(
            actorPlayerId,
            firstOpponentPlayerId,
            secondOpponentPlayerId,
            actorTeamId,
            opponentTeamId,
            actorMana: 8,
            sakuraCakeCount: 0);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A006";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A007");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A008");
        _ = addCardToPlayerHandZone(gameState, actorPlayerId, "starter:magicCircuit", 999905);

        var processor = new ActionRequestProcessor();
        _ = processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91167,
            actorPlayerId = actorPlayerId,
        });

        var inputContextBefore = gameState.currentInputContext;
        var actionChainBefore = gameState.currentActionChain;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var producedEventCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
            {
                requestId = 91168,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextBefore!.inputContextId,
                choiceKey = "invalid-choice",
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:anomalyA007ArrivalOptionalBanishFlow.", exception.Message);
        Assert.Same(inputContextBefore, gameState.currentInputContext);
        Assert.Same(actionChainBefore, gameState.currentActionChain);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventCountBefore, gameState.currentActionChain.producedEvents.Count);
    }

    [Fact]
    public void TryResolveAnomaly_WhenCurrentAnomalyDefinitionIdIsMissingInState_ShouldThrow()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = null;

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
            {
                requestId = 91015,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("TryResolveAnomalyActionRequest requires gameState.currentAnomalyState.currentAnomalyDefinitionId to be initialized.", exception.Message);
    }

    [Fact]
    public void TryResolveAnomaly_WhenCurrentAnomalyStateIsMissing_ShouldThrow()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);
        gameState.currentAnomalyState = null;

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
            {
                requestId = 91016,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("TryResolveAnomalyActionRequest requires gameState.currentAnomalyState to be initialized.", exception.Message);
    }

    [Fact]
    public void StartNextTurn_ShouldResetHasResolvedAnomalyThisTurnGate()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 5000);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 6000);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            turnState = new TurnState
            {
                turnNumber = 6,
                currentPlayerId = currentPlayerId,
                currentTeamId = currentTeamId,
                currentPhase = TurnPhase.end,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = true,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A003",
            },
        };
        gameState.matchMeta.seatOrder.Add(currentPlayerId);
        gameState.matchMeta.seatOrder.Add(nextPlayerId);
        gameState.matchMeta.teamAssignments[currentPlayerId] = currentTeamId;
        gameState.matchMeta.teamAssignments[nextPlayerId] = nextTeamId;
        gameState.players[currentPlayerId] = currentPlayerState;
        gameState.players[nextPlayerId] = nextPlayerState;

        addStandardPlayerZones(gameState, currentPlayerState);
        addStandardPlayerZones(gameState, nextPlayerState);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 91021,
            actorPlayerId = currentPlayerId,
        });

        Assert.Empty(events);
        Assert.Equal(nextPlayerId, gameState.turnState.currentPlayerId);
        Assert.Equal(TurnPhase.start, gameState.turnState.currentPhase);
        Assert.False(gameState.turnState.hasResolvedAnomalyThisTurn);
    }

    [Fact]
    public void TryResolveAnomaly_WhenRewardIsNoOp_ShouldNotChangeTeamScoresOrLeyline()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createAnomalyReadyGameState(actorPlayerId, allyPlayerId, actorTeamId, enemyTeamId);
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A004";

        gameState.teams[actorTeamId].killScore = 7;
        gameState.teams[actorTeamId].leyline = 3;
        gameState.teams[enemyTeamId].killScore = 9;
        gameState.teams[enemyTeamId].leyline = 5;

        var actorCharacterId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new TryResolveAnomalyActionRequest
        {
            requestId = 91022,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(8, gameState.players[actorPlayerId].mana);
        Assert.Equal(3, gameState.characterInstances[actorCharacterId].currentHp);
        Assert.Equal(3, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(7, gameState.teams[actorTeamId].killScore);
        Assert.Equal(3, gameState.teams[actorTeamId].leyline);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.Equal(5, gameState.teams[enemyTeamId].leyline);
    }

    private static RuleCore.GameState.GameState createAnomalyReadyGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId)
    {
        return createA001SampleGameState(
            actorPlayerId,
            allyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana: 8,
            actorCharacterHp: 3,
            allyCharacterHp: 3,
            hasAllyActiveCharacter: true,
            currentAnomalyDefinitionId: "A001",
            nextAnomalyDefinitionId: "A002");
    }

    private static RuleCore.GameState.GameState createA009ConditionFlowGameState(
        PlayerId actorPlayerId,
        PlayerId firstOpponentPlayerId,
        PlayerId? secondOpponentPlayerId,
        TeamId actorTeamId,
        TeamId opponentTeamId,
        int actorMana,
        int sakuraCakeCount,
        IReadOnlyList<string>? gapZoneDefinitionIds = null,
        IReadOnlyList<string>? summonZoneDefinitionIds = null)
    {
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 9700);
        var firstOpponentPlayerState = createPlayerState(firstOpponentPlayerId, opponentTeamId, 9800);
        PlayerState? secondOpponentPlayerState = null;
        if (secondOpponentPlayerId.HasValue)
        {
            secondOpponentPlayerState = createPlayerState(secondOpponentPlayerId.Value, opponentTeamId, 9900);
        }

        var actorCharacterInstanceId = new CharacterInstanceId(9951);
        var firstOpponentCharacterInstanceId = new CharacterInstanceId(9952);
        CharacterInstanceId? secondOpponentCharacterInstanceId = secondOpponentPlayerId.HasValue
            ? new CharacterInstanceId(9953)
            : null;

        actorPlayerState.mana = actorMana;
        actorPlayerState.activeCharacterInstanceId = actorCharacterInstanceId;
        firstOpponentPlayerState.mana = 0;
        firstOpponentPlayerState.activeCharacterInstanceId = firstOpponentCharacterInstanceId;
        if (secondOpponentPlayerState is not null && secondOpponentCharacterInstanceId.HasValue)
        {
            secondOpponentPlayerState.mana = 0;
            secondOpponentPlayerState.activeCharacterInstanceId = secondOpponentCharacterInstanceId.Value;
        }

        var sakuraCakeDeckZoneId = new ZoneId(9960);
        var gapZoneId = new ZoneId(9961);
        var publicTreasureDeckZoneId = new ZoneId(9962);
        var summonZoneId = new ZoneId(9963);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            publicState = new PublicState
            {
                sakuraCakeDeckZoneId = sakuraCakeDeckZoneId,
                gapZoneId = gapZoneId,
                publicTreasureDeckZoneId = publicTreasureDeckZoneId,
                summonZoneId = summonZoneId,
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A009",
                anomalyDeckDefinitionIds =
                {
                    "A010",
                },
            },
        };

        gameState.matchMeta.seatOrder.Add(actorPlayerId);
        gameState.matchMeta.seatOrder.Add(firstOpponentPlayerId);
        if (secondOpponentPlayerId.HasValue)
        {
            gameState.matchMeta.seatOrder.Add(secondOpponentPlayerId.Value);
        }
        gameState.matchMeta.teamAssignments[actorPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[firstOpponentPlayerId] = opponentTeamId;
        if (secondOpponentPlayerId.HasValue)
        {
            gameState.matchMeta.teamAssignments[secondOpponentPlayerId.Value] = opponentTeamId;
        }

        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[firstOpponentPlayerId] = firstOpponentPlayerState;
        if (secondOpponentPlayerId.HasValue && secondOpponentPlayerState is not null)
        {
            gameState.players[secondOpponentPlayerId.Value] = secondOpponentPlayerState;
        }

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:a009-actor",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[firstOpponentCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = firstOpponentCharacterInstanceId,
            definitionId = "test:a009-opponent-1",
            ownerPlayerId = firstOpponentPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        if (secondOpponentPlayerId.HasValue && secondOpponentCharacterInstanceId.HasValue)
        {
            gameState.characterInstances[secondOpponentCharacterInstanceId.Value] = new CharacterInstance
            {
                characterInstanceId = secondOpponentCharacterInstanceId.Value,
                definitionId = "test:a009-opponent-2",
                ownerPlayerId = secondOpponentPlayerId.Value,
                currentHp = 4,
                maxHp = 4,
                isAlive = true,
                isInPlay = true,
            };
        }

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                actorPlayerId,
            },
        };
        var opponentTeamState = new TeamState
        {
            teamId = opponentTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                firstOpponentPlayerId,
            },
        };
        if (secondOpponentPlayerId.HasValue)
        {
            opponentTeamState.memberPlayerIds.Add(secondOpponentPlayerId.Value);
        }
        gameState.teams[opponentTeamId] = opponentTeamState;

        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, firstOpponentPlayerState);
        if (secondOpponentPlayerState is not null)
        {
            addStandardPlayerZones(gameState, secondOpponentPlayerState);
        }
        gameState.zones[sakuraCakeDeckZoneId] = new ZoneState
        {
            zoneId = sakuraCakeDeckZoneId,
            zoneType = ZoneKey.sakuraCakeDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[gapZoneId] = new ZoneState
        {
            zoneId = gapZoneId,
            zoneType = ZoneKey.gapZone,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        for (var sakuraCakeIndex = 0; sakuraCakeIndex < sakuraCakeCount; sakuraCakeIndex++)
        {
            var sakuraCakeCardInstanceId = new CardInstanceId(9970 + sakuraCakeIndex);
            gameState.cardInstances[sakuraCakeCardInstanceId] = new CardInstance
            {
                cardInstanceId = sakuraCakeCardInstanceId,
                definitionId = "test:sakura-cake",
                ownerPlayerId = actorPlayerId,
                zoneId = sakuraCakeDeckZoneId,
                zoneKey = ZoneKey.sakuraCakeDeck,
                isFaceUp = false,
            };
            gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Add(sakuraCakeCardInstanceId);
        }

        if (gapZoneDefinitionIds is not null)
        {
            for (var gapCardIndex = 0; gapCardIndex < gapZoneDefinitionIds.Count; gapCardIndex++)
            {
                var gapCardInstanceId = new CardInstanceId(9980 + gapCardIndex);
                gameState.cardInstances[gapCardInstanceId] = new CardInstance
                {
                    cardInstanceId = gapCardInstanceId,
                    definitionId = gapZoneDefinitionIds[gapCardIndex],
                    ownerPlayerId = actorPlayerId,
                    zoneId = gapZoneId,
                    zoneKey = ZoneKey.gapZone,
                    isFaceUp = true,
                };
                gameState.zones[gapZoneId].cardInstanceIds.Add(gapCardInstanceId);
            }
        }

        if (summonZoneDefinitionIds is not null)
        {
            for (var summonZoneCardIndex = 0; summonZoneCardIndex < summonZoneDefinitionIds.Count; summonZoneCardIndex++)
            {
                var summonZoneCardInstanceId = new CardInstanceId(9990 + summonZoneCardIndex);
                gameState.cardInstances[summonZoneCardInstanceId] = new CardInstance
                {
                    cardInstanceId = summonZoneCardInstanceId,
                    definitionId = summonZoneDefinitionIds[summonZoneCardIndex],
                    ownerPlayerId = actorPlayerId,
                    zoneId = summonZoneId,
                    zoneKey = ZoneKey.summonZone,
                    isFaceUp = true,
                };
                gameState.zones[summonZoneId].cardInstanceIds.Add(summonZoneCardInstanceId);
            }
        }

        return gameState;
    }

    private static RuleCore.GameState.GameState createA001SampleGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana,
        int actorCharacterHp,
        int allyCharacterHp,
        bool hasAllyActiveCharacter,
        string currentAnomalyDefinitionId = "A001",
        string nextAnomalyDefinitionId = "A002")
    {
        var enemyPlayerId = new PlayerId(2);
        var actorCharacterInstanceId = new CharacterInstanceId(7001);
        var allyCharacterInstanceId = new CharacterInstanceId(7002);
        var enemyCharacterInstanceId = new CharacterInstanceId(7003);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            publicState = new PublicState
            {
                gapZoneId = new ZoneId(8299),
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = currentAnomalyDefinitionId,
                anomalyDeckDefinitionIds =
                {
                    nextAnomalyDefinitionId,
                    "A003",
                },
            },
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            mana = actorMana,
            activeCharacterInstanceId = actorCharacterInstanceId,
        };
        gameState.players[allyPlayerId] = new PlayerState
        {
            playerId = allyPlayerId,
            teamId = actorTeamId,
            mana = 0,
            activeCharacterInstanceId = hasAllyActiveCharacter ? allyCharacterInstanceId : null,
        };
        gameState.players[enemyPlayerId] = new PlayerState
        {
            playerId = enemyPlayerId,
            teamId = enemyTeamId,
            mana = 0,
            activeCharacterInstanceId = enemyCharacterInstanceId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:actor",
            ownerPlayerId = actorPlayerId,
            currentHp = actorCharacterHp,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.characterInstances[enemyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = enemyCharacterInstanceId,
            definitionId = "test:enemy",
            ownerPlayerId = enemyPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        if (hasAllyActiveCharacter)
        {
            gameState.characterInstances[allyCharacterInstanceId] = new CharacterInstance
            {
                characterInstanceId = allyCharacterInstanceId,
                definitionId = "test:ally",
                ownerPlayerId = allyPlayerId,
                currentHp = allyCharacterHp,
                maxHp = 4,
                isAlive = true,
                isInPlay = true,
            };
        }

        var actorTeamState = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
        };
        actorTeamState.memberPlayerIds.Add(actorPlayerId);
        actorTeamState.memberPlayerIds.Add(allyPlayerId);
        gameState.teams[actorTeamId] = actorTeamState;

        var enemyTeamState = new TeamState
        {
            teamId = enemyTeamId,
            killScore = 10,
            leyline = 0,
        };
        enemyTeamState.memberPlayerIds.Add(enemyPlayerId);
        gameState.teams[enemyTeamId] = enemyTeamState;

        gameState.matchMeta.seatOrder.Add(actorPlayerId);
        gameState.matchMeta.seatOrder.Add(enemyPlayerId);
        gameState.matchMeta.seatOrder.Add(allyPlayerId);
        gameState.matchMeta.teamAssignments[actorPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[allyPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[enemyPlayerId] = enemyTeamId;

        addStandardPlayerZones(gameState, gameState.players[actorPlayerId]);
        addStandardPlayerZones(gameState, gameState.players[allyPlayerId]);
        addStandardPlayerZones(gameState, gameState.players[enemyPlayerId]);
        gameState.zones[gameState.publicState.gapZoneId] = new ZoneState
        {
            zoneId = gameState.publicState.gapZoneId,
            zoneType = ZoneKey.gapZone,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        return gameState;
    }

    private static RuleCore.GameState.GameState createA003SampleGameState(
        PlayerId actorPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana,
        int actorTeamLeyline,
        int enemyTeamKillScore)
    {
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            publicState = new PublicState
            {
                gapZoneId = new ZoneId(8298),
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A003",
                anomalyDeckDefinitionIds =
                {
                    "A004",
                },
            },
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            mana = actorMana,
        };

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = actorTeamLeyline,
        };
        gameState.teams[enemyTeamId] = new TeamState
        {
            teamId = enemyTeamId,
            killScore = enemyTeamKillScore,
            leyline = 0,
        };

        return gameState;
    }

    private static RuleCore.GameState.GameState createA005SampleGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        PlayerId enemyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana,
        int actorHandCardCount,
        int allyHandCardCount,
        IReadOnlyList<string> summonZoneDefinitionIds)
    {
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 81000);
        var allyPlayerState = createPlayerState(allyPlayerId, actorTeamId, 82000);
        var enemyPlayerState = createPlayerState(enemyPlayerId, enemyTeamId, 83000);
        var actorCharacterInstanceId = new CharacterInstanceId(8400);
        var allyCharacterInstanceId = new CharacterInstanceId(8401);
        var enemyCharacterInstanceId = new CharacterInstanceId(8402);
        var summonZoneId = new ZoneId(8900);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            publicState = new PublicState
            {
                summonZoneId = summonZoneId,
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A005",
                anomalyDeckDefinitionIds =
                {
                    "A006",
                    "A007",
                },
            },
        };

        actorPlayerState.mana = actorMana;
        actorPlayerState.activeCharacterInstanceId = actorCharacterInstanceId;
        allyPlayerState.mana = 0;
        allyPlayerState.activeCharacterInstanceId = allyCharacterInstanceId;
        enemyPlayerState.mana = 0;
        enemyPlayerState.activeCharacterInstanceId = enemyCharacterInstanceId;
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[allyPlayerId] = allyPlayerState;
        gameState.players[enemyPlayerId] = enemyPlayerState;

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[allyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = allyCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = allyPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[enemyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = enemyCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = enemyPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                actorPlayerId,
                allyPlayerId,
            },
        };
        gameState.teams[enemyTeamId] = new TeamState
        {
            teamId = enemyTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                enemyPlayerId,
            },
        };

        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, allyPlayerState);
        addStandardPlayerZones(gameState, enemyPlayerState);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, actorPlayerId, ZonePublicOrPrivate.publicZone);

        for (var handCardIndex = 0; handCardIndex < actorHandCardCount; handCardIndex++)
        {
            var actorHandCardId = new CardInstanceId(8500 + handCardIndex);
            gameState.cardInstances[actorHandCardId] = new CardInstance
            {
                cardInstanceId = actorHandCardId,
                definitionId = "test:a005-actor-hand-card",
                ownerPlayerId = actorPlayerId,
                zoneId = actorPlayerState.handZoneId,
                zoneKey = ZoneKey.hand,
                isFaceUp = false,
            };
            gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Add(actorHandCardId);
        }

        for (var handCardIndex = 0; handCardIndex < allyHandCardCount; handCardIndex++)
        {
            var allyHandCardId = new CardInstanceId(8600 + handCardIndex);
            gameState.cardInstances[allyHandCardId] = new CardInstance
            {
                cardInstanceId = allyHandCardId,
                definitionId = "test:a005-ally-hand-card",
                ownerPlayerId = allyPlayerId,
                zoneId = allyPlayerState.handZoneId,
                zoneKey = ZoneKey.hand,
                isFaceUp = false,
            };
            gameState.zones[allyPlayerState.handZoneId].cardInstanceIds.Add(allyHandCardId);
        }

        for (var summonCardIndex = 0; summonCardIndex < summonZoneDefinitionIds.Count; summonCardIndex++)
        {
            var summonCardId = new CardInstanceId(8700 + summonCardIndex);
            gameState.cardInstances[summonCardId] = new CardInstance
            {
                cardInstanceId = summonCardId,
                definitionId = summonZoneDefinitionIds[summonCardIndex],
                ownerPlayerId = actorPlayerId,
                zoneId = summonZoneId,
                zoneKey = ZoneKey.summonZone,
                isFaceUp = true,
            };
            gameState.zones[summonZoneId].cardInstanceIds.Add(summonCardId);
        }

        return gameState;
    }

    private static RuleCore.GameState.GameState createA002ConditionFlowGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        PlayerId enemyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana,
        int actorHandCardCount,
        int allyHandCardCount,
        int sakuraCakeDeckCardCount = 2)
    {
        var gameState = createA005SampleGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId,
            actorMana,
            actorHandCardCount,
            allyHandCardCount,
            Array.Empty<string>());
        gameState.currentAnomalyState!.currentAnomalyDefinitionId = "A002";
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Clear();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A003");

        var publicState = gameState.publicState!;
        var publicTreasureDeckZoneId = new ZoneId(8980);
        var anomalyDeckZoneId = new ZoneId(8981);
        var sakuraCakeDeckZoneId = new ZoneId(8982);
        var gapZoneId = new ZoneId(8983);

        publicState.publicTreasureDeckZoneId = publicTreasureDeckZoneId;
        publicState.anomalyDeckZoneId = anomalyDeckZoneId;
        publicState.sakuraCakeDeckZoneId = sakuraCakeDeckZoneId;
        publicState.gapZoneId = gapZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, actorPlayerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, anomalyDeckZoneId, ZoneKey.anomalyDeck, actorPlayerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, sakuraCakeDeckZoneId, ZoneKey.sakuraCakeDeck, actorPlayerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gapZoneId, ZoneKey.gapZone, actorPlayerId, ZonePublicOrPrivate.publicZone);

        for (var cardIndex = 0; cardIndex < sakuraCakeDeckCardCount; cardIndex++)
        {
            var sakuraCakeCardInstanceId = new CardInstanceId(8990 + cardIndex);
            gameState.cardInstances[sakuraCakeCardInstanceId] = new CardInstance
            {
                cardInstanceId = sakuraCakeCardInstanceId,
                definitionId = "test:a002-sakura-cake",
                ownerPlayerId = actorPlayerId,
                zoneId = sakuraCakeDeckZoneId,
                zoneKey = ZoneKey.sakuraCakeDeck,
                isFaceUp = true,
            };
            gameState.zones[sakuraCakeDeckZoneId].cardInstanceIds.Add(sakuraCakeCardInstanceId);
        }

        return gameState;
    }

    private static string createA002ConditionChoiceKey(CardInstanceId cardInstanceId)
    {
        return "handCard:" + cardInstanceId.Value;
    }

    private static string createA002RewardBanishChoiceKey(CardInstanceId cardInstanceId)
    {
        return "banishCard:" + cardInstanceId.Value;
    }

    private static string createA005ConditionChoiceKey(CardInstanceId cardInstanceId)
    {
        return "handCard:" + cardInstanceId.Value;
    }

    private static string createA005RewardSelectSummonChoiceKey(CardInstanceId cardInstanceId)
    {
        return "summonCard:" + cardInstanceId.Value;
    }

    private static string createA007ArrivalHandChoiceKey(CardInstanceId cardInstanceId)
    {
        return "handCard:" + cardInstanceId.Value;
    }

    private static string createA007ArrivalDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return "discardCard:" + cardInstanceId.Value;
    }

    private static string createA008ConditionDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return "discardCard:" + cardInstanceId.Value;
    }

    private static string createA009RewardSelectGapChoiceKey(CardInstanceId cardInstanceId)
    {
        return "gapCard:" + cardInstanceId.Value;
    }

    private static CardInstanceId addCardToPlayerDiscardZone(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string definitionId,
        long cardInstanceNumericId)
    {
        var playerState = gameState.players[playerId];
        var discardZoneId = playerState.discardZoneId;
        var discardCardInstanceId = new CardInstanceId(cardInstanceNumericId);
        gameState.cardInstances[discardCardInstanceId] = new CardInstance
        {
            cardInstanceId = discardCardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerId,
            zoneId = discardZoneId,
            zoneKey = ZoneKey.discard,
            isFaceUp = true,
        };
        gameState.zones[discardZoneId].cardInstanceIds.Add(discardCardInstanceId);
        return discardCardInstanceId;
    }

    private static CardInstanceId addCardToPlayerHandZone(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string definitionId,
        long cardInstanceNumericId)
    {
        var playerState = gameState.players[playerId];
        var handZoneId = playerState.handZoneId;
        var handCardInstanceId = new CardInstanceId(cardInstanceNumericId);
        gameState.cardInstances[handCardInstanceId] = new CardInstance
        {
            cardInstanceId = handCardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerId,
            zoneId = handZoneId,
            zoneKey = ZoneKey.hand,
            isFaceUp = false,
        };
        gameState.zones[handZoneId].cardInstanceIds.Add(handCardInstanceId);
        return handCardInstanceId;
    }

    private static RuleCore.GameState.GameState createA006SampleGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        PlayerId enemyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana,
        int actorCharacterHp,
        int allyCharacterHp,
        int enemyCharacterHp,
        int enemyTeamKillScore)
    {
        var actorCharacterInstanceId = new CharacterInstanceId(8201);
        var allyCharacterInstanceId = new CharacterInstanceId(8202);
        var enemyCharacterInstanceId = new CharacterInstanceId(8203);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            publicState = new PublicState
            {
                gapZoneId = new ZoneId(8298),
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A006",
                anomalyDeckDefinitionIds =
                {
                    "A007",
                    "A008",
                    "A009",
                },
            },
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            mana = actorMana,
            activeCharacterInstanceId = actorCharacterInstanceId,
        };
        gameState.players[allyPlayerId] = new PlayerState
        {
            playerId = allyPlayerId,
            teamId = actorTeamId,
            mana = 0,
            activeCharacterInstanceId = allyCharacterInstanceId,
        };
        gameState.players[enemyPlayerId] = new PlayerState
        {
            playerId = enemyPlayerId,
            teamId = enemyTeamId,
            mana = 0,
            activeCharacterInstanceId = enemyCharacterInstanceId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = actorPlayerId,
            currentHp = actorCharacterHp,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[allyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = allyCharacterInstanceId,
            definitionId = "test:nonHuman",
            ownerPlayerId = allyPlayerId,
            currentHp = allyCharacterHp,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[enemyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = enemyCharacterInstanceId,
            definitionId = "test:nonHuman",
            ownerPlayerId = enemyPlayerId,
            currentHp = enemyCharacterHp,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actorTeamState = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
        };
        actorTeamState.memberPlayerIds.Add(actorPlayerId);
        actorTeamState.memberPlayerIds.Add(allyPlayerId);
        gameState.teams[actorTeamId] = actorTeamState;

        var enemyTeamState = new TeamState
        {
            teamId = enemyTeamId,
            killScore = enemyTeamKillScore,
            leyline = 0,
        };
        enemyTeamState.memberPlayerIds.Add(enemyPlayerId);
        gameState.teams[enemyTeamId] = enemyTeamState;

        gameState.matchMeta.seatOrder.Add(actorPlayerId);
        gameState.matchMeta.seatOrder.Add(enemyPlayerId);
        gameState.matchMeta.seatOrder.Add(allyPlayerId);
        gameState.matchMeta.teamAssignments[actorPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[allyPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[enemyPlayerId] = enemyTeamId;

        addStandardPlayerZones(gameState, gameState.players[actorPlayerId]);
        addStandardPlayerZones(gameState, gameState.players[allyPlayerId]);
        addStandardPlayerZones(gameState, gameState.players[enemyPlayerId]);
        gameState.zones[gameState.publicState.gapZoneId] = new ZoneState
        {
            zoneId = gameState.publicState.gapZoneId,
            zoneType = ZoneKey.gapZone,
            ownerPlayerId = null,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        return gameState;
    }

    private static RuleCore.GameState.GameState createA007SampleGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        PlayerId targetOpponentPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId,
        int actorMana)
    {
        var actorCharacterInstanceId = new CharacterInstanceId(8101);
        var allyCharacterInstanceId = new CharacterInstanceId(8102);
        var targetOpponentCharacterInstanceId = new CharacterInstanceId(8103);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
                hasResolvedAnomalyThisTurn = false,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A007",
                anomalyDeckDefinitionIds =
                {
                    "A008",
                    "A009",
                },
            },
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            mana = actorMana,
            activeCharacterInstanceId = actorCharacterInstanceId,
        };
        gameState.players[allyPlayerId] = new PlayerState
        {
            playerId = allyPlayerId,
            teamId = actorTeamId,
            mana = 0,
            activeCharacterInstanceId = allyCharacterInstanceId,
        };
        gameState.players[targetOpponentPlayerId] = new PlayerState
        {
            playerId = targetOpponentPlayerId,
            teamId = enemyTeamId,
            mana = 0,
            activeCharacterInstanceId = targetOpponentCharacterInstanceId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:a007-actor",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[allyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = allyCharacterInstanceId,
            definitionId = "test:a007-ally",
            ownerPlayerId = allyPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[targetOpponentCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = targetOpponentCharacterInstanceId,
            definitionId = "test:a007-target",
            ownerPlayerId = targetOpponentPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actorTeamState = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
        };
        actorTeamState.memberPlayerIds.Add(actorPlayerId);
        actorTeamState.memberPlayerIds.Add(allyPlayerId);
        gameState.teams[actorTeamId] = actorTeamState;

        var enemyTeamState = new TeamState
        {
            teamId = enemyTeamId,
            killScore = 10,
            leyline = 0,
        };
        enemyTeamState.memberPlayerIds.Add(targetOpponentPlayerId);
        gameState.teams[enemyTeamId] = enemyTeamState;

        return gameState;
    }
    private static PlayerState createPlayerState(PlayerId playerId, TeamId teamId, long zoneIdBase)
    {
        return new PlayerState
        {
            playerId = playerId,
            teamId = teamId,
            deckZoneId = new ZoneId(zoneIdBase),
            handZoneId = new ZoneId(zoneIdBase + 1),
            discardZoneId = new ZoneId(zoneIdBase + 2),
            fieldZoneId = new ZoneId(zoneIdBase + 3),
            characterSetAsideZoneId = new ZoneId(zoneIdBase + 4),
        };
    }

    private static void addStandardPlayerZones(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState)
    {
        addZone(gameState, playerState.deckZoneId, ZoneKey.deck, playerState.playerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, playerState.handZoneId, ZoneKey.hand, playerState.playerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, playerState.discardZoneId, ZoneKey.discard, playerState.playerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, playerState.fieldZoneId, ZoneKey.field, playerState.playerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, playerState.characterSetAsideZoneId, ZoneKey.characterSetAside, playerState.playerId, ZonePublicOrPrivate.privateZone);
    }

    private static void addZone(
        RuleCore.GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey zoneKey,
        PlayerId ownerPlayerId,
        ZonePublicOrPrivate zonePublicOrPrivate)
    {
        gameState.zones[zoneId] = new ZoneState
        {
            zoneId = zoneId,
            zoneType = zoneKey,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = zonePublicOrPrivate,
        };
    }
}

















