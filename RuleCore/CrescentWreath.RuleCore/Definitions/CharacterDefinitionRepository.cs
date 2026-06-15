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
            characterName = string.Empty,
            baseMaxHp = 4,
            factionKey = string.Empty,
            raceTags = new List<string>(),
            allowedMarkerTypes = new List<string>(),
            markerCaps = new Dictionary<string, int>(),
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        };
    }

    public static IReadOnlyList<CharacterDefinition> getAllDefinitions()
    {
        var definitions = new List<CharacterDefinition>();
        foreach (var definition in DefinitionsById.Values)
        {
            if (definition.definitionId.StartsWith("C", System.StringComparison.Ordinal))
            {
                definitions.Add(definition);
            }
        }

        definitions.Sort((left, right) =>
            System.StringComparer.Ordinal.Compare(left.definitionId, right.definitionId));
        return definitions;
    }

    public static bool tryResolveSkillCost(
        string characterDefinitionId,
        string skillKey,
        out int manaCost,
        out int leylineCost)
    {
        var characterDefinition = resolveByDefinitionId(characterDefinitionId);
        if (characterDefinition.skills.TryGetValue(skillKey, out var characterSkillDefinition))
        {
            manaCost = characterSkillDefinition.manaCost;
            leylineCost = characterSkillDefinition.leylineCost;
            return true;
        }

        manaCost = 0;
        leylineCost = 0;
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
