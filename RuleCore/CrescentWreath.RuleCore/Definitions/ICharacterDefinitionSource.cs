using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public interface ICharacterDefinitionSource
{
    IReadOnlyList<CharacterDefinition> getCharacterDefinitions();
}
