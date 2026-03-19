using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.EffectSystem;

public sealed class EffectFrame
{
    public string effectKey { get; set; } = string.Empty;
    public CardInstanceId? movingCardInstanceId { get; set; }
    public ZoneKey? fromZoneKey { get; set; }
    public ZoneKey? toZoneKey { get; set; }
    public CardMoveReason? moveReason { get; set; }
    public PlayerId? sourcePlayerId { get; set; }
    public CardInstanceId? sourceCardInstanceId { get; set; }
    public CharacterInstanceId? sourceCharacterInstanceId { get; set; }
    public PlayerId? targetPlayerId { get; set; }
    public CardInstanceId? targetCardInstanceId { get; set; }
    public CharacterInstanceId? targetCharacterInstanceId { get; set; }
    public string? contextKey { get; set; }
}
