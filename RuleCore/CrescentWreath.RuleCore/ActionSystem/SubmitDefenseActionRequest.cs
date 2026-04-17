using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class SubmitDefenseActionRequest : ActionRequest
{
    public CardInstanceId defenseCardInstanceId { get; set; }
    public string defenseTypeKey { get; set; } = string.Empty;
}
