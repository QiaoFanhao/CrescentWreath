using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public sealed class ZoneMovementService
{
    public CardMovedEvent moveCard(
        GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneKey targetZoneKey,
        CardMoveReason moveReason,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var fromZoneKey = cardInstance.zoneKey;
        var fromZoneState = gameState.zones[fromZoneKey];
        var toZoneState = gameState.zones[targetZoneKey];

        fromZoneState.cardInstanceIds.Remove(cardInstance.cardInstanceId);
        toZoneState.cardInstanceIds.Add(cardInstance.cardInstanceId);
        cardInstance.zoneKey = targetZoneKey;

        return new CardMovedEvent
        {
            eventId = eventId,
            eventTypeKey = "cardMoved",
            sourceActionChainId = sourceActionChainId,
            cardInstanceId = cardInstance.cardInstanceId,
            fromZoneKey = fromZoneKey,
            toZoneKey = targetZoneKey,
            moveReason = moveReason,
        };
    }
}
