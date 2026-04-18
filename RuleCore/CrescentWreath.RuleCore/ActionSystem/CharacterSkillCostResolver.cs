using System;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class CharacterSkillCostResolver
{
    public static (int manaCost, int skillPointCost) resolveSkillCost(string characterDefinitionId, string skillKey)
    {
        if (CharacterDefinitionRepository.tryResolveSkillCost(
                characterDefinitionId,
                skillKey,
                out var manaCost,
                out var skillPointCost))
        {
            return (manaCost, skillPointCost);
        }

        throw new NotSupportedException("UseSkillActionRequest requires skillKey to exist in character definition.");
    }
}

