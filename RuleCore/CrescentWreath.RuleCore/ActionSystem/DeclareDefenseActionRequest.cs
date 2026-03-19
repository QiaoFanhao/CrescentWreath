using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class DeclareDefenseActionRequest : ActionRequest
{
    public CardInstanceId defenseCardInstanceId { get; set; }
    public string defenseTypeKey { get; set; } = string.Empty;
}
