using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorDrawOneCardTests
{
    [Fact]
    public void HappyPath_WhenDeckHasCard_ShouldMoveTopCardFromDeckToHand()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3000);
        var firstDeckCardId = new CardInstanceId(30001);
        var secondDeckCardId = new CardInstanceId(30002);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addZone(gameState, actorPlayerState.deckZoneId, ZoneKey.deck, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.handZoneId, ZoneKey.hand, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);

        createCardInZone(gameState, firstDeckCardId, actorPlayerId, actorPlayerState.deckZoneId, ZoneKey.deck, "deck-first");
        createCardInZone(gameState, secondDeckCardId, actorPlayerId, actorPlayerState.deckZoneId, ZoneKey.deck, "deck-second");

        var request = new DrawOneCardActionRequest
        {
            requestId = 30101,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.Single(gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Equal(firstDeckCardId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds[0]);
        Assert.Equal(secondDeckCardId, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds[0]);
        Assert.True(gameState.currentActionChain!.isCompleted);

        var movedEvent = Assert.IsType<CardMovedEvent>(Assert.Single(producedEvents));
        Assert.Equal(CardMoveReason.draw, movedEvent.moveReason);
        Assert.Equal(ZoneKey.deck, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, movedEvent.toZoneKey);
    }

    [Fact]
    public void WhenDeckIsEmptyAndDiscardHasCards_ShouldRebuildThenDrawOne()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3100);
        var firstDiscardCardId = new CardInstanceId(31001);
        var secondDiscardCardId = new CardInstanceId(31002);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addZone(gameState, actorPlayerState.deckZoneId, ZoneKey.deck, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.handZoneId, ZoneKey.hand, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);

        createCardInZone(gameState, firstDiscardCardId, actorPlayerId, actorPlayerState.discardZoneId, ZoneKey.discard, "discard-first");
        createCardInZone(gameState, secondDiscardCardId, actorPlayerId, actorPlayerState.discardZoneId, ZoneKey.discard, "discard-second");

        var request = new DrawOneCardActionRequest
        {
            requestId = 31101,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.True(gameState.currentActionChain!.isCompleted);

        Assert.Empty(gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Equal(firstDiscardCardId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds[0]);
        Assert.Equal(secondDiscardCardId, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds[0]);

        Assert.Equal(3, producedEvents.Count);
        var rebuildEvent1 = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        var rebuildEvent2 = Assert.IsType<CardMovedEvent>(producedEvents[1]);
        var drawEvent = Assert.IsType<CardMovedEvent>(producedEvents[2]);

        Assert.Equal(CardMoveReason.returnToSource, rebuildEvent1.moveReason);
        Assert.Equal(CardMoveReason.returnToSource, rebuildEvent2.moveReason);
        Assert.Equal(ZoneKey.discard, rebuildEvent1.fromZoneKey);
        Assert.Equal(ZoneKey.deck, rebuildEvent1.toZoneKey);
        Assert.Equal(ZoneKey.discard, rebuildEvent2.fromZoneKey);
        Assert.Equal(ZoneKey.deck, rebuildEvent2.toZoneKey);

        Assert.Equal(CardMoveReason.draw, drawEvent.moveReason);
        Assert.Equal(ZoneKey.deck, drawEvent.fromZoneKey);
        Assert.Equal(ZoneKey.hand, drawEvent.toZoneKey);
    }

    [Fact]
    public void WhenDeckAndDiscardAreBothEmpty_ShouldCompleteWithoutDrawing()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3200);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addZone(gameState, actorPlayerState.deckZoneId, ZoneKey.deck, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.handZoneId, ZoneKey.hand, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);

        var request = new DrawOneCardActionRequest
        {
            requestId = 32101,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.Empty(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Empty(producedEvents);
    }

    [Fact]
    public void WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3300);
        var deckCardId = new CardInstanceId(33001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        addZone(gameState, actorPlayerState.deckZoneId, ZoneKey.deck, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.handZoneId, ZoneKey.hand, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);
        createCardInZone(gameState, deckCardId, actorPlayerId, actorPlayerState.deckZoneId, ZoneKey.deck, "deck-card");

        var sentinelChain = new RuleCore.EffectSystem.ActionChainState
        {
            actionChainId = new ActionChainId(33991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 33991,
            eventTypeKey = "actionAccepted",
            requestId = 33991,
            actorPlayerId = actorPlayerId,
            requestTypeKey = "sentinel",
        });
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        gameState.matchState = MatchState.ended;

        var request = new DrawOneCardActionRequest
        {
            requestId = 33101,
            actorPlayerId = actorPlayerId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
        Assert.Single(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Equal(deckCardId, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds[0]);
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

    private static void addZone(
        RuleCore.GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey zoneType,
        PlayerId? ownerPlayerId,
        ZonePublicOrPrivate publicOrPrivate)
    {
        gameState.zones.Add(
            zoneId,
            new ZoneState
            {
                zoneId = zoneId,
                zoneType = zoneType,
                ownerPlayerId = ownerPlayerId,
                publicOrPrivate = publicOrPrivate,
            });
    }

    private static void createCardInZone(
        RuleCore.GameState.GameState gameState,
        CardInstanceId cardInstanceId,
        PlayerId ownerPlayerId,
        ZoneId zoneId,
        ZoneKey zoneKey,
        string definitionId)
    {
        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerId,
            zoneId = zoneId,
            zoneKey = zoneKey,
        };

        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static void setRunningTurnForPlayer(
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
            currentPhase = TurnPhase.start,
            phaseStepIndex = 0,
        };
    }
}
