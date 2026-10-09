using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private LocalPlayerHudView? localPlayerHudView;
    private TeamResourcePanelView? teamResourcePanelView;
    private BattlefieldEffectPresentationTimeline? effectPresentationTimeline;
    private SkillPresentationTimeline? skillPresentationTimeline;
    private RectTransform? characterSkillMenuOverlay;
    private RectTransform? anomalyResolveMenuOverlay;
    private RectTransform? messageOverlay;
    private bool isAnomalyResolveConfirmOpen;
    private ProjectionViewModel? latestProjection;
    private readonly HashSet<string> shownRevealEventKeys = new();
    private readonly Dictionary<string, CurrentPlayerMarkerView> currentPlayerMarkerByZoneKey = new();
    private readonly long?[] previousSummonZoneSlotCardInstanceIds = new long?[6];
    private WorldCardView? lastClickedWorldCardView;
    private bool wasPresentationBusy;

    public BattlefieldView? View => battlefieldView;

    public void Configure(
        ProjectionViewState newProjectionViewState,
        BattlefieldLayoutRoot newLayoutRoot,
        BattlefieldView newBattlefieldView,
        TMP_Text? newStatusText)
    {
        projectionViewState = newProjectionViewState;
        layoutRoot = newLayoutRoot;
        battlefieldView = newBattlefieldView;
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

    private void Update()
    {
        var isPresentationBusy = BattlefieldInteractionPresentationGate.IsPresentationBusy;
        if (wasPresentationBusy && !isPresentationBusy && latestProjection is not null)
        {
            renderActionPanel(latestProjection);
        }

        wasPresentationBusy = isPresentationBusy;

        if (Keyboard.current?.f9Key.wasPressedThisFrame != true)
        {
            return;
        }

        if (lastClickedWorldCardView is null)
        {
            Debug.LogWarning("[BattlefieldProjectionPresenter] VFX calibration skipped: click a battlefield card first, then press F9.");
            return;
        }

        effectPresentationTimeline?.TestVfxOnCard(lastClickedWorldCardView, EffectCategoryKeys.CardActivate);
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

            effectPresentationTimeline ??= layoutRoot.TableWorld.GetComponent<BattlefieldEffectPresentationTimeline>();
            if (effectPresentationTimeline is null)
            {
                effectPresentationTimeline = layoutRoot.TableWorld.gameObject.AddComponent<BattlefieldEffectPresentationTimeline>();
            }

            skillPresentationTimeline ??= layoutRoot.TableWorld.GetComponent<SkillPresentationTimeline>();
            if (skillPresentationTimeline is null)
            {
                skillPresentationTimeline = layoutRoot.TableWorld.gameObject.AddComponent<SkillPresentationTimeline>();
            }
        }

        if (layoutRoot?.ScreenHudCanvas is not null)
        {
            statusText ??= layoutRoot.ScreenHudCanvas.transform
                .Find("TurnInfoPanel/Label")
                ?.GetComponent<TMP_Text>();
            teamResourcePanelView ??= layoutRoot.ScreenHudCanvas.transform
                .Find("TurnInfoPanel")
                ?.GetComponent<TeamResourcePanelView>();
            if (teamResourcePanelView is null)
            {
                var turnInfoPanel = layoutRoot.ScreenHudCanvas.transform.Find("TurnInfoPanel");
                if (turnInfoPanel is not null)
                {
                    teamResourcePanelView = turnInfoPanel.gameObject.AddComponent<TeamResourcePanelView>();
                }
            }

            teamResourcePanelView?.Configure(primaryFont);
            localPlayerHudView ??= layoutRoot.ScreenHudCanvas.transform
                .Find("PlayerHud")
                ?.GetComponent<LocalPlayerHudView>();
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
            teamResourcePanelView?.ShowWaiting();
            setStatus("Waiting for projection.");
            return;
        }

        if (layoutRoot?.OverlayCanvas is not null)
        {
            skillPresentationTimeline?.Configure(layoutRoot.OverlayCanvas, primaryFont);
            skillPresentationTimeline?.PresentProjection(projection);
        }

        showNewRevealEvents(projection);

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
            teamResourcePanelView?.Bind(projection);
            setStatus(string.Empty);
        }

        renderPublicZones(projection);
        renderPlayerCharacters(seatResult, playerById, projection.currentPlayerNumericId);
        renderPlayerFields(seatResult, playerById);
        renderPlayerStatuses(seatResult, playerById);
        renderCurrentPlayerMirror(projection, playerById);
        renderCurrentPlayerResources(projection, playerById);
        renderLocalPlayerHud(projection, playerById);
        renderLocalHand(projection);
        renderActionPanel(projection);
        effectPresentationTimeline?.Configure(
            battlefieldView,
            layoutRoot?.TableWorld?.transform,
            layoutRoot?.OverlayCanvas,
            primaryFont);
        effectPresentationTimeline?.PresentProjection(projection);
    }

    private void showNewRevealEvents(ProjectionViewModel projection)
    {
        if (zoneCardListOverlayView is null || projection.revealedCardEvents.Count == 0)
        {
            return;
        }

        var newRevealedCards = new List<ProjectionCardViewModel>();
        var firstNewRevealReason = string.Empty;
        foreach (var revealedCardEvent in projection.revealedCardEvents)
        {
            var revealIdentity =
                $"{revealedCardEvent.eventId}:{revealedCardEvent.cardInstanceNumericId}:{revealedCardEvent.revealReasonKey}:{revealedCardEvent.declaredDefinitionId}";
            if (!shownRevealEventKeys.Add(revealIdentity))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(firstNewRevealReason))
            {
                firstNewRevealReason = revealedCardEvent.revealReasonKey ?? string.Empty;
            }

            newRevealedCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = revealedCardEvent.cardInstanceNumericId,
                definitionId = revealedCardEvent.definitionId,
                ownerPlayerNumericId = revealedCardEvent.ownerPlayerNumericId,
                zoneKey = localizeRevealReason(revealedCardEvent.revealReasonKey),
                isFaceUp = true,
            });
        }

        if (newRevealedCards.Count == 0)
        {
            return;
        }

        var firstNewRevealEvent = projection.revealedCardEvents.FirstOrDefault(revealedCardEvent =>
            revealedCardEvent.revealReasonKey == firstNewRevealReason &&
            newRevealedCards.Any(card => card.cardInstanceNumericId == revealedCardEvent.cardInstanceNumericId));
        var title = createRevealTitle(firstNewRevealReason, newRevealedCards, firstNewRevealEvent?.declaredDefinitionId);
        zoneCardListOverlayView.Show(title, newRevealedCards);
    }

    private static string createRevealTitle(
        string revealReasonKey,
        IReadOnlyList<ProjectionCardViewModel> revealedCards,
        string? declaredDefinitionId)
    {
        var reason = localizeRevealReason(revealReasonKey);
        if (revealedCards.Count == 1)
        {
            if (string.Equals(revealReasonKey, "treasureOnPlay:T013:declareCardName", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(declaredDefinitionId))
            {
                return $"【圣骸布】宣言【{CardNameLocalizer.Localize(declaredDefinitionId)}】，展示【{CardNameLocalizer.Localize(revealedCards[0].definitionId)}】";
            }

            if (string.Equals(revealReasonKey, "character:C031:3:alleyAlliance", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(declaredDefinitionId))
            {
                return $"弓冢五月发动技能【小巷子同盟】（③）：说一个牌名，翻开目标对手牌库顶1张牌，若翻开的牌是你所说的同名牌，你获得1灵脉；否则他和你各抓1张牌。宣言的是：【{CardNameLocalizer.Localize(declaredDefinitionId)}】，翻开的是：【{CardNameLocalizer.Localize(revealedCards[0].definitionId)}】";
            }

            return $"{reason}：{CardNameLocalizer.Localize(revealedCards[0].definitionId)}";
        }

        return $"{reason}：{revealedCards.Count} 张牌";
    }

    private static string localizeRevealReason(string revealReasonKey)
    {
        return revealReasonKey switch
        {
            "treasureOnPlay:T013:declareCardName" => "【圣骸布】宣言后公开展示牌库顶牌",
            "character:C013:3:darkJadeFist" => "【巫净翡翠】技能【暗黑翡翠拳】公开展示目标牌库顶牌",
            "character:C013:4:criminal" => "【巫净翡翠】终结技【“你被犯人了”】公开展示目标牌库顶牌",
            "character:C005:2:sixRealms" => "【魂魄妖梦】响应技【六根清净斩】公开展示牌库顶牌",
            "character:C031:3:alleyAlliance" => "【弓冢五月】技能【小巷子同盟】公开展示目标牌库顶牌",
            "character:C022:3:grandGuignol" => "【爱丽丝】技能【大千枪】公开展示抓到的牌",
            _ when string.IsNullOrWhiteSpace(revealReasonKey) => "公开展示",
            _ => revealReasonKey,
        };
    }

    private void renderLocalPlayerHud(
        ProjectionViewModel projection,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById)
    {
        if (localPlayerHudView is null)
        {
            return;
        }

        playerById.TryGetValue(projection.viewerPlayerNumericId, out var viewerPlayer);
        localPlayerHudView.Bind(viewerPlayer);
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

        var nextSummonZoneSlotCardInstanceIds = new long?[6];
        for (var index = 0; index < nextSummonZoneSlotCardInstanceIds.Length; index += 1)
        {
            nextSummonZoneSlotCardInstanceIds[index] = index < projection.summonZoneCards.Count
                ? projection.summonZoneCards[index].cardInstanceNumericId
                : null;
        }

        // SummonZone is displayed as six physical slots, but the server sends it as
        // one ordered zone. Clear changed slots from right to left first so cards
        // that shift left after a refill can be reused by the global visual registry
        // before the new public-deck card enters the last slot.
        for (var index = previousSummonZoneSlotCardInstanceIds.Length - 1; index >= 0; index -= 1)
        {
            if (previousSummonZoneSlotCardInstanceIds[index] == nextSummonZoneSlotCardInstanceIds[index])
            {
                continue;
            }

            battlefieldView.ClearZone($"summonZone:{index + 1}");
        }

        for (var index = 0; index < nextSummonZoneSlotCardInstanceIds.Length; index += 1)
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

            previousSummonZoneSlotCardInstanceIds[index] = nextSummonZoneSlotCardInstanceIds[index];
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

        battlefieldView.ReconcileZone(
            "resolvedAnomaly",
            BattlefieldCardViewModelMapper.CreatePublicPilePreview(
                "resolvedAnomaly",
                -125000,
                projection.resolvedAnomalyCards,
                projection.resolvedAnomalyCards.Count,
                "A001"));

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
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById,
        long? currentPlayerNumericId)
    {
        renderSeatCharacter("playerCharacter:bottom", seatResult.bottomPlayerNumericId, playerById, -400000, currentPlayerNumericId);
        renderSeatCharacter("playerCharacter:top", seatResult.topPlayerNumericId, playerById, -410000, currentPlayerNumericId);
        renderSeatCharacter("playerCharacter:left", seatResult.leftPlayerNumericId, playerById, -420000, currentPlayerNumericId);
        renderSeatCharacter("playerCharacter:right", seatResult.rightPlayerNumericId, playerById, -430000, currentPlayerNumericId);
    }

    private void renderSeatCharacter(
        string zoneViewKey,
        long? playerNumericId,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playerById,
        long syntheticBase,
        long? currentPlayerNumericId)
    {
        if (!playerNumericId.HasValue || !playerById.TryGetValue(playerNumericId.Value, out var player) ||
            string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            clearCharacterMarkers(zoneViewKey);
            clearCurrentPlayerMarker(zoneViewKey);
            return;
        }

        var primaryDefinitionId = normalizeCharacterCardDefinitionId(player.activeCharacterDefinitionId);
        if (string.IsNullOrWhiteSpace(primaryDefinitionId))
        {
            battlefieldView!.ClearZone(zoneViewKey);
            clearCharacterMarkers(zoneViewKey);
            clearCurrentPlayerMarker(zoneViewKey);
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
                isActivated = index == 0 && player.activeCharacterIsActivated,
                displayMode = WorldCardDisplayMode.readOnlyMirror,
                overlayCardCount = index == 0 ? player.characterSetAsideCount : 0,
            });
        }

        battlefieldView!.ReconcileZone(zoneViewKey, models);
        reconcileCharacterMarkers(zoneViewKey, player.activeCharacterMarkers);
        reconcileCurrentPlayerMarker(zoneViewKey, currentPlayerNumericId.HasValue && currentPlayerNumericId.Value == player.playerNumericId);
    }

    private void reconcileCurrentPlayerMarker(string characterZoneViewKey, bool isCurrentPlayer)
    {
        if (!isCurrentPlayer)
        {
            clearCurrentPlayerMarker(characterZoneViewKey);
            return;
        }

        if (battlefieldView is null ||
            !battlefieldView.LayoutByZoneKey.TryGetValue(characterZoneViewKey, out var layout) ||
            layout.Anchor is null)
        {
            return;
        }

        var marker = getOrCreateCurrentPlayerMarkerView(layout.Anchor.transform, characterZoneViewKey);
        marker.Configure(
            getCurrentPlayerMarkerLocalPosition(characterZoneViewKey),
            getCurrentPlayerMarkerLocalRotation(characterZoneViewKey),
            new Vector3(0.085f, 0.085f, 0.085f));
    }

    private void clearCurrentPlayerMarker(string characterZoneViewKey)
    {
        if (currentPlayerMarkerByZoneKey.TryGetValue(characterZoneViewKey, out var marker))
        {
            marker.Hide();
            return;
        }

        if (battlefieldView is null ||
            !battlefieldView.LayoutByZoneKey.TryGetValue(characterZoneViewKey, out var layout) ||
            layout.Anchor is null)
        {
            return;
        }

        layout.Anchor.transform.Find("CurrentPlayerMarkerView")?.GetComponent<CurrentPlayerMarkerView>()?.Hide();
    }

    private CurrentPlayerMarkerView getOrCreateCurrentPlayerMarkerView(Transform anchorTransform, string characterZoneViewKey)
    {
        if (currentPlayerMarkerByZoneKey.TryGetValue(characterZoneViewKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var markerTransform = anchorTransform.Find("CurrentPlayerMarkerView");
        CurrentPlayerMarkerView marker;
        if (markerTransform is not null)
        {
            marker = markerTransform.GetComponent<CurrentPlayerMarkerView>() ??
                     markerTransform.gameObject.AddComponent<CurrentPlayerMarkerView>();
        }
        else
        {
            var markerObject = new GameObject("CurrentPlayerMarkerView");
            markerObject.transform.SetParent(anchorTransform, false);
            marker = markerObject.AddComponent<CurrentPlayerMarkerView>();
        }

        currentPlayerMarkerByZoneKey[characterZoneViewKey] = marker;
        return marker;
    }

    private static Vector3 getCurrentPlayerMarkerLocalPosition(string characterZoneViewKey)
    {
        return characterZoneViewKey switch
        {
            "playerCharacter:top" => new Vector3(0f, 0.055f, 0.86f),
            "playerCharacter:left" => new Vector3(-0.86f, 0.055f, 0f),
            "playerCharacter:right" => new Vector3(0.86f, 0.055f, 0f),
            _ => new Vector3(0f, 0.055f, -0.86f),
        };
    }

    private static Vector3 getCurrentPlayerMarkerLocalRotation(string characterZoneViewKey)
    {
        return characterZoneViewKey switch
        {
            "playerCharacter:top" => new Vector3(90f, 180f, 0f),
            "playerCharacter:left" => new Vector3(90f, 90f, 0f),
            "playerCharacter:right" => new Vector3(90f, -90f, 0f),
            _ => new Vector3(90f, 0f, 0f),
        };
    }

    private void reconcileCharacterMarkers(
        string characterZoneViewKey,
        IReadOnlyList<ProjectionMarkerViewModel> markers)
    {
        if (battlefieldView is null ||
            !battlefieldView.LayoutByZoneKey.TryGetValue(characterZoneViewKey, out var layout) ||
            layout.Anchor is null)
        {
            return;
        }

        var markerView = getOrCreateCharacterMarkerView(layout.Anchor.transform);
        markerView.Reconcile(markers, layout.Anchor.LocalRotation);
    }

    private void clearCharacterMarkers(string characterZoneViewKey)
    {
        if (battlefieldView is null ||
            !battlefieldView.LayoutByZoneKey.TryGetValue(characterZoneViewKey, out var layout) ||
            layout.Anchor is null)
        {
            return;
        }

        var markerView = layout.Anchor.transform
            .Find("CharacterMarkerView")
            ?.GetComponent<BattlefieldCharacterMarkerView>();
        markerView?.Clear();
    }

    private static BattlefieldCharacterMarkerView getOrCreateCharacterMarkerView(Transform anchorTransform)
    {
        var markerTransform = anchorTransform.Find("CharacterMarkerView");
        if (markerTransform is not null)
        {
            return markerTransform.GetComponent<BattlefieldCharacterMarkerView>() ??
                   markerTransform.gameObject.AddComponent<BattlefieldCharacterMarkerView>();
        }

        var markerObject = new GameObject("CharacterMarkerView");
        markerObject.transform.SetParent(anchorTransform, false);
        markerObject.transform.localPosition = Vector3.zero;
        return markerObject.AddComponent<BattlefieldCharacterMarkerView>();
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
                     .Concat(player.playerStatusKeys)
                     .Select(normalizeStatusDefinitionId)
                     .Where(definitionId => !string.IsNullOrWhiteSpace(definitionId))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            BuffIconSpriteService.TryGetTooltip(statusKey, out var tooltipTitle, out var tooltipDescription);
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
                tooltipTitle = tooltipTitle,
                tooltipDescription = tooltipDescription,
            });
            statusIndex += 1;
        }

        battlefieldView!.ReconcileZone(zoneViewKey, statusModels);
    }

    private static string normalizeStatusDefinitionId(string statusKey)
    {
        return BuffIconSpriteService.NormalizeStatusDefinitionId(statusKey);
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
            createPlayerDeckPileModel($"playerDeck:{seatKey}", syntheticBase, player.deckCount));
        var discardModels = BattlefieldCardViewModelMapper.CreatePublicPilePreview(
            $"playerDiscard:{seatKey}",
            syntheticBase - 1000,
            player.discardCards,
            player.discardCount,
            "T001");
        battlefieldView.ReconcileZone($"playerDiscard:{seatKey}", discardModels);
    }

    private static IReadOnlyList<WorldCardViewModel> createPlayerDeckPileModel(
        string zoneKey,
        long syntheticBase,
        int deckCount)
    {
        if (deckCount > 0)
        {
            return BattlefieldCardViewModelMapper.CreateDeckBackStack(zoneKey, syntheticBase, "T001", deckCount);
        }

        return new[]
        {
            new WorldCardViewModel
            {
                cardInstanceNumericId = syntheticBase,
                definitionId = "T001",
                zoneKey = zoneKey,
                isFaceUp = false,
                isInteractable = false,
                isReadOnly = true,
                displayMode = WorldCardDisplayMode.emptyZone,
                pileCardCount = 0,
            },
        };
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

        cardButton.Configure(card, handleLocalHandCardClicked);

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
        if (tryBlockPresentationBusyAction())
        {
            return;
        }

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
            new Vector2(0f, 0.77f),
            Vector2.one,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f));
        var characterContainer = getOrCreateChildRect(
            content,
            "CharacterActionCardContainer",
            Vector2.zero,
            new Vector2(1f, 0.72f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f));
        clearChildren(phaseContainer);
        clearChildren(characterContainer);

        var localPlayer = projection.playerSummaries.FirstOrDefault(player => player.isViewerPlayer) ??
                          projection.playerSummaries.FirstOrDefault(player => player.playerNumericId == projection.viewerPlayerNumericId);

        if (BattlefieldInteractionPresentationGate.IsPresentationBusy)
        {
            wasPresentationBusy = true;
            renderPresentationBusyPanel(phaseContainer, characterContainer);
            return;
        }

        renderPhaseActionButton(phaseContainer, projection);
        renderCharacterActionCard(characterContainer, projection, localPlayer);
    }

    private void renderPresentationBusyPanel(RectTransform phaseContainer, RectTransform characterContainer)
    {
        createPhaseButton(
            phaseContainer,
            $"效果处理中... {BattlefieldInteractionPresentationGate.RemainingDelaySeconds:0.0}s",
            false,
            () => { });

        var debugText = createButtonText(
            characterContainer,
            "PresentationBusyDebug",
            BattlefieldInteractionPresentationGate.DebugSummary,
            13f,
            TextAlignmentOptions.TopLeft);
        debugText.enableWordWrapping = true;
        debugText.overflowMode = TextOverflowModes.Overflow;
        debugText.color = new Color(0.82f, 0.93f, 0.96f, 1f);
        var rect = debugText.rectTransform;
        rect.anchorMin = new Vector2(0.04f, 0.08f);
        rect.anchorMax = new Vector2(0.96f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
                    if (tryBlockPresentationBusyAction())
                    {
                        return;
                    }

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
                    if (tryBlockPresentationBusyAction())
                    {
                        return;
                    }

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

    private void renderCharacterActionCard(
        RectTransform parent,
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel? localPlayer)
    {
        var background = parent.GetComponent<Image>();
        if (background is null)
        {
            background = parent.gameObject.AddComponent<Image>();
        }

        background.color = new Color(0.015f, 0.055f, 0.075f, 0.82f);
        background.raycastTarget = false;

        if (localPlayer is null ||
            string.IsNullOrWhiteSpace(localPlayer.activeCharacterDefinitionId))
        {
            var text = createButtonText(parent, "NoCharacterLabel", "未选择角色", 18f, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return;
        }

        var inspectModel = buildInspectModelForDefinition(
            localPlayer.activeCharacterDefinitionId,
            0,
            localPlayer.playerNumericId,
            "playerCharacter:bottom",
            true);

        var cardObject = new GameObject(
            "LocalCharacterActionCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        cardObject.transform.SetParent(parent, false);
        var cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.06f, 0.08f);
        cardRect.anchorMax = new Vector2(0.42f, 0.94f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        var image = cardObject.GetComponent<Image>();
        image.sprite = inspectModel.artworkSprite ?? CardArtService.GetCardBackSprite();
        image.preserveAspect = true;
        image.raycastTarget = true;

        var inspectable = cardObject.AddComponent<BattlefieldInspectableUiCard>();
        inspectable.Configure(inspectModel, () => showCharacterSkillMenu(projection, localPlayer));

        var hintObject = new GameObject("Hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(parent, false);
        var hint = hintObject.GetComponent<TMP_Text>();
        hint.text = $"{inspectModel.displayName}\n左键：技能菜单\n右键按住：查看详情";
        hint.fontSize = 15f;
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.color = Color.white;
        hint.enableWordWrapping = true;
        if (primaryFont is not null)
        {
            hint.font = primaryFont;
        }

        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.47f, 0.08f);
        hintRect.anchorMax = new Vector2(0.96f, 0.94f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
    }

    private CardInspectViewModel buildInspectModelForDefinition(
        string definitionId,
        long cardInstanceNumericId,
        long ownerPlayerNumericId,
        string zoneKey,
        bool isFaceUp)
    {
        var displayEntry = CardDisplayCatalogService.TryGetEntry(definitionId);
        return new CardInspectViewModel
        {
            cardInstanceNumericId = cardInstanceNumericId,
            definitionId = definitionId,
            displayName = displayEntry?.name ?? CardNameLocalizer.Localize(definitionId),
            attributeText = CardDisplayCatalogService.BuildAttributeText(definitionId),
            rulesText = CardDisplayCatalogService.BuildRulesText(definitionId),
            ownerPlayerNumericId = ownerPlayerNumericId,
            zoneKey = zoneKey,
            isFaceUp = isFaceUp,
            artworkSprite = isFaceUp ? CardArtService.GetSpriteForDefinition(definitionId) : CardArtService.GetCardBackSprite(),
        };
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
                    if (tryBlockPresentationBusyAction())
                    {
                        return;
                    }

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

    private void showCharacterSkillMenu(
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel localPlayer)
    {
        ensureReferences();
        if (layoutRoot?.OverlayCanvas is null)
        {
            setStatus("OverlayCanvas 未初始化，无法打开技能菜单。");
            return;
        }

        hideOverlay(characterSkillMenuOverlay);
        characterSkillMenuOverlay = createCenteredOverlay("CharacterSkillMenuOverlay", new Vector2(980f, 720f));
        if (characterSkillMenuOverlay is null)
        {
            return;
        }

        var window = characterSkillMenuOverlay.Find("Window") as RectTransform;
        if (window is null)
        {
            return;
        }

        var characterDefinition = projection.characterDefinitions.FirstOrDefault(definition =>
            string.Equals(definition.definitionId, localPlayer.activeCharacterDefinitionId, StringComparison.Ordinal));
        var inspectModel = buildInspectModelForDefinition(
            localPlayer.activeCharacterDefinitionId,
            0,
            localPlayer.playerNumericId,
            "playerCharacter:bottom",
            true);

        var art = createImage(window, "CharacterArt", inspectModel.artworkSprite, new Vector2(0.035f, 0.17f), new Vector2(0.36f, 0.9f));
        art.preserveAspect = true;

        var title = createOverlayText(window, "Title", inspectModel.displayName, 28f, TextAlignmentOptions.Center, FontStyles.Bold);
        setAnchors(title.rectTransform, new Vector2(0.38f, 0.83f), new Vector2(0.95f, 0.93f));

        var resourceLine = createOverlayText(
            window,
            "ResourceLine",
            $"当前资源：魔力 {projection.mana} / 技能点 {projection.skillPoint} / 灵脉 {getTeamLeyline(projection, localPlayer.teamNumericId)} / 手牌 {localPlayer.handCount}",
            16f,
            TextAlignmentOptions.Center,
            FontStyles.Normal);
        resourceLine.color = new Color(0.82f, 0.92f, 1f, 1f);
        setAnchors(resourceLine.rectTransform, new Vector2(0.38f, 0.76f), new Vector2(0.95f, 0.82f));

        if (characterDefinition is null || characterDefinition.skills.Count == 0)
        {
            var empty = createOverlayText(window, "NoSkills", "当前角色没有可显示的技能定义。", 20f, TextAlignmentOptions.Center, FontStyles.Normal);
            setAnchors(empty.rectTransform, new Vector2(0.4f, 0.42f), new Vector2(0.94f, 0.58f));
        }
        else
        {
            var orderedSkills = characterDefinition.skills.OrderBy(skill => skill.skillOrder).Take(4).ToList();
            for (var index = 0; index < orderedSkills.Count; index += 1)
            {
                createSkillMenuButton(window, projection, localPlayer, orderedSkills[index], index);
            }
        }

        createCloseButton(window, "关闭", new Vector2(0.76f, 0.04f), new Vector2(0.95f, 0.12f), () => hideOverlay(characterSkillMenuOverlay));
        setOverlayVisible(characterSkillMenuOverlay, true);
    }

    private void createSkillMenuButton(
        RectTransform window,
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel localPlayer,
        ProjectionCharacterSkillDefinitionViewModel skill,
        int index)
    {
        var isResponseSkill = string.Equals(skill.skillTypeRaw, "响应", StringComparison.Ordinal);
        var canUse = canLikelyUseSkill(projection, localPlayer, skill);
        var yMax = 0.72f - index * 0.155f;
        var yMin = yMax - 0.13f;
        var buttonObject = new GameObject($"SkillOption_{skill.skillKey}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(window, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        setAnchors(rect, new Vector2(0.39f, yMin), new Vector2(0.95f, yMax));

        var image = buttonObject.GetComponent<Image>();
        image.color = canUse
            ? new Color(0.72f, 0.52f, 0.13f, 0.94f)
            : isResponseSkill
                ? new Color(0.36f, 0.18f, 0.48f, 0.82f)
                : new Color(0.12f, 0.28f, 0.34f, 0.82f);

        var button = buttonObject.GetComponent<Button>();
        button.interactable = !isResponseSkill && localPlayer.activeCharacterInstanceNumericId.HasValue;
        button.onClick.AddListener(() =>
        {
            if (tryBlockPresentationBusyAction())
            {
                return;
            }

            if (!localPlayer.activeCharacterInstanceNumericId.HasValue)
            {
                showMessagePopup("尚未选择角色。");
                return;
            }

            ensureReferences();
            if (networkController is null ||
                !networkController.TryUseSkill(localPlayer.activeCharacterInstanceNumericId.Value, skill.skillKey))
            {
                showMessagePopup(networkController?.LastError ?? $"技能请求未发送：{skill.skillName}");
                return;
            }

            hideOverlay(characterSkillMenuOverlay);
        });

        var title = createOverlayText(
            rect,
            "Title",
            $"{skill.skillName}（{emptyToDash(skill.skillCostRaw)}）",
            17f,
            TextAlignmentOptions.Left,
            FontStyles.Bold);
        setAnchors(title.rectTransform, new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.95f));

        var subtitle = createOverlayText(
            rect,
            "Subtitle",
            $"{(isResponseSkill ? "响应技" : emptyToDash(skill.skillTypeRaw))} / {emptyToDash(skill.effectText)}",
            12.5f,
            TextAlignmentOptions.TopLeft,
            FontStyles.Normal);
        subtitle.enableWordWrapping = true;
        subtitle.overflowMode = TextOverflowModes.Ellipsis;
        subtitle.color = canUse ? Color.white : new Color(0.74f, 0.78f, 0.82f, 1f);
        setAnchors(subtitle.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.56f));
    }

    private bool canLikelyUseSkill(
        ProjectionViewModel projection,
        ProjectionPlayerSummaryViewModel localPlayer,
        ProjectionCharacterSkillDefinitionViewModel skill)
    {
        if (localPlayer.activeCharacterInstanceNumericId is null ||
            projection.currentPlayerNumericId != projection.viewerPlayerNumericId ||
            !string.Equals(projection.currentPhase, "action", StringComparison.Ordinal) ||
            string.Equals(skill.skillTypeRaw, "响应", StringComparison.Ordinal) ||
            projection.skillPoint <= 0)
        {
            return false;
        }

        var requiredMana = parseCircledNumber(skill.skillCostRaw);
        var requiredLeyline = parseRomanLeyline(skill.skillCostRaw);
        if (requiredMana > projection.mana ||
            requiredLeyline > getTeamLeyline(projection, localPlayer.teamNumericId))
        {
            return false;
        }

        if ((skill.skillCostRaw.Contains("弃", StringComparison.Ordinal) ||
             skill.effectText.Contains("弃", StringComparison.Ordinal)) &&
            localPlayer.handCount <= 0)
        {
            return false;
        }

        return true;
    }

    private int getTeamLeyline(ProjectionViewModel projection, long teamNumericId)
    {
        return projection.teamSummaries.FirstOrDefault(team => team.teamNumericId == teamNumericId)?.leyline ?? 0;
    }

    private static int parseCircledNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var max = 0;
        foreach (var character in value)
        {
            max = Math.Max(max, character switch
            {
                '①' => 1,
                '②' => 2,
                '③' => 3,
                '④' => 4,
                '⑤' => 5,
                '⑥' => 6,
                '⑦' => 7,
                '⑧' => 8,
                '⑨' => 9,
                '⑩' => 10,
                >= '0' and <= '9' => character - '0',
                _ => 0,
            });
        }

        return max;
    }

    private static int parseRomanLeyline(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (value.Contains("V", StringComparison.Ordinal))
        {
            return 5;
        }

        if (value.Contains("III", StringComparison.Ordinal))
        {
            return 3;
        }

        if (value.Contains("II", StringComparison.Ordinal))
        {
            return 2;
        }

        return value.Contains("I", StringComparison.Ordinal) ? 1 : 0;
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

    private RectTransform? createCenteredOverlay(string name, Vector2 size)
    {
        if (layoutRoot?.OverlayCanvas is null)
        {
            return null;
        }

        var existing = layoutRoot.OverlayCanvas.transform.Find(name);
        if (existing is not null)
        {
            destroy(existing.gameObject);
        }

        var rootObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        rootObject.transform.SetParent(layoutRoot.OverlayCanvas.transform, false);
        var rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dimmer = rootObject.GetComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.58f);
        dimmer.raycastTarget = true;

        var group = rootObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        var windowObject = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        windowObject.transform.SetParent(rootRect, false);
        var window = windowObject.GetComponent<RectTransform>();
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = size;
        window.anchoredPosition = Vector2.zero;
        var image = windowObject.GetComponent<Image>();
        image.color = new Color(0.015f, 0.05f, 0.07f, 0.97f);
        image.raycastTarget = true;
        rootRect.SetAsLastSibling();
        return rootRect;
    }

    private static void setOverlayVisible(RectTransform? overlay, bool visible)
    {
        if (overlay is null)
        {
            return;
        }

        var group = overlay.GetComponent<CanvasGroup>();
        if (group is null)
        {
            group = overlay.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        overlay.gameObject.SetActive(visible);
    }

    private static void hideOverlay(RectTransform? overlay)
    {
        if (overlay is null)
        {
            return;
        }

        setOverlayVisible(overlay, false);
    }

    private Image createImage(Transform parent, string name, Sprite? sprite, Vector2 anchorMin, Vector2 anchorMax)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.sprite = sprite ?? CardArtService.GetCardBackSprite();
        image.raycastTarget = false;
        setAnchors(image.rectTransform, anchorMin, anchorMax);
        return image;
    }

    private TMP_Text createOverlayText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        FontStyles fontStyle)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (primaryFont is not null)
        {
            text.font = primaryFont;
        }

        return text;
    }

    private Button createCloseButton(
        RectTransform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        UnityEngine.Events.UnityAction clicked)
    {
        var buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        setAnchors(rect, anchorMin, anchorMax);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.48f, 0.56f, 0.94f);
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(clicked);

        var text = createOverlayText(buttonObject.transform, "Label", label, 16f, TextAlignmentOptions.Center, FontStyles.Bold);
        setAnchors(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private void showMessagePopup(string message)
    {
        ensureReferences();
        if (layoutRoot?.OverlayCanvas is null)
        {
            setStatus(message);
            return;
        }

        hideOverlay(messageOverlay);
        messageOverlay = createCenteredOverlay("BattlefieldMessagePopup", new Vector2(520f, 220f));
        var window = messageOverlay?.Find("Window") as RectTransform;
        if (window is null)
        {
            setStatus(message);
            return;
        }

        var text = createOverlayText(window, "Message", message, 20f, TextAlignmentOptions.Center, FontStyles.Bold);
        setAnchors(text.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.82f));
        createCloseButton(window, "知道了", new Vector2(0.34f, 0.08f), new Vector2(0.66f, 0.26f), () => hideOverlay(messageOverlay));
        setStatus(message);
        setOverlayVisible(messageOverlay, true);
    }

    private static void setAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
        if (tryBlockPresentationBusyAction())
        {
            return;
        }

        ensureReferences();
        if (networkController is null || !networkController.TryResolveAnomaly(targetPlayerNumericId))
        {
            showMessagePopup(networkController?.LastError ?? "异变解决请求未发送。");
            return;
        }

        isAnomalyResolveConfirmOpen = false;
    }

    private void showAnomalyResolveMenu(ProjectionViewModel projection)
    {
        ensureReferences();
        if (layoutRoot?.OverlayCanvas is null)
        {
            setStatus("OverlayCanvas 未初始化，无法打开异变菜单。");
            return;
        }

        if (!projection.currentAnomaly.hasCurrentAnomaly ||
            string.IsNullOrWhiteSpace(projection.currentAnomaly.definitionId))
        {
            showMessagePopup("当前没有可尝试解决的异变。");
            return;
        }

        hideOverlay(anomalyResolveMenuOverlay);
        anomalyResolveMenuOverlay = createCenteredOverlay("AnomalyResolveMenuOverlay", new Vector2(920f, 680f));
        var window = anomalyResolveMenuOverlay?.Find("Window") as RectTransform;
        if (window is null)
        {
            return;
        }

        var inspectModel = buildInspectModelForDefinition(
            projection.currentAnomaly.definitionId,
            0,
            0,
            "currentAnomaly",
            true);
        var art = createImage(window, "AnomalyArt", inspectModel.artworkSprite, new Vector2(0.04f, 0.17f), new Vector2(0.36f, 0.9f));
        art.preserveAspect = true;

        var title = createOverlayText(window, "Title", inspectModel.displayName, 28f, TextAlignmentOptions.Center, FontStyles.Bold);
        setAnchors(title.rectTransform, new Vector2(0.38f, 0.84f), new Vector2(0.95f, 0.94f));

        var detail = createOverlayText(
            window,
            "Detail",
            buildAnomalyMenuText(projection, inspectModel),
            17f,
            TextAlignmentOptions.TopLeft,
            FontStyles.Normal);
        detail.overflowMode = TextOverflowModes.Overflow;
        setAnchors(detail.rectTransform, new Vector2(0.4f, 0.24f), new Vector2(0.94f, 0.82f));

        var isLocalTurn = projection.currentPlayerNumericId == projection.viewerPlayerNumericId;
        var resolveButton = createCloseButton(
            window,
            "尝试解决该异变",
            new Vector2(0.42f, 0.08f),
            new Vector2(0.72f, 0.17f),
            () =>
            {
                if (tryBlockPresentationBusyAction())
                {
                    return;
                }

                ensureReferences();
                if (networkController is null || !networkController.TryResolveAnomaly())
                {
                    showMessagePopup(networkController?.LastError ?? "异变解决请求未发送。");
                    return;
                }

                hideOverlay(anomalyResolveMenuOverlay);
            });
        resolveButton.interactable = isLocalTurn;
        resolveButton.GetComponent<Image>().color = isLocalTurn
            ? new Color(0.72f, 0.52f, 0.13f, 0.96f)
            : new Color(0.16f, 0.16f, 0.16f, 0.72f);

        createCloseButton(window, "关闭", new Vector2(0.76f, 0.08f), new Vector2(0.94f, 0.17f), () => hideOverlay(anomalyResolveMenuOverlay));
        setOverlayVisible(anomalyResolveMenuOverlay, true);
    }

    private static string buildAnomalyMenuText(ProjectionViewModel projection, CardInspectViewModel inspectModel)
    {
        var builder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(inspectModel.rulesText))
        {
            builder.AppendLine(inspectModel.rulesText);
            builder.AppendLine();
        }

        builder.AppendLine($"当前阶段：{localizePhase(projection.currentPhase)}");
        builder.AppendLine(projection.currentAnomaly.hasResolvedThisTurn
            ? "本回合已解决过异变。"
            : "本回合尚未解决该异变。");
        if (!string.IsNullOrWhiteSpace(projection.currentAnomaly.oncePerTurnHint))
        {
            builder.AppendLine(projection.currentAnomaly.oncePerTurnHint);
        }

        return builder.ToString().Trim();
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
        lastClickedWorldCardView = view;
        var model = view.CurrentModel;
        if (string.Equals(model.zoneKey, "currentAnomaly", StringComparison.Ordinal))
        {
            if (latestProjection is not null)
            {
                showAnomalyResolveMenu(latestProjection);
            }
            return;
        }

        if (tryOpenOverlayCards(model))
        {
            return;
        }

        if (tryOpenBrowsableZone(model))
        {
            return;
        }

    }

    private bool tryOpenOverlayCards(WorldCardViewModel model)
    {
        if (zoneCardListOverlayView is null || latestProjection is null || model.overlayCardCount <= 0)
        {
            return false;
        }

        if (model.zoneKey.StartsWith("playerCharacter:", StringComparison.Ordinal))
        {
            var player = latestProjection.playerSummaries.FirstOrDefault(candidate =>
                candidate.playerNumericId == model.ownerPlayerNumericId);
            if (player is null)
            {
                return false;
            }

            var title = $"{CardNameLocalizer.Localize(model.definitionId)} 下方盖放";
            if (player.characterSetAsideCardsVisible)
            {
                zoneCardListOverlayView.Show(title, player.characterSetAsideCards);
            }
            else
            {
                zoneCardListOverlayView.ShowHiddenCount(
                    title,
                    player.characterSetAsideCount,
                    "这些牌当前未向你公开。");
            }
            return true;
        }

        var projectionCard = findProjectionCard(model.cardInstanceNumericId);
        if (projectionCard is null)
        {
            zoneCardListOverlayView.ShowHiddenCount(
                $"{CardNameLocalizer.Localize(model.definitionId)} 下方叠放",
                model.overlayCardCount,
                "当前投影只公开了叠放数量。");
            return true;
        }

        if (projectionCard.overlayCards.Count > 0)
        {
            zoneCardListOverlayView.Show(
                $"{CardNameLocalizer.Localize(model.definitionId)} 下方叠放",
                projectionCard.overlayCards);
        }
        else
        {
            zoneCardListOverlayView.ShowHiddenCount(
                $"{CardNameLocalizer.Localize(model.definitionId)} 下方叠放",
                projectionCard.overlayCardCount,
                "当前投影只公开了叠放数量。");
        }

        return true;
    }

    private ProjectionCardViewModel? findProjectionCard(long cardInstanceNumericId)
    {
        if (latestProjection is null || cardInstanceNumericId <= 0)
        {
            return null;
        }

        foreach (var card in enumerateProjectionCards(latestProjection))
        {
            if (card.cardInstanceNumericId == cardInstanceNumericId)
            {
                return card;
            }
        }

        return null;
    }

    private static IEnumerable<ProjectionCardViewModel> enumerateProjectionCards(ProjectionViewModel projection)
    {
        foreach (var card in projection.handCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.discardCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.fieldCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.summonZoneCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.sakuraCakeCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.gapZoneCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var card in projection.resolvedAnomalyCards)
        {
            yield return card;
            foreach (var overlayCard in enumerateOverlayCards(card))
            {
                yield return overlayCard;
            }
        }
        foreach (var player in projection.playerSummaries)
        {
            foreach (var card in player.fieldCards)
            {
                yield return card;
                foreach (var overlayCard in enumerateOverlayCards(card))
                {
                    yield return overlayCard;
                }
            }
            foreach (var card in player.discardCards)
            {
                yield return card;
                foreach (var overlayCard in enumerateOverlayCards(card))
                {
                    yield return overlayCard;
                }
            }
            foreach (var card in player.characterSetAsideCards)
            {
                yield return card;
                foreach (var overlayCard in enumerateOverlayCards(card))
                {
                    yield return overlayCard;
                }
            }
        }
    }

    private static IEnumerable<ProjectionCardViewModel> enumerateOverlayCards(ProjectionCardViewModel card)
    {
        foreach (var overlayCard in card.overlayCards)
        {
            yield return overlayCard;
            foreach (var nestedOverlayCard in enumerateOverlayCards(overlayCard))
            {
                yield return nestedOverlayCard;
            }
        }
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

        if (string.Equals(model.zoneKey, "resolvedAnomaly", System.StringComparison.Ordinal))
        {
            zoneCardListOverlayView.Show("已解决异变牌堆", latestProjection.resolvedAnomalyCards);
            return true;
        }

        if (model.zoneKey.StartsWith("playerDeck:", System.StringComparison.Ordinal))
        {
            var deckSeatKey = model.zoneKey["playerDeck:".Length..];
            var deckPlayer = resolvePlayerSummaryForSeatKey(deckSeatKey);
            if (deckPlayer is null)
            {
                return false;
            }

            zoneCardListOverlayView.ShowDeckKnowledge(
                $"Player {deckPlayer.playerNumericId} 卡组公开信息",
                deckPlayer.ownedCardKnowledge);
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
        if (!isPublicSummonCardZone(model.zoneKey))
        {
            return;
        }

        if (model.cardInstanceNumericId <= 0)
        {
            setStatus("该区域当前只有牌堆摘要，不能直接召唤。");
            return;
        }

        if (!model.isSummonableByViewer)
        {
            setStatus(localizeSummonUnavailableReason(model.summonUnavailableReasonKey, model.effectiveSummonSigilCost));
            return;
        }

        if (tryBlockPresentationBusyAction())
        {
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

    private static string localizeSummonUnavailableReason(string reasonKey, int? effectiveCost)
    {
        var costText = effectiveCost.HasValue ? $"需要 {effectiveCost.Value} 灵符。" : string.Empty;
        return reasonKey switch
        {
            "interactionPending" => "当前有待处理的选择或响应，暂时不能召唤宝具。",
            "notViewerTurn" => "现在不是你的回合，不能召唤宝具。",
            "wrongPhase" => "当前不是召唤阶段，不能召唤宝具。",
            "cardNotInSummonableZone" => "这张牌不在可召唤区域。",
            "insufficientLockedSigil" => string.IsNullOrEmpty(costText)
                ? "当前锁定灵符不足，不能召唤这张宝具。"
                : $"当前锁定灵符不足，不能召唤这张宝具。{costText}",
            _ => string.IsNullOrWhiteSpace(reasonKey)
                ? "当前不能召唤这张宝具。"
                : $"当前不能召唤这张宝具。原因：{reasonKey}",
        };
    }

    private bool tryBlockPresentationBusyAction()
    {
        if (!BattlefieldInteractionPresentationGate.IsPresentationBusy)
        {
            return false;
        }

        setStatus($"效果演出处理中，请稍候 {BattlefieldInteractionPresentationGate.RemainingDelaySeconds:0.0}s。");
        return true;
    }

    private void setStatus(string value)
    {
        if (statusText is not null)
        {
            statusText.text = value;
        }
    }

}
}

