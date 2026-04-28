namespace CrescentWreath.RuleCore.Definitions;

public sealed class TreasureDefinition
{
    public string definitionId { get; set; } = string.Empty;

    public int initialPublicDeckCopies { get; set; }

    public int manaGainOnEnterField { get; set; }

    public int sigilPreviewGainOnEnterField { get; set; }

    public int? summonSigilCost { get; set; }

    public bool persistOnFieldAcrossEnd { get; set; }

    public int? defenseValue { get; set; }

    public string? defenseTypeKey { get; set; }
}
