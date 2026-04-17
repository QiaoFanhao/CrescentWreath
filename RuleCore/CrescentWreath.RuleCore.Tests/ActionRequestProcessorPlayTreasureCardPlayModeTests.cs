using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorPlayTreasureCardPlayModeTests
{
    [Fact]
    public void ProcessPlayTreasureCardActionRequest_WhenPlayModeIsNormal_ShouldUsePlayMoveReason()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2000);
        var cardInstanceId = new CardInstanceId(20001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 20002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        var movedEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.play, movedEvent.moveReason);
        Assert.Equal(1, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.sigilPreview);
        Assert.False(gameState.cardInstances[cardInstanceId].isDefensePlacedOnField);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(CardMoveReason.play, gameState.currentActionChain.effectFrames[0].moveReason);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_WhenPlayModeIsDefense_ShouldUseDefensePlaceMoveReason()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2100);
        var cardInstanceId = new CardInstanceId(21001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 21002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "defense",
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        var movedEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.defensePlace, movedEvent.moveReason);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.sigilPreview);
        Assert.True(gameState.cardInstances[cardInstanceId].isDefensePlacedOnField);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(CardMoveReason.defensePlace, gameState.currentActionChain.effectFrames[0].moveReason);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_WhenPlayModeIsLegacyPlay_ShouldRemainCompatibleWithPlayMoveReason()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2200);
        var cardInstanceId = new CardInstanceId(22001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "test:playModeLegacy");

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 22002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "play",
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        var movedEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.play, movedEvent.moveReason);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(CardMoveReason.play, gameState.currentActionChain.effectFrames[0].moveReason);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_WhenStarterKourindouCouponIsPlayed_ShouldNotIncreaseMana()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2300);
        var cardInstanceId = new CardInstanceId(23001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "starter:kourindouCoupon");

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 23002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        var movedEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.play, movedEvent.moveReason);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.sigilPreview);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Theory]
    [InlineData(TurnPhase.start)]
    [InlineData(TurnPhase.summon)]
    [InlineData(TurnPhase.end)]
    public void ProcessPlayTreasureCardActionRequest_WhenCurrentPhaseIsNotAction_ShouldThrowAndKeepStateUnchanged(
        TurnPhase currentPhase)
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2400);
        actorPlayerState.mana = 3;
        actorPlayerState.sigilPreview = 2;
        var cardInstanceId = new CardInstanceId(24001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addStandardPlayerZones(gameState, actorPlayerState);
        setRunningTurnForActor(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "starter:magicCircuit");

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var sentinelProducedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var sigilPreviewBefore = actorPlayerState.sigilPreview;
        var cardInstance = gameState.cardInstances[cardInstanceId];
        var cardZoneBefore = cardInstance.zoneId;
        var handCountBefore = gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count;
        var fieldCountBefore = gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count;

        var request = new PlayTreasureCardActionRequest
        {
            requestId = 24002,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("PlayTreasureCardActionRequest requires gameState.turnState.currentPhase to be action.", exception.Message);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(sigilPreviewBefore, actorPlayerState.sigilPreview);
        Assert.Equal(handCountBefore, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(fieldCountBefore, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(sentinelProducedEventsBefore, sentinelChain.producedEvents.Count);
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

    private static void setRunningTurnForActor(
        RuleCore.GameState.GameState gameState,
        PlayerId currentPlayerId,
        TeamId currentTeamId,
        TurnPhase currentPhase = TurnPhase.action)
    {
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = currentPlayerId,
            currentTeamId = currentTeamId,
            currentPhase = currentPhase,
            phaseStepIndex = 0,
        };
    }

    private static ActionChainState createSentinelActionChain()
    {
        var sentinelChain = new ActionChainState
        {
            actionChainId = new ActionChainId(24991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 24991,
            eventTypeKey = "actionAccepted",
            requestId = 24991,
            actorPlayerId = new PlayerId(1),
            requestTypeKey = "sentinel",
        });
        return sentinelChain;
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
}
