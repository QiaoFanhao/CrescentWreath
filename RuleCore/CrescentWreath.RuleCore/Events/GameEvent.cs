using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public abstract class GameEvent
{
    public long eventId { get; set; }
    public string eventTypeKey { get; set; } = string.Empty;
    public ActionChainId? sourceActionChainId { get; set; }
}
