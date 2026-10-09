using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class SkillTriggeredEvent : GameEvent
{
    public PlayerId actorPlayerId { get; set; }

    public CharacterInstanceId characterInstanceId { get; set; }

    public string characterDefinitionId { get; set; } = string.Empty;

    public string skillKey { get; set; } = string.Empty;

    public string triggerTypeKey { get; set; } = string.Empty;
}
