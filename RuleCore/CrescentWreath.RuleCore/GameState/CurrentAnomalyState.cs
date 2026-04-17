using System.Collections.Generic;

namespace CrescentWreath.RuleCore.GameState;

public sealed class CurrentAnomalyState
{
    public string? currentAnomalyDefinitionId { get; set; }

    public List<string> anomalyDeckDefinitionIds { get; } = new();

    public Dictionary<string, string> localState { get; } = new();
}
