using System.Collections.Generic;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class OpenInputContextActionRequest : ActionRequest
{
    public string inputTypeKey { get; set; } = string.Empty;
    public string? contextKey { get; set; }
    public List<string> choiceKeys { get; } = new();
}
