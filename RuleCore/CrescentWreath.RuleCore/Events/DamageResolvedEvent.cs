using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class DamageResolvedEvent : GameEvent
{
    public DamageContextId damageContextId { get; set; }
    public int finalDamageValue { get; set; }
    public bool didDealDamage { get; set; }
}
