using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Events;

public sealed class CharacterActivationChangedEvent : GameEvent
{
    public PlayerId targetPlayerId { get; set; }
    public CharacterInstanceId targetCharacterInstanceId { get; set; }
    public bool wasActivated { get; set; }
    public bool isActivated { get; set; }
}
