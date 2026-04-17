namespace CrescentWreath.RuleCore.Definitions;

public sealed class CharacterSkillDefinition
{
    public string skillKey { get; set; } = string.Empty;

    public int manaCost { get; set; }

    public int skillPointCost { get; set; }

    public string? skillType { get; set; }
}
