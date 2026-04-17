using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class PlayerState
{
    public PlayerId playerId { get; set; }
    public TeamId teamId { get; set; }
    public CharacterInstanceId? activeCharacterInstanceId { get; set; }
    public int mana { get; set; }
    public int skillPoint { get; set; }
    public int sigilPreview { get; set; }
    public int? lockedSigil { get; set; }
    public bool isSigilLocked { get; set; }
    public ZoneId deckZoneId { get; set; }
    public ZoneId handZoneId { get; set; }
    public ZoneId discardZoneId { get; set; }
    public ZoneId fieldZoneId { get; set; }
    public ZoneId characterSetAsideZoneId { get; set; }
}
