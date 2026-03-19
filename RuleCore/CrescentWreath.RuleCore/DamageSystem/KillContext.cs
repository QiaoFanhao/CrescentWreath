using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.DamageSystem;

public sealed class KillContext
{
    public long killContextId { get; set; }
    public PlayerId? killerPlayerId { get; set; }
    public CharacterInstanceId? killedCharacterInstanceId { get; set; }
    public PlayerId? killedPlayerId { get; set; }
    public bool causedByDamage { get; set; }
    public DamageContextId? sourceDamageContextId { get; set; }
}
