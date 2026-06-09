using System;
using CrescentWreath.RuleCore.Entities;

namespace CrescentWreath.RuleCore.Zones;

public static class TreasureStaticMovementRuleGuard
{
    private const string DefinitionIdT017 = "T017";
    private const string DefinitionIdT020 = "T020";
    private const string DefinitionIdT029 = "T029";

    public static void ensureTreasureStaticMovementRestrictionsOrThrow(
        CardInstance cardInstance,
        ZoneKey fromZoneKey,
        ZoneKey toZoneKey,
        CardMoveReason moveReason)
    {
        if (string.Equals(cardInstance.definitionId, DefinitionIdT017, StringComparison.Ordinal) &&
            fromZoneKey == ZoneKey.summonZone &&
            (moveReason == CardMoveReason.banish || toZoneKey == ZoneKey.gapZone))
        {
            throw new InvalidOperationException("T017 static movement restriction prevents moving from summonZone to gapZone by banish.");
        }

        if ((string.Equals(cardInstance.definitionId, DefinitionIdT020, StringComparison.Ordinal) ||
             string.Equals(cardInstance.definitionId, DefinitionIdT029, StringComparison.Ordinal)) &&
            fromZoneKey == ZoneKey.gapZone &&
            toZoneKey != ZoneKey.gapZone)
        {
            throw new InvalidOperationException($"{cardInstance.definitionId} static movement restriction prevents leaving gapZone.");
        }
    }
}
