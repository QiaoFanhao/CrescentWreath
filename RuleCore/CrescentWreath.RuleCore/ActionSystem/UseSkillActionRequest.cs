using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class UseSkillActionRequest : ActionRequest
{
    public CharacterInstanceId characterInstanceId { get; set; }
    public string skillKey { get; set; } = string.Empty;
}
