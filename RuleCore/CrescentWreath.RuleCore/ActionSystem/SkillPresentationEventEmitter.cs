using System;
using System.Threading;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

internal static class SkillPresentationEventEmitter
{
    private const string EmittedStateKeyPrefix = "presentation:skillTriggered:";
    private static long nextPresentationEventNumericId = 10_000_000;

    public static void appendIfNeeded(
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        CharacterInstance characterInstance,
        string skillKey,
        string triggerTypeKey)
    {
        if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(triggerTypeKey))
        {
            return;
        }

        var emittedStateKey = EmittedStateKeyPrefix + skillKey + ":" + triggerTypeKey;
        if (actionChainState.localState.ContainsKey(emittedStateKey))
        {
            return;
        }

        actionChainState.localState[emittedStateKey] = "true";
        actionChainState.producedEvents.Add(new SkillTriggeredEvent
        {
            eventId = Interlocked.Increment(ref nextPresentationEventNumericId),
            eventTypeKey = "skillTriggered",
            sourceActionChainId = actionChainState.actionChainId,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstance.characterInstanceId,
            characterDefinitionId = characterInstance.definitionId,
            skillKey = skillKey,
            triggerTypeKey = triggerTypeKey,
        });
    }
}
