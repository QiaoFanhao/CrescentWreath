using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.DamageSystem;

public sealed class DamageContext
{
    public DamageContextId damageContextId { get; set; }

    public PlayerId? sourcePlayerId { get; set; }
    public CardInstanceId? sourceCardInstanceId { get; set; }
    public CharacterInstanceId? sourceCharacterInstanceId { get; set; }

    public PlayerId? targetPlayerId { get; set; }
    public CardInstanceId? targetCardInstanceId { get; set; }
    public CharacterInstanceId? targetCharacterInstanceId { get; set; }

    public int baseDamageValue { get; set; }
    public string? defenseDeclarationKey { get; set; }
    public bool isReplaced { get; set; }
    public bool isImmune { get; set; }
    public bool isPrevented { get; set; }
    public int finalDamageValue { get; set; }
    public bool didDealDamage { get; set; }
}
