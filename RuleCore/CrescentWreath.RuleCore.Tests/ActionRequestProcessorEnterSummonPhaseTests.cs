using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorEnterSummonPhaseTests
{
    [Fact]
    public void HappyPath_WhenCurrentPhaseIsAction_ShouldSwitchToSummonAndCompleteChain()
    {
        var actorPlayerId = new PlayerId(1);
        var teamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, teamId, 6000);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addFieldTreasureCard(gameState, actorPlayerState, new CardInstanceId(61001), "starter:magicCircuit");
        addFieldTreasureCard(gameState, actorPlayerState, new CardInstanceId(61002), "starter:kourindouCoupon");
        actorPlayerState.sigilPreview = 7;
        actorPlayerState.lockedSigil = null;
        actorPlayerState.isSigilLocked = false;
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = actorPlayerId,
            currentTeamId = teamId,
            currentPhase = TurnPhase.action,
            phaseStepIndex = 7,
        };

        var request = new EnterSummonPhaseActionRequest
        {
            requestId = 40101,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.Empty(producedEvents);
        Assert.Equal(TurnPhase.summon, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.Equal(0, gameState.players[actorPlayerId].sigilPreview);
        Assert.Equal(1, gameState.players[actorPlayerId].lockedSigil);
        Assert.True(gameState.players[actorPlayerId].isSigilLocked);
        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<EnterSummonPhaseActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(1, gameState.currentActionChain.currentFrameIndex);
    }

    [Fact]
    public void WhenActorIsNotCurrentTurnPlayer_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var currentPlayerId = new PlayerId(2);
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var actorPlayerState = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = teamAId,
            sigilPreview = 4,
            lockedSigil = 2,
            isSigilLocked = true,
        };
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[currentPlayerId] = new PlayerState { playerId = currentPlayerId, teamId = teamBId };
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = currentPlayerId,
            currentTeamId = teamBId,
            currentPhase = TurnPhase.action,
            phaseStepIndex = 3,
        };
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var phaseBefore = gameState.turnState.currentPhase;
        var stepBefore = gameState.turnState.phaseStepIndex;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var sigilPreviewBefore = actorPlayerState.sigilPreview;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var isSigilLockedBefore = actorPlayerState.isSigilLocked;

        var request = new EnterSummonPhaseActionRequest
        {
            requestId = 40102,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("EnterSummonPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.", exception.Message);
        Assert.Equal(phaseBefore, gameState.turnState.currentPhase);
        Assert.Equal(stepBefore, gameState.turnState.phaseStepIndex);
        Assert.Equal(sigilPreviewBefore, actorPlayerState.sigilPreview);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Equal(isSigilLockedBefore, actorPlayerState.isSigilLocked);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenEnteringSummonTwice_ShouldRejectSecondEnterAndKeepLockedSigilUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var teamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, teamId, 6200);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        addFieldZone(gameState, actorPlayerState);
        addFieldTreasureCard(gameState, actorPlayerState, new CardInstanceId(62001), "starter:kourindouCoupon");
        actorPlayerState.sigilPreview = 3;
        actorPlayerState.lockedSigil = null;
        actorPlayerState.isSigilLocked = false;
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = actorPlayerId,
            currentTeamId = teamId,
            currentPhase = TurnPhase.action,
            phaseStepIndex = 5,
        };

        var processor = new ActionRequestProcessor();
        var firstRequest = new EnterSummonPhaseActionRequest
        {
            requestId = 40105,
            actorPlayerId = actorPlayerId,
        };

        var firstEvents = processor.processActionRequest(gameState, firstRequest);
        Assert.Empty(firstEvents);
        Assert.Equal(TurnPhase.summon, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.players[actorPlayerId].sigilPreview);
        Assert.Equal(1, gameState.players[actorPlayerId].lockedSigil);
        Assert.True(gameState.players[actorPlayerId].isSigilLocked);

        var chainAfterFirstEnter = gameState.currentActionChain;
        var producedEventsBeforeSecondEnter = chainAfterFirstEnter!.producedEvents.Count;
        gameState.players[actorPlayerId].sigilPreview = 7;
        var sigilPreviewBeforeSecondEnter = gameState.players[actorPlayerId].sigilPreview;
        var lockedSigilBeforeSecondEnter = gameState.players[actorPlayerId].lockedSigil;
        var isSigilLockedBeforeSecondEnter = gameState.players[actorPlayerId].isSigilLocked;

        var secondRequest = new EnterSummonPhaseActionRequest
        {
            requestId = 40106,
            actorPlayerId = actorPlayerId,
        };

        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, secondRequest));
        Assert.Equal("EnterSummonPhaseActionRequest requires gameState.turnState.currentPhase to be action.", exception.Message);
        Assert.Equal(sigilPreviewBeforeSecondEnter, gameState.players[actorPlayerId].sigilPreview);
        Assert.Equal(lockedSigilBeforeSecondEnter, gameState.players[actorPlayerId].lockedSigil);
        Assert.Equal(isSigilLockedBeforeSecondEnter, gameState.players[actorPlayerId].isSigilLocked);
        Assert.Same(chainAfterFirstEnter, gameState.currentActionChain);
        Assert.Equal(producedEventsBeforeSecondEnter, chainAfterFirstEnter.producedEvents.Count);
    }

    [Fact]
    public void WhenCurrentPhaseIsNotAction_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var teamId = new TeamId(1);
        var actorPlayerState = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = teamId,
            sigilPreview = 3,
            lockedSigil = 1,
            isSigilLocked = true,
        };
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = actorPlayerId,
            currentTeamId = teamId,
            currentPhase = TurnPhase.start,
            phaseStepIndex = 2,
        };
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var phaseBefore = gameState.turnState.currentPhase;
        var stepBefore = gameState.turnState.phaseStepIndex;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var sigilPreviewBefore = actorPlayerState.sigilPreview;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var isSigilLockedBefore = actorPlayerState.isSigilLocked;

        var request = new EnterSummonPhaseActionRequest
        {
            requestId = 40103,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("EnterSummonPhaseActionRequest requires gameState.turnState.currentPhase to be action.", exception.Message);
        Assert.Equal(phaseBefore, gameState.turnState.currentPhase);
        Assert.Equal(stepBefore, gameState.turnState.phaseStepIndex);
        Assert.Equal(sigilPreviewBefore, actorPlayerState.sigilPreview);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Equal(isSigilLockedBefore, actorPlayerState.isSigilLocked);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenCurrentResponseWindowIsActive_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var teamId = new TeamId(1);
        var actorPlayerState = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = teamId,
            sigilPreview = 5,
            lockedSigil = 2,
            isSigilLocked = true,
        };
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = actorPlayerId,
            currentTeamId = teamId,
            currentPhase = TurnPhase.action,
            phaseStepIndex = 1,
        };
        gameState.currentResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(77701),
            windowTypeKey = "damageResponse",
            originType = ResponseWindowOriginType.flow,
            currentResponderPlayerId = actorPlayerId,
        };
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var phaseBefore = gameState.turnState.currentPhase;
        var stepBefore = gameState.turnState.phaseStepIndex;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var sigilPreviewBefore = actorPlayerState.sigilPreview;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var isSigilLockedBefore = actorPlayerState.isSigilLocked;

        var request = new EnterSummonPhaseActionRequest
        {
            requestId = 40104,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("EnterSummonPhaseActionRequest requires gameState.currentResponseWindow to be null.", exception.Message);
        Assert.Equal(phaseBefore, gameState.turnState.currentPhase);
        Assert.Equal(stepBefore, gameState.turnState.phaseStepIndex);
        Assert.Equal(sigilPreviewBefore, actorPlayerState.sigilPreview);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Equal(isSigilLockedBefore, actorPlayerState.isSigilLocked);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    private static ActionChainState createSentinelActionChain()
    {
        var sentinelChain = new ActionChainState
        {
            actionChainId = new ActionChainId(40991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 40991,
            eventTypeKey = "actionAccepted",
            requestId = 40991,
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
}
