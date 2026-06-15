using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class CharacterSelectedEvent : GameEvent
{
    public PlayerId playerId { get; set; }
    public CharacterInstanceId characterInstanceId { get; set; }
    public string characterDefinitionId { get; set; } = string.Empty;
}
