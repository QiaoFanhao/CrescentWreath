using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class StatusChangedEvent : GameEvent
{
    public string statusKey { get; set; } = string.Empty;
    public CardInstanceId? targetCardInstanceId { get; set; }
    public CharacterInstanceId? targetCharacterInstanceId { get; set; }
    public bool isApplied { get; set; }
}
