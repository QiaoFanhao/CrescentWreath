using System.Collections.Generic;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class ZoneCardListOverlayView : MonoBehaviour
{
    private readonly List<GameObject> rowObjects = new();

    private BattlefieldUiTheme? theme;
    private CanvasGroup? canvasGroup;
    private RectTransform? windowRoot;
    private RectTransform? listRoot;
    private TMP_Text? titleText;
    private Button? closeButton;
    private OverlayCollapseToggle? collapseToggle;

    public void Configure(BattlefieldUiTheme newTheme)
    {
        theme = newTheme;
        ensureVisualTree();
        Hide();
    }

    private void Awake()
    {
        ensureVisualTree();
        Hide();
    }

    public void Show(string title, IReadOnlyList<ProjectionCardViewModel> cards)
    {
        ensureVisualTree();
        transform.SetAsLastSibling();
        windowRoot!.SetAsLastSibling();
        titleText!.text = title;
        clearRows();

        if (cards.Count == 0)
        {
            createTextRow("该区域当前没有公开卡牌。", string.Empty);
        }
        else
        {
            for (var index = 0; index < cards.Count; index += 1)
            {
                createCardRow(cards[index], index);
            }
        }

        listRoot!.sizeDelta = new Vector2(listRoot.sizeDelta.x, Mathf.Max(360f, cards.Count * 82f));
        setVisible(true, resetCollapsed: true);
    }

    public void ShowDeckKnowledge(string title, IReadOnlyList<ProjectionOwnedCardKnowledgeViewModel> cards)
    {
        ensureVisualTree();
        transform.SetAsLastSibling();
        windowRoot!.SetAsLastSibling();
        titleText!.text = title;
        clearRows();

        if (cards.Count == 0)
        {
            createTextRow("没有可公开推测的卡牌。", "手牌和卡组仍保持未知。");
            listRoot!.sizeDelta = new Vector2(listRoot.sizeDelta.x, 360f);
            setVisible(true, resetCollapsed: true);
            return;
        }

        const int columns = 8;
        const float cellWidth = 92f;
        const float cellHeight = 130f;
        const float gap = 10f;
        for (var index = 0; index < cards.Count; index += 1)
        {
            createKnowledgeCell(cards[index], index, columns, cellWidth, cellHeight, gap);
        }

        var rows = Mathf.CeilToInt(cards.Count / (float)columns);
        listRoot!.sizeDelta = new Vector2(listRoot.sizeDelta.x, Mathf.Max(360f, rows * (cellHeight + gap) + 8f));
        setVisible(true, resetCollapsed: true);
    }

    public void ShowHiddenCount(string title, int hiddenCount, string hint)
    {
        ensureVisualTree();
        transform.SetAsLastSibling();
        windowRoot!.SetAsLastSibling();
        titleText!.text = title;
        clearRows();

        var countText = hiddenCount <= 0 ? "该区域当前没有卡牌。" : $"该区域有 {hiddenCount} 张牌。";
        createTextRow(countText, hint);
        listRoot!.sizeDelta = new Vector2(listRoot.sizeDelta.x, 360f);
        setVisible(true, resetCollapsed: true);
    }

    public void Hide()
    {
        setVisible(false);
    }

    private void ensureVisualTree()
    {
        canvasGroup ??= gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup is null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        var rootRect = gameObject.GetComponent<RectTransform>();
        if (rootRect is not null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        var dimmer = gameObject.GetComponent<Image>();
        if (dimmer is null)
        {
            dimmer = gameObject.AddComponent<Image>();
        }

        dimmer.color = new Color(0f, 0.01f, 0.012f, 0.72f);
        dimmer.raycastTarget = true;

        windowRoot ??= transform.Find("WindowRoot") as RectTransform;
        if (windowRoot is null)
        {
            var windowObject = new GameObject("WindowRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            windowObject.transform.SetParent(transform, false);
            windowRoot = windowObject.GetComponent<RectTransform>();
        }

        removeLegacyDirectChild("Title");
        removeLegacyDirectChild("CloseButton");
        removeLegacyDirectChild("ListRoot");
        if (titleText is not null && titleText.transform.parent != windowRoot)
        {
            titleText = null;
        }

        if (closeButton is not null && closeButton.transform.parent != windowRoot)
        {
            closeButton = null;
        }

        setAnchors(windowRoot, new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.84f));
        var windowImage = windowRoot.GetComponent<Image>();
        if (windowImage is null)
        {
            windowImage = windowRoot.gameObject.AddComponent<Image>();
        }

        windowImage.color = new Color(0.015f, 0.035f, 0.05f, 0.97f);
        windowImage.raycastTarget = true;

        titleText ??= windowRoot.Find("Title")?.GetComponent<TMP_Text>();
        if (titleText is null)
        {
            titleText = createText("Title", windowRoot, "区域", 26f, TextAlignmentOptions.Left);
        }

        setAnchors(titleText.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.74f, 0.985f));

        closeButton ??= windowRoot.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton is null)
        {
            closeButton = createButton("CloseButton", "关闭", windowRoot, new Vector2(0.78f, 0.90f), new Vector2(0.97f, 0.985f));
        }

        setAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.90f), new Vector2(0.97f, 0.985f));
        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);

        var scrollView = windowRoot.Find("ScrollView") as RectTransform;
        if (scrollView is null)
        {
            var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(windowRoot, false);
            scrollView = scrollObject.GetComponent<RectTransform>();
        }

        setAnchors(scrollView, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.88f));
        var scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;

        var viewport = scrollView.Find("Viewport") as RectTransform;
        if (viewport is null)
        {
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollView, false);
            viewport = viewportObject.GetComponent<RectTransform>();
        }

        setAnchors(viewport, Vector2.zero, Vector2.one);
        var viewportImage = viewport.GetComponent<Image>();
        if (viewportImage is null)
        {
            viewportImage = viewport.gameObject.AddComponent<Image>();
        }

        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        var viewportMask = viewport.GetComponent<Mask>();
        if (viewportMask is null)
        {
            viewportMask = viewport.gameObject.AddComponent<Mask>();
        }

        viewportMask.showMaskGraphic = false;
        scrollRect.viewport = viewport;

        listRoot ??= viewport.Find("Content") as RectTransform;
        if (listRoot is null || listRoot.parent != viewport)
        {
            if (listRoot is not null && listRoot.parent != viewport)
            {
                if (Application.isPlaying)
                {
                    Destroy(listRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(listRoot.gameObject);
                }
            }

            var listObject = new GameObject("Content", typeof(RectTransform));
            listObject.transform.SetParent(viewport, false);
            listRoot = listObject.GetComponent<RectTransform>();
        }

        listRoot.anchorMin = new Vector2(0f, 1f);
        listRoot.anchorMax = new Vector2(1f, 1f);
        listRoot.pivot = new Vector2(0.5f, 1f);
        listRoot.anchoredPosition = Vector2.zero;
        listRoot.offsetMin = new Vector2(0f, listRoot.offsetMin.y);
        listRoot.offsetMax = new Vector2(0f, listRoot.offsetMax.y);
        scrollRect.content = listRoot;

        collapseToggle = gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
    }

    private void createCardRow(ProjectionCardViewModel card, int index)
    {
        var row = createRowObject($"CardRow_{index}", index);
        var imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(row.transform, false);
        var imageRect = imageObject.GetComponent<RectTransform>();
        setAnchors(imageRect, new Vector2(0.02f, 0.08f), new Vector2(0.12f, 0.92f));
        var image = imageObject.GetComponent<Image>();
        image.sprite = string.IsNullOrWhiteSpace(card.definitionId)
            ? CardArtService.GetCardBackSprite()
            : CardArtService.GetSpriteForDefinition(card.definitionId);
        image.preserveAspect = true;
        image.raycastTarget = false;

        var name = CardNameLocalizer.Localize(card.definitionId);
        createTextOnRow(row.transform, "Name", name, 18f, new Vector2(0.15f, 0.48f), new Vector2(0.96f, 0.92f));
        createTextOnRow(row.transform, "Detail", localizeZone(card.zoneKey), 13f, new Vector2(0.15f, 0.08f), new Vector2(0.96f, 0.46f));
    }

    private void createTextRow(string title, string detail)
    {
        var row = createRowObject("EmptyRow", 0);
        createTextOnRow(row.transform, "Title", title, 18f, new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.92f));
        createTextOnRow(row.transform, "Detail", detail, 13f, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.43f));
    }

    private void createKnowledgeCell(
        ProjectionOwnedCardKnowledgeViewModel card,
        int index,
        int columns,
        float cellWidth,
        float cellHeight,
        float gap)
    {
        var cell = new GameObject($"KnowledgeCard_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cell.transform.SetParent(listRoot, false);
        var rect = cell.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cellWidth, cellHeight);
        var column = index % columns;
        var row = index / columns;
        rect.anchoredPosition = new Vector2(column * (cellWidth + gap), -row * (cellHeight + gap));
        cell.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.06f, 0.88f);
        rowObjects.Add(cell);

        var imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(cell.transform, false);
        var imageRect = imageObject.GetComponent<RectTransform>();
        setAnchors(imageRect, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.96f));
        var image = imageObject.GetComponent<Image>();
        image.sprite = CardArtService.GetSpriteForDefinition(card.definitionId);
        image.preserveAspect = true;
        image.raycastTarget = false;

        if (!card.isHiddenCandidate)
        {
            var dimObject = new GameObject("KnownLocationDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimObject.transform.SetParent(cell.transform, false);
            setAnchors(dimObject.GetComponent<RectTransform>(), new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.96f));
            var dim = dimObject.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = false;

            var slashObject = new GameObject("KnownLocationSlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slashObject.transform.SetParent(cell.transform, false);
            var slashRect = slashObject.GetComponent<RectTransform>();
            slashRect.anchorMin = new Vector2(0.50f, 0.58f);
            slashRect.anchorMax = new Vector2(0.50f, 0.58f);
            slashRect.pivot = new Vector2(0.5f, 0.5f);
            slashRect.sizeDelta = new Vector2(7f, 98f);
            slashRect.localEulerAngles = new Vector3(0f, 0f, -38f);
            var slash = slashObject.GetComponent<Image>();
            slash.color = new Color(0.35f, 0.95f, 0.05f, 0.92f);
            slash.raycastTarget = false;
        }

        var name = CardNameLocalizer.Localize(card.definitionId);
        var nameText = createText("Name", cell.transform, name, 11f, TextAlignmentOptions.Center);
        setAnchors(nameText.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.18f));
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        var tooltipObject = new GameObject("LocationTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tooltipObject.transform.SetParent(cell.transform, false);
        setAnchors(tooltipObject.GetComponent<RectTransform>(), new Vector2(0.02f, 0.35f), new Vector2(0.98f, 0.65f));
        tooltipObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        var tooltipText = createText(
            "Text",
            tooltipObject.transform,
            card.isHiddenCandidate ? string.Empty : $"位置：{localizeOwnedCardLocation(card.locationKey)}",
            12f,
            TextAlignmentOptions.Center);
        setAnchors(tooltipText.rectTransform, Vector2.zero, Vector2.one);
        tooltipObject.SetActive(false);

        var hover = cell.AddComponent<DeckKnowledgeCellHoverHandler>();
        hover.Configure(tooltipObject, !card.isHiddenCandidate);
    }

    private GameObject createRowObject(string name, int index)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        row.transform.SetParent(listRoot, false);
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 76f);
        rect.anchoredPosition = new Vector2(0f, -index * 82f);
        row.GetComponent<Image>().color = index % 2 == 0
            ? new Color(0.08f, 0.16f, 0.20f, 0.96f)
            : new Color(0.06f, 0.12f, 0.16f, 0.96f);
        rowObjects.Add(row);
        return row;
    }

    private TMP_Text createTextOnRow(Transform parent, string name, string value, float fontSize, Vector2 min, Vector2 max)
    {
        var text = createText(name, parent, value, fontSize, TextAlignmentOptions.Left);
        text.color = theme?.NormalText ?? Color.white;
        setAnchors(text.rectTransform, min, max);
        return text;
    }

    private Button createButton(string name, string label, Transform parent, Vector2 min, Vector2 max)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        setAnchors(buttonObject.GetComponent<RectTransform>(), min, max);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.35f, 0.42f, 1f);
        var button = buttonObject.GetComponent<Button>();
        var text = createText("Label", buttonObject.transform, label, 16f, TextAlignmentOptions.Center);
        setAnchors(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private TMP_Text createText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = theme?.PrimaryFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = theme?.NormalText ?? Color.white;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static void setAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void clearRows()
    {
        foreach (var row in rowObjects)
        {
            if (row is not null)
            {
                Destroy(row);
            }
        }

        rowObjects.Clear();
    }

    private void removeLegacyDirectChild(string childName)
    {
        var legacyChild = transform.Find(childName);
        if (legacyChild is null || legacyChild.parent != transform)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(legacyChild.gameObject);
        }
        else
        {
            DestroyImmediate(legacyChild.gameObject);
        }
    }

    private void setVisible(bool visible, bool resetCollapsed = false)
    {
        canvasGroup ??= gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup is null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        collapseToggle = collapseToggle != null
            ? collapseToggle
            : gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
        collapseToggle.SetVisible(visible, resetCollapsed);
    }

    private static string localizeZone(string zoneKey)
    {
        if (zoneKey.Contains("gap", System.StringComparison.OrdinalIgnoreCase))
        {
            return "间隙区";
        }

        if (zoneKey.Contains("discard", System.StringComparison.OrdinalIgnoreCase))
        {
            return "弃牌区";
        }

        return string.IsNullOrWhiteSpace(zoneKey) ? "公开区域" : zoneKey;
    }

    private static string localizeOwnedCardLocation(string locationKey)
    {
        return locationKey switch
        {
            "discard" => "弃牌区",
            "field" => "阵地区",
            "defensePlacedField" => "阵地区（防御放置）",
            "overlay" => "卡牌下方叠放",
            "characterSetAside" => "角色卡下方",
            _ => string.IsNullOrWhiteSpace(locationKey) ? "公开区域" : locationKey,
        };
    }
}

public sealed class DeckKnowledgeCellHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject? tooltipObject;
    private bool shouldShowTooltip;

    public void Configure(GameObject newTooltipObject, bool newShouldShowTooltip)
    {
        tooltipObject = newTooltipObject;
        shouldShowTooltip = newShouldShowTooltip;
        tooltipObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shouldShowTooltip && tooltipObject is not null)
        {
            tooltipObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject is not null)
        {
            tooltipObject.SetActive(false);
        }
    }
}
}
