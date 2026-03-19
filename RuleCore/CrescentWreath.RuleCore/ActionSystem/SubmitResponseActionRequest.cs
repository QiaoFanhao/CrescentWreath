using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitResponseActionRequest : ActionRequest
{
    public ResponseWindowId responseWindowId { get; set; }
    public bool shouldRespond { get; set; }
    public string? responseKey { get; set; }
}
