using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class InMemoryAnomalyDefinitionSource : IAnomalyDefinitionSource
{
    private static readonly IReadOnlyList<AnomalyDefinition> AnomalyDefinitions = new List<AnomalyDefinition>
    {
        createA001SampleDefinition(),
        createA002SampleDefinition(),
        createA003SampleDefinition(),
        createMinimalAnomalyDefinition("A004", "Eternal Night"),
        createA005SampleDefinition(),
        createA006SampleDefinition(),
        createA007SampleDefinition(),
        createA008SampleDefinition(),
        createA009SampleDefinition(),
        createMinimalAnomalyDefinition("A010", "Fate Stay Night"),
    };

    public IReadOnlyList<AnomalyDefinition> getAnomalyDefinitions()
    {
        return AnomalyDefinitions;
    }

    private static AnomalyDefinition createMinimalAnomalyDefinition(string definitionId, string name)
    {
        return new AnomalyDefinition
        {
            definitionId = definitionId,
            name = name,
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "legacyAutoSuccess",
            resolveRewardKey = "none",
            resolveManaCost = null,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
        };
    }

    private static AnomalyDefinition createA001SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A001",
            name = "Crimson Mist",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAndFriendlyTeamHpAtLeastCost",
            resolveRewardKey = "teamDeltaReward",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = 1,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = -1,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applyA001RaceFlowWithRemiliaHealInput",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "friendlyTeamActiveCharacterHpAboveCostPerPlayer",
                },
            },
        };
    }

    private static AnomalyDefinition createA003SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A003",
            name = "Blooming Flowers",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "teamDeltaReward",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 2,
            rewardOpponentTeamKillScoreDelta = -1,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "selectOpponentApplyShackleAndGrantYuukaTeamLeyline",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
        };
    }

    private static AnomalyDefinition createA002SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A002",
            name = "Spring Snow",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "none",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "legacyNoop",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 0,
                    opponentTeamKillScoreDelta = -1,
                },
            },
        };
    }

    private static AnomalyDefinition createA007SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A007",
            name = "Nightmare Reflection",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "applyStatusToTargetOpponent",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = "Charm",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applyOptionalBanishForAllPlayersAndExtraOptionalForC007",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Charm",
                },
            },
        };
    }

    private static AnomalyDefinition createA005SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A005",
            name = "Hot Spring",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "none",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "directSummonFromSummonZoneWithInput",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 0,
                    opponentTeamKillScoreDelta = -1,
                },
            },
        };
    }

    private static AnomalyDefinition createA006SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A006",
            name = "Walachia Night",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "none",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applyA006RaceFlowWithDefenseDiscardInput",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 0,
                    opponentTeamKillScoreDelta = -1,
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "healFriendlyNonHumanActiveCharactersToMaxHp",
                },
            },
        };
    }

    private static AnomalyDefinition createA008SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A008",
            name = "Paradox Spiral",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "none",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 0,
                    opponentTeamKillScoreDelta = -1,
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Shackle",
                    isOptional = true,
                },
            },
        };
    }

    private static AnomalyDefinition createA009SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A009",
            name = "666",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = string.Empty,
            resolveText = string.Empty,
            sourceHeaderRaw = string.Empty,
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "none",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = 0,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "banishSummonZoneToGap",
                },
            },
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 0,
                    opponentTeamKillScoreDelta = -1,
                },
            },
        };
    }
}
