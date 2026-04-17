using System;
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

public class ActionRequestProcessorTurnFlowSkeletonTests
{
    private const string ContinuationKeyEndPhaseHandDiscard = "continuation:endPhaseHandDiscard";
    private const string ContinuationKeyTurnStartShackleDiscard = "continuation:turnStartShackleDiscard";
    private const string EndPhaseDiscardChoiceKeyPrefix = "discardCard:";
    private const string TurnStartShackleDiscardChoiceKeyPrefix = "discardCard:";

    [Fact]
    public void HappyPath_StartToActionToSummonToEndToNextTurn_ShouldAdvanceInOrder()
    {
        var player1Id = new PlayerId(1);
        var player2Id = new PlayerId(2);
        var team1Id = new TeamId(1);
        var team2Id = new TeamId(2);
        var player1State = createPlayerState(player1Id, team1Id, 7100);
        var player2State = createPlayerState(player2Id, team2Id, 7200);
        player1State.mana = 6;
        player1State.lockedSigil = 9;
        player1State.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta
            {
                baseKillScore = 10,
                mode = MatchMode.standard2v2,
            },
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1Id,
                currentTeamId = team1Id,
                currentPhase = TurnPhase.start,
                phaseStepIndex = 3,
            },
        };
        gameState.matchMeta.seatOrder.Add(player1Id);
        gameState.matchMeta.seatOrder.Add(player2Id);
        gameState.matchMeta.teamAssignments[player1Id] = team1Id;
        gameState.matchMeta.teamAssignments[player2Id] = team2Id;
        gameState.players[player1Id] = player1State;
        gameState.players[player2Id] = player2State;

        addFieldZone(gameState, player1State);
        addFieldZone(gameState, player2State);
        addDiscardZone(gameState, player1State);
        addDiscardZone(gameState, player2State);
        addDeckZone(gameState, player1State);
        addDeckZone(gameState, player2State);
        addHandZone(gameState, player1State);
        addHandZone(gameState, player2State);
        addFieldTreasureCard(gameState, player1State, new CardInstanceId(71001), "starter:kourindouCoupon");

        var processor = new ActionRequestProcessor();

        var enterActionEvents = processor.processActionRequest(gameState, new EnterActionPhaseActionRequest
        {
            requestId = 81101,
            actorPlayerId = player1Id,
        });
        Assert.Empty(enterActionEvents);
        Assert.Equal(TurnPhase.action, gameState.turnState.currentPhase);
        Assert.Equal(1, player1State.skillPoint);

        var enterSummonEvents = processor.processActionRequest(gameState, new EnterSummonPhaseActionRequest
        {
            requestId = 81102,
            actorPlayerId = player1Id,
        });
        Assert.Empty(enterSummonEvents);
        Assert.Equal(TurnPhase.summon, gameState.turnState.currentPhase);
        Assert.Equal(0, player1State.sigilPreview);
        Assert.Equal(1, player1State.lockedSigil);
        Assert.True(player1State.isSigilLocked);

        var enterEndEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 81103,
            actorPlayerId = player1Id,
        });
        Assert.Equal(3, enterEndEvents.Count);
        var discardEvent = Assert.IsType<CardMovedEvent>(enterEndEvents[0]);
        Assert.Equal(ZoneKey.field, discardEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, discardEvent.toZoneKey);
        Assert.Equal(CardMoveReason.discard, discardEvent.moveReason);
        var rebuildEvent = Assert.IsType<CardMovedEvent>(enterEndEvents[1]);
        Assert.Equal(ZoneKey.discard, rebuildEvent.fromZoneKey);
        Assert.Equal(ZoneKey.deck, rebuildEvent.toZoneKey);
        Assert.Equal(CardMoveReason.returnToSource, rebuildEvent.moveReason);
        var drawEvent = Assert.IsType<CardMovedEvent>(enterEndEvents[2]);
        Assert.Equal(ZoneKey.deck, drawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, drawEvent.toZoneKey);
        Assert.Equal(CardMoveReason.draw, drawEvent.moveReason);
        Assert.Equal(TurnPhase.end, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.Equal(0, player1State.mana);
        Assert.Equal(0, player1State.sigilPreview);
        Assert.Null(player1State.lockedSigil);
        Assert.False(player1State.isSigilLocked);
        Assert.Empty(gameState.zones[player1State.fieldZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player1State.discardZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player1State.handZoneId].cardInstanceIds);

        var startNextTurnEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81104,
            actorPlayerId = player1Id,
        });
        Assert.Empty(startNextTurnEvents);
        Assert.Equal(2, gameState.turnState.turnNumber);
        Assert.Equal(player2Id, gameState.turnState.currentPlayerId);
        Assert.Equal(team2Id, gameState.turnState.currentTeamId);
        Assert.Equal(TurnPhase.start, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
    }

    [Fact]
    public void EnterAction_WhenPhaseIsNotStart_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7300);
        actorPlayerState.skillPoint = 3;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 4,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 5,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        var cardInstanceId = new CardInstanceId(74001);
        addFieldTreasureCard(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");
        var fieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, actorPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new EnterActionPhaseActionRequest
            {
                requestId = 81201,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("EnterActionPhaseActionRequest requires gameState.turnState.currentPhase to be start.", exception.Message);
        assertFailureStateUnchanged(gameState, actorPlayerState, sentinelChain, snapshot);
    }

    [Fact]
    public void EnterEnd_WhenPhaseIsNeitherActionNorSummon_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7400);
        actorPlayerState.mana = 7;
        actorPlayerState.lockedSigil = 3;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 8,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.start,
                phaseStepIndex = 2,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        var cardInstanceId = new CardInstanceId(74001);
        addFieldTreasureCard(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");
        var fieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, actorPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new EnterEndPhaseActionRequest
            {
                requestId = 81202,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("EnterEndPhaseActionRequest requires gameState.turnState.currentPhase to be action or summon.", exception.Message);
        assertFailureStateUnchanged(gameState, actorPlayerState, sentinelChain, snapshot);
        Assert.Equal(fieldCountBefore, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
    }

    [Fact]
    public void EnterEnd_WhenActorIsNotCurrentPlayer_ShouldThrowAndKeepStateUnchanged()
    {
        var currentPlayerId = new PlayerId(1);
        var actorPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 7410);
        currentPlayerState.mana = 8;
        currentPlayerState.lockedSigil = 2;
        currentPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 8,
                currentPlayerId = currentPlayerId,
                currentTeamId = currentTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 4,
            },
        };
        gameState.players[currentPlayerId] = currentPlayerState;
        addFieldZone(gameState, currentPlayerState);
        addDiscardZone(gameState, currentPlayerState);
        var cardInstanceId = new CardInstanceId(74101);
        addFieldTreasureCard(gameState, currentPlayerState, cardInstanceId, "starter:magicCircuit");
        var fieldCountBefore = gameState.zones[currentPlayerState.fieldZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, currentPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new EnterEndPhaseActionRequest
            {
                requestId = 81221,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("EnterEndPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.", exception.Message);
        assertFailureStateUnchanged(gameState, currentPlayerState, sentinelChain, snapshot);
        Assert.Equal(fieldCountBefore, gameState.zones[currentPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
    }

    [Fact]
    public void EnterEnd_WhenInputContextIsActive_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7420);
        actorPlayerState.mana = 8;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 8,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 4,
            },
            currentInputContext = new InputContextState
            {
                inputContextId = new InputContextId(74299),
                inputTypeKey = "sentinel",
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        var cardInstanceId = new CardInstanceId(74201);
        addFieldTreasureCard(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");
        var fieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, actorPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new EnterEndPhaseActionRequest
            {
                requestId = 81222,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("EnterEndPhaseActionRequest requires gameState.currentInputContext to be null.", exception.Message);
        assertFailureStateUnchanged(gameState, actorPlayerState, sentinelChain, snapshot);
        Assert.Equal(fieldCountBefore, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
    }

    [Fact]
    public void EnterEnd_WhenResponseWindowIsActive_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7430);
        actorPlayerState.mana = 8;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 8,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 4,
            },
            currentResponseWindow = new ResponseWindowState
            {
                responseWindowId = new ResponseWindowId(74399),
                originType = ResponseWindowOriginType.chain,
                windowTypeKey = "sentinel",
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        var cardInstanceId = new CardInstanceId(74301);
        addFieldTreasureCard(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");
        var fieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, actorPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new EnterEndPhaseActionRequest
            {
                requestId = 81223,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("EnterEndPhaseActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        assertFailureStateUnchanged(gameState, actorPlayerState, sentinelChain, snapshot);
        Assert.Equal(fieldCountBefore, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
    }

    [Fact]
    public void StartNextTurn_WhenPhaseIsNotEnd_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7500);
        actorPlayerState.lockedSigil = 4;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            turnState = new TurnState
            {
                turnNumber = 9,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 6,
            },
        };
        gameState.matchMeta.seatOrder.Add(actorPlayerId);
        gameState.matchMeta.seatOrder.Add(nextPlayerId);
        gameState.matchMeta.teamAssignments[actorPlayerId] = actorTeamId;
        gameState.matchMeta.teamAssignments[nextPlayerId] = nextTeamId;
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[nextPlayerId] = createPlayerState(nextPlayerId, nextTeamId, 7600);
        addDeckZone(gameState, actorPlayerState);
        addDeckZone(gameState, gameState.players[nextPlayerId]);
        addHandZone(gameState, actorPlayerState);
        addHandZone(gameState, gameState.players[nextPlayerId]);
        addDiscardZone(gameState, actorPlayerState);
        addDiscardZone(gameState, gameState.players[nextPlayerId]);
        addFieldZone(gameState, actorPlayerState);
        addFieldZone(gameState, gameState.players[nextPlayerId]);
        var currentDeckCountBefore = gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count;
        var currentHandCountBefore = gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count;
        var currentDiscardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var currentFieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;
        var nextPlayerState = gameState.players[nextPlayerId];
        var nextDeckCountBefore = gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count;
        var nextHandCountBefore = gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count;
        var nextDiscardCountBefore = gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count;
        var nextFieldCountBefore = gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count;
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, actorPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new StartNextTurnActionRequest
            {
                requestId = 81203,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("StartNextTurnActionRequest requires gameState.turnState.currentPhase to be end.", exception.Message);
        assertFailureStateUnchanged(gameState, actorPlayerState, sentinelChain, snapshot);
        Assert.Equal(currentDeckCountBefore, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(currentHandCountBefore, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(currentDiscardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(currentFieldCountBefore, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDeckCountBefore, gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(nextHandCountBefore, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDiscardCountBefore, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(nextFieldCountBefore, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void EnterEnd_ShouldClearLockedSigilAndUnlockGate()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7700);
        actorPlayerState.mana = 5;
        actorPlayerState.sigilPreview = 6;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 2,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 3,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 81301,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(TurnPhase.end, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(6, actorPlayerState.sigilPreview);
        Assert.Null(actorPlayerState.lockedSigil);
        Assert.False(actorPlayerState.isSigilLocked);
        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<EnterEndPhaseActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void EnterEnd_FromAction_ShouldClearManaAndLockState()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7750);
        actorPlayerState.mana = 4;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 3,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 7,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 81311,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(TurnPhase.end, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Null(actorPlayerState.lockedSigil);
        Assert.False(actorPlayerState.isSigilLocked);
    }

    [Fact]
    public void EnterEnd_WhenFieldCardIsDefensePlaced_ShouldKeepCardOnField()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7760);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 3,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 2,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);

        var defenseCardInstanceId = new CardInstanceId(77601);
        addFieldTreasureCard(gameState, actorPlayerState, defenseCardInstanceId, "starter:magicCircuit");
        gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField = true;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 81312,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Contains(defenseCardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.DoesNotContain(defenseCardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.fieldZoneId, gameState.cardInstances[defenseCardInstanceId].zoneId);
    }

    [Fact]
    public void EnterEnd_WhenFieldCardIsMechanicalJade_ShouldKeepCardOnField()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7770);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 4,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 3,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);

        var jadeCardInstanceId = new CardInstanceId(77701);
        addFieldTreasureCard(gameState, actorPlayerState, jadeCardInstanceId, "T016");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 81313,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Contains(jadeCardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.DoesNotContain(jadeCardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.fieldZoneId, gameState.cardInstances[jadeCardInstanceId].zoneId);
    }

    [Fact]
    public void StartNextTurn_ShouldRotatePlayerAndTeamAndResetPhaseToStart()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 7800);
        currentPlayerState.lockedSigil = null;
        currentPlayerState.isSigilLocked = false;
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 7900);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            turnState = new TurnState
            {
                turnNumber = 10,
                currentPlayerId = currentPlayerId,
                currentTeamId = currentTeamId,
                currentPhase = TurnPhase.end,
                phaseStepIndex = 11,
            },
        };
        gameState.matchMeta.seatOrder.Add(currentPlayerId);
        gameState.matchMeta.seatOrder.Add(nextPlayerId);
        gameState.matchMeta.teamAssignments[currentPlayerId] = currentTeamId;
        gameState.matchMeta.teamAssignments[nextPlayerId] = nextTeamId;
        gameState.players[currentPlayerId] = currentPlayerState;
        gameState.players[nextPlayerId] = nextPlayerState;
        addDeckZone(gameState, currentPlayerState);
        addDeckZone(gameState, nextPlayerState);
        addHandZone(gameState, currentPlayerState);
        addHandZone(gameState, nextPlayerState);
        addDiscardZone(gameState, currentPlayerState);
        addDiscardZone(gameState, nextPlayerState);
        addFieldZone(gameState, currentPlayerState);
        addFieldZone(gameState, nextPlayerState);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81302,
            actorPlayerId = currentPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(11, gameState.turnState.turnNumber);
        Assert.Equal(nextPlayerId, gameState.turnState.currentPlayerId);
        Assert.Equal(nextTeamId, gameState.turnState.currentTeamId);
        Assert.Equal(TurnPhase.start, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<StartNextTurnActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerActiveCharacterHasSeal_ShouldClearOnlyNextPlayerSeal()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 7950);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 7960);
        var currentCharacterInstanceId = new CharacterInstanceId(79501);
        var nextCharacterInstanceId = new CharacterInstanceId(79601);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 19);

        currentPlayerState.activeCharacterInstanceId = currentCharacterInstanceId;
        nextPlayerState.activeCharacterInstanceId = nextCharacterInstanceId;
        gameState.characterInstances[currentCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = currentCharacterInstanceId,
            definitionId = "current-character",
            ownerPlayerId = currentPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[nextCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nextCharacterInstanceId,
            definitionId = "next-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = currentCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = nextCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });

        var defenseCardInstanceId = new CardInstanceId(79611);
        addCardInZone(gameState, nextPlayerState, defenseCardInstanceId, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField = true;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81303,
            actorPlayerId = currentPlayerId,
        });

        Assert.Equal(2, producedEvents.Count);
        Assert.IsType<CardMovedEvent>(producedEvents[0]);
        var sealClearedEvent = Assert.IsType<StatusChangedEvent>(producedEvents[1]);
        Assert.Equal("Seal", sealClearedEvent.statusKey);
        Assert.Equal(nextCharacterInstanceId, sealClearedEvent.targetCharacterInstanceId);
        Assert.False(sealClearedEvent.isApplied);
        Assert.DoesNotContain(
            StatusRuntime.queryStatusesForCharacter(gameState, nextCharacterInstanceId),
            status => string.Equals(status.statusKey, "Seal", StringComparison.Ordinal));
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, currentCharacterInstanceId),
            status => string.Equals(status.statusKey, "Seal", StringComparison.Ordinal));
        Assert.Equal(20, gameState.turnState!.turnNumber);
        Assert.Equal(nextPlayerId, gameState.turnState.currentPlayerId);
        Assert.Equal(TurnPhase.start, gameState.turnState.currentPhase);
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerActiveCharacterHasShackleAndHandAtLeastFour_ShouldOpenInputAfterDefenseReturn()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 7970);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 7980);
        var nextCharacterInstanceId = new CharacterInstanceId(79801);
        var defenseCardInstanceId = new CardInstanceId(79811);
        var handCard1 = new CardInstanceId(79812);
        var handCard2 = new CardInstanceId(79813);
        var handCard3 = new CardInstanceId(79814);
        var handCard4 = new CardInstanceId(79815);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 19);

        nextPlayerState.activeCharacterInstanceId = nextCharacterInstanceId;
        gameState.characterInstances[nextCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nextCharacterInstanceId,
            definitionId = "next-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        applyCharacterShackleStatus(gameState, nextCharacterInstanceId);
        addCardInZone(gameState, nextPlayerState, defenseCardInstanceId, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField = true;
        addCardInZone(gameState, nextPlayerState, handCard1, nextPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, nextPlayerState, handCard2, nextPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, nextPlayerState, handCard3, nextPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, nextPlayerState, handCard4, nextPlayerState.handZoneId, ZoneKey.hand, "hand-4");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81304,
            actorPlayerId = currentPlayerId,
        });

        Assert.Equal(2, producedEvents.Count);
        var returnEvent = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        Assert.Equal(defenseCardInstanceId, returnEvent.cardInstanceId);
        Assert.Equal(CardMoveReason.returnToSource, returnEvent.moveReason);

        var openInputEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[1]);
        Assert.True(openInputEvent.isOpened);
        Assert.Equal("inputContext", openInputEvent.windowKindKey);
        Assert.NotNull(openInputEvent.inputContextId);

        Assert.Equal(TurnPhase.start, gameState.turnState!.currentPhase);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(createTurnStartShackleDiscardChoiceKey(defenseCardInstanceId), gameState.currentInputContext!.choiceKeys);
        Assert.Equal(ContinuationKeyTurnStartShackleDiscard, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain!.isCompleted);
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, nextCharacterInstanceId),
            status => string.Equals(status.statusKey, "Shackle", StringComparison.Ordinal));
    }

    [Fact]
    public void SubmitInputChoice_WhenTurnStartShackleDiscardSelectsFour_ShouldDiscardFourAndClearShackle()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 7990);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 8000);
        var nextCharacterInstanceId = new CharacterInstanceId(80001);
        var handCard1 = new CardInstanceId(80011);
        var handCard2 = new CardInstanceId(80012);
        var handCard3 = new CardInstanceId(80013);
        var handCard4 = new CardInstanceId(80014);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 20);

        nextPlayerState.activeCharacterInstanceId = nextCharacterInstanceId;
        gameState.characterInstances[nextCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nextCharacterInstanceId,
            definitionId = "next-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        applyCharacterShackleStatus(gameState, nextCharacterInstanceId);
        addCardInZone(gameState, nextPlayerState, handCard1, nextPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, nextPlayerState, handCard2, nextPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, nextPlayerState, handCard3, nextPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, nextPlayerState, handCard4, nextPlayerState.handZoneId, ZoneKey.hand, "hand-4");

        var processor = new ActionRequestProcessor();
        var startNextTurnEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81305,
            actorPlayerId = currentPlayerId,
        });
        var openedEvent = Assert.IsType<InteractionWindowEvent>(Assert.Single(startNextTurnEvents));

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 81306,
            actorPlayerId = nextPlayerId,
            inputContextId = openedEvent.inputContextId!.Value,
            choiceKeys =
            {
                createTurnStartShackleDiscardChoiceKey(handCard1),
                createTurnStartShackleDiscardChoiceKey(handCard2),
                createTurnStartShackleDiscardChoiceKey(handCard3),
                createTurnStartShackleDiscardChoiceKey(handCard4),
            },
        });

        Assert.Equal(7, submitEvents.Count);
        var openInputEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[0]);
        Assert.True(openInputEvent.isOpened);
        var closeInputEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[1]);
        Assert.False(closeInputEvent.isOpened);
        Assert.All(submitEvents[2..6], static e => Assert.IsType<CardMovedEvent>(e));
        var shackleClearedEvent = Assert.IsType<StatusChangedEvent>(submitEvents[6]);
        Assert.Equal("Shackle", shackleClearedEvent.statusKey);
        Assert.Equal(nextCharacterInstanceId, shackleClearedEvent.targetCharacterInstanceId);
        Assert.False(shackleClearedEvent.isApplied);

        Assert.Equal(0, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(4, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(TurnPhase.start, gameState.turnState!.currentPhase);
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.DoesNotContain(
            StatusRuntime.queryStatusesForCharacter(gameState, nextCharacterInstanceId),
            status => string.Equals(status.statusKey, "Shackle", StringComparison.Ordinal));
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerActiveCharacterHasShackleAndHandBelowFour_ShouldForceEndSettlementAndClearShackle()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 8010);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 8020);
        var nextCharacterInstanceId = new CharacterInstanceId(80201);
        var fieldCardInstanceId = new CardInstanceId(80211);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 21);

        nextPlayerState.activeCharacterInstanceId = nextCharacterInstanceId;
        gameState.characterInstances[nextCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nextCharacterInstanceId,
            definitionId = "next-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        applyCharacterShackleStatus(gameState, nextCharacterInstanceId);
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80212), nextPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80213), nextPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80214), nextPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80215), nextPlayerState.deckZoneId, ZoneKey.deck, "deck-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80216), nextPlayerState.deckZoneId, ZoneKey.deck, "deck-2");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(80217), nextPlayerState.deckZoneId, ZoneKey.deck, "deck-3");
        addCardInZone(gameState, nextPlayerState, fieldCardInstanceId, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81307,
            actorPlayerId = currentPlayerId,
        });

        Assert.Equal(5, producedEvents.Count);
        var fieldDiscardEvent = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        Assert.Equal(fieldCardInstanceId, fieldDiscardEvent.cardInstanceId);
        Assert.Equal(CardMoveReason.discard, fieldDiscardEvent.moveReason);
        Assert.All(producedEvents[1..4], static e => Assert.IsType<CardMovedEvent>(e));
        var shackleClearedEvent = Assert.IsType<StatusChangedEvent>(producedEvents[4]);
        Assert.Equal("Shackle", shackleClearedEvent.statusKey);
        Assert.Equal(nextCharacterInstanceId, shackleClearedEvent.targetCharacterInstanceId);
        Assert.False(shackleClearedEvent.isApplied);

        Assert.Equal(TurnPhase.end, gameState.turnState!.currentPhase);
        Assert.Equal(6, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.DoesNotContain(
            StatusRuntime.queryStatusesForCharacter(gameState, nextCharacterInstanceId),
            status => string.Equals(status.statusKey, "Shackle", StringComparison.Ordinal));
    }

    [Fact]
    public void SubmitInputChoice_WhenTurnStartShackleChoiceKeysInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 8030);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 8040);
        var nextCharacterInstanceId = new CharacterInstanceId(80401);
        var handCard1 = new CardInstanceId(80411);
        var handCard2 = new CardInstanceId(80412);
        var handCard3 = new CardInstanceId(80413);
        var handCard4 = new CardInstanceId(80414);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 22);

        nextPlayerState.activeCharacterInstanceId = nextCharacterInstanceId;
        gameState.characterInstances[nextCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nextCharacterInstanceId,
            definitionId = "next-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        applyCharacterShackleStatus(gameState, nextCharacterInstanceId);
        addCardInZone(gameState, nextPlayerState, handCard1, nextPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, nextPlayerState, handCard2, nextPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, nextPlayerState, handCard3, nextPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, nextPlayerState, handCard4, nextPlayerState.handZoneId, ZoneKey.hand, "hand-4");

        var processor = new ActionRequestProcessor();
        var startNextTurnEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81308,
            actorPlayerId = currentPlayerId,
        });
        var openedEvent = Assert.IsType<InteractionWindowEvent>(Assert.Single(startNextTurnEvents));

        var handCountBefore = gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count;
        var phaseBefore = gameState.turnState!.currentPhase;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var currentInputContextBefore = gameState.currentInputContext;
        var producedEventsCountBefore = gameState.currentActionChain.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new SubmitInputChoiceActionRequest
            {
                requestId = 81309,
                actorPlayerId = nextPlayerId,
                inputContextId = openedEvent.inputContextId!.Value,
                choiceKeys =
                {
                    createTurnStartShackleDiscardChoiceKey(handCard1),
                    createTurnStartShackleDiscardChoiceKey(handCard1),
                    createTurnStartShackleDiscardChoiceKey(handCard2),
                    createTurnStartShackleDiscardChoiceKey(handCard3),
                },
            }));

        Assert.Equal("SubmitInputChoiceActionRequest requires choiceKeys to contain exactly four unique values from currentInputContext.choiceKeys for continuation:turnStartShackleDiscard.", exception.Message);
        Assert.Equal(handCountBefore, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(phaseBefore, gameState.turnState.currentPhase);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Same(currentInputContextBefore, gameState.currentInputContext);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain.producedEvents.Count);
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, nextCharacterInstanceId),
            status => string.Equals(status.statusKey, "Shackle", StringComparison.Ordinal));
    }

    [Fact]
    public void StartNextTurn_WhenOnlyNonActiveCharacterHasShackle_ShouldNotTriggerShackleSettlement()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 8050);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 8060);
        var activeCharacterInstanceId = new CharacterInstanceId(80601);
        var nonActiveCharacterInstanceId = new CharacterInstanceId(80602);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 23);

        nextPlayerState.activeCharacterInstanceId = activeCharacterInstanceId;
        gameState.characterInstances[activeCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = activeCharacterInstanceId,
            definitionId = "next-active-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[nonActiveCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = nonActiveCharacterInstanceId,
            definitionId = "next-non-active-character",
            ownerPlayerId = nextPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        applyCharacterShackleStatus(gameState, nonActiveCharacterInstanceId);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 81310,
            actorPlayerId = currentPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(TurnPhase.start, gameState.turnState!.currentPhase);
        Assert.Null(gameState.currentInputContext);
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, nonActiveCharacterInstanceId),
            status => string.Equals(status.statusKey, "Shackle", StringComparison.Ordinal));
    }

    [Fact]
    public void EnterEnd_WhenActorHandBelowSix_ShouldDrawUpToSix()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8000);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 20);

        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81001), actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81002), actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81003), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81004), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81005), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-3");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(81006), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-4");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82001,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, producedEvents.Count);
        Assert.All(producedEvents, static e => Assert.IsType<CardMovedEvent>(e));
        Assert.All(producedEvents, e =>
        {
            var movedEvent = Assert.IsType<CardMovedEvent>(e);
            Assert.Equal(CardMoveReason.draw, movedEvent.moveReason);
            Assert.Equal(ZoneKey.deck, movedEvent.fromZoneKey);
            Assert.Equal(ZoneKey.hand, movedEvent.toZoneKey);
        });

        Assert.Equal(TurnPhase.end, gameState.turnState!.currentPhase);
        Assert.Equal(6, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
    }

    [Fact]
    public void EnterEnd_WhenDeckInsufficientAndDiscardHasCards_ShouldRebuildThenDrawToSix()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8200);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 21);

        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83001), actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83002), actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83003), actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83004), actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83005), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83006), actorPlayerState.discardZoneId, ZoneKey.discard, "discard-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83007), actorPlayerState.discardZoneId, ZoneKey.discard, "discard-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(83008), actorPlayerState.discardZoneId, ZoneKey.discard, "discard-3");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82002,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(5, producedEvents.Count);
        Assert.Equal(CardMoveReason.draw, Assert.IsType<CardMovedEvent>(producedEvents[0]).moveReason);
        Assert.Equal(CardMoveReason.returnToSource, Assert.IsType<CardMovedEvent>(producedEvents[1]).moveReason);
        Assert.Equal(CardMoveReason.returnToSource, Assert.IsType<CardMovedEvent>(producedEvents[2]).moveReason);
        Assert.Equal(CardMoveReason.returnToSource, Assert.IsType<CardMovedEvent>(producedEvents[3]).moveReason);
        Assert.Equal(CardMoveReason.draw, Assert.IsType<CardMovedEvent>(producedEvents[4]).moveReason);

        Assert.Equal(6, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(2, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
    }

    [Fact]
    public void EnterEnd_WhenDeckAndDiscardAreEmpty_ShouldStopAtCurrentHandCount()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8400);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 22);

        addCardInZone(gameState, actorPlayerState, new CardInstanceId(85001), actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(85002), actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(85003), actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(85004), actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(85005), actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82003,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(5, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
    }

    [Fact]
    public void EnterEnd_WhenActorHandAlreadyAtLeastSix_ShouldNotDrawExtraCards()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8600);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 23);

        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87001), actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87002), actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87003), actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87004), actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87005), actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87006), actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87007), actorPlayerState.deckZoneId, ZoneKey.deck, "deck-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(87008), actorPlayerState.discardZoneId, ZoneKey.discard, "discard-1");

        var deckCountBefore = gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82004,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(producedEvents);
        Assert.Equal(6, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(deckCountBefore, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void EnterEnd_WhenActorHandAboveSix_ShouldOpenInputAndSuspendFinalization()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8750);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 24);

        var handCard1 = new CardInstanceId(87501);
        var handCard2 = new CardInstanceId(87502);
        var handCard3 = new CardInstanceId(87503);
        var handCard4 = new CardInstanceId(87504);
        var handCard5 = new CardInstanceId(87505);
        var handCard6 = new CardInstanceId(87506);
        var handCard7 = new CardInstanceId(87507);
        addCardInZone(gameState, actorPlayerState, handCard1, actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, handCard2, actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, handCard3, actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, handCard4, actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, handCard5, actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, handCard6, actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, handCard7, actorPlayerState.handZoneId, ZoneKey.hand, "hand-7");

        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82012,
            actorPlayerId = actorPlayerId,
        });

        var openedEvent = Assert.IsType<InteractionWindowEvent>(Assert.Single(producedEvents));
        Assert.True(openedEvent.isOpened);
        Assert.Equal("inputContext", openedEvent.windowKindKey);
        Assert.NotNull(openedEvent.inputContextId);

        Assert.Equal(TurnPhase.action, gameState.turnState!.currentPhase);
        Assert.Equal(7, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(7, gameState.currentInputContext!.choiceKeys.Count);
        Assert.Equal(ContinuationKeyEndPhaseHandDiscard, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain!.isCompleted);
        Assert.Contains(createEndPhaseDiscardChoiceKey(handCard7), gameState.currentInputContext.choiceKeys);
    }

    [Fact]
    public void SubmitInputChoice_WhenHandStillAboveSix_ShouldCloseAndReopenWithFreshChoiceKeys()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8755);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 24);

        var handCard1 = new CardInstanceId(87551);
        var handCard2 = new CardInstanceId(87552);
        var handCard3 = new CardInstanceId(87553);
        var handCard4 = new CardInstanceId(87554);
        var handCard5 = new CardInstanceId(87555);
        var handCard6 = new CardInstanceId(87556);
        var handCard7 = new CardInstanceId(87557);
        var handCard8 = new CardInstanceId(87558);
        addCardInZone(gameState, actorPlayerState, handCard1, actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, handCard2, actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, handCard3, actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, handCard4, actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, handCard5, actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, handCard6, actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, handCard7, actorPlayerState.handZoneId, ZoneKey.hand, "hand-7");
        addCardInZone(gameState, actorPlayerState, handCard8, actorPlayerState.handZoneId, ZoneKey.hand, "hand-8");

        var processor = new ActionRequestProcessor();
        var enterEndEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82014,
            actorPlayerId = actorPlayerId,
        });
        var firstOpenEvent = Assert.IsType<InteractionWindowEvent>(Assert.Single(enterEndEvents));
        var firstInputContextId = firstOpenEvent.inputContextId!.Value;

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 82015,
            actorPlayerId = actorPlayerId,
            inputContextId = firstInputContextId,
            choiceKey = createEndPhaseDiscardChoiceKey(handCard8),
        });

        Assert.Equal(4, submitEvents.Count);
        Assert.IsType<InteractionWindowEvent>(submitEvents[0]);
        var closeEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[1]);
        Assert.False(closeEvent.isOpened);
        var discardEvent = Assert.IsType<CardMovedEvent>(submitEvents[2]);
        Assert.Equal(handCard8, discardEvent.cardInstanceId);
        Assert.Equal(ZoneKey.hand, discardEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, discardEvent.toZoneKey);
        Assert.Equal(CardMoveReason.discard, discardEvent.moveReason);
        var reopenEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[3]);
        Assert.True(reopenEvent.isOpened);
        Assert.NotEqual(firstInputContextId, reopenEvent.inputContextId!.Value);

        Assert.Equal(TurnPhase.action, gameState.turnState!.currentPhase);
        Assert.Equal(7, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.NotNull(gameState.currentInputContext);
        Assert.DoesNotContain(createEndPhaseDiscardChoiceKey(handCard8), gameState.currentInputContext!.choiceKeys);
        Assert.Contains(createEndPhaseDiscardChoiceKey(handCard1), gameState.currentInputContext.choiceKeys);
        Assert.Equal(ContinuationKeyEndPhaseHandDiscard, gameState.currentActionChain!.pendingContinuationKey);
        Assert.False(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void SubmitInputChoice_WhenHandReachesSix_ShouldFinalizeEndCleanup()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8760);
        actorPlayerState.mana = 3;
        actorPlayerState.lockedSigil = 2;
        actorPlayerState.isSigilLocked = true;

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 25);

        var fieldCard = new CardInstanceId(87601);
        addFieldTreasureCard(gameState, actorPlayerState, fieldCard, "starter:magicCircuit");
        var handCard1 = new CardInstanceId(87602);
        var handCard2 = new CardInstanceId(87603);
        var handCard3 = new CardInstanceId(87604);
        var handCard4 = new CardInstanceId(87605);
        var handCard5 = new CardInstanceId(87606);
        var handCard6 = new CardInstanceId(87607);
        var handCard7 = new CardInstanceId(87608);
        addCardInZone(gameState, actorPlayerState, handCard1, actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, handCard2, actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, handCard3, actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, handCard4, actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, handCard5, actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, handCard6, actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, handCard7, actorPlayerState.handZoneId, ZoneKey.hand, "hand-7");

        var processor = new ActionRequestProcessor();
        var enterEndEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82013,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(2, enterEndEvents.Count);
        var fieldDiscardEvent = Assert.IsType<CardMovedEvent>(enterEndEvents[0]);
        Assert.Equal(fieldCard, fieldDiscardEvent.cardInstanceId);
        Assert.Equal(ZoneKey.field, fieldDiscardEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, fieldDiscardEvent.toZoneKey);
        Assert.Equal(CardMoveReason.discard, fieldDiscardEvent.moveReason);
        var openEvent = Assert.IsType<InteractionWindowEvent>(enterEndEvents[1]);
        Assert.True(openEvent.isOpened);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 82016,
            actorPlayerId = actorPlayerId,
            inputContextId = openEvent.inputContextId!.Value,
            choiceKey = createEndPhaseDiscardChoiceKey(handCard7),
        });

        Assert.Equal(4, submitEvents.Count);
        Assert.IsType<CardMovedEvent>(submitEvents[0]);
        Assert.IsType<InteractionWindowEvent>(submitEvents[1]);
        var inputClosedEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[2]);
        Assert.False(inputClosedEvent.isOpened);
        var handDiscardEvent = Assert.IsType<CardMovedEvent>(submitEvents[3]);
        Assert.Equal(handCard7, handDiscardEvent.cardInstanceId);
        Assert.Equal(ZoneKey.hand, handDiscardEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, handDiscardEvent.toZoneKey);
        Assert.Equal(CardMoveReason.discard, handDiscardEvent.moveReason);

        Assert.Equal(6, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Contains(fieldCard, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(handCard7, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(TurnPhase.end, gameState.turnState!.currentPhase);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Null(actorPlayerState.lockedSigil);
        Assert.False(actorPlayerState.isSigilLocked);
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenChoiceKeyNotInCurrentRound_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8765);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 26);

        var handCard1 = new CardInstanceId(87651);
        var handCard2 = new CardInstanceId(87652);
        var handCard3 = new CardInstanceId(87653);
        var handCard4 = new CardInstanceId(87654);
        var handCard5 = new CardInstanceId(87655);
        var handCard6 = new CardInstanceId(87656);
        var handCard7 = new CardInstanceId(87657);
        var handCard8 = new CardInstanceId(87658);
        addCardInZone(gameState, actorPlayerState, handCard1, actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, handCard2, actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, handCard3, actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, handCard4, actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, handCard5, actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, handCard6, actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, handCard7, actorPlayerState.handZoneId, ZoneKey.hand, "hand-7");
        addCardInZone(gameState, actorPlayerState, handCard8, actorPlayerState.handZoneId, ZoneKey.hand, "hand-8");

        var processor = new ActionRequestProcessor();
        var enterEndEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82017,
            actorPlayerId = actorPlayerId,
        });
        var firstOpenEvent = Assert.IsType<InteractionWindowEvent>(Assert.Single(enterEndEvents));

        var firstSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 82018,
            actorPlayerId = actorPlayerId,
            inputContextId = firstOpenEvent.inputContextId!.Value,
            choiceKey = createEndPhaseDiscardChoiceKey(handCard8),
        });
        var secondOpenEvent = Assert.IsType<InteractionWindowEvent>(firstSubmitEvents[3]);

        var handCountBefore = gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count;
        var discardCountBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;
        var phaseBefore = gameState.turnState!.currentPhase;
        var pendingContinuationBefore = gameState.currentActionChain!.pendingContinuationKey;
        var currentInputContextBefore = gameState.currentInputContext;
        var producedEventsCountBefore = gameState.currentActionChain!.producedEvents.Count;

        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new SubmitInputChoiceActionRequest
            {
                requestId = 82019,
                actorPlayerId = actorPlayerId,
                inputContextId = secondOpenEvent.inputContextId!.Value,
                choiceKey = createEndPhaseDiscardChoiceKey(handCard8),
            }));

        Assert.Equal("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.", exception.Message);
        Assert.Equal(handCountBefore, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(phaseBefore, gameState.turnState.currentPhase);
        Assert.Equal(pendingContinuationBefore, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Same(currentInputContextBefore, gameState.currentInputContext);
        Assert.Equal(producedEventsCountBefore, gameState.currentActionChain!.producedEvents.Count);
    }

    [Fact]
    public void EnterEnd_WhenShortStatusesExist_ShouldClearAllPlayersShortStatusesAndEmitStatusChangedEvents()
    {
        var actorPlayerId = new PlayerId(1);
        var otherPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var otherTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8800);
        var otherPlayerState = createPlayerState(otherPlayerId, otherTeamId, 8900);
        var actorCharacterInstanceId = new CharacterInstanceId(88001);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 27);
        gameState.players[otherPlayerId] = otherPlayerState;
        addDeckZone(gameState, otherPlayerState);
        addHandZone(gameState, otherPlayerState);
        addDiscardZone(gameState, otherPlayerState);
        addFieldZone(gameState, otherPlayerState);

        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88011), actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88012), actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88013), actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88014), actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88015), actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, new CardInstanceId(88016), actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Silence",
                targetPlayerId = actorPlayerId,
                durationTypeKey = "untilEndOfTurn",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Charm",
                targetPlayerId = otherPlayerId,
                durationTypeKey = "untilEndOfTurn",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Penetrate",
                targetPlayerId = actorPlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Barrier",
                targetCharacterInstanceId = actorCharacterInstanceId,
                durationTypeKey = "untilConsumed",
            });

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82020,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(3, producedEvents.Count);
        var firstChangedEvent = Assert.IsType<StatusChangedEvent>(producedEvents[0]);
        var secondChangedEvent = Assert.IsType<StatusChangedEvent>(producedEvents[1]);
        var thirdChangedEvent = Assert.IsType<StatusChangedEvent>(producedEvents[2]);
        Assert.Equal("statusChanged", firstChangedEvent.eventTypeKey);
        Assert.False(firstChangedEvent.isApplied);
        Assert.Equal("Silence", firstChangedEvent.statusKey);
        Assert.Equal(actorPlayerId, firstChangedEvent.targetPlayerId);
        Assert.Equal("Charm", secondChangedEvent.statusKey);
        Assert.Equal(otherPlayerId, secondChangedEvent.targetPlayerId);
        Assert.Equal("Penetrate", thirdChangedEvent.statusKey);
        Assert.Equal(actorPlayerId, thirdChangedEvent.targetPlayerId);
        Assert.Equal(TurnPhase.end, gameState.turnState!.currentPhase);
        Assert.DoesNotContain(gameState.statusInstances, status => string.Equals(status.statusKey, "Silence", StringComparison.Ordinal));
        Assert.DoesNotContain(gameState.statusInstances, status => string.Equals(status.statusKey, "Charm", StringComparison.Ordinal));
        Assert.DoesNotContain(gameState.statusInstances, status => string.Equals(status.statusKey, "Penetrate", StringComparison.Ordinal));
        Assert.Contains(gameState.statusInstances, status => string.Equals(status.statusKey, "Barrier", StringComparison.Ordinal));
    }

    [Fact]
    public void EnterEnd_WhenHandAboveSixWithShortStatus_ShouldClearShortStatusOnlyAfterFinalize()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8810);

        var gameState = createEnterEndReadyGameState(actorPlayerId, actorTeamId, actorPlayerState, turnNumber: 28);

        var handCard1 = new CardInstanceId(88101);
        var handCard2 = new CardInstanceId(88102);
        var handCard3 = new CardInstanceId(88103);
        var handCard4 = new CardInstanceId(88104);
        var handCard5 = new CardInstanceId(88105);
        var handCard6 = new CardInstanceId(88106);
        var handCard7 = new CardInstanceId(88107);
        addCardInZone(gameState, actorPlayerState, handCard1, actorPlayerState.handZoneId, ZoneKey.hand, "hand-1");
        addCardInZone(gameState, actorPlayerState, handCard2, actorPlayerState.handZoneId, ZoneKey.hand, "hand-2");
        addCardInZone(gameState, actorPlayerState, handCard3, actorPlayerState.handZoneId, ZoneKey.hand, "hand-3");
        addCardInZone(gameState, actorPlayerState, handCard4, actorPlayerState.handZoneId, ZoneKey.hand, "hand-4");
        addCardInZone(gameState, actorPlayerState, handCard5, actorPlayerState.handZoneId, ZoneKey.hand, "hand-5");
        addCardInZone(gameState, actorPlayerState, handCard6, actorPlayerState.handZoneId, ZoneKey.hand, "hand-6");
        addCardInZone(gameState, actorPlayerState, handCard7, actorPlayerState.handZoneId, ZoneKey.hand, "hand-7");
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Penetrate",
                targetPlayerId = actorPlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });

        var processor = new ActionRequestProcessor();
        var enterEndEvents = processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 82021,
            actorPlayerId = actorPlayerId,
        });

        Assert.Single(enterEndEvents);
        Assert.IsType<InteractionWindowEvent>(enterEndEvents[0]);
        Assert.Contains(gameState.statusInstances, status => string.Equals(status.statusKey, "Penetrate", StringComparison.Ordinal));

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 82022,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = createEndPhaseDiscardChoiceKey(handCard7),
        });

        Assert.Equal(4, submitEvents.Count);
        Assert.IsType<InteractionWindowEvent>(submitEvents[0]);
        var closeEvent = Assert.IsType<InteractionWindowEvent>(submitEvents[1]);
        Assert.False(closeEvent.isOpened);
        Assert.IsType<CardMovedEvent>(submitEvents[2]);
        var statusChangedEvent = Assert.IsType<StatusChangedEvent>(submitEvents[3]);
        Assert.Equal("Penetrate", statusChangedEvent.statusKey);
        Assert.False(statusChangedEvent.isApplied);
        Assert.Equal(actorPlayerId, statusChangedEvent.targetPlayerId);
        Assert.Equal(TurnPhase.end, gameState.turnState!.currentPhase);
        Assert.DoesNotContain(gameState.statusInstances, status => string.Equals(status.statusKey, "Penetrate", StringComparison.Ordinal));
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerHasDefensePlacedCards_ShouldReturnToHandAndResetFlag()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 9500);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 9600);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 27);

        var defenseCardInstanceId = new CardInstanceId(96001);
        addCardInZone(gameState, nextPlayerState, defenseCardInstanceId, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField = true;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 82008,
            actorPlayerId = currentPlayerId,
        });

        var returnEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.returnToSource, returnEvent.moveReason);
        Assert.Equal(ZoneKey.field, returnEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, returnEvent.toZoneKey);
        Assert.Contains(defenseCardInstanceId, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(defenseCardInstanceId, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
        Assert.False(gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField);
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerHasMultipleDefensePlacedCards_ShouldReturnInFieldOrder()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 9700);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 9800);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 28);

        var defenseCard1 = new CardInstanceId(98001);
        var normalCard = new CardInstanceId(98002);
        var defenseCard2 = new CardInstanceId(98003);
        addCardInZone(gameState, nextPlayerState, defenseCard1, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        addCardInZone(gameState, nextPlayerState, normalCard, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:kourindouCoupon");
        addCardInZone(gameState, nextPlayerState, defenseCard2, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        gameState.cardInstances[defenseCard1].isDefensePlacedOnField = true;
        gameState.cardInstances[defenseCard2].isDefensePlacedOnField = true;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 82009,
            actorPlayerId = currentPlayerId,
        });

        Assert.Equal(2, producedEvents.Count);
        var firstReturn = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        var secondReturn = Assert.IsType<CardMovedEvent>(producedEvents[1]);
        Assert.Equal(defenseCard1, firstReturn.cardInstanceId);
        Assert.Equal(defenseCard2, secondReturn.cardInstanceId);
        Assert.Contains(normalCard, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
        Assert.DoesNotContain(defenseCard1, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
        Assert.DoesNotContain(defenseCard2, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
    }

    [Fact]
    public void StartNextTurn_WhenNextPlayerHasDefenseAndNonDefenseFieldCards_ShouldOnlyReturnDefense()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 9900);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 10000);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 29);

        var defenseCard = new CardInstanceId(100001);
        var normalCard = new CardInstanceId(100002);
        addCardInZone(gameState, nextPlayerState, defenseCard, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        addCardInZone(gameState, nextPlayerState, normalCard, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:kourindouCoupon");
        gameState.cardInstances[defenseCard].isDefensePlacedOnField = true;

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 82010,
            actorPlayerId = currentPlayerId,
        });

        var returnEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(defenseCard, returnEvent.cardInstanceId);
        Assert.DoesNotContain(defenseCard, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Contains(defenseCard, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(normalCard, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds);
        Assert.False(gameState.cardInstances[defenseCard].isDefensePlacedOnField);
        Assert.False(gameState.cardInstances[normalCard].isDefensePlacedOnField);
    }

    [Fact]
    public void StartNextTurn_WhenDefenseReturnHappens_ShouldNotDrawAtTurnStart()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 10100);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 10200);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 30);

        addCardInZone(gameState, nextPlayerState, new CardInstanceId(102001), nextPlayerState.handZoneId, ZoneKey.hand, "next-hand-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(102002), nextPlayerState.handZoneId, ZoneKey.hand, "next-hand-2");
        var defenseCard = new CardInstanceId(102003);
        addCardInZone(gameState, nextPlayerState, defenseCard, nextPlayerState.fieldZoneId, ZoneKey.field, "starter:magicCircuit");
        gameState.cardInstances[defenseCard].isDefensePlacedOnField = true;
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(102004), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(102005), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-2");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(102006), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-3");

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new StartNextTurnActionRequest
        {
            requestId = 82011,
            actorPlayerId = currentPlayerId,
        });

        var returnEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.returnToSource, returnEvent.moveReason);
        Assert.Equal(ZoneKey.field, returnEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, returnEvent.toZoneKey);
        Assert.Equal(3, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(3, gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void StartNextTurn_WhenActorIsNotCurrentPlayer_ShouldThrowAndKeepHandDeckDiscardUnchanged()
    {
        var currentPlayerId = new PlayerId(1);
        var actorPlayerId = new PlayerId(2);
        var nextPlayerId = new PlayerId(3);
        var currentTeamId = new TeamId(1);
        var actorTeamId = new TeamId(2);
        var nextTeamId = new TeamId(1);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 8800);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 8900);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 9000);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 24);
        gameState.matchMeta.seatOrder.Clear();
        gameState.matchMeta.seatOrder.Add(currentPlayerId);
        gameState.matchMeta.seatOrder.Add(nextPlayerId);
        gameState.players[actorPlayerId] = actorPlayerState;
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addFieldZone(gameState, actorPlayerState);

        addCardInZone(gameState, nextPlayerState, new CardInstanceId(90001), nextPlayerState.handZoneId, ZoneKey.hand, "next-hand-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(90002), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(90003), nextPlayerState.discardZoneId, ZoneKey.discard, "next-discard-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(90004), nextPlayerState.fieldZoneId, ZoneKey.field, "next-field-1");
        var nextHandBefore = gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count;
        var nextDeckBefore = gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count;
        var nextDiscardBefore = gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count;
        var nextFieldBefore = gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, currentPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new StartNextTurnActionRequest
            {
                requestId = 82005,
                actorPlayerId = actorPlayerId,
            }));

        Assert.Equal("StartNextTurnActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.", exception.Message);
        assertFailureStateUnchanged(gameState, currentPlayerState, sentinelChain, snapshot);
        Assert.Equal(nextHandBefore, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDeckBefore, gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDiscardBefore, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(nextFieldBefore, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void StartNextTurn_WhenInputContextIsActive_ShouldThrowAndKeepHandDeckDiscardUnchanged()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 9100);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 9200);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 25);
        gameState.currentInputContext = new InputContextState
        {
            inputContextId = new InputContextId(92099),
            inputTypeKey = "sentinel",
        };

        addCardInZone(gameState, nextPlayerState, new CardInstanceId(92001), nextPlayerState.handZoneId, ZoneKey.hand, "next-hand-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(92002), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(92003), nextPlayerState.discardZoneId, ZoneKey.discard, "next-discard-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(92004), nextPlayerState.fieldZoneId, ZoneKey.field, "next-field-1");
        var nextHandBefore = gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count;
        var nextDeckBefore = gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count;
        var nextDiscardBefore = gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count;
        var nextFieldBefore = gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, currentPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new StartNextTurnActionRequest
            {
                requestId = 82006,
                actorPlayerId = currentPlayerId,
            }));

        Assert.Equal("StartNextTurnActionRequest requires gameState.currentInputContext to be null.", exception.Message);
        assertFailureStateUnchanged(gameState, currentPlayerState, sentinelChain, snapshot);
        Assert.Equal(nextHandBefore, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDeckBefore, gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDiscardBefore, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(nextFieldBefore, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void StartNextTurn_WhenResponseWindowIsActive_ShouldThrowAndKeepHandDeckDiscardUnchanged()
    {
        var currentPlayerId = new PlayerId(1);
        var nextPlayerId = new PlayerId(2);
        var currentTeamId = new TeamId(1);
        var nextTeamId = new TeamId(2);
        var currentPlayerState = createPlayerState(currentPlayerId, currentTeamId, 9300);
        var nextPlayerState = createPlayerState(nextPlayerId, nextTeamId, 9400);

        var gameState = createStartNextTurnReadyGameState(
            currentPlayerId,
            nextPlayerId,
            currentTeamId,
            nextTeamId,
            currentPlayerState,
            nextPlayerState,
            turnNumber: 26);
        gameState.currentResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(94099),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "sentinel",
        };

        addCardInZone(gameState, nextPlayerState, new CardInstanceId(94001), nextPlayerState.handZoneId, ZoneKey.hand, "next-hand-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(94002), nextPlayerState.deckZoneId, ZoneKey.deck, "next-deck-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(94003), nextPlayerState.discardZoneId, ZoneKey.discard, "next-discard-1");
        addCardInZone(gameState, nextPlayerState, new CardInstanceId(94004), nextPlayerState.fieldZoneId, ZoneKey.field, "next-field-1");
        var nextHandBefore = gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count;
        var nextDeckBefore = gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count;
        var nextDiscardBefore = gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count;
        var nextFieldBefore = gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count;

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var snapshot = captureTurnFailureSnapshot(gameState, currentPlayerState, sentinelChain);

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new StartNextTurnActionRequest
            {
                requestId = 82007,
                actorPlayerId = currentPlayerId,
            }));

        Assert.Equal("StartNextTurnActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        assertFailureStateUnchanged(gameState, currentPlayerState, sentinelChain, snapshot);
        Assert.Equal(nextHandBefore, gameState.zones[nextPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDeckBefore, gameState.zones[nextPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.Equal(nextDiscardBefore, gameState.zones[nextPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(nextFieldBefore, gameState.zones[nextPlayerState.fieldZoneId].cardInstanceIds.Count);
    }

    private static (TurnPhase currentPhase, int phaseStepIndex, PlayerId currentPlayerId, TeamId currentTeamId, int turnNumber, int mana, int skillPoint, int? lockedSigil, bool isSigilLocked, int producedEventCount)
        captureTurnFailureSnapshot(
            RuleCore.GameState.GameState gameState,
            PlayerState actorPlayerState,
            ActionChainState sentinelChain)
    {
        return (
            gameState.turnState!.currentPhase,
            gameState.turnState.phaseStepIndex,
            gameState.turnState.currentPlayerId,
            gameState.turnState.currentTeamId,
            gameState.turnState.turnNumber,
            actorPlayerState.mana,
            actorPlayerState.skillPoint,
            actorPlayerState.lockedSigil,
            actorPlayerState.isSigilLocked,
            sentinelChain.producedEvents.Count);
    }

    private static void assertFailureStateUnchanged(
        RuleCore.GameState.GameState gameState,
        PlayerState actorPlayerState,
        ActionChainState sentinelChain,
        (TurnPhase currentPhase, int phaseStepIndex, PlayerId currentPlayerId, TeamId currentTeamId, int turnNumber, int mana, int skillPoint, int? lockedSigil, bool isSigilLocked, int producedEventCount) snapshot)
    {
        Assert.Equal(snapshot.currentPhase, gameState.turnState!.currentPhase);
        Assert.Equal(snapshot.phaseStepIndex, gameState.turnState.phaseStepIndex);
        Assert.Equal(snapshot.currentPlayerId, gameState.turnState.currentPlayerId);
        Assert.Equal(snapshot.currentTeamId, gameState.turnState.currentTeamId);
        Assert.Equal(snapshot.turnNumber, gameState.turnState.turnNumber);
        Assert.Equal(snapshot.mana, actorPlayerState.mana);
        Assert.Equal(snapshot.skillPoint, actorPlayerState.skillPoint);
        Assert.Equal(snapshot.lockedSigil, actorPlayerState.lockedSigil);
        Assert.Equal(snapshot.isSigilLocked, actorPlayerState.isSigilLocked);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(snapshot.producedEventCount, sentinelChain.producedEvents.Count);
    }

    private static ActionChainState createSentinelActionChain()
    {
        var sentinelChain = new ActionChainState
        {
            actionChainId = new ActionChainId(81991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 81991,
            eventTypeKey = "actionAccepted",
            requestId = 81991,
            actorPlayerId = new PlayerId(1),
            requestTypeKey = "sentinel",
        });
        return sentinelChain;
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

    private static void addFieldZone(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones[playerState.fieldZoneId] = new ZoneState
        {
            zoneId = playerState.fieldZoneId,
            zoneType = ZoneKey.field,
            ownerPlayerId = playerState.playerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
    }

    private static void addDeckZone(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones[playerState.deckZoneId] = new ZoneState
        {
            zoneId = playerState.deckZoneId,
            zoneType = ZoneKey.deck,
            ownerPlayerId = playerState.playerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
    }

    private static void addHandZone(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones[playerState.handZoneId] = new ZoneState
        {
            zoneId = playerState.handZoneId,
            zoneType = ZoneKey.hand,
            ownerPlayerId = playerState.playerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
    }

    private static void addDiscardZone(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones[playerState.discardZoneId] = new ZoneState
        {
            zoneId = playerState.discardZoneId,
            zoneType = ZoneKey.discard,
            ownerPlayerId = playerState.playerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
    }

    private static void addFieldTreasureCard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.fieldZoneId,
            zoneKey = ZoneKey.field,
            isFaceUp = true,
            isSetAside = false,
        };
        gameState.zones[playerState.fieldZoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static void addCardInZone(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        ZoneId zoneId,
        ZoneKey zoneKey,
        string definitionId)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = zoneId,
            zoneKey = zoneKey,
            isFaceUp = true,
            isSetAside = false,
        };
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static string createEndPhaseDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return EndPhaseDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static string createTurnStartShackleDiscardChoiceKey(CardInstanceId cardInstanceId)
    {
        return TurnStartShackleDiscardChoiceKeyPrefix + cardInstanceId.Value;
    }

    private static void applyCharacterShackleStatus(
        RuleCore.GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Shackle",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "resolveAtNextTurnStart",
            });
    }

    private static RuleCore.GameState.GameState createEnterEndReadyGameState(
        PlayerId actorPlayerId,
        TeamId actorTeamId,
        PlayerState actorPlayerState,
        int turnNumber)
    {
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = turnNumber,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
            },
        };
        gameState.players[actorPlayerId] = actorPlayerState;
        addDeckZone(gameState, actorPlayerState);
        addHandZone(gameState, actorPlayerState);
        addDiscardZone(gameState, actorPlayerState);
        addFieldZone(gameState, actorPlayerState);
        return gameState;
    }

    private static RuleCore.GameState.GameState createStartNextTurnReadyGameState(
        PlayerId currentPlayerId,
        PlayerId nextPlayerId,
        TeamId currentTeamId,
        TeamId nextTeamId,
        PlayerState currentPlayerState,
        PlayerState nextPlayerState,
        int turnNumber)
    {
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            matchMeta = new MatchMeta(),
            turnState = new TurnState
            {
                turnNumber = turnNumber,
                currentPlayerId = currentPlayerId,
                currentTeamId = currentTeamId,
                currentPhase = TurnPhase.end,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta.seatOrder.Add(currentPlayerId);
        gameState.matchMeta.seatOrder.Add(nextPlayerId);
        gameState.matchMeta.teamAssignments[currentPlayerId] = currentTeamId;
        gameState.matchMeta.teamAssignments[nextPlayerId] = nextTeamId;
        gameState.players[currentPlayerId] = currentPlayerState;
        gameState.players[nextPlayerId] = nextPlayerState;
        addDeckZone(gameState, currentPlayerState);
        addDeckZone(gameState, nextPlayerState);
        addHandZone(gameState, currentPlayerState);
        addHandZone(gameState, nextPlayerState);
        addDiscardZone(gameState, currentPlayerState);
        addDiscardZone(gameState, nextPlayerState);
        addFieldZone(gameState, currentPlayerState);
        addFieldZone(gameState, nextPlayerState);
        return gameState;
    }
}
