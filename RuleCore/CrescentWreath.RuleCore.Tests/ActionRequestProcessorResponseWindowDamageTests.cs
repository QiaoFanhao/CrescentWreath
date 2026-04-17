using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorResponseWindowDamageTests
{
    private const string ContinuationKeyStagedResponseDamage = "continuation:stagedResponseDamage";

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_ScriptedOnPlayDeal1_ShouldOpenAndCloseWindowThenResolveDamage()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1000);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1100);
        var cardInstanceId = new CardInstanceId(7001);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(actorPlayerState.teamId, new TeamState
        {
            teamId = actorPlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetPlayerState.teamId, new TeamState
        {
            teamId = targetPlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayDeal1");

        var previousActionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(999),
        };
        gameState.currentActionChain = previousActionChain;

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 7002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();

        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.NotNull(gameState.currentActionChain);
        Assert.NotSame(previousActionChain, gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(9, targetCharacter.currentHp);

        Assert.Equal(5, producedEvents.Count);
        Assert.IsType<CardMovedEvent>(producedEvents[0]);

        var openedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[1]);
        Assert.True(openedEvent.isOpened);
        Assert.True(openedEvent.responseWindowId.HasValue);
        Assert.Equal(ResponseWindowOriginType.chain, openedEvent.responseWindowOriginType);

        var closedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[2]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal(openedEvent.responseWindowId, closedEvent.responseWindowId);
        Assert.Equal(ResponseWindowOriginType.chain, closedEvent.responseWindowOriginType);

        Assert.IsType<DamageResolvedEvent>(producedEvents[3]);
        Assert.IsType<HpChangedEvent>(producedEvents[4]);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_ScriptedOnPlayWaitResponseDamage_ThenSubmitResponse_ShouldContinueSameChainAndResolveDamage()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1200);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1300);
        var cardInstanceId = new CardInstanceId(7101);
        var targetCharacterInstanceId = new CharacterInstanceId(3001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(actorPlayerState.teamId, new TeamState
        {
            teamId = actorPlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetPlayerState.teamId, new TeamState
        {
            teamId = targetPlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();

        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7102,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var eventsAfterPlay = processor.processActionRequest(gameState, playRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.Equal(ContinuationKeyStagedResponseDamage, gameState.currentActionChain.pendingContinuationKey);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(ResponseWindowOriginType.flow, gameState.currentResponseWindow!.originType);
        Assert.Equal(targetCharacterInstanceId, gameState.currentResponseWindow.pendingDamageTargetCharacterInstanceId);
        Assert.Equal(1, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal(actorPlayerId, gameState.currentResponseWindow.pendingDamageSourcePlayerId);
        Assert.Equal(targetPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(10, targetCharacter.currentHp);

        Assert.Equal(2, eventsAfterPlay.Count);
        Assert.IsType<CardMovedEvent>(eventsAfterPlay[0]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterPlay[1]);
        Assert.True(openedEvent.isOpened);
        Assert.True(openedEvent.responseWindowId.HasValue);
        Assert.Equal(ResponseWindowOriginType.flow, openedEvent.responseWindowOriginType);

        var existingChain = gameState.currentActionChain;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7103,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitRequest);

        Assert.Same(existingChain, gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(9, targetCharacter.currentHp);

        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<CardMovedEvent>(finalEvents[0]);
        var reopenedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        Assert.True(reopenedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[2]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal(reopenedEvent.responseWindowId, closedEvent.responseWindowId);
        Assert.Equal(ResponseWindowOriginType.flow, closedEvent.responseWindowOriginType);
        Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.IsType<HpChangedEvent>(finalEvents[4]);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_ScriptedOnPlayWaitResponseDamage_WhenDamageKillsTarget_ShouldAppendKillAndRestoreHpAtChainTail()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1250);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1350);
        var cardInstanceId = new CardInstanceId(7121);
        var rewardCardInstanceId = new CardInstanceId(7124);
        var targetCharacterInstanceId = new CharacterInstanceId(3021);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(actorPlayerState.teamId, new TeamState
        {
            teamId = actorPlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetPlayerState.teamId, new TeamState
        {
            teamId = targetPlayerState.teamId,
            killScore = 1,
            leyline = 0,
        });
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");
        var rewardCard = new CardInstance
        {
            cardInstanceId = rewardCardInstanceId,
            definitionId = "reward-card",
            ownerPlayerId = actorPlayerId,
            zoneId = actorPlayerState.deckZoneId,
            zoneKey = ZoneKey.deck,
        };
        gameState.cardInstances.Add(rewardCardInstanceId, rewardCard);
        gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Add(rewardCardInstanceId);

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7122,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var eventsAfterPlay = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterPlay[1]);

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7123,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var eventsAfterDamageSubmit = processor.processActionRequest(gameState, submitRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);

        Assert.Equal(10, eventsAfterDamageSubmit.Count);
        Assert.IsType<CardMovedEvent>(eventsAfterDamageSubmit[0]);
        var responseOpenedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[1]);
        Assert.True(responseOpenedEvent.isOpened);
        var responseClosedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[2]);
        Assert.False(responseClosedEvent.isOpened);
        Assert.IsType<DamageResolvedEvent>(eventsAfterDamageSubmit[3]);

        var damageHpChangedEvent = Assert.IsType<HpChangedEvent>(eventsAfterDamageSubmit[4]);
        Assert.Equal(1, damageHpChangedEvent.hpBefore);
        Assert.Equal(0, damageHpChangedEvent.hpAfter);
        Assert.Equal(-1, damageHpChangedEvent.delta);

        var killWindowOpenedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[5]);
        Assert.True(killWindowOpenedEvent.isOpened);
        Assert.Equal("responseWindow", killWindowOpenedEvent.windowKindKey);

        var killWindowClosedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[6]);
        Assert.False(killWindowClosedEvent.isOpened);
        Assert.Equal("responseWindow", killWindowClosedEvent.windowKindKey);

        var killRecordedEvent = Assert.IsType<KillRecordedEvent>(eventsAfterDamageSubmit[7]);
        Assert.Equal(actorPlayerId, killRecordedEvent.killerPlayerId);
        Assert.Equal(targetCharacterInstanceId, killRecordedEvent.killedCharacterInstanceId);

        var rewardDrawEvent = Assert.IsType<CardMovedEvent>(eventsAfterDamageSubmit[8]);
        Assert.Equal(rewardCardInstanceId, rewardDrawEvent.cardInstanceId);
        Assert.Equal(ZoneKey.deck, rewardDrawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, rewardDrawEvent.toZoneKey);
        Assert.Equal(CardMoveReason.draw, rewardDrawEvent.moveReason);

        var restoreHpChangedEvent = Assert.IsType<HpChangedEvent>(eventsAfterDamageSubmit[9]);
        Assert.Equal(0, restoreHpChangedEvent.hpBefore);
        Assert.Equal(targetCharacter.maxHp, restoreHpChangedEvent.hpAfter);
        Assert.Equal(targetCharacter.maxHp, restoreHpChangedEvent.delta);
        Assert.Equal(10, gameState.teams[actorPlayerState.teamId].killScore);
        Assert.Equal(0, gameState.teams[targetPlayerState.teamId].killScore);
        Assert.Equal(MatchState.ended, gameState.matchState);
        Assert.Equal(actorPlayerState.teamId, gameState.winnerTeamId);
        Assert.Empty(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Contains(rewardCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
    }

    [Fact]
    public void SubmitResponse_WhenActorDoesNotMatchCurrentResponder_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var wrongActorPlayerId = new PlayerId(3);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1350);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1450);
        var wrongActorPlayerState = createPlayerState(wrongActorPlayerId, new TeamId(1), 1550);
        var cardInstanceId = new CardInstanceId(7151);
        var targetCharacterInstanceId = new CharacterInstanceId(3051);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.players.Add(wrongActorPlayerId, wrongActorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        addStandardPlayerZones(gameState, wrongActorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7152,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7153,
            actorPlayerId = wrongActorPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitResponseActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(targetPlayerId, gameState.currentResponseWindow!.currentResponderPlayerId);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_WithUnsupportedResponseValues_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1400);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1500);
        var cardInstanceId = new CardInstanceId(7201);
        var targetCharacterInstanceId = new CharacterInstanceId(4001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();

        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7202,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7203,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = true,
            responseKey = "counter",
        };

        var exception = Assert.Throws<NotSupportedException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitResponseActionRequest currently only supports no-response continuation (shouldRespond=false, responseKey=null).", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_WhenResponderHasSilenceAndShouldRespondIsTrue_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1404);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1504);
        var cardInstanceId = new CardInstanceId(7204);
        var targetCharacterInstanceId = new CharacterInstanceId(4004);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7205,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain.producedEvents.Count;
        applyPlayerSilenceStatus(gameState, targetPlayerId);

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7206,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = true,
            responseKey = "counter",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitResponseActionRequest actorPlayerId cannot respond while Silence status is active.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_WhenResponderHasSilenceAndShouldRespondIsFalse_ShouldAllowNoResponsePass()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1407);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1507);
        var cardInstanceId = new CardInstanceId(7207);
        var targetCharacterInstanceId = new CharacterInstanceId(4007);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");
        applyPlayerSilenceStatus(gameState, targetPlayerId);

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7208,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };
        var eventsAfterPlay = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterPlay[1]);

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7209,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(9, targetCharacter.currentHp);
        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<CardMovedEvent>(finalEvents[0]);
        Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        Assert.IsType<InteractionWindowEvent>(finalEvents[2]);
        Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.IsType<HpChangedEvent>(finalEvents[4]);
    }

    [Fact]
    public void SubmitDefense_ThenCounter_ShouldResolveBaseDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7230);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7330);
        var targetCharacterInstanceId = new CharacterInstanceId(7231);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7232,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]);
        Assert.True(openedEvent.isOpened);
        var existingActionChain = gameState.currentActionChain;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7233,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(7234),
            defenseTypeKey = "fixedReduce1",
        };

        var eventsAfterDefense = processor.processActionRequest(gameState, defenseRequest);
        Assert.Single(eventsAfterDefense);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("awaitCounter", gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal("fixedReduce1", gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(sourcePlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(10, targetCharacter.currentHp);

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 7235,
            actorPlayerId = sourcePlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            counterTypeKey = "cancelFixedReduce1",
        };

        var finalEvents = processor.processActionRequest(gameState, counterRequest);

        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(8, targetCharacter.currentHp);

        Assert.Equal(4, finalEvents.Count);
        openedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[0]);
        Assert.True(openedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal(openedEvent.responseWindowId, closedEvent.responseWindowId);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(2, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);

        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[3]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(8, hpChangedEvent.hpAfter);
        Assert.Equal(-2, hpChangedEvent.delta);
    }

    [Fact]
    public void SubmitDefense_ThenSourceNoResponse_ShouldResolveReducedDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7240);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7340);
        var targetCharacterInstanceId = new CharacterInstanceId(7241);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7242,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        processor.processActionRequest(gameState, openRequest);
        var responseWindow = gameState.currentResponseWindow!;
        var responseWindowId = responseWindow.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7243,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(7244),
            defenseTypeKey = "fixedReduce1",
        };

        var eventsAfterDefense = processor.processActionRequest(gameState, defenseRequest);
        Assert.Single(eventsAfterDefense);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("awaitCounter", gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal("fixedReduce1", gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(sourcePlayerId, gameState.currentResponseWindow.currentResponderPlayerId);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 7245,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(9, targetCharacter.currentHp);

        Assert.Equal(4, finalEvents.Count);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(1, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);

        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[3]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(9, hpChangedEvent.hpAfter);
        Assert.Equal(-1, hpChangedEvent.delta);
    }

    [Theory]
    [InlineData("physical", "test:defensePhysical2", "physical", 1, 9)]
    [InlineData("physical", "test:defenseSpell2", "spell", 3, 7)]
    [InlineData("spell", "test:defenseDual2", "dual", 1, 9)]
    public void SubmitDefense_WithDefinitionDefenseTypeMatchingRules_ShouldResolveExpectedDamage(
        string damageTypeKey,
        string defenseCardDefinitionId,
        string declaredDefenseTypeKey,
        int expectedFinalDamageValue,
        int expectedTargetHp)
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72410);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73410);
        var targetCharacterInstanceId = new CharacterInstanceId(72411);
        var defenseCardInstanceId = new CardInstanceId(72412);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, defenseCardDefinitionId);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72413,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageTypeKey = damageTypeKey,
        };

        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72414,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = declaredDefenseTypeKey,
        };

        var eventsAfterDefense = processor.processActionRequest(gameState, defenseRequest);
        Assert.Equal(2, eventsAfterDefense.Count);
        Assert.IsType<InteractionWindowEvent>(eventsAfterDefense[0]);
        var defensePlaceMovedEvent = Assert.IsType<CardMovedEvent>(eventsAfterDefense[1]);
        Assert.Equal(CardMoveReason.defensePlace, defensePlaceMovedEvent.moveReason);
        Assert.Equal(ZoneKey.hand, defensePlaceMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.field, defensePlaceMovedEvent.toZoneKey);
        Assert.Equal(targetPlayerState.fieldZoneId, gameState.cardInstances[defenseCardInstanceId].zoneId);
        Assert.True(gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField);
        Assert.Equal("awaitCounter", gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal(sourcePlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.StartsWith("cardDefense:", gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72415,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(expectedTargetHp, targetCharacter.currentHp);

        Assert.Equal(5, finalEvents.Count);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(expectedFinalDamageValue, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);
        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[4]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(expectedTargetHp, hpChangedEvent.hpAfter);
        Assert.Equal(-expectedFinalDamageValue, hpChangedEvent.delta);
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenCardAlreadyDefensePlacedOnField_ShouldNotMoveAgainAndShouldResolveDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72450);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73450);
        var targetCharacterInstanceId = new CharacterInstanceId(72451);
        var defenseCardInstanceId = new CardInstanceId(72452);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");
        var defenseCard = gameState.cardInstances[defenseCardInstanceId];
        gameState.zones[targetPlayerState.handZoneId].cardInstanceIds.Remove(defenseCardInstanceId);
        gameState.zones[targetPlayerState.fieldZoneId].cardInstanceIds.Add(defenseCardInstanceId);
        defenseCard.zoneId = targetPlayerState.fieldZoneId;
        defenseCard.zoneKey = ZoneKey.field;
        defenseCard.isDefensePlacedOnField = true;
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72453,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageTypeKey = "physical",
        };
        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72454,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };
        var eventsAfterDefense = processor.processActionRequest(gameState, defenseRequest);
        Assert.Single(eventsAfterDefense);
        Assert.Equal(targetPlayerState.fieldZoneId, defenseCard.zoneId);
        Assert.True(defenseCard.isDefensePlacedOnField);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72455,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(1, damageResolvedEvent.finalDamageValue);
        Assert.Equal(9, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenOwnerActiveCharacterHasSeal_ShouldReduceDefenseByOne()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72416);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73416);
        var targetCharacterInstanceId = new CharacterInstanceId(72417);
        var defenseCardInstanceId = new CardInstanceId(72418);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        targetPlayerState.activeCharacterInstanceId = targetCharacterInstanceId;
        applyCharacterSealStatus(gameState, targetCharacterInstanceId);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72419,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageTypeKey = "physical",
        };

        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72420,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };

        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72421,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(2, damageResolvedEvent.finalDamageValue);
        Assert.Equal(8, targetCharacter.currentHp);
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId),
            status => string.Equals(status.statusKey, "Seal", StringComparison.Ordinal));
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenSealAppliedOnOwnerActiveCharacterButTargetIsDifferent_ShouldStillReduceDefenseByOne()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72430);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73430);
        var targetCharacterInstanceId = new CharacterInstanceId(72431);
        var activeCharacterInstanceId = new CharacterInstanceId(72432);
        var defenseCardInstanceId = new CardInstanceId(72433);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createTargetCharacter(gameState, activeCharacterInstanceId, targetPlayerId, 4);
        targetPlayerState.activeCharacterInstanceId = activeCharacterInstanceId;
        applyCharacterSealStatus(gameState, activeCharacterInstanceId);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72434,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageTypeKey = "physical",
        };

        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72435,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };

        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72436,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(2, damageResolvedEvent.finalDamageValue);
        Assert.Equal(8, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenOwnerActiveCharacterHasSealAndDefenseDropsToZero_ShouldClampAtZero()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72440);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73440);
        var targetCharacterInstanceId = new CharacterInstanceId(72441);
        var defenseCardInstanceId = new CardInstanceId(72442);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        targetPlayerState.activeCharacterInstanceId = targetCharacterInstanceId;
        applyCharacterSealStatus(gameState, targetCharacterInstanceId);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical1");
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72443,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 1,
            damageTypeKey = "physical",
        };

        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72444,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };

        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72445,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<InteractionWindowEvent>(finalEvents[0]);
        Assert.IsType<CardMovedEvent>(finalEvents[1]);
        Assert.IsType<InteractionWindowEvent>(finalEvents[2]);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(1, damageResolvedEvent.finalDamageValue);
        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[4]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(9, hpChangedEvent.hpAfter);
        Assert.Equal(-1, hpChangedEvent.delta);
        Assert.Equal(9, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WithFormalDefenseType_WhenDefenseCardDoesNotExist_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72420);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73420);
        var targetCharacterInstanceId = new CharacterInstanceId(72421);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72422,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        processor.processActionRequest(gameState, openRequest);
        var existingResponseWindow = gameState.currentResponseWindow!;
        var pendingDefenseBefore = existingResponseWindow.pendingDamageDefenseDeclarationKey;
        var producedEventsBefore = gameState.currentActionChain!.producedEvents.Count;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72423,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(999001),
            defenseTypeKey = "physical",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, defenseRequest));

        Assert.Equal("SubmitDefenseActionRequest requires defenseCardInstanceId to exist in gameState.cardInstances.", exception.Message);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingDefenseBefore, gameState.currentResponseWindow!.pendingDamageDefenseDeclarationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_DefenderNoResponse_ShouldResolveBaseDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7246);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7346);
        var targetCharacterInstanceId = new CharacterInstanceId(7247);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7248,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 7249,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(8, targetCharacter.currentHp);

        Assert.Equal(4, finalEvents.Count);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(2, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);

        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[3]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(8, hpChangedEvent.hpAfter);
        Assert.Equal(-2, hpChangedEvent.delta);
    }

    [Fact]
    public void SubmitDefense_WhenDefenderHasCharm_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72461);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73461);
        var targetCharacterInstanceId = new CharacterInstanceId(72471);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyPlayerCharmStatus(gameState, targetPlayerId);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72481,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var stageBefore = existingResponseWindow!.pendingDamageResponseStageKey;
        var defenseBefore = existingResponseWindow.pendingDamageDefenseDeclarationKey;
        var producedEventsBefore = existingActionChain.producedEvents.Count;

        var submitDefenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72482,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(72483),
            defenseTypeKey = "fixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitDefenseRequest));

        Assert.Equal("SubmitDefenseActionRequest actorPlayerId cannot defend while Charm status is active.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(stageBefore, gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal(defenseBefore, gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Equal(openedEvent.responseWindowId, gameState.currentResponseWindow.responseWindowId);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_DefenderNoResponse_WhenDefenderHasCharm_ShouldStillResolveBaseDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72462);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73462);
        var targetCharacterInstanceId = new CharacterInstanceId(72472);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyPlayerCharmStatus(gameState, targetPlayerId);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72484,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72485,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(8, targetCharacter.currentHp);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetPlayerId == targetPlayerId &&
                      string.Equals(status.statusKey, "Charm", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(5, finalEvents.Count);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(2, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);
        Assert.IsType<HpChangedEvent>(finalEvents[3]);
        var charmRemovedEvent = Assert.IsType<StatusChangedEvent>(finalEvents[4]);
        Assert.Equal("Charm", charmRemovedEvent.statusKey);
        Assert.False(charmRemovedEvent.isApplied);
        Assert.Equal(targetPlayerId, charmRemovedEvent.targetPlayerId);
    }

    [Fact]
    public void SubmitDefense_AfterCharmConsumedByFirstDamageInSameTurn_ShouldNoLongerBeBlockedByCharm()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72463);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73463);
        var targetCharacterInstanceId = new CharacterInstanceId(72473);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyPlayerCharmStatus(gameState, targetPlayerId);
        var processor = new ActionRequestProcessor();

        var firstOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72486,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };
        var firstOpenEvents = processor.processActionRequest(gameState, firstOpenRequest);
        var firstResponseWindowId = Assert.IsType<InteractionWindowEvent>(firstOpenEvents[0]).responseWindowId!.Value;

        var firstSubmitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72487,
            actorPlayerId = targetPlayerId,
            responseWindowId = firstResponseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        processor.processActionRequest(gameState, firstSubmitNoResponseRequest);
        Assert.Equal(8, targetCharacter.currentHp);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetPlayerId == targetPlayerId &&
                      string.Equals(status.statusKey, "Charm", StringComparison.OrdinalIgnoreCase));

        var secondOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72488,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var secondOpenEvents = processor.processActionRequest(gameState, secondOpenRequest);
        var secondResponseWindowId = Assert.IsType<InteractionWindowEvent>(secondOpenEvents[0]).responseWindowId!.Value;

        var secondDefenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72489,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(0),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, secondDefenseRequest);

        var secondSubmitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72490,
            actorPlayerId = sourcePlayerId,
            responseWindowId = secondResponseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var secondFinalEvents = processor.processActionRequest(gameState, secondSubmitNoResponseRequest);

        var secondDamageResolvedEvent = Assert.IsType<DamageResolvedEvent>(secondFinalEvents[2]);
        Assert.Equal(1, secondDamageResolvedEvent.finalDamageValue);
        Assert.Equal(7, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_DefenderNoResponse_WhenBarrierAndCharmPresentAndDamageBecomesZero_ShouldConsumeBothStatuses()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72464);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73464);
        var targetCharacterInstanceId = new CharacterInstanceId(72474);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyCharacterBarrierStatus(gameState, targetCharacterInstanceId);
        applyPlayerCharmStatus(gameState, targetPlayerId);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72491,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var responseWindowId = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]).responseWindowId!.Value;

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72492,
            actorPlayerId = targetPlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(0, damageResolvedEvent.finalDamageValue);
        Assert.False(damageResolvedEvent.didDealDamage);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetCharacterInstanceId == targetCharacterInstanceId &&
                      string.Equals(status.statusKey, "Barrier", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetPlayerId == targetPlayerId &&
                      string.Equals(status.statusKey, "Charm", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            finalEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         string.Equals(statusChangedEvent.statusKey, "Barrier", StringComparison.Ordinal) &&
                         statusChangedEvent.targetCharacterInstanceId == targetCharacterInstanceId &&
                         !statusChangedEvent.isApplied);
        Assert.Contains(
            finalEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         string.Equals(statusChangedEvent.statusKey, "Charm", StringComparison.Ordinal) &&
                         statusChangedEvent.targetPlayerId == targetPlayerId &&
                         !statusChangedEvent.isApplied);
    }

    [Fact]
    public void SubmitResponse_DefenderNoResponse_WhenBarrierPresent_ShouldPreventDamageAndConsumeBarrier()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72491);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73491);
        var targetCharacterInstanceId = new CharacterInstanceId(72492);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyCharacterBarrierStatus(gameState, targetCharacterInstanceId);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72493,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };

        var eventsAfterOpen = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterOpen[0]);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72494,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetCharacterInstanceId == targetCharacterInstanceId &&
                      string.Equals(status.statusKey, "Barrier", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<InteractionWindowEvent>(finalEvents[0]);
        Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[2]);
        Assert.Equal(0, damageResolvedEvent.finalDamageValue);
        Assert.False(damageResolvedEvent.didDealDamage);
        var hpChangedEvent = Assert.IsType<HpChangedEvent>(finalEvents[3]);
        Assert.Equal(10, hpChangedEvent.hpBefore);
        Assert.Equal(10, hpChangedEvent.hpAfter);
        Assert.Equal(0, hpChangedEvent.delta);
        var statusChangedEvent = Assert.IsType<StatusChangedEvent>(finalEvents[4]);
        Assert.Equal("Barrier", statusChangedEvent.statusKey);
        Assert.False(statusChangedEvent.isApplied);
        Assert.Equal(targetCharacterInstanceId, statusChangedEvent.targetCharacterInstanceId);
    }

    [Fact]
    public void SubmitResponse_DefenderNoResponse_WhenBarrierConsumedOnce_ShouldNotPreventNextDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72495);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73495);
        var targetCharacterInstanceId = new CharacterInstanceId(72496);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyCharacterBarrierStatus(gameState, targetCharacterInstanceId);
        var processor = new ActionRequestProcessor();

        var firstOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72497,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var firstOpenEvents = processor.processActionRequest(gameState, firstOpenRequest);
        var firstWindowId = Assert.IsType<InteractionWindowEvent>(firstOpenEvents[0]).responseWindowId!.Value;
        var firstSubmitRequest = new SubmitResponseActionRequest
        {
            requestId = 72498,
            actorPlayerId = targetPlayerId,
            responseWindowId = firstWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var firstFinalEvents = processor.processActionRequest(gameState, firstSubmitRequest);
        var firstDamageResolved = Assert.IsType<DamageResolvedEvent>(firstFinalEvents[2]);
        Assert.Equal(0, firstDamageResolved.finalDamageValue);
        Assert.Equal(10, targetCharacter.currentHp);

        var secondOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72499,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var secondOpenEvents = processor.processActionRequest(gameState, secondOpenRequest);
        var secondWindowId = Assert.IsType<InteractionWindowEvent>(secondOpenEvents[0]).responseWindowId!.Value;
        var secondSubmitRequest = new SubmitResponseActionRequest
        {
            requestId = 72500,
            actorPlayerId = targetPlayerId,
            responseWindowId = secondWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var secondFinalEvents = processor.processActionRequest(gameState, secondSubmitRequest);
        var secondDamageResolved = Assert.IsType<DamageResolvedEvent>(secondFinalEvents[2]);
        Assert.Equal(2, secondDamageResolved.finalDamageValue);
        Assert.Equal(8, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenPenetratePresentAndFullyDefended_ShouldDealOneAndConsumePenetrate()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72501);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73501);
        var targetCharacterInstanceId = new CharacterInstanceId(72502);
        var defenseCardInstanceId = new CardInstanceId(72503);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");
        applyNextDamagePenetrateStatus(gameState, sourcePlayerId);

        var processor = new ActionRequestProcessor();
        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72504,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72505,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72506,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(1, damageResolvedEvent.finalDamageValue);
        Assert.True(damageResolvedEvent.didDealDamage);
        Assert.Equal(9, targetCharacter.currentHp);
        var statusChangedEvent = Assert.IsType<StatusChangedEvent>(finalEvents[5]);
        Assert.Equal("Penetrate", statusChangedEvent.statusKey);
        Assert.False(statusChangedEvent.isApplied);
        Assert.Equal(sourcePlayerId, statusChangedEvent.targetPlayerId);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.targetPlayerId == sourcePlayerId &&
                      string.Equals(status.statusKey, "Penetrate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SubmitDefense_WithFormalDefense_WhenNoPenetrateAndFullyDefended_ShouldRemainZero()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72511);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73511);
        var targetCharacterInstanceId = new CharacterInstanceId(72512);
        var defenseCardInstanceId = new CardInstanceId(72513);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");

        var processor = new ActionRequestProcessor();
        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72514,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        processor.processActionRequest(gameState, openRequest);
        var responseWindowId = gameState.currentResponseWindow!.responseWindowId;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72515,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72516,
            actorPlayerId = sourcePlayerId,
            responseWindowId = responseWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var finalEvents = processor.processActionRequest(gameState, submitNoResponseRequest);

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.Equal(0, damageResolvedEvent.finalDamageValue);
        Assert.False(damageResolvedEvent.didDealDamage);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WhenPenetrateConsumedByBarrier_ShouldNotApplyOnNextFullyDefendedDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72521);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73521);
        var targetCharacterInstanceId = new CharacterInstanceId(72522);
        var defenseCardInstanceId = new CardInstanceId(72523);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        applyCharacterBarrierStatus(gameState, targetCharacterInstanceId);
        applyNextDamagePenetrateStatus(gameState, sourcePlayerId);
        createCardInPlayerHand(gameState, targetPlayerState, defenseCardInstanceId, "test:defensePhysical2");

        var processor = new ActionRequestProcessor();

        var firstOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72524,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        var firstOpenEvents = processor.processActionRequest(gameState, firstOpenRequest);
        var firstWindowId = Assert.IsType<InteractionWindowEvent>(firstOpenEvents[0]).responseWindowId!.Value;
        var firstNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72525,
            actorPlayerId = targetPlayerId,
            responseWindowId = firstWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var firstFinalEvents = processor.processActionRequest(gameState, firstNoResponseRequest);
        var firstDamageResolved = Assert.IsType<DamageResolvedEvent>(firstFinalEvents[2]);
        Assert.Equal(0, firstDamageResolved.finalDamageValue);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.IsType<StatusChangedEvent>(firstFinalEvents[4]);
        Assert.IsType<StatusChangedEvent>(firstFinalEvents[5]);

        var secondOpenRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72526,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageTypeKey = "physical",
        };
        processor.processActionRequest(gameState, secondOpenRequest);
        var secondWindowId = gameState.currentResponseWindow!.responseWindowId;

        var secondDefenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72527,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = defenseCardInstanceId,
            defenseTypeKey = "physical",
        };
        processor.processActionRequest(gameState, secondDefenseRequest);

        var secondNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72528,
            actorPlayerId = sourcePlayerId,
            responseWindowId = secondWindowId,
            shouldRespond = false,
            responseKey = null,
        };
        var secondFinalEvents = processor.processActionRequest(gameState, secondNoResponseRequest);
        var secondDamageResolved = Assert.IsType<DamageResolvedEvent>(secondFinalEvents[3]);
        Assert.Equal(0, secondDamageResolved.finalDamageValue);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_ThenSourceNoResponse_WhenLethal_ShouldOpenOnKilledResponseAndCommitToEnded()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72461);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73461);
        var targetCharacterInstanceId = new CharacterInstanceId(72462);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(sourcePlayerState.teamId, new TeamState
        {
            teamId = sourcePlayerState.teamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetPlayerState.teamId, new TeamState
        {
            teamId = targetPlayerState.teamId,
            killScore = 1,
            leyline = 0,
        });
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72463,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        var openEvents = processor.processActionRequest(gameState, openRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[0]);

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72464,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(72465),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var submitNoResponseRequest = new SubmitResponseActionRequest
        {
            requestId = 72466,
            actorPlayerId = sourcePlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var eventsAfterDamageSubmit = processor.processActionRequest(gameState, submitNoResponseRequest);

        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(8, eventsAfterDamageSubmit.Count);
        Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[0]);
        Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[1]);
        Assert.IsType<DamageResolvedEvent>(eventsAfterDamageSubmit[2]);
        Assert.IsType<HpChangedEvent>(eventsAfterDamageSubmit[3]);
        var killWindowOpenedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[4]);
        Assert.True(killWindowOpenedEvent.isOpened);
        Assert.Equal(MatchState.ended, gameState.matchState);
        Assert.Equal(sourcePlayerState.teamId, gameState.winnerTeamId);
        Assert.Equal(1, gameState.teams[sourcePlayerState.teamId].leyline);
        Assert.Equal(0, gameState.teams[targetPlayerState.teamId].leyline);
        Assert.Equal(targetCharacter.maxHp, targetCharacter.currentHp);
        Assert.IsType<InteractionWindowEvent>(eventsAfterDamageSubmit[5]);
        Assert.IsType<KillRecordedEvent>(eventsAfterDamageSubmit[6]);
        Assert.IsType<HpChangedEvent>(eventsAfterDamageSubmit[7]);
    }

    [Fact]
    public void SubmitDefense_WhenActorIsNotPendingDamageDefender_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7250);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7350);
        var targetCharacterInstanceId = new CharacterInstanceId(7251);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7252,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        processor.processActionRequest(gameState, openRequest);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7253,
            actorPlayerId = sourcePlayerId,
            defenseCardInstanceId = new CardInstanceId(7254),
            defenseTypeKey = "fixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, defenseRequest));

        Assert.Equal("SubmitDefenseActionRequest actorPlayerId must equal pending damage defender player.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72711);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73711);
        var targetCharacterInstanceId = new CharacterInstanceId(72712);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72713,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };
        processor.processActionRequest(gameState, openRequest);

        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;
        var pendingContinuationKeyBefore = existingActionChain.pendingContinuationKey;

        gameState.matchState = MatchState.ended;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72714,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(72715),
            defenseTypeKey = "fixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, defenseRequest));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDamageCounter_WhenDefenseWasNotDeclared_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7260);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7360);
        var targetCharacterInstanceId = new CharacterInstanceId(7261);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7262,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        processor.processActionRequest(gameState, openRequest);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 7263,
            actorPlayerId = sourcePlayerId,
            responseWindowId = existingResponseWindow!.responseWindowId,
            counterTypeKey = "cancelFixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, counterRequest));

        Assert.Equal("SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitCounter.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDamageCounter_WhenActorIsNotCurrentResponder_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7265);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7365);
        var targetCharacterInstanceId = new CharacterInstanceId(7266);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7267,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        processor.processActionRequest(gameState, openRequest);
        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7268,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(7269),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 7270,
            actorPlayerId = targetPlayerId,
            responseWindowId = existingResponseWindow!.responseWindowId,
            counterTypeKey = "cancelFixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, counterRequest));

        Assert.Equal("SubmitDamageCounterActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDamageCounter_WhenAwaitCounterResponderIsDefender_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72695);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73695);
        var targetCharacterInstanceId = new CharacterInstanceId(72696);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72697,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };
        processor.processActionRequest(gameState, openRequest);

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72698,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(72699),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;
        var stageBefore = existingResponseWindow!.pendingDamageResponseStageKey;
        var defenseBefore = existingResponseWindow.pendingDamageDefenseDeclarationKey;

        existingResponseWindow.currentResponderPlayerId = targetPlayerId;

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 72700,
            actorPlayerId = targetPlayerId,
            responseWindowId = existingResponseWindow.responseWindowId,
            counterTypeKey = "cancelFixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, counterRequest));

        Assert.Equal(
            "SubmitDamageCounterActionRequest requires currentResponseWindow.currentResponderPlayerId to match pending damage source player while stage is awaitCounter.",
            exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(stageBefore, gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal(defenseBefore, gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDamageCounter_WithUnsupportedCounterType_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7271);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7371);
        var targetCharacterInstanceId = new CharacterInstanceId(7272);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7273,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };
        processor.processActionRequest(gameState, openRequest);

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7274,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(7275),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, defenseRequest);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 7276,
            actorPlayerId = sourcePlayerId,
            responseWindowId = existingResponseWindow!.responseWindowId,
            counterTypeKey = "unsupported",
        };

        var exception = Assert.Throws<NotSupportedException>(
            () => processor.processActionRequest(gameState, counterRequest));

        Assert.Equal("SubmitDamageCounterActionRequest currently only supports counterTypeKey=cancelFixedReduce1.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDamageCounter_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 72721);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 73721);
        var targetCharacterInstanceId = new CharacterInstanceId(72722);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 72723,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };
        processor.processActionRequest(gameState, openRequest);

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 72724,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(72725),
            defenseTypeKey = "fixedReduce1",
        };
        processor.processActionRequest(gameState, defenseRequest);

        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;
        var pendingContinuationKeyBefore = existingActionChain.pendingContinuationKey;

        gameState.matchState = MatchState.ended;

        var counterRequest = new SubmitDamageCounterActionRequest
        {
            requestId = 72726,
            actorPlayerId = sourcePlayerId,
            responseWindowId = existingResponseWindow!.responseWindowId,
            counterTypeKey = "cancelFixedReduce1",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, counterRequest));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitDefense_WithUnsupportedDefenseType_ShouldThrowAndKeepStateUnchanged()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7277);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7377);
        var targetCharacterInstanceId = new CharacterInstanceId(7278);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, sourcePlayerId, sourcePlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var processor = new ActionRequestProcessor();

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7279,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
        };

        processor.processActionRequest(gameState, openRequest);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        var defenseRequest = new SubmitDefenseActionRequest
        {
            requestId = 7280,
            actorPlayerId = targetPlayerId,
            defenseCardInstanceId = new CardInstanceId(7281),
            defenseTypeKey = "unsupported",
        };

        var exception = Assert.Throws<NotSupportedException>(
            () => processor.processActionRequest(gameState, defenseRequest));

        Assert.Equal("SubmitDefenseActionRequest currently only supports defenseTypeKey=fixedReduce1.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void OpenDamageResponseWindow_HappyPath_ShouldSetPendingContinuationKeyAndOpenWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7282);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7382);
        var targetCharacterInstanceId = new CharacterInstanceId(8999);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7241,
            actorPlayerId = actorPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 1,
        };

        var events = processor.processActionRequest(gameState, openRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.False(gameState.currentActionChain!.isCompleted);
        Assert.Equal(ContinuationKeyStagedResponseDamage, gameState.currentActionChain.pendingContinuationKey);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(actorPlayerId, gameState.currentResponseWindow!.pendingDamageSourcePlayerId);
        Assert.Equal(1, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal(targetCharacterInstanceId, gameState.currentResponseWindow.pendingDamageTargetCharacterInstanceId);
        Assert.Equal(targetPlayerId, gameState.currentResponseWindow.pendingDamageDefenderPlayerId);
        Assert.Equal("awaitDefense", gameState.currentResponseWindow.pendingDamageResponseStageKey);
        Assert.Null(gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(targetPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);

        Assert.Single(events);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[0]);
        Assert.True(openedEvent.isOpened);
    }

    [Fact]
    public void OpenDamageResponseWindow_WhenMatchStateIsNotRunning_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));
        gameState.matchState = MatchState.initializing;

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7251,
            actorPlayerId = actorPlayerId,
            targetCharacterInstanceId = new CharacterInstanceId(9001),
            baseDamageValue = 1,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, openRequest));

        Assert.Equal("OpenDamageResponseWindowActionRequest requires gameState.matchState to be running.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void OpenDamageResponseWindow_WhenTurnStateIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));
        gameState.turnState = null;

        var openRequest = new OpenDamageResponseWindowActionRequest
        {
            requestId = 7261,
            actorPlayerId = actorPlayerId,
            targetCharacterInstanceId = new CharacterInstanceId(9002),
            baseDamageValue = 1,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, openRequest));

        Assert.Equal("OpenDamageResponseWindowActionRequest requires gameState.turnState to be initialized.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void SubmitResponse_WhenMatchStateIsNotRunning_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1420);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1520);
        var cardInstanceId = new CardInstanceId(7211);
        var targetCharacterInstanceId = new CharacterInstanceId(4011);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7212,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        gameState.matchState = MatchState.initializing;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7213,
            actorPlayerId = actorPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitResponseActionRequest requires gameState.matchState to be running.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1425);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1525);
        var cardInstanceId = new CardInstanceId(7216);
        var targetCharacterInstanceId = new CharacterInstanceId(4016);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7217,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain.producedEvents.Count;

        gameState.matchState = MatchState.ended;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7218,
            actorPlayerId = actorPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void SubmitResponse_OnKilledResponse_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var gameState = new RuleCore.GameState.GameState();
        var actorPlayerId = new PlayerId(2);
        var actionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(72162),
            pendingContinuationKey = "continuation:onKilledResponse",
            isCompleted = false,
        };
        actionChain.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = 72162,
            eventTypeKey = "responseWindowOpened",
            windowKindKey = "responseWindow",
            responseWindowId = new ResponseWindowId(72162),
            isOpened = true,
        });
        gameState.currentActionChain = actionChain;
        var responseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(72162),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "onKilledResponse",
            currentResponderPlayerId = actorPlayerId,
        };
        gameState.currentResponseWindow = responseWindow;
        var producedEventsBefore = actionChain.producedEvents.Count;
        var pendingContinuationKeyBefore = actionChain.pendingContinuationKey;
        gameState.matchState = MatchState.ended;

        var processor = new ActionRequestProcessor();
        var submitKillRequest = new SubmitResponseActionRequest
        {
            requestId = 72164,
            actorPlayerId = actorPlayerId,
            responseWindowId = responseWindow.responseWindowId,
            shouldRespond = true,
            responseKey = "commitKill",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitKillRequest));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(actionChain, gameState.currentActionChain);
        Assert.Same(responseWindow, gameState.currentResponseWindow);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    [Fact]
    public void SubmitResponse_WhenTurnStateIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1430);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1530);
        var cardInstanceId = new CardInstanceId(7221);
        var targetCharacterInstanceId = new CharacterInstanceId(4021);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var processor = new ActionRequestProcessor();
        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7222,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var openEvents = processor.processActionRequest(gameState, playRequest);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(openEvents[1]);
        var existingActionChain = gameState.currentActionChain;
        var existingResponseWindow = gameState.currentResponseWindow;
        var pendingContinuationKeyBefore = existingActionChain!.pendingContinuationKey;
        var producedEventsBefore = existingActionChain!.producedEvents.Count;

        gameState.turnState = null;

        var submitRequest = new SubmitResponseActionRequest
        {
            requestId = 7223,
            actorPlayerId = actorPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitResponseActionRequest requires gameState.turnState to be initialized.", exception.Message);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_ScriptedOnPlayWaitResponseDamage_WhenCurrentResponseWindowAlreadyExists_ShouldThrowAndKeepWindowUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1600);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 1700);
        var cardInstanceId = new CardInstanceId(7301);
        var targetCharacterInstanceId = new CharacterInstanceId(5001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var existingResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(9901),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "existingWindow",
            sourceActionChainId = new ActionChainId(9901),
        };
        gameState.currentResponseWindow = existingResponseWindow;

        var playRequest = new PlayTreasureCardActionRequest
        {
            requestId = 7302,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, playRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, gameState.cardInstances[cardInstanceId].zoneId);
        Assert.Equal(ZoneKey.hand, gameState.cardInstances[cardInstanceId].zoneKey);
        Assert.Equal(10, targetCharacter.currentHp);
    }

    private static void applyCharacterBarrierStatus(
        RuleCore.GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Barrier",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilConsumed",
                stackCount = 1,
            });
    }

    private static void applyCharacterSealStatus(
        RuleCore.GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
                stackCount = 1,
            });
    }

    private static void applyPlayerSilenceStatus(
        RuleCore.GameState.GameState gameState,
        PlayerId targetPlayerId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Silence",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "untilEndOfTurn",
                stackCount = 1,
            });
    }

    private static void applyPlayerCharmStatus(
        RuleCore.GameState.GameState gameState,
        PlayerId targetPlayerId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Charm",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "untilEndOfTurn",
                stackCount = 1,
            });
    }

    private static void applyNextDamagePenetrateStatus(
        RuleCore.GameState.GameState gameState,
        PlayerId targetPlayerId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Penetrate",
                targetPlayerId = targetPlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                stackCount = 1,
            });
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

    private static void addStandardPlayerZones(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones.Add(
            playerState.deckZoneId,
            new ZoneState
            {
                zoneId = playerState.deckZoneId,
                zoneType = ZoneKey.deck,
                ownerPlayerId = playerState.playerId,
                publicOrPrivate = ZonePublicOrPrivate.privateZone,
            });

        gameState.zones.Add(
            playerState.handZoneId,
            new ZoneState
            {
                zoneId = playerState.handZoneId,
                zoneType = ZoneKey.hand,
                ownerPlayerId = playerState.playerId,
                publicOrPrivate = ZonePublicOrPrivate.privateZone,
            });

        gameState.zones.Add(
            playerState.discardZoneId,
            new ZoneState
            {
                zoneId = playerState.discardZoneId,
                zoneType = ZoneKey.discard,
                ownerPlayerId = playerState.playerId,
                publicOrPrivate = ZonePublicOrPrivate.publicZone,
            });

        gameState.zones.Add(
            playerState.fieldZoneId,
            new ZoneState
            {
                zoneId = playerState.fieldZoneId,
                zoneType = ZoneKey.field,
                ownerPlayerId = playerState.playerId,
                publicOrPrivate = ZonePublicOrPrivate.publicZone,
            });

        gameState.zones.Add(
            playerState.characterSetAsideZoneId,
            new ZoneState
            {
                zoneId = playerState.characterSetAsideZoneId,
                zoneType = ZoneKey.characterSetAside,
                ownerPlayerId = playerState.playerId,
                publicOrPrivate = ZonePublicOrPrivate.privateZone,
            });
    }

    private static void createCardInPlayerHand(
        RuleCore.GameState.GameState gameState,
        PlayerState ownerPlayerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerState.playerId,
            zoneId = ownerPlayerState.handZoneId,
            zoneKey = ZoneKey.hand,
        };

        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[ownerPlayerState.handZoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static CharacterInstance createTargetCharacter(
        RuleCore.GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        PlayerId ownerPlayerId,
        int currentHp)
    {
        var targetCharacter = new CharacterInstance
        {
            characterInstanceId = targetCharacterInstanceId,
            definitionId = "target-character",
            ownerPlayerId = ownerPlayerId,
            currentHp = currentHp,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances.Add(targetCharacterInstanceId, targetCharacter);
        return targetCharacter;
    }

    private static void setRunningTurnForActor(
        RuleCore.GameState.GameState gameState,
        PlayerId currentPlayerId,
        TeamId currentTeamId)
    {
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = currentPlayerId,
            currentTeamId = currentTeamId,
            currentPhase = TurnPhase.action,
            phaseStepIndex = 0,
        };
    }
}
