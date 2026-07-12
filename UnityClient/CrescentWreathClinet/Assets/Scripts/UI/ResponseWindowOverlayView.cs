using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class ResponseWindowOverlayView : MonoBehaviour
{
    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private GameClientNetworkController? networkController;

    [SerializeField]
    private BattlefieldUiTheme? theme;

    private RectTransform? windowRoot;
    private RectTransform? choiceContent;
    private TMP_Text? titleText;
    private TMP_Text? detailText;
    private CanvasGroup? canvasGroup;
    private Image? rootImage;
    private long? renderedResponseWindowNumericId;

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

    private void Awake()
    {
        setVisible(false);
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
        if (projection is null ||
            !projection.interaction.hasResponseWindow ||
            !projection.interaction.responseWindowNumericId.HasValue ||
            projection.interaction.responseWindowNumericId.Value <= 0)
        {
            setVisible(false);
            renderedResponseWindowNumericId = null;
            return;
        }

        setVisible(true);
        renderedResponseWindowNumericId = projection.interaction.responseWindowNumericId;
        clearChoices();

        var responder = projection.interaction.responseCurrentResponderPlayerNumericId;
        var isLocalResponder = responder.HasValue && responder.Value == projection.viewerPlayerNumericId;
        var isAwaitDefense = string.Equals(
            projection.interaction.pendingDamageResponseStageKey,
            "awaitDefense",
            System.StringComparison.Ordinal);

        setText(titleText, $"响应窗口 #{renderedResponseWindowNumericId}");
        setText(
            detailText,
            $"{buildDamageSentence(projection)}\n" +
            $"响应者：Player {responder?.ToString() ?? "-"}    防守玩家：Player {projection.interaction.pendingDamageDefenderPlayerNumericId?.ToString() ?? "-"}\n" +
            $"阶段：{projection.interaction.pendingDamageResponseStageKey}");

        if (!isLocalResponder)
        {
            createInfoText($"等待 Player {responder?.ToString() ?? "?"} 响应。");
            return;
        }

        if (!isAwaitDefense)
        {
            createInfoText("当前响应阶段不是 awaitDefense，正式 UI 暂不提供操作。");
            return;
        }

        createActionButton("不防御，承受伤害", 0, () =>
        {
            if (networkController is null || !networkController.TrySubmitResponseNo())
            {
                setText(detailText, networkController?.LastError ?? "尚未初始化正式网络控制器。");
            }
        });

        var localPlayer = projection.playerSummaries.FirstOrDefault(player =>
            player.playerNumericId == projection.viewerPlayerNumericId || player.isViewerPlayer);
        if (localPlayer is not null && hasCharm(localPlayer))
        {
            createInfoText("当前角色处于【魅惑】：本次伤害不能防御。", offsetIndex: 1);
            return;
        }

        var handCards = projection.handCards
            .Where(card => card.cardInstanceNumericId > 0)
            .ToList();
        if (handCards.Count == 0)
        {
            createInfoText("当前手牌为空，无法提交防御牌。", offsetIndex: 1);
            return;
        }

        for (var index = 0; index < handCards.Count; index += 1)
        {
            var card = handCards[index];
            var capturedCardId = card.cardInstanceNumericId;
            createActionButton(
                $"使用 {CardNameLocalizer.Localize(card.definitionId)} 防御",
                index + 1,
                () =>
                {
                    var declaredType = string.IsNullOrWhiteSpace(projection.interaction.pendingDamageTypeKey)
                        ? "physical"
                        : projection.interaction.pendingDamageTypeKey;
                    if (networkController is null ||
                        !networkController.TrySubmitDefenseFormal(declaredType, capturedCardId))
                    {
                        setText(detailText, networkController?.LastError ?? "防御请求未发送。");
                    }
                });
        }
    }

    private static string buildDamageSentence(ProjectionViewModel projection)
    {
        var sourceCharacterName = resolveCharacterDisplayName(
            projection,
            projection.interaction.pendingDamageSourceCharacterInstanceNumericId,
            projection.interaction.pendingDamageSourceCharacterDefinitionId,
            projection.interaction.pendingDamageSourcePlayerNumericId);
        var targetCharacterName = resolveCharacterDisplayName(
            projection,
            projection.interaction.pendingDamageTargetCharacterInstanceNumericId,
            projection.interaction.pendingDamageTargetCharacterDefinitionId,
            projection.interaction.pendingDamageDefenderPlayerNumericId);
        var sourceName = resolveDamageSourceDisplayName(projection);
        var damageValue = projection.interaction.pendingDamageBaseDamageValue?.ToString() ?? "?";
        var damageType = localizeDamageType(projection.interaction.pendingDamageTypeKey);
        return $"【{sourceCharacterName}】通过【{sourceName}】对【{targetCharacterName}】造成【{damageValue}】点{damageType}伤害。";
    }

    private static string resolveDamageSourceDisplayName(ProjectionViewModel projection)
    {
        if (!string.IsNullOrWhiteSpace(projection.interaction.pendingDamageSourceSkillKey))
        {
            var skillName = resolveSkillName(
                projection,
                projection.interaction.pendingDamageSourceCharacterDefinitionId,
                projection.interaction.pendingDamageSourceSkillKey);
            return string.IsNullOrWhiteSpace(skillName)
                ? projection.interaction.pendingDamageSourceSkillKey
                : skillName;
        }

        if (!string.IsNullOrWhiteSpace(projection.interaction.pendingDamageSourceCardDefinitionId))
        {
            return CardNameLocalizer.Localize(projection.interaction.pendingDamageSourceCardDefinitionId);
        }

        return string.IsNullOrWhiteSpace(projection.interaction.responseWindowOriginType)
            ? "伤害来源"
            : projection.interaction.responseWindowOriginType;
    }

    private static string resolveSkillName(
        ProjectionViewModel projection,
        string characterDefinitionId,
        string skillKey)
    {
        var definition = projection.characterDefinitions.FirstOrDefault(candidate =>
            string.Equals(candidate.definitionId, characterDefinitionId, System.StringComparison.Ordinal));
        var skill = definition?.skills.FirstOrDefault(candidate =>
            string.Equals(candidate.skillKey, skillKey, System.StringComparison.Ordinal));
        return skill?.skillName ?? string.Empty;
    }

    private static string resolveCharacterDisplayName(
        ProjectionViewModel projection,
        long? characterInstanceNumericId,
        string characterDefinitionId,
        long? fallbackPlayerNumericId)
    {
        var resolvedDefinitionId = characterDefinitionId;
        if (string.IsNullOrWhiteSpace(resolvedDefinitionId) && characterInstanceNumericId.HasValue)
        {
            var owner = projection.playerSummaries.FirstOrDefault(player =>
                player.activeCharacterInstanceNumericId == characterInstanceNumericId.Value);
            resolvedDefinitionId = owner?.activeCharacterDefinitionId ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(resolvedDefinitionId))
        {
            var definition = projection.characterDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.definitionId, resolvedDefinitionId, System.StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(definition?.characterName))
            {
                return definition.characterName;
            }

            return resolvedDefinitionId;
        }

        return fallbackPlayerNumericId.HasValue ? $"Player {fallbackPlayerNumericId.Value}" : "未知角色";
    }

    private static string localizeDamageType(string damageTypeKey)
    {
        return damageTypeKey switch
        {
            "physical" => "体术",
            "spell" => "咒术",
            "direct" => "直接",
            _ => string.IsNullOrWhiteSpace(damageTypeKey) ? "未知类型" : damageTypeKey,
        };
    }

    private static bool hasCharm(ProjectionPlayerSummaryViewModel player)
    {
        return player.playerStatusKeys.Any(isCharmStatusKey) ||
               player.activeCharacterStatusKeys.Any(isCharmStatusKey);
    }

    private static bool isCharmStatusKey(string statusKey)
    {
        return string.Equals(statusKey, "Charm", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(statusKey, "status:charm", System.StringComparison.OrdinalIgnoreCase);
    }

    private void ensureVisualTree()
    {
        if (windowRoot is not null)
        {
            return;
        }

        ensureCanvasGroup();
        rootImage = GetComponent<Image>();
        if (rootImage is null)
        {
            rootImage = gameObject.AddComponent<Image>();
        }

        rootImage.color = new Color(0.015f, 0.025f, 0.035f, 0.96f);
        rootImage.raycastTarget = false;

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

        windowRoot = GetComponent<RectTransform>();
        if (windowRoot is null)
        {
            windowRoot = gameObject.AddComponent<RectTransform>();
        }

        titleText = createText("ResponseTitle", transform, "响应窗口", 18f, TextAlignmentOptions.Center);
        setAnchors(titleText.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));
        detailText = createText("ResponseDetail", transform, string.Empty, 12f, TextAlignmentOptions.Center);
        setAnchors(detailText.rectTransform, new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.88f));

        var viewportObject = new GameObject("ResponseChoiceViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        setAnchors(viewport, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.64f));
        viewportObject.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.78f);
        choiceContent = new GameObject("ResponseChoiceContent", typeof(RectTransform)).GetComponent<RectTransform>();
        choiceContent.transform.SetParent(viewportObject.transform, false);
        choiceContent.anchorMin = new Vector2(0f, 1f);
        choiceContent.anchorMax = new Vector2(1f, 1f);
        choiceContent.pivot = new Vector2(0.5f, 1f);
        choiceContent.offsetMin = Vector2.zero;
        choiceContent.offsetMax = Vector2.zero;
        var scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = choiceContent;
        scrollRect.horizontal = false;
    }

    private void createActionButton(string label, int index, UnityEngine.Events.UnityAction clicked)
    {
        if (choiceContent is null)
        {
            return;
        }

        var buttonObject = new GameObject($"ResponseAction_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(choiceContent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 42f);
        rect.anchoredPosition = new Vector2(0f, -index * 48f);
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.38f, 0.46f, 0.96f);
        var button = buttonObject.GetComponent<Button>();
        button.interactable = networkController?.IsRequestInFlight != true;
        button.onClick.AddListener(clicked);
        var labelText = createText("Label", buttonObject.transform, label, 13f, TextAlignmentOptions.Center);
        setAnchors(labelText.rectTransform, Vector2.zero, Vector2.one);
        choiceContent.sizeDelta = new Vector2(0f, Mathf.Max(choiceContent.sizeDelta.y, (index + 1) * 48f));
    }

    private void createInfoText(string value, int offsetIndex = 0)
    {
        if (choiceContent is null)
        {
            return;
        }

        var text = createText($"Info_{offsetIndex}", choiceContent, value, 13f, TextAlignmentOptions.Center);
        text.color = theme?.MutedText ?? Color.gray;
        text.rectTransform.anchorMin = new Vector2(0f, 1f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);
        text.rectTransform.sizeDelta = new Vector2(0f, 38f);
        text.rectTransform.anchoredPosition = new Vector2(0f, -offsetIndex * 44f);
        choiceContent.sizeDelta = new Vector2(0f, Mathf.Max(choiceContent.sizeDelta.y, (offsetIndex + 1) * 44f));
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
    }

    private void setVisible(bool visible)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(visible);
        }

        var currentRootImage = rootImage == null ? GetComponent<Image>() : rootImage;
        rootImage = currentRootImage;
        if (currentRootImage != null)
        {
            currentRootImage.enabled = visible;
            currentRootImage.raycastTarget = visible;
        }

        var currentCanvasGroup = ensureCanvasGroup();
        currentCanvasGroup.alpha = visible ? 1f : 0f;
        currentCanvasGroup.interactable = visible;
        currentCanvasGroup.blocksRaycasts = visible;
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

    private static void setText(TMP_Text? text, string value)
    {
        if (text is not null)
        {
            text.text = value;
        }
    }

    private static void setAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
}
