using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.GameState;

public sealed class CharacterSelectionState
{
    public PlayerId? currentSelectingPlayerId { get; set; }
    public Dictionary<PlayerId, string> selectedCharacterDefinitionIds { get; } = new();
    public bool isCompleted { get; set; }
}
