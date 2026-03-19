using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.GameState;

public sealed class GameState
{
    public Dictionary<PlayerId, PlayerState> players { get; } = new();
    public Dictionary<TeamId, TeamState> teams { get; } = new();
    public Dictionary<CardInstanceId, CardInstance> cardInstances { get; } = new();
    public Dictionary<CharacterInstanceId, CharacterInstance> characterInstances { get; } = new();
    public Dictionary<ZoneKey, ZoneState> zones { get; } = new();

    public ActionChainState? currentActionChain { get; set; }
    public ResponseWindowState? currentResponseWindow { get; set; }
    public InputContextState? currentInputContext { get; set; }

    public string? currentAnomalyDefinitionId { get; set; }
}
