using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ResponseSystem;

public sealed class ResponseWindowState
{
    public ResponseWindowId responseWindowId { get; set; }
    public string? windowTypeKey { get; set; }
    public ActionChainId? sourceActionChainId { get; set; }
    public List<PlayerId> responderPlayerIds { get; } = new();
    public List<string> usedResponseKeys { get; } = new();
    public PlayerId? currentResponderPlayerId { get; set; }
}
