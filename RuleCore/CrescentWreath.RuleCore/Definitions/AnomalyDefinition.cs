using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class AnomalyDefinition
{
    public string definitionId { get; set; } = string.Empty;

    public string name { get; set; } = string.Empty;

    public string oncePerTurnHint { get; set; } = string.Empty;

    public string arrivalText { get; set; } = string.Empty;

    public string resolveText { get; set; } = string.Empty;

    public string sourceHeaderRaw { get; set; } = string.Empty;

    public string resolveConditionKey { get; set; } = string.Empty;

    public string resolveRewardKey { get; set; } = string.Empty;

    public int? resolveManaCost { get; set; }

    public int? resolveFriendlyTeamHpCostPerPlayer { get; set; }

    public int rewardActorTeamLeylineDelta { get; set; }

    public int rewardOpponentTeamKillScoreDelta { get; set; }

    public string rewardStatusKey { get; set; } = string.Empty;

    public List<AnomalyArrivalStepDefinition> arrivalSteps { get; } = new();

    public List<AnomalyConditionStepDefinition> conditionSteps { get; } = new();

    public List<AnomalyRewardStepDefinition> rewardSteps { get; } = new();
}
