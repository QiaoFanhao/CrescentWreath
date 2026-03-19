using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class PlayCardActionRequest : ActionRequest
{
    public CardInstanceId cardInstanceId { get; set; }
    public ZoneKey targetZoneKey { get; set; }
}
