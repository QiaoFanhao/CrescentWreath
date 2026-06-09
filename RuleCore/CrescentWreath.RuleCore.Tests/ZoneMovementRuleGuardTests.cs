using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ZoneMovementRuleGuardTests
{
    [Fact]
    public void MoveCard_WhenPlayFromHandToField_ShouldSucceed()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.hand, ZoneKey.field);
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.play,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.hand, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.field, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.play, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    [Fact]
    public void MoveCard_WhenDefensePlaceFromHandToField_ShouldSucceed()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.hand, ZoneKey.field);
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.defensePlace,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.hand, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.field, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.defensePlace, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    [Fact]
    public void MoveCard_WhenSummonFromSummonZoneToDiscard_ShouldSucceed()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.summonZone, ZoneKey.discard);
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.summon,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.summonZone, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.summon, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    [Fact]
    public void MoveCard_WhenSummonFromSakuraCakeDeckToDiscard_ShouldSucceed()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.sakuraCakeDeck, ZoneKey.discard);
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.summon,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.sakuraCakeDeck, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.summon, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    [Fact]
    public void MoveCard_WhenPlayRouteInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.hand, ZoneKey.discard);
        var service = new ZoneMovementService();
        var sourceCountBefore = gameState.zones[sourceZoneId].cardInstanceIds.Count;
        var targetCountBefore = gameState.zones[targetZoneId].cardInstanceIds.Count;
        var cardZoneBefore = cardInstance.zoneId;

        var exception = Assert.Throws<InvalidOperationException>(() => service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.play,
            new ActionChainId(1),
            1));

        Assert.Equal("ZoneMovementService requires play moveReason to move from hand to field.", exception.Message);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
        Assert.Equal(sourceCountBefore, gameState.zones[sourceZoneId].cardInstanceIds.Count);
        Assert.Equal(targetCountBefore, gameState.zones[targetZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void MoveCard_WhenSummonRouteInvalid_ShouldThrowAndKeepStateUnchanged()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(ZoneKey.publicTreasureDeck, ZoneKey.discard);
        var service = new ZoneMovementService();
        var sourceCountBefore = gameState.zones[sourceZoneId].cardInstanceIds.Count;
        var targetCountBefore = gameState.zones[targetZoneId].cardInstanceIds.Count;
        var cardZoneBefore = cardInstance.zoneId;

        var exception = Assert.Throws<InvalidOperationException>(() => service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.summon,
            new ActionChainId(1),
            1));

        Assert.Equal("ZoneMovementService requires summon moveReason to move from summonZone or sakuraCakeDeck to discard.", exception.Message);
        Assert.Equal(cardZoneBefore, cardInstance.zoneId);
        Assert.Equal(sourceCountBefore, gameState.zones[sourceZoneId].cardInstanceIds.Count);
        Assert.Equal(targetCountBefore, gameState.zones[targetZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void MoveCard_WhenT017InSummonZoneIsBanishedToGapZone_ShouldThrowAndKeepStateUnchanged()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(
            ZoneKey.summonZone,
            ZoneKey.gapZone,
            definitionId: "T017");
        var service = new ZoneMovementService();

        var exception = Assert.Throws<InvalidOperationException>(() => service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.banish,
            new ActionChainId(1),
            1));

        Assert.Equal("T017 static movement restriction prevents moving from summonZone to gapZone by banish.", exception.Message);
        assertMoveRejectedStateUnchanged(gameState, cardInstance, sourceZoneId, targetZoneId);
    }

    [Fact]
    public void MoveCard_WhenT017InSummonZoneIsSummoned_ShouldSucceed()
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(
            ZoneKey.summonZone,
            ZoneKey.discard,
            definitionId: "T017");
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.summon,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.summonZone, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.discard, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.summon, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    [Theory]
    [InlineData("T020")]
    [InlineData("T029")]
    public void MoveCard_WhenGapLockedTreasureInGapZoneMovesToHand_ShouldThrowAndKeepStateUnchanged(string definitionId)
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(
            ZoneKey.gapZone,
            ZoneKey.hand,
            definitionId);
        var service = new ZoneMovementService();

        var exception = Assert.Throws<InvalidOperationException>(() => service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.returnToSource,
            new ActionChainId(1),
            1));

        Assert.Equal($"{definitionId} static movement restriction prevents leaving gapZone.", exception.Message);
        assertMoveRejectedStateUnchanged(gameState, cardInstance, sourceZoneId, targetZoneId);
    }

    [Theory]
    [InlineData("T020")]
    [InlineData("T029")]
    public void MoveCard_WhenGapLockedTreasureEntersGapZoneFromNonGapZone_ShouldSucceed(string definitionId)
    {
        var (gameState, cardInstance, sourceZoneId, targetZoneId) = createState(
            ZoneKey.discard,
            ZoneKey.gapZone,
            definitionId);
        var service = new ZoneMovementService();

        var movedEvent = service.moveCard(
            gameState,
            cardInstance,
            targetZoneId,
            CardMoveReason.banish,
            new ActionChainId(1),
            1);

        Assert.Equal(ZoneKey.discard, movedEvent.fromZoneKey);
        Assert.Equal(ZoneKey.gapZone, movedEvent.toZoneKey);
        Assert.Equal(CardMoveReason.banish, movedEvent.moveReason);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }

    private static (RuleCore.GameState.GameState gameState, CardInstance cardInstance, ZoneId sourceZoneId, ZoneId targetZoneId) createState(
        ZoneKey sourceZoneKey,
        ZoneKey targetZoneKey,
        string definitionId = "test")
    {
        var gameState = new RuleCore.GameState.GameState();
        var sourceZoneId = new ZoneId(100);
        var targetZoneId = new ZoneId(101);
        var cardInstanceId = new CardInstanceId(500);
        var ownerPlayerId = new PlayerId(1);

        gameState.zones[sourceZoneId] = new ZoneState
        {
            zoneId = sourceZoneId,
            zoneType = sourceZoneKey,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };

        gameState.zones[targetZoneId] = new ZoneState
        {
            zoneId = targetZoneId,
            zoneType = targetZoneKey,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerId,
            zoneId = sourceZoneId,
            zoneKey = sourceZoneKey,
            isFaceUp = true,
        };
        gameState.cardInstances[cardInstanceId] = cardInstance;
        gameState.zones[sourceZoneId].cardInstanceIds.Add(cardInstanceId);

        return (gameState, cardInstance, sourceZoneId, targetZoneId);
    }

    private static void assertMoveRejectedStateUnchanged(
        RuleCore.GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneId sourceZoneId,
        ZoneId targetZoneId)
    {
        Assert.Equal(sourceZoneId, cardInstance.zoneId);
        Assert.Equal(gameState.zones[sourceZoneId].zoneType, cardInstance.zoneKey);
        Assert.Contains(cardInstance.cardInstanceId, gameState.zones[sourceZoneId].cardInstanceIds);
        Assert.DoesNotContain(cardInstance.cardInstanceId, gameState.zones[targetZoneId].cardInstanceIds);
    }
}
