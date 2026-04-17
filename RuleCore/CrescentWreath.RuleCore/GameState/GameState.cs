using System.Collections.Generic;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.GameState;

public sealed class GameState
{
    public MatchState matchState { get; set; } = MatchState.initializing;
    public TeamId? winnerTeamId { get; set; }
    public Dictionary<PlayerId, PlayerState> players { get; } = new();
    public Dictionary<TeamId, TeamState> teams { get; } = new();
    public Dictionary<CardInstanceId, CardInstance> cardInstances { get; } = new();
    public Dictionary<CharacterInstanceId, CharacterInstance> characterInstances { get; } = new();
    public List<StatusInstance> statusInstances { get; } = new();
    public Dictionary<ZoneId, ZoneState> zones { get; } = new();
    public MatchMeta? matchMeta { get; set; }
    public PublicState? publicState { get; set; }
    public TurnState? turnState { get; set; }

    public ActionChainState? currentActionChain { get; set; }
    public ResponseWindowState? currentResponseWindow { get; set; }
    public InputContextState? currentInputContext { get; set; }

    public CurrentAnomalyState? currentAnomalyState { get; set; }
}
