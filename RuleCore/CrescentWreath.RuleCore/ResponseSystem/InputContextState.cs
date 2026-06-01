using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ResponseSystem;

public sealed class InputContextState
{
    public InputContextId inputContextId { get; set; }
    public PlayerId? requiredPlayerId { get; set; }
    public List<PlayerId> requiredPlayerIds { get; } = new();
    public ActionChainId? sourceActionChainId { get; set; }
    public string? inputTypeKey { get; set; }
    public string? contextKey { get; set; }
    public List<string> choiceKeys { get; } = new();
    public Dictionary<long, List<string>> choiceKeysByRequiredPlayerNumericId { get; } = new();
    public List<PlayerId> submittedPlayerIds { get; } = new();
    public string? selectedChoiceKey { get; set; }
}
