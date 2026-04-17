using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.Tests;

public class AnomalyRewardActionFactoryTests
{
    [Fact]
    public void TryCreateActions_WhenRewardStepsAreProvided_ShouldCreateActionsInSameOrder()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A006",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    isOptional = true,
                    actorTeamLeylineDelta = 1,
                    opponentTeamKillScoreDelta = -2,
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    isOptional = false,
                    statusKey = "Charm",
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "healFriendlyNonHumanActiveCharactersToMaxHp",
                },
            },
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.True(success);
        Assert.Null(failedReasonKey);
        Assert.Equal(3, rewardActions.Count);
        Assert.Equal("teamDelta", rewardActions[0].rewardActionKey);
        Assert.True(rewardActions[0].isOptional);
        Assert.Equal(1, rewardActions[0].actorTeamLeylineDelta);
        Assert.Equal(-2, rewardActions[0].opponentTeamKillScoreDelta);
        Assert.Equal("applyStatusToTargetOpponent", rewardActions[1].rewardActionKey);
        Assert.False(rewardActions[1].isOptional);
        Assert.Equal("Charm", rewardActions[1].statusKey);
        Assert.Equal("healFriendlyNonHumanActiveCharactersToMaxHp", rewardActions[2].rewardActionKey);
        Assert.False(rewardActions[2].isOptional);
    }

    [Fact]
    public void TryCreateActions_WhenRewardStepIsMoveCardFromZoneToZone_ShouldMapZoneKeys()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                },
            },
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.True(success);
        Assert.Null(failedReasonKey);
        Assert.Single(rewardActions);
        Assert.Equal("moveCardFromZoneToZone", rewardActions[0].rewardActionKey);
        Assert.Equal("discard", rewardActions[0].fromZoneKey);
        Assert.Equal("hand", rewardActions[0].toZoneKey);
    }

    [Fact]
    public void TryCreateActions_WhenLegacyResolveRewardKeyIsTeamDelta_ShouldMapToSingleAction()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A003",
            resolveRewardKey = "teamDeltaReward",
            rewardActorTeamLeylineDelta = 2,
            rewardOpponentTeamKillScoreDelta = -1,
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.True(success);
        Assert.Null(failedReasonKey);
        Assert.Single(rewardActions);
        Assert.Equal("teamDelta", rewardActions[0].rewardActionKey);
        Assert.False(rewardActions[0].isOptional);
        Assert.Equal(2, rewardActions[0].actorTeamLeylineDelta);
        Assert.Equal(-1, rewardActions[0].opponentTeamKillScoreDelta);
    }

    [Fact]
    public void TryCreateActions_WhenLegacyResolveRewardKeyIsApplyStatusToTargetOpponent_ShouldMapToSingleAction()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A007",
            resolveRewardKey = "applyStatusToTargetOpponent",
            rewardStatusKey = "Shackle",
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.True(success);
        Assert.Null(failedReasonKey);
        Assert.Single(rewardActions);
        Assert.Equal("applyStatusToTargetOpponent", rewardActions[0].rewardActionKey);
        Assert.False(rewardActions[0].isOptional);
        Assert.Equal("Shackle", rewardActions[0].statusKey);
    }

    [Fact]
    public void TryCreateActions_WhenResolveRewardKeyIsUnsupported_ShouldFailWithUnsupportedResolveReward()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A999",
            resolveRewardKey = "unsupportedRewardKey",
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.False(success);
        Assert.Empty(rewardActions);
        Assert.Equal("unsupportedResolveReward", failedReasonKey);
    }

    [Fact]
    public void TryCreateActions_WhenRewardStepsContainUnsupportedStep_ShouldFailWithUnsupportedResolveReward()
    {
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A999",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "unsupportedRewardStepKey",
                },
            },
        };

        var success = AnomalyRewardActionFactory.tryCreateActions(
            anomalyDefinition,
            out var rewardActions,
            out var failedReasonKey);

        Assert.False(success);
        Assert.Empty(rewardActions);
        Assert.Equal("unsupportedResolveReward", failedReasonKey);
    }
}
