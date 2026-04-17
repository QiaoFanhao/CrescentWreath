using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class MatchMeta
{
    public List<PlayerId> seatOrder { get; } = new();
    public Dictionary<PlayerId, TeamId> teamAssignments { get; } = new();
    public Dictionary<TeamId, List<PlayerId>> teamSeatMap { get; } = new();
    public int baseKillScore { get; set; }
    public MatchMode mode { get; set; } = MatchMode.standard2v2;
}
