using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class TurnState
{
    public int turnNumber { get; set; }
    public PlayerId currentPlayerId { get; set; }
    public TeamId currentTeamId { get; set; }
    public TurnPhase currentPhase { get; set; } = TurnPhase.start;
    public int phaseStepIndex { get; set; }
    public bool hasResolvedAnomalyThisTurn { get; set; }
}
