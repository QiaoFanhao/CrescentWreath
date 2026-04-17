using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class OpenDamageResponseWindowActionRequest : ActionRequest
{
    public CharacterInstanceId targetCharacterInstanceId { get; set; }
    public int baseDamageValue { get; set; }
    public string? damageTypeKey { get; set; }
    public CardInstanceId? sourceCardInstanceId { get; set; }
    public CharacterInstanceId? sourceCharacterInstanceId { get; set; }
}
