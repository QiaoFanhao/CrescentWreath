using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public abstract class ActionRequest
{
    public long requestId { get; set; }
    public PlayerId actorPlayerId { get; set; }
    public string? sourceKey { get; set; }
}
