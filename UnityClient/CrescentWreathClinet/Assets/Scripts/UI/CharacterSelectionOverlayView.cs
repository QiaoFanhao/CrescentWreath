using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class CharacterSelectionOverlayView : MonoBehaviour
{
    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private GameClientNetworkController? networkController;

    [SerializeField]
    private BattlefieldUiTheme? theme;

    private readonly Dictionary<string, Button> buttonByDefinitionId = new(StringComparer.Ordinal);
    private CanvasGroup? canvasGroup;
    private RectTransform? characterViewport;
    private RectTransform? listContent;
    private TMP_Text? titleText;
    private TMP_Text? detailText;
    private TMP_Text? selectedText;
    private OverlayCollapseToggle? collapseToggle;
    private long? renderedSelectingPlayerNumericId;
    private ProjectionViewModel? currentProjection;

    private void Awake()
    {
        ensureVisualTree();
        setVisible(false);
    }

    public void Configure(
        ProjectionViewState newProjectionViewState,
        GameClientNetworkController newNetworkController,
        BattlefieldUiTheme newTheme)
    {
        projectionViewState = newProjectionViewState;
        networkController = newNetworkController;
        theme = newTheme;
        ensureVisualTree();
        subscribe();
        render(projectionViewState.Current);
    }

    private void OnEnable()
    {
        ensureVisualTree();
        subscribe();
        render(projectionViewState == null ? null : projectionViewState.Current);
    }

    private void OnDisable()
    {
        if (projectionViewState != null)
        {
            projectionViewState.ProjectionChanged -= render;
        }
    }

    private void subscribe()
    {
        if (projectionViewState == null)
        {
            return;
        }

        projectionViewState.ProjectionChanged -= render;
        projectionViewState.ProjectionChanged += render;
    }

    private void render(ProjectionViewModel? projection)
    {
        ensureVisualTree();
        if (projection is null ||
            !projection.hasStateProjection ||
            !projection.characterSelection.isActive ||
            projection.characterSelection.isCompleted)
        {
            setVisible(false);
            renderedSelectingPlayerNumericId = null;
            currentProjection = null;
            return;
        }

        currentProjection = projection;
        var currentSelectingPlayerId = projection.characterSelection.currentSelectingPlayerNumericId;
        var isNewSelectingPlayer = renderedSelectingPlayerNumericId != currentSelectingPlayerId;
        setVisible(true, isNewSelectingPlayer);
        renderedSelectingPlayerNumericId = currentSelectingPlayerId;
        var isViewerSelecting = currentSelectingPlayerId.HasValue &&
                                currentSelectingPlayerId.Value == projection.viewerPlayerNumericId;
        setText(titleText, "开局角色选择");
        setText(
            detailText,
            isViewerSelecting
                ? "轮到你选择角色。已被选择的角色不能重复选择。"
                : $"等待 Player {currentSelectingPlayerId?.ToString() ?? "?"} 选择角色。");
        setText(selectedText, buildSelectedText(projection));
        renderCharacterButtons(projection, isViewerSelecting);
        refreshCollapsedState();
    }

    private void renderCharacterButtons(ProjectionViewModel projection, bool isViewerSelecting)
    {
        clearList();
        buttonByDefinitionId.Clear();
        var selectedDefinitionIds = new HashSet<string>(
            projection.characterSelection.selections.Select(selection => selection.characterDefinitionId),
            StringComparer.Ordinal);
        var definitions = projection.characterDefinitions
            .OrderBy(definition => definition.definitionId, StringComparer.Ordinal)
            .ToList();

        for (var index = 0; index < definitions.Count; index += 1)
        {
            var definition = definitions[index];
            var alreadySelected = selectedDefinitionIds.Contains(definition.definitionId);
            var interactable = isViewerSelecting &&
                               definition.isImplemented &&
                               !alreadySelected &&
                               networkController != null &&
                               !networkController.IsRequestInFlight;
            var button = createCharacterButton(listContent!, definition, alreadySelected, interactable, index);
            buttonByDefinitionId[definition.definitionId] = button;
        }
    }

    private Button createCharacterButton(
        RectTransform parent,
        ProjectionCharacterDefinitionViewModel definition,
        bool alreadySelected,
        bool interactable,
        int index)
    {
        const float cardWidth = 90f;
        const float cardHeight = 126f;
        const float cellWidth = 106f;
        const float cellHeight = 146f;
        const float startX = 10f;
        const float startY = 8f;

        Canvas.ForceUpdateCanvases();
        var viewportWidth = characterViewport?.rect.width ?? parent.rect.width;
        if (viewportWidth < cellWidth * 2f)
        {
            viewportWidth = 1480f;
        }
        var columns = Mathf.Max(1, Mathf.FloorToInt((viewportWidth - startX * 2f) / cellWidth));
        var column = index % columns;
        var row = index / columns;
        var buttonObject = new GameObject(
            $"Character_{definition.definitionId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        rect.anchoredPosition = new Vector2(startX + column * cellWidth, -startY - row * cellHeight);

        var image = buttonObject.GetComponent<Image>();
        image.sprite = CardArtService.GetSpriteForDefinition(definition.definitionId);
        image.preserveAspect = true;
        image.color = interactable || alreadySelected || definition.isImplemented
            ? Color.white
            : new Color(0.42f, 0.42f, 0.42f, 0.86f);
        var button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(() => submitCharacter(definition.definitionId));

        var inspectable = buttonObject.AddComponent<BattlefieldInspectableUiCard>();
        inspectable.Configure(buildCharacterInspectModel(definition), null);

        if (alreadySelected || !definition.isImplemented)
        {
            var overlayObject = new GameObject("UnavailableOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(buttonObject.transform, false);
            var overlay = overlayObject.GetComponent<Image>();
            overlay.color = alreadySelected
                ? new Color(0f, 0f, 0f, 0.42f)
                : new Color(0f, 0f, 0f, 0.58f);
            overlay.raycastTarget = false;
            setAnchors(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var badge = createText(
                "StateBadge",
                buttonObject.transform,
                alreadySelected ? "已选" : "未开放",
                13f,
                TextAlignmentOptions.Center);
            badge.color = alreadySelected
                ? new Color(1f, 0.86f, 0.26f, 1f)
                : new Color(0.82f, 0.84f, 0.88f, 1f);
            setAnchors(badge.rectTransform, new Vector2(0f, 0.42f), Vector2.one, Vector2.zero, Vector2.zero);
        }

        var rows = row + 1;
        parent.sizeDelta = new Vector2(
            Mathf.Max(parent.sizeDelta.x, startX * 2f + columns * cellWidth),
            Mathf.Max(parent.sizeDelta.y, startY * 2f + rows * cellHeight));
        return button;
    }

    private static CardInspectViewModel buildCharacterInspectModel(ProjectionCharacterDefinitionViewModel definition)
    {
        var displayEntry = CardDisplayCatalogService.TryGetEntry(definition.definitionId);
        return new CardInspectViewModel
        {
            cardInstanceNumericId = 0,
            definitionId = definition.definitionId,
            displayName = displayEntry?.name ?? CardNameLocalizer.Localize(definition.definitionId),
            attributeText = CardDisplayCatalogService.BuildAttributeText(definition.definitionId),
            rulesText = CardDisplayCatalogService.BuildRulesText(definition.definitionId),
            ownerPlayerNumericId = 0,
            zoneKey = "characterSelection",
            isFaceUp = true,
            artworkSprite = CardArtService.GetSpriteForDefinition(definition.definitionId),
        };
    }

    private void submitCharacter(string definitionId)
    {
        if (!canSubmitCharacterSelectionNow())
        {
            setText(detailText, "当前还没有轮到你选择角色。");
            return;
        }

        setAllCharacterButtonsInteractable(false);
        if (networkController == null ||
            !networkController.TrySubmitCharacterSelection(definitionId))
        {
            setText(detailText, networkController == null ? "角色选择请求未发送。" : networkController.LastError);
            render(currentProjection);
        }
    }

    private bool canSubmitCharacterSelectionNow()
    {
        if (networkController == null ||
            networkController.IsRequestInFlight ||
            currentProjection is null ||
            !currentProjection.characterSelection.isActive ||
            currentProjection.characterSelection.isCompleted)
        {
            return false;
        }

        var currentSelectingPlayerId = currentProjection.characterSelection.currentSelectingPlayerNumericId;
        return currentSelectingPlayerId.HasValue &&
               currentSelectingPlayerId.Value == currentProjection.viewerPlayerNumericId &&
               !currentProjection.characterSelection.selections.Any(selection =>
                   selection.playerNumericId == currentProjection.viewerPlayerNumericId);
    }

    private void setAllCharacterButtonsInteractable(bool interactable)
    {
        foreach (var button in buttonByDefinitionId.Values)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private void ensureVisualTree()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        var image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        image.color = new Color(0.015f, 0.03f, 0.045f, 0.96f);
        image.raycastTarget = true;

        var rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }
        rect.anchorMin = new Vector2(0.06f, 0.08f);
        rect.anchorMax = new Vector2(0.94f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (titleText != null)
        {
            collapseToggle = gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
            collapseToggle.Configure(theme);
            return;
        }

        for (var index = transform.childCount - 1; index >= 0; index -= 1)
        {
            destroy(transform.GetChild(index).gameObject);
        }

        titleText = createText("Title", transform, "开局角色选择", 20f, TextAlignmentOptions.Center);
        setAnchors(titleText.rectTransform, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        detailText = createText("Detail", transform, string.Empty, 13f, TextAlignmentOptions.Center);
        setAnchors(detailText.rectTransform, new Vector2(0.04f, 0.81f), new Vector2(0.96f, 0.89f), Vector2.zero, Vector2.zero);
        selectedText = createText("Selected", transform, string.Empty, 11.5f, TextAlignmentOptions.Left);
        setAnchors(selectedText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.18f), Vector2.zero, Vector2.zero);

        var viewportObject = new GameObject("CharacterViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        characterViewport = viewport;
        setAnchors(viewport, new Vector2(0.03f, 0.20f), new Vector2(0.97f, 0.80f), Vector2.zero, Vector2.zero);
        viewportObject.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.76f);

        listContent = new GameObject("CharacterListContent", typeof(RectTransform)).GetComponent<RectTransform>();
        listContent.transform.SetParent(viewportObject.transform, false);
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(0f, 1f);
        listContent.pivot = new Vector2(0f, 1f);
        listContent.offsetMin = Vector2.zero;
        listContent.offsetMax = Vector2.zero;
        listContent.sizeDelta = Vector2.zero;

        var scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = listContent;
        scrollRect.horizontal = false;

        collapseToggle = gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
    }

    private TMP_Text createText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (theme != null && theme.PrimaryFont != null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private void setVisible(bool visible, bool resetCollapsed = false)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        collapseToggle = collapseToggle != null
            ? collapseToggle
            : gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
        collapseToggle.SetVisible(visible, resetCollapsed);
    }

    private void refreshCollapsedState()
    {
        collapseToggle?.RefreshIfCollapsed();
    }

    private void clearList()
    {
        if (listContent == null)
        {
            return;
        }

        for (var index = listContent.childCount - 1; index >= 0; index -= 1)
        {
            destroy(listContent.GetChild(index).gameObject);
        }

        listContent.sizeDelta = Vector2.zero;
    }

    private static string buildSelectedText(ProjectionViewModel projection)
    {
        if (projection.characterSelection.selections.Count <= 0)
        {
            return "已选择：无";
        }

        var definitionNames = projection.characterDefinitions.ToDictionary(
            definition => definition.definitionId,
            definition => string.IsNullOrWhiteSpace(definition.characterName)
                ? CardNameLocalizer.Localize(definition.definitionId)
                : definition.characterName,
            StringComparer.Ordinal);
        return "已选择：" + string.Join(
            "  /  ",
            projection.characterSelection.selections
                .OrderBy(selection => selection.playerNumericId)
                .Select(selection =>
                {
                    var name = definitionNames.TryGetValue(selection.characterDefinitionId, out var resolvedName)
                        ? resolvedName
                        : CardNameLocalizer.Localize(selection.characterDefinitionId);
                    return $"P{selection.playerNumericId}={name}";
                }));
    }

    private static string buildRaceText(IReadOnlyList<string> raceTags)
    {
        return raceTags.Count <= 0 ? "种族未知" : string.Join("/", raceTags);
    }

    private static string localizeFaction(string factionKey)
    {
        return factionKey switch
        {
            "TH" => "东方",
            "TM" => "型月",
            _ => string.IsNullOrWhiteSpace(factionKey) ? "阵营未知" : factionKey,
        };
    }

    private static void setText(TMP_Text? text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void setAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void destroy(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
}
