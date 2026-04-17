using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class PlayTreasureCardActionRequest : ActionRequest
{
    public CardInstanceId cardInstanceId { get; set; }
    public string playMode { get; set; } = "play";
    public List<string> targets { get; } = new();
    public Dictionary<string, string> extraSelections { get; } = new();
}
