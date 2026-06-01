using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    [SerializeField] private string damageTargetPlayerNumericIdText = "2";
    [SerializeField] private string debugDamageValueText = "3";
    [SerializeField] private string debugDamageTypeKeyText = "physical";
    [SerializeField] private string debugTreasureDefinitionIdText = "T004";
    [SerializeField] private string inputChoiceKeyManualText = string.Empty;
    [SerializeField] private string traceExportCountText = "20";

    private const int MaxFlowTraceLines = 40;
    private const float DebugCardWidth = 150f;
    private const float DebugCardHeight = 210f;
    private const float DebugCardImageHeight = 130f;
    private const float DebugCardGap = 8f;

    private ServerBridge? bridge;
    private readonly object stateLock = new();
    private readonly DebugFlowChecklistRuntime flowChecklistRuntime = new();
    private readonly Dictionary<DebugChecklistMode, List<string>> flowTraceLinesByMode = new();

    private string connectionState = "disconnected";
    private string latestRawRequestJson = string.Empty;
    private string latestRawResponseJson = string.Empty;
    private string latestError = string.Empty;
    private string summaryText = "尚未收到响应。";
    private ProjectionViewModel latestProjection = ProjectionViewModel.createDefault(1);
    private bool requestInFlight;
    private long? pendingRequestId;
    private long? lastDirectRequestId;
    private long? lastPushRequestId;
    private int pushUpdateCount;
    private int lastPushEventLogDelta;

    private long? selectedHandCardId;
    private long? selectedSummonCardId;
    private long? selectedSakuraCakeCardId;
    private long? selectedDefenseCardId;
    private readonly List<string> selectedShackleDiscardChoiceKeys = new();
    private long? selectedShackleInputContextNumericId;

    private long? lastPlayedSelectedHandCardId;
    private long? lastSummonedSelectedCardId;

    private string pendingActionType = string.Empty;
    private string currentResponseActionType = string.Empty;
    private string currentResponseOrigin = "unknown";
    private string lastResponseOriginForUi = "(none)";
    private string lastFailedActionTypeForUi = string.Empty;
    private string lastFailedErrorCodeForUi = string.Empty;
    private string lastFailedErrorMessageForUi = string.Empty;
    private string lastFailedReasonKeyForUi = string.Empty;
    private long? lastFailedRequestIdForUi;
    private string lastFailedResponseSourceForUi = string.Empty;
    private string responseNoLocalBlockBanner = string.Empty;
    private string localInterceptionBanner = string.Empty;
    private string traceCopyStatus = string.Empty;
    private DebugChecklistMode checklistMode = DebugChecklistMode.mainFlowA;
    private bool autoRunMacroEnabled;
    private bool autoRunMacroTickInProgress;
    private DebugChecklistMode autoRunMacroMode = DebugChecklistMode.mainFlowA;
    private string autoRunMacroStatus = "未启动";

    private Vector2 projectionScroll;
    private Vector2 rawRequestJsonScroll;
    private Vector2 rawResponseJsonScroll;
    private Vector2 rootScroll;
    private Vector2 flowTraceScroll;
    private Vector2 responseWindowPopupScroll;

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

    public static void ApplyHandCardSelection(ref long? selectedHandCardId, ref long? selectedDefenseCardId, long cardInstanceNumericId)
    {
        selectedHandCardId = cardInstanceNumericId;
        selectedDefenseCardId = cardInstanceNumericId;
    }

    public static void ApplySummonCardSelection(ref long? selectedSummonCardId, ref long? selectedSakuraCakeCardId, long cardInstanceNumericId)
    {
        selectedSummonCardId = cardInstanceNumericId;
        selectedSakuraCakeCardId = null;
    }

    public static void ApplySakuraCardSelection(ref long? selectedSakuraCakeCardId, ref long? selectedSummonCardId, long cardInstanceNumericId)
    {
        selectedSakuraCakeCardId = cardInstanceNumericId;
        selectedSummonCardId = null;
    }

    public static bool TryApplyFieldCardSelection(
        ref long? selectedHandCardId,
        ref long? selectedSummonCardId,
        ref long? selectedSakuraCakeCardId,
        ref long? selectedDefenseCardId,
        long cardInstanceNumericId)
    {
        _ = selectedHandCardId;
        _ = selectedSummonCardId;
        _ = selectedSakuraCakeCardId;
        _ = selectedDefenseCardId;
        _ = cardInstanceNumericId;
        return false;
    }

    public static string BuildFieldCardReadOnlyMessage(long cardInstanceNumericId)
    {
        return $"field card is read-only（场上牌仅展示，不可操作）：{cardInstanceNumericId}";
    }

    public static string BuildReadOnlyZoneCardMessage(string zoneKey, long cardInstanceNumericId)
    {
        return $"{zoneKey} card is read-only（{localizeZoneKeyText(zoneKey)}仅展示，不可操作）：{cardInstanceNumericId}";
    }

    public static bool TryResolveSelectedOrManualCardId(long? selectedCardId, string manualText, out long resolvedCardId)
    {
        if (TryResolveSelectedCardId(selectedCardId, out resolvedCardId))
        {
            return true;
        }

        return long.TryParse(manualText, out resolvedCardId);
    }

    public static bool HasRenderableResponseWindow(ProjectionViewModel projection)
    {
        return projection.interaction.hasResponseWindow &&
               projection.interaction.responseWindowNumericId.HasValue &&
               projection.interaction.responseWindowNumericId.Value > 0;
    }

    public static bool IsAwaitDefenseStageForResponseWindow(ProjectionViewModel projection)
    {
        return HasRenderableResponseWindow(projection) &&
               string.Equals(
                   projection.interaction.pendingDamageResponseStageKey,
                   "awaitDefense",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLegacyAwaitCounterStageForResponseWindow(ProjectionViewModel projection)
    {
        return HasRenderableResponseWindow(projection) &&
               string.Equals(
                   projection.interaction.pendingDamageResponseStageKey,
                   "awaitCounter",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTurnStartShackleInputContext(ProjectionViewModel projection)
    {
        return projection.interaction.hasInputContext &&
               projection.interaction.inputContextNumericId.HasValue &&
               projection.interaction.inputContextNumericId.Value > 0 &&
               string.Equals(
                   projection.interaction.contextKey,
                   "turnStart:shackleDiscard",
                   StringComparison.Ordinal);
    }

    public static bool IsShackleDiscardChoiceKey(string choiceKey)
    {
        return !string.IsNullOrWhiteSpace(choiceKey) &&
               choiceKey.StartsWith("discardCard:", StringComparison.Ordinal);
    }

    public static List<string> CollectShackleDiscardChoiceKeys(List<string> inputChoiceKeys)
    {
        var result = new List<string>();
        foreach (var choiceKey in inputChoiceKeys)
        {
            if (IsShackleDiscardChoiceKey(choiceKey))
            {
                result.Add(choiceKey);
            }
        }

        return result;
    }

    public static void PruneSelectedChoiceKeysByAvailable(
        List<string> selectedChoiceKeys,
        List<string> availableChoiceKeys)
    {
        if (selectedChoiceKeys.Count == 0)
        {
            return;
        }

        var availableSet = new HashSet<string>(availableChoiceKeys, StringComparer.Ordinal);
        selectedChoiceKeys.RemoveAll(choiceKey => !availableSet.Contains(choiceKey));
    }

    public static bool TryToggleBoundedChoiceSelection(
        List<string> selectedChoiceKeys,
        string choiceKey,
        int maxSelectionCount,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            failureReason = "本地拦截：无效选项。";
            return false;
        }

        var existingIndex = selectedChoiceKeys.FindIndex(
            selectedChoiceKey => string.Equals(selectedChoiceKey, choiceKey, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            selectedChoiceKeys.RemoveAt(existingIndex);
            return true;
        }

        if (selectedChoiceKeys.Count >= maxSelectionCount)
        {
            failureReason = $"本地拦截：最多只能选择 {maxSelectionCount} 张弃牌。";
            return false;
        }

        selectedChoiceKeys.Add(choiceKey);
        return true;
    }

    public static bool IsDirectResponseForPendingRequest(
        bool requestInFlight,
        long? pendingRequestId,
        long? responseRequestId)
    {
        return requestInFlight &&
               pendingRequestId.HasValue &&
               responseRequestId.HasValue &&
               pendingRequestId.Value == responseRequestId.Value;
    }

    public static bool TryValidateSubmitResponseNoActor(
        ProjectionViewModel projection,
        string actorPlayerNumericIdText,
        out long currentResponderPlayerNumericId,
        out string failureReason)
    {
        currentResponderPlayerNumericId = 0;
        failureReason = string.Empty;

        if (!HasRenderableResponseWindow(projection))
        {
            failureReason = "本地拦截：当前没有可用的响应窗口。";
            return false;
        }

        if (!projection.interaction.responseCurrentResponderPlayerNumericId.HasValue ||
            projection.interaction.responseCurrentResponderPlayerNumericId.Value <= 0)
        {
            failureReason = "本地拦截：currentResponderPlayerNumericId 缺失。";
            return false;
        }

        currentResponderPlayerNumericId = projection.interaction.responseCurrentResponderPlayerNumericId.Value;

        if (!long.TryParse(actorPlayerNumericIdText, out var actorPlayerNumericId) || actorPlayerNumericId <= 0)
        {
            failureReason = "本地拦截：actorPlayerNumericId 非法。";
            return false;
        }

        if (actorPlayerNumericId != currentResponderPlayerNumericId)
        {
            failureReason =
                $"本地拦截：actorPlayerNumericId（{actorPlayerNumericId}）必须等于 currentResponderPlayerNumericId（{currentResponderPlayerNumericId}）。";
            return false;
        }

        return true;
    }

    public static bool TryValidateSubmitDefenseActor(
        ProjectionViewModel projection,
        string actorPlayerNumericIdText,
        out string failureReason)
    {
        return TryValidateSubmitResponseNoActor(
            projection,
            actorPlayerNumericIdText,
            out _,
            out failureReason);
    }

    public static bool TryValidateSelectedDefenseCardInHand(
        ProjectionViewModel projection,
        long? selectedDefenseCardId,
        out long selectedCardId,
        out ProjectionCardViewModel? selectedCard,
        out string failureReason)
    {
        selectedCardId = 0;
        selectedCard = null;
        failureReason = string.Empty;
        if (!selectedDefenseCardId.HasValue)
        {
            failureReason = "本地拦截：尚未选择防御牌，无法提交正式防御。";
            return false;
        }

        selectedCardId = selectedDefenseCardId.Value;
        foreach (var handCard in projection.handCards)
        {
            if (handCard.cardInstanceNumericId == selectedCardId)
            {
                selectedCard = handCard;
                return true;
            }
        }

        failureReason =
            $"本地拦截：selected defense card is not in hand（已选防御牌不在手牌中）：{selectedCardId}";
        return false;
    }

    public static bool TryInferDefenseTypeKeyFromDefinitionId(string definitionId, out string defenseTypeKey)
    {
        defenseTypeKey = string.Empty;
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return false;
        }

        var normalized = definitionId.Trim();
        var normalizedUpper = normalized.ToUpperInvariant();
        if (string.Equals(normalized, "starter:magicCircuit", StringComparison.OrdinalIgnoreCase))
        {
            defenseTypeKey = "dual";
            return true;
        }

        if (string.Equals(normalized, "starter:kourindouCoupon", StringComparison.OrdinalIgnoreCase))
        {
            defenseTypeKey = "dual";
            return true;
        }

        if (string.Equals(normalized, "test:defensePhysical2", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "test:defensePhysical1", StringComparison.OrdinalIgnoreCase))
        {
            defenseTypeKey = "physical";
            return true;
        }

        if (string.Equals(normalized, "test:defenseSpell2", StringComparison.OrdinalIgnoreCase))
        {
            defenseTypeKey = "spell";
            return true;
        }

        if (string.Equals(normalized, "test:defenseDual2", StringComparison.OrdinalIgnoreCase))
        {
            defenseTypeKey = "dual";
            return true;
        }

        switch (normalizedUpper)
        {
            case "T003":
            case "T004":
            case "T009":
            case "T018":
            case "T021":
                defenseTypeKey = "physical";
                return true;
            case "T002":
            case "T005":
            case "T011":
            case "T013":
            case "T019":
                defenseTypeKey = "spell";
                return true;
            case "S001":
            case "T001":
            case "T006":
            case "T007":
            case "T008":
            case "T010":
            case "T012":
            case "T014":
            case "T015":
            case "T016":
            case "T017":
            case "T020":
            case "T022":
            case "T023":
            case "T024":
            case "T025":
            case "T026":
            case "T027":
            case "T028":
            case "T029":
                defenseTypeKey = "dual";
                return true;
        }

        return false;
    }

    public static bool TryResolveFormalDefenseTypeKey(
        ProjectionCardViewModel? selectedDefenseCard,
        string manualDefenseTypeKey,
        out string resolvedDefenseTypeKey,
        out string sourceLabel)
    {
        resolvedDefenseTypeKey = string.Empty;
        sourceLabel = "(未解析)";
        if (selectedDefenseCard is not null &&
            TryInferDefenseTypeKeyFromDefinitionId(selectedDefenseCard.definitionId, out var inferredDefenseTypeKey))
        {
            resolvedDefenseTypeKey = inferredDefenseTypeKey;
            sourceLabel = "已选防御牌定义";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(manualDefenseTypeKey))
        {
            resolvedDefenseTypeKey = manualDefenseTypeKey.Trim();
            sourceLabel = "手动输入";
            return true;
        }

        return false;
    }

    public static string TryExtractFailedReasonKeyFromRawResponse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return string.Empty;
        }

        try
        {
            var probe = JsonUtility.FromJson<FailedReasonProbeEnvelope>(rawJson);
            if (probe?.error is null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(probe.error.failedReasonKey))
            {
                return probe.error.failedReasonKey;
            }

            if (!string.IsNullOrWhiteSpace(probe.error.failedReason))
            {
                return probe.error.failedReason;
            }

            if (!string.IsNullOrWhiteSpace(probe.error.reasonKey))
            {
                return probe.error.reasonKey;
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    public static bool IsLocalResponderForResponseWindow(ProjectionViewModel projection)
    {
        return HasRenderableResponseWindow(projection) &&
               projection.interaction.responseCurrentResponderPlayerNumericId.HasValue &&
               projection.interaction.responseCurrentResponderPlayerNumericId.Value > 0 &&
               projection.viewerPlayerNumericId == projection.interaction.responseCurrentResponderPlayerNumericId.Value;
    }

    public static bool TryResolveTargetActiveCharacterInstanceId(
        ProjectionViewModel projection,
        string targetPlayerNumericIdText,
        out long targetCharacterInstanceNumericId,
        out string failureReason)
    {
        targetCharacterInstanceNumericId = 0;
        failureReason = string.Empty;

        if (!long.TryParse(targetPlayerNumericIdText, out var targetPlayerNumericId) || targetPlayerNumericId <= 0)
        {
            failureReason = "本地拦截：目标玩家ID无效。";
            return false;
        }

        ProjectionPlayerSummaryViewModel? targetPlayerSummary = null;
        foreach (var summary in projection.playerSummaries)
        {
            if (summary.playerNumericId == targetPlayerNumericId)
            {
                targetPlayerSummary = summary;
                break;
            }
        }

        if (targetPlayerSummary is null)
        {
            failureReason = $"本地拦截：未在投影中找到目标玩家（playerId={targetPlayerNumericId}）。";
            return false;
        }

        if (!targetPlayerSummary.activeCharacterInstanceNumericId.HasValue ||
            targetPlayerSummary.activeCharacterInstanceNumericId.Value <= 0)
        {
            failureReason = $"本地拦截：目标玩家（playerId={targetPlayerNumericId}）没有可用在场角色。";
            return false;
        }

        targetCharacterInstanceNumericId = targetPlayerSummary.activeCharacterInstanceNumericId.Value;
        return true;
    }

    public static bool TryResolveDebugDamageArguments(
        string damageValueText,
        string damageTypeKeyText,
        out int resolvedDamageValue,
        out string resolvedDamageTypeKey,
        out string failureReason)
    {
        resolvedDamageValue = 0;
        resolvedDamageTypeKey = string.Empty;
        failureReason = string.Empty;

        if (!int.TryParse(damageValueText, out var parsedDamageValue) || parsedDamageValue <= 0)
        {
            failureReason = "本地拦截：伤害值必须是大于0的整数。";
            return false;
        }

        var normalizedDamageTypeSource = (damageTypeKeyText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedDamageTypeSource))
        {
            failureReason = "本地拦截：伤害类型不能为空（physical/spell/direct 或 体术/咒术/直接）。";
            return false;
        }

        var normalizedLower = normalizedDamageTypeSource.ToLowerInvariant();
        resolvedDamageTypeKey = normalizedLower switch
        {
            "physical" => "physical",
            "spell" => "spell",
            "direct" => "direct",
            "体术" => "physical",
            "咒术" => "spell",
            "直接" => "direct",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(resolvedDamageTypeKey))
        {
            failureReason = "本地拦截：伤害类型仅支持 physical/spell/direct（或 体术/咒术/直接）。";
            return false;
        }

        resolvedDamageValue = parsedDamageValue;
        return true;
    }

    private void OnEnable()
    {
        ensureFlowTraceBuckets();
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

    private void ensureFlowTraceBuckets()
    {
        if (!flowTraceLinesByMode.ContainsKey(DebugChecklistMode.mainFlowA))
        {
            flowTraceLinesByMode[DebugChecklistMode.mainFlowA] = new List<string>();
        }

        if (!flowTraceLinesByMode.ContainsKey(DebugChecklistMode.responseWindowB))
        {
            flowTraceLinesByMode[DebugChecklistMode.responseWindowB] = new List<string>();
        }

        if (!flowTraceLinesByMode.ContainsKey(DebugChecklistMode.inputContextC))
        {
            flowTraceLinesByMode[DebugChecklistMode.inputContextC] = new List<string>();
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20f, 20f, panelWidth, panelHeight), "Socket 调试面板", GUI.skin.window);
        rootScroll = GUILayout.BeginScrollView(
            rootScroll,
            GUILayout.Width(panelWidth - 16f),
            GUILayout.Height(panelHeight - 30f));

        drawConnectionSection();
        GUILayout.Space(8f);
        drawInteractionStatusBar();
        GUILayout.Space(8f);
        drawLocalInterceptionBanner();
        GUILayout.Space(8f);
        drawLatestServerFailureBanner();
        GUILayout.Space(8f);
        drawPhaseActionSection();
        GUILayout.Space(8f);
        drawDamageInteractionTestSection();
        GUILayout.Space(8f);
        drawCardActionSection();
        GUILayout.Space(8f);
        drawDefenseActionSection();
        GUILayout.Space(8f);
        drawInputContextActionSection();
        GUILayout.Space(8f);
        drawProjectionSection();
        GUILayout.Space(8f);
        drawFlowChecklistSection();
        GUILayout.Space(8f);
        drawSummarySection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        drawResponseWindowPopup();
    }

    private void drawConnectionSection()
    {
        var connectionBoundText = "(未连接)";
        if (bridge is not null && bridge.isConnected)
        {
            connectionBoundText = $"viewerPlayerNumericId={viewerPlayerNumericIdText}";
        }

        GUILayout.Label("WebSocket 地址");
        wsUrl = GUILayout.TextField(wsUrl, GUILayout.Height(26f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("观察者", GUILayout.Width(80f));
        viewerPlayerNumericIdText = GUILayout.TextField(viewerPlayerNumericIdText, GUILayout.Width(120f));
        GUILayout.Label("操作者", GUILayout.Width(80f));
        actorPlayerNumericIdText = GUILayout.TextField(actorPlayerNumericIdText, GUILayout.Width(120f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("连接", GUILayout.Width(120f)))
        {
            connect();
        }

        if (GUILayout.Button("断开", GUILayout.Width(120f)))
        {
            bridge?.Disconnect();
        }

        GUILayout.Label($"连接状态: {localizeConnectionStateText(connectionState)}");
        GUILayout.Label($"请求进行中: {(requestInFlight ? "是" : "否")}");
        GUILayout.Label($"连接绑定: {connectionBoundText}");
        GUILayout.EndHorizontal();
    }

    private void drawInteractionStatusBar()
    {
        ProjectionViewModel projectionSnapshot;
        string actorTextSnapshot;
        string viewerTextSnapshot;
        long? pendingRequestIdSnapshot;
        long? lastDirectRequestIdSnapshot;
        long? lastPushRequestIdSnapshot;
        int pushUpdateCountSnapshot;
        int lastPushEventLogDeltaSnapshot;
        string lastResponseOriginSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorTextSnapshot = actorPlayerNumericIdText;
            viewerTextSnapshot = viewerPlayerNumericIdText;
            pendingRequestIdSnapshot = pendingRequestId;
            lastDirectRequestIdSnapshot = lastDirectRequestId;
            lastPushRequestIdSnapshot = lastPushRequestId;
            pushUpdateCountSnapshot = pushUpdateCount;
            lastPushEventLogDeltaSnapshot = lastPushEventLogDelta;
            lastResponseOriginSnapshot = lastResponseOriginForUi;
        }

        var actorValid = long.TryParse(actorTextSnapshot, out var actorPlayerNumericId);
        var viewerValid = long.TryParse(viewerTextSnapshot, out var viewerPlayerNumericId);

        var hasValidResponseWindow = projectionSnapshot.interaction.hasResponseWindow &&
                                     projectionSnapshot.interaction.responseWindowNumericId.HasValue &&
                                     projectionSnapshot.interaction.responseWindowNumericId.Value > 0 &&
                                     projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId.HasValue &&
                                     projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId.Value > 0;
        var responseSubmitReady = hasValidResponseWindow &&
                                  actorValid &&
                                  actorPlayerNumericId == projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId!.Value;

        var hasValidInputContext = projectionSnapshot.interaction.hasInputContext &&
                                   projectionSnapshot.interaction.inputContextNumericId.HasValue &&
                                   projectionSnapshot.interaction.inputContextNumericId.Value > 0;
        var isParallelInputContext = projectionSnapshot.interaction.inputRequiredPlayerNumericIds.Count > 0;
        var actorMatchesInputRequired = false;
        var actorAlreadySubmittedInput = false;
        if (actorValid && hasValidInputContext)
        {
            if (isParallelInputContext)
            {
                actorMatchesInputRequired = projectionSnapshot.interaction.inputRequiredPlayerNumericIds.Contains(actorPlayerNumericId);
                actorAlreadySubmittedInput = projectionSnapshot.interaction.inputSubmittedPlayerNumericIds.Contains(actorPlayerNumericId);
            }
            else if (projectionSnapshot.interaction.inputRequiredPlayerNumericId.HasValue &&
                     projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value > 0)
            {
                actorMatchesInputRequired =
                    actorPlayerNumericId == projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value;
            }
        }

        var inputSubmitReady = hasValidInputContext &&
                               actorValid &&
                               actorMatchesInputRequired &&
                               !actorAlreadySubmittedInput;

        var previousColor = GUI.color;
        GUI.color = new Color(0.14f, 0.14f, 0.14f, 0.95f);
        GUILayout.BeginVertical("box");
        GUI.color = previousColor;

        GUILayout.Label(
            $"交互状态条 | 操作者={actorTextSnapshot} 观察者={viewerTextSnapshot} 当前玩家={projectionSnapshot.currentPlayerNumericId?.ToString() ?? "(空)"} 当前阶段={localizePhaseText(projectionSnapshot.currentPhase)}");
        GUILayout.Label(
            $"ResponseWindow: has={projectionSnapshot.interaction.hasResponseWindow} id={projectionSnapshot.interaction.responseWindowNumericId?.ToString() ?? "(空)"} responder={projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"} | actor可提交response={(responseSubmitReady ? "匹配" : "不匹配")}");
        var inputRequiredText = isParallelInputContext
            ? $"[{string.Join(",", projectionSnapshot.interaction.inputRequiredPlayerNumericIds)}]"
            : projectionSnapshot.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(空)";
        var inputSubmittedText = projectionSnapshot.interaction.inputSubmittedPlayerNumericIds.Count > 0
            ? $"[{string.Join(",", projectionSnapshot.interaction.inputSubmittedPlayerNumericIds)}]"
            : "(空)";
        GUILayout.Label(
            $"InputContext: has={projectionSnapshot.interaction.hasInputContext} id={projectionSnapshot.interaction.inputContextNumericId?.ToString() ?? "(空)"} required={inputRequiredText} submitted={inputSubmittedText} | actor可提交input={(inputSubmitReady ? "匹配" : "不匹配")}");
        GUILayout.Label(
            $"消息来源: 最近={lastResponseOriginSnapshot} pendingRequestId={pendingRequestIdSnapshot?.ToString() ?? "(空)"} lastDirectRequestId={lastDirectRequestIdSnapshot?.ToString() ?? "(空)"} pushCount={pushUpdateCountSnapshot} lastPushRequestId={lastPushRequestIdSnapshot?.ToString() ?? "(空)"} lastPushEventDelta={lastPushEventLogDeltaSnapshot}");

        if (!actorValid || !viewerValid)
        {
            GUILayout.Label("提示：actor/viewer 需要是有效数字 ID。");
        }

        GUILayout.EndVertical();
    }

    private void drawLocalInterceptionBanner()
    {
        string bannerSnapshot;
        lock (stateLock)
        {
            bannerSnapshot = string.IsNullOrWhiteSpace(localInterceptionBanner)
                ? string.Empty
                : localInterceptionBanner;
        }

        if (string.IsNullOrWhiteSpace(bannerSnapshot))
        {
            return;
        }

        var previousBackgroundColor = GUI.backgroundColor;
        var previousContentColor = GUI.contentColor;
        GUI.backgroundColor = new Color(0.80f, 0.14f, 0.14f, 1f);
        GUI.contentColor = Color.white;
        GUILayout.Box($"本地拦截：{bannerSnapshot}", GUILayout.MinHeight(40f), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = previousBackgroundColor;
        GUI.contentColor = previousContentColor;
    }

    private void drawLatestServerFailureBanner()
    {
        string actionTypeSnapshot;
        string errorCodeSnapshot;
        string errorMessageSnapshot;
        string failedReasonKeySnapshot;
        long? requestIdSnapshot;
        string sourceSnapshot;
        lock (stateLock)
        {
            actionTypeSnapshot = lastFailedActionTypeForUi;
            errorCodeSnapshot = lastFailedErrorCodeForUi;
            errorMessageSnapshot = lastFailedErrorMessageForUi;
            failedReasonKeySnapshot = lastFailedReasonKeyForUi;
            requestIdSnapshot = lastFailedRequestIdForUi;
            sourceSnapshot = lastFailedResponseSourceForUi;
        }

        if (string.IsNullOrWhiteSpace(errorCodeSnapshot))
        {
            return;
        }

        var previousBackgroundColor = GUI.backgroundColor;
        var previousContentColor = GUI.contentColor;
        GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f, 1f);
        GUI.contentColor = Color.white;

        var localizedActionType = localizeActionTypeText(actionTypeSnapshot);
        var failedReasonKeyText = string.IsNullOrWhiteSpace(failedReasonKeySnapshot)
            ? "(未提供)"
            : failedReasonKeySnapshot;
        var requestIdText = requestIdSnapshot?.ToString() ?? "(空)";
        var sourceText = string.IsNullOrWhiteSpace(sourceSnapshot) ? "(未知)" : sourceSnapshot;

        GUILayout.BeginVertical("box");
        GUILayout.Label("最近服务端失败详情");
        GUILayout.Label($"动作: {actionTypeSnapshot}（{localizedActionType}） | requestId: {requestIdText} | source: {sourceText}");
        GUILayout.Label($"error.code: {errorCodeSnapshot}");
        GUILayout.Label($"error.message: {errorMessageSnapshot}");
        GUILayout.Label($"failedReasonKey: {failedReasonKeyText}");
        GUILayout.EndVertical();

        GUI.backgroundColor = previousBackgroundColor;
        GUI.contentColor = previousContentColor;
    }

    private void drawPhaseActionSection()
    {
        ProjectionViewModel projectionSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
        }

        var canSend = canSendRequest();
        var previousEnabled = GUI.enabled;

        GUILayout.BeginVertical("box");
        GUILayout.Label($"主阶段操作（当前阶段：{localizePhaseText(projectionSnapshot.currentPhase)}）");

        GUI.enabled = canSend;
        GUILayout.BeginHorizontal();
        if (string.Equals(projectionSnapshot.currentPhase, "action", StringComparison.OrdinalIgnoreCase) &&
            GUILayout.Button("进入召唤", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterSummonPhase", () => bridge?.SendEnterSummonPhase());
        }

        if (string.Equals(projectionSnapshot.currentPhase, "summon", StringComparison.OrdinalIgnoreCase) &&
            GUILayout.Button("回合结束", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterEndPhase", () => bridge?.SendEnterEndPhase());
        }

        if (!string.Equals(projectionSnapshot.currentPhase, "action", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(projectionSnapshot.currentPhase, "summon", StringComparison.OrdinalIgnoreCase))
        {
            GUILayout.Label("当前阶段不提供玩家手动切换，等待系统自动推进或交互处理。");
        }
        GUILayout.EndHorizontal();

        GUI.enabled = canSend;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("调试：进入行动", GUILayout.Width(140f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterActionPhase", () => bridge?.SendEnterActionPhase());
        }

        if (GUILayout.Button("调试：开始下一回合", GUILayout.Width(170f)))
        {
            applyViewerAndActor();
            sendTrackedAction("startNextTurn", () => bridge?.SendStartNextTurn());
        }

        if (GUILayout.Button("调试：打开伤害响应窗", GUILayout.Width(210f)))
        {
            applyViewerAndActor();
            sendTrackedAction("debugOpenDamageResponseWindow", () => bridge?.SendDebugOpenDamageResponseWindow());
        }

        if (GUILayout.Button("调试：重开本局", GUILayout.Width(160f)))
        {
            applyViewerAndActor();
            sendTrackedAction("debugResetMatch", () => bridge?.SendDebugResetMatch());
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("调试宝具定义ID", GUILayout.Width(105f));
        debugTreasureDefinitionIdText = GUILayout.TextField(debugTreasureDefinitionIdText, GUILayout.Width(120f));
        GUI.enabled = canSend;
        if (GUILayout.Button("调试：移入手牌", GUILayout.Width(140f)))
        {
            var trimmedDefinitionId = debugTreasureDefinitionIdText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedDefinitionId))
            {
                onLocalBlocked("本地拦截：调试宝具定义ID不能为空（例如 T004）。");
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction(
                    "debugMoveTreasureToHandByDefinition",
                    () => bridge?.SendDebugMoveTreasureToHandByDefinition(trimmedDefinitionId));
            }
        }

        GUI.enabled = canSend;
        if (GUILayout.Button("调试：置入宝具牌堆顶部", GUILayout.Width(210f)))
        {
            var trimmedDefinitionId = debugTreasureDefinitionIdText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedDefinitionId))
            {
                onLocalBlocked("本地拦截：调试宝具定义ID不能为空（例如 T004）。");
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction(
                    "debugPutTreasureOnTopByDefinition",
                    () => bridge?.SendDebugPutTreasureOnTopByDefinition(trimmedDefinitionId));
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUI.enabled = previousEnabled;
    }

    private void drawDamageInteractionTestSection()
    {
        ProjectionViewModel projectionSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
        }

        var canSend = canSendRequest();
        var previousEnabled = GUI.enabled;

        GUILayout.BeginVertical("box");
        GUILayout.Label("伤害交互测试（debug-only）");

        GUILayout.BeginHorizontal();
        GUILayout.Label("目标玩家ID", GUILayout.Width(90f));
        damageTargetPlayerNumericIdText = GUILayout.TextField(damageTargetPlayerNumericIdText, GUILayout.Width(90f));
        GUILayout.Label("伤害值", GUILayout.Width(60f));
        debugDamageValueText = GUILayout.TextField(debugDamageValueText, GUILayout.Width(70f));
        GUILayout.Label("伤害类型", GUILayout.Width(70f));
        debugDamageTypeKeyText = GUILayout.TextField(debugDamageTypeKeyText, GUILayout.Width(90f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = canSend;
        if (GUILayout.Button("体术", GUILayout.Width(80f)))
        {
            debugDamageTypeKeyText = "physical";
        }

        if (GUILayout.Button("咒术", GUILayout.Width(80f)))
        {
            debugDamageTypeKeyText = "spell";
        }

        if (GUILayout.Button("直接", GUILayout.Width(80f)))
        {
            debugDamageTypeKeyText = "direct";
        }

        GUI.enabled = canSend;
        if (GUILayout.Button("调试：对目标玩家造成伤害", GUILayout.Width(230f)))
        {
            if (!TryResolveTargetActiveCharacterInstanceId(
                    projectionSnapshot,
                    damageTargetPlayerNumericIdText,
                    out var targetCharacterInstanceNumericId,
                    out var failureReason))
            {
                onLocalBlocked(failureReason);
            }
            else if (!TryResolveDebugDamageArguments(
                         debugDamageValueText,
                         debugDamageTypeKeyText,
                         out var resolvedDamageValue,
                         out var resolvedDamageTypeKey,
                         out failureReason))
            {
                onLocalBlocked(failureReason);
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction(
                    "debugOpenDamageResponseWindow",
                    () => bridge?.SendDebugOpenDamageResponseWindow(
                        targetCharacterInstanceNumericId,
                        resolvedDamageValue,
                        resolvedDamageTypeKey));
            }
        }

        GUI.enabled = previousEnabled;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
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
        var isActionPhase = string.Equals(projectionSnapshot.currentPhase, "action", StringComparison.OrdinalIgnoreCase);
        var isSummonPhase = string.Equals(projectionSnapshot.currentPhase, "summon", StringComparison.OrdinalIgnoreCase);
        var canDraw = canSend && isActionPhase;
        var canPlaySelected = canSend && isActionPhase;
        var canPlayFirstHand = canSend && isActionPhase && projectionSnapshot.handCards.Count > 0;
        var canSummonSelected = canSend && isSummonPhase;
        var canSummonSakuraSelected = canSend && isSummonPhase;
        var canSummonFirst = canSend && isSummonPhase && projectionSnapshot.summonZoneCards.Count > 0;
        var previousEnabled = GUI.enabled;

        GUILayout.BeginHorizontal();
        GUI.enabled = canDraw;
        if (GUILayout.Button("抽1张", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("drawOneCard", () => bridge?.SendDrawOneCard());
        }

        GUILayout.Label("打出卡牌ID", GUILayout.Width(90f));
        playCardInstanceNumericIdText = GUILayout.TextField(playCardInstanceNumericIdText, GUILayout.Width(130f));

        if (GUILayout.Button("打出", GUILayout.Width(100f)))
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
                onLocalBlocked("本地拦截：打出失败，需要已选手牌或输入合法卡牌ID。");
            }
        }

        GUI.enabled = canPlaySelected;
        if (GUILayout.Button("打出已选手牌", GUILayout.Width(120f)))
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
                onLocalBlocked("本地拦截：打出已选手牌失败，尚未选择手牌。");
            }
        }

        GUI.enabled = canPlayFirstHand;
        if (GUILayout.Button("打出首张手牌", GUILayout.Width(140f)))
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
        GUI.enabled = canSummonSelected;
        GUILayout.Label("召唤卡牌ID", GUILayout.Width(105f));
        summonCardInstanceNumericIdText = GUILayout.TextField(summonCardInstanceNumericIdText, GUILayout.Width(130f));

        if (GUILayout.Button("召唤", GUILayout.Width(100f)))
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
                onLocalBlocked("本地拦截：召唤失败，需要已选召唤卡或输入合法卡牌ID。");
            }
        }

        GUI.enabled = canSummonSelected;
        if (GUILayout.Button("召唤已选卡", GUILayout.Width(120f)))
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
                onLocalBlocked("本地拦截：召唤已选卡失败，尚未选择召唤区卡牌。");
            }
        }

        GUI.enabled = canSummonSakuraSelected;
        if (GUILayout.Button("召唤已选樱花饼", GUILayout.Width(190f)))
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
                onLocalBlocked("本地拦截：召唤已选樱花饼失败，尚未选择樱花饼卡牌。");
            }
        }

        GUI.enabled = canSummonFirst;
        if (GUILayout.Button("召唤区首张召唤", GUILayout.Width(140f)))
        {
            lock (stateLock)
            {
                lastSummonedSelectedCardId = null;
            }

            applyViewerAndActor();
            sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(projectionSnapshot.summonZoneCards[0].cardInstanceNumericId));
        }
        GUILayout.EndHorizontal();

        if (!isActionPhase && !isSummonPhase)
        {
            GUILayout.Label("当前阶段为内部自动阶段或交互阶段，请等待系统自动推进/交互处理后再执行主操作。");
        }

        GUI.enabled = previousEnabled;
    }

    private void drawDefenseActionSection()
    {
        ProjectionViewModel projectionSnapshot;
        string actorPlayerNumericIdTextSnapshot;
        long? selectedHandCardIdSnapshot;
        long? selectedDefenseCardIdSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorPlayerNumericIdTextSnapshot = actorPlayerNumericIdText;
            selectedHandCardIdSnapshot = selectedHandCardId;
            selectedDefenseCardIdSnapshot = selectedDefenseCardId;
        }

        if (HasRenderableResponseWindow(projectionSnapshot))
        {
            if (IsLocalResponderForResponseWindow(projectionSnapshot))
            {
                GUILayout.Label("防御操作已切换到响应窗口，请在弹窗中选择“响应：不防御”或“使用选中手牌防御”。");
            }
            else
            {
                var responderText = projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)";
                GUILayout.Label($"当前有响应窗口，等待 Player {responderText} 响应。当前客户端为观察模式。");
            }

            return;
        }

        var canSend = canSendRequest();
        var canSetDefenseFromHand = canSend && selectedHandCardIdSnapshot.HasValue;
        var canDefenseSelected = canSend;
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("防御：固定减1", GUILayout.Width(140f)))
        {
            if (!TryValidateSubmitDefenseActor(projectionSnapshot, actorPlayerNumericIdTextSnapshot, out var failureReason))
            {
                onLocalBlocked(failureReason);
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFixedReduce1());
            }
        }

        GUI.enabled = canSetDefenseFromHand;
        if (GUILayout.Button("从手牌设为防御牌", GUILayout.Width(170f)))
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
                onLocalBlocked("本地拦截：设置防御牌失败，尚未选择手牌。");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = canSend;
        GUILayout.Label("防御类型", GUILayout.Width(80f));
        defenseTypeKeyText = GUILayout.TextField(defenseTypeKeyText, GUILayout.Width(120f));
        GUILayout.Label("卡牌ID", GUILayout.Width(55f));
        defenseCardInstanceNumericIdText = GUILayout.TextField(defenseCardInstanceNumericIdText, GUILayout.Width(120f));

        if (GUILayout.Button("防御：正式", GUILayout.Width(130f)))
        {
            ProjectionCardViewModel? selectedDefenseCard = null;
            if (TryValidateSelectedDefenseCardInHand(
                    projectionSnapshot,
                    selectedDefenseCardIdSnapshot,
                    out _,
                    out var selectedCardForTypeInference,
                    out _))
            {
                selectedDefenseCard = selectedCardForTypeInference;
            }

            if (!TryResolveFormalDefenseTypeKey(
                    selectedDefenseCard,
                    defenseTypeKeyText,
                    out var resolvedDefenseTypeKey,
                    out _))
            {
                onLocalBlocked("本地拦截：防御类型不能为空，且无法从已选防御牌推断。");
            }
            else if (!TryValidateSubmitDefenseActor(projectionSnapshot, actorPlayerNumericIdTextSnapshot, out var failureReason))
            {
                onLocalBlocked(failureReason);
            }
            else if (TryResolveSelectedCardId(selectedDefenseCardIdSnapshot, out _))
            {
                if (!TryValidateSelectedDefenseCardInHand(
                        projectionSnapshot,
                        selectedDefenseCardIdSnapshot,
                        out var defenseCardInstanceIdFromSelection,
                        out _,
                        out var selectedDefenseFailureReason))
                {
                    onLocalBlocked(selectedDefenseFailureReason);
                }
                else
                {
                    applyViewerAndActor();
                    sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(resolvedDefenseTypeKey, defenseCardInstanceIdFromSelection));
                }
            }
            else if (long.TryParse(defenseCardInstanceNumericIdText, out var defenseCardInstanceIdFromManual))
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(resolvedDefenseTypeKey, defenseCardInstanceIdFromManual));
            }
            else
            {
                onLocalBlocked("本地拦截：正式防御失败，需要已选防御牌或输入合法防御卡牌ID。");
            }
        }

        GUI.enabled = canDefenseSelected;
        if (GUILayout.Button("防御已选卡", GUILayout.Width(130f)))
        {
            if (!TryValidateSelectedDefenseCardInHand(
                    projectionSnapshot,
                    selectedDefenseCardIdSnapshot,
                    out var selectedCardId,
                    out var selectedDefenseCard,
                    out var selectedDefenseFailureReason))
            {
                onLocalBlocked(selectedDefenseFailureReason);
            }
            else if (!TryValidateSubmitDefenseActor(projectionSnapshot, actorPlayerNumericIdTextSnapshot, out var failureReason))
            {
                onLocalBlocked(failureReason);
            }
            else if (!TryResolveFormalDefenseTypeKey(
                         selectedDefenseCard,
                         defenseTypeKeyText,
                         out var resolvedDefenseTypeKey,
                         out _))
            {
                onLocalBlocked("本地拦截：防御类型不能为空，且无法从已选防御牌推断。");
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(resolvedDefenseTypeKey, selectedCardId));
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

        GUILayout.Label("状态投影");
        projectionScroll = GUILayout.BeginScrollView(projectionScroll, GUILayout.Height(560f));
        GUILayout.Label($"回合数: {projectionSnapshot.turnNumber}");
        GUILayout.Label($"当前阶段: {localizePhaseText(projectionSnapshot.currentPhase)}");
        GUILayout.Label($"当前玩家ID: {projectionSnapshot.currentPlayerNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label(
            $"自身资源: 灵力={projectionSnapshot.mana}, 技能点={projectionSnapshot.skillPoint}, 灵符预览={projectionSnapshot.sigilPreview}, 锁定灵符={projectionSnapshot.lockedSigil?.ToString() ?? "(空)"}");
        GUILayout.Label($"弃牌区摘要: {buildDiscardAreaSummary(projectionSnapshot)}");
        GUILayout.Label($"自身手牌数: {projectionSnapshot.viewerHandCardCount}");
        drawCurrentPlayerSnapshotSection(projectionSnapshot);
        drawTeamResourceSummarySection(projectionSnapshot);
        drawAllPlayerSummarySection(projectionSnapshot);
        drawAllPlayerSpecialStatusSection(projectionSnapshot);

        GUILayout.Label($"已选手牌: {selectedHandCardIdSnapshot?.ToString() ?? "(无)"}");
        GUILayout.Label($"已选防御牌: {selectedDefenseCardIdSnapshot?.ToString() ?? "(无)"}");
        drawCardGrid("自己的手牌", projectionSnapshot.handCards, card =>
        {
            var label = string.Empty;
            if (selectedHandCardIdSnapshot.HasValue &&
                selectedHandCardIdSnapshot.Value == card.cardInstanceNumericId)
            {
                label += "[手牌已选]";
            }

            if (selectedDefenseCardIdSnapshot.HasValue &&
                selectedDefenseCardIdSnapshot.Value == card.cardInstanceNumericId)
            {
                label += "[防御已选]";
            }

            return label;
        }, card =>
        {
            lock (stateLock)
            {
                var nextSelectedHandCardId = selectedHandCardId;
                var nextSelectedDefenseCardId = selectedDefenseCardId;
                ApplyHandCardSelection(ref nextSelectedHandCardId, ref nextSelectedDefenseCardId, card.cardInstanceNumericId);
                selectedHandCardId = nextSelectedHandCardId;
                selectedDefenseCardId = nextSelectedDefenseCardId;
            }
        }, isReadOnly: false);

        drawCardGrid("自己的弃牌区明细（只读）", projectionSnapshot.discardCards, _ => string.Empty, card =>
        {
            onLocalBlocked(BuildReadOnlyZoneCardMessage("discard", card.cardInstanceNumericId));
        }, isReadOnly: true);

        drawCardGrid("自己的场上牌（只读）", projectionSnapshot.fieldCards, _ => string.Empty, card =>
        {
            lock (stateLock)
            {
                var nextSelectedHandCardId = selectedHandCardId;
                var nextSelectedSummonCardId = selectedSummonCardId;
                var nextSelectedSakuraCakeCardId = selectedSakuraCakeCardId;
                var nextSelectedDefenseCardId = selectedDefenseCardId;
                var applied = TryApplyFieldCardSelection(
                    ref nextSelectedHandCardId,
                    ref nextSelectedSummonCardId,
                    ref nextSelectedSakuraCakeCardId,
                    ref nextSelectedDefenseCardId,
                    card.cardInstanceNumericId);
                if (!applied)
                {
                    onLocalBlocked(BuildFieldCardReadOnlyMessage(card.cardInstanceNumericId));
                }
            }
        }, isReadOnly: true);

        GUILayout.Label($"已选召唤区卡牌: {selectedSummonCardIdSnapshot?.ToString() ?? "(无)"}");
        drawCardGrid("召唤区卡牌", projectionSnapshot.summonZoneCards, card =>
        {
            return selectedSummonCardIdSnapshot.HasValue &&
                   selectedSummonCardIdSnapshot.Value == card.cardInstanceNumericId
                ? "[召唤已选]"
                : string.Empty;
        }, card =>
        {
            lock (stateLock)
            {
                var nextSelectedSummonCardId = selectedSummonCardId;
                var nextSelectedSakuraCakeCardId = selectedSakuraCakeCardId;
                ApplySummonCardSelection(ref nextSelectedSummonCardId, ref nextSelectedSakuraCakeCardId, card.cardInstanceNumericId);
                selectedSummonCardId = nextSelectedSummonCardId;
                selectedSakuraCakeCardId = nextSelectedSakuraCakeCardId;
            }
        }, isReadOnly: false);

        drawCardGrid("放逐区卡牌（公开）", projectionSnapshot.gapZoneCards, _ => string.Empty, card =>
        {
            onLocalBlocked(BuildReadOnlyZoneCardMessage("gapZone", card.cardInstanceNumericId));
        }, isReadOnly: true);

        GUILayout.Label($"已选樱花饼卡牌: {selectedSakuraCakeCardIdSnapshot?.ToString() ?? "(无)"}");
        drawCardGrid("樱花饼区卡牌", projectionSnapshot.sakuraCakeCards, card =>
        {
            return selectedSakuraCakeCardIdSnapshot.HasValue &&
                   selectedSakuraCakeCardIdSnapshot.Value == card.cardInstanceNumericId
                ? "[樱花饼已选]"
                : string.Empty;
        }, card =>
        {
            lock (stateLock)
            {
                var nextSelectedSakuraCakeCardId = selectedSakuraCakeCardId;
                var nextSelectedSummonCardId = selectedSummonCardId;
                ApplySakuraCardSelection(ref nextSelectedSakuraCakeCardId, ref nextSelectedSummonCardId, card.cardInstanceNumericId);
                selectedSakuraCakeCardId = nextSelectedSakuraCakeCardId;
                selectedSummonCardId = nextSelectedSummonCardId;
            }
        }, isReadOnly: false);

        GUILayout.Label(
            $"当前角色: 生命={projectionSnapshot.activeCharacterCurrentHp?.ToString() ?? "(空)"}/{projectionSnapshot.activeCharacterMaxHp?.ToString() ?? "(空)"} 状态=[{string.Join(",", projectionSnapshot.activeCharacterStatusKeys)}]");
        GUILayout.Label(
            $"交互信息: 有输入上下文={projectionSnapshot.interaction.hasInputContext} 输入上下文ID={projectionSnapshot.interaction.inputContextNumericId?.ToString() ?? "(空)"} 必需玩家={projectionSnapshot.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(空)"} 输入类型={projectionSnapshot.interaction.inputTypeKey} 上下文键={projectionSnapshot.interaction.contextKey} 选项数量={projectionSnapshot.interaction.inputChoiceCount} 有响应窗={projectionSnapshot.interaction.hasResponseWindow} 响应窗ID={projectionSnapshot.interaction.responseWindowNumericId?.ToString() ?? "(空)"} 当前响应者={projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"} 响应者数量={projectionSnapshot.interaction.responseResponderCount}");
        GUILayout.Label(
            $"交互选项键=[{string.Join(", ", projectionSnapshot.interaction.inputChoiceKeys)}] 已选键={projectionSnapshot.interaction.selectedChoiceKey}");

        GUILayout.Label("最近事件日志:");
        if (projectionSnapshot.eventLog.Count == 0)
        {
            GUILayout.Label("(无)");
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

    private static void drawCurrentPlayerSnapshotSection(ProjectionViewModel projection)
    {
        if (!projection.currentPlayerNumericId.HasValue)
        {
            GUILayout.Label("当前回合玩家快照: (当前玩家ID为空)");
            return;
        }

        var currentPlayerSummary = projection.playerSummaries
            .Find(summary => summary.playerNumericId == projection.currentPlayerNumericId.Value);
        if (currentPlayerSummary is null)
        {
            GUILayout.Label($"当前回合玩家快照: (未找到玩家 {projection.currentPlayerNumericId.Value} 的公开摘要)");
            return;
        }

        var currentTeamSummary = findTeamSummaryById(projection, currentPlayerSummary.teamNumericId);
        var leylineText = currentTeamSummary is null
            ? "(未提供)"
            : currentTeamSummary.leyline.ToString();
        GUILayout.Label(
            $"当前回合玩家快照: P{currentPlayerSummary.playerNumericId} 队伍={currentPlayerSummary.teamNumericId} 队伍灵脉={leylineText} 灵力={currentPlayerSummary.mana} 技能点={currentPlayerSummary.skillPoint} 灵符预览={currentPlayerSummary.sigilPreview} 锁定灵符={(currentPlayerSummary.isSigilLocked ? currentPlayerSummary.lockedSigil?.ToString() ?? "(空)" : "(未锁定)")} 手牌={currentPlayerSummary.handCount} 场上={currentPlayerSummary.fieldCount} 弃牌={currentPlayerSummary.discardCount}");
        GUILayout.Label(
            $"当前回合玩家角色: HP={currentPlayerSummary.activeCharacterCurrentHp?.ToString() ?? "(空)"}/{currentPlayerSummary.activeCharacterMaxHp?.ToString() ?? "(空)"} 状态=[{string.Join(",", currentPlayerSummary.activeCharacterStatusKeys)}]");
    }

    private static void drawTeamResourceSummarySection(ProjectionViewModel projection)
    {
        GUILayout.Label("队伍资源摘要");
        if (projection.teamSummaries.Count == 0)
        {
            GUILayout.Label("(暂无队伍资源信息：当前投影未提供 teams)");
            return;
        }

        foreach (var teamSummary in projection.teamSummaries)
        {
            GUILayout.Label(
                $"队伍{teamSummary.teamNumericId}: 灵脉={teamSummary.leyline} 击坠分={teamSummary.killScore}");
        }
    }

    private static void drawAllPlayerSummarySection(ProjectionViewModel projection)
    {
        GUILayout.Label("全部玩家公开摘要");
        if (projection.playerSummaries.Count == 0)
        {
            GUILayout.Label("(暂无玩家摘要)");
            return;
        }

        foreach (var summary in projection.playerSummaries)
        {
            var currentTag = summary.isCurrentPlayer ? "[当前]" : string.Empty;
            var viewerTag = summary.isViewerPlayer ? "[自己]" : string.Empty;
            var lockedSigilText = summary.isSigilLocked
                ? summary.lockedSigil?.ToString() ?? "(空)"
                : "(未锁定)";
            var hpText = $"{summary.activeCharacterCurrentHp?.ToString() ?? "(空)"}/{summary.activeCharacterMaxHp?.ToString() ?? "(空)"}";
            var statusText = summary.activeCharacterStatusKeys.Count > 0
                ? string.Join(",", summary.activeCharacterStatusKeys)
                : "(无)";
            var playerStatusText = summary.playerStatusKeys.Count > 0
                ? string.Join(",", summary.playerStatusKeys)
                : "(无)";
            GUILayout.Label(
                $"P{summary.playerNumericId} {currentTag}{viewerTag} 队伍={summary.teamNumericId} HP={hpText} 角色状态=[{statusText}] 玩家状态=[{playerStatusText}] 灵力={summary.mana} 技能点={summary.skillPoint} 灵符预览={summary.sigilPreview} 锁定灵符={lockedSigilText} 手牌={summary.handCount} 场上={summary.fieldCount} 弃牌={summary.discardCount}");
        }
    }

    private static void drawAllPlayerSpecialStatusSection(ProjectionViewModel projection)
    {
        GUILayout.Label("全部玩家特殊状态（封印/禁锢/结界等）");
        if (projection.playerSummaries.Count == 0)
        {
            GUILayout.Label("(暂无玩家状态信息)");
            return;
        }

        foreach (var summary in projection.playerSummaries)
        {
            var playerStatusText = buildLocalizedStatusText(summary.playerStatusKeys);
            var characterStatusText = buildLocalizedStatusText(summary.activeCharacterStatusKeys);

            var viewerTag = summary.isViewerPlayer ? "（自己）" : string.Empty;
            var currentTag = summary.isCurrentPlayer ? "（当前行动）" : string.Empty;
            GUILayout.Label($"P{summary.playerNumericId}{viewerTag}{currentTag}：玩家状态={playerStatusText}；角色状态={characterStatusText}");
        }
    }

    private static string buildLocalizedStatusText(List<string> statusKeys)
    {
        if (statusKeys.Count <= 0)
        {
            return "(无)";
        }

        var localizedStatuses = new List<string>(statusKeys.Count);
        foreach (var statusKey in statusKeys)
        {
            localizedStatuses.Add(localizeStatusKey(statusKey));
        }

        return string.Join("，", localizedStatuses);
    }

    private void drawFlowChecklistSection()
    {
        List<DebugFlowStepState> stepStates;
        string recommendedNextStep;
        string lastStepResult;
        string keyProjectionSummary;
        List<string> flowTraceLinesSnapshot;
        string traceCopyStatusSnapshot;
        DebugChecklistMode checklistModeSnapshot;
        string autoRunMacroStatusSnapshot;
        bool autoRunMacroEnabledSnapshot;
        DebugChecklistMode autoRunMacroModeSnapshot;

        lock (stateLock)
        {
            checklistModeSnapshot = checklistMode;
            flowChecklistRuntime.setCurrentMode(checklistModeSnapshot);
            stepStates = flowChecklistRuntime.getStepStatesSnapshot(checklistModeSnapshot);
            recommendedNextStep = flowChecklistRuntime.getRecommendedNextStep(checklistModeSnapshot);
            lastStepResult = flowChecklistRuntime.getLastStepResultText(checklistModeSnapshot);
            keyProjectionSummary = buildChecklistKeyProjectionSummary(latestProjection.deepClone(), checklistModeSnapshot);
            flowTraceLinesSnapshot = flowTraceLinesByMode.TryGetValue(checklistModeSnapshot, out var flowTraceLinesForMode)
                ? new List<string>(flowTraceLinesForMode)
                : new List<string>();
            traceCopyStatusSnapshot = traceCopyStatus;
            autoRunMacroStatusSnapshot = autoRunMacroStatus;
            autoRunMacroEnabledSnapshot = autoRunMacroEnabled;
            autoRunMacroModeSnapshot = autoRunMacroMode;
        }

        GUILayout.Label("流程检查");
        drawChecklistModeSwitcher();
        GUILayout.Label($"当前清单模式: {localizeChecklistModeText(checklistModeSnapshot)}");
        GUILayout.Label($"推荐下一步: {localizeRecommendedNextStepText(recommendedNextStep)}");
        GUILayout.Label($"最近一步结果: {localizeFlowNoteText(lastStepResult)}");
        GUILayout.Label($"关键字段摘要: {keyProjectionSummary}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Run A Step", GUILayout.Width(120f)))
        {
            runChecklistStepManual(DebugChecklistMode.mainFlowA);
        }

        if (GUILayout.Button("Run B Step", GUILayout.Width(120f)))
        {
            runChecklistStepManual(DebugChecklistMode.responseWindowB);
        }

        if (GUILayout.Button("Run C Step", GUILayout.Width(120f)))
        {
            runChecklistStepManual(DebugChecklistMode.inputContextC);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Run A Full", GUILayout.Width(120f)))
        {
            startChecklistMacro(DebugChecklistMode.mainFlowA);
        }

        if (GUILayout.Button("Run B Full", GUILayout.Width(120f)))
        {
            startChecklistMacro(DebugChecklistMode.responseWindowB);
        }

        if (GUILayout.Button("Run C Full", GUILayout.Width(120f)))
        {
            startChecklistMacro(DebugChecklistMode.inputContextC);
        }

        if (GUILayout.Button("停止 Full", GUILayout.Width(120f)))
        {
            stopChecklistMacro("手动停止自动流程。");
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(
            $"Full 自动运行: {(autoRunMacroEnabledSnapshot ? "运行中" : "未运行")} 模式={localizeChecklistModeText(autoRunMacroModeSnapshot)} 状态={localizeFlowNoteText(autoRunMacroStatusSnapshot)}");

        for (var index = 0; index < stepStates.Count; index++)
        {
            var step = stepStates[index];
            GUILayout.Label($"[{step.stepNumber}] {localizeStepDisplayNameText(step.displayName)} -> {localizeFlowStatusText(step.status)} {localizeFlowNoteText(step.note)}");
        }

        GUILayout.Label("流程追踪");
        flowTraceScroll = GUILayout.BeginScrollView(flowTraceScroll, GUILayout.Height(120f));
        if (flowTraceLinesSnapshot.Count == 0)
        {
            GUILayout.Label("(暂无追踪记录)");
        }
        else
        {
            for (var index = 0; index < flowTraceLinesSnapshot.Count; index++)
            {
                GUILayout.Label(flowTraceLinesSnapshot[index]);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUILayout.Label("导出条数", GUILayout.Width(65f));
        traceExportCountText = GUILayout.TextField(traceExportCountText, GUILayout.Width(80f));
        if (GUILayout.Button("复制当前模式追踪", GUILayout.Width(180f)))
        {
            var traceCount = TryParsePositiveTraceCount(traceExportCountText, 20);
            var exportText = BuildFlowTraceExport(flowTraceLinesSnapshot, traceCount);
            GUIUtility.systemCopyBuffer = exportText;
            lock (stateLock)
            {
                traceCopyStatus = $"已复制 {Math.Min(traceCount, flowTraceLinesSnapshot.Count)} 条追踪记录。";
            }
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(traceCopyStatusSnapshot))
        {
            GUILayout.Label(traceCopyStatusSnapshot);
        }
    }

    public static int TryParsePositiveTraceCount(string traceCountText, int defaultCount)
    {
        if (int.TryParse(traceCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCount) &&
            parsedCount > 0)
        {
            return parsedCount;
        }

        return defaultCount;
    }

    public static string BuildFlowTraceExport(List<string> flowTraceLines, int maxLineCount)
    {
        if (flowTraceLines.Count == 0)
        {
            return "(暂无追踪记录)";
        }

        var effectiveLineCount = Math.Max(1, maxLineCount);
        var startIndex = Math.Max(0, flowTraceLines.Count - effectiveLineCount);
        var outputLines = new List<string>(flowTraceLines.Count - startIndex);
        for (var index = startIndex; index < flowTraceLines.Count; index++)
        {
            outputLines.Add(flowTraceLines[index]);
        }

        return string.Join("\n", outputLines);
    }

    public static int ResolveStepNumberFromResult(string lastStepResult, int fallbackStepNumber)
    {
        if (string.IsNullOrWhiteSpace(lastStepResult) ||
            !lastStepResult.StartsWith("step ", StringComparison.Ordinal))
        {
            return fallbackStepNumber;
        }

        var colonIndex = lastStepResult.IndexOf(':');
        var header = colonIndex > 0 ? lastStepResult.Substring(0, colonIndex) : lastStepResult;
        var segments = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return fallbackStepNumber;
        }

        return int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStepNumber)
            ? parsedStepNumber
            : fallbackStepNumber;
    }

    public static string GetMacroActionTypeForStep(DebugChecklistMode mode, int stepNumber)
    {
        return mode switch
        {
            DebugChecklistMode.mainFlowA => stepNumber switch
            {
                2 => "drawOneCard",
                3 => "playTreasureCard",
                4 => "enterSummonPhase",
                5 => "summonTreasureCard",
                6 => "enterEndPhase",
                _ => string.Empty,
            },
            DebugChecklistMode.responseWindowB => stepNumber switch
            {
                1 => "debugOpenDamageResponseWindow",
                4 => "submitResponse",
                _ => string.Empty,
            },
            DebugChecklistMode.inputContextC => stepNumber switch
            {
                1 => "enterActionPhase",
                2 => "drawOneCard",
                3 => "enterEndPhase",
                6 => "submitInputChoice",
                _ => string.Empty,
            },
            _ => string.Empty,
        };
    }

    public static bool IsMacroStepDispatchable(DebugChecklistMode mode, int stepNumber)
    {
        return mode == DebugChecklistMode.mainFlowA && stepNumber == 1 ||
               !string.IsNullOrWhiteSpace(GetMacroActionTypeForStep(mode, stepNumber));
    }

    private void runChecklistStepManual(DebugChecklistMode mode)
    {
        stopChecklistMacro("手动执行单步，已停止自动流程。", onlyIfActive: true);
        runChecklistStep(mode);
    }

    private void startChecklistMacro(DebugChecklistMode mode)
    {
        lock (stateLock)
        {
            checklistMode = mode;
            flowChecklistRuntime.setCurrentMode(mode);
            autoRunMacroMode = mode;
            autoRunMacroEnabled = true;
            autoRunMacroStatus = $"已启动 {localizeChecklistModeText(mode)} 自动流程。";
        }

        tryContinueChecklistMacro("start-macro");
    }

    private void stopChecklistMacro(string reason, bool onlyIfActive = false)
    {
        lock (stateLock)
        {
            if (onlyIfActive && !autoRunMacroEnabled)
            {
                return;
            }

            autoRunMacroEnabled = false;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                autoRunMacroStatus = reason;
            }
        }
    }

    private void tryContinueChecklistMacro(string triggerSource)
    {
        lock (stateLock)
        {
            if (!autoRunMacroEnabled || autoRunMacroTickInProgress)
            {
                return;
            }

            autoRunMacroTickInProgress = true;
        }

        try
        {
            while (true)
            {
                DebugChecklistMode modeSnapshot;
                var hasInFlightRequest = false;
                var isCompleted = false;
                var stepIndex = 0;
                lock (stateLock)
                {
                    if (!autoRunMacroEnabled)
                    {
                        return;
                    }

                    modeSnapshot = autoRunMacroMode;
                    flowChecklistRuntime.setCurrentMode(modeSnapshot);
                    hasInFlightRequest = requestInFlight;
                    isCompleted = flowChecklistRuntime.isCompletedForMode(modeSnapshot);
                    stepIndex = flowChecklistRuntime.getCurrentStepIndex(modeSnapshot);
                }

                if (isCompleted)
                {
                    stopChecklistMacro($"自动流程完成：{localizeChecklistModeText(modeSnapshot)}。");
                    return;
                }

                var stepStatus = flowChecklistRuntime.getStepStatus(modeSnapshot, stepIndex);
                if (stepStatus == DebugFlowStepStatus.failed)
                {
                    var lastStepResult = flowChecklistRuntime.getLastStepResultText(modeSnapshot);
                    stopChecklistMacro(
                        $"自动流程停止：{localizeChecklistModeText(modeSnapshot)} Step {stepIndex} 已失败。{localizeFlowNoteText(lastStepResult)}");
                    return;
                }

                if (hasInFlightRequest)
                {
                    lock (stateLock)
                    {
                        autoRunMacroStatus = $"等待响应中（触发来源={triggerSource}）。";
                    }

                    return;
                }

                if (!IsMacroStepDispatchable(modeSnapshot, stepIndex))
                {
                    lock (stateLock)
                    {
                        autoRunMacroStatus =
                            $"等待观察步骤推进：{localizeChecklistModeText(modeSnapshot)} Step {stepIndex}（触发来源={triggerSource}）。";
                    }

                    return;
                }

                var stepBeforeDispatch = stepIndex;
                runChecklistStep(modeSnapshot);

                DebugChecklistMode modeAfterDispatch;
                var hasInFlightAfterDispatch = false;
                var isCompletedAfterDispatch = false;
                var stepAfterDispatch = 0;
                lock (stateLock)
                {
                    if (!autoRunMacroEnabled)
                    {
                        return;
                    }

                    modeAfterDispatch = autoRunMacroMode;
                    flowChecklistRuntime.setCurrentMode(modeAfterDispatch);
                    hasInFlightAfterDispatch = requestInFlight;
                    isCompletedAfterDispatch = flowChecklistRuntime.isCompletedForMode(modeAfterDispatch);
                    stepAfterDispatch = flowChecklistRuntime.getCurrentStepIndex(modeAfterDispatch);
                }

                if (isCompletedAfterDispatch)
                {
                    stopChecklistMacro($"自动流程完成：{localizeChecklistModeText(modeAfterDispatch)}。");
                    return;
                }

                if (hasInFlightAfterDispatch)
                {
                    lock (stateLock)
                    {
                        autoRunMacroStatus = $"已发送步骤 {stepBeforeDispatch} 请求，等待响应。";
                    }

                    return;
                }

                if (modeAfterDispatch == DebugChecklistMode.mainFlowA &&
                    stepBeforeDispatch == 1 &&
                    stepAfterDispatch == 1)
                {
                    lock (stateLock)
                    {
                        autoRunMacroStatus = "等待连接建立（A1）。";
                    }

                    return;
                }

                if (stepAfterDispatch == stepBeforeDispatch)
                {
                    stopChecklistMacro(
                        $"自动流程中断：步骤 {stepBeforeDispatch} 未推进（触发来源={triggerSource}，请检查错误提示）。");
                    return;
                }

                lock (stateLock)
                {
                    autoRunMacroStatus = $"步骤 {stepBeforeDispatch} 已推进，继续执行下一步。";
                }
            }
        }
        finally
        {
            lock (stateLock)
            {
                autoRunMacroTickInProgress = false;
            }
        }
    }

    private void runChecklistStep(DebugChecklistMode mode)
    {
        if (bridge is null)
        {
            onLocalBlocked("本地拦截：SocketBridge 尚未初始化。");
            return;
        }

        ProjectionViewModel projectionSnapshot;
        long? selectedHandCardIdSnapshot;
        long? selectedSummonCardIdSnapshot;
        long? selectedSakuraCakeCardIdSnapshot;
        int stepIndex;

        lock (stateLock)
        {
            checklistMode = mode;
            flowChecklistRuntime.setCurrentMode(mode);
            projectionSnapshot = latestProjection.deepClone();
            selectedHandCardIdSnapshot = selectedHandCardId;
            selectedSummonCardIdSnapshot = selectedSummonCardId;
            selectedSakuraCakeCardIdSnapshot = selectedSakuraCakeCardId;
            stepIndex = flowChecklistRuntime.getCurrentStepIndex(mode);
        }

        if (flowChecklistRuntime.isCompletedForMode(mode))
        {
            onLocalBlocked($"本地拦截：{localizeChecklistModeText(mode)} 已完成，无需继续执行。");
            return;
        }

        var macroActionType = GetMacroActionTypeForStep(mode, stepIndex);
        var isConnectStep = mode == DebugChecklistMode.mainFlowA && stepIndex == 1;
        if (!isConnectStep &&
            !string.IsNullOrWhiteSpace(macroActionType) &&
            !canSendRequest())
        {
            onLocalBlocked($"本地拦截：无法执行 {macroActionType}，当前未连接或有请求进行中。");
            return;
        }

        switch (mode)
        {
            case DebugChecklistMode.mainFlowA:
                runMainFlowStep(stepIndex, projectionSnapshot, selectedHandCardIdSnapshot, selectedSummonCardIdSnapshot, selectedSakuraCakeCardIdSnapshot);
                return;
            case DebugChecklistMode.responseWindowB:
                runResponseWindowFlowStep(stepIndex, projectionSnapshot);
                return;
            case DebugChecklistMode.inputContextC:
                runInputContextFlowStep(stepIndex, projectionSnapshot);
                return;
            default:
                onLocalBlocked("本地拦截：未知 Checklist 模式。");
                return;
        }
    }

    private void runMainFlowStep(
        int stepIndex,
        ProjectionViewModel projectionSnapshot,
        long? selectedHandCardIdSnapshot,
        long? selectedSummonCardIdSnapshot,
        long? selectedSakuraCakeCardIdSnapshot)
    {
        switch (stepIndex)
        {
            case 1:
                connect();
                return;
            case 2:
                if (!string.Equals(projectionSnapshot.currentPhase, "action", StringComparison.OrdinalIgnoreCase))
                {
                    onLocalBlocked("本地拦截：A2 需要当前阶段为 action。若当前是 start，请先点“调试：进入行动”。");
                    return;
                }

                applyViewerAndActor();
                sendTrackedAction("drawOneCard", () => bridge?.SendDrawOneCard());
                return;
            case 3:
                if (TryResolveSelectedCardId(selectedHandCardIdSnapshot, out var playSelectedCardId))
                {
                    lock (stateLock)
                    {
                        lastPlayedSelectedHandCardId = playSelectedCardId;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("playTreasureCard", () => bridge?.SendPlayTreasureCard(playSelectedCardId));
                    return;
                }

                if (projectionSnapshot.handCards.Count > 0)
                {
                    var playFirstCardId = projectionSnapshot.handCards[0].cardInstanceNumericId;
                    lock (stateLock)
                    {
                        lastPlayedSelectedHandCardId = null;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("playTreasureCard", () => bridge?.SendPlayTreasureCard(playFirstCardId));
                    return;
                }

                onLocalBlocked("本地拦截：A3 需要先有可打出的手牌。");
                return;
            case 4:
                applyViewerAndActor();
                sendTrackedAction("enterSummonPhase", () => bridge?.SendEnterSummonPhase());
                return;
            case 5:
                if (TryResolveSelectedCardId(selectedSummonCardIdSnapshot, out var selectedSummonCardId))
                {
                    lock (stateLock)
                    {
                        lastSummonedSelectedCardId = selectedSummonCardId;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(selectedSummonCardId));
                    return;
                }

                if (TryResolveSelectedCardId(selectedSakuraCakeCardIdSnapshot, out var selectedSakuraCardId))
                {
                    lock (stateLock)
                    {
                        lastSummonedSelectedCardId = selectedSakuraCardId;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(selectedSakuraCardId));
                    return;
                }

                if (projectionSnapshot.summonZoneCards.Count > 0)
                {
                    var summonFirstCardId = projectionSnapshot.summonZoneCards[0].cardInstanceNumericId;
                    lock (stateLock)
                    {
                        lastSummonedSelectedCardId = null;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(summonFirstCardId));
                    return;
                }

                if (projectionSnapshot.sakuraCakeCards.Count > 0)
                {
                    var summonSakuraFirstCardId = projectionSnapshot.sakuraCakeCards[0].cardInstanceNumericId;
                    lock (stateLock)
                    {
                        lastSummonedSelectedCardId = null;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("summonTreasureCard", () => bridge?.SendSummonTreasureCard(summonSakuraFirstCardId));
                    return;
                }

                onLocalBlocked("本地拦截：A5 没有可召唤的召唤区/樱花饼区卡牌。");
                return;
            case 6:
                applyViewerAndActor();
                sendTrackedAction("enterEndPhase", () => bridge?.SendEnterEndPhase());
                return;
            case 7:
            case 8:
                onLocalBlocked($"本地提示：A{stepIndex} 为观察步骤，请等待自动推进或交互续跑结果。");
                return;
            default:
                onLocalBlocked($"本地拦截：A 模式未知步骤 {stepIndex}。");
                return;
        }
    }

    private void runResponseWindowFlowStep(int stepIndex, ProjectionViewModel projectionSnapshot)
    {
        switch (stepIndex)
        {
            case 1:
                applyViewerAndActor();
                sendTrackedAction("debugOpenDamageResponseWindow", () => bridge?.SendDebugOpenDamageResponseWindow());
                return;
            case 4:
                if (!projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId.HasValue ||
                    projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId.Value <= 0)
                {
                    onLocalBlocked("本地拦截：B4 缺少 currentResponderPlayerNumericId。");
                    return;
                }

                if (!TryValidateSubmitResponseNoActor(
                        projectionSnapshot,
                        actorPlayerNumericIdText,
                        out _,
                        out var failureReason))
                {
                    onLocalBlocked($"本地拦截：B4 提交不响应失败。{failureReason}");
                    return;
                }

                applyViewerAndActor();
                sendTrackedAction("submitResponse", () => bridge?.SendSubmitResponseNo());
                return;
            case 2:
            case 3:
            case 5:
            case 6:
            case 7:
            case 8:
                onLocalBlocked($"本地提示：B{stepIndex} 为观察步骤，请等待对应响应或检查投影字段。");
                return;
            default:
                onLocalBlocked($"本地拦截：B 模式未知步骤 {stepIndex}。");
                return;
        }
    }

    private void runInputContextFlowStep(int stepIndex, ProjectionViewModel projectionSnapshot)
    {
        switch (stepIndex)
        {
            case 1:
                applyViewerAndActor();
                sendTrackedAction("enterActionPhase", () => bridge?.SendEnterActionPhase());
                return;
            case 2:
                applyViewerAndActor();
                sendTrackedAction("drawOneCard", () => bridge?.SendDrawOneCard());
                return;
            case 3:
                applyViewerAndActor();
                sendTrackedAction("enterEndPhase", () => bridge?.SendEnterEndPhase());
                return;
            case 6:
                if (!projectionSnapshot.interaction.inputContextNumericId.HasValue ||
                    projectionSnapshot.interaction.inputContextNumericId.Value <= 0)
                {
                    onLocalBlocked("本地拦截：C6 没有有效 inputContextNumericId。");
                    return;
                }

                if (projectionSnapshot.interaction.inputRequiredPlayerNumericId.HasValue &&
                    projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value > 0)
                {
                    actorPlayerNumericIdText = projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value.ToString();
                }

                string choiceKeyToSend;
                if (projectionSnapshot.interaction.inputChoiceKeys.Count > 0)
                {
                    choiceKeyToSend = projectionSnapshot.interaction.inputChoiceKeys[0];
                }
                else if (!string.IsNullOrWhiteSpace(inputChoiceKeyManualText))
                {
                    choiceKeyToSend = inputChoiceKeyManualText;
                }
                else
                {
                    onLocalBlocked("本地拦截：C6 未找到可提交的 choiceKey。");
                    return;
                }

                applyViewerAndActor();
                sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoice(choiceKeyToSend));
                return;
            case 4:
            case 5:
            case 7:
            case 8:
            case 9:
                onLocalBlocked($"本地提示：C{stepIndex} 为观察步骤，请等待对应响应或检查投影字段。");
                return;
            default:
                onLocalBlocked($"本地拦截：C 模式未知步骤 {stepIndex}。");
                return;
        }
    }

    private void drawChecklistModeSwitcher()
    {
        var previousEnabled = GUI.enabled;
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        drawChecklistModeButton(DebugChecklistMode.mainFlowA, "A：主流程");
        drawChecklistModeButton(DebugChecklistMode.responseWindowB, "B：响应窗续跑");
        drawChecklistModeButton(DebugChecklistMode.inputContextC, "C：输入续跑");
        GUILayout.EndHorizontal();
        GUI.enabled = previousEnabled;
    }

    private void drawChecklistModeButton(DebugChecklistMode mode, string label)
    {
        var originalColor = GUI.color;
        if (checklistMode == mode)
        {
            GUI.color = new Color(0.38f, 0.72f, 0.38f, 1f);
        }

        if (GUILayout.Button(label, GUILayout.Width(170f)))
        {
            lock (stateLock)
            {
                checklistMode = mode;
                flowChecklistRuntime.setCurrentMode(mode);
            }
        }

        GUI.color = originalColor;
    }

    private static string buildDiscardAreaSummary(ProjectionViewModel projection)
    {
        string discardEventSummary = "(无)";
        for (var index = projection.eventLog.Count - 1; index >= 0; index--)
        {
            var eventLine = projection.eventLog[index];
            if (eventLine.IndexOf("discard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                discardEventSummary = eventLine;
                break;
            }
        }

        return $"数量={projection.discardCount}，最近弃牌事件={discardEventSummary}";
    }

    private static string buildInputChoiceDisplayLabel(ProjectionViewModel projection, string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            return "选项：（空）";
        }

        if (string.Equals(choiceKey, "banish:decline", StringComparison.Ordinal))
        {
            return "选项：不放逐（banish:decline）";
        }

        if (string.Equals(choiceKey, "shackle:decline", StringComparison.Ordinal))
        {
            return "选项：不弃牌并跳过行动/召唤（shackle:decline）";
        }

        if (choiceKey.StartsWith("player:", StringComparison.Ordinal))
        {
            var playerSegment = choiceKey.Substring("player:".Length);
            return $"选项：玩家 {playerSegment}";
        }

        if (choiceKey.StartsWith("discardCard:", StringComparison.Ordinal))
        {
            var cardSegment = choiceKey.Substring("discardCard:".Length);
            if (long.TryParse(cardSegment, out var cardNumericId) &&
                tryResolveCardChoiceDisplayInfo(projection, cardNumericId, out var definitionId, out var zoneKey))
            {
                return $"选项：弃置 #{cardNumericId}（{definitionId} / {localizeZoneKeyText(zoneKey)}）";
            }

            return $"选项：弃置 #{cardSegment}";
        }

        if (choiceKey.StartsWith("banishCard:", StringComparison.Ordinal))
        {
            var cardSegment = choiceKey.Substring("banishCard:".Length);
            if (long.TryParse(cardSegment, out var cardNumericId) &&
                tryResolveCardChoiceDisplayInfo(projection, cardNumericId, out var definitionId, out var zoneKey))
            {
                return $"选项：放逐 #{cardNumericId}（{definitionId} / {localizeZoneKeyText(zoneKey)}）";
            }

            return $"选项：放逐 #{cardSegment}";
        }

        return $"选项：{choiceKey}";
    }

    private static bool tryResolveCardChoiceDisplayInfo(
        ProjectionViewModel projection,
        long cardInstanceNumericId,
        out string definitionId,
        out string zoneKey)
    {
        if (tryFindCardById(projection.handCards, cardInstanceNumericId, out definitionId, out zoneKey) ||
            tryFindCardById(projection.discardCards, cardInstanceNumericId, out definitionId, out zoneKey) ||
            tryFindCardById(projection.fieldCards, cardInstanceNumericId, out definitionId, out zoneKey) ||
            tryFindCardById(projection.summonZoneCards, cardInstanceNumericId, out definitionId, out zoneKey) ||
            tryFindCardById(projection.sakuraCakeCards, cardInstanceNumericId, out definitionId, out zoneKey) ||
            tryFindCardById(projection.gapZoneCards, cardInstanceNumericId, out definitionId, out zoneKey))
        {
            return true;
        }

        definitionId = "(未知)";
        zoneKey = string.Empty;
        return false;
    }

    private static bool tryFindCardById(
        List<ProjectionCardViewModel> cards,
        long cardInstanceNumericId,
        out string definitionId,
        out string zoneKey)
    {
        foreach (var card in cards)
        {
            if (card.cardInstanceNumericId != cardInstanceNumericId)
            {
                continue;
            }

            definitionId = string.IsNullOrWhiteSpace(card.definitionId) ? "(未知)" : card.definitionId;
            zoneKey = card.zoneKey;
            return true;
        }

        definitionId = string.Empty;
        zoneKey = string.Empty;
        return false;
    }

    private void drawCardGrid(
        string title,
        List<ProjectionCardViewModel> cards,
        Func<ProjectionCardViewModel, string> getSelectionLabel,
        Action<ProjectionCardViewModel>? onCardClicked,
        bool isReadOnly)
    {
        GUILayout.Label(title);
        if (cards.Count == 0)
        {
            GUILayout.Label("(无)");
            return;
        }

        var availableWidth = Mathf.Max(1f, panelWidth - 80f);
        var maxColumns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + DebugCardGap) / (DebugCardWidth + DebugCardGap)));
        var cardIndex = 0;

        while (cardIndex < cards.Count)
        {
            GUILayout.BeginHorizontal();
            for (var column = 0; column < maxColumns && cardIndex < cards.Count; column++, cardIndex++)
            {
                if (column > 0)
                {
                    GUILayout.Space(DebugCardGap);
                }

                var card = cards[cardIndex];
                var selectionLabel = getSelectionLabel(card);
                drawDebugCard(card, selectionLabel, onCardClicked, isReadOnly);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(DebugCardGap);
        }
    }

    private void drawDebugCard(
        ProjectionCardViewModel card,
        string selectionLabel,
        Action<ProjectionCardViewModel>? onCardClicked,
        bool isReadOnly)
    {
        var cardRect = GUILayoutUtility.GetRect(
            DebugCardWidth,
            DebugCardHeight,
            GUILayout.Width(DebugCardWidth),
            GUILayout.Height(DebugCardHeight));

        var isSelected = !string.IsNullOrWhiteSpace(selectionLabel);
        drawCardBackground(cardRect, isSelected);

        var texture = DebugCardTextureResolver.GetTextureForDefinition(card.definitionId);
        var imageRect = new Rect(
            cardRect.x + 8f,
            cardRect.y + 8f,
            cardRect.width - 16f,
            DebugCardImageHeight);

        if (texture is not null)
        {
            GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
        }
        else
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            GUI.DrawTexture(imageRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;

            var placeholderStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            GUI.Label(imageRect, "NO IMAGE", placeholderStyle);
        }

        var definitionRect = new Rect(cardRect.x + 8f, cardRect.y + 144f, cardRect.width - 16f, 22f);
        var definitionStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white },
        };
        GUI.Label(definitionRect, card.definitionId, definitionStyle);

        var idRect = new Rect(cardRect.x + 8f, cardRect.y + 166f, cardRect.width - 16f, 20f);
        var metaStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) },
        };
        GUI.Label(idRect, $"#{card.cardInstanceNumericId}", metaStyle);

        var zoneRect = new Rect(cardRect.x + 8f, cardRect.y + 186f, cardRect.width - 16f, 16f);
        GUI.Label(zoneRect, localizeZoneKeyText(card.zoneKey), metaStyle);

        if (!string.IsNullOrWhiteSpace(selectionLabel))
        {
            var selectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 0.92f, 0.25f, 1f) },
            };
            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 4f, cardRect.width - 16f, 14f), selectionLabel, selectionStyle);
        }

        if (isReadOnly)
        {
            var readOnlyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(1f, 0.6f, 0.4f, 1f) },
            };
            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 4f, cardRect.width - 16f, 14f), "只读", readOnlyStyle);
        }

        if (onCardClicked is not null && GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
        {
            onCardClicked(card);
        }
    }

    private static void drawCardBackground(Rect cardRect, bool isSelected)
    {
        var backgroundColor = isSelected
            ? new Color(0.38f, 0.33f, 0.08f, 0.96f)
            : new Color(0.18f, 0.18f, 0.18f, 0.96f);
        var previousColor = GUI.color;
        GUI.color = backgroundColor;
        GUI.DrawTexture(cardRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;

        if (!isSelected)
        {
            return;
        }

        var borderColor = new Color(1f, 0.92f, 0.22f, 1f);
        drawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 2f), borderColor);
        drawRect(new Rect(cardRect.x, cardRect.yMax - 2f, cardRect.width, 2f), borderColor);
        drawRect(new Rect(cardRect.x, cardRect.y, 2f, cardRect.height), borderColor);
        drawRect(new Rect(cardRect.xMax - 2f, cardRect.y, 2f, cardRect.height), borderColor);
    }

    private static void drawRect(Rect rect, Color color)
    {
        var previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }

    private void drawSummarySection()
    {
        string rawRequestJsonSnapshot;
        string rawResponseJsonSnapshot;
        string errorSnapshot;
        string summarySnapshot;
        string responseOriginSnapshot;
        lock (stateLock)
        {
            rawRequestJsonSnapshot = latestRawRequestJson;
            rawResponseJsonSnapshot = latestRawResponseJson;
            errorSnapshot = latestError;
            summarySnapshot = summaryText;
            responseOriginSnapshot = lastResponseOriginForUi;
        }

        GUILayout.Label("结果摘要");
        GUILayout.TextArea(summarySnapshot, GUILayout.Height(85f));
        GUILayout.Label($"最近响应来源: {responseOriginSnapshot}");

        GUILayout.Label("错误信息");
        GUILayout.TextArea(string.IsNullOrEmpty(errorSnapshot) ? "(无)" : errorSnapshot, GUILayout.Height(45f));

        GUILayout.Label("最近请求原始 JSON");
        rawRequestJsonScroll = GUILayout.BeginScrollView(rawRequestJsonScroll, GUILayout.Height(120f));
        GUILayout.TextArea(string.IsNullOrEmpty(rawRequestJsonSnapshot) ? "(暂无请求)" : rawRequestJsonSnapshot, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.Label("最近响应原始 JSON");
        rawResponseJsonScroll = GUILayout.BeginScrollView(rawResponseJsonScroll, GUILayout.Height(120f));
        GUILayout.TextArea(string.IsNullOrEmpty(rawResponseJsonSnapshot) ? "(暂无响应)" : rawResponseJsonSnapshot, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
    }

    private void drawInputContextActionSection()
    {
        ProjectionViewModel projectionSnapshot;
        string actorTextSnapshot;
        List<string> selectedShackleDiscardChoiceKeysSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorTextSnapshot = actorPlayerNumericIdText;
            syncShackleDiscardSelectionWithInputContextLocked(projectionSnapshot);
            selectedShackleDiscardChoiceKeysSnapshot = new List<string>(selectedShackleDiscardChoiceKeys);
        }

        GUILayout.Label("输入上下文");
        var isParallelInputContext = projectionSnapshot.interaction.inputRequiredPlayerNumericIds.Count > 0;
        var requiredPlayersText = isParallelInputContext
            ? string.Join(",", projectionSnapshot.interaction.inputRequiredPlayerNumericIds)
            : projectionSnapshot.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(空)";
        var submittedPlayersText = projectionSnapshot.interaction.inputSubmittedPlayerNumericIds.Count > 0
            ? string.Join(",", projectionSnapshot.interaction.inputSubmittedPlayerNumericIds)
            : "(空)";
        GUILayout.Label(
            $"输入上下文ID: {projectionSnapshot.interaction.inputContextNumericId?.ToString() ?? "(空)"} 必需玩家: {requiredPlayersText} 选项数量: {projectionSnapshot.interaction.inputChoiceCount}");
        GUILayout.Label(
            $"输入类型: {projectionSnapshot.interaction.inputTypeKey} 上下文键: {projectionSnapshot.interaction.contextKey}");
        GUILayout.Label(
            $"并行提交: {isParallelInputContext} 已提交: {projectionSnapshot.interaction.inputSubmittedPlayerCount}/{projectionSnapshot.interaction.inputRequiredPlayerCount} 已提交玩家: {submittedPlayersText}");

        var hasRenderableInputContext = projectionSnapshot.interaction.hasInputContext &&
                                        projectionSnapshot.interaction.inputContextNumericId.HasValue &&
                                        projectionSnapshot.interaction.inputContextNumericId.Value > 0;

        if (!hasRenderableInputContext)
        {
            GUILayout.Label("(当前无输入上下文)");
            return;
        }

        var actorMatchedRequiredPlayer = true;
        var actorHasAlreadySubmitted = false;
        if (long.TryParse(actorTextSnapshot, out var actorPlayerNumericId))
        {
            if (isParallelInputContext)
            {
                actorMatchedRequiredPlayer =
                    projectionSnapshot.interaction.inputRequiredPlayerNumericIds.Contains(actorPlayerNumericId);
                actorHasAlreadySubmitted =
                    projectionSnapshot.interaction.inputSubmittedPlayerNumericIds.Contains(actorPlayerNumericId);
            }
            else if (projectionSnapshot.interaction.inputRequiredPlayerNumericId.HasValue)
            {
                actorMatchedRequiredPlayer =
                    actorPlayerNumericId == projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value;
            }
        }

        if (!actorMatchedRequiredPlayer)
        {
            GUILayout.Label(
                $"警告：操作者ID（{actorTextSnapshot}）不在当前输入上下文的可提交玩家集合内。");
        }

        if (actorHasAlreadySubmitted)
        {
            GUILayout.Label("你已提交本次选择，正在等待其他玩家完成。");
        }

        var canSend = canSendRequest() && actorMatchedRequiredPlayer && !actorHasAlreadySubmitted;
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

        var isTurnStartShackleContext = IsTurnStartShackleInputContext(projectionSnapshot);
        if (isTurnStartShackleContext)
        {
            GUILayout.Label("禁锢处理：逐张点选要弃置的手牌（可取消），选满4张后提交；或选择“不弃牌”。");
            GUILayout.Label($"当前已选：{selectedShackleDiscardChoiceKeysSnapshot.Count}/4");
        }

        if (projectionSnapshot.interaction.inputChoiceKeys.Count == 0)
        {
            GUILayout.Label("(当前观察者不可见 choiceKeys，或选项为空)");
        }
        else
        {
            GUILayout.Label("选项按钮");
            foreach (var choiceKey in projectionSnapshot.interaction.inputChoiceKeys)
            {
                var capturedChoiceKey = choiceKey;
                var optionLabel = buildInputChoiceDisplayLabel(projectionSnapshot, capturedChoiceKey);
                if (isTurnStartShackleContext && IsShackleDiscardChoiceKey(capturedChoiceKey))
                {
                    var isSelected = selectedShackleDiscardChoiceKeysSnapshot.Contains(capturedChoiceKey);
                    optionLabel = isSelected
                        ? $"[已选] {optionLabel}"
                        : $"[可选] {optionLabel}";
                }
                if (GUILayout.Button(optionLabel, GUILayout.Height(28f)))
                {
                    var isShackleDiscardSingleChoice =
                        isTurnStartShackleContext &&
                        IsShackleDiscardChoiceKey(capturedChoiceKey);
                    if (isShackleDiscardSingleChoice)
                    {
                        var toggleSucceeded = false;
                        string toggleFailureReasonSnapshot;
                        lock (stateLock)
                        {
                            toggleSucceeded = TryToggleBoundedChoiceSelection(
                                selectedShackleDiscardChoiceKeys,
                                capturedChoiceKey,
                                4,
                                out toggleFailureReasonSnapshot);
                            if (toggleSucceeded)
                            {
                                localInterceptionBanner = string.Empty;
                            }

                            selectedShackleDiscardChoiceKeysSnapshot = new List<string>(selectedShackleDiscardChoiceKeys);
                        }

                        if (!toggleSucceeded)
                        {
                            onLocalBlocked(toggleFailureReasonSnapshot);
                        }
                    }
                    else
                    {
                        applyViewerAndActor();
                        sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoice(capturedChoiceKey));
                    }
                }
            }

            if (isTurnStartShackleContext)
            {
                var shackleDiscardChoiceKeys = CollectShackleDiscardChoiceKeys(projectionSnapshot.interaction.inputChoiceKeys);
                var hasShackleDeclineChoice = projectionSnapshot.interaction.inputChoiceKeys
                    .Contains("shackle:decline");

                GUILayout.Space(6f);
                GUILayout.Label("已选弃牌：");
                if (selectedShackleDiscardChoiceKeysSnapshot.Count == 0)
                {
                    GUILayout.Label("(未选择)");
                }
                else
                {
                    foreach (var selectedChoiceKey in selectedShackleDiscardChoiceKeysSnapshot)
                    {
                        GUILayout.Label($"- {buildInputChoiceDisplayLabel(projectionSnapshot, selectedChoiceKey)}");
                    }
                }

                var selectedCountValid = selectedShackleDiscardChoiceKeysSnapshot.Count == 4;
                GUI.enabled = canSend && selectedCountValid;
                if (GUILayout.Button("禁锢：提交已选4张手牌弃置", GUILayout.Height(28f)))
                {
                    if (shackleDiscardChoiceKeys.Count < 4)
                    {
                        onLocalBlocked("本地拦截：禁锢弃牌可选项不足4张，无法提交。");
                    }
                    else if (!selectedCountValid)
                    {
                        onLocalBlocked($"本地拦截：禁锢弃牌需要恰好选择4张，当前为 {selectedShackleDiscardChoiceKeysSnapshot.Count} 张。");
                    }
                    else
                    {
                        var selectedChoiceKeysForSubmit = new List<string>(selectedShackleDiscardChoiceKeysSnapshot);
                        applyViewerAndActor();
                        sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoices(selectedChoiceKeysForSubmit));
                    }
                }

                GUI.enabled = canSend && hasShackleDeclineChoice;
                if (GUILayout.Button("禁锢：不弃牌（跳过行动与召唤）", GUILayout.Height(28f)))
                {
                    applyViewerAndActor();
                    sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoice("shackle:decline"));
                }
                GUI.enabled = canSend;
            }
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("手动输入 choiceKey", GUILayout.Width(115f));
        inputChoiceKeyManualText = GUILayout.TextField(inputChoiceKeyManualText, GUILayout.Width(220f));
        if (GUILayout.Button("提交输入选择", GUILayout.Width(150f)))
        {
            if (string.IsNullOrWhiteSpace(inputChoiceKeyManualText))
            {
                onLocalBlocked("本地拦截：提交输入选择失败，choiceKey 不能为空。");
            }
            else if (isTurnStartShackleContext && IsShackleDiscardChoiceKey(inputChoiceKeyManualText))
            {
                onLocalBlocked("本地拦截：禁锢弃牌请使用上方点选4张并提交，不支持单张 manual choiceKey 提交。");
            }
            else
            {
                applyViewerAndActor();
                sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoice(inputChoiceKeyManualText));
            }
        }
        GUILayout.EndHorizontal();

        GUI.enabled = previousEnabled;
    }

    private void syncShackleDiscardSelectionWithInputContextLocked(ProjectionViewModel projection)
    {
        if (!IsTurnStartShackleInputContext(projection))
        {
            selectedShackleDiscardChoiceKeys.Clear();
            selectedShackleInputContextNumericId = null;
            return;
        }

        var inputContextNumericId = projection.interaction.inputContextNumericId!.Value;
        if (!selectedShackleInputContextNumericId.HasValue ||
            selectedShackleInputContextNumericId.Value != inputContextNumericId)
        {
            selectedShackleDiscardChoiceKeys.Clear();
            selectedShackleInputContextNumericId = inputContextNumericId;
        }

        var availableDiscardChoiceKeys = CollectShackleDiscardChoiceKeys(projection.interaction.inputChoiceKeys);
        PruneSelectedChoiceKeysByAvailable(selectedShackleDiscardChoiceKeys, availableDiscardChoiceKeys);
    }

    private void drawResponseWindowPopup()
    {
        ProjectionViewModel projectionSnapshot;
        string actorTextSnapshot;
        string viewerTextSnapshot;
        string defenseTypeKeyTextSnapshot;
        string responseNoLocalBlockBannerSnapshot;
        long? selectedDefenseCardIdSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorTextSnapshot = actorPlayerNumericIdText;
            viewerTextSnapshot = viewerPlayerNumericIdText;
            defenseTypeKeyTextSnapshot = defenseTypeKeyText;
            responseNoLocalBlockBannerSnapshot = responseNoLocalBlockBanner;
            selectedDefenseCardIdSnapshot = selectedDefenseCardId;
        }

        if (!HasRenderableResponseWindow(projectionSnapshot))
        {
            lock (stateLock)
            {
                responseNoLocalBlockBanner = string.Empty;
            }

            return;
        }

        var originalGuiColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = originalGuiColor;

        var popupWidth = 520f;
        var popupHeight = 440f;
        var popupRect = new Rect(
            (Screen.width - popupWidth) * 0.5f,
            Mathf.Max(24f, (Screen.height - popupHeight) * 0.5f),
            popupWidth,
            popupHeight);
        var popupScrollHeight = popupHeight - 34f;

        GUILayout.BeginArea(popupRect, "响应窗口", GUI.skin.window);
        responseWindowPopupScroll = GUILayout.BeginScrollView(
            responseWindowPopupScroll,
            GUILayout.Height(popupScrollHeight));
        GUILayout.Label($"响应窗口ID: {projectionSnapshot.interaction.responseWindowNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label($"当前响应者ID: {projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label($"观察者ID: {viewerTextSnapshot}");
        GUILayout.Label($"操作者ID: {actorTextSnapshot}");
        GUILayout.Label($"响应者数量: {projectionSnapshot.interaction.responseResponderCount}");
        GUILayout.Label($"窗口来源: {(string.IsNullOrWhiteSpace(projectionSnapshot.interaction.responseWindowOriginType) ? "(空)" : projectionSnapshot.interaction.responseWindowOriginType)}");
        GUILayout.Label($"待结算阶段: {(string.IsNullOrWhiteSpace(projectionSnapshot.interaction.pendingDamageResponseStageKey) ? "(空)" : projectionSnapshot.interaction.pendingDamageResponseStageKey)}");
        GUILayout.Label($"伤害值: （投影未提供）");
        GUILayout.Label($"待伤害类型: {(string.IsNullOrWhiteSpace(projectionSnapshot.interaction.pendingDamageTypeKey) ? "(空)" : projectionSnapshot.interaction.pendingDamageTypeKey)}");
        GUILayout.Label($"目标角色ID: {projectionSnapshot.interaction.pendingDamageTargetCharacterInstanceNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label($"防守玩家ID: {projectionSnapshot.interaction.pendingDamageDefenderPlayerNumericId?.ToString() ?? "(空)"}");
        var isActorParseSuccess = long.TryParse(actorTextSnapshot, out var parsedActorPlayerNumericId);
        var isViewerParseSuccess = long.TryParse(viewerTextSnapshot, out var parsedViewerPlayerNumericId);
        var responderPlayerNumericId = projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId;
        var isLocalResponder = IsLocalResponderForResponseWindow(projectionSnapshot);
        var isAwaitDefenseStage = IsAwaitDefenseStageForResponseWindow(projectionSnapshot);
        var isLegacyAwaitCounterStage = IsLegacyAwaitCounterStageForResponseWindow(projectionSnapshot);
        var isActorResponderMatch = isActorParseSuccess &&
                                    responderPlayerNumericId.HasValue &&
                                    parsedActorPlayerNumericId == responderPlayerNumericId.Value;
        var isActorViewerMatch = isActorParseSuccess &&
                                 isViewerParseSuccess &&
                                 parsedActorPlayerNumericId == parsedViewerPlayerNumericId;
        GUILayout.Label($"操作者是否匹配当前响应者: {isActorResponderMatch}");
        GUILayout.Label($"操作者是否匹配观察者: {isActorViewerMatch}");

        if (!string.IsNullOrWhiteSpace(responseNoLocalBlockBannerSnapshot))
        {
            var previousBackgroundColor = GUI.backgroundColor;
            var previousContentColor = GUI.contentColor;
            GUI.backgroundColor = new Color(0.78f, 0.1f, 0.1f, 1f);
            GUI.contentColor = Color.white;
            GUILayout.Box(responseNoLocalBlockBannerSnapshot, GUILayout.MinHeight(40f), GUILayout.ExpandWidth(true));
            GUI.backgroundColor = previousBackgroundColor;
            GUI.contentColor = previousContentColor;
        }

        GUILayout.Space(10f);

        var previousEnabled = GUI.enabled;
        if (!isAwaitDefenseStage)
        {
            if (isLegacyAwaitCounterStage)
            {
                GUILayout.Label("检测到旧 awaitCounter 阶段：当前正式联调不支持。");
                GUILayout.Label("应由 RuleCore 修复为 SubmitDefense 后直接结算并关闭响应窗口。");
            }
            else
            {
                var stageText = string.IsNullOrWhiteSpace(projectionSnapshot.interaction.pendingDamageResponseStageKey)
                    ? "(空)"
                    : projectionSnapshot.interaction.pendingDamageResponseStageKey;
                GUILayout.Label($"当前响应窗口阶段不是 awaitDefense（当前：{stageText}）。");
                GUILayout.Label("本地已禁用响应提交操作，避免误导。");
            }

            if (!isLocalResponder)
            {
                var responderText = responderPlayerNumericId?.ToString() ?? "(空)";
                GUILayout.Label($"等待 Player {responderText} 响应");
                GUILayout.Label("当前客户端不是响应者，只能观察。");
            }
        }
        else if (!isLocalResponder)
        {
            var responderText = responderPlayerNumericId?.ToString() ?? "(空)";
            GUILayout.Label($"等待 Player {responderText} 响应");
            GUILayout.Label("当前客户端不是响应者，只能观察。");
            GUILayout.Label("多人模式下已禁用“切换操作者”引导按钮。");
        }
        else
        {
            var hasValidSelectedDefenseCard = TryValidateSelectedDefenseCardInHand(
                projectionSnapshot,
                selectedDefenseCardIdSnapshot,
                out var selectedDefenseCardIdToSubmit,
                out var selectedDefenseCard,
                out var selectedDefenseCardFailureReason);
            var selectedDefenseDefinitionId = selectedDefenseCard?.definitionId ?? "(未知)";
            var selectedDefenseZoneKey = selectedDefenseCard?.zoneKey ?? "(未知)";
            var damageTypeKeyText = string.IsNullOrWhiteSpace(projectionSnapshot.interaction.pendingDamageTypeKey)
                ? "(空)"
                : projectionSnapshot.interaction.pendingDamageTypeKey;
            var hasResolvedDefenseTypeKey = TryResolveFormalDefenseTypeKey(
                selectedDefenseCard,
                defenseTypeKeyTextSnapshot,
                out var resolvedDefenseTypeKey,
                out var resolvedDefenseTypeKeySource);
            GUILayout.Label("你需要响应当前伤害。");
            GUILayout.Label($"已选防御牌ID: {selectedDefenseCardIdSnapshot?.ToString() ?? "(无)"}");
            GUILayout.Label($"已选防御牌定义ID: {selectedDefenseDefinitionId}");
            GUILayout.Label($"已选防御牌区域: {localizeZoneKeyText(selectedDefenseZoneKey)}");
            GUILayout.Label("已选防御牌防御类型: 未知（当前投影未提供）");
            GUILayout.Label("已选防御牌防御值: 未知（当前投影未提供）");
            GUILayout.Label($"防御类型匹配待伤害类型（{damageTypeKeyText}）: 未知（当前投影未提供）");
            GUILayout.Label(hasResolvedDefenseTypeKey
                ? $"防御类型: {resolvedDefenseTypeKey}（来源：{resolvedDefenseTypeKeySource}）"
                : $"防御类型: {defenseTypeKeyTextSnapshot}（未解析）");

            GUI.enabled = canSendRequest();
            if (GUILayout.Button("不防御，承受伤害", GUILayout.Height(34f)))
            {
                if (!TryValidateSubmitResponseNoActor(
                        projectionSnapshot,
                        actorTextSnapshot,
                        out _,
                        out var failureReason))
                {
                    lock (stateLock)
                    {
                        responseNoLocalBlockBanner = failureReason;
                    }

                    onLocalBlocked(failureReason);
                }
                else
                {
                    lock (stateLock)
                    {
                        responseNoLocalBlockBanner = string.Empty;
                    }

                    applyViewerAndActor();
                    sendTrackedAction("submitResponse", () => bridge?.SendSubmitResponseNo());
                }
            }

            if (GUILayout.Button("使用选中手牌防御", GUILayout.Height(30f)))
            {
                if (!TryValidateSubmitDefenseActor(projectionSnapshot, actorTextSnapshot, out var failureReason))
                {
                    onLocalBlocked(failureReason);
                }
                else if (!hasResolvedDefenseTypeKey)
                {
                    onLocalBlocked("本地拦截：防御类型不能为空，且无法从已选防御牌推断。");
                }
                else if (!hasValidSelectedDefenseCard)
                {
                    onLocalBlocked(selectedDefenseCardFailureReason);
                }
                else
                {
                    applyViewerAndActor();
                    sendTrackedAction(
                        "submitDefense",
                        () => bridge?.SendSubmitDefenseFormal(resolvedDefenseTypeKey, selectedDefenseCardIdToSubmit));
                }
            }
            GUI.enabled = false;
            GUILayout.Button("响应：是（当前调试面板暂不支持）", GUILayout.Height(30f));
        }

        GUI.enabled = previousEnabled;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void connect()
    {
        lock (stateLock)
        {
            connectionState = "connecting";
            latestError = string.Empty;
            localInterceptionBanner = string.Empty;
            traceCopyStatus = string.Empty;
            requestInFlight = false;
            pendingActionType = string.Empty;
            currentResponseActionType = string.Empty;
            pendingRequestId = null;
            lastDirectRequestId = null;
            lastPushRequestId = null;
            pushUpdateCount = 0;
            lastPushEventLogDelta = 0;
            lastFailedActionTypeForUi = string.Empty;
            lastFailedErrorCodeForUi = string.Empty;
            lastFailedErrorMessageForUi = string.Empty;
            lastFailedReasonKeyForUi = string.Empty;
            lastFailedRequestIdForUi = null;
            lastFailedResponseSourceForUi = string.Empty;
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
            if (requestInFlight)
            {
                var pendingRequestText = pendingRequestId?.ToString() ?? "(空)";
                localInterceptionBanner =
                    $"本地拦截：已有请求进行中（pendingRequestId={pendingRequestText}），已阻止重复发送 {localizeActionTypeText(actionType)}。";
                return;
            }

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

        tryContinueChecklistMacro("connection-state-changed");
    }

    private void onRawRequest(string rawJson)
    {
        lock (stateLock)
        {
            latestRawRequestJson = rawJson;
            pendingRequestId = tryParseRequestId(rawJson);
            requestInFlight = true;
        }
    }

    private void onRawResponse(string rawJson)
    {
        lock (stateLock)
        {
            latestRawResponseJson = rawJson;
            var responseRequestId = tryParseRequestId(rawJson);
            var isDirectResponse = IsDirectResponseForPendingRequest(
                requestInFlight,
                pendingRequestId,
                responseRequestId);

            if (isDirectResponse)
            {
                lastDirectRequestId = responseRequestId;
                currentResponseActionType = string.IsNullOrWhiteSpace(pendingActionType)
                    ? "(unknown)"
                    : pendingActionType;
                currentResponseOrigin = "direct";
                pendingActionType = string.Empty;
                pendingRequestId = null;
                requestInFlight = false;
            }
            else
            {
                if (responseRequestId.HasValue)
                {
                    lastPushRequestId = responseRequestId.Value;
                }

                pushUpdateCount++;
                currentResponseActionType = "pushUpdate";
                currentResponseOrigin = "push";
            }
        }
    }

    private void onSummaryUpdated(ServerResponseSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"执行成功: {summary.isSucceeded}");
        builder.AppendLine($"错误码: {summary.errorCode}");
        builder.AppendLine($"错误信息: {summary.errorMessage}");
        builder.AppendLine($"观察者玩家ID: {summary.viewerPlayerNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"当前阶段: {localizePhaseText(summary.currentPhase)}");
        builder.AppendLine($"当前玩家ID: {summary.currentPlayerNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"我的手牌数: {summary.myHandCount}");
        builder.AppendLine($"最近事件类型: {summary.recentEventTypeKey}");
        builder.AppendLine($"是否有输入上下文: {summary.hasInputContext}");
        builder.AppendLine($"输入上下文ID: {summary.inputContextNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"输入必需玩家ID: {summary.inputRequiredPlayerNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"输入选项数量: {summary.inputChoiceCount}");
        builder.AppendLine($"是否有响应窗口: {summary.hasResponseWindow}");
        builder.AppendLine($"响应窗口ID: {summary.responseWindowNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"当前响应者ID: {summary.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"}");
        builder.AppendLine($"响应者数量: {summary.responseResponderCount}");

        lock (stateLock)
        {
            summaryText = builder.ToString();
            if (!summary.isSucceeded)
            {
                lastFailedActionTypeForUi = string.IsNullOrWhiteSpace(currentResponseActionType)
                    ? "(unknown)"
                    : currentResponseActionType;
                lastFailedErrorCodeForUi = summary.errorCode ?? string.Empty;
                lastFailedErrorMessageForUi = summary.errorMessage ?? string.Empty;
                lastFailedRequestIdForUi = tryParseRequestId(latestRawResponseJson);
                lastFailedResponseSourceForUi = string.IsNullOrWhiteSpace(currentResponseOrigin)
                    ? "(unknown)"
                    : currentResponseOrigin;
                lastFailedReasonKeyForUi = TryExtractFailedReasonKeyFromRawResponse(latestRawResponseJson);
            }
        }
    }

    private void onProjectionUpdated(ProjectionViewModel projection)
    {
        lock (stateLock)
        {
            var previousProjection = latestProjection.deepClone();
            var responseActionType = currentResponseActionType;
            var responseOrigin = currentResponseOrigin;
            lastResponseOriginForUi = responseOrigin;
            currentResponseActionType = string.Empty;
            currentResponseOrigin = "unknown";
            var checklistModeSnapshot = checklistMode;

            latestProjection = projection.deepClone();
            syncShackleDiscardSelectionWithInputContextLocked(latestProjection);
            if (projection.isSucceeded)
            {
                localInterceptionBanner = string.Empty;
            }

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

            if (!string.IsNullOrWhiteSpace(responseActionType) &&
                responseActionType != "(unknown)" &&
                responseActionType != "pushUpdate")
            {
                flowChecklistRuntime.RecordProjectionResponse(
                    checklistModeSnapshot,
                    responseActionType,
                    latestProjection,
                    previousProjection,
                    playSelectionCleared,
                    summonSelectionCleared);
                var lastStepResult = flowChecklistRuntime.getLastStepResultText(checklistModeSnapshot);
                var currentStepIndexForMode = flowChecklistRuntime.getCurrentStepIndex(checklistModeSnapshot);
                var resolvedStepNumber = ResolveStepNumberFromResult(lastStepResult, currentStepIndexForMode);
                appendFlowTraceLine(
                    checklistModeSnapshot,
                    resolvedStepNumber,
                    responseActionType,
                    responseOrigin,
                    latestProjection,
                    previousProjection,
                    projection.isSucceeded,
                    lastStepResult);
            }

            if (string.Equals(responseOrigin, "push", StringComparison.Ordinal))
            {
                lastPushEventLogDelta = latestProjection.eventLog.Count - previousProjection.eventLog.Count;
            }
        }

        tryContinueChecklistMacro("projection-updated");
    }

    private void onLocalBlocked(string error)
    {
        lock (stateLock)
        {
            localInterceptionBanner = error;
        }

        onError(error);
    }

    private void appendFlowTraceLine(
        DebugChecklistMode mode,
        int stepNumber,
        string responseActionType,
        string responseOrigin,
        ProjectionViewModel projection,
        ProjectionViewModel previousProjection,
        bool isSucceeded,
        string lastStepResult)
    {
        ensureFlowTraceBuckets();
        var action = string.IsNullOrWhiteSpace(responseActionType) ? "(未知动作)" : localizeActionTypeText(responseActionType);
        var flowTag = localizeChecklistModeTag(mode);
        var eventDelta = projection.eventLog.Count - previousProjection.eventLog.Count;
        var flowTraceLine =
            $"[{flowTag}] step={stepNumber} actionType={responseActionType}({action}) | 来源={responseOrigin} | 成功={isSucceeded} | 错误码={projection.errorCode} | 错误信息={projection.errorMessage} | 阶段={localizePhaseText(projection.currentPhase)} | 玩家={projection.currentPlayerNumericId?.ToString() ?? "(空)"} | 手牌={projection.viewerHandCardCount} | 场上={projection.fieldCards.Count} | 召唤区={projection.summonZoneCards.Count} | 樱花饼区={projection.sakuraCakeCards.Count} | 放逐区={projection.gapZoneCards.Count} | 响应窗={projection.interaction.hasResponseWindow}/{projection.interaction.responseWindowNumericId?.ToString() ?? "(空)"} | 输入={projection.interaction.hasInputContext}/{projection.interaction.inputContextNumericId?.ToString() ?? "(空)"} | 事件数={projection.eventLog.Count} | 事件增量={eventDelta} | 最近步骤={localizeFlowNoteText(lastStepResult)}";

        flowTraceLinesByMode[mode].Add(flowTraceLine);
        while (flowTraceLinesByMode[mode].Count > MaxFlowTraceLines)
        {
            flowTraceLinesByMode[mode].RemoveAt(0);
        }
    }

    private void onError(string error)
    {
        lock (stateLock)
        {
            latestError = error;
            requestInFlight = false;
            pendingRequestId = null;
            if (!string.IsNullOrWhiteSpace(error) &&
                error.StartsWith("本地", StringComparison.Ordinal))
            {
                localInterceptionBanner = error;
            }
        }

        tryContinueChecklistMacro("error");
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

    private static long? tryParseRequestId(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            var envelope = JsonUtility.FromJson<RequestIdEnvelopeDto>(rawJson);
            if (envelope is null || envelope.requestId <= 0)
            {
                return null;
            }

            return envelope.requestId;
        }
        catch (ArgumentException)
        {
        }
        catch (FormatException)
        {
        }

        return null;
    }

    [Serializable]
    private sealed class RequestIdEnvelopeDto
    {
        public long requestId;
    }

    [Serializable]
    private sealed class FailedReasonProbeEnvelope
    {
        public FailedReasonProbeError? error;
    }

    [Serializable]
    private sealed class FailedReasonProbeError
    {
        public string failedReasonKey = string.Empty;
        public string failedReason = string.Empty;
        public string reasonKey = string.Empty;
    }

    private static string buildChecklistKeyProjectionSummary(ProjectionViewModel projection, DebugChecklistMode mode)
    {
        var sharedSummary =
            $"阶段={localizePhaseText(projection.currentPhase)} 当前玩家={projection.currentPlayerNumericId?.ToString() ?? "(空)"} 手牌={projection.viewerHandCardCount} 场上={projection.fieldCards.Count} 弃牌={projection.discardCount} 召唤区={projection.summonZoneCards.Count} 樱花饼区={projection.sakuraCakeCards.Count} 放逐区={projection.gapZoneCards.Count} 事件数={projection.eventLog.Count}";
        return mode switch
        {
            DebugChecklistMode.mainFlowA => sharedSummary,
            DebugChecklistMode.responseWindowB =>
                $"{sharedSummary} 响应窗={projection.interaction.hasResponseWindow} 响应窗ID={projection.interaction.responseWindowNumericId?.ToString() ?? "(空)"} 当前响应者={projection.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"}",
            DebugChecklistMode.inputContextC =>
                $"{sharedSummary} 输入上下文={projection.interaction.hasInputContext} 输入ID={projection.interaction.inputContextNumericId?.ToString() ?? "(空)"} 必需玩家={projection.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(空)"} 选项数量={projection.interaction.inputChoiceCount}",
            _ => sharedSummary,
        };
    }

    private static string localizeConnectionStateText(string state)
    {
        return state switch
        {
            "connected" => "已连接",
            "connecting" => "连接中",
            "disconnected" => "已断开",
            _ when state.StartsWith("disconnected", StringComparison.OrdinalIgnoreCase) => $"已断开（{state}）",
            _ => state,
        };
    }

    private static string localizeChecklistModeText(DebugChecklistMode mode)
    {
        return mode switch
        {
            DebugChecklistMode.mainFlowA => "A：主流程",
            DebugChecklistMode.responseWindowB => "B：响应窗续跑",
            DebugChecklistMode.inputContextC => "C：输入续跑",
            _ => mode.ToString(),
        };
    }

    private static string localizeChecklistModeTag(DebugChecklistMode mode)
    {
        return mode switch
        {
            DebugChecklistMode.mainFlowA => "A",
            DebugChecklistMode.responseWindowB => "B",
            DebugChecklistMode.inputContextC => "C",
            _ => "?",
        };
    }

    private static string localizePhaseText(string phaseKey)
    {
        var phaseName = phaseKey switch
        {
            "start" => "开始阶段",
            "action" => "行动阶段",
            "summon" => "召唤阶段",
            "end" => "结束阶段",
            _ => "未知阶段",
        };

        return string.IsNullOrWhiteSpace(phaseKey)
            ? phaseName
            : $"{phaseKey}（{phaseName}）";
    }

    private static string localizeZoneKeyText(string zoneKey)
    {
        var zoneName = zoneKey switch
        {
            "hand" => "手牌区",
            "field" => "场地区",
            "discard" => "弃牌区",
            "deck" => "牌库区",
            "summonZone" => "召唤区",
            "sakuraCakeDeck" => "樱花饼区",
            "publicTreasureDeck" => "公共宝具牌堆",
            "gapZone" => "间隙区",
            _ => zoneKey,
        };

        return string.IsNullOrWhiteSpace(zoneKey) ? zoneName : $"{zoneKey}/{zoneName}";
    }

    private static string localizeStatusKey(string statusKey)
    {
        return statusKey switch
        {
            "Barrier" => "Barrier/结界",
            "Seal" => "Seal/封印",
            "Shackle" => "Shackle/禁锢",
            "Silence" => "Silence/沉默",
            "Charm" => "Charm/破魔",
            "Penetrate" => "Penetrate/穿透",
            _ => statusKey,
        };
    }

    private static string localizeStepDisplayNameText(string displayName)
    {
        return displayName switch
        {
            "Connect" => "连接",
            "EnterAction" => "进入行动",
            "Draw" => "抽牌",
            "Play Selected" => "打出已选手牌",
            "EnterSummon" => "进入召唤",
            "Summon Selected" => "召唤已选卡",
            "End Turn" => "回合结束",
            "Verify Next Player" => "验证切到下一位玩家",
            "Next Player Action Ready" => "验证下一位玩家可行动",
            "EnterEnd" => "进入结束",
            "Debug Open DamageWindow" => "调试打开伤害响应窗",
            "ResponseWindow Valid" => "响应窗有效",
            "Current Responder Valid" => "当前响应者有效",
            "Submit Response No" => "提交不响应",
            "Submit Response Succeeded" => "提交不响应成功",
            "ResponseWindow Closed" => "响应窗已关闭",
            "DamageResolved Observed" => "检测到伤害结算",
            "HpChanged Observed" => "检测到生命变化",
            "InputContext Opened" => "输入上下文已打开",
            "InputContext Detail Valid" => "输入上下文细节有效",
            "Submit InputChoice" => "提交输入选择",
            "InputChoice Succeeded" => "输入选择提交成功",
            "InputContext Closed/Advanced" => "输入上下文已关闭或推进",
            "InputContext Event Observed" => "检测到输入上下文事件",
            _ => displayName,
        };
    }

    private static string localizeFlowStatusText(DebugFlowStepStatus status)
    {
        return status switch
        {
            DebugFlowStepStatus.pending => "待执行",
            DebugFlowStepStatus.passed => "通过",
            DebugFlowStepStatus.failed => "失败",
            _ => status.ToString(),
        };
    }

    private static string localizeRecommendedNextStepText(string recommendedNextStep)
    {
        if (string.Equals(recommendedNextStep, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return "已完成";
        }

        if (string.IsNullOrWhiteSpace(recommendedNextStep))
        {
            return "(无)";
        }

        var dotIndex = recommendedNextStep.IndexOf('.');
        if (dotIndex > 0 && dotIndex < recommendedNextStep.Length - 1)
        {
            var prefix = recommendedNextStep.Substring(0, dotIndex + 1);
            var suffix = recommendedNextStep.Substring(dotIndex + 1).Trim();
            return $"{prefix} {localizeStepDisplayNameText(suffix)}";
        }

        return localizeStepDisplayNameText(recommendedNextStep);
    }

    private static string localizeFlowNoteText(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return string.Empty;
        }

        return note
            .Replace("No flow step executed yet.", "尚未执行流程步骤。", StringComparison.Ordinal)
            .Replace("flow already completed", "流程已完成", StringComparison.Ordinal)
            .Replace("socket connected", "Socket 已连接", StringComparison.Ordinal)
            .Replace("request failed", "请求失败", StringComparison.Ordinal)
            .Replace("expected action=", "期望动作=", StringComparison.Ordinal)
            .Replace("actual=", "实际动作=", StringComparison.Ordinal)
            .Replace("phase is action", "阶段为行动阶段", StringComparison.Ordinal)
            .Replace("phase is not action", "阶段不是行动阶段", StringComparison.Ordinal)
            .Replace("hand count increased", "手牌数量增加", StringComparison.Ordinal)
            .Replace("hand count did not increase", "手牌数量未增加", StringComparison.Ordinal)
            .Replace("played selected hand card was not cleared", "已打出的手牌选择未被清理", StringComparison.Ordinal)
            .Replace("hand count decreased", "手牌数量减少", StringComparison.Ordinal)
            .Replace("field count increased", "场上数量增加", StringComparison.Ordinal)
            .Replace("play result not observed in hand/field", "未观察到打出后的手牌/场上变化", StringComparison.Ordinal)
            .Replace("phase is summon", "阶段为召唤阶段", StringComparison.Ordinal)
            .Replace("phase is not summon", "阶段不是召唤阶段", StringComparison.Ordinal)
            .Replace("summoned selected card was not cleared", "已召唤的卡牌选择未被清理", StringComparison.Ordinal)
            .Replace("summon zone count decreased", "召唤区数量减少", StringComparison.Ordinal)
            .Replace("summon zone count did not decrease", "召唤区数量未减少", StringComparison.Ordinal)
            .Replace("phase is end", "阶段为结束阶段", StringComparison.Ordinal)
            .Replace("phase is not end", "阶段不是结束阶段", StringComparison.Ordinal)
            .Replace("turn number did not advance", "回合数未推进", StringComparison.Ordinal)
            .Replace("current player did not switch", "当前玩家未切换", StringComparison.Ordinal)
            .Replace("turn advanced and player switched", "回合推进且当前玩家已切换", StringComparison.Ordinal)
            .Replace("current player mismatch after next turn", "下一回合后当前玩家不匹配", StringComparison.Ordinal)
            .Replace("next player entered action", "下一位玩家已进入行动阶段", StringComparison.Ordinal)
            .Replace("debug window opened request returned", "调试开窗请求已返回", StringComparison.Ordinal)
            .Replace("hasResponseWindow is true", "响应窗口存在", StringComparison.Ordinal)
            .Replace("hasResponseWindow is false", "响应窗口不存在", StringComparison.Ordinal)
            .Replace("responseWindow id and responder are valid", "响应窗口ID与当前响应者有效", StringComparison.Ordinal)
            .Replace("responseWindowNumericId is invalid", "响应窗口ID无效", StringComparison.Ordinal)
            .Replace("currentResponderPlayerNumericId is invalid", "当前响应者ID无效", StringComparison.Ordinal)
            .Replace("waiting for submitResponse", "等待提交 submitResponse", StringComparison.Ordinal)
            .Replace("submitResponse returned", "submitResponse 请求已返回", StringComparison.Ordinal)
            .Replace("submitResponse succeeded", "submitResponse 成功", StringComparison.Ordinal)
            .Replace("responseWindow still open", "响应窗口仍然打开", StringComparison.Ordinal)
            .Replace("responseWindow closed", "响应窗口已关闭", StringComparison.Ordinal)
            .Replace("damageResolved observed", "已检测到 damageResolved", StringComparison.Ordinal)
            .Replace("damageResolved not found in eventLog", "eventLog 中未检测到 damageResolved", StringComparison.Ordinal)
            .Replace("hpChanged observed", "已检测到 hpChanged", StringComparison.Ordinal)
            .Replace("hpChanged not found in eventLog", "eventLog 中未检测到 hpChanged", StringComparison.Ordinal)
            .Replace("entered action phase", "已进入行动阶段", StringComparison.Ordinal)
            .Replace("draw succeeded", "抽牌成功", StringComparison.Ordinal)
            .Replace("draw succeeded and phase is action", "抽牌成功且阶段为行动阶段", StringComparison.Ordinal)
            .Replace("hand count did not increase after draw", "抽牌后手牌数量未增加", StringComparison.Ordinal)
            .Replace("enterEndPhase returned", "enterEndPhase 请求已返回", StringComparison.Ordinal)
            .Replace("end turn auto-advanced to next player action", "回合结束后已自动推进到下一位玩家行动阶段", StringComparison.Ordinal)
            .Replace("end turn opened interaction", "回合结束后打开了交互窗口", StringComparison.Ordinal)
            .Replace("end turn returned but next state requires observation", "回合结束请求已返回，等待后续状态观察", StringComparison.Ordinal)
            .Replace("turn number did not advance or current player did not switch", "回合数未推进或当前玩家未切换", StringComparison.Ordinal)
            .Replace("next player action is ready", "下一位玩家已可行动", StringComparison.Ordinal)
            .Replace("phase is not action or current player mismatch", "阶段不是行动或当前玩家不匹配", StringComparison.Ordinal)
            .Replace("waiting for input/response continuation before player switch", "等待输入/响应续跑后再切换玩家", StringComparison.Ordinal)
            .Replace("waiting for input/response continuation before action phase", "等待输入/响应续跑后再进入行动阶段", StringComparison.Ordinal)
            .Replace("waiting for connected", "等待连接完成", StringComparison.Ordinal)
            .Replace("inputContext opened", "输入上下文已打开", StringComparison.Ordinal)
            .Replace("inputContext detail is valid", "输入上下文细节有效", StringComparison.Ordinal)
            .Replace("inputContextNumericId is invalid", "输入上下文ID无效", StringComparison.Ordinal)
            .Replace("requiredPlayerNumericId is invalid", "必需玩家ID无效", StringComparison.Ordinal)
            .Replace("choiceKeys are empty", "可选项为空", StringComparison.Ordinal)
            .Replace("waiting for submitInputChoice", "等待提交 submitInputChoice", StringComparison.Ordinal)
            .Replace("submitInputChoice returned", "submitInputChoice 请求已返回", StringComparison.Ordinal)
            .Replace("submitInputChoice succeeded", "submitInputChoice 成功", StringComparison.Ordinal)
            .Replace("inputContext closed", "输入上下文已关闭", StringComparison.Ordinal)
            .Replace("inputContext advanced", "输入上下文已推进", StringComparison.Ordinal)
            .Replace("inputContext neither closed nor advanced", "输入上下文既未关闭也未推进", StringComparison.Ordinal)
            .Replace("inputContext event observed", "已检测到输入上下文相关事件", StringComparison.Ordinal)
            .Replace("inputContext-related events not observed", "未检测到输入上下文相关事件", StringComparison.Ordinal)
            .Replace("unsupported step", "不支持的流程步骤", StringComparison.Ordinal);
    }

    private static string localizeActionTypeText(string actionType)
    {
        return actionType switch
        {
            "enterActionPhase" => "进入行动阶段",
            "drawOneCard" => "抽1张牌",
            "playTreasureCard" => "打出宝具",
            "enterSummonPhase" => "进入召唤阶段",
            "summonTreasureCard" => "召唤宝具",
            "enterEndPhase" => "进入结束阶段",
            "startNextTurn" => "开始下一回合",
            "submitInputChoice" => "提交输入选择",
            "submitResponse" => "提交响应",
            "submitDefense" => "提交防御",
            "debugOpenDamageResponseWindow" => "调试：打开伤害响应窗",
            "debugResetMatch" => "调试：重开本局",
            "debugMoveTreasureToHandByDefinition" => "调试：按定义移入手牌",
            "debugPutTreasureOnTopByDefinition" => "调试：按定义置入牌堆顶部",
            _ => actionType,
        };
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

    private static ProjectionTeamSummaryViewModel? findTeamSummaryById(
        ProjectionViewModel projection,
        long teamNumericId)
    {
        for (var index = 0; index < projection.teamSummaries.Count; index++)
        {
            var teamSummary = projection.teamSummaries[index];
            if (teamSummary.teamNumericId == teamNumericId)
            {
                return teamSummary;
            }
        }

        return null;
    }
}

public static class DebugCardTextureResolver
{
    public const string CardBackAssetPath = "Assets/Art/Cards/Illustrations/CardBack.png";

    private const string BasicRelicFolderPath = "Assets/Art/Cards/Illustrations/Relics/Basic";
    private const string SummonRelicFolderPath = "Assets/Art/Cards/Illustrations/Relics/Summon";
    private const string SakuraRelicFolderPath = "Assets/Art/Cards/Illustrations/Relics/Sakuracake";
    private const string AnomalyFolderPath = "Assets/Art/Cards/Illustrations/Anomaly";

    private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private static readonly HashSet<string> missingTextureKeys = new HashSet<string>();

    public static string ResolveAssetPathForDefinition(string definitionId)
    {
        var normalizedDefinitionId = normalizeDefinitionId(definitionId);
        if (string.Equals(normalizedDefinitionId, "STARTER:KOURINDOUCOUPON", StringComparison.Ordinal))
        {
            return $"{BasicRelicFolderPath}/T001B.png";
        }

        if (string.Equals(normalizedDefinitionId, "STARTER:MAGICCIRCUIT", StringComparison.Ordinal))
        {
            return $"{BasicRelicFolderPath}/T002B.png";
        }

        if (isBasicTreasureDefinitionId(normalizedDefinitionId))
        {
            return $"{BasicRelicFolderPath}/{normalizedDefinitionId}.png";
        }

        if (isTreasureDefinitionId(normalizedDefinitionId))
        {
            return $"{SummonRelicFolderPath}/{normalizedDefinitionId}.png";
        }

        if (string.Equals(normalizedDefinitionId, "S001", StringComparison.Ordinal))
        {
            return $"{SakuraRelicFolderPath}/{normalizedDefinitionId}.png";
        }

        if (isAnomalyDefinitionId(normalizedDefinitionId))
        {
            return $"{AnomalyFolderPath}/{normalizedDefinitionId}.png";
        }

        return CardBackAssetPath;
    }

    public static Texture2D GetTextureForDefinition(string definitionId)
    {
        var normalizedDefinitionId = normalizeDefinitionId(definitionId);
        if (textureCache.TryGetValue(normalizedDefinitionId, out var cachedTexture))
        {
            return cachedTexture;
        }

        if (missingTextureKeys.Contains(normalizedDefinitionId))
        {
            return null;
        }

        var resolvedAssetPath = ResolveAssetPathForDefinition(normalizedDefinitionId);
        var resolvedTexture = loadTextureAtPath(resolvedAssetPath);
        if (resolvedTexture == null && !string.Equals(resolvedAssetPath, CardBackAssetPath, StringComparison.Ordinal))
        {
            resolvedTexture = loadTextureAtPath(CardBackAssetPath);
        }

        if (resolvedTexture == null)
        {
            missingTextureKeys.Add(normalizedDefinitionId);
            return null;
        }

        textureCache[normalizedDefinitionId] = resolvedTexture;
        return resolvedTexture;
    }

    public static void ClearCacheForTests()
    {
        textureCache.Clear();
        missingTextureKeys.Clear();
    }

    private static string normalizeDefinitionId(string definitionId)
    {
        return string.IsNullOrWhiteSpace(definitionId)
            ? string.Empty
            : definitionId.Trim().ToUpperInvariant();
    }

    private static bool isTreasureDefinitionId(string definitionId)
    {
        if (definitionId.Length != 4 || definitionId[0] != 'T')
        {
            return false;
        }

        return char.IsDigit(definitionId[1]) &&
               char.IsDigit(definitionId[2]) &&
               char.IsDigit(definitionId[3]);
    }

    private static bool isBasicTreasureDefinitionId(string definitionId)
    {
        if (definitionId.Length != 5 || definitionId[0] != 'T' || definitionId[4] != 'B')
        {
            return false;
        }

        return char.IsDigit(definitionId[1]) &&
               char.IsDigit(definitionId[2]) &&
               char.IsDigit(definitionId[3]);
    }

    private static bool isAnomalyDefinitionId(string definitionId)
    {
        if (definitionId.Length != 4 || definitionId[0] != 'A')
        {
            return false;
        }

        return char.IsDigit(definitionId[1]) &&
               char.IsDigit(definitionId[2]) &&
               char.IsDigit(definitionId[3]);
    }

    private static Texture2D loadTextureAtPath(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
#else
        _ = assetPath;
        return null;
#endif
    }
}
}
