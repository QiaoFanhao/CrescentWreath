using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public sealed class ZoneMovementService
{
    public CardMovedEvent moveCard(
        GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneId targetZoneId,
        CardMoveReason moveReason,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var fromZoneId = cardInstance.zoneId;
        var fromZoneState = gameState.zones[fromZoneId];
        var toZoneState = gameState.zones[targetZoneId];

        ZoneMovementRuleGuard.ensureCoreMoveReasonRouteOrThrow(fromZoneState.zoneType, toZoneState.zoneType, moveReason);

        fromZoneState.cardInstanceIds.Remove(cardInstance.cardInstanceId);
        toZoneState.cardInstanceIds.Add(cardInstance.cardInstanceId);
        cardInstance.zoneId = targetZoneId;
        cardInstance.zoneKey = toZoneState.zoneType;

        return new CardMovedEvent
        {
            eventId = eventId,
            eventTypeKey = "cardMoved",
            sourceActionChainId = sourceActionChainId,
            cardInstanceId = cardInstance.cardInstanceId,
            fromZoneKey = fromZoneState.zoneType,
            toZoneKey = toZoneState.zoneType,
            moveReason = moveReason,
        };
    }
}
