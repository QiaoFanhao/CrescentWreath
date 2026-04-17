using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class InMemoryCharacterDefinitionSource : ICharacterDefinitionSource
{
    private static readonly IReadOnlyList<CharacterDefinition> CharacterDefinitions = new List<CharacterDefinition>
    {
        new()
        {
            definitionId = "C004",
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C004:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C004:1",
                    manaCost = 5,
                    skillPointCost = 1,
                    skillType = null,
                },
            },
        },
        new()
        {
            definitionId = "test:human",
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        },
        new()
        {
            definitionId = "test:nonHuman",
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        },
    };

    public IReadOnlyList<CharacterDefinition> getCharacterDefinitions()
    {
        return CharacterDefinitions;
    }
}
