using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Events;

public sealed class CardMovedEvent : GameEvent
{
    public CardInstanceId cardInstanceId { get; set; }
    public ZoneKey fromZoneKey { get; set; }
    public ZoneKey toZoneKey { get; set; }
    public CardMoveReason moveReason { get; set; }
}
