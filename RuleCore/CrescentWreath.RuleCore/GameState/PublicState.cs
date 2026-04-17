using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class PublicState
{
    public ZoneId publicTreasureDeckZoneId { get; set; }
    public ZoneId anomalyDeckZoneId { get; set; }
    public ZoneId sakuraCakeDeckZoneId { get; set; }
    public ZoneId summonZoneId { get; set; }
    public ZoneId gapZoneId { get; set; }
}
