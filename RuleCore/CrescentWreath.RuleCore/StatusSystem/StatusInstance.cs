using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.StatusSystem;

public sealed class StatusInstance
{
    public string statusKey { get; set; } = string.Empty;

    public PlayerId? applierPlayerId { get; set; }
    public CharacterInstanceId? applierCharacterInstanceId { get; set; }
    public CardInstanceId? applierCardInstanceId { get; set; }

    public CardInstanceId? targetCardInstanceId { get; set; }
    public CharacterInstanceId? targetCharacterInstanceId { get; set; }
    public PlayerId? targetPlayerId { get; set; }

    public int stackCount { get; set; }
    public string durationTypeKey { get; set; } = string.Empty;
    public int? remainingDuration { get; set; }
    public Dictionary<string, string> parameters { get; } = new();
}
