using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class MarkerChangedEvent : GameEvent
{
    public CharacterInstanceId targetCharacterInstanceId { get; set; }
    public PlayerId targetPlayerId { get; set; }
    public string markerTypeKey { get; set; } = string.Empty;
    public int beforeCount { get; set; }
    public int afterCount { get; set; }
    public int delta { get; set; }
}
