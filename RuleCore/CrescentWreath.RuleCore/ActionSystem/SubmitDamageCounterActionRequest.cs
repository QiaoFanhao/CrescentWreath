using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitDamageCounterActionRequest : ActionRequest
{
    public ResponseWindowId responseWindowId { get; set; }
    public string counterTypeKey { get; set; } = string.Empty;
}
