namespace CrescentWreath.RuleCore.ActionSystem;

public static class TreasureResourceValueResolver
{
    public static int resolveManaGainOnEnterField(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.resolveManaGainOnEnterField(definitionId);
    }

    public static int resolveSigilPreviewGainOnEnterField(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.resolveSigilPreviewGainOnEnterField(definitionId);
    }

    public static int resolveSummonSigilCost(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.resolveSummonSigilCost(definitionId);
    }

    public static bool shouldPersistOnFieldAcrossEnd(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.shouldPersistOnFieldAcrossEnd(definitionId);
    }

    public static int? resolveDefenseValue(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.resolveDefenseValue(definitionId);
    }

    public static string? resolveDefenseTypeKey(string definitionId)
    {
        return TemporaryTreasureDefinitionResolver.resolveDefenseTypeKey(definitionId);
    }
}
