using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class TeamState
{
    public TeamId teamId { get; set; }
    public List<PlayerId> memberPlayerIds { get; } = new();
}
