using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class UnitTest1
{
    [Fact]
    public void ProcessPlayCardRequest_M2HappyPath_ShouldMoveCardAndProduceCardMovedEvent()
    {
        var playerId = new PlayerId(1);
        var playerState = createPlayerState(playerId, new TeamId(1), 100);
        var cardInstanceId = new CardInstanceId(1001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(playerId, playerState);
        addStandardPlayerZones(gameState, playerState);
        setRunningTurnForActor(gameState, playerId, playerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, playerState, cardInstanceId, "test-card");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 42,
            actorPlayerId = playerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();

        var events = actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[playerState.handZoneId].cardInstanceIds);
        Assert.Contains(cardInstanceId, gameState.zones[playerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(playerState.fieldZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.field, cardInstance.zoneKey);
        Assert.Single(events);
        Assert.IsType<CardMovedEvent>(events[0]);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenMatchStateIsNotRunning_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 150);
        var cardInstanceId = new CardInstanceId(1501);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        gameState.matchState = MatchState.initializing;

        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test-card");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1502,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.matchState to be running.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 151);
        var cardInstanceId = new CardInstanceId(1511);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test-card");
        var sentinelActionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(1512),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelActionChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 1512,
            eventTypeKey = "actionAccepted",
            requestId = 1512,
            actorPlayerId = actorPlayerId,
            requestTypeKey = "sentinel",
        });
        gameState.currentActionChain = sentinelActionChain;
        var producedEventsBefore = sentinelActionChain.producedEvents.Count;
        gameState.matchState = MatchState.ended;

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1513,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(sentinelActionChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenActorIsNotCurrentTurnPlayer_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var nonCurrentPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 160);
        var nonCurrentPlayerState = createPlayerState(nonCurrentPlayerId, new TeamId(2), 260);
        var cardInstanceId = new CardInstanceId(1601);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(nonCurrentPlayerId, nonCurrentPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, nonCurrentPlayerState);
        setRunningTurnForActor(gameState, nonCurrentPlayerId, nonCurrentPlayerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test-card");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1602,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenCurrentInputContextIsActive_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 165);
        var cardInstanceId = new CardInstanceId(1651);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test-card");
        var existingInputContext = new InputContextState
        {
            inputContextId = new InputContextId(1652),
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = new ActionChainId(1653),
            inputTypeKey = "test-input",
            contextKey = "test-context",
        };
        existingInputContext.choiceKeys.Add("choice");
        gameState.currentInputContext = existingInputContext;

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1654,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.currentInputContext to be null.", exception.Message);
        Assert.Same(existingInputContext, gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenCurrentResponseWindowIsActive_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 166);
        var cardInstanceId = new CardInstanceId(1661);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test-card");
        var existingResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(1662),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "test-window",
            sourceActionChainId = new ActionChainId(1663),
        };
        gameState.currentResponseWindow = existingResponseWindow;

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1664,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenActorTriesToPlayAnotherPlayersCard_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var ownerPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 170);
        var ownerPlayerState = createPlayerState(ownerPlayerId, new TeamId(2), 270);
        var cardInstanceId = new CardInstanceId(1701);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(ownerPlayerId, ownerPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, ownerPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var cardInstance = createCardInPlayerHand(gameState, ownerPlayerState, cardInstanceId, "test-card");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1702,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires cardInstance.ownerPlayerId to equal actorPlayerId.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[ownerPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(ownerPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_WhenCardIsNotInActorsHand_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 180);
        var cardInstanceId = new CardInstanceId(1801);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-card",
            ownerPlayerId = actorPlayerId,
            zoneId = actorPlayerState.discardZoneId,
            zoneKey = ZoneKey.discard,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Add(cardInstanceId);

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 1802,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires cardInstance.zoneId to equal actor player's handZoneId.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.discardZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.discard, cardInstance.zoneKey);
    }

    [Fact]
    public void ProcessPlayCardRequest_ScriptedOnPlayDamage_ShouldStayInSingleChainAndProduceOrderedEvents()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 200);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 300);
        var cardInstanceId = new CardInstanceId(3001);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayDeal1");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 9001,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();

        var producedEvents = actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<PlayTreasureCardActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.Equal(new ActionChainId(playTreasureCardActionRequest.requestId), gameState.currentActionChain.actionChainId);

        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.fieldZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.field, cardInstance.zoneKey);

        Assert.Equal(5, producedEvents.Count);
        Assert.IsType<CardMovedEvent>(producedEvents[0]);

        var responseOpenedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[1]);
        Assert.True(responseOpenedEvent.isOpened);

        var responseClosedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[2]);
        Assert.False(responseClosedEvent.isOpened);

        Assert.IsType<DamageResolvedEvent>(producedEvents[3]);
        Assert.IsType<HpChangedEvent>(producedEvents[4]);
        Assert.Equal(9, targetCharacter.currentHp);
    }

    [Fact]
    public void ProcessPlayCardRequest_ScriptedOnPlayChooseDamage_ShouldOpenInputThenSubmitAndResolveDamageOnSameChain()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 400);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 500);
        var cardInstanceId = new CardInstanceId(3101);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayChooseDamage");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 9101,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();

        var eventsAfterPlay = actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest);
        var playChain = gameState.currentActionChain;

        Assert.NotNull(playChain);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("continuation:inputChoiceDamage", playChain!.pendingContinuationKey);
        Assert.False(playChain.isCompleted);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Equal(2, eventsAfterPlay.Count);
        Assert.IsType<CardMovedEvent>(eventsAfterPlay[0]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterPlay[1]);
        Assert.True(openedEvent.isOpened);
        Assert.Equal("inputContext", openedEvent.windowKindKey);
        gameState.currentInputContext!.contextKey = "mutated:nonDecisionKey";

        var submitInputChoiceActionRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 9102,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "deal1",
        };

        var finalEvents = actionRequestProcessor.processActionRequest(gameState, submitInputChoiceActionRequest);

        Assert.Same(playChain, gameState.currentActionChain);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(9, targetCharacter.currentHp);

        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<CardMovedEvent>(finalEvents[0]);
        var inputOpenedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        Assert.True(inputOpenedEvent.isOpened);
        var inputClosedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[2]);
        Assert.False(inputClosedEvent.isOpened);
        Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.IsType<HpChangedEvent>(finalEvents[4]);
    }

    [Fact]
    public void ProcessPlayCardRequest_ScriptedOnPlayWaitResponseDamage_ShouldOpenWindowThenSubmitAndResolveDamageOnSameChain()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 600);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 700);
        var cardInstanceId = new CardInstanceId(3201);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 9201,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();

        var eventsAfterPlay = actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest);
        var playChain = gameState.currentActionChain;

        Assert.NotNull(playChain);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(ResponseWindowOriginType.flow, gameState.currentResponseWindow!.originType);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.fieldZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.field, cardInstance.zoneKey);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Equal(2, eventsAfterPlay.Count);
        Assert.IsType<CardMovedEvent>(eventsAfterPlay[0]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterPlay[1]);
        Assert.True(openedEvent.isOpened);
        Assert.True(openedEvent.responseWindowId.HasValue);
        Assert.Equal(ResponseWindowOriginType.flow, openedEvent.responseWindowOriginType);

        var submitResponseActionRequest = new SubmitResponseActionRequest
        {
            requestId = 9202,
            actorPlayerId = targetPlayerId,
            responseWindowId = openedEvent.responseWindowId!.Value,
            shouldRespond = false,
            responseKey = null,
        };

        var finalEvents = actionRequestProcessor.processActionRequest(gameState, submitResponseActionRequest);

        Assert.Same(playChain, gameState.currentActionChain);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(9, targetCharacter.currentHp);
        Assert.Equal(5, finalEvents.Count);
        Assert.IsType<CardMovedEvent>(finalEvents[0]);
        var responseOpenedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[1]);
        Assert.True(responseOpenedEvent.isOpened);
        var responseClosedEvent = Assert.IsType<InteractionWindowEvent>(finalEvents[2]);
        Assert.False(responseClosedEvent.isOpened);
        Assert.Equal(ResponseWindowOriginType.flow, responseClosedEvent.responseWindowOriginType);
        Assert.IsType<DamageResolvedEvent>(finalEvents[3]);
        Assert.IsType<HpChangedEvent>(finalEvents[4]);
    }

    [Fact]
    public void ProcessPlayCardRequest_ScriptedOnPlayWaitResponseDamage_WhenCurrentResponseWindowAlreadyExists_ShouldThrowAndNotAppendResponseOpened()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 800);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 900);
        var cardInstanceId = new CardInstanceId(3301);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        addStandardPlayerZones(gameState, targetPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var cardInstance = createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:onPlayWaitResponseDamage");

        var existingResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(9801),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "existingWindow",
            sourceActionChainId = new ActionChainId(9801),
        };
        gameState.currentResponseWindow = existingResponseWindow;

        var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
        {
            requestId = 9301,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var actionRequestProcessor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        Assert.Same(existingResponseWindow, gameState.currentResponseWindow);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Null(gameState.currentActionChain);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
    }

    [Fact]
    public void ResolveDamage_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var targetPlayerId = new PlayerId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(19991);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.ended,
        };
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(19992),
            sourcePlayerId = new PlayerId(1),
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => damageProcessor.resolveDamage(gameState, damageContext));

        Assert.Equal("DamageProcessor cannot accept external resolution calls when gameState.matchState is ended.", exception.Message);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDirectKill_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var targetPlayerId = new PlayerId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(19993);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.ended,
        };
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        var killContext = new KillContext
        {
            killContextId = 19994,
            killerPlayerId = new PlayerId(1),
            killedCharacterInstanceId = targetCharacterInstanceId,
            causedByDamage = false,
        };

        var damageProcessor = new DamageProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => damageProcessor.resolveDirectKill(gameState, killContext));

        Assert.Equal("DamageProcessor cannot accept external resolution calls when gameState.matchState is ended.", exception.Message);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolvePendingKillResponse_WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var targetPlayerId = new PlayerId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(19995);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.ended,
        };
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(19996),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "onKilledResponse",
            pendingKillTargetCharacterInstanceId = targetCharacterInstanceId,
            pendingKillKillerPlayerId = new PlayerId(1),
        };

        var damageProcessor = new DamageProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => damageProcessor.resolvePendingKillResponse(gameState, responseWindowState, "commitKill", 19997));

        Assert.Equal("DamageProcessor cannot accept external resolution calls when gameState.matchState is ended.", exception.Message);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDamage_HappyPath_ShouldUpdateHpAndProduceDamageEvents()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5100);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8100);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8200);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        createCardInZone(gameState, sourcePlayerState, sourceRewardCardInstanceId, "reward-card", sourcePlayerState.deckZoneId, ZoneKey.deck);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.matchState = MatchState.running;

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5001),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();

        var events = damageProcessor.resolveDamage(gameState, damageContext);

        Assert.Equal(7, targetCharacter.currentHp);
        Assert.Equal(3, damageContext.finalDamageValue);
        Assert.True(damageContext.didDealDamage);
        Assert.True(targetCharacter.isAlive);
        Assert.True(targetCharacter.isInPlay);
        Assert.Equal(2, events.Count);
        Assert.IsType<DamageResolvedEvent>(events[0]);
        Assert.IsType<HpChangedEvent>(events[1]);
        Assert.DoesNotContain(events, static e => e is KillRecordedEvent);
        Assert.DoesNotContain(events, static e => e is InteractionWindowEvent);
        Assert.DoesNotContain(events, static e => e is CardMovedEvent);
        Assert.Equal(1, gameState.teams[sourceTeamId].leyline);
        Assert.Equal(0, gameState.teams[targetTeamId].leyline);
        Assert.Equal(10, gameState.teams[targetTeamId].killScore);
        Assert.Empty(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Equal(sourceRewardCardInstanceId, gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds[0]);
    }

    [Fact]
    public void ResolveDamage_WhenFinalDamageIsZero_ShouldNotGrantLeyline()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(2009);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8850);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8860);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5005),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 0,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var events = damageProcessor.resolveDamage(gameState, damageContext);

        Assert.False(damageContext.didDealDamage);
        Assert.Equal(10, targetCharacter.currentHp);
        Assert.Equal(2, events.Count);
        Assert.IsType<DamageResolvedEvent>(events[0]);
        Assert.IsType<HpChangedEvent>(events[1]);
        Assert.Equal(0, gameState.teams[sourceTeamId].leyline);
        Assert.Equal(0, gameState.teams[targetTeamId].leyline);
    }

    [Fact]
    public void ResolveDamage_WhenSourcePlayerIdIsNull_ShouldNotGrantLeyline()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(2012);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8870);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8880);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 10);
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5006),
            sourcePlayerId = null,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var events = damageProcessor.resolveDamage(gameState, damageContext);

        Assert.True(damageContext.didDealDamage);
        Assert.Equal(8, targetCharacter.currentHp);
        Assert.Equal(2, events.Count);
        Assert.IsType<DamageResolvedEvent>(events[0]);
        Assert.IsType<HpChangedEvent>(events[1]);
        Assert.Equal(0, gameState.teams[sourceTeamId].leyline);
        Assert.Equal(0, gameState.teams[targetTeamId].leyline);
    }

    [Fact]
    public void ResolveDirectKill_HappyPath_ShouldAppendKillSuffixWithoutDamagePrefix()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5400);
        var targetCharacterInstanceId = new CharacterInstanceId(2010);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8900);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 9000);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 2,
            leyline = 0,
        });
        gameState.matchState = MatchState.running;

        createCardInZone(gameState, sourcePlayerState, sourceRewardCardInstanceId, "reward-card", sourcePlayerState.deckZoneId, ZoneKey.deck);
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);

        var killContext = new KillContext
        {
            killContextId = 6001,
            killerPlayerId = sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceId,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var damageProcessor = new DamageProcessor();
        var events = damageProcessor.resolveDirectKill(gameState, killContext);

        Assert.Equal(targetCharacter.maxHp, targetCharacter.currentHp);
        Assert.True(targetCharacter.isAlive);
        Assert.True(targetCharacter.isInPlay);

        Assert.Equal(5, events.Count);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(events[0]);
        Assert.True(openedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(events[1]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal(openedEvent.responseWindowId, closedEvent.responseWindowId);

        var killRecordedEvent = Assert.IsType<KillRecordedEvent>(events[2]);
        Assert.Equal(6001, killRecordedEvent.killContextId);
        Assert.Equal(sourcePlayerId, killRecordedEvent.killerPlayerId);
        Assert.Equal(targetCharacterInstanceId, killRecordedEvent.killedCharacterInstanceId);
        Assert.Null(killRecordedEvent.sourceDamageContextId);

        var rewardDrawEvent = Assert.IsType<CardMovedEvent>(events[3]);
        Assert.Equal(sourceRewardCardInstanceId, rewardDrawEvent.cardInstanceId);
        Assert.Equal(ZoneKey.deck, rewardDrawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, rewardDrawEvent.toZoneKey);
        Assert.Equal(CardMoveReason.draw, rewardDrawEvent.moveReason);

        var restoreHpChangedEvent = Assert.IsType<HpChangedEvent>(events[4]);
        Assert.Equal(1, restoreHpChangedEvent.hpBefore);
        Assert.Equal(targetCharacter.maxHp, restoreHpChangedEvent.hpAfter);
        Assert.Equal(targetCharacter.maxHp - 1, restoreHpChangedEvent.delta);
        Assert.DoesNotContain(events, static e => e is DamageResolvedEvent);
        Assert.Single(events, static e => e is HpChangedEvent);
        Assert.Equal(10, gameState.teams[sourceTeamId].killScore);
        Assert.Equal(1, gameState.teams[targetTeamId].killScore);
        Assert.Equal(MatchState.running, gameState.matchState);
        Assert.Null(gameState.winnerTeamId);
        Assert.Empty(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
        Assert.Equal(sourceRewardCardInstanceId, gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds[0]);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDirectKill_WhenKillerPlayerIdIsNull_ShouldSkipRewardDrawAndStillApplyKillScore()
    {
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(2011);

        var gameState = new RuleCore.GameState.GameState();
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 9100);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 2);
        var killContext = new KillContext
        {
            killContextId = 6002,
            killerPlayerId = null,
            killedCharacterInstanceId = targetCharacterInstanceId,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var damageProcessor = new DamageProcessor();
        var events = damageProcessor.resolveDirectKill(gameState, killContext);

        Assert.Equal(targetCharacter.maxHp, targetCharacter.currentHp);
        Assert.Equal(4, events.Count);
        Assert.IsType<InteractionWindowEvent>(events[0]);
        Assert.IsType<InteractionWindowEvent>(events[1]);

        var killRecordedEvent = Assert.IsType<KillRecordedEvent>(events[2]);
        Assert.Equal(6002, killRecordedEvent.killContextId);
        Assert.Null(killRecordedEvent.killerPlayerId);
        Assert.Equal(targetCharacterInstanceId, killRecordedEvent.killedCharacterInstanceId);
        Assert.Null(killRecordedEvent.sourceDamageContextId);

        var restoreHpChangedEvent = Assert.IsType<HpChangedEvent>(events[3]);
        Assert.Equal(2, restoreHpChangedEvent.hpBefore);
        Assert.Equal(targetCharacter.maxHp, restoreHpChangedEvent.hpAfter);
        Assert.Equal(targetCharacter.maxHp - 2, restoreHpChangedEvent.delta);
        Assert.DoesNotContain(events, static e => e is DamageResolvedEvent);
        Assert.DoesNotContain(events, static e => e is CardMovedEvent);
        Assert.Equal(9, gameState.teams[targetTeamId].killScore);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDamage_WhenKillReplacementFlagIsSet_ShouldBlockKillCommitAndConsumeFlag()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5500);
        var targetCharacterInstanceId = new CharacterInstanceId(2020);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 9200);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 9300);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.matchState = MatchState.paused;
        gameState.winnerTeamId = sourceTeamId;

        createCardInZone(gameState, sourcePlayerState, sourceRewardCardInstanceId, "reward-card", sourcePlayerState.deckZoneId, ZoneKey.deck);
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);
        targetCharacter.hasPendingOnKilledReplacement = true;

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5010),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var damageEvents = damageProcessor.resolveDamage(gameState, damageContext);
        Assert.Equal(5, damageEvents.Count);
        Assert.IsType<DamageResolvedEvent>(damageEvents[0]);
        Assert.IsType<HpChangedEvent>(damageEvents[1]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[2]);
        Assert.True(openedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[3]);
        Assert.False(closedEvent.isOpened);
        Assert.IsType<HpChangedEvent>(damageEvents[4]);

        Assert.Equal(targetCharacter.maxHp, targetCharacter.currentHp);
        Assert.False(targetCharacter.hasPendingOnKilledReplacement);
        Assert.DoesNotContain(damageEvents, static e => e is KillRecordedEvent);
        Assert.DoesNotContain(damageEvents, static e => e is CardMovedEvent);
        Assert.Equal(10, gameState.teams[sourceTeamId].killScore);
        Assert.Equal(10, gameState.teams[targetTeamId].killScore);
        Assert.Equal(MatchState.paused, gameState.matchState);
        Assert.Equal(sourceTeamId, gameState.winnerTeamId);
        Assert.Single(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Equal(sourceRewardCardInstanceId, gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds[0]);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDirectKill_WhenKillReplacementFlagIsSet_ShouldBlockKillCommitAndConsumeFlag()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5600);
        var targetCharacterInstanceIdA = new CharacterInstanceId(2030);
        var targetCharacterInstanceIdB = new CharacterInstanceId(2031);

        var gameStateA = new RuleCore.GameState.GameState();
        var sourcePlayerStateA = createPlayerState(sourcePlayerId, sourceTeamId, 9400);
        var targetPlayerStateA = createPlayerState(targetPlayerId, targetTeamId, 9500);
        gameStateA.players.Add(sourcePlayerId, sourcePlayerStateA);
        gameStateA.players.Add(targetPlayerId, targetPlayerStateA);
        addStandardPlayerZones(gameStateA, sourcePlayerStateA);
        gameStateA.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameStateA.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        createCardInZone(gameStateA, sourcePlayerStateA, sourceRewardCardInstanceId, "reward-card", sourcePlayerStateA.deckZoneId, ZoneKey.deck);
        var targetCharacterA = createTargetCharacter(gameStateA, targetCharacterInstanceIdA, targetPlayerId, 1);
        targetCharacterA.hasPendingOnKilledReplacement = true;

        var damageProcessor = new DamageProcessor();
        var directKillContextA = new KillContext
        {
            killContextId = 6010,
            killerPlayerId = sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceIdA,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var eventsA = damageProcessor.resolveDirectKill(gameStateA, directKillContextA);

        Assert.Equal(targetCharacterA.maxHp, targetCharacterA.currentHp);
        Assert.False(targetCharacterA.hasPendingOnKilledReplacement);
        Assert.Equal(3, eventsA.Count);
        Assert.IsType<InteractionWindowEvent>(eventsA[0]);
        Assert.IsType<InteractionWindowEvent>(eventsA[1]);
        Assert.IsType<HpChangedEvent>(eventsA[2]);
        Assert.DoesNotContain(eventsA, static e => e is DamageResolvedEvent);
        Assert.DoesNotContain(eventsA, static e => e is KillRecordedEvent);
        Assert.DoesNotContain(eventsA, static e => e is CardMovedEvent);
        Assert.Equal(10, gameStateA.teams[sourceTeamId].killScore);
        Assert.Equal(10, gameStateA.teams[targetTeamId].killScore);
        Assert.Single(gameStateA.zones[sourcePlayerStateA.deckZoneId].cardInstanceIds);
        Assert.Equal(sourceRewardCardInstanceId, gameStateA.zones[sourcePlayerStateA.deckZoneId].cardInstanceIds[0]);
        Assert.Null(gameStateA.currentResponseWindow);

        var gameStateB = new RuleCore.GameState.GameState();
        var sourcePlayerStateB = createPlayerState(sourcePlayerId, sourceTeamId, 9600);
        var targetPlayerStateB = createPlayerState(targetPlayerId, targetTeamId, 9700);
        gameStateB.players.Add(sourcePlayerId, sourcePlayerStateB);
        gameStateB.players.Add(targetPlayerId, targetPlayerStateB);
        gameStateB.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameStateB.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        var targetCharacterB = createTargetCharacter(gameStateB, targetCharacterInstanceIdB, targetPlayerId, 4);
        targetCharacterB.hasPendingOnKilledReplacement = true;

        var directKillContextB = new KillContext
        {
            killContextId = 6011,
            killerPlayerId = sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceIdB,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var eventsB = damageProcessor.resolveDirectKill(gameStateB, directKillContextB);

        Assert.Equal(targetCharacterB.maxHp, targetCharacterB.currentHp);
        Assert.False(targetCharacterB.hasPendingOnKilledReplacement);
        Assert.Equal(2, eventsB.Count);
        Assert.IsType<InteractionWindowEvent>(eventsB[0]);
        Assert.IsType<InteractionWindowEvent>(eventsB[1]);
        Assert.DoesNotContain(eventsB, static e => e is DamageResolvedEvent);
        Assert.DoesNotContain(eventsB, static e => e is HpChangedEvent);
        Assert.DoesNotContain(eventsB, static e => e is KillRecordedEvent);
        Assert.DoesNotContain(eventsB, static e => e is CardMovedEvent);
        Assert.Equal(10, gameStateB.teams[sourceTeamId].killScore);
        Assert.Equal(10, gameStateB.teams[targetTeamId].killScore);
        Assert.Null(gameStateB.currentResponseWindow);
    }

    [Fact]
    public void KillReplacementFlag_ShouldBeConsumed_AndSecondKillShouldCommitNormally()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5700);
        var targetCharacterInstanceId = new CharacterInstanceId(2040);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 9800);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 9900);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });
        createCardInZone(gameState, sourcePlayerState, sourceRewardCardInstanceId, "reward-card", sourcePlayerState.deckZoneId, ZoneKey.deck);
        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);
        targetCharacter.hasPendingOnKilledReplacement = true;

        var damageProcessor = new DamageProcessor();

        var firstKillContext = new KillContext
        {
            killContextId = 6020,
            killerPlayerId = sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceId,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var firstEvents = damageProcessor.resolveDirectKill(gameState, firstKillContext);

        Assert.False(targetCharacter.hasPendingOnKilledReplacement);
        Assert.Equal(3, firstEvents.Count);
        Assert.IsType<InteractionWindowEvent>(firstEvents[0]);
        Assert.IsType<InteractionWindowEvent>(firstEvents[1]);
        Assert.IsType<HpChangedEvent>(firstEvents[2]);
        Assert.DoesNotContain(firstEvents, static e => e is KillRecordedEvent);
        Assert.DoesNotContain(firstEvents, static e => e is CardMovedEvent);
        Assert.Equal(10, gameState.teams[sourceTeamId].killScore);
        Assert.Equal(10, gameState.teams[targetTeamId].killScore);
        Assert.Single(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);

        targetCharacter.currentHp = 1;

        var secondKillContext = new KillContext
        {
            killContextId = 6021,
            killerPlayerId = sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceId,
            causedByDamage = false,
            sourceDamageContextId = null,
        };

        var secondEvents = damageProcessor.resolveDirectKill(gameState, secondKillContext);

        Assert.Equal(5, secondEvents.Count);
        Assert.IsType<InteractionWindowEvent>(secondEvents[0]);
        Assert.IsType<InteractionWindowEvent>(secondEvents[1]);
        Assert.IsType<KillRecordedEvent>(secondEvents[2]);
        Assert.IsType<CardMovedEvent>(secondEvents[3]);
        Assert.IsType<HpChangedEvent>(secondEvents[4]);
        Assert.Equal(10, gameState.teams[sourceTeamId].killScore);
        Assert.Equal(9, gameState.teams[targetTeamId].killScore);
        Assert.Empty(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
        Assert.Null(gameState.currentResponseWindow);
    }

    [Fact]
    public void ResolveDamage_WhenDamageKillsTargetAndKillerDeckHasCard_ShouldAppendKillDrawAndRestoreHpToMax()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceRewardCardInstanceId = new CardInstanceId(5200);
        var targetCharacterInstanceId = new CharacterInstanceId(2002);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8300);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8400);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        createCardInZone(gameState, sourcePlayerState, sourceRewardCardInstanceId, "reward-card", sourcePlayerState.deckZoneId, ZoneKey.deck);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 1,
            leyline = 0,
        });
        gameState.matchState = MatchState.running;

        var targetCharacter = createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5002),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var damageEvents = damageProcessor.resolveDamage(gameState, damageContext);

        Assert.Equal(targetCharacter.maxHp, targetCharacter.currentHp);
        Assert.Equal(2, damageContext.finalDamageValue);
        Assert.True(damageContext.didDealDamage);
        Assert.True(targetCharacter.isAlive);
        Assert.True(targetCharacter.isInPlay);

        Assert.Equal(7, damageEvents.Count);
        Assert.IsType<DamageResolvedEvent>(damageEvents[0]);

        var damageHpChangedEvent = Assert.IsType<HpChangedEvent>(damageEvents[1]);
        Assert.Equal(1, damageHpChangedEvent.hpBefore);
        Assert.Equal(-1, damageHpChangedEvent.hpAfter);
        Assert.Equal(-2, damageHpChangedEvent.delta);

        var openedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[2]);
        Assert.True(openedEvent.isOpened);
        Assert.Equal("responseWindow", openedEvent.windowKindKey);

        var closedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[3]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal("responseWindow", closedEvent.windowKindKey);

        var killRecordedEvent = Assert.IsType<KillRecordedEvent>(damageEvents[4]);
        Assert.Equal(damageContext.damageContextId.Value, killRecordedEvent.killContextId);
        Assert.Equal(sourcePlayerId, killRecordedEvent.killerPlayerId);
        Assert.Equal(targetCharacterInstanceId, killRecordedEvent.killedCharacterInstanceId);
        Assert.Equal(damageContext.damageContextId, killRecordedEvent.sourceDamageContextId);

        var rewardDrawEvent = Assert.IsType<CardMovedEvent>(damageEvents[5]);
        Assert.Equal(sourceRewardCardInstanceId, rewardDrawEvent.cardInstanceId);
        Assert.Equal(ZoneKey.deck, rewardDrawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, rewardDrawEvent.toZoneKey);
        Assert.Equal(CardMoveReason.draw, rewardDrawEvent.moveReason);

        var restoreHpChangedEvent = Assert.IsType<HpChangedEvent>(damageEvents[6]);
        Assert.Equal(-1, restoreHpChangedEvent.hpBefore);
        Assert.Equal(targetCharacter.maxHp, restoreHpChangedEvent.hpAfter);
        Assert.Equal(targetCharacter.maxHp + 1, restoreHpChangedEvent.delta);
        Assert.Equal(10, gameState.teams[sourceTeamId].killScore);
        Assert.Equal(1, gameState.teams[sourceTeamId].leyline);
        Assert.Equal(0, gameState.teams[targetTeamId].killScore);
        Assert.Equal(MatchState.ended, gameState.matchState);
        Assert.Equal(sourceTeamId, gameState.winnerTeamId);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Empty(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
        Assert.Equal(sourceRewardCardInstanceId, gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds[0]);
    }

    [Fact]
    public void ResolveDamage_WhenDamageKillsTargetAndKillerDeckEmptyDiscardHasCard_ShouldRebuildThenDrawOne()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var sourceDiscardCardInstanceId = new CardInstanceId(5300);
        var targetCharacterInstanceId = new CharacterInstanceId(2003);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8500);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8600);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        createCardInZone(gameState, sourcePlayerState, sourceDiscardCardInstanceId, "reward-card", sourcePlayerState.discardZoneId, ZoneKey.discard);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });

        createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5003),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var damageEvents = damageProcessor.resolveDamage(gameState, damageContext);
        Assert.Equal(8, damageEvents.Count);
        Assert.IsType<DamageResolvedEvent>(damageEvents[0]);
        Assert.IsType<HpChangedEvent>(damageEvents[1]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[2]);
        Assert.True(openedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[3]);
        Assert.False(closedEvent.isOpened);
        Assert.IsType<KillRecordedEvent>(damageEvents[4]);

        var rebuildEvent = Assert.IsType<CardMovedEvent>(damageEvents[5]);
        Assert.Equal(sourceDiscardCardInstanceId, rebuildEvent.cardInstanceId);
        Assert.Equal(ZoneKey.discard, rebuildEvent.fromZoneKey);
        Assert.Equal(ZoneKey.deck, rebuildEvent.toZoneKey);
        Assert.Equal(CardMoveReason.returnToSource, rebuildEvent.moveReason);

        var drawEvent = Assert.IsType<CardMovedEvent>(damageEvents[6]);
        Assert.Equal(sourceDiscardCardInstanceId, drawEvent.cardInstanceId);
        Assert.Equal(ZoneKey.deck, drawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, drawEvent.toZoneKey);
        Assert.Equal(CardMoveReason.draw, drawEvent.moveReason);

        Assert.IsType<HpChangedEvent>(damageEvents[7]);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Empty(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[sourcePlayerState.discardZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
        Assert.Equal(sourceDiscardCardInstanceId, gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds[0]);
    }

    [Fact]
    public void ResolveDamage_WhenDamageKillsTargetAndKillerHasNoCards_ShouldNotAppendRewardDrawEvents()
    {
        var sourcePlayerId = new PlayerId(1);
        var sourceTeamId = new TeamId(1);
        var targetPlayerId = new PlayerId(2);
        var targetTeamId = new TeamId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(2004);

        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerState = createPlayerState(sourcePlayerId, sourceTeamId, 8700);
        var targetPlayerState = createPlayerState(targetPlayerId, targetTeamId, 8800);
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        addStandardPlayerZones(gameState, sourcePlayerState);
        gameState.teams.Add(sourceTeamId, new TeamState
        {
            teamId = sourceTeamId,
            killScore = 10,
            leyline = 0,
        });
        gameState.teams.Add(targetTeamId, new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        });

        createTargetCharacter(gameState, targetCharacterInstanceId, targetPlayerId, 1);

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(5004),
            sourcePlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 2,
            damageType = "direct",
        };

        var damageProcessor = new DamageProcessor();
        var damageEvents = damageProcessor.resolveDamage(gameState, damageContext);
        Assert.Equal(6, damageEvents.Count);
        Assert.IsType<DamageResolvedEvent>(damageEvents[0]);
        Assert.IsType<HpChangedEvent>(damageEvents[1]);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[2]);
        Assert.True(openedEvent.isOpened);
        var closedEvent = Assert.IsType<InteractionWindowEvent>(damageEvents[3]);
        Assert.False(closedEvent.isOpened);
        Assert.IsType<KillRecordedEvent>(damageEvents[4]);
        Assert.IsType<HpChangedEvent>(damageEvents[5]);
        Assert.DoesNotContain(damageEvents, static e => e is CardMovedEvent);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Empty(gameState.zones[sourcePlayerState.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[sourcePlayerState.discardZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[sourcePlayerState.handZoneId].cardInstanceIds);
    }

    private static CardInstance createCardInZone(
        RuleCore.GameState.GameState gameState,
        PlayerState ownerPlayerState,
        CardInstanceId cardInstanceId,
        string definitionId,
        ZoneId zoneId,
        ZoneKey zoneKey)
    {
        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerState.playerId,
            zoneId = zoneId,
            zoneKey = zoneKey,
        };

        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
        return cardInstance;
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

    private static CardInstance createCardInPlayerHand(
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
        return cardInstance;
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
