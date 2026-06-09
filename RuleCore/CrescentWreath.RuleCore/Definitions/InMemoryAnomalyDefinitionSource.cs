using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class InMemoryAnomalyDefinitionSource : IAnomalyDefinitionSource
{
    private static readonly IReadOnlyList<AnomalyDefinition> AnomalyDefinitions = new List<AnomalyDefinition>
    {
        createA001SampleDefinition(),
        createA002SampleDefinition(),
        createA003SampleDefinition(),
        createA004Definition(),
        createA005SampleDefinition(),
        createA006SampleDefinition(),
        createA007SampleDefinition(),
        createA008SampleDefinition(),
        createA009SampleDefinition(),
        createA010Definition(),
    };

    public IReadOnlyList<AnomalyDefinition> getAnomalyDefinitions()
    {
        return AnomalyDefinitions;
    }

    private static AnomalyDefinition createMinimalAnomalyDefinition(
        string definitionId,
        string name,
        string arrivalText,
        string resolveText,
        string sourceHeaderRaw)
    {
        return new AnomalyDefinition
        {
            definitionId = definitionId,
            name = name,
            oncePerTurnHint = "oncePerTurn",
            arrivalText = arrivalText,
            resolveText = resolveText,
            sourceHeaderRaw = sourceHeaderRaw,
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
            name = "红雾异变",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】人外角色抓1张牌，人类角色弃1张牌，蕾米莉雅还可以回复所有生命",
            resolveText = "【8魔力，每个友方玩家生命减少1；对手队伍有蕾米莉雅时，需要额外支付②】扣除对手1点击杀分；你可以禁锢对手队伍中的蕾米莉雅和另一个人外角色。",
            sourceHeaderRaw = "红雾异变：【降临】人外角色抓1张牌，人类角色弃1张牌，蕾米莉雅还可以回复所有生命",
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
            name = "花开异变",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】当前回合玩家选择一名对手【禁锢】；拥有风见幽香的队伍获得1灵脉",
            resolveText = "【8魔力，【禁锢】你自己；风见幽香不需要禁锢自己】扣除对手1点击杀分；获得2灵脉。",
            sourceHeaderRaw = "花开异变：【降临】当前回合玩家选择一名对手【禁锢】；拥有风见幽香的队伍获得1灵脉",
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

    private static AnomalyDefinition createA010Definition()
    {
        var definition = createMinimalAnomalyDefinition(
            "A010",
            "命运长夜",
            "【降临】命运长夜被解决前，异变卡组不会再因任何效果发生变动。每位玩家将手牌、弃牌堆、阵地区或间隙区的一张牌面朝下盖放在自己的角色卡下方；当任意玩家击杀对手时，该玩家队伍选择一名友方玩家将自己盖放的牌放逐；此异变被解决时，放逐所有以此法盖放的牌",
            "【先将盖放在角色卡下的牌全部放逐的一方解决此异变】 选择2项：获得3灵脉；己方击杀分+1；将召唤区一张牌直接置于手中",
            "命运长夜：【降临】命运长夜被解决前，异变卡组不会再因任何效果发生变动。每位玩家将手牌、弃牌堆、阵地区或间隙区的一张牌面朝下盖放在自己的角色卡下方；当任意玩家击杀对手时，该玩家队伍选择一名友方玩家将自己盖放的牌放逐；此异变被解决时，放逐所有以此法盖放的牌");
        definition.arrivalSteps.Add(new AnomalyArrivalStepDefinition
        {
            arrivalStepKey = "applyA010FateStayNightSetAsideInput",
        });
        return definition;
    }

    private static AnomalyDefinition createA004Definition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A004",
            name = "永夜异变",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】每位玩家将自己阵地区一张防御牌拿回手中。然后辉夜可以将自己的手牌抓满至6张。",
            resolveText = "【8魔力，每个友方玩家弃掉1张含技能值的手牌】扣除对手1点击杀分。本回合结束后，你立刻开始一个额外的回合。",
            sourceHeaderRaw = "永夜异变：【降临】每位玩家将自己阵地区一张防御牌拿回手中。然后辉夜可以将自己的手牌抓满至6张。",
            resolveConditionKey = "actorManaAtLeastCost",
            resolveRewardKey = "teamDeltaReward",
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = null,
            rewardActorTeamLeylineDelta = 0,
            rewardOpponentTeamKillScoreDelta = -1,
            rewardStatusKey = string.Empty,
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "a004ArrivalReturnDefenseCardsThenKaguyaDraw",
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

    private static AnomalyDefinition createA002SampleDefinition()
    {
        return new AnomalyDefinition
        {
            definitionId = "A002",
            name = "春雪异变",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】每位玩家可以直接召唤一张樱花饼；幽幽子可以改为选择从召唤区直接召唤一张费用不高于5的宝具卡。",
            resolveText = "【8魔力，每个友方玩家弃1张牌；友方队伍中魂魄妖梦不需要因此弃牌】扣除对手1点击杀分；你可以将手牌或弃牌堆中最多2张任意卡放逐，并可以召唤等量樱花饼卡替代",
            sourceHeaderRaw = "春雪异变：【降临】每位玩家可以直接召唤一张樱花饼；幽幽子可以改为选择从召唤区直接召唤一张费用不高于5的宝具卡。",
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
                    arrivalStepKey = "a002ArrivalParallelDirectSummonChoice",
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
            name = "噩梦倒影",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】所有玩家从手牌中放逐1张牌，莲可以从弃牌堆中再放逐1张",
            resolveText = "【8魔力，每位对手可以抓1张牌】扣除对手1点击杀分。魅惑目标对手。",
            sourceHeaderRaw = "噩梦倒影：【降临】所有玩家从手牌中放逐1张牌，莲可以从弃牌堆中再放逐1张",
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
            name = "温泉异变",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】当前玩家将召唤区一张卡直接无消耗召唤",
            resolveText = "【8魔力，每位友方玩家将2张手牌如防御牌般放在阵地区】对手击杀分-1。将召唤区任意召唤费用7以下的宝具牌置入手中。",
            sourceHeaderRaw = "温泉异变：【降临】当前玩家将召唤区一张卡直接无消耗召唤",
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
            name = "瓦拉齐亚之夜",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】所有人类玩家将1张防御牌置入弃牌堆，人外玩家抓1张牌",
            resolveText = "【8魔力，对手的TM角色可以直接启动】对手击杀分-1。对手的人类玩家弃一张牌，你队伍中的人外玩家回复所有生命",
            sourceHeaderRaw = "瓦拉齐亚之夜：【降临】所有人类玩家将1张防御牌置入弃牌堆，人外玩家抓1张牌",
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
            name = "矛盾螺旋",
            oncePerTurnHint = "oncePerTurn",
            arrivalText = "【降临】封印击杀分多的一方所有玩家，两仪式不会被封印",
            resolveText = "【8魔力，对手可以从其弃牌堆中将一张牌置入手牌】对手击杀分-1， 禁锢目标对手；你队伍中的两仪式可以抓1张牌",
            sourceHeaderRaw = "矛盾螺旋：【降临】封印击杀分多的一方所有玩家，两仪式不会被封印",
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
            arrivalText = "【降临】放逐当前召唤区所有宝具卡",
            resolveText = "【8魔力，每位对手可以获得【结界】并召唤1张樱花饼】扣除对手1点击杀分；从间隙中召唤一张牌到手中",
            sourceHeaderRaw = "666：【降临】放逐当前召唤区所有宝具卡",
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
