using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitInputChoiceActionRequest : ActionRequest
{
    public InputContextId inputContextId { get; set; }
    public string choiceKey { get; set; } = string.Empty;
    public List<string> choiceKeys { get; } = new();
}
