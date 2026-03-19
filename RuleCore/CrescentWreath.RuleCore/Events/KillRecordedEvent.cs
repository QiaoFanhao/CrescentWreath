using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class KillRecordedEvent : GameEvent
{
    public long killContextId { get; set; }
    public PlayerId? killerPlayerId { get; set; }
    public CharacterInstanceId? killedCharacterInstanceId { get; set; }
    public DamageContextId? sourceDamageContextId { get; set; }
}
