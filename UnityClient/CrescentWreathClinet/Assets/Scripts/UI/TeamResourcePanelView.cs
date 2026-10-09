using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class TeamResourcePanelView : MonoBehaviour
{
    private const int MaxKillScore = 10;
    private const int MaxLeyline = 5;
    private const float PanelAlpha = 0.78f;
    private static readonly Vector2 RootSize = new(600f, 284f);
    private static readonly Vector2 RootAnchoredPosition = new(12f, -8f);

    [SerializeField]
    private TMP_FontAsset? primaryFont;

    private TMP_Text? turnText;
    private CanvasGroup? canvasGroup;
    private readonly List<TeamRowView> rows = new();

    public void Configure(TMP_FontAsset? font)
    {
        primaryFont = font;
        enforceRootRect();
        ensureHierarchy();
    }

    private void OnEnable()
    {
        enforceRootRect();
    }

    private void LateUpdate()
    {
        enforceRootRect();
    }

    public void Bind(ProjectionViewModel projection)
    {
        enforceRootRect();
        ensureHierarchy();
        if (turnText is not null)
        {
            turnText.text = $"Turn {projection.turnNumber} / {localizePhase(projection.currentPhase)} / P{projection.currentPlayerNumericId?.ToString() ?? "-"}";
        }

        var orderedTeams = projection.teamSummaries
            .OrderBy(team => team.teamNumericId)
            .Take(2)
            .ToList();
        for (var index = 0; index < rows.Count; index++)
        {
            if (index >= orderedTeams.Count)
            {
                rows[index].SetVisible(false);
                continue;
            }

            rows[index].SetVisible(true);
            rows[index].Bind(orderedTeams[index], index == 0, primaryFont);
        }
    }

    public void ShowWaiting()
    {
        enforceRootRect();
        ensureHierarchy();
        if (turnText is not null)
        {
            turnText.text = "等待投影";
        }

        foreach (var row in rows)
        {
            row.SetVisible(false);
        }
    }

    private void enforceRootRect()
    {
        if (transform is not RectTransform rect)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = RootAnchoredPosition;
        rect.sizeDelta = RootSize;

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = PanelAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ensureHierarchy()
    {
        if (turnText is not null && rows.Count == 2)
        {
            return;
        }

        clearGeneratedChildren(transform);

        var rootRect = transform as RectTransform ?? gameObject.GetComponent<RectTransform>();
        turnText = createText(
            "TurnText",
            rootRect,
            new Vector2(0.02f, 0.89f),
            new Vector2(0.98f, 0.995f),
            16f,
            TextAlignmentOptions.Center);

        rows.Clear();
        rows.Add(createRow("TeamAResourceRow", new Vector2(0.02f, 0.47f), new Vector2(0.98f, 0.88f), true));
        rows.Add(createRow("TeamBResourceRow", new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.45f), false));
    }

    private TeamRowView createRow(string name, Vector2 anchorMin, Vector2 anchorMax, bool isTeamA)
    {
        var rowObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rowObject.transform.SetParent(transform, false);
        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = anchorMin;
        rowRect.anchorMax = anchorMax;
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        var baseImage = rowObject.GetComponent<Image>();
        baseImage.sprite = TeamResourceSpriteService.GetBaseSprite(isTeamA);
        baseImage.color = Color.white;
        baseImage.preserveAspect = true;
        baseImage.raycastTarget = false;

        var row = new TeamRowView(rowRect, baseImage);
        row.Build(primaryFont, isTeamA);
        return row;
    }

    private TMP_Text createText(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        if (primaryFont is not null)
        {
            text.font = primaryFont;
        }

        return text;
    }

    private static void clearGeneratedChildren(Transform parent)
    {
        for (var index = parent.childCount - 1; index >= 0; index--)
        {
            var child = parent.GetChild(index);
            if (child.name == "Label")
            {
                child.gameObject.SetActive(false);
                continue;
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
    }

    private static string localizePhase(string phase)
    {
        return phase switch
        {
            "start" => "开始阶段",
            "action" => "行动阶段",
            "summon" => "召唤阶段",
            "end" => "结束阶段",
            _ => string.IsNullOrWhiteSpace(phase) ? "阶段未知" : phase,
        };
    }

    private sealed class TeamRowView
    {
        private readonly RectTransform root;
        private readonly Image baseImage;
        private TMP_Text? killText;
        private TMP_Text? leylineText;
        private readonly List<Image> killIcons = new();
        private readonly List<Image> leylineIcons = new();

        public TeamRowView(RectTransform root, Image baseImage)
        {
            this.root = root;
            this.baseImage = baseImage;
        }

        public void Build(TMP_FontAsset? font, bool isTeamA)
        {
            killText = createText(
                "KillScoreText",
                root,
                new Vector2(0.30f, 0.34f),
                new Vector2(0.51f, 0.80f),
                22f,
                TextAlignmentOptions.Center,
                font);
            leylineText = createText(
                "LeylineText",
                root,
                new Vector2(0.70f, 0.34f),
                new Vector2(0.90f, 0.80f),
                22f,
                TextAlignmentOptions.Center,
                font);

            createIconRow(
                killIcons,
                "KillIcon",
                root,
                MaxKillScore,
                new Vector2(0.30f, 0.16f),
                new Vector2(0.61f, 0.39f),
                index => TeamResourceSpriteService.GetKillSprite(isTeamA, false));
            createIconRow(
                leylineIcons,
                "LeylineIcon",
                root,
                MaxLeyline,
                new Vector2(0.70f, 0.16f),
                new Vector2(0.91f, 0.39f),
                index => TeamResourceSpriteService.GetLeylineSprite(isTeamA, false));
        }

        public void Bind(ProjectionTeamSummaryViewModel team, bool isTeamA, TMP_FontAsset? font)
        {
            baseImage.sprite = TeamResourceSpriteService.GetBaseSprite(isTeamA);
            setFont(killText, font);
            setFont(leylineText, font);

            var killScore = Mathf.Clamp(team.killScore, 0, MaxKillScore);
            var leyline = Mathf.Clamp(team.leyline, 0, MaxLeyline);
            if (killText is not null)
            {
                killText.text = $"击杀分 {killScore}/{MaxKillScore}";
            }

            if (leylineText is not null)
            {
                leylineText.text = $"灵脉 {leyline}/{MaxLeyline}";
            }

            for (var index = 0; index < killIcons.Count; index++)
            {
                killIcons[index].sprite = TeamResourceSpriteService.GetKillSprite(isTeamA, index < killScore);
                killIcons[index].enabled = killIcons[index].sprite is not null;
            }

            for (var index = 0; index < leylineIcons.Count; index++)
            {
                leylineIcons[index].sprite = TeamResourceSpriteService.GetLeylineSprite(isTeamA, index < leyline);
                leylineIcons[index].enabled = leylineIcons[index].sprite is not null;
            }
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
        }

        private static TMP_Text createText(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            TextAlignmentOptions alignment,
            TMP_FontAsset? font)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TMP_Text>();
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            setFont(text, font);
            return text;
        }

        private static void createIconRow(
            List<Image> icons,
            string namePrefix,
            RectTransform parent,
            int count,
            Vector2 anchorMin,
            Vector2 anchorMax,
            System.Func<int, Sprite?> initialSpriteProvider)
        {
            for (var index = 0; index < count; index++)
            {
                var iconObject = new GameObject($"{namePrefix}{index + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(parent, false);
                var icon = iconObject.GetComponent<Image>();
                var rect = icon.GetComponent<RectTransform>();
                var width = (anchorMax.x - anchorMin.x) / count;
                rect.anchorMin = new Vector2(anchorMin.x + width * index, anchorMin.y);
                rect.anchorMax = new Vector2(anchorMin.x + width * (index + 0.82f), anchorMax.y);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.sprite = initialSpriteProvider(index);
                icon.enabled = icon.sprite is not null;
                icons.Add(icon);
            }
        }

        private static void setFont(TMP_Text? text, TMP_FontAsset? font)
        {
            if (text is not null && font is not null)
            {
                text.font = font;
            }
        }
    }
}
}
