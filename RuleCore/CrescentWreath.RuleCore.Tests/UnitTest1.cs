using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class UnitTest1
{
    [Fact]
    public void ProcessPlayCardRequest_M2HappyPath_ShouldMoveCardAndProduceCardMovedEvent()
    {
        var playerId = new PlayerId(1);
        var cardInstanceId = new CardInstanceId(1001);
        var sourceZoneKey = ZoneKey.hand;
        var targetZoneKey = ZoneKey.summonZone;

        var gameState = new RuleCore.GameState.GameState();

        var sourceZoneState = new ZoneState
        {
            zoneKey = sourceZoneKey,
            ownerPlayerId = playerId,
        };
        sourceZoneState.cardInstanceIds.Add(cardInstanceId);

        var targetZoneState = new ZoneState
        {
            zoneKey = targetZoneKey,
            ownerPlayerId = playerId,
        };

        gameState.zones.Add(sourceZoneKey, sourceZoneState);
        gameState.zones.Add(targetZoneKey, targetZoneState);

        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = "test-card",
            ownerPlayerId = playerId,
            zoneKey = sourceZoneKey,
        };
        gameState.cardInstances.Add(cardInstanceId, cardInstance);

        var playCardActionRequest = new PlayCardActionRequest
        {
            requestId = 42,
            actorPlayerId = playerId,
            cardInstanceId = cardInstanceId,
            targetZoneKey = targetZoneKey,
        };

        var actionRequestProcessor = new ActionRequestProcessor();

        var events = actionRequestProcessor.processActionRequest(gameState, playCardActionRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.DoesNotContain(cardInstanceId, sourceZoneState.cardInstanceIds);
        Assert.Contains(cardInstanceId, targetZoneState.cardInstanceIds);
        Assert.Equal(targetZoneKey, cardInstance.zoneKey);
        Assert.Single(events);
        Assert.IsType<CardMovedEvent>(events[0]);
    }
}
