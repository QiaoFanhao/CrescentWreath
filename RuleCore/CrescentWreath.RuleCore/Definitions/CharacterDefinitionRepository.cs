using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public static class CharacterDefinitionRepository
{
    private static readonly ICharacterDefinitionSource Source = new InMemoryCharacterDefinitionSource();
    private static readonly Dictionary<string, CharacterDefinition> DefinitionsById = buildDefinitionsById(Source);

    public static CharacterDefinition resolveByDefinitionId(string definitionId)
    {
        if (DefinitionsById.TryGetValue(definitionId, out var characterDefinition))
        {
            return characterDefinition;
        }

        return new CharacterDefinition
        {
            definitionId = definitionId,
            raceTags = new List<string>(),
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        };
    }

    public static bool tryResolveSkillCost(
        string characterDefinitionId,
        string skillKey,
        out int manaCost,
        out int skillPointCost)
    {
        var characterDefinition = resolveByDefinitionId(characterDefinitionId);
        if (characterDefinition.skills.TryGetValue(skillKey, out var characterSkillDefinition))
        {
            manaCost = characterSkillDefinition.manaCost;
            skillPointCost = characterSkillDefinition.skillPointCost;
            return true;
        }

        manaCost = 0;
        skillPointCost = 0;
        return false;
    }

    internal static Dictionary<string, CharacterDefinition> buildDefinitionsById(ICharacterDefinitionSource source)
    {
        var definitionsById = new Dictionary<string, CharacterDefinition>();
        foreach (var characterDefinition in source.getCharacterDefinitions())
        {
            definitionsById[characterDefinition.definitionId] = characterDefinition;
        }

        return definitionsById;
    }
}
