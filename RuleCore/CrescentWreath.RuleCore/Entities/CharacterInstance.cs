using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Entities;

public sealed class CharacterInstance
{
    public CharacterInstanceId characterInstanceId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public PlayerId ownerPlayerId { get; set; }
    public bool isAlive { get; set; }
    public bool isInPlay { get; set; }
}
