using CrescentWreath.RuleCore.GameState;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class SigilSnapshotCalculator
{
    public static int recomputeSigilPreviewFromCurrentFieldState(
        GameState.GameState gameState,
        PlayerState actorPlayerState)
    {
        if (!gameState.zones.TryGetValue(actorPlayerState.fieldZoneId, out var fieldZoneState))
        {
            return 0;
        }

        var recomputedSigilPreview = 0;
        foreach (var cardInstanceId in fieldZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            recomputedSigilPreview += TreasureResourceValueResolver.resolveSigilPreviewGainOnEnterField(cardInstance.definitionId);
        }

        return recomputedSigilPreview;
    }
}
