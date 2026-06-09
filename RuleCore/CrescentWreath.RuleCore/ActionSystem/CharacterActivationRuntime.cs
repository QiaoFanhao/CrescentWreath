using System;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class CharacterActivationRuntime
{
    private const string ActivationSkillTypeRaw = "\u542f\u52a8";

    public static bool canCharacterActivate(
        CharacterInstanceId characterInstanceId,
        RuleCore.GameState.GameState gameState)
    {
        if (!gameState.characterInstances.TryGetValue(characterInstanceId, out var characterInstance))
        {
            return false;
        }

        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        return characterDefinition.skills.Values.Any(
            skill => string.Equals(skill.skillTypeRaw, ActivationSkillTypeRaw, StringComparison.Ordinal));
    }

    public static CharacterActivationChangedEvent setActivated(
        RuleCore.GameState.GameState gameState,
        CharacterInstanceId characterInstanceId,
        bool isActivated,
        ActionChainId? sourceActionChainId,
        long eventId)
    {
        if (!gameState.characterInstances.TryGetValue(characterInstanceId, out var characterInstance))
        {
            throw new InvalidOperationException("Character activation requires characterInstanceId to exist.");
        }

        if (isActivated && !canCharacterActivate(characterInstanceId, gameState))
        {
            throw new InvalidOperationException("Character activation requires the character definition to contain an activation skill.");
        }

        var wasActivated = characterInstance.isActivated;
        characterInstance.isActivated = isActivated;
        return new CharacterActivationChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "characterActivationChanged",
            sourceActionChainId = sourceActionChainId,
            targetPlayerId = characterInstance.ownerPlayerId,
            targetCharacterInstanceId = characterInstanceId,
            wasActivated = wasActivated,
            isActivated = isActivated,
        };
    }
}
