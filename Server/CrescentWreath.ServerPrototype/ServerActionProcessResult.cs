using System.Collections.Generic;
using CrescentWreath.RuleCore.Events;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerActionProcessResult
{
    public bool isSucceeded { get; set; }
    public RuleCore.GameState.GameState updatedState { get; set; } = new();
    public List<GameEvent> producedEvents { get; set; } = new();
    public string? errorMessage { get; set; }
}
