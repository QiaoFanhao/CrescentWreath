namespace CrescentWreath.RuleCore.Definitions;

public sealed class CharacterSkillDefinition
{
    public string skillKey { get; set; } = string.Empty;

    public string skillName { get; set; } = string.Empty;

    public int skillOrder { get; set; }

    public string skillTypeRaw { get; set; } = string.Empty;

    public string skillCostRaw { get; set; } = string.Empty;

    public int manaCost { get; set; }

    public int leylineCost { get; set; }

    public string? skillType { get; set; }

    public string effectText { get; set; } = string.Empty;
}

