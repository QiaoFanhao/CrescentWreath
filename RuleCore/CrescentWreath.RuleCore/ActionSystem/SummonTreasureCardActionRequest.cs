using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SummonTreasureCardActionRequest : ActionRequest
{
    public CardInstanceId cardInstanceId { get; set; }
}
