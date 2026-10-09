using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class InputContextOverlayView : MonoBehaviour
{
    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private GameClientNetworkController? networkController;

    [SerializeField]
    private BattlefieldUiTheme? theme;

    private readonly List<string> selectedChoiceKeys = new();
    private readonly Dictionary<string, Button> buttonByChoiceKey = new();
    private RectTransform? contentRoot;
    private RectTransform? choiceContent;
    private TMP_Text? titleText;
    private TMP_Text? detailText;
    private TMP_Text? selectionText;
    private Button? submitButton;
    private Button? clearButton;
    private CanvasGroup? canvasGroup;
    private Image? rootImage;
    private OverlayCollapseToggle? collapseToggle;
    private ProjectionViewModel? currentProjection;
    private long? renderedInputContextNumericId;
    private Coroutine? delayedRenderCoroutine;
    private int delayedRenderVersion;
    private bool isCurrentMultiSelect;
    private int currentMaxSelectionCount = 1;
    private int currentMinSelectionCount = 1;

    private void Awake()
    {
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
        render(projectionViewState?.Current);
    }

    private void OnDisable()
    {
        if (projectionViewState is not null)
        {
            projectionViewState.ProjectionChanged -= render;
        }

        if (delayedRenderCoroutine is not null)
        {
            StopCoroutine(delayedRenderCoroutine);
            delayedRenderCoroutine = null;
        }
    }

    private void subscribe()
    {
        if (projectionViewState is null)
        {
            return;
        }

        projectionViewState.ProjectionChanged -= render;
        projectionViewState.ProjectionChanged += render;
    }

    private void render(ProjectionViewModel? projection)
    {
        ensureVisualTree();
        if (projection is null || !projection.interaction.hasInputContext)
        {
            setVisible(false);
            currentProjection = null;
            renderedInputContextNumericId = null;
            selectedChoiceKeys.Clear();
            return;
        }

        currentProjection = projection;
        var isNewInputContext = renderedInputContextNumericId != projection.interaction.inputContextNumericId;
        if (BattlefieldInteractionPresentationGate.ShouldDelayInteraction(projection, isNewInputContext, out var remainingSeconds))
        {
            setVisible(false);
            scheduleDelayedRender(remainingSeconds);
            return;
        }

        setVisible(true, isNewInputContext);
        if (renderedInputContextNumericId != projection.interaction.inputContextNumericId)
        {
            selectedChoiceKeys.Clear();
            renderedInputContextNumericId = projection.interaction.inputContextNumericId;
        }

        var isRequired = projection.interaction.isViewerRequiredPlayerForInput ||
                         projection.interaction.inputRequiredPlayerNumericId == projection.viewerPlayerNumericId ||
                         projection.interaction.inputRequiredPlayerNumericIds.Contains(projection.viewerPlayerNumericId);
        var hasSubmitted = projection.interaction.isViewerSubmittedInput ||
                           projection.interaction.inputSubmittedPlayerNumericIds.Contains(projection.viewerPlayerNumericId);
        isCurrentMultiSelect = ChoicePresentationResolver.IsMultiSelectInput(projection.interaction);
        currentMinSelectionCount = ChoicePresentationResolver.GetSuggestedMinSelectionCount(projection.interaction);
        currentMaxSelectionCount = ChoicePresentationResolver.GetSuggestedMaxSelectionCount(projection.interaction);
        if (!isCurrentMultiSelect && selectedChoiceKeys.Count > 1)
        {
            selectedChoiceKeys.RemoveRange(1, selectedChoiceKeys.Count - 1);
        }

        var description = InputContextDescriptionResolver.Resolve(projection);
        setText(titleText, description.title);
        if (!isRequired)
        {
            setText(detailText, description.waitingText);
            clearChoices();
            setSubmitVisible(false);
            refreshCollapsedState();
            return;
        }

        if (hasSubmitted)
        {
            setText(detailText, description.submittedText);
            clearChoices();
            setSubmitVisible(false);
            refreshCollapsedState();
            return;
        }

        setText(detailText, description.detail);
        renderChoices(projection);
        setSubmitVisible(true);
        refreshSubmitState();
        refreshCollapsedState();
    }

    private void renderChoices(ProjectionViewModel projection)
    {
        clearChoices();
        buttonByChoiceKey.Clear();
        var choices = projection.interaction.inputChoiceKeys
            .Select(choiceKey => ChoicePresentationResolver.Resolve(projection, choiceKey))
            .ToList();

        for (var index = 0; index < choices.Count; index += 1)
        {
            var choice = choices[index];
            var button = createChoiceButton(choiceContent!, choice, index);
            buttonByChoiceKey[choice.choiceKey] = button;
        }

        refreshChoiceHighlights();
    }

    private Button createChoiceButton(RectTransform parent, ChoicePresentationViewModel choice, int index)
    {
        var buttonObject = new GameObject($"Choice_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 46f);
        rect.anchoredPosition = new Vector2(0f, -index * 50f);

        var image = buttonObject.GetComponent<Image>();
        image.color = choice.isDeclineChoice
            ? new Color(0.28f, 0.24f, 0.16f, 0.96f)
            : new Color(0.12f, 0.29f, 0.36f, 0.96f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => toggleChoice(choice.choiceKey));

        var title = createText("Title", buttonObject.transform, choice.title, 13f, TextAlignmentOptions.Left);
        setAnchors(title.rectTransform, new Vector2(0f, 0.48f), Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, -2f));
        var detail = createText("Detail", buttonObject.transform, choice.detail, 10.5f, TextAlignmentOptions.Left);
        detail.color = theme?.MutedText ?? new Color(0.65f, 0.72f, 0.78f, 1f);
        setAnchors(detail.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(8f, 2f), new Vector2(-8f, 0f));
        parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, (index + 1) * 50f));
        return button;
    }

    private void toggleChoice(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            return;
        }

        var existingIndex = selectedChoiceKeys.FindIndex(selected => selected == choiceKey);
        if (existingIndex >= 0)
        {
            selectedChoiceKeys.RemoveAt(existingIndex);
        }
        else if (isCurrentMultiSelect)
        {
            if (ChoicePresentationResolver.IsDeclineChoice(choiceKey))
            {
                selectedChoiceKeys.Clear();
                selectedChoiceKeys.Add(choiceKey);
            }
            else if (currentMaxSelectionCount == int.MaxValue || selectedChoiceKeys.Count < currentMaxSelectionCount)
            {
                selectedChoiceKeys.RemoveAll(ChoicePresentationResolver.IsDeclineChoice);
                selectedChoiceKeys.Add(choiceKey);
            }
        }
        else
        {
            selectedChoiceKeys.Clear();
            selectedChoiceKeys.Add(choiceKey);
        }

        refreshChoiceHighlights();
        refreshSubmitState();
    }

    private void submitSelection()
    {
        if (BattlefieldInteractionPresentationGate.IsPresentationBusy)
        {
            return;
        }

        if (networkController is null || selectedChoiceKeys.Count == 0)
        {
            return;
        }

        if (selectedChoiceKeys.Count == 1 &&
            ChoicePresentationResolver.IsDeclineChoice(selectedChoiceKeys[0]))
        {
            networkController.TrySubmitInputChoice(selectedChoiceKeys[0]);
        }
        else if (isCurrentMultiSelect)
        {
            networkController.TrySubmitInputChoices(selectedChoiceKeys.ToList());
        }
        else
        {
            networkController.TrySubmitInputChoice(selectedChoiceKeys[0]);
        }
    }

    private void clearSelection()
    {
        selectedChoiceKeys.Clear();
        refreshChoiceHighlights();
        refreshSubmitState();
    }

    private void refreshChoiceHighlights()
    {
        foreach (var pair in buttonByChoiceKey)
        {
            var image = pair.Value.GetComponent<Image>();
            if (image is null)
            {
                continue;
            }

            image.color = selectedChoiceKeys.Contains(pair.Key)
                ? new Color(0.92f, 0.66f, 0.15f, 0.96f)
                : new Color(0.12f, 0.29f, 0.36f, 0.96f);
        }
    }

    private void refreshSubmitState()
    {
        setText(selectionText, buildSelectionText());
        if (submitButton is not null)
        {
            var selectedDeclineOnly =
                selectedChoiceKeys.Count == 1 &&
                ChoicePresentationResolver.IsDeclineChoice(selectedChoiceKeys[0]);
            submitButton.interactable =
                (selectedDeclineOnly || selectedChoiceKeys.Count >= currentMinSelectionCount) &&
                selectedChoiceKeys.Count > 0 &&
                networkController?.IsRequestInFlight != true;
        }

        if (clearButton is not null)
        {
            clearButton.interactable = selectedChoiceKeys.Count > 0;
        }
    }

    private string buildSelectionText()
    {
        if (selectedChoiceKeys.Count == 0)
        {
            return isCurrentMultiSelect ? "请选择一个或多个选项。" : "请选择一个选项。";
        }

        var maxText = currentMaxSelectionCount == int.MaxValue ? "不限" : currentMaxSelectionCount.ToString();
        var minText = currentMinSelectionCount > 1 ? $"，至少 {currentMinSelectionCount}" : string.Empty;
        if (currentProjection is null)
        {
            return isCurrentMultiSelect
                ? $"已选 {selectedChoiceKeys.Count}/{maxText}{minText}"
                : $"已选：{selectedChoiceKeys[0]}";
        }

        var selectedNames = selectedChoiceKeys
            .Select(choiceKey => ChoicePresentationResolver.Resolve(currentProjection, choiceKey).title)
            .ToList();
        return isCurrentMultiSelect
            ? $"已选 {selectedChoiceKeys.Count}/{maxText}{minText}：{string.Join("、", selectedNames)}"
            : $"已选：{selectedNames[0]}";
    }

    private void ensureVisualTree()
    {
        if (contentRoot is not null)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootImage = GetComponent<Image>();

        var rect = GetComponent<RectTransform>();
        if (rect is null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        contentRoot = rect;
        for (var index = transform.childCount - 1; index >= 0; index -= 1)
        {
            var child = transform.GetChild(index).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        titleText = createText("InputTitle", transform, "输入选择", 18f, TextAlignmentOptions.Center);
        setAnchors(titleText.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        detailText = createText("InputDetail", transform, string.Empty, 12f, TextAlignmentOptions.Center);
        setAnchors(detailText.rectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);

        var viewportObject = new GameObject("ChoiceViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        setAnchors(viewport, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.76f), Vector2.zero, Vector2.zero);
        viewportObject.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.028f, 0.78f);
        choiceContent = new GameObject("ChoiceContent", typeof(RectTransform)).GetComponent<RectTransform>();
        choiceContent.transform.SetParent(viewportObject.transform, false);
        choiceContent.anchorMin = new Vector2(0f, 1f);
        choiceContent.anchorMax = new Vector2(1f, 1f);
        choiceContent.pivot = new Vector2(0.5f, 1f);
        choiceContent.offsetMin = Vector2.zero;
        choiceContent.offsetMax = Vector2.zero;
        choiceContent.sizeDelta = new Vector2(0f, 0f);
        var scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = choiceContent;
        scrollRect.horizontal = false;

        selectionText = createText("SelectionText", transform, string.Empty, 12f, TextAlignmentOptions.Left);
        setAnchors(selectionText.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.56f, 0.2f), Vector2.zero, Vector2.zero);
        submitButton = createButton("SubmitButton", transform, "提交选择", new Vector2(0.58f, 0.08f), new Vector2(0.78f, 0.2f), submitSelection);
        clearButton = createButton("ClearButton", transform, "清空选择", new Vector2(0.8f, 0.08f), new Vector2(0.95f, 0.2f), clearSelection);
        collapseToggle = gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
    }

    private void setVisible(bool visible, bool resetCollapsed = false)
    {
        collapseToggle = collapseToggle != null
            ? collapseToggle
            : gameObject.GetComponent<OverlayCollapseToggle>() ?? gameObject.AddComponent<OverlayCollapseToggle>();
        collapseToggle.Configure(theme);
        collapseToggle.SetVisible(visible, resetCollapsed);

        if (!visible)
        {
            return;
        }

        ensureRootImage();
        ensureCanvasGroup();
    }

    private void refreshCollapsedState()
    {
        collapseToggle?.RefreshIfCollapsed();
    }

    private CanvasGroup ensureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private Image? ensureRootImage()
    {
        if (rootImage == null)
        {
            rootImage = GetComponent<Image>();
        }

        return rootImage;
    }


    private void scheduleDelayedRender(float delaySeconds)
    {
        delayedRenderVersion += 1;
        var version = delayedRenderVersion;
        if (delayedRenderCoroutine is not null)
        {
            StopCoroutine(delayedRenderCoroutine);
        }

        delayedRenderCoroutine = StartCoroutine(delayedRender(version, delaySeconds));
    }

    private IEnumerator delayedRender(int version, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delaySeconds));
        delayedRenderCoroutine = null;
        if (version != delayedRenderVersion)
        {
            yield break;
        }

        render(projectionViewState?.Current);
    }
    private void clearChoices()
    {
        if (choiceContent is null)
        {
            return;
        }

        for (var index = choiceContent.childCount - 1; index >= 0; index -= 1)
        {
            Destroy(choiceContent.GetChild(index).gameObject);
        }

        choiceContent.sizeDelta = Vector2.zero;
        buttonByChoiceKey.Clear();
    }

    private void setSubmitVisible(bool visible)
    {
        selectionText?.gameObject.SetActive(visible);
        submitButton?.gameObject.SetActive(visible);
        clearButton?.gameObject.SetActive(visible);
        choiceContent?.gameObject.SetActive(visible);
    }

    private TMP_Text createText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = theme?.NormalText ?? Color.white;
        text.raycastTarget = false;
        if (theme?.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private Button createButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction clicked)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        setAnchors(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        buttonObject.GetComponent<Image>().color = new Color(0.14f, 0.36f, 0.44f, 0.95f);
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(clicked);
        var labelText = createText("Label", buttonObject.transform, label, 13f, TextAlignmentOptions.Center);
        setAnchors(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, -1f));
        return button;
    }

    private static void setText(TMP_Text? text, string value)
    {
        if (text is not null)
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
}
}
