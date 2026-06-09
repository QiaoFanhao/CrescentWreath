using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Entities;

public sealed class MarkerState
{
    public Dictionary<string, int> markerMap { get; set; } = new();
}
