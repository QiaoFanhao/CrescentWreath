using CrescentWreath.RuleCore.Ids;
using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Entities;

public sealed class CharacterInstance
{
    public CharacterInstanceId characterInstanceId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public PlayerId ownerPlayerId { get; set; }
    public int currentHp { get; set; }
    public int maxHp { get; set; } = 4;
    public bool isAlive { get; set; }
    public bool isInPlay { get; set; }
    public bool hasPendingOnKilledReplacement { get; set; }
    public List<string> raceTags { get; set; } = new();
}
