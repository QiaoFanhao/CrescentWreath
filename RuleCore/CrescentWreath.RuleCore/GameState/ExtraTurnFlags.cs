using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class ExtraTurnFlags
{
    public PlayerId? pendingExtraTurnForPlayerId { get; set; }
    public bool extraTurnGrantedThisTurn { get; set; }
    public bool isCurrentTurnExtraTurn { get; set; }
}
