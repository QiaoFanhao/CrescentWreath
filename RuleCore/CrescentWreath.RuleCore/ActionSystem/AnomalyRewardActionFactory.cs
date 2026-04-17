using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyRewardActionFactory
{
    public const string RewardActionKeyTeamDelta = "teamDelta";
    public const string RewardActionKeyApplyStatusToTargetOpponent = "applyStatusToTargetOpponent";
    public const string RewardActionKeyHealFriendlyNonHumanActiveCharactersToMaxHp = "healFriendlyNonHumanActiveCharactersToMaxHp";
    public const string RewardActionKeyMoveCardFromZoneToZone = "moveCardFromZoneToZone";

    private const string RewardKeyNone = "none";
    private const string RewardKeyTeamDelta = "teamDeltaReward";
    private const string RewardKeyApplyStatusToTargetOpponent = "applyStatusToTargetOpponent";

    public static bool tryCreateActions(
        AnomalyDefinition anomalyDefinition,
        out List<AnomalyRewardAction> rewardActions,
        out string? failedReasonKey)
    {
        rewardActions = new List<AnomalyRewardAction>();

        if (anomalyDefinition.rewardSteps.Count > 0)
        {
            foreach (var rewardStep in anomalyDefinition.rewardSteps)
            {
                if (!tryCreateActionFromStep(rewardStep, out var rewardAction))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.UnsupportedResolveReward;
                    return false;
                }

                rewardActions.Add(rewardAction);
            }

            failedReasonKey = null;
            return true;
        }

        if (string.Equals(anomalyDefinition.resolveRewardKey, RewardKeyNone, StringComparison.Ordinal))
        {
            failedReasonKey = null;
            return true;
        }

        if (string.Equals(anomalyDefinition.resolveRewardKey, RewardKeyTeamDelta, StringComparison.Ordinal))
        {
            rewardActions.Add(new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyTeamDelta,
                isOptional = false,
                actorTeamLeylineDelta = anomalyDefinition.rewardActorTeamLeylineDelta,
                opponentTeamKillScoreDelta = anomalyDefinition.rewardOpponentTeamKillScoreDelta,
                statusKey = string.Empty,
            });
            failedReasonKey = null;
            return true;
        }

        if (string.Equals(anomalyDefinition.resolveRewardKey, RewardKeyApplyStatusToTargetOpponent, StringComparison.Ordinal))
        {
            rewardActions.Add(new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyApplyStatusToTargetOpponent,
                isOptional = false,
                actorTeamLeylineDelta = 0,
                opponentTeamKillScoreDelta = 0,
                statusKey = anomalyDefinition.rewardStatusKey,
            });
            failedReasonKey = null;
            return true;
        }

        failedReasonKey = AnomalyValidationFailureKeys.UnsupportedResolveReward;
        return false;
    }

    private static bool tryCreateActionFromStep(
        AnomalyRewardStepDefinition rewardStep,
        out AnomalyRewardAction rewardAction)
    {
        if (string.Equals(rewardStep.rewardStepKey, RewardActionKeyTeamDelta, StringComparison.Ordinal))
        {
            rewardAction = new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyTeamDelta,
                isOptional = rewardStep.isOptional,
                actorTeamLeylineDelta = rewardStep.actorTeamLeylineDelta,
                opponentTeamKillScoreDelta = rewardStep.opponentTeamKillScoreDelta,
                statusKey = string.Empty,
            };
            return true;
        }

        if (string.Equals(rewardStep.rewardStepKey, RewardActionKeyApplyStatusToTargetOpponent, StringComparison.Ordinal))
        {
            rewardAction = new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyApplyStatusToTargetOpponent,
                isOptional = rewardStep.isOptional,
                actorTeamLeylineDelta = 0,
                opponentTeamKillScoreDelta = 0,
                statusKey = rewardStep.statusKey,
            };
            return true;
        }

        if (string.Equals(rewardStep.rewardStepKey, RewardActionKeyHealFriendlyNonHumanActiveCharactersToMaxHp, StringComparison.Ordinal))
        {
            rewardAction = new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyHealFriendlyNonHumanActiveCharactersToMaxHp,
                isOptional = rewardStep.isOptional,
                actorTeamLeylineDelta = 0,
                opponentTeamKillScoreDelta = 0,
                statusKey = string.Empty,
            };
            return true;
        }

        if (string.Equals(rewardStep.rewardStepKey, RewardActionKeyMoveCardFromZoneToZone, StringComparison.Ordinal))
        {
            rewardAction = new AnomalyRewardAction
            {
                rewardActionKey = RewardActionKeyMoveCardFromZoneToZone,
                isOptional = rewardStep.isOptional,
                actorTeamLeylineDelta = 0,
                opponentTeamKillScoreDelta = 0,
                statusKey = string.Empty,
                fromZoneKey = rewardStep.fromZoneKey,
                toZoneKey = rewardStep.toZoneKey,
            };
            return true;
        }

        rewardAction = new AnomalyRewardAction();
        return false;
    }
}
