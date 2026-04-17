namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyRewardAction
{
    public string rewardActionKey { get; set; } = string.Empty;

    public bool isOptional { get; set; }

    public int actorTeamLeylineDelta { get; set; }

    public int opponentTeamKillScoreDelta { get; set; }

    public string statusKey { get; set; } = string.Empty;

    public string fromZoneKey { get; set; } = string.Empty;

    public string toZoneKey { get; set; } = string.Empty;
}
