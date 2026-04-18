using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class CharacterDefinition
{
    public string definitionId { get; set; } = string.Empty;

    public string characterName { get; set; } = string.Empty;

    public int baseMaxHp { get; set; } = 4;

    public List<string> raceTags { get; set; } = new();

    public Dictionary<string, CharacterSkillDefinition> skills { get; set; } = new();
}

