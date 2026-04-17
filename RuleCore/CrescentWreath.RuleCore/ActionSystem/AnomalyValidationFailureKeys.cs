namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyValidationFailureKeys
{
    public const string UnsupportedResolveConditionStep = "unsupportedResolveConditionStep";
    public const string UnsupportedResolveCondition = "unsupportedResolveCondition";

    public const string ActorPlayerStateMissing = "actorPlayerStateMissing";
    public const string ResolveManaCostMissing = "resolveManaCostMissing";
    public const string InsufficientMana = "insufficientMana";

    public const string ResolveFriendlyTeamHpCostPerPlayerMissing = "resolveFriendlyTeamHpCostPerPlayerMissing";
    public const string FriendlyTeamPlayerMissing = "friendlyTeamPlayerMissing";
    public const string FriendlyPlayerStateMissing = "friendlyPlayerStateMissing";
    public const string ActiveCharacterMissing = "activeCharacterMissing";
    public const string InsufficientFriendlyHp = "insufficientFriendlyHp";

    public const string TargetPlayerRequired = "targetPlayerRequired";
    public const string TargetPlayerMissing = "targetPlayerMissing";
    public const string TargetPlayerMustBeOpponent = "targetPlayerMustBeOpponent";

    public const string RewardStatusKeyMissing = "rewardStatusKeyMissing";
    public const string InsufficientFriendlyHandCards = "insufficientFriendlyHandCards";
    public const string AnomalyConditionInputRequired = "anomalyConditionInputRequired";
    public const string RewardSourceZoneUnsupported = "rewardSourceZoneUnsupported";
    public const string RewardTargetZoneUnsupported = "rewardTargetZoneUnsupported";
    public const string RewardSourceZoneMissing = "rewardSourceZoneMissing";
    public const string RewardTargetZoneMissing = "rewardTargetZoneMissing";
    public const string RewardSourceCardMissing = "rewardSourceCardMissing";
    public const string AnomalyRewardInputRequired = "anomalyRewardInputRequired";
    public const string UnsupportedResolveReward = "unsupportedResolveReward";
}
