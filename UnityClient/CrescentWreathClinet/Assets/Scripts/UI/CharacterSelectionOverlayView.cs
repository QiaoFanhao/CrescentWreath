using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
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
    private RectTransform? listContent;
    private TMP_Text? titleText;
    private TMP_Text? detailText;
    private TMP_Text? selectedText;

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
            return;
        }

        setVisible(true);
        var currentSelectingPlayerId = projection.characterSelection.currentSelectingPlayerNumericId;
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
    }

    private void renderCharacterButtons(ProjectionViewModel projection, bool isViewerSelecting)
    {
        clearList();
        buttonByDefinitionId.Clear();
        var selectedDefinitionIds = new HashSet<string>(
            projection.characterSelection.selections.Select(selection => selection.characterDefinitionId),
            StringComparer.Ordinal);
        var definitions = projection.characterDefinitions
            .OrderByDescending(definition => definition.isImplemented)
            .ThenBy(definition => definition.definitionId, StringComparer.Ordinal)
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
        var buttonObject = new GameObject(
            $"Character_{definition.definitionId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 62f);
        rect.anchoredPosition = new Vector2(0f, -index * 68f);

        var image = buttonObject.GetComponent<Image>();
        image.color = interactable
            ? new Color(0.13f, 0.34f, 0.42f, 0.96f)
            : new Color(0.12f, 0.12f, 0.14f, 0.74f);
        var button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(() => submitCharacter(definition.definitionId));

        var stateText = alreadySelected
            ? "（已选择）"
            : definition.isImplemented ? string.Empty : "（占位）";
        var title = createText(
            "Title",
            buttonObject.transform,
            $"{definition.definitionId} {definition.characterName} {stateText}",
            15f,
            TextAlignmentOptions.Left);
        setAnchors(title.rectTransform, new Vector2(0f, 0.52f), Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -2f));

        var detail = createText(
            "Detail",
            buttonObject.transform,
            $"HP {definition.baseMaxHp} / {localizeFaction(definition.factionKey)} / {buildRaceText(definition.raceTags)} / 技能 {definition.skills.Count}",
            11.5f,
            TextAlignmentOptions.Left);
        detail.color = interactable ? Color.white : new Color(0.62f, 0.66f, 0.7f, 1f);
        setAnchors(detail.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(10f, 3f), new Vector2(-10f, 0f));

        parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, (index + 1) * 68f));
        return button;
    }

    private void submitCharacter(string definitionId)
    {
        if (networkController == null ||
            !networkController.TrySubmitCharacterSelection(definitionId))
        {
            setText(detailText, networkController == null ? "角色选择请求未发送。" : networkController.LastError);
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

        if (titleText != null)
        {
            return;
        }

        for (var index = transform.childCount - 1; index >= 0; index -= 1)
        {
            destroy(transform.GetChild(index).gameObject);
        }

        titleText = createText("Title", transform, "寮€灞€瑙掕壊閫夋嫨", 20f, TextAlignmentOptions.Center);
        setAnchors(titleText.rectTransform, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        detailText = createText("Detail", transform, string.Empty, 13f, TextAlignmentOptions.Center);
        setAnchors(detailText.rectTransform, new Vector2(0.04f, 0.81f), new Vector2(0.96f, 0.89f), Vector2.zero, Vector2.zero);
        selectedText = createText("Selected", transform, string.Empty, 11.5f, TextAlignmentOptions.Left);
        setAnchors(selectedText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.18f), Vector2.zero, Vector2.zero);

        var viewportObject = new GameObject("CharacterViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        setAnchors(viewport, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.80f), Vector2.zero, Vector2.zero);
        viewportObject.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.76f);

        listContent = new GameObject("CharacterListContent", typeof(RectTransform)).GetComponent<RectTransform>();
        listContent.transform.SetParent(viewportObject.transform, false);
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.offsetMin = Vector2.zero;
        listContent.offsetMax = Vector2.zero;
        listContent.sizeDelta = Vector2.zero;

        var scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = listContent;
        scrollRect.horizontal = false;
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

    private void setVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(visible);
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
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

        return "已选择：" + string.Join(
            "  /  ",
            projection.characterSelection.selections
                .OrderBy(selection => selection.playerNumericId)
                .Select(selection => $"P{selection.playerNumericId}={selection.characterDefinitionId}"));
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
