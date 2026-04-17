using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;

namespace CrescentWreath.RuleCore.Events;

public sealed class InteractionWindowEvent : GameEvent
{
    public string windowKindKey { get; set; } = string.Empty;
    public ResponseWindowId? responseWindowId { get; set; }
    public ResponseWindowOriginType? responseWindowOriginType { get; set; }
    public InputContextId? inputContextId { get; set; }
    public bool isOpened { get; set; }
}
