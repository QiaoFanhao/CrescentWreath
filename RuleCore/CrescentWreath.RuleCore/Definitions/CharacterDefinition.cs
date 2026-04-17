using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class CharacterDefinition
{
    public string definitionId { get; set; } = string.Empty;

    public List<string> raceTags { get; set; } = new();

    public Dictionary<string, CharacterSkillDefinition> skills { get; set; } = new();
}
