using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class CardRevealedEvent : GameEvent
{
    public CardInstanceId cardInstanceId { get; set; }
    public PlayerId ownerPlayerId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public string revealReasonKey { get; set; } = string.Empty;
}
