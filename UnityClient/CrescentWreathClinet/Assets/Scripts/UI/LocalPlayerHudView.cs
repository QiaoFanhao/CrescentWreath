using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class LocalPlayerHudView : MonoBehaviour
{
    private const string PrimaryFontResourcePath = "UI/Fonts/SourceHanSansSC-Regular SDF";
    private static TMP_FontAsset? primaryFont;

    [SerializeField]
    private Image? hpImage;

    [SerializeField]
    private TMP_Text? hpFallbackText;

    [SerializeField]
    private TMP_Text? labelText;

    public void Bind(ProjectionPlayerSummaryViewModel? player)
    {
        ensureVisuals();
        var currentHp = player?.activeCharacterCurrentHp;
        var maxHp = player?.activeCharacterMaxHp;
        var sprite = HpCardSpriteService.GetSpriteForHp(currentHp);

        if (hpImage is not null)
        {
            hpImage.sprite = sprite;
            hpImage.enabled = true;
            hpImage.preserveAspect = true;
            hpImage.color = sprite is null
                ? new Color(0.66f, 0.06f, 0.1f, 0.9f)
                : Color.white;
        }

        if (hpFallbackText is not null)
        {
            hpFallbackText.gameObject.SetActive(sprite is null);
            hpFallbackText.text = currentHp.HasValue
                ? $"HP\n{currentHp.Value}/{(maxHp.HasValue ? maxHp.Value.ToString() : "?")}"
                : "HP\n-";
        }

        if (labelText is not null)
        {
            labelText.text = player is null
                ? "HP -\n状态：无\n指示物：无"
                : $"HP {formatNullableInt(currentHp)}/{formatNullableInt(maxHp)}\n" +
                  $"状态：{buildLocalizedStatusText(player.activeCharacterStatusKeys)}\n" +
                  $"指示物：{buildMarkerText(player.activeCharacterMarkers)}";
        }
    }

    private void ensureVisuals()
    {
        var rect = GetComponent<RectTransform>();

        if (hpImage is null)
        {
            var imageObject = new GameObject("LocalHpCardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            hpImage = imageObject.GetComponent<Image>();
            var imageRect = hpImage.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.04f, 0.08f);
            imageRect.anchorMax = new Vector2(0.38f, 0.92f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            hpImage.raycastTarget = false;
            hpImage.preserveAspect = true;
        }

        if (hpFallbackText is null)
        {
            var fallbackObject = new GameObject("LocalHpFallbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            fallbackObject.transform.SetParent(transform, false);
            hpFallbackText = fallbackObject.GetComponent<TMP_Text>();
            var fallbackRect = hpFallbackText.GetComponent<RectTransform>();
            fallbackRect.anchorMin = new Vector2(0.04f, 0.08f);
            fallbackRect.anchorMax = new Vector2(0.38f, 0.92f);
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
            hpFallbackText.alignment = TextAlignmentOptions.Center;
            hpFallbackText.fontSize = 24f;
            hpFallbackText.color = Color.white;
            hpFallbackText.font = getPrimaryFont() ?? hpFallbackText.font;
            hpFallbackText.raycastTarget = false;
        }

        if (labelText is null)
        {
            var labelObject = new GameObject("LocalHpLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            labelText = labelObject.GetComponent<TMP_Text>();
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.42f, 0.14f);
            labelRect.anchorMax = new Vector2(0.96f, 0.88f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.fontSize = 15f;
            labelText.color = Color.white;
            labelText.font = getPrimaryFont() ?? labelText.font;
            labelText.raycastTarget = false;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (rect is not null)
        {
            rect.SetAsLastSibling();
        }
    }

    private static string formatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "-";
    }

    private static TMP_FontAsset? getPrimaryFont()
    {
        if (primaryFont is not null)
        {
            return primaryFont;
        }

        primaryFont = Resources.Load<TMP_FontAsset>(PrimaryFontResourcePath);
        return primaryFont;
    }

    private static string buildLocalizedStatusText(System.Collections.Generic.IReadOnlyList<string> statusKeys)
    {
        if (statusKeys.Count <= 0)
        {
            return "无";
        }

        var parts = new System.Collections.Generic.List<string>(statusKeys.Count);
        foreach (var statusKey in statusKeys)
        {
            parts.Add(localizeStatusKey(statusKey));
        }

        return string.Join(",", parts);
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
            parts.Add($"{localizeMarkerKey(marker.markerTypeKey)}{marker.count}");
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
            "PhysicalDamageBoostNext" => "体术+",
            "SpellDamageBoostNext" => "咒术+",
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
