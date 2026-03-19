using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public sealed class ZoneState
{
    public ZoneKey zoneKey { get; set; }
    public PlayerId? ownerPlayerId { get; set; }
    public List<CardInstanceId> cardInstanceIds { get; } = new();
    public Dictionary<string, string> tags { get; } = new();
}
