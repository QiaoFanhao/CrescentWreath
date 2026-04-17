namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TryResolveAnomalyActionRequest : ActionRequest
{
    public CrescentWreath.RuleCore.Ids.PlayerId? targetPlayerId { get; set; }
}
