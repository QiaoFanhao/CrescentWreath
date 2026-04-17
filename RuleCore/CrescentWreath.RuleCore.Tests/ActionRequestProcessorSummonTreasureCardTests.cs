using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorSummonTreasureCardTests
{
    [Fact]
    public void HappyPath_WhenPublicTreasureDeckHasCard_ShouldSummonAndRefillSummonZone()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1000);
        actorPlayerState.sigilPreview = 5;
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var summonedCardInstanceId = new CardInstanceId(5001);
        var refillCardInstanceId = new CardInstanceId(5002);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        var publicTreasureDeckZoneId = gameState.publicState.publicTreasureDeckZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var summonedCardInstance = new CardInstance
        {
            cardInstanceId = summonedCardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(summonedCardInstanceId, summonedCardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(summonedCardInstanceId);

        var refillCardInstance = new CardInstance
        {
            cardInstanceId = refillCardInstanceId,
            definitionId = "test-public-deck-card",
            ownerPlayerId = actorPlayerId,
            zoneId = publicTreasureDeckZoneId,
            zoneKey = ZoneKey.publicTreasureDeck,
        };
        gameState.cardInstances.Add(refillCardInstanceId, refillCardInstance);
        gameState.zones[publicTreasureDeckZoneId].cardInstanceIds.Add(refillCardInstanceId);

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9801,
            actorPlayerId = actorPlayerId,
            cardInstanceId = summonedCardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.DoesNotContain(summonedCardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Contains(summonedCardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.discardZoneId, summonedCardInstance.zoneId);
        Assert.Equal(ZoneKey.discard, summonedCardInstance.zoneKey);

        Assert.DoesNotContain(refillCardInstanceId, gameState.zones[publicTreasureDeckZoneId].cardInstanceIds);
        Assert.Contains(refillCardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(summonZoneId, refillCardInstance.zoneId);
        Assert.Equal(ZoneKey.summonZone, refillCardInstance.zoneKey);
        Assert.Equal(0, actorPlayerState.lockedSigil);
        Assert.Equal(5, actorPlayerState.sigilPreview);

        Assert.Equal(2, producedEvents.Count);
        var summonMovedEvent = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        Assert.Equal(summonedCardInstanceId, summonMovedEvent.cardInstanceId);
        Assert.Equal(ZoneKey.summonZone, summonMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, summonMovedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.summon, summonMovedEvent.moveReason);

        var refillMovedEvent = Assert.IsType<CardMovedEvent>(producedEvents[1]);
        Assert.Equal(refillCardInstanceId, refillMovedEvent.cardInstanceId);
        Assert.Equal(ZoneKey.publicTreasureDeck, refillMovedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.summonZone, refillMovedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.reveal, refillMovedEvent.moveReason);

        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<SummonTreasureCardActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(1, gameState.currentActionChain.currentFrameIndex);
    }

    [Fact]
    public void HappyPath_WhenPublicTreasureDeckIsEmpty_ShouldSummonWithoutRefill()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1100);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var summonedCardInstanceId = new CardInstanceId(5003);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        var publicTreasureDeckZoneId = gameState.publicState.publicTreasureDeckZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var summonedCardInstance = new CardInstance
        {
            cardInstanceId = summonedCardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(summonedCardInstanceId, summonedCardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(summonedCardInstanceId);

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9804,
            actorPlayerId = actorPlayerId,
            cardInstanceId = summonedCardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.Empty(gameState.zones[publicTreasureDeckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Contains(summonedCardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(actorPlayerState.discardZoneId, summonedCardInstance.zoneId);
        Assert.Equal(ZoneKey.discard, summonedCardInstance.zoneKey);
        Assert.Equal(0, actorPlayerState.lockedSigil);

        Assert.Single(producedEvents);
        var movedEvent = Assert.IsType<CardMovedEvent>(producedEvents[0]);
        Assert.Equal(summonedCardInstanceId, movedEvent.cardInstanceId);
        Assert.Equal(ZoneKey.summonZone, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.summon, movedEvent.moveReason);

        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void WhenActorIsNotCurrentTurnPlayer_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var currentTurnPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2000);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var currentTurnPlayerState = createPlayerState(currentTurnPlayerId, new TeamId(2), 3000);
        currentTurnPlayerState.lockedSigil = 1;
        currentTurnPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5005);
        var refillCardInstanceId = new CardInstanceId(5006);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(currentTurnPlayerId, currentTurnPlayerState);
        setRunningTurnForPlayer(gameState, currentTurnPlayerId, currentTurnPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        var publicTreasureDeckZoneId = gameState.publicState.publicTreasureDeckZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardInstanceId);

        var refillCardInstance = new CardInstance
        {
            cardInstanceId = refillCardInstanceId,
            definitionId = "test-public-deck-card",
            ownerPlayerId = actorPlayerId,
            zoneId = publicTreasureDeckZoneId,
            zoneKey = ZoneKey.publicTreasureDeck,
        };
        gameState.cardInstances.Add(refillCardInstanceId, refillCardInstance);
        gameState.zones[publicTreasureDeckZoneId].cardInstanceIds.Add(refillCardInstanceId);

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var lockedSigilBefore = actorPlayerState.lockedSigil;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9802,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("SummonTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.", exception.Message);
        Assert.Equal(summonZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.summonZone, cardInstance.zoneKey);
        Assert.Contains(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(refillCardInstanceId, gameState.zones[publicTreasureDeckZoneId].cardInstanceIds);
        Assert.DoesNotContain(refillCardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenCardIsNotInSummonZone_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4000);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5007);
        var refillCardInstanceId = new CardInstanceId(5008);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        var publicTreasureDeckZoneId = gameState.publicState.publicTreasureDeckZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.handZoneId, ZoneKey.hand, actorPlayerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = actorPlayerState.handZoneId,
            zoneKey = ZoneKey.hand,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Add(cardInstanceId);

        var refillCardInstance = new CardInstance
        {
            cardInstanceId = refillCardInstanceId,
            definitionId = "test-public-deck-card",
            ownerPlayerId = actorPlayerId,
            zoneId = publicTreasureDeckZoneId,
            zoneKey = ZoneKey.publicTreasureDeck,
        };
        gameState.cardInstances.Add(refillCardInstanceId, refillCardInstance);
        gameState.zones[publicTreasureDeckZoneId].cardInstanceIds.Add(refillCardInstanceId);

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var lockedSigilBefore = actorPlayerState.lockedSigil;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9803,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("SummonTreasureCardActionRequest requires cardInstance.zoneId to equal gameState.publicState.summonZoneId.", exception.Message);
        Assert.Equal(actorPlayerState.handZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, cardInstance.zoneKey);
        Assert.Contains(cardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(refillCardInstanceId, gameState.zones[publicTreasureDeckZoneId].cardInstanceIds);
        Assert.DoesNotContain(refillCardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenMatchStateIsEnded_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4100);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5009);
        var refillCardInstanceId = new CardInstanceId(5010);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        var publicTreasureDeckZoneId = gameState.publicState.publicTreasureDeckZoneId;

        addZone(gameState, publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardInstanceId);

        var refillCardInstance = new CardInstance
        {
            cardInstanceId = refillCardInstanceId,
            definitionId = "test-public-deck-card",
            ownerPlayerId = actorPlayerId,
            zoneId = publicTreasureDeckZoneId,
            zoneKey = ZoneKey.publicTreasureDeck,
        };
        gameState.cardInstances.Add(refillCardInstanceId, refillCardInstance);
        gameState.zones[publicTreasureDeckZoneId].cardInstanceIds.Add(refillCardInstanceId);

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        gameState.matchState = MatchState.ended;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9810,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.", exception.Message);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
        Assert.Contains(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(refillCardInstanceId, gameState.zones[publicTreasureDeckZoneId].cardInstanceIds);
        Assert.DoesNotContain(refillCardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(summonZoneId, cardInstance.zoneId);
        Assert.Equal(ZoneKey.summonZone, cardInstance.zoneKey);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
    }

    [Fact]
    public void WhenLockedSigilIsInsufficient_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4200);
        actorPlayerState.lockedSigil = 0;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5011);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        addZone(gameState, gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardInstanceId);

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var producedEventsBefore = sentinelChain.producedEvents.Count;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9811,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("SummonTreasureCardActionRequest requires actor player lockedSigil to be sufficient for summon cost.", exception.Message);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Contains(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenCurrentPhaseIsNotSummon_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4300);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = true;
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5012);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, TurnPhase.start);
        gameState.publicState = createPublicState(summonZoneId);
        addZone(gameState, gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardInstanceId);

        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var producedEventsBefore = sentinelChain.producedEvents.Count;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9812,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));

        Assert.Equal("SummonTreasureCardActionRequest requires gameState.turnState.currentPhase to be summon.", exception.Message);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Contains(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenSigilIsNotLockedOrLockedSigilIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var teamId = new TeamId(1);
        var summonZoneId = new ZoneId(9004);
        var cardInstanceId = new CardInstanceId(5013);

        var gameState = new RuleCore.GameState.GameState();
        var actorPlayerState = createPlayerState(actorPlayerId, teamId, 4400);
        actorPlayerState.lockedSigil = 1;
        actorPlayerState.isSigilLocked = false;
        gameState.players.Add(actorPlayerId, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, teamId, TurnPhase.summon);
        gameState.publicState = createPublicState(summonZoneId);
        addZone(gameState, gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, actorPlayerState.discardZoneId, ZoneKey.discard, actorPlayerId, ZonePublicOrPrivate.publicZone);
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-summon-card",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
        };
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardInstanceId);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var lockedSigilBefore = actorPlayerState.lockedSigil;
        var producedEventsBefore = sentinelChain.producedEvents.Count;

        var request = new SummonTreasureCardActionRequest
        {
            requestId = 9813,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
        };

        var processor = new ActionRequestProcessor();
        var lockException = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));
        Assert.Equal("SummonTreasureCardActionRequest requires actor player sigil to be locked.", lockException.Message);
        Assert.Equal(lockedSigilBefore, actorPlayerState.lockedSigil);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);

        actorPlayerState.isSigilLocked = true;
        actorPlayerState.lockedSigil = null;
        var nullLockedException = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, request));
        Assert.Equal("SummonTreasureCardActionRequest requires actor player lockedSigil to be initialized.", nullLockedException.Message);
        Assert.Null(actorPlayerState.lockedSigil);
        Assert.Contains(cardInstanceId, gameState.zones[summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    private static ActionChainState createSentinelActionChain()
    {
        var sentinelChain = new ActionChainState
        {
            actionChainId = new ActionChainId(9991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 9991,
            eventTypeKey = "actionAccepted",
            requestId = 9991,
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

    private static PublicState createPublicState(ZoneId summonZoneId)
    {
        return new PublicState
        {
            publicTreasureDeckZoneId = new ZoneId(9001),
            anomalyDeckZoneId = new ZoneId(9002),
            sakuraCakeDeckZoneId = new ZoneId(9003),
            summonZoneId = summonZoneId,
            gapZoneId = new ZoneId(9005),
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

    private static void setRunningTurnForPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId currentPlayerId,
        TeamId currentTeamId,
        TurnPhase currentPhase = TurnPhase.summon)
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
}
