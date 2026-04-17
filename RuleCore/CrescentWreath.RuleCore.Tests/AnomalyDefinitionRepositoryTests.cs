using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.Tests;

public class AnomalyDefinitionRepositoryTests
{
    [Fact]
    public void InMemoryAnomalyDefinitionSource_ShouldContainFixedTenAnomalies()
    {
        var source = new InMemoryAnomalyDefinitionSource();
        var definitions = source.getAnomalyDefinitions();

        Assert.Equal(10, definitions.Count);
        for (var i = 1; i <= 10; i++)
        {
            Assert.Contains(definitions, definition => definition.definitionId == $"A{i:000}");
        }
    }

    [Fact]
    public void GetInitialDeckDefinitionIds_ShouldReturnA001ToA010InOrder()
    {
        var definitionIds = AnomalyDefinitionRepository.getInitialDeckDefinitionIds();

        Assert.Equal(10, definitionIds.Count);
        for (var i = 1; i <= 10; i++)
        {
            Assert.Equal($"A{i:000}", definitionIds[i - 1]);
        }
    }

    [Fact]
    public void ResolveByDefinitionId_WhenUnknown_ShouldReturnDefaultShape()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A999");

        Assert.Equal("A999", definition.definitionId);
        Assert.Equal(string.Empty, definition.name);
        Assert.Equal(string.Empty, definition.arrivalText);
        Assert.Equal(string.Empty, definition.resolveText);
        Assert.Equal(string.Empty, definition.resolveConditionKey);
        Assert.Equal(string.Empty, definition.resolveRewardKey);
        Assert.Null(definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(0, definition.rewardActorTeamLeylineDelta);
        Assert.Equal(0, definition.rewardOpponentTeamKillScoreDelta);
        Assert.Equal(string.Empty, definition.rewardStatusKey);
        Assert.Empty(definition.arrivalSteps);
        Assert.Empty(definition.conditionSteps);
        Assert.Empty(definition.rewardSteps);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA002_ShouldExposeActorManaConditionSample()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A002");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("none", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Single(definition.arrivalSteps);
        Assert.Equal("legacyNoop", definition.arrivalSteps[0].arrivalStepKey);
        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);
        Assert.Single(definition.rewardSteps);
        Assert.Equal("teamDelta", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal(-1, definition.rewardSteps[0].opponentTeamKillScoreDelta);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA001_ShouldExposeCompositeCostSampleFields()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A001");

        Assert.Equal("actorManaAndFriendlyTeamHpAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("teamDeltaReward", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Equal(1, definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(0, definition.rewardActorTeamLeylineDelta);
        Assert.Equal(-1, definition.rewardOpponentTeamKillScoreDelta);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Equal(2, definition.conditionSteps.Count);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);
        Assert.Equal("friendlyTeamActiveCharacterHpAboveCostPerPlayer", definition.conditionSteps[1].conditionStepKey);

        Assert.Single(definition.arrivalSteps);
        Assert.Equal("applyA001RaceFlowWithRemiliaHealInput", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Empty(definition.rewardSteps);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA003_ShouldExposeSampleRealConditionAndReward()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A003");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("teamDeltaReward", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(2, definition.rewardActorTeamLeylineDelta);
        Assert.Equal(-1, definition.rewardOpponentTeamKillScoreDelta);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);
        Assert.Single(definition.arrivalSteps);
        Assert.Equal("selectOpponentApplyShackleAndGrantYuukaTeamLeyline", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Empty(definition.rewardSteps);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA005_ShouldExposeTeamDeltaRewardStepForA005Fix()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A005");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("none", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(0, definition.rewardActorTeamLeylineDelta);
        Assert.Equal(0, definition.rewardOpponentTeamKillScoreDelta);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Single(definition.arrivalSteps);
        Assert.Equal("directSummonFromSummonZoneWithInput", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);

        Assert.Single(definition.rewardSteps);
        Assert.Equal("teamDelta", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal(-1, definition.rewardSteps[0].opponentTeamKillScoreDelta);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA007_ShouldExposeTargetedStatusRewardStepSample()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A007");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("applyStatusToTargetOpponent", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(0, definition.rewardActorTeamLeylineDelta);
        Assert.Equal(0, definition.rewardOpponentTeamKillScoreDelta);
        Assert.Equal("Charm", definition.rewardStatusKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);

        Assert.Single(definition.arrivalSteps);
        Assert.Equal("applyOptionalBanishForAllPlayersAndExtraOptionalForC007", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Single(definition.rewardSteps);
        Assert.Equal("applyStatusToTargetOpponent", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal("Charm", definition.rewardSteps[0].statusKey);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA006_ShouldExposeDualRewardStepWithNonHumanHealSample()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A006");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("none", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);

        Assert.Single(definition.arrivalSteps);
        Assert.Equal("applyA006RaceFlowWithDefenseDiscardInput", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Equal(2, definition.rewardSteps.Count);
        Assert.Equal("teamDelta", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal(-1, definition.rewardSteps[0].opponentTeamKillScoreDelta);
        Assert.Equal("healFriendlyNonHumanActiveCharactersToMaxHp", definition.rewardSteps[1].rewardStepKey);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA008_ShouldExposeDualRewardStepSample()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A008");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("none", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);

        Assert.Single(definition.arrivalSteps);
        Assert.Equal("applySealToLeadingTeamActiveCharacters", definition.arrivalSteps[0].arrivalStepKey);

        Assert.Equal(2, definition.rewardSteps.Count);
        Assert.Equal("teamDelta", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal(-1, definition.rewardSteps[0].opponentTeamKillScoreDelta);
        Assert.Equal("applyStatusToTargetOpponent", definition.rewardSteps[1].rewardStepKey);
        Assert.Equal("Shackle", definition.rewardSteps[1].statusKey);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenKnownA009_ShouldExposeTeamDeltaRewardStepForA009Sample()
    {
        var definition = AnomalyDefinitionRepository.resolveByDefinitionId("A009");

        Assert.Equal("actorManaAtLeastCost", definition.resolveConditionKey);
        Assert.Equal("none", definition.resolveRewardKey);
        Assert.Equal(8, definition.resolveManaCost);
        Assert.Null(definition.resolveFriendlyTeamHpCostPerPlayer);
        Assert.Equal(string.Empty, definition.rewardStatusKey);

        Assert.Single(definition.conditionSteps);
        Assert.Equal("actorManaAtLeast", definition.conditionSteps[0].conditionStepKey);
        Assert.Single(definition.arrivalSteps);
        Assert.Equal("banishSummonZoneToGap", definition.arrivalSteps[0].arrivalStepKey);
        Assert.Single(definition.rewardSteps);
        Assert.Equal("teamDelta", definition.rewardSteps[0].rewardStepKey);
        Assert.Equal(-1, definition.rewardSteps[0].opponentTeamKillScoreDelta);
    }
}
