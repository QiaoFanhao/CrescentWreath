using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class ActionAcceptedEvent : GameEvent
{
    public long requestId { get; set; }
    public PlayerId actorPlayerId { get; set; }
    public string requestTypeKey { get; set; } = string.Empty;
}
