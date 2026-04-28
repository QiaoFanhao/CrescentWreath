using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrescentWreath.Client.Net
{
public sealed class SocketDebugPanel : MonoBehaviour
{
    [SerializeField] private float panelWidth = 1080f;
    [SerializeField] private float panelHeight = 980f;
    [SerializeField] private string wsUrl = "ws://127.0.0.1:18080/ws";
    [SerializeField] private string viewerPlayerNumericIdText = "1";
    [SerializeField] private string actorPlayerNumericIdText = "1";
    [SerializeField] private string playCardInstanceNumericIdText = "0";
    [SerializeField] private string summonCardInstanceNumericIdText = "0";
    [SerializeField] private string defenseTypeKeyText = "physical";
    [SerializeField] private string defenseCardInstanceNumericIdText = "0";

    private const int MaxFlowTraceLines = 40;

    private ServerBridge? bridge;
    private readonly object stateLock = new();
    private readonly DebugFlowChecklistRuntime flowChecklistRuntime = new();
    private readonly List<string> flowTraceLines = new();

    private string connectionState = "disconnected";
    private string latestRawRequestJson = string.Empty;
    private string latestRawResponseJson = string.Empty;
    private string latestError = string.Empty;
    private string summaryText = "No response yet.";
    private ProjectionViewModel latestProjection = ProjectionViewModel.createDefault(1);
    private bool requestInFlight;

    private long? selectedHandCardId;
    private long? selectedSummonCardId;
    private long? selectedSakuraCakeCardId;
    private long? selectedDefenseCardId;

    private long? lastPlayedSelectedHandCardId;
    private long? lastSummonedSelectedCardId;

    private string pendingActionType = string.Empty;
    private string currentResponseActionType = string.Empty;

    private Vector2 projectionScroll;
    private Vector2 rawRequestJsonScroll;
    private Vector2 rawResponseJsonScroll;
    private Vector2 rootScroll;
    private Vector2 flowTraceScroll;

    public static long? PruneSelectionIfMissing(long? selectedCardId, List<ProjectionCardViewModel> cards)
    {
        if (!selectedCardId.HasValue)
        {
            return null;
        }

        foreach (var card in cards)
        {
            if (card.cardInstanceNumericId == selectedCardId.Value)
            {
                return selectedCardId;
            }
        }

        return null;
    }

    public static bool TryResolveSelectedCardId(long? selectedCardId, out long resolvedCardId)
    {
        if (selectedCardId.HasValue)
        {
            resolvedCardId = selectedCardId.Value;
            return true;
        }

        resolvedCardId = 0;
        return false;
    }

    public static bool TryResolveSelectedOrManualCardId(long? selectedCardId, string manualText, out long resolvedCardId)
    {
        if (TryResolveSelectedCardId(selectedCardId, out resolvedCardId))
        {
            return true;
        }

        return long.TryParse(manualText, out resolvedCardId);
    }

    private void OnEnable()
    {
        bridge = new ServerBridge();
        bridge.OnConnectionStateChanged += onConnectionStateChanged;
        bridge.OnRawRequest += onRawRequest;
        bridge.OnRawResponse += onRawResponse;
        bridge.OnSummaryUpdated += onSummaryUpdated;
        bridge.OnProjectionUpdated += onProjectionUpdated;
        bridge.OnError += onError;
    }

    private void OnDisable()
    {
        if (bridge is null)
        {
            return;
        }

        bridge.OnConnectionStateChanged -= onConnectionStateChanged;
        bridge.OnRawRequest -= onRawRequest;
        bridge.OnRawResponse -= onRawResponse;
        bridge.OnSummaryUpdated -= onSummaryUpdated;
        bridge.OnProjectionUpdated -= onProjectionUpdated;
        bridge.OnError -= onError;
        bridge.Dispose();
        bridge = null;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20f, 20f, panelWidth, panelHeight), "Socket Debug", GUI.skin.window);
        rootScroll = GUILayout.BeginScrollView(
            rootScroll,
            GUILayout.Width(panelWidth - 16f),
            GUILayout.Height(panelHeight - 30f));

        drawConnectionSection();
        GUILayout.Space(8f);
        drawPhaseActionSection();
        GUILayout.Space(8f);
        drawCardActionSection();
        GUILayout.Space(8f);
        drawDefenseActionSection();
        GUILayout.Space(8f);
        drawProjectionSection();
        GUILayout.Space(8f);
        drawFlowChecklistSection();
        GUILayout.Space(8f);
        drawSummarySection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void drawConnectionSection()
    {
        GUILayout.Label("WebSocket URL");
        wsUrl = GUILayout.TextField(wsUrl, GUILayout.Height(26f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Viewer", GUILayout.Width(80f));
        viewerPlayerNumericIdText = GUILayout.TextField(viewerPlayerNumericIdText, GUILayout.Width(120f));
        GUILayout.Label("Actor", GUILayout.Width(80f));
        actorPlayerNumericIdText = GUILayout.TextField(actorPlayerNumericIdText, GUILayout.Width(120f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Connect", GUILayout.Width(120f)))
        {
            connect();
        }

        if (GUILayout.Button("Disconnect", GUILayout.Width(120f)))
        {
            bridge?.Disconnect();
        }

        GUILayout.Label($"Connection: {connectionState}");
        GUILayout.Label($"RequestInFlight: {requestInFlight}");
        GUILayout.EndHorizontal();
    }

    private void drawPhaseActionSection()
    {
        var canSend = canSendRequest();
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("EnterAction", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterActionPhase", () => bridge?.SendEnterActionPhase());
        }

        if (GUILayout.Button("EnterSummon", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterSummonPhase", () => bridge?.SendEnterSummonPhase());
        }

        if (GUILayout.Button("EnterEnd", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterEndPhase", () => bridge?.SendEnterEndPhase());
        }

        if (GUILayout.Button("StartNextTurn", GUILayout.Width(140f)))
        {
            applyViewerAndActor();
            sendTrackedAction("startNextTurn", () => bridge?.SendStartNextTurn());
        }
        GUILayout.EndHorizontal();

        GUI.enabled = previousEnabled;
    }

    private void drawCardActionSection()
    {
        ProjectionViewModel projectionSnapshot;
        long? selectedHandCardIdSnapshot;
        long? selectedSummonCardIdSnapshot;
        long? selectedSakuraCakeCardIdSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            selectedHandCardIdSnapshot = selectedHandCardId;
            selectedSummonCardIdSnapshot = selectedSummonCardId;
            selectedSakuraCakeCardIdSnapshot = selectedSakuraCakeCardId;
        }

        var canSend = canSendRequest();
        var canPlaySelected = canSend;
        var canPlayFirstHand = canSend && projectionSnapshot.handCards.Count > 0;
        var canSummonSelected = canSend;
        var canSummonSakuraSelected = canSend;
        var canSummonFirst = canSend && projectionSnapshot.summonZoneCards.Count > 0;
        var previousEnabled = GUI.enabled;

        GUILayout.BeginHorizontal();
        GUI.enabled = canSend;
        if (GUILayout.Button("Draw", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("drawOneCard", () => bridge?.SendDrawOneCard());
        }

        GUILayout.Label("Play Card Id", GUILayout.Width(90f));
        playCardInstanceNumericIdText = GUILayout.TextField(playCardInstanceNumericIdText, GUILayout.Width(130f));

        if (GUILayout.Button("Play", GUILayout.Width(100f)))
        {
            if (TryResolveSelectedOrManualCardId(selectedHandCardIdSnapshot, playCardInstanceNumericIdText, out var cardInstanceId))
            {
                lock (stateLock)
                {
                    lastPlayedSelectedHandCardId = selectedHandCardIdSnapshot;
                }

                applyViewerAndActor();
                sendTrackedAction("playTreasureCard", () => bridge?.SendPlayTreasureCard(cardInstanceId));
            }
            else
            {
                onError("Play requires selectedHandCardId or valid manual cardInstanceId.");
            }
        }

        GUI.enabled = canPlaySelected;
        if (GUILayout.Button("Play Selected", GUILayout.Width(120f)))
        {
            if (TryResolveSelectedCardId(selectedHandCardIdSnapshot, out var selectedCardId))
            {
                lock (stateLock)
                {
                    lastPlayedSelectedHandCardId = selectedCardId;
                }

                applyViewerAndActor();
                sendTrackedAction("playTreasureCard", () => bridge?.SendPlayTreasureCard(selectedCardId));
            }
            else
            {
                onError("Play Selected: hand card not selected.");
            }
        }

        GUI.enabled = canPlayFirstHand;
        if (GUILayout.Button("Play First Hand", GUILayout.Width(140f)))
        {
            lock (stateLock)
            {
                lastPlayedSelectedHandCardId = null;
            }

            applyViewerAndActor();
            sendTrackedAction("playTreasureCard", () => bridge?.SendPlayTreasureCard(projectionSnapshot.handCards[0].cardInstanceNumericId));
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = canSend;
        GUILayout.Label("Summon Card Id", GUILayout.Width(105f));
        summonCardInstanceNumericIdText = GUILayout.TextField(summonCardInstanceNumericIdText, GUILayout.Width(130f));

        if (GUILayout.Button("Summon", GUILayout.Width(100f)))
        {
            if (TryResolveSelectedOrManualCardId(selectedSummonCardIdSnapshot, summonCardInstanceNumericIdText, out var summonCardInstanceId))
            {
                lock (stateLock)
                {
                    lastSummonedSelectedCardId = selectedSummonCardIdSnapshot;
                }

                applyViewerAndActor();
                sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(summonCardInstanceId));
            }
            else
            {
                onError("Summon requires selectedSummonCardId or valid manual cardInstanceId.");
            }
        }

        GUI.enabled = canSummonSelected;
        if (GUILayout.Button("Summon Selected", GUILayout.Width(120f)))
        {
            if (TryResolveSelectedCardId(selectedSummonCardIdSnapshot, out var selectedCardId))
            {
                lock (stateLock)
                {
                    lastSummonedSelectedCardId = selectedCardId;
                }

                applyViewerAndActor();
                sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(selectedCardId));
            }
            else
            {
                onError("Summon Selected: summon card not selected.");
            }
        }

        GUI.enabled = canSummonSakuraSelected;
        if (GUILayout.Button("Summon SakuraCake Selected", GUILayout.Width(190f)))
        {
            if (TryResolveSelectedCardId(selectedSakuraCakeCardIdSnapshot, out var selectedCardId))
            {
                lock (stateLock)
                {
                    lastSummonedSelectedCardId = selectedCardId;
                }

                applyViewerAndActor();
                sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(selectedCardId));
            }
            else
            {
                onError("Summon SakuraCake Selected: sakura cake card not selected.");
            }
        }

        GUI.enabled = canSummonFirst;
        if (GUILayout.Button("Summon First", GUILayout.Width(140f)))
        {
            lock (stateLock)
            {
                lastSummonedSelectedCardId = null;
            }

            applyViewerAndActor();
            sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(projectionSnapshot.summonZoneCards[0].cardInstanceNumericId));
        }
        GUILayout.EndHorizontal();

        GUI.enabled = previousEnabled;
    }

    private void drawDefenseActionSection()
    {
        long? selectedHandCardIdSnapshot;
        long? selectedDefenseCardIdSnapshot;
        lock (stateLock)
        {
            selectedHandCardIdSnapshot = selectedHandCardId;
            selectedDefenseCardIdSnapshot = selectedDefenseCardId;
        }

        var canSend = canSendRequest();
        var canSetDefenseFromHand = canSend && selectedHandCardIdSnapshot.HasValue;
        var canDefenseSelected = canSend;
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Defense:Fixed", GUILayout.Width(140f)))
        {
            applyViewerAndActor();
            sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFixedReduce1());
        }

        GUI.enabled = canSetDefenseFromHand;
        if (GUILayout.Button("Set Defense From Hand", GUILayout.Width(170f)))
        {
            if (selectedHandCardIdSnapshot.HasValue)
            {
                lock (stateLock)
                {
                    selectedDefenseCardId = selectedHandCardIdSnapshot.Value;
                }
            }
            else
            {
                onError("Set Defense From Hand: hand card not selected.");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = canSend;
        GUILayout.Label("FormalType", GUILayout.Width(80f));
        defenseTypeKeyText = GUILayout.TextField(defenseTypeKeyText, GUILayout.Width(120f));
        GUILayout.Label("Card Id", GUILayout.Width(55f));
        defenseCardInstanceNumericIdText = GUILayout.TextField(defenseCardInstanceNumericIdText, GUILayout.Width(120f));

        if (GUILayout.Button("Defense:Formal", GUILayout.Width(130f)))
        {
            if (string.IsNullOrWhiteSpace(defenseTypeKeyText))
            {
                onError("defenseTypeKey is required.");
            }
            else if (TryResolveSelectedOrManualCardId(selectedDefenseCardIdSnapshot, defenseCardInstanceNumericIdText, out var defenseCardInstanceId))
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(defenseTypeKeyText, defenseCardInstanceId));
            }
            else
            {
                onError("Defense:Formal requires selectedDefenseCardId or valid manual defenseCardInstanceId.");
            }
        }

        GUI.enabled = canDefenseSelected;
        if (GUILayout.Button("Defense Selected", GUILayout.Width(130f)))
        {
            if (string.IsNullOrWhiteSpace(defenseTypeKeyText))
            {
                onError("defenseTypeKey is required.");
            }
            else if (TryResolveSelectedCardId(selectedDefenseCardIdSnapshot, out var selectedCardId))
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(defenseTypeKeyText, selectedCardId));
            }
            else
            {
                onError("Defense Selected: defense card not selected.");
            }
        }

        GUILayout.EndHorizontal();
        GUI.enabled = previousEnabled;
    }

    private void drawProjectionSection()
    {
        ProjectionViewModel projectionSnapshot;
        long? selectedHandCardIdSnapshot;
        long? selectedSummonCardIdSnapshot;
        long? selectedSakuraCakeCardIdSnapshot;
        long? selectedDefenseCardIdSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            selectedHandCardIdSnapshot = selectedHandCardId;
            selectedSummonCardIdSnapshot = selectedSummonCardId;
            selectedSakuraCakeCardIdSnapshot = selectedSakuraCakeCardId;
            selectedDefenseCardIdSnapshot = selectedDefenseCardId;
        }

        GUILayout.Label("Projection");
        projectionScroll = GUILayout.BeginScrollView(projectionScroll, GUILayout.Height(320f));
        GUILayout.Label($"turnNumber: {projectionSnapshot.turnNumber}");
        GUILayout.Label($"currentPhase: {projectionSnapshot.currentPhase}");
        GUILayout.Label($"currentPlayerNumericId: {projectionSnapshot.currentPlayerNumericId?.ToString() ?? "(null)"}");
        GUILayout.Label(
            $"selfResources: mana={projectionSnapshot.mana}, skillPoint={projectionSnapshot.skillPoint}, sigilPreview={projectionSnapshot.sigilPreview}, lockedSigil={projectionSnapshot.lockedSigil?.ToString() ?? "(null)"}");
        GUILayout.Label($"selfDiscardCount: {projectionSnapshot.discardCount}");
        GUILayout.Label($"selfHandCardCount: {projectionSnapshot.viewerHandCardCount}");

        GUILayout.Label($"Selected Hand Card: {selectedHandCardIdSnapshot?.ToString() ?? "(none)"}");
        drawSelectableCardList("selfHandCards", projectionSnapshot.handCards, selectedHandCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedHandCardId = cardId;
            }
        });

        GUILayout.Label("selfFieldCards");
        drawFieldCardList(projectionSnapshot.fieldCards);

        GUILayout.Label($"Selected Summon Card: {selectedSummonCardIdSnapshot?.ToString() ?? "(none)"}");
        drawSelectableCardList("summonZoneCards", projectionSnapshot.summonZoneCards, selectedSummonCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedSummonCardId = cardId;
            }
        });

        GUILayout.Label($"Selected SakuraCake Card: {selectedSakuraCakeCardIdSnapshot?.ToString() ?? "(none)"}");
        drawSelectableCardList("sakuraCakeCards", projectionSnapshot.sakuraCakeCards, selectedSakuraCakeCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedSakuraCakeCardId = cardId;
            }
        });

        GUILayout.Label($"Selected Defense Card: {selectedDefenseCardIdSnapshot?.ToString() ?? "(none)"}");

        GUILayout.Label(
            $"activeCharacter: hp={projectionSnapshot.activeCharacterCurrentHp?.ToString() ?? "(null)"}/{projectionSnapshot.activeCharacterMaxHp?.ToString() ?? "(null)"} statuses=[{string.Join(",", projectionSnapshot.activeCharacterStatusKeys)}]");
        GUILayout.Label(
            $"interaction: hasInput={projectionSnapshot.interaction.hasInputContext} requiredPlayer={projectionSnapshot.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(null)"} choiceCount={projectionSnapshot.interaction.inputChoiceCount} hasResponse={projectionSnapshot.interaction.hasResponseWindow} currentResponder={projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(null)"} responderCount={projectionSnapshot.interaction.responseResponderCount}");

        GUILayout.Label("recentEventLog:");
        if (projectionSnapshot.eventLog.Count == 0)
        {
            GUILayout.Label("(none)");
        }
        else
        {
            foreach (var eventLine in projectionSnapshot.eventLog)
            {
                GUILayout.Label(eventLine);
            }
        }

        GUILayout.EndScrollView();
    }

    private void drawFlowChecklistSection()
    {
        List<DebugFlowStepState> stepStates;
        string recommendedNextStep;
        string lastStepResult;
        List<string> flowTraceLinesSnapshot;

        lock (stateLock)
        {
            stepStates = flowChecklistRuntime.getStepStatesSnapshot();
            recommendedNextStep = flowChecklistRuntime.recommendedNextStep;
            lastStepResult = flowChecklistRuntime.lastStepResultText;
            flowTraceLinesSnapshot = new List<string>(flowTraceLines);
        }

        GUILayout.Label("Flow Checklist");
        GUILayout.Label($"Recommended Next Step: {recommendedNextStep}");
        GUILayout.Label($"Last Step Result: {lastStepResult}");

        for (var index = 0; index < stepStates.Count; index++)
        {
            var step = stepStates[index];
            GUILayout.Label($"[{step.stepNumber}] {step.displayName} -> {step.status} {step.note}");
        }

        GUILayout.Label("Flow Trace");
        flowTraceScroll = GUILayout.BeginScrollView(flowTraceScroll, GUILayout.Height(120f));
        if (flowTraceLinesSnapshot.Count == 0)
        {
            GUILayout.Label("(no successful responses yet)");
        }
        else
        {
            for (var index = 0; index < flowTraceLinesSnapshot.Count; index++)
            {
                GUILayout.Label(flowTraceLinesSnapshot[index]);
            }
        }

        GUILayout.EndScrollView();
    }

    private void drawSelectableCardList(
        string title,
        List<ProjectionCardViewModel> cards,
        long? selectedCardId,
        Action<long> onSelect)
    {
        GUILayout.Label(title);
        if (cards.Count == 0)
        {
            GUILayout.Label("(none)");
            return;
        }

        var previousEnabled = GUI.enabled;
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var isSelected = selectedCardId.HasValue && selectedCardId.Value == card.cardInstanceNumericId;
            GUI.enabled = true;
            var labelPrefix = isSelected ? "[Selected] " : string.Empty;
            var label = $"{labelPrefix}{card.cardInstanceNumericId} : {card.definitionId} ({card.zoneKey})";
            if (GUILayout.Button(label, GUILayout.Height(24f)))
            {
                onSelect(card.cardInstanceNumericId);
            }
        }

        GUI.enabled = previousEnabled;
    }

    private void drawFieldCardList(List<ProjectionCardViewModel> cards)
    {
        if (cards.Count == 0)
        {
            GUILayout.Label("(none)");
            return;
        }

        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var label = $"{card.cardInstanceNumericId} : {card.definitionId} ({card.zoneKey})";
            if (GUILayout.Button(label, GUILayout.Height(24f)))
            {
                onError($"Field card clicked: {card.cardInstanceNumericId} (display only).");
            }
        }
    }

    private void drawSummarySection()
    {
        string rawRequestJsonSnapshot;
        string rawResponseJsonSnapshot;
        string errorSnapshot;
        string summarySnapshot;
        lock (stateLock)
        {
            rawRequestJsonSnapshot = latestRawRequestJson;
            rawResponseJsonSnapshot = latestRawResponseJson;
            errorSnapshot = latestError;
            summarySnapshot = summaryText;
        }

        GUILayout.Label("Summary");
        GUILayout.TextArea(summarySnapshot, GUILayout.Height(85f));

        GUILayout.Label("Error");
        GUILayout.TextArea(string.IsNullOrEmpty(errorSnapshot) ? "(none)" : errorSnapshot, GUILayout.Height(45f));

        GUILayout.Label("Last Request Raw JSON");
        rawRequestJsonScroll = GUILayout.BeginScrollView(rawRequestJsonScroll, GUILayout.Height(120f));
        GUILayout.TextArea(string.IsNullOrEmpty(rawRequestJsonSnapshot) ? "(no request yet)" : rawRequestJsonSnapshot, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.Label("Last Response Raw JSON");
        rawResponseJsonScroll = GUILayout.BeginScrollView(rawResponseJsonScroll, GUILayout.Height(120f));
        GUILayout.TextArea(string.IsNullOrEmpty(rawResponseJsonSnapshot) ? "(no response yet)" : rawResponseJsonSnapshot, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
    }

    private void connect()
    {
        lock (stateLock)
        {
            connectionState = "connecting";
            latestError = string.Empty;
            requestInFlight = false;
            pendingActionType = string.Empty;
            currentResponseActionType = string.Empty;
        }

        applyViewerAndActor();
        bridge?.Connect(wsUrl);
    }

    private void applyViewerAndActor()
    {
        if (bridge is null)
        {
            return;
        }

        if (long.TryParse(viewerPlayerNumericIdText, out var viewerPlayerId))
        {
            bridge.viewerPlayerNumericId = viewerPlayerId;
        }

        if (long.TryParse(actorPlayerNumericIdText, out var actorPlayerId))
        {
            bridge.actorPlayerNumericId = actorPlayerId;
        }
    }

    private void sendTrackedAction(string actionType, Action sendAction)
    {
        lock (stateLock)
        {
            pendingActionType = actionType;
        }

        sendAction();
    }

    private void onConnectionStateChanged(string state)
    {
        lock (stateLock)
        {
            connectionState = state;
            flowChecklistRuntime.OnConnectionStateChanged(state);
            if (!string.Equals(state, "connected", StringComparison.Ordinal))
            {
                requestInFlight = false;
            }
        }
    }

    private void onRawRequest(string rawJson)
    {
        lock (stateLock)
        {
            latestRawRequestJson = rawJson;
            requestInFlight = true;
        }
    }

    private void onRawResponse(string rawJson)
    {
        lock (stateLock)
        {
            latestRawResponseJson = rawJson;
            currentResponseActionType = string.IsNullOrWhiteSpace(pendingActionType) ? "(unknown)" : pendingActionType;
            pendingActionType = string.Empty;
            requestInFlight = false;
        }
    }

    private void onSummaryUpdated(ServerResponseSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"isSucceeded: {summary.isSucceeded}");
        builder.AppendLine($"errorCode: {summary.errorCode}");
        builder.AppendLine($"errorMessage: {summary.errorMessage}");
        builder.AppendLine($"viewerPlayerNumericId: {summary.viewerPlayerNumericId?.ToString() ?? "(null)"}");
        builder.AppendLine($"currentPhase: {summary.currentPhase}");
        builder.AppendLine($"currentPlayerNumericId: {summary.currentPlayerNumericId?.ToString() ?? "(null)"}");
        builder.AppendLine($"myHandCount: {summary.myHandCount}");
        builder.AppendLine($"recentEventType: {summary.recentEventTypeKey}");
        builder.AppendLine($"hasInputContext: {summary.hasInputContext}");
        builder.AppendLine($"inputRequiredPlayerNumericId: {summary.inputRequiredPlayerNumericId?.ToString() ?? "(null)"}");
        builder.AppendLine($"inputChoiceCount: {summary.inputChoiceCount}");
        builder.AppendLine($"hasResponseWindow: {summary.hasResponseWindow}");
        builder.AppendLine($"responseCurrentResponderPlayerNumericId: {summary.responseCurrentResponderPlayerNumericId?.ToString() ?? "(null)"}");
        builder.AppendLine($"responseResponderCount: {summary.responseResponderCount}");

        lock (stateLock)
        {
            summaryText = builder.ToString();
        }
    }

    private void onProjectionUpdated(ProjectionViewModel projection)
    {
        lock (stateLock)
        {
            var previousProjection = latestProjection.deepClone();
            var responseActionType = currentResponseActionType;
            currentResponseActionType = string.Empty;

            latestProjection = projection.deepClone();

            var playSelectionCleared = true;
            var summonSelectionCleared = true;

            if (projection.isSucceeded && projection.hasStateProjection)
            {
                if (lastPlayedSelectedHandCardId.HasValue)
                {
                    playSelectionCleared = !PruneSelectionIfMissing(lastPlayedSelectedHandCardId, latestProjection.handCards).HasValue;
                }

                if (lastSummonedSelectedCardId.HasValue)
                {
                    summonSelectionCleared = !containsCardInList(lastSummonedSelectedCardId.Value, latestProjection.summonZoneCards) &&
                                             !containsCardInList(lastSummonedSelectedCardId.Value, latestProjection.sakuraCakeCards);
                }

                selectedHandCardId = PruneSelectionIfMissing(selectedHandCardId, latestProjection.handCards);
                selectedSummonCardId = PruneSelectionIfMissing(selectedSummonCardId, latestProjection.summonZoneCards);
                selectedSakuraCakeCardId = PruneSelectionIfMissing(selectedSakuraCakeCardId, latestProjection.sakuraCakeCards);
                selectedDefenseCardId = PruneSelectionIfMissing(selectedDefenseCardId, latestProjection.handCards);
            }

            lastPlayedSelectedHandCardId = null;
            lastSummonedSelectedCardId = null;

            if (!string.IsNullOrWhiteSpace(responseActionType) && responseActionType != "(unknown)")
            {
                flowChecklistRuntime.RecordProjectionResponse(
                    responseActionType,
                    latestProjection,
                    previousProjection,
                    playSelectionCleared,
                    summonSelectionCleared);
            }

            if (projection.isSucceeded)
            {
                appendFlowTraceLine(responseActionType, latestProjection);
            }
        }
    }

    private void appendFlowTraceLine(string responseActionType, ProjectionViewModel projection)
    {
        var action = string.IsNullOrWhiteSpace(responseActionType) ? "(unknown)" : responseActionType;
        var flowTraceLine =
            $"{action} | phase={projection.currentPhase} | player={projection.currentPlayerNumericId?.ToString() ?? "(null)"} | hand={projection.viewerHandCardCount} | field={projection.fieldCards.Count} | summon={projection.summonZoneCards.Count} | sakura={projection.sakuraCakeCards.Count} | events={projection.eventLog.Count}";

        flowTraceLines.Add(flowTraceLine);
        while (flowTraceLines.Count > MaxFlowTraceLines)
        {
            flowTraceLines.RemoveAt(0);
        }
    }

    private void onError(string error)
    {
        lock (stateLock)
        {
            latestError = error;
            requestInFlight = false;
        }
    }

    private bool canSendRequest()
    {
        lock (stateLock)
        {
            return bridge is not null &&
                   bridge.isConnected &&
                   !requestInFlight;
        }
    }

    private static bool containsCardInList(long cardId, List<ProjectionCardViewModel> cards)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            if (cards[index].cardInstanceNumericId == cardId)
            {
                return true;
            }
        }

        return false;
    }
}
}
