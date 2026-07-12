using TMPro;
using CrescentWreath.Client.Net;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public enum BattlefieldSeatDirection
{
    bottom,
    left,
    top,
    right,
}

public sealed class BattlefieldPlayerSeatView : MonoBehaviour
{
    [SerializeField]
    private BattlefieldSeatDirection direction;

    [SerializeField]
    private RectTransform? characterPortraitPlaceholder;

    [SerializeField]
    private TMP_Text? playerNameText;

    [SerializeField]
    private TMP_Text? hpText;

    [SerializeField]
    private RectTransform? resourceBarPlaceholder;

    [SerializeField]
    private RectTransform? statusContainer;

    [SerializeField]
    private RectTransform? counterContainer;

    [SerializeField]
    private TMP_Text? handCountText;

    [SerializeField]
    private TMP_Text? deckCountText;

    [SerializeField]
    private TMP_Text? discardCountText;

    [SerializeField]
    private RectTransform? publicFieldContainer;

    public BattlefieldSeatDirection Direction => direction;
    public TMP_Text? PlayerNameText => playerNameText;
    public TMP_Text? HpText => hpText;
    public RectTransform? PublicFieldContainer => publicFieldContainer;

    private TMP_Text? resourceText;
    private TMP_Text? statusText;
    public void Configure(
        BattlefieldSeatDirection newDirection,
        RectTransform portrait,
        TMP_Text playerName,
        TMP_Text hp,
        RectTransform resourceBar,
        RectTransform statuses,
        RectTransform counters,
        TMP_Text handCount,
        TMP_Text deckCount,
        TMP_Text discardCount,
        RectTransform publicField)
    {
        direction = newDirection;
        characterPortraitPlaceholder = portrait;
        playerNameText = playerName;
        hpText = hp;
        resourceBarPlaceholder = resourceBar;
        statusContainer = statuses;
        counterContainer = counters;
        handCountText = handCount;
        deckCountText = deckCount;
        discardCountText = discardCount;
        publicFieldContainer = publicField;
    }

    public void Bind(
        ProjectionPlayerSummaryViewModel? player,
        ProjectionTeamSummaryViewModel? team,
        bool isCurrentPlayer,
        bool isViewerPlayer)
    {
        if (player is null)
        {
            setText(playerNameText, $"{direction}: (empty)");
            setText(hpText, "HP -");
            removeSeatHpCardVisual();
            setText(handCountText, "Hand -");
            setText(deckCountText, "Deck -");
            setText(discardCountText, "Discard -");
            setText(ensureResourceText(), "队伍 - / 魔力 - / 技能 - / 灵符 -");
            setText(ensureStatusText(), "状态：无");
            return;
        }

        var currentMarker = isCurrentPlayer ? "Current " : string.Empty;
        var viewerMarker = isViewerPlayer ? "You" : "P" + player.playerNumericId.ToString();
        setText(playerNameText, $"{currentMarker}{viewerMarker} / 队伍{player.teamNumericId}");
        setText(hpText, $"HP {formatNullableInt(player.activeCharacterCurrentHp)}/{formatNullableInt(player.activeCharacterMaxHp)}");
        removeSeatHpCardVisual();
        setText(handCountText, $"Hand {player.handCount}");
        setText(deckCountText, $"Deck {player.deckCount}");
        setText(discardCountText, $"Discard {player.discardCount}");
        var sigilValue = player.lockedSigil ?? player.sigilPreview;
        var teamText = team is null
            ? $"队伍{player.teamNumericId} 灵脉?"
            : $"队伍{team.teamNumericId} 灵脉{team.leyline} 击坠{team.killScore}";
        setText(
            ensureResourceText(),
            $"{teamText}\n魔力{player.mana} 技能{player.skillPoint} 灵符{sigilValue}");
        setText(ensureStatusText(), buildStatusText(player));
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        BattlefieldSeatDirection newDirection,
        RectTransform portrait,
        TMP_Text playerName,
        TMP_Text hp,
        RectTransform resourceBar,
        RectTransform statuses,
        RectTransform counters,
        TMP_Text handCount,
        TMP_Text deckCount,
        TMP_Text discardCount,
        RectTransform publicField)
    {
        Configure(
            newDirection,
            portrait,
            playerName,
            hp,
            resourceBar,
            statuses,
            counters,
            handCount,
            deckCount,
            discardCount,
            publicField);
    }
#endif

    private static void setText(TMP_Text? target, string value)
    {
        if (target is not null)
        {
            target.text = value;
        }
    }

    private static string formatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "-";
    }

    private void removeSeatHpCardVisual()
    {
        if (characterPortraitPlaceholder is null)
        {
            return;
        }

        removeChild(characterPortraitPlaceholder, "HpCardImage");
        removeChild(characterPortraitPlaceholder, "HpCardFallbackText");
    }

    private static void removeChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child is null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
        }
        else
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private TMP_Text? ensureResourceText()
    {
        if (resourceBarPlaceholder is null)
        {
            return null;
        }

        resourceText = ensureTextChild(
            resourceText,
            resourceBarPlaceholder,
            "ResourceText",
            11f,
            TextAlignmentOptions.Left,
            playerNameText?.font ?? hpText?.font);
        return resourceText;
    }

    private TMP_Text? ensureStatusText()
    {
        if (statusContainer is null)
        {
            return null;
        }

        statusText = ensureTextChild(
            statusText,
            statusContainer,
            "StatusText",
            10f,
            TextAlignmentOptions.Left,
            playerNameText?.font ?? hpText?.font);
        return statusText;
    }

    private static TMP_Text ensureTextChild(
        TMP_Text? cached,
        RectTransform parent,
        string name,
        float fontSize,
        TextAlignmentOptions alignment,
        TMP_FontAsset? font)
    {
        if (cached is not null)
        {
            return cached;
        }

        var existing = parent.Find(name);
        TMP_Text text;
        if (existing is not null)
        {
            text = existing.GetComponent<TMP_Text>();
            if (text is null)
            {
                text = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }
        else
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TMP_Text>();
        }

        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 1f);
        rect.offsetMax = new Vector2(-2f, -1f);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (font is not null)
        {
            text.font = font;
        }
        return text;
    }

    private static string buildStatusText(ProjectionPlayerSummaryViewModel player)
    {
        var playerStatuses = buildLocalizedStatusText(player.playerStatusKeys);
        var characterStatuses = buildLocalizedStatusText(player.activeCharacterStatusKeys);
        var markers = buildMarkerText(player.activeCharacterMarkers);
        var raceText = player.activeCharacterRaceTags.Count > 0
            ? string.Join("/", player.activeCharacterRaceTags)
            : "未知";
        var activatedText = player.activeCharacterIsActivated ? "启动" : "未启动";
        return $"种族:{raceText} {activatedText}\n玩家:{playerStatuses} 角色:{characterStatuses} 指示物:{markers}";
    }

    private static string buildLocalizedStatusText(System.Collections.Generic.IReadOnlyList<string> statusKeys)
    {
        if (statusKeys.Count <= 0)
        {
            return "无";
        }

        var localized = new System.Collections.Generic.List<string>(statusKeys.Count);
        foreach (var statusKey in statusKeys)
        {
            localized.Add(localizeStatusKey(statusKey));
        }

        return string.Join(",", localized);
    }

    private static string buildMarkerText(System.Collections.Generic.IReadOnlyList<ProjectionMarkerViewModel> markers)
    {
        if (markers.Count <= 0)
        {
            return "无";
        }

        var parts = new System.Collections.Generic.List<string>(markers.Count);
        foreach (var marker in markers)
        {
            var label = string.IsNullOrWhiteSpace(marker.displayNameKey)
                ? localizeMarkerKey(marker.markerTypeKey)
                : localizeMarkerKey(marker.displayNameKey);
            parts.Add($"{label}{marker.count}/{marker.maxCount}");
        }

        return string.Join(",", parts);
    }

    private static string localizeStatusKey(string statusKey)
    {
        return statusKey switch
        {
            "Barrier" => "结界",
            "Seal" => "封印",
            "Shackle" => "禁锢",
            "Silence" => "沉默",
            "Charm" => "魅惑",
            "Penetrate" => "穿透",
            "PhysicalDamageBoostNext" => "下次体术伤害+1",
            "SpellDamageBoostNext" => "下次咒术伤害+1",
            "status:charm" => "魅惑",
            _ => statusKey,
        };
    }

    private static string localizeMarkerKey(string markerKey)
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
}
}

