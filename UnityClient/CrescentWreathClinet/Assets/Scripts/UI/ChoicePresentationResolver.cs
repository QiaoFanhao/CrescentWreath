using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;

namespace CrescentWreath.Client.UI
{
public sealed class ChoicePresentationViewModel
{
    public string choiceKey = string.Empty;
    public string title = string.Empty;
    public string detail = string.Empty;
    public string categoryKey = string.Empty;
    public bool isDeclineChoice;
}

public static class ChoicePresentationResolver
{
    public static ChoicePresentationViewModel Resolve(ProjectionViewModel projection, string choiceKey)
    {
        var normalized = choiceKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return create(normalized, "（空选项）", string.Empty, "unknown", false);
        }

        if (string.Equals(normalized, "banish1", StringComparison.Ordinal))
        {
            return create(normalized, "放逐1张牌", "春雪异变解决奖励", "anomalyReward", false);
        }

        if (string.Equals(normalized, "banish2", StringComparison.Ordinal))
        {
            return create(normalized, "放逐2张牌", "春雪异变解决奖励；之后可以选择是否召唤等量樱花饼", "anomalyReward", false);
        }

        if (string.Equals(normalized, "c001Barrier:decline", StringComparison.Ordinal))
        {
            return create(normalized, "不发动博丽大结界", "不弃牌，不给予结界", "decline", true);
        }

        if (string.Equals(normalized, "moveToHand:accept", StringComparison.Ordinal))
        {
            return create(normalized, "是，加入手牌", "将刚放到牌库顶的牌加入手牌", "accept", false);
        }

        if (string.Equals(normalized, "moveToHand:decline", StringComparison.Ordinal))
        {
            return create(normalized, "否，留在牌库顶", "不将该牌加入手牌", "decline", true);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "character:C003:2", "gungnirDiscard") &&
            isDeclineChoice(normalized))
        {
            return create(normalized, "不赋予穿透", "不弃牌，本次伤害不获得【穿透】", "decline", true);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A002:rewardOptionalBanishAndSakuraReplacement") &&
            isAcceptChoice(normalized))
        {
            return create(normalized, "是，召唤等量樱花饼", "按【春雪异变】解决奖励召唤樱花饼", "accept", false);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A002:rewardOptionalBanishAndSakuraReplacement") &&
            isDeclineChoice(normalized))
        {
            return create(normalized, "否，不召唤樱花饼", "结束【春雪异变】解决奖励的可选召唤", "decline", true);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A007:conditionOpponentOptionalDraw") &&
            isAcceptChoice(normalized))
        {
            return create(normalized, "抽1张牌", "因【噩梦倒影】解决条件选择抽牌", "draw", false);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A007:conditionOpponentOptionalDraw") &&
            isDeclineChoice(normalized))
        {
            return create(normalized, "不抽牌", "不因【噩梦倒影】抽牌", "decline", true);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A009:conditionOpponentOptionalBenefit") &&
            isAcceptChoice(normalized))
        {
            return create(normalized, "是", "获得【结界】并召唤【樱花饼】", "accept", false);
        }

        if (containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "anomaly:A009:conditionOpponentOptionalBenefit") &&
            isDeclineChoice(normalized))
        {
            return create(normalized, "否", "不获得结界，也不召唤樱花饼", "decline", true);
        }

        if (isAcceptChoice(normalized))
        {
            return create(normalized, localizeAccept(normalized), normalized, "accept", false);
        }

        if (isDeclineChoice(normalized))
        {
            return create(normalized, localizeDecline(normalized), normalized, "decline", true);
        }

        if (string.Equals(normalized, "c005SixRealms:use", StringComparison.Ordinal))
        {
            return create(normalized, "发动六根清净斩", "移除 1 个剑气，展示牌库顶牌并反击", "skill", false);
        }

        var separatorIndex = normalized.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= normalized.Length - 1)
        {
            return create(normalized, normalized, "未识别选项", "unknown", false);
        }

        var prefix = normalized[..separatorIndex];
        var value = normalized[(separatorIndex + 1)..];
        return prefix switch
        {
            "player" => resolvePlayer(projection, normalized, value, "玩家"),
            "opponentPlayer" => resolvePlayer(projection, normalized, value, "对手"),
            "character" => resolveCharacter(projection, normalized, value),
            "handCard" => containsAny(projection.interaction.inputTypeKey, projection.interaction.contextKey, "character:C003:2", "gungnirDiscard")
                ? resolveCardWithOverrideDetail(projection, normalized, value, "弃置此牌", "使本次伤害获得【穿透】")
                : resolveCard(projection, normalized, value, "手牌"),
            "barrierCard" => resolveCard(projection, normalized, value, "博丽大结界费用"),
            "defenseCard" => resolveCard(projection, normalized, value, "防御放置牌"),
            "discardCard" => resolveCard(projection, normalized, value, "弃牌区"),
            "banishCard" => resolveCard(projection, normalized, value, "可放逐牌"),
            "banishDiscardCard" => resolveCard(projection, normalized, value, "弃牌区"),
            "banishSummonZoneCard" => resolveCard(projection, normalized, value, "召唤区"),
            "fieldCard" => resolveCard(projection, normalized, value, "阵地区"),
            "summonZoneCard" => resolveCard(projection, normalized, value, "召唤区"),
            "summonCard" => resolveCard(projection, normalized, value, "召唤区"),
            "gapCard" => resolveCard(projection, normalized, value, "间隙区"),
            "overlayCard" => resolveCard(projection, normalized, value, "叠放候选"),
            "setAsideCard" => resolveCard(projection, normalized, value, "盖放候选"),
            "friendlySetAside" => resolveFriendlySetAsideCard(projection, normalized, value),
            "markerGrant" => resolveMarkerGrant(projection, normalized, value),
            "declareCardName" => resolveDeclareCardName(normalized, value),
            "damageType" => resolveDamageType(normalized, value),
            "draw" => create(normalized, localizeDrawChoice(value), "抽牌选择", "draw", false),
            "c014AndThen" => resolveC014AndThenChoice(normalized, value),
            "c022DefenseCard" => resolveCard(projection, normalized, value, "对手防御牌"),
            "c022DollWar" => resolveC022DollWarChoice(normalized, value),
            "destruction" => resolveDestructionChoice(normalized, value),
            "doll" => resolveDollChoice(normalized, value),
            "leyline" => resolveLeylineChoice(normalized, value),
            "discard" => resolveDiscardOperation(projection, normalized, value),
            "banish" => resolveBanishOperation(projection, normalized, value),
            "banishDiscard" => resolveBanishDiscardOperation(projection, normalized, value),
            "banishSummonZone" => resolveBanishSummonZoneOperation(projection, normalized, value),
            "giveToAlly" => create(normalized, "交给队友", "将【梦魇的铃铛】交给友方玩家", "treasure", false),
            "allyDiscardForMana" => create(normalized, "队友弃牌获得魔力", "友方玩家弃1张牌并获得魔力", "treasure", false),
            "sakuraCake" => resolveSakuraCakeOperation(normalized, value),
            "summon" => resolveSummonOperation(projection, normalized, value),
            "reward" => resolveRewardChoice(normalized, value),
            _ => create(normalized, normalized, $"未识别前缀：{prefix}", "unknown", false),
        };
    }

    public static bool IsMultiSelectInput(ProjectionInteractionViewModel interaction)
    {
        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C005:3:targets"))
        {
            return true;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C014:2:discardCards"))
        {
            return true;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C022:4:targetOptions", "character:C022:4:targetDiscard"))
        {
            return true;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey,
                "arrivalSetAside",
                "selectGapTreasureToHand",
                "arrivalDirectSummonFromSummonZone",
                "selectSummonCardToHand",
                "targetOpponent",
                "targetPlayer",
                "damageType"))
        {
            return false;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "turnStartC013MechanicalJadeChoice", "turnStart:c013MechanicalJade"))
        {
            return true;
        }

        return containsAny(
            interaction.inputTypeKey,
            interaction.contextKey,
            "turnStartShackleDiscard",
            "shackleDiscard",
            "OverlayCardsChoice",
            "overlayCards",
            "extraDiscard",
            "OptionalShackle",
            "rewardOptionalShackleOpponents",
            "DefenseLikePlace",
            "conditionDefenseLikePlace",
            "rewardChooseTwo",
            "ChooseTwo",
            "multi",
            "Multiple",
            "T021",
            "T025");
    }

    public static int GetSuggestedMaxSelectionCount(ProjectionInteractionViewModel interaction)
    {
        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C005:3:targets"))
        {
            return 2;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "rewardOptionalShackleOpponents"))
        {
            return 2;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "T025", "extraDiscard"))
        {
            return int.MaxValue;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C014:2:discardCards"))
        {
            return int.MaxValue;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C022:4:targetOptions", "character:C022:4:targetDiscard"))
        {
            return int.MaxValue;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "turnStartC013MechanicalJadeChoice", "turnStart:c013MechanicalJade"))
        {
            return 2;
        }

        if (containsAny(
                interaction.inputTypeKey,
                interaction.contextKey,
                "turnStartShackleDiscard",
                "shackleDiscard",
                "SelectDiscardCards"))
        {
            return 4;
        }

        if (containsAny(
                interaction.inputTypeKey,
                interaction.contextKey,
                "T021",
                "ChooseTwo",
                "DefenseLikePlace",
                "conditionDefenseLikePlace"))
        {
            return 2;
        }

        return IsMultiSelectInput(interaction) ? int.MaxValue : 1;
    }

    public static int GetSuggestedMinSelectionCount(ProjectionInteractionViewModel interaction)
    {
        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C005:3:targets"))
        {
            return 1;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "turnStartC013MechanicalJadeChoice", "turnStart:c013MechanicalJade"))
        {
            return 2;
        }

        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "character:C022:4:targetOptions", "character:C022:4:targetDiscard"))
        {
            return 1;
        }

        return 1;
    }

    public static bool IsDeclineChoice(string choiceKey)
    {
        return isDeclineChoice(choiceKey);
    }

    private static ChoicePresentationViewModel resolvePlayer(
        ProjectionViewModel projection,
        string choiceKey,
        string value,
        string label)
    {
        if (!long.TryParse(value, out var playerId))
        {
            return create(choiceKey, choiceKey, "玩家 ID 无效", "player", false);
        }

        var player = projection.playerSummaries.FirstOrDefault(summary => summary.playerNumericId == playerId);
        if (player is null)
        {
            return create(choiceKey, $"{label} {playerId}", "未在玩家摘要中找到", "player", false);
        }

        var character = string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId)
            ? "未选择角色"
            : CardNameLocalizer.Localize(player.activeCharacterDefinitionId);
        var hp = player.activeCharacterCurrentHp.HasValue && player.activeCharacterMaxHp.HasValue
            ? $"HP {player.activeCharacterCurrentHp}/{player.activeCharacterMaxHp}"
            : "HP ?";
        return create(choiceKey, $"{character}（P{playerId}）", $"{label} / {hp}", "player", false);
    }

    private static ChoicePresentationViewModel resolveCharacter(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (!long.TryParse(value, out var characterId))
        {
            return create(choiceKey, choiceKey, "角色 ID 无效", "character", false);
        }

        var player = projection.playerSummaries.FirstOrDefault(summary => summary.activeCharacterInstanceNumericId == characterId);
        if (player is null)
        {
            return create(choiceKey, $"角色 #{characterId}", "未在公开角色摘要中找到", "character", false);
        }

        var characterName = string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId)
            ? $"角色 #{characterId}"
            : CardNameLocalizer.Localize(player.activeCharacterDefinitionId);
        return create(choiceKey, characterName, $"玩家 P{player.playerNumericId}", "character", false);
    }

    private static ChoicePresentationViewModel resolveLeylineChoice(string choiceKey, string value)
    {
        if (!int.TryParse(value, out var amount) || amount < 0)
        {
            return create(choiceKey, choiceKey, "灵脉数量无效", "leyline", false);
        }

        return create(choiceKey, $"X={amount}", $"消耗 {amount} 点灵脉", "leyline", false);
    }

    private static ChoicePresentationViewModel resolveCard(
        ProjectionViewModel projection,
        string choiceKey,
        string value,
        string fallbackZoneLabel)
    {
        if (!long.TryParse(value, out var cardId))
        {
            return create(choiceKey, choiceKey, "卡牌 ID 无效", "card", false);
        }

        var card = findCard(projection, cardId);
        if (card is null)
        {
            return create(choiceKey, fallbackZoneLabel, "此卡当前不在本客户端可见投影中", "card", false);
        }

        var zone = string.IsNullOrWhiteSpace(card.zoneKey) ? fallbackZoneLabel : localizeZone(card.zoneKey, fallbackZoneLabel);
        return create(choiceKey, CardNameLocalizer.Localize(card.definitionId), zone, "card", false);
    }

    private static ChoicePresentationViewModel resolveCardWithOverrideDetail(
        ProjectionViewModel projection,
        string choiceKey,
        string value,
        string titlePrefix,
        string detail)
    {
        if (!long.TryParse(value, out var cardId))
        {
            return create(choiceKey, choiceKey, "卡牌 ID 无效", "card", false);
        }

        var card = findCard(projection, cardId);
        if (card is null)
        {
            return create(choiceKey, titlePrefix, "此卡当前不在本客户端可见投影中", "card", false);
        }

        return create(choiceKey, $"{titlePrefix}【{CardNameLocalizer.Localize(card.definitionId)}】", detail, "card", false);
    }

    private static ChoicePresentationViewModel resolveMarkerGrant(ProjectionViewModel projection, string choiceKey, string value)
    {
        var parts = value.Split(':');
        if (parts.Length < 2 || !long.TryParse(parts[0], out var playerId))
        {
            return create(choiceKey, choiceKey, "指示物选项无效", "marker", false);
        }

        var markerKey = parts[1];
        var player = projection.playerSummaries.FirstOrDefault(summary => summary.playerNumericId == playerId);
        var character = player is null || string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId)
            ? $"P{playerId}"
            : $"{CardNameLocalizer.Localize(player.activeCharacterDefinitionId)}（P{playerId}）";
        return create(choiceKey, $"给予{character}：{localizeMarker(markerKey)}", "指示物选择", "marker", false);
    }

    private static ChoicePresentationViewModel resolveFriendlySetAsideCard(
        ProjectionViewModel projection,
        string choiceKey,
        string value)
    {
        var parts = value.Split(':');
        if (parts.Length < 2 || !long.TryParse(parts[0], out var playerId) || !long.TryParse(parts[1], out var cardId))
        {
            return create(choiceKey, choiceKey, "友方盖放牌选项无效", "card", false);
        }

        var player = projection.playerSummaries.FirstOrDefault(summary => summary.playerNumericId == playerId);
        var playerName = player is null || string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId)
            ? $"P{playerId}"
            : $"{CardNameLocalizer.Localize(player.activeCharacterDefinitionId)}（P{playerId}）";
        var card = findCard(projection, cardId);
        var cardName = card is null ? $"卡牌 #{cardId}" : CardNameLocalizer.Localize(card.definitionId);
        return create(choiceKey, $"放逐【{cardName}】", $"{playerName} 下方盖放牌", "card", false);
    }

    private static ChoicePresentationViewModel resolveDeclareCardName(string choiceKey, string value)
    {
        return create(choiceKey, $"宣言：{CardNameLocalizer.Localize(value)}", "卡名宣言", "declareCardName", false);
    }

    private static ChoicePresentationViewModel resolveDamageType(string choiceKey, string value)
    {
        var title = value switch
        {
            "physical" => "体术伤害",
            "spell" => "咒术伤害",
            "direct" => "直接伤害",
            _ => value,
        };
        return create(choiceKey, title, "选择伤害类型", "damageType", false);
    }

    private static ChoicePresentationViewModel resolveC014AndThenChoice(string choiceKey, string value)
    {
        if (value.StartsWith("x:", StringComparison.Ordinal) &&
            int.TryParse(value.Substring(2), out var amount))
        {
            return create(
                choiceKey,
                $"X={amount}",
                $"发动【之后就谁都不在了】：减少1生命并弃{amount}张牌，使本次伤害+{amount}",
                "skill",
                false);
        }

        return create(choiceKey, choiceKey, "芙兰朵露响应技选项", "skill", false);
    }

    private static ChoicePresentationViewModel resolveC022DollWarChoice(string choiceKey, string value)
    {
        return value switch
        {
            "aliceDraw" => create(choiceKey, "爱丽丝抓牌", "【人形战争】选项：爱丽丝抓X张牌", "skill", false),
            "targetDiscard" => create(choiceKey, "你弃牌", "【人形战争】选项：目标弃X张牌", "skill", false),
            "directDamage" => create(choiceKey, "受到直接伤害", "【人形战争】选项：目标受到X点直接伤害", "skill", false),
            "reduceLeyline" => create(choiceKey, "扣除灵脉", "【人形战争】选项：目标队伍扣除X点灵脉", "skill", false),
            _ => create(choiceKey, choiceKey, "爱丽丝【人形战争】选项", "skill", false),
        };
    }

    private static ChoicePresentationViewModel resolveDollChoice(string choiceKey, string value)
    {
        return int.TryParse(value, out var amount)
            ? create(choiceKey, $"X={amount}", $"移除{amount}个人形指示物", "marker", false)
            : create(choiceKey, choiceKey, "人形指示物选项", "marker", false);
    }

    private static ChoicePresentationViewModel resolveDestructionChoice(string choiceKey, string value)
    {
        return int.TryParse(value, out var amount)
            ? create(choiceKey, $"X={amount}", $"移除{amount}个毁灭指示物", "marker", false)
            : create(choiceKey, choiceKey, "毁灭指示物选项", "marker", false);
    }

    private static ChoicePresentationViewModel resolveDiscardOperation(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (string.Equals(value, "decline", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "不弃牌", "跳过此次可选弃牌", "decline", true);
        }

        if (string.Equals(value, "noCard", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "没有可弃置的手牌", "无需弃牌", "discard", false);
        }

        if (long.TryParse(value, out _))
        {
            return resolveCard(projection, choiceKey, value, "弃牌候选");
        }

        return create(choiceKey, choiceKey, "弃牌操作", "discard", false);
    }

    private static ChoicePresentationViewModel resolveBanishOperation(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (string.Equals(value, "decline", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "不放逐", "跳过此次可选放逐", "decline", true);
        }

        if (long.TryParse(value, out _))
        {
            return resolveCard(projection, choiceKey, value, "放逐候选");
        }

        return create(choiceKey, choiceKey, "放逐操作", "banish", false);
    }

    private static ChoicePresentationViewModel resolveBanishDiscardOperation(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (string.Equals(value, "decline", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "不放逐弃牌区牌", "跳过此次可选放逐", "decline", true);
        }

        if (long.TryParse(value, out _))
        {
            return resolveCard(projection, choiceKey, value, "弃牌区");
        }

        return create(choiceKey, choiceKey, "弃牌区放逐操作", "banish", false);
    }

    private static ChoicePresentationViewModel resolveBanishSummonZoneOperation(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (string.Equals(value, "decline", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "不放逐召唤区牌", "跳过此次可选放逐", "decline", true);
        }

        if (long.TryParse(value, out _))
        {
            return resolveCard(projection, choiceKey, value, "召唤区");
        }

        return create(choiceKey, choiceKey, "召唤区放逐操作", "banish", false);
    }

    private static ChoicePresentationViewModel resolveSakuraCakeOperation(string choiceKey, string value)
    {
        return string.Equals(value, "accept", StringComparison.OrdinalIgnoreCase)
            ? create(choiceKey, "召唤樱花饼", "处理异变效果，召唤【樱花饼】", "sakuraCake", false)
            : create(choiceKey, "不召唤樱花饼", "跳过此次樱花饼召唤", "decline", true);
    }

    private static ChoicePresentationViewModel resolveSummonOperation(ProjectionViewModel projection, string choiceKey, string value)
    {
        if (string.Equals(value, "decline", StringComparison.OrdinalIgnoreCase))
        {
            return create(choiceKey, "不召唤", "跳过此次可选召唤", "decline", true);
        }

        if (long.TryParse(value, out _))
        {
            return resolveCard(projection, choiceKey, value, "召唤区");
        }

        return create(choiceKey, choiceKey, "召唤操作", "summon", false);
    }

    private static ChoicePresentationViewModel resolveRewardChoice(string choiceKey, string value)
    {
        return value switch
        {
            "leyline3" => create(choiceKey, "获得3灵脉", "【命运长夜】解决奖励选项", "anomalyReward", false),
            "killScorePlus1" => create(choiceKey, "回复1击杀分", "【命运长夜】解决奖励选项", "anomalyReward", false),
            "summonToHand" => create(choiceKey, "选择召唤区宝具加入手牌", "【命运长夜】解决奖励选项", "anomalyReward", false),
            _ => create(choiceKey, choiceKey, "异变奖励选项", "anomalyReward", false),
        };
    }

    private static ProjectionCardViewModel? findCard(ProjectionViewModel projection, long cardId)
    {
        IEnumerable<ProjectionCardViewModel> allCards = projection.handCards
            .Concat(projection.discardCards)
            .Concat(projection.fieldCards)
            .Concat(projection.summonZoneCards)
            .Concat(projection.sakuraCakeCards)
            .Concat(projection.gapZoneCards)
            .Concat(projection.playerSummaries.SelectMany(player => player.fieldCards))
            .Concat(projection.playerSummaries.SelectMany(player => player.discardCards));
        return allCards.FirstOrDefault(card => card.cardInstanceNumericId == cardId);
    }

    private static ChoicePresentationViewModel create(
        string choiceKey,
        string title,
        string detail,
        string categoryKey,
        bool isDeclineChoice)
    {
        return new ChoicePresentationViewModel
        {
            choiceKey = choiceKey,
            title = title,
            detail = detail,
            categoryKey = categoryKey,
            isDeclineChoice = isDeclineChoice,
        };
    }

    private static bool isAcceptChoice(string choiceKey)
    {
        return string.Equals(choiceKey, "accept", StringComparison.Ordinal) ||
               choiceKey.EndsWith(":accept", StringComparison.Ordinal);
    }

    private static bool isDeclineChoice(string choiceKey)
    {
        return choiceKey.EndsWith(":decline", StringComparison.Ordinal) ||
               string.Equals(choiceKey, "decline", StringComparison.Ordinal) ||
               string.Equals(choiceKey, "skip", StringComparison.Ordinal) ||
               string.Equals(choiceKey, "none", StringComparison.Ordinal);
    }

    private static string localizeAccept(string choiceKey)
    {
        if (choiceKey.Contains("draw", StringComparison.OrdinalIgnoreCase))
        {
            return "抽1张牌";
        }

        if (choiceKey.Contains("sakura", StringComparison.OrdinalIgnoreCase))
        {
            return "召唤樱花饼";
        }

        return "是 / 处理";
    }

    private static string localizeDecline(string choiceKey)
    {
        if (choiceKey.Contains("discard", StringComparison.OrdinalIgnoreCase))
        {
            return "不弃牌 / 跳过";
        }

        if (choiceKey.Contains("draw", StringComparison.OrdinalIgnoreCase))
        {
            return "不抽牌";
        }

        return "否 / 不处理";
    }

    private static string localizeDrawChoice(string value)
    {
        return string.Equals(value, "accept", StringComparison.Ordinal)
            ? "抽1张牌"
            : "不抽牌";
    }

    private static string localizeZone(string zoneKey, string fallback)
    {
        if (zoneKey.Contains("hand", StringComparison.OrdinalIgnoreCase))
        {
            return "手牌";
        }

        if (zoneKey.Contains("discard", StringComparison.OrdinalIgnoreCase))
        {
            return "弃牌区";
        }

        if (zoneKey.Contains("field", StringComparison.OrdinalIgnoreCase))
        {
            return "阵地区";
        }

        if (zoneKey.Contains("summon", StringComparison.OrdinalIgnoreCase))
        {
            return "召唤区";
        }

        if (zoneKey.Contains("gap", StringComparison.OrdinalIgnoreCase))
        {
            return "间隙区";
        }

        return fallback;
    }

    private static string localizeMarker(string markerKey)
    {
        return markerKey switch
        {
            "swordAura" => "剑气",
            "dream" => "梦境",
            "destruction" => "毁灭",
            "metal" => "金",
            "wood" => "木",
            "water" => "水",
            "fire" => "火",
            "earth" => "土",
            "doll" => "人形",
            "divinity" => "神灵",
            "jewel" => "宝石",
            _ => markerKey,
        };
    }

    private static bool containsAny(string inputTypeKey, string contextKey, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if ((!string.IsNullOrWhiteSpace(inputTypeKey) && inputTypeKey.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(contextKey) && contextKey.Contains(needle, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
}
