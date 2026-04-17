using System.Collections.Generic;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.EffectSystem;

public sealed class ActionChainState
{
    public ActionChainId actionChainId { get; set; }
    public PlayerId? actorPlayerId { get; set; }
    public ActionRequest? rootActionRequest { get; set; }
    public string? pendingContinuationKey { get; set; }
    public bool isCompleted { get; set; }
    public List<EffectFrame> effectFrames { get; } = new();
    public int currentFrameIndex { get; set; }
    public List<GameEvent> producedEvents { get; } = new();
    public List<string> producedEventKeys { get; } = new();
}
