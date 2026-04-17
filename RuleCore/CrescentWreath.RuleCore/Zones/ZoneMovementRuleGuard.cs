using System;

namespace CrescentWreath.RuleCore.Zones;

public static class ZoneMovementRuleGuard
{
    public static void ensureCoreMoveReasonRouteOrThrow(
        ZoneKey fromZoneKey,
        ZoneKey toZoneKey,
        CardMoveReason moveReason)
    {
        if (moveReason == CardMoveReason.play)
        {
            if (fromZoneKey != ZoneKey.hand || toZoneKey != ZoneKey.field)
            {
                throw new InvalidOperationException("ZoneMovementService requires play moveReason to move from hand to field.");
            }

            return;
        }

        if (moveReason == CardMoveReason.defensePlace)
        {
            if (fromZoneKey != ZoneKey.hand || toZoneKey != ZoneKey.field)
            {
                throw new InvalidOperationException("ZoneMovementService requires defensePlace moveReason to move from hand to field.");
            }

            return;
        }

        if (moveReason == CardMoveReason.summon)
        {
            var isValidSourceZone =
                fromZoneKey == ZoneKey.summonZone ||
                fromZoneKey == ZoneKey.sakuraCakeDeck;
            if (!isValidSourceZone || toZoneKey != ZoneKey.discard)
            {
                throw new InvalidOperationException("ZoneMovementService requires summon moveReason to move from summonZone or sakuraCakeDeck to discard.");
            }
        }
    }
}
