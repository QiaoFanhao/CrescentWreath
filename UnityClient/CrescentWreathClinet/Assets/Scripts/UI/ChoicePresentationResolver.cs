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
            return create(normalized, "(空选项)", string.Empty, "unknown", false);
        }

        if (isAcceptChoice(normalized))
        {
            return create(normalized, localizeAccept(normalized), normalized, "accept", false);
        }

        if (isDeclineChoice(normalized))
        {
            return create(normalized, localizeDecline(normalized), normalized, "decline", true);
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
            "handCard" => resolveCard(projection, normalized, value, "手牌"),
            "discardCard" => resolveCard(projection, normalized, value, "弃牌区"),
            "banishCard" => resolveCard(projection, normalized, value, "可放逐牌"),
            "banishDiscardCard" => resolveCard(projection, normalized, value, "弃牌区"),
            "banishSummonZoneCard" => resolveCard(projection, normalized, value, "召唤区"),
            "fieldCard" => resolveCard(projection, normalized, value, "阵地区"),
            "summonZoneCard" => resolveCard(projection, normalized, value, "召唤区"),
            "gapCard" => resolveCard(projection, normalized, value, "间隙区"),
            "overlayCard" => resolveCard(projection, normalized, value, "叠放候选"),
            "setAsideCard" => resolveCard(projection, normalized, value, "盖放候选"),
            "markerGrant" => resolveMarkerGrant(projection, normalized, value),
            "declareCardName" => resolveDeclareCardName(normalized, value),
            "draw" => create(normalized, localizeDrawChoice(value), "抽牌选择", "draw", false),
            _ => create(normalized, normalized, $"未识别前缀：{prefix}", "unknown", false),
        };
    }

    public static bool IsMultiSelectInput(ProjectionInteractionViewModel interaction)
    {
        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "arrivalSetAside"))
        {
            return false;
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
        if (containsAny(interaction.inputTypeKey, interaction.contextKey, "T025", "extraDiscard"))
        {
            return int.MaxValue;
        }

        if (containsAny(
                interaction.inputTypeKey,
                interaction.contextKey,
                "turnStartShackleDiscard",
                "shackleDiscard",
                "SelectDiscardCards",
                "turnStartShackleDiscard"))
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
            : player.activeCharacterDefinitionId;
        var hp = player.activeCharacterCurrentHp.HasValue && player.activeCharacterMaxHp.HasValue
            ? $"HP {player.activeCharacterCurrentHp}/{player.activeCharacterMaxHp}"
            : "HP ?";
        return create(choiceKey, $"{label} {playerId}", $"{character} / {hp}", "player", false);
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
            : player.activeCharacterDefinitionId;
        return create(choiceKey, characterName, $"玩家 {player.playerNumericId}", "character", false);
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
            return create(choiceKey, "未知卡牌", fallbackZoneLabel, "card", false);
        }

        var zone = string.IsNullOrWhiteSpace(card.zoneKey) ? fallbackZoneLabel : localizeZone(card.zoneKey, fallbackZoneLabel);
        return create(choiceKey, CardNameLocalizer.Localize(card.definitionId), zone, "card", false);
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
        return create(choiceKey, $"给予玩家 {playerId}：{localizeMarker(markerKey)}", player?.activeCharacterDefinitionId ?? string.Empty, "marker", false);
    }

    private static ChoicePresentationViewModel resolveDeclareCardName(string choiceKey, string value)
    {
        return create(choiceKey, $"宣言：{CardNameLocalizer.Localize(value)}", "卡名宣言", "declareCardName", false);
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
            return "抽 1 张牌";
        }

        if (choiceKey.Contains("sakura", StringComparison.OrdinalIgnoreCase))
        {
            return "召唤樱花饼";
        }

        return "处理";
    }

    private static string localizeDecline(string choiceKey)
    {
        if (choiceKey.Contains("banish", StringComparison.OrdinalIgnoreCase) ||
            choiceKey.Contains("gapCard", StringComparison.OrdinalIgnoreCase))
        {
            return "不处理";
        }

        if (choiceKey.Contains("discard", StringComparison.OrdinalIgnoreCase))
        {
            return "不弃牌 / 跳过";
        }

        if (choiceKey.Contains("draw", StringComparison.OrdinalIgnoreCase))
        {
            return "不抽牌";
        }

        return "跳过 / 不处理";
    }

    private static string localizeDrawChoice(string value)
    {
        return string.Equals(value, "accept", StringComparison.Ordinal)
            ? "抽 1 张牌"
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
