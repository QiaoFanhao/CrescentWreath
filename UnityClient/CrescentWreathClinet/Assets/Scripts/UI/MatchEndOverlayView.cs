using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class MatchEndOverlayView : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup? canvasGroup;

    private ProjectionViewState? projectionViewState;
    private BattlefieldUiTheme? theme;
    private TMP_Text? titleText;
    private TMP_Text? bodyText;
    private OverlayCollapseToggle? collapseToggle;

    public void Configure(ProjectionViewState newProjectionViewState, BattlefieldUiTheme newTheme)
    {
        if (projectionViewState is not null)
        {
            projectionViewState.ProjectionChanged -= render;
        }

        projectionViewState = newProjectionViewState;
        theme = newTheme;
        ensureBuilt();
        projectionViewState.ProjectionChanged += render;
        render(projectionViewState.Current);
    }

    private void Awake()
    {
        ensureBuilt();
        setVisible(false);
    }

    private void OnDestroy()
    {
        if (projectionViewState is not null)
        {
            projectionViewState.ProjectionChanged -= render;
        }
    }

    private void ensureBuilt()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        var rectTransform = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        image.color = new Color(0.005f, 0.015f, 0.02f, 0.72f);
        image.raycastTarget = true;

        var panel = transform.Find("Panel") as RectTransform;
        if (panel is null)
        {
            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.09f, 0.13f, 0.96f);
        }

        panel.anchorMin = new Vector2(0.32f, 0.40f);
        panel.anchorMax = new Vector2(0.68f, 0.62f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        titleText = getOrCreateText(panel, "Title", 30f, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = new Vector2(0.06f, 0.58f);
        titleText.rectTransform.anchorMax = new Vector2(0.94f, 0.92f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;

        bodyText = getOrCreateText(panel, "Body", 18f, TextAlignmentOptions.Center);
        bodyText.rectTransform.anchorMin = new Vector2(0.08f, 0.16f);
        bodyText.rectTransform.anchorMax = new Vector2(0.92f, 0.54f);
        bodyText.rectTransform.offsetMin = Vector2.zero;
        bodyText.rectTransform.offsetMax = Vector2.zero;

        collapseToggle = GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
    }

    private TMP_Text getOrCreateText(RectTransform parent, string name, float fontSize, TextAlignmentOptions alignment)
    {
        var existing = parent.Find(name);
        var textObject = existing?.gameObject ?? new GameObject(name, typeof(RectTransform));
        if (existing is null)
        {
            textObject.transform.SetParent(parent, false);
        }

        var text = textObject.GetComponent<TMP_Text>() ?? textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = theme?.NormalText ?? Color.white;
        text.enableWordWrapping = true;
        if (theme?.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private void render(ProjectionViewModel? projection)
    {
        var isEnded = projection is not null &&
                      string.Equals(projection.matchState, "ended", System.StringComparison.OrdinalIgnoreCase);
        if (!isEnded)
        {
            setVisible(false);
            return;
        }

        ensureBuilt();
        if (titleText is not null)
        {
            titleText.text = projection!.winnerTeamNumericId.HasValue
                ? $"队伍 {projection.winnerTeamNumericId.Value} 获胜"
                : "游戏结束";
            titleText.color = theme?.GoldActive ?? new Color(1f, 0.82f, 0.22f, 1f);
        }

        if (bodyText is not null)
        {
            bodyText.text = "有一方击坠分已经归零。本局操作已冻结，请准备离开或重新开始对局。";
        }

        setVisible(true);
    }

    private void setVisible(bool visible)
    {
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        collapseToggle = collapseToggle != null
            ? collapseToggle
            : GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
        collapseToggle.SetVisible(visible);
    }
}
}
