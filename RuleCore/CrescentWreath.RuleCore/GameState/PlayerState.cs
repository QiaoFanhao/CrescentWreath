using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.GameState;

public sealed class PlayerState
{
    public PlayerId playerId { get; set; }
    public TeamId teamId { get; set; }
    public int hp { get; set; }
    public int leyline { get; set; }
    public int killScore { get; set; }
    public List<ZoneKey> ownedZones { get; } = new();
}
