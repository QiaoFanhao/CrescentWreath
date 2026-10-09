using TMPro;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using UnityEngine;
using UnityEngine.UI;

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
    private Image? hpCardImage;
    private Image? characterCardImage;
    private Image? teamIconImage;

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
            bindSeatVisuals(null);
            setText(playerNameText, string.Empty);
            setText(hpText, string.Empty);
            setText(handCountText, "手牌 -");
            setText(deckCountText, "牌库 -");
            setText(discardCountText, "弃牌 -");
            setText(ensureResourceText(), string.Empty);
            setText(ensureStatusText(), string.Empty);
            return;
        }

        bindSeatVisuals(player);

        setText(playerNameText, string.Empty);
        setText(hpText, string.Empty);
        setText(handCountText, $"手牌 {player.handCount}");
        setText(deckCountText, $"牌库 {player.deckCount}");
        setText(discardCountText, $"弃牌 {player.discardCount}");

        setText(ensureResourceText(), string.Empty);
        setText(ensureStatusText(), string.Empty);
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

    private void bindSeatVisuals(ProjectionPlayerSummaryViewModel? player)
    {
        if (characterPortraitPlaceholder is null)
        {
            return;
        }

        hpCardImage = ensureSeatImage(hpCardImage, characterPortraitPlaceholder, "SeatHpCardImage",
            new Vector2(0.00f, 0.04f), new Vector2(0.42f, 0.96f));
        characterCardImage = ensureSeatImage(characterCardImage, characterPortraitPlaceholder, "SeatCharacterCardImage",
            new Vector2(0.46f, 0.04f), new Vector2(1.00f, 0.96f));
        teamIconImage = ensureSeatImage(teamIconImage, characterPortraitPlaceholder, "SeatTeamIconImage",
            new Vector2(0.34f, 0.02f), new Vector2(0.52f, 0.24f));

        var hpSprite = HpCardSpriteService.GetSpriteForHp(player?.activeCharacterCurrentHp);
        hpCardImage.sprite = hpSprite;
        hpCardImage.enabled = hpSprite is not null;

        var characterDefinitionId = player?.activeCharacterDefinitionId;
        var characterSprite = string.IsNullOrWhiteSpace(characterDefinitionId)
            ? null
            : CardArtService.GetSpriteForDefinition(characterDefinitionId);
        characterCardImage.sprite = characterSprite;
        characterCardImage.enabled = characterSprite is not null;

        var teamIconSprite = TeamResourceSpriteService.GetTeamIconSprite(player?.teamNumericId);
        teamIconImage.sprite = teamIconSprite;
        teamIconImage.enabled = teamIconSprite is not null;

        hpCardImage.transform.SetAsFirstSibling();
        characterCardImage.transform.SetSiblingIndex(Mathf.Min(1, characterCardImage.transform.parent.childCount - 1));
        teamIconImage.transform.SetAsLastSibling();
    }

    private static Image ensureSeatImage(
        Image? cached,
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        var image = cached;
        if (image is null)
        {
            var existing = parent.Find(name);
            if (existing is not null)
            {
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                image = imageObject.GetComponent<Image>();
            }
        }

        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
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

