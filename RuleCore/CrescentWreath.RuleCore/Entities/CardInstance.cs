using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Entities;

public sealed class CardInstance
{
    public CardInstanceId cardInstanceId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public PlayerId ownerPlayerId { get; set; }
    public ZoneKey zoneKey { get; set; }
    public bool isFaceUp { get; set; }
    public bool isSetAside { get; set; }
}
