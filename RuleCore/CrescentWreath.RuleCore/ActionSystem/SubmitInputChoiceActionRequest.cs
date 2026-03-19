using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitInputChoiceActionRequest : ActionRequest
{
    public InputContextId inputContextId { get; set; }
    public string choiceKey { get; set; } = string.Empty;
}
