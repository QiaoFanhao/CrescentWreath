using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class HpChangedEvent : GameEvent
{
    public PlayerId targetPlayerId { get; set; }
    public CharacterInstanceId? targetCharacterInstanceId { get; set; }
    public int hpBefore { get; set; }
    public int hpAfter { get; set; }
    public int delta { get; set; }
}
