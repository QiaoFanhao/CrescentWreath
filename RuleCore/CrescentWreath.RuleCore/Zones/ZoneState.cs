using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public sealed class ZoneState
{
    public ZoneId zoneId { get; set; }
    public ZoneKey zoneType { get; set; }
    public PlayerId? ownerPlayerId { get; set; }
    public ZonePublicOrPrivate publicOrPrivate { get; set; }
    public List<CardInstanceId> cardInstanceIds { get; } = new();
}
