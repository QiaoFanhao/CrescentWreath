namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitCharacterSelectionActionRequest : ActionRequest
{
    public string characterDefinitionId { get; set; } = string.Empty;
}
