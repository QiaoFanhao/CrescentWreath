using CrescentWreath.Client.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class CardInspectPreviewView : MonoBehaviour
{
    private RectTransform? rootRect;
    private CanvasGroup? canvasGroup;
    private Image? artworkImage;
    private TMP_Text? titleText;
    private TMP_Text? detailText;
    private bool visible;

    public void Configure(BattlefieldUiTheme? theme)
    {
        ensureVisualTree(theme);
        Hide();
    }

    private void Awake()
    {
        ensureVisualTree(null);
        Hide();
    }

    public void Show(CardInspectViewModel model)
    {
        ensureVisualTree(null);
        transform.SetAsLastSibling();
        resizeForText(model);

        if (artworkImage is not null)
        {
            artworkImage.sprite = model.artworkSprite ?? CardArtService.GetCardBackSprite();
            artworkImage.preserveAspect = true;
        }

        if (titleText is not null)
        {
            titleText.text = model.isFaceUp ? model.displayName : "背面 / 未知卡牌";
        }

        if (detailText is not null)
        {
            detailText.text = buildDetailText(model);
        }

        setVisible(true);
    }

    public void Hide()
    {
        setVisible(false);
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (!visible || rootRect is null)
        {
            return;
        }

        var parentRect = transform.parent as RectTransform;
        if (parentRect is null)
        {
            rootRect.position = screenPosition;
            return;
        }

        var canvas = GetComponentInParent<Canvas>();
        var cameraToUse = canvas is not null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                cameraToUse,
                out var localPoint))
        {
            return;
        }

        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0f, 1f);

        var margin = 16f;
        var offset = new Vector2(28f, -28f);
        var target = localPoint + offset;
        var parentSize = parentRect.rect.size;
        var availableHeight = Mathf.Max(240f, parentSize.y - margin * 2f);
        var availableWidth = Mathf.Max(240f, parentSize.x - margin * 2f);
        var desiredSize = rootRect.sizeDelta;
        var scale = Mathf.Min(
            1f,
            availableWidth / Mathf.Max(desiredSize.x, 1f),
            availableHeight / Mathf.Max(desiredSize.y, 1f));
        rootRect.localScale = new Vector3(scale, scale, 1f);

        var scaledSize = desiredSize * scale;
        var minX = parentRect.rect.xMin + margin;
        var maxX = parentRect.rect.xMax - scaledSize.x - margin;
        if (maxX < minX)
        {
            maxX = minX;
        }

        var maxY = parentRect.rect.yMax - margin;
        var minY = parentRect.rect.yMin + scaledSize.y + margin;
        if (minY > maxY)
        {
            minY = maxY;
        }

        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, minY, maxY);
        rootRect.anchoredPosition = target;
    }

    private void ensureVisualTree(BattlefieldUiTheme? theme)
    {
        rootRect ??= transform.Find("PreviewRoot") as RectTransform;
        if (rootRect is null)
        {
            var rootObject = new GameObject(
                "PreviewRoot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            rootObject.transform.SetParent(transform, false);
            rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(480f, 760f);
        }

        var background = rootRect.GetComponent<Image>();
        background.color = new Color(0.015f, 0.035f, 0.05f, 0.96f);
        background.raycastTarget = false;

        canvasGroup = rootRect.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        artworkImage ??= rootRect.Find("Artwork")?.GetComponent<Image>();
        if (artworkImage is null)
        {
            var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artworkObject.transform.SetParent(rootRect, false);
            artworkImage = artworkObject.GetComponent<Image>();
        }

        setAnchors(artworkImage.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.96f));
        artworkImage.raycastTarget = false;

        titleText ??= rootRect.Find("Title")?.GetComponent<TMP_Text>();
        if (titleText is null)
        {
            titleText = createText("Title", rootRect, 24f, TextAlignmentOptions.Center, FontStyles.Bold, theme);
        }

        setAnchors(titleText.rectTransform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.405f));
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        detailText ??= rootRect.Find("Detail")?.GetComponent<TMP_Text>();
        if (detailText is null)
        {
            detailText = createText("Detail", rootRect, 17f, TextAlignmentOptions.TopLeft, FontStyles.Normal, theme);
        }

        setAnchors(detailText.rectTransform, new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.33f));
        detailText.enableWordWrapping = true;
        detailText.overflowMode = TextOverflowModes.Overflow;
        applyTextTheme(titleText, theme);
        applyTextTheme(detailText, theme);
    }

    private static TMP_Text createText(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment,
        FontStyles fontStyle,
        BattlefieldUiTheme? theme)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.color = theme?.NormalText ?? Color.white;
        text.raycastTarget = false;
        if (theme?.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private static void setAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void applyTextTheme(TMP_Text? text, BattlefieldUiTheme? theme)
    {
        if (text is null || theme is null)
        {
            return;
        }

        text.color = theme.NormalText;
        if (theme.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }
    }

    private static string buildDetailText(CardInspectViewModel model)
    {
        if (!model.isFaceUp)
        {
            return "该卡当前为背面，正式 UI 不显示隐藏信息。";
        }

        var builder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(model.attributeText))
        {
            builder.AppendLine(model.attributeText);
        }

        if (!string.IsNullOrWhiteSpace(model.rulesText))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(model.rulesText);
        }

        if (builder.Length == 0)
        {
            builder.Append("暂无展示文本。");
        }

        return builder.ToString();
    }

    private void resizeForText(CardInspectViewModel model)
    {
        if (rootRect is null)
        {
            return;
        }

        var textLength = (model.attributeText?.Length ?? 0) + (model.rulesText?.Length ?? 0);
        var estimatedExtraHeight = Mathf.Clamp(textLength * 1.1f, 0f, 360f);
        rootRect.sizeDelta = new Vector2(480f, 760f + estimatedExtraHeight);
    }

    private void setVisible(bool shouldBeVisible)
    {
        visible = shouldBeVisible;
        if (canvasGroup is null)
        {
            return;
        }

        canvasGroup.alpha = shouldBeVisible ? 1f : 0f;
    }
}
}

