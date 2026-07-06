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
public sealed class BattlefieldProjectionPresenter : MonoBehaviour
{
    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private BattlefieldLayoutRoot? layoutRoot;

    [SerializeField]
    private BattlefieldView? battlefieldView;

    [SerializeField]
    private CardDetailPanel? cardDetailPanel;

    [SerializeField]
    private ZoneCardListOverlayView? zoneCardListOverlayView;

    [SerializeField]
    private TMP_Text? statusText;

    [SerializeField]
    private TMP_FontAsset? primaryFont;

    [SerializeField]
    private GameClientNetworkController? networkController;

    private readonly Dictionary<BattlefieldSeatDirection, BattlefieldPlayerSeatView> seatViewByDirection = new();
    private readonly Dictionary<long, GameObject> localHandUiByCardInstanceId = new();
    private TMP_Text? currentManaText;
    private TMP_Text? currentSkillText;
    private TMP_Text? currentSigilText;
    private bool isAnomalyResolveConfirmOpen;
    private ProjectionViewModel? latestProjection;

    public BattlefieldView? View => battlefieldView;

    public void Configure(
        ProjectionViewState newProjectionViewState,
        BattlefieldLayoutRoot newLayoutRoot,
        BattlefieldView newBattlefieldView,
        CardDetailPanel newCardDetailPanel,
        TMP_Text? newStatusText)
    {
        projectionViewState = newProjectionViewState;
        layoutRoot = newLayoutRoot;
        battlefieldView = newBattlefieldView;
        cardDetailPanel = newCardDetailPanel;
        statusText = newStatusText;
        rebuildSeatViews();
    }

    private void OnEnable()
    {
        ensureReferences();
        if (projectionViewState is not null)
        {
            projectionViewState.ProjectionChanged += render;
            if (projectionViewState.Current is not null)
            {
                render(projectionViewState.Current);
            }
        }
    }

    private void OnDisable()
    {
        if (projectionViewState is not null)
        {
            projectionViewState.ProjectionChanged -= render;
        }

        if (battlefieldView is not null)
        {
            battlefieldView.CardClicked -= handleCardClicked;
            battlefieldView.CardDoubleClicked -= handleCardDoubleClicked;
        }
    }

    private void ensureReferences()
    {
        var root = GetComponentInParent<GameClientRoot>();
        if (root is not null)
        {
            projectionViewState ??= root.ProjectionViewState;
            layoutRoot ??= root.BattlefieldLayoutRoot;
            networkController ??= root.GameClientNetworkController;
        }

        if (layoutRoot is null)
        {
            layoutRoot = GetComponentInParent<BattlefieldLayoutRoot>();
        }

        primaryFont ??= layoutRoot?.Theme?.PrimaryFont;

        if (layoutRoot?.TableWorld is not null)
        {
            var demo = layoutRoot.TableWorld.GetComponent<BattlefieldWorldCardDemoController>();
            if (demo is not null)
            {
                demo.enabled = false;
            }

            battlefieldView ??= layoutRoot.TableWorld.GetComponent<BattlefieldView>();
            if (battlefieldView is null)
            {
                battlefieldView = layoutRoot.TableWorld.gameObject.AddComponent<BattlefieldView>();
            }

            battlefieldView.Configure(layoutRoot.TableWorld);
            battlefieldView.CardClicked -= handleCardClicked;
            battlefieldView.CardClicked += handleCardClicked;
            battlefieldView.CardDoubleClicked -= handleCardDoubleClicked;
            battlefieldView.CardDoubleClicked += handleCardDoubleClicked;
        }

        if (layoutRoot?.ScreenHudCanvas is not null)
        {
            var detailText = layoutRoot.ScreenHudCanvas.transform
                .Find("CardDetailPanel/Label")
                ?.GetComponent<TMP_Text>();
            if (detailText is not null)
            {
                cardDetailPanel ??= detailText.GetComponentInParent<CardDetailPanel>();
                if (cardDetailPanel is null)
                {
                    cardDetailPanel = detailText.transform.parent.gameObject.AddComponent<CardDetailPanel>();
                }

                cardDetailPanel.Configure(detailText);
            }

            statusText ??= layoutRoot.ScreenHudCanvas.transform
                .Find("TurnInfoPanel/Label")
                ?.GetComponent<TMP_Text>();
        }

        if (layoutRoot?.OverlayCanvas is not null)
        {
            zoneCardListOverlayView ??= layoutRoot.OverlayCanvas
                .GetComponentInChildren<ZoneCardListOverlayView>(true);
        }

        rebuildSeatViews();
    }

    private void rebuildSeatViews()
    {
        seatViewByDirection.Clear();
        if (layoutRoot is null)
        {
            return;
        }

        foreach (var seatView in layoutRoot.GetComponentsInChildren<BattlefieldPlayerSeatView>(true))
        {
            seatViewByDirection[seatView.Direction] = seatView;
        }
    }

    private void render(ProjectionViewModel projection)
    {
        ensureReferences();
        latestProjection = projection;
        if (battlefieldView is null)
        {
            return;
        }

        if (!projection.hasStateProjection)
        {
            battlefieldView.ClearAll();
            cardDetailPanel?.ShowMessage("Waiting for projection.");
            setStatus("Waiting for projection.");
            return;
        }

        var playerById = projection.playerSummaries
            .Where(player => player.playerNumericId > 0)
            .GroupBy(player => player.playerNumericId)
            .ToDictionary(group => group.Key, group => group.First());
        var seatResult = SeatLayoutResolver.Resolve(
            projection.viewerPlayerNumericId,
            projection.turnOrderPlayerNumericIds,
            projection.playerSummaries);

        if (!seatResult.isResolved)
        {
            setStatus($"Seat layout failed: {seatResult.failureReasonKey}");
        }
        else
        {
            bindSeat(BattlefieldSeatDirection.bottom, seatResult.bottomPlayerNumericId, projection, playerById);
            bindSeat(BattlefieldSeatDirection.top, seatResult.topPlayerNumericId, projection, playerById);
            bindSeat(BattlefieldSeatDirection.left, seatResult.leftPlayerNumericId, projection, playerById);
            bindSeat(BattlefieldSeatDirection.right, seatResult.rightPlayerNumericId, projection, playerById);
            setStatus($"Turn {projection.turnNumber} / {projection.currentPhase} / P{projection.currentPlayerNumericId?.ToString() ?? "-"}\n{buildTeamSummaryText(projection)}");
        }

        renderPublicZones(projection);
        renderPlayerCharacters(seatResult, playerById);
        renderPlayerFields(seatResult, playerById);
        renderPlayerStatuses(seatResult, playerById);
        renderCurrentPlayerMirror(projection, playerById);
        renderCurrentPlayerResources(projection, playerById);
        renderLocalHand(projection);
        renderActionPanel(projection);
    }

    private void bindSeat(
        BattlefieldSeatDirection direction,
        long? playerNumericId,
        ProjectionViewModel projection,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        if (!seatViewByDirection.TryGetValue(direction, out var seatView))
        {
            return;
        }

        ProjectionPlayerSummaryViewModel? player = null;
        if (playerNumericId.HasValue)
        {
            playerById.TryGetValue(playerNumericId.Value, out player);
        }

        seatView.Bind(
            player,
            player is null ? null : projection.teamSummaries.FirstOrDefault(team => team.teamNumericId == player.teamNumericId),
            playerNumericId.HasValue && projection.currentPlayerNumericId == playerNumericId.Value,
            playerNumericId.HasValue && projection.viewerPlayerNumericId == playerNumericId.Value);
    }

    private void renderPublicZones(ProjectionViewModel projection)
    {
        battlefieldView!.ReconcileZone(
            "publicTreasureDeck",
            BattlefieldCardViewModelMapper.CreateDeckBackStack(
                "publicTreasureDeck",
                -100000,
                "T001",
                projection.publicTreasureDeckCount));

        for (var index = 0; index < 6; index += 1)
        {
            var zoneKey = $"summonZone:{index + 1}";
            if (index < projection.summonZoneCards.Count)
            {
                var summonCardModel = BattlefieldCardViewModelMapper.FromProjectionCard(
                    projection.summonZoneCards[index],
                    readOnly: true);
                summonCardModel.zoneKey = zoneKey;
                battlefieldView.ReconcileZone(
                    zoneKey,
                    new[] { summonCardModel });
            }
            else
            {
                battlefieldView.ClearZone(zoneKey);
            }
        }

        var sakuraModels = BattlefieldCardViewModelMapper.CreatePublicPilePreview(
                "sakuraCakeZone",
                -110000,
                projection.sakuraCakeCards,
                projection.sakuraCakeDeckCount,
                "S001")
            .ToList();
        foreach (var sakuraModel in sakuraModels)
        {
            sakuraModel.zoneKey = "sakuraCakeZone";
        }

        battlefieldView.ReconcileZone("sakuraCakeZone", sakuraModels);

        battlefieldView.ReconcileZone(
            "gapZone",
            BattlefieldCardViewModelMapper.CreatePublicPilePreview(
                "gapZone",
                -115000,
                projection.gapZoneCards,
                projection.gapZoneCards.Count,
                "T001"));

        battlefieldView.ReconcileZone(
            "anomalyDeck",
            BattlefieldCardViewModelMapper.CreateDeckBackStack(
                "anomalyDeck",
                -120000,
                "A001",
                projection.currentAnomaly.remainingDeckCount));

        if (!string.IsNullOrWhiteSpace(projection.currentAnomaly.definitionId))
        {
            battlefieldView.ReconcileZone(
                "currentAnomaly",
                new[]
                {
                    new WorldCardViewModel
                    {
                        cardInstanceNumericId = -130000,
                        definitionId = projection.currentAnomaly.definitionId,
                        zoneKey = "currentAnomaly",
                        isFaceUp = true,
                        isReadOnly = true,
                        displayMode = WorldCardDisplayMode.readOnlyMirror,
                    },
                });
        }
        else
        {
            battlefieldView.ClearZone("currentAnomaly");
        }
    }

    private void renderPlayerFields(
        SeatLayoutResult seatResult,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        renderSeatField("playerField:bottom", seatResult.bottomPlayerNumericId, playerById);
        renderSeatField("playerField:top", seatResult.topPlayerNumericId, playerById);
        renderSeatField("playerField:left", seatResult.leftPlayerNumericId, playerById);
        renderSeatField("playerField:right", seatResult.rightPlayerNumericId, playerById);
        renderSeatDeckAndDiscard("bottom", seatResult.bottomPlayerNumericId, playerById, -200000);
        renderSeatDeckAndDiscard("top", seatResult.topPlayerNumericId, playerById, -210000);
        renderSeatDeckAndDiscard("left", seatResult.leftPlayerNumericId, playerById, -220000);
        renderSeatDeckAndDiscard("right", seatResult.rightPlayerNumericId, playerById, -230000);
    }

    private void renderPlayerCharacters(
        SeatLayoutResult seatResult,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        renderSeatCharacter("playerCharacter:bottom", seatResult.bottomPlayerNumericId, playerById, -400000);
        renderSeatCharacter("playerCharacter:top", seatResult.topPlayerNumericId, playerById, -410000);
        renderSeatCharacter("playerCharacter:left", seatResult.leftPlayerNumericId, playerById, -420000);
        renderSeatCharacter("playerCharacter:right", seatResult.rightPlayerNumericId, playerById, -430000);
    }

    private void renderSeatCharacter(
        string zoneViewKey,
        long? playerNumericId,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById,
        long syntheticBase)
    {
        if (!playerNumericId.HasValue || !playerById.TryGetValue(playerNumericId.Value, out var player) ||
            string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            return;
        }

        var primaryDefinitionId = normalizeCharacterCardDefinitionId(player.activeCharacterDefinitionId);
        if (string.IsNullOrWhiteSpace(primaryDefinitionId))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            return;
        }

        var definitionIds = new List<string> { primaryDefinitionId };
        var extensionDefinitionId = getCharacterExtensionDefinitionId(primaryDefinitionId);
        if (!string.IsNullOrWhiteSpace(extensionDefinitionId))
        {
            definitionIds.Add(extensionDefinitionId);
        }

        var models = new List<WorldCardViewModel>(definitionIds.Count);
        for (var index = 0; index < definitionIds.Count; index += 1)
        {
            models.Add(new WorldCardViewModel
            {
                cardInstanceNumericId = syntheticBase - player.playerNumericId * 100 - index,
                definitionId = definitionIds[index],
                ownerPlayerNumericId = player.playerNumericId,
                zoneKey = zoneViewKey,
                isFaceUp = true,
                isReadOnly = true,
                displayMode = WorldCardDisplayMode.readOnlyMirror,
            });
        }

        battlefieldView!.ReconcileZone(zoneViewKey, models);
    }

    private static string getCharacterExtensionDefinitionId(string characterDefinitionId)
    {
        return characterDefinitionId.Trim().ToUpperInvariant() switch
        {
            "C017" => "C017-1",
            "C026" => "C026-1",
            _ => string.Empty,
        };
    }

    private static string normalizeCharacterCardDefinitionId(string characterDefinitionId)
    {
        var value = string.IsNullOrWhiteSpace(characterDefinitionId)
            ? string.Empty
            : characterDefinitionId.Trim().ToUpperInvariant();

        for (var index = 0; index <= value.Length - 4; index += 1)
        {
            if (value[index] != 'C' ||
                !char.IsDigit(value[index + 1]) ||
                !char.IsDigit(value[index + 2]) ||
                !char.IsDigit(value[index + 3]))
            {
                continue;
            }

            return value.Substring(index, 4);
        }

        return string.Empty;
    }

    private void renderPlayerStatuses(
        SeatLayoutResult seatResult,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        renderSeatStatuses("playerStatus:bottom", seatResult.bottomPlayerNumericId, playerById, -300000);
        renderSeatStatuses("playerStatus:top", seatResult.topPlayerNumericId, playerById, -310000);
        renderSeatStatuses("playerStatus:left", seatResult.leftPlayerNumericId, playerById, -320000);
        renderSeatStatuses("playerStatus:right", seatResult.rightPlayerNumericId, playerById, -330000);
    }

    private void renderSeatStatuses(
        string zoneViewKey,
        long? playerNumericId,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById,
        long syntheticBase)
    {
        if (!playerNumericId.HasValue || !playerById.TryGetValue(playerNumericId.Value, out var player))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            return;
        }

        var statusModels = new List<WorldCardViewModel>();
        var statusIndex = 0;
        foreach (var statusKey in player.activeCharacterStatusKeys
                     .Select(normalizeStatusDefinitionId)
                     .Where(definitionId => !string.IsNullOrWhiteSpace(definitionId))
                     .Distinct())
        {
            statusModels.Add(new WorldCardViewModel
            {
                cardInstanceNumericId = syntheticBase - player.playerNumericId * 100 - statusIndex,
                definitionId = statusKey,
                ownerPlayerNumericId = player.playerNumericId,
                zoneKey = zoneViewKey,
                isFaceUp = true,
                isReadOnly = true,
                isDefensePlacedOnField = true,
                displayMode = WorldCardDisplayMode.readOnlyMirror,
            });
            statusIndex += 1;
        }

        battlefieldView!.ReconcileZone(zoneViewKey, statusModels);
    }

    private static string normalizeStatusDefinitionId(string statusKey)
    {
        return statusKey?.Trim().ToLowerInvariant() switch
        {
            "seal" => "Seal",
            "shackle" => "Shackle",
            "barrier" => "Barrier",
            _ => string.Empty,
        };
    }

    private void renderSeatField(
        string zoneViewKey,
        long? playerNumericId,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        if (!playerNumericId.HasValue || !playerById.TryGetValue(playerNumericId.Value, out var player))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            return;
        }

        battlefieldView!.ReconcileZone(
            zoneViewKey,
            BattlefieldCardViewModelMapper.FromProjectionCards(player.fieldCards, readOnly: true));
    }

    private void renderSeatDeckAndDiscard(
        string seatKey,
        long? playerNumericId,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById,
        long syntheticBase)
    {
        if (!playerNumericId.HasValue || !playerById.TryGetValue(playerNumericId.Value, out var player))
        {
            battlefieldView!.ClearZone($"playerDeck:{seatKey}");
            battlefieldView.ClearZone($"playerDiscard:{seatKey}");
            return;
        }

        battlefieldView!.ReconcileZone(
            $"playerDeck:{seatKey}",
            BattlefieldCardViewModelMapper.CreateDeckBackStack($"playerDeck:{seatKey}", syntheticBase, "T001", player.deckCount));
        var discardModels = BattlefieldCardViewModelMapper.CreatePublicPilePreview(
            $"playerDiscard:{seatKey}",
            syntheticBase - 1000,
            player.discardCards,
            player.discardCount,
            "T001");
        battlefieldView.ReconcileZone($"playerDiscard:{seatKey}", discardModels);
    }

    private void renderCurrentPlayerMirror(
        ProjectionViewModel projection,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        if (!projection.currentPlayerNumericId.HasValue ||
            !playerById.TryGetValue(projection.currentPlayerNumericId.Value, out var currentPlayer))
        {
            battlefieldView!.ClearZone("currentPlayerMirror");
            return;
        }

        battlefieldView!.ReconcileZone(
            "currentPlayerMirror",
            BattlefieldCardViewModelMapper.FromProjectionCards(
                currentPlayer.fieldCards,
                readOnly: true,
                WorldCardDisplayMode.readOnlyMirror));
    }

    private void renderCurrentPlayerResources(
        ProjectionViewModel projection,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        if (battlefieldView is null ||
            !battlefieldView.LayoutByZoneKey.TryGetValue("currentPlayerResources", out var resourceLayout))
        {
            return;
        }

        ensureCurrentResourceTexts(resourceLayout.transform);
        if (!projection.currentPlayerNumericId.HasValue ||
            !playerById.TryGetValue(projection.currentPlayerNumericId.Value, out var currentPlayer))
        {
            setResourceText(currentManaText, "魔力 -", new Color(0.85f, 0.18f, 0.14f, 1f));
            setResourceText(currentSkillText, "技能点 -", new Color(239f / 255f, 0f, 1f, 1f));
            setResourceText(currentSigilText, "灵符 -", new Color(0.22f, 0.58f, 1f, 1f));
            return;
        }

        var sigilValue = currentPlayer.lockedSigil ?? currentPlayer.sigilPreview;
        setResourceText(currentManaText, $"魔力 {currentPlayer.mana}", new Color(0.95f, 0.22f, 0.16f, 1f));
        setResourceText(currentSkillText, $"技能点 {currentPlayer.skillPoint}", new Color(239f / 255f, 0f, 1f, 1f));
        setResourceText(currentSigilText, $"灵符 {sigilValue}", new Color(0.24f, 0.64f, 1f, 1f));
    }

    private void ensureCurrentResourceTexts(Transform parent)
    {
        currentManaText ??= getOrCreateWorldResourceText(parent, "CurrentManaText", new Vector3(-2.35f, 0.26f, 0f));
        currentSkillText ??= getOrCreateWorldResourceText(parent, "CurrentSkillText", new Vector3(0f, 0.26f, 0f));
        currentSigilText ??= getOrCreateWorldResourceText(parent, "CurrentSigilText", new Vector3(2.35f, 0.26f, 0f));
    }

    private TMP_Text getOrCreateWorldResourceText(Transform parent, string name, Vector3 localPosition)
    {
        var existing = parent.Find(name);
        TMP_Text text;
        if (existing is not null)
        {
            text = existing.GetComponent<TMP_Text>();
            if (text is null)
            {
                text = existing.gameObject.AddComponent<TextMeshPro>();
            }
        }
        else
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TMP_Text>();
        }

        text.transform.localPosition = localPosition;
        text.transform.localEulerAngles = new Vector3(65f, 0f, 0f);
        text.transform.localScale = Vector3.one * 0.46f;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 15f;
        text.raycastTarget = false;
        if (primaryFont is not null)
        {
            text.font = primaryFont;
        }

        return text;
    }

    private static void setResourceText(TMP_Text? text, string value, Color color)
    {
        if (text is null)
        {
            return;
        }

        text.text = value;
        text.color = color;
    }

    private void renderLocalHand(ProjectionViewModel projection)
    {
        if (battlefieldView!.LayoutByZoneKey.ContainsKey("localHand"))
        {
            battlefieldView.ClearZone("localHand");
        }

        renderLocalHandPanel(projection.handCards);
    }

    private void renderLocalHandPanel(IReadOnlyList<ProjectionCardViewModel> handCards)
    {
        var localHandArea = layoutRoot?.LocalHandArea;
        if (localHandArea is null)
        {
            return;
        }

        var placeholderLabel = localHandArea.Find("Label")?.gameObject;
        if (placeholderLabel is not null)
        {
            placeholderLabel.SetActive(handCards.Count <= 0);
        }

        var content = localHandArea.Find("ProjectionLocalHandContent") as RectTransform;
        if (content is null)
        {
            var contentObject = new GameObject("ProjectionLocalHandContent", typeof(RectTransform));
            contentObject.transform.SetParent(localHandArea, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(12f, 8f);
            content.offsetMax = new Vector2(-12f, -8f);
        }

        var incomingIds = new HashSet<long>(handCards.Select(card => card.cardInstanceNumericId));
        foreach (var existing in localHandUiByCardInstanceId.Keys.ToArray())
        {
            if (incomingIds.Contains(existing))
            {
                continue;
            }

            destroy(localHandUiByCardInstanceId[existing]);
            localHandUiByCardInstanceId.Remove(existing);
        }

        for (var index = 0; index < handCards.Count; index += 1)
        {
            var card = handCards[index];
            if (!localHandUiByCardInstanceId.TryGetValue(card.cardInstanceNumericId, out var cardObject))
            {
                cardObject = createLocalHandCard(content, card.cardInstanceNumericId);
                localHandUiByCardInstanceId[card.cardInstanceNumericId] = cardObject;
            }

            bindLocalHandCard(cardObject, card, index, handCards.Count);
        }
    }

    private static GameObject createLocalHandCard(RectTransform parent, long cardInstanceNumericId)
    {
        var cardObject = new GameObject(
            $"LocalHandCard_{cardInstanceNumericId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        cardObject.transform.SetParent(parent, false);
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(cardObject.transform, false);
        return cardObject;
    }

    private void bindLocalHandCard(
        GameObject cardObject,
        ProjectionCardViewModel card,
        int index,
        int count)
    {
        var rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(86f, 120f);
        var totalWidth = Mathf.Max(1, count) * 92f;
        rect.anchoredPosition = new Vector2(index * 92f - (totalWidth - 92f) * 0.5f, 0f);

        var image = cardObject.GetComponent<Image>();
        image.sprite = CardArtService.GetSpriteForDefinition(card.definitionId);
        image.preserveAspect = true;
        image.raycastTarget = true;

        var cardButton = cardObject.GetComponent<BattlefieldLocalHandCardButton>();
        if (cardButton is null)
        {
            cardButton = cardObject.AddComponent<BattlefieldLocalHandCardButton>();
        }

        cardButton.Configure(card.cardInstanceNumericId, handleLocalHandCardClicked);

        var label = cardObject.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label is null)
        {
            return;
        }

        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.2f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = card.definitionId;
        label.fontSize = 11f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        if (primaryFont is not null)
        {
            label.font = primaryFont;
        }
    }

    private void handleLocalHandCardClicked(long cardInstanceNumericId)
    {
        ensureReferences();
        if (networkController is null || !networkController.TryPlayTreasureCard(cardInstanceNumericId))
        {
            setStatus(networkController?.LastError ?? "尚未初始化正式网络控制器。");
        }
    }

    private void renderActionPanel(ProjectionViewModel projection)
    {
        var actionPanel = layoutRoot?.ScreenHudCanvas?.transform.Find("ActionPanel") as RectTransform;
        if (actionPanel is null)
        {
            return;
        }

        var placeholderLabel = actionPanel.Find("Label")?.gameObject;
        if (placeholderLabel is not null)
        {
            placeholderLabel.SetActive(false);
        }

        var content = actionPanel.Find("ProjectionActionPanelContent") as RectTransform;
        if (content is null)
        {
            var contentObject = new GameObject("ProjectionActionPanelContent", typeof(RectTransform));
            contentObject.transform.SetParent(actionPanel, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(10f, 8f);
            content.offsetMax = new Vector2(-10f, -8f);
        }

        var phaseContainer = getOrCreateChildRect(
            content,
            "PhaseActionContainer",
            new Vector2(0f, 0.78f),
            Vector2.one,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f));
        var skillContainer = getOrCreateChildRect(
            content,
            "SkillListContainer",
            Vector2.zero,
            new Vector2(1f, 0.74f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f));
        clearChildren(phaseContainer);
        clearChildren(skillContainer);

        var localPlayer = projection.playerSummaries.FirstOrDefault(player => player.isViewerPlayer) ??
                          projection.playerSummaries.FirstOrDefault(player => player.playerNumericId == projection.viewerPlayerNumericId);

        renderPhaseActionButton(phaseContainer, projection);
        var skillStartIndex = renderAnomalyResolveButtons(skillContainer, projection, localPlayer, 0);
        renderSkillButtons(skillContainer, projection, localPlayer, skillStartIndex);
    }

    private void renderPhaseActionButton(RectTransform parent, ProjectionViewModel projection)
    {
        var isLocalTurn = projection.currentPlayerNumericId == projection.viewerPlayerNumericId;
        if (string.Equals(projection.currentPhase, "action", System.StringComparison.Ordinal))
        {
            createPhaseButton(
                parent,
                "进入召唤阶段",
                isLocalTurn,
                () =>
                {
                    ensureReferences();
                    if (networkController is null || !networkController.TryEnterSummonPhase())
                    {
                        setStatus(networkController?.LastError ?? "尚未初始化正式网络控制器。");
                    }
                });
        }
        else if (string.Equals(projection.currentPhase, "summon", System.StringComparison.Ordinal))
        {
            createPhaseButton(
                parent,
                "结束回合",
                isLocalTurn,
                () =>
                {
                    ensureReferences();
                    if (networkController is null || !networkController.TryEnterEndPhase())
                    {
                        setStatus(networkController?.LastError ?? "尚未初始化正式网络控制器。");
                    }
                });
        }
        else
        {
            createPhaseButton(parent, $"当前阶段：{localizePhase(projection.currentPhase)}", false, () => { });
        }
    }

    private void renderSkillButtons(
        RectTransform parent,
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel? localPlayer,
        int startIndex)
    {
        var localCharacterDefinition = localPlayer is null
            ? null
            : projection.characterDefinitions.FirstOrDefault(definition =>
                string.Equals(
                    definition.definitionId,
                    localPlayer.activeCharacterDefinitionId,
                    System.StringComparison.Ordinal));
        if (localPlayer?.activeCharacterInstanceNumericId is null || localCharacterDefinition is null)
        {
            createSkillButton(parent, "技能：未选择角色", string.Empty, false, startIndex, () => { });
            return;
        }

        var localCharacterInstanceNumericId = localPlayer.activeCharacterInstanceNumericId.Value;
        var index = startIndex;
        foreach (var skill in localCharacterDefinition.skills.OrderBy(skill => skill.skillOrder).Take(4))
        {
            var capturedSkill = skill;
            var isResponseSkill = string.Equals(skill.skillTypeRaw, "响应", System.StringComparison.Ordinal);
            var subtitle = isResponseSkill
                ? "响应技能暂不可主动发动"
                : buildSkillSubtitle(skill);
            createSkillButton(
                parent,
                $"{skill.skillName}（{emptyToDash(skill.skillCostRaw)}）",
                subtitle,
                !isResponseSkill,
                index,
                () =>
                {
                    ensureReferences();
                    if (networkController is null ||
                        !networkController.TryUseSkill(localCharacterInstanceNumericId, capturedSkill.skillKey))
                    {
                        setStatus(networkController?.LastError ?? $"技能请求未发送：{capturedSkill.skillKey}");
                    }
                });
            index += 1;
        }
    }

    private int renderAnomalyResolveButtons(
        RectTransform parent,
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel? localPlayer,
        int startIndex)
    {
        if (!projection.currentAnomaly.hasCurrentAnomaly ||
            string.IsNullOrWhiteSpace(projection.currentAnomaly.definitionId))
        {
            isAnomalyResolveConfirmOpen = false;
            return startIndex;
        }

        var isLocalTurn = projection.currentPlayerNumericId == projection.viewerPlayerNumericId;
        var title = isAnomalyResolveConfirmOpen
            ? $"确认解决 {projection.currentAnomaly.definitionId}"
            : $"尝试解决异变：{projection.currentAnomaly.definitionId}";
        var detail = buildAnomalyResolveSubtitle(projection);
        createSkillButton(
            parent,
            title,
            detail,
            isLocalTurn,
            startIndex,
            () =>
            {
                isAnomalyResolveConfirmOpen = !isAnomalyResolveConfirmOpen;
                render(projection);
            });

        if (!isAnomalyResolveConfirmOpen)
        {
            return startIndex + 1;
        }

        var index = startIndex + 1;
        createSkillButton(
            parent,
            "确认解决（不指定目标）",
            "适用于不需要 root target 的异变；若失败会显示服务器原因。",
            isLocalTurn,
            index,
            () => tryResolveAnomalyAndClose(null));
        index += 1;

        if (localPlayer is not null &&
            !usesDeferredAnomalyTargetInput(projection.currentAnomaly.definitionId))
        {
            var opponentPlayers = projection.playerSummaries
                .Where(player => player.playerNumericId > 0 &&
                                 player.teamNumericId != localPlayer.teamNumericId)
                .OrderBy(player => player.playerNumericId);
            foreach (var opponent in opponentPlayers)
            {
                var capturedTargetPlayerId = opponent.playerNumericId;
                createSkillButton(
                    parent,
                    $"指定 Player {capturedTargetPlayerId} 并解决",
                    "用于需要目标对手的异变奖励/条件。",
                    isLocalTurn,
                    index,
                    () => tryResolveAnomalyAndClose(capturedTargetPlayerId));
                index += 1;
            }
        }

        return index;
    }

    private static bool usesDeferredAnomalyTargetInput(string anomalyDefinitionId)
    {
        return string.Equals(anomalyDefinitionId, "A007", StringComparison.Ordinal) ||
               string.Equals(anomalyDefinitionId, "A008", StringComparison.Ordinal);
    }

    private Button createPhaseButton(
        RectTransform parent,
        string label,
        bool interactable,
        UnityEngine.Events.UnityAction clicked)
    {
        var buttonObject = new GameObject("PhaseActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.18f);
        rect.anchorMax = new Vector2(1f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = buttonObject.GetComponent<Image>();
        image.color = interactable ? new Color(0.18f, 0.48f, 0.56f, 0.95f) : new Color(0.16f, 0.16f, 0.16f, 0.62f);
        var button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(clicked);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 2f);
        labelRect.offsetMax = new Vector2(-6f, -2f);
        var text = labelObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        if (primaryFont is not null)
        {
            text.font = primaryFont;
        }
        return button;
    }

    private Button createSkillButton(
        RectTransform parent,
        string title,
        string subtitle,
        bool interactable,
        int index,
        UnityEngine.Events.UnityAction clicked)
    {
        const float buttonHeight = 52f;
        const float buttonGap = 7f;
        var buttonObject = new GameObject($"SkillButton_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, buttonHeight);
        rect.anchoredPosition = new Vector2(0f, -index * (buttonHeight + buttonGap));

        var image = buttonObject.GetComponent<Image>();
        image.color = interactable ? new Color(0.12f, 0.34f, 0.42f, 0.92f) : new Color(0.14f, 0.14f, 0.14f, 0.64f);
        var button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(clicked);

        var titleText = createButtonText(buttonObject.transform, "Title", title, 14f, TextAlignmentOptions.Center);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.48f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(6f, 0f);
        titleRect.offsetMax = new Vector2(-6f, -2f);

        var subtitleText = createButtonText(buttonObject.transform, "Subtitle", subtitle, 10.5f, TextAlignmentOptions.Center);
        var subtitleRect = subtitleText.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0f, 0f);
        subtitleRect.anchorMax = new Vector2(1f, 0.5f);
        subtitleRect.offsetMin = new Vector2(8f, 3f);
        subtitleRect.offsetMax = new Vector2(-8f, 0f);
        subtitleText.color = interactable ? new Color(0.86f, 0.92f, 0.96f, 1f) : new Color(0.55f, 0.58f, 0.6f, 1f);
        return button;
    }

    private TMP_Text createButtonText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
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
        if (primaryFont is not null)
        {
            text.font = primaryFont;
        }

        return text;
    }

    private static RectTransform getOrCreateChildRect(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var existing = parent.Find(name);
        RectTransform rect;
        if (existing is null)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }
        else
        {
            rect = existing.GetComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static void clearChildren(Transform parent)
    {
        for (var index = parent.childCount - 1; index >= 0; index -= 1)
        {
            destroy(parent.GetChild(index).gameObject);
        }
    }

    private static string emptyToDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string buildSkillSubtitle(ProjectionCharacterSkillDefinitionViewModel skill)
    {
        var type = string.IsNullOrWhiteSpace(skill.skillTypeRaw)
            ? "技能"
            : skill.skillTypeRaw.Trim();
        var effect = string.IsNullOrWhiteSpace(skill.effectText)
            ? string.Empty
            : skill.effectText.Trim();
        if (effect.Length > 28)
        {
            effect = effect[..28] + "…";
        }

        return string.IsNullOrWhiteSpace(effect) ? type : $"{type} / {effect}";
    }

    private static string buildAnomalyResolveSubtitle(ProjectionViewModel projection)
    {
        var resolveText = string.IsNullOrWhiteSpace(projection.currentAnomaly.resolveText)
            ? projection.currentAnomaly.resolveConditionKey
            : projection.currentAnomaly.resolveText;
        if (string.IsNullOrWhiteSpace(resolveText))
        {
            resolveText = "解决条件未提供";
        }

        if (projection.currentAnomaly.hasResolvedThisTurn)
        {
            resolveText = $"本回合已解决过异变 / {resolveText}";
        }

        if (resolveText.Length > 34)
        {
            resolveText = resolveText[..34] + "…";
        }

        return resolveText;
    }

    private void tryResolveAnomalyAndClose(long? targetPlayerNumericId)
    {
        ensureReferences();
        if (networkController is null || !networkController.TryResolveAnomaly(targetPlayerNumericId))
        {
            setStatus(networkController?.LastError ?? "异变解决请求未发送。");
            return;
        }

        isAnomalyResolveConfirmOpen = false;
    }

    private static string localizePhase(string phase)
    {
        return phase switch
        {
            "action" => "行动阶段",
            "summon" => "召唤阶段",
            "start" => "开始阶段",
            "end" => "结束阶段",
            _ => string.IsNullOrWhiteSpace(phase) ? "未知阶段" : phase,
        };
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

    private void handleCardClicked(WorldCardView view)
    {
        if (tryOpenBrowsableZone(view.CurrentModel))
        {
            return;
        }

        cardDetailPanel?.Show(view.CurrentModel);
    }

    private bool tryOpenBrowsableZone(WorldCardViewModel model)
    {
        if (zoneCardListOverlayView is null || latestProjection is null)
        {
            return false;
        }

        if (string.Equals(model.zoneKey, "gapZone", System.StringComparison.Ordinal))
        {
            zoneCardListOverlayView.Show("间隙区", latestProjection.gapZoneCards);
            return true;
        }

        if (!model.zoneKey.StartsWith("playerDiscard:", System.StringComparison.Ordinal))
        {
            return false;
        }

        var seatKey = model.zoneKey["playerDiscard:".Length..];
        var player = resolvePlayerSummaryForSeatKey(seatKey);
        if (player is null)
        {
            return false;
        }

        zoneCardListOverlayView.Show($"Player {player.playerNumericId} 弃牌区", player.discardCards);
        return true;
    }

    private ProjectionPlayerSummaryViewModel? resolvePlayerSummaryForSeatKey(string seatKey)
    {
        if (latestProjection is null)
        {
            return null;
        }

        var seatResult = SeatLayoutResolver.Resolve(
            latestProjection.viewerPlayerNumericId,
            latestProjection.turnOrderPlayerNumericIds,
            latestProjection.playerSummaries);
        if (!seatResult.isResolved)
        {
            return null;
        }

        long? playerId = seatKey switch
        {
            "bottom" => seatResult.bottomPlayerNumericId,
            "top" => seatResult.topPlayerNumericId,
            "left" => seatResult.leftPlayerNumericId,
            "right" => seatResult.rightPlayerNumericId,
            _ => null,
        };

        return playerId.HasValue
            ? latestProjection.playerSummaries.FirstOrDefault(player => player.playerNumericId == playerId.Value)
            : null;
    }

    private void handleCardDoubleClicked(WorldCardView view)
    {
        var model = view.CurrentModel;
        cardDetailPanel?.Show(model);
        if (!isPublicSummonCardZone(model.zoneKey))
        {
            return;
        }

        if (model.cardInstanceNumericId <= 0)
        {
            setStatus("该区域当前只有牌堆摘要，不能直接召唤。");
            return;
        }

        ensureReferences();
        if (networkController is null || !networkController.TrySummonTreasureCard(model.cardInstanceNumericId))
        {
            setStatus(networkController?.LastError ?? "尚未初始化正式网络控制器。");
        }
    }

    private static bool isPublicSummonCardZone(string zoneKey)
    {
        return zoneKey.StartsWith("summonZone:", System.StringComparison.Ordinal) ||
               string.Equals(zoneKey, "sakuraCakeZone", System.StringComparison.Ordinal);
    }

    private void setStatus(string value)
    {
        if (statusText is not null)
        {
            statusText.text = value;
        }
    }

    private static string buildTeamSummaryText(ProjectionViewModel projection)
    {
        if (projection.teamSummaries.Count <= 0)
        {
            return "队伍资源：未提供";
        }

        return string.Join(
            "  /  ",
            projection.teamSummaries
                .OrderBy(team => team.teamNumericId)
                .Select(team => $"队伍{team.teamNumericId} 灵脉{team.leyline} 击坠{team.killScore}"));
    }
}
}

