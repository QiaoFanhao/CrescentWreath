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
    [SerializeField] private string inputChoiceKeyManualText = string.Empty;

    private const int MaxFlowTraceLines = 40;

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

    private long? selectedHandCardId;
    private long? selectedSummonCardId;
    private long? selectedSakuraCakeCardId;
    private long? selectedDefenseCardId;

    private long? lastPlayedSelectedHandCardId;
    private long? lastSummonedSelectedCardId;

    private string pendingActionType = string.Empty;
    private string currentResponseActionType = string.Empty;
    private string responseNoLocalBlockBanner = string.Empty;
    private DebugChecklistMode checklistMode = DebugChecklistMode.mainFlowA;

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

    public static bool HasRenderableResponseWindow(ProjectionViewModel projection)
    {
        return projection.interaction.hasResponseWindow &&
               projection.interaction.responseWindowNumericId.HasValue &&
               projection.interaction.responseWindowNumericId.Value > 0;
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
        drawPhaseActionSection();
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
        GUILayout.EndHorizontal();
    }

    private void drawPhaseActionSection()
    {
        var canSend = canSendRequest();
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("进入行动", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterActionPhase", () => bridge?.SendEnterActionPhase());
        }

        if (GUILayout.Button("进入召唤", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterSummonPhase", () => bridge?.SendEnterSummonPhase());
        }

        if (GUILayout.Button("进入结束", GUILayout.Width(120f)))
        {
            applyViewerAndActor();
            sendTrackedAction("enterEndPhase", () => bridge?.SendEnterEndPhase());
        }

        if (GUILayout.Button("开始下一回合", GUILayout.Width(140f)))
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
                onError("打出失败：需要已选手牌，或输入合法卡牌ID。");
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
                onError("打出已选手牌失败：尚未选择手牌。");
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
        GUI.enabled = canSend;
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
                onError("召唤失败：需要已选召唤卡，或输入合法卡牌ID。");
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
                onError("召唤已选卡失败：尚未选择召唤区卡牌。");
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
                onError("召唤已选樱花饼失败：尚未选择樱花饼卡牌。");
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
        if (GUILayout.Button("防御：固定减1", GUILayout.Width(140f)))
        {
            applyViewerAndActor();
            sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFixedReduce1());
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
                onError("设置防御牌失败：尚未选择手牌。");
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
            if (string.IsNullOrWhiteSpace(defenseTypeKeyText))
            {
                onError("防御类型不能为空。");
            }
            else if (TryResolveSelectedOrManualCardId(selectedDefenseCardIdSnapshot, defenseCardInstanceNumericIdText, out var defenseCardInstanceId))
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(defenseTypeKeyText, defenseCardInstanceId));
            }
            else
            {
                onError("正式防御失败：需要已选防御牌，或输入合法防御卡牌ID。");
            }
        }

        GUI.enabled = canDefenseSelected;
        if (GUILayout.Button("防御已选卡", GUILayout.Width(130f)))
        {
            if (string.IsNullOrWhiteSpace(defenseTypeKeyText))
            {
                onError("防御类型不能为空。");
            }
            else if (TryResolveSelectedCardId(selectedDefenseCardIdSnapshot, out var selectedCardId))
            {
                applyViewerAndActor();
                sendTrackedAction("submitDefense", () => bridge?.SendSubmitDefenseFormal(defenseTypeKeyText, selectedCardId));
            }
            else
            {
                onError("防御已选卡失败：尚未选择防御牌。");
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
        projectionScroll = GUILayout.BeginScrollView(projectionScroll, GUILayout.Height(320f));
        GUILayout.Label($"回合数: {projectionSnapshot.turnNumber}");
        GUILayout.Label($"当前阶段: {localizePhaseText(projectionSnapshot.currentPhase)}");
        GUILayout.Label($"当前玩家ID: {projectionSnapshot.currentPlayerNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label(
            $"自身资源: 灵力={projectionSnapshot.mana}, 技能点={projectionSnapshot.skillPoint}, 灵脉预览={projectionSnapshot.sigilPreview}, 锁定符卡={projectionSnapshot.lockedSigil?.ToString() ?? "(空)"}");
        GUILayout.Label($"自身弃牌数: {projectionSnapshot.discardCount}");
        GUILayout.Label($"自身手牌数: {projectionSnapshot.viewerHandCardCount}");

        GUILayout.Label($"已选手牌: {selectedHandCardIdSnapshot?.ToString() ?? "(无)"}");
        drawSelectableCardList("自己的手牌", projectionSnapshot.handCards, selectedHandCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedHandCardId = cardId;
            }
        });

        GUILayout.Label("自己的场上牌");
        drawFieldCardList(projectionSnapshot.fieldCards);

        GUILayout.Label($"已选召唤区卡牌: {selectedSummonCardIdSnapshot?.ToString() ?? "(无)"}");
        drawSelectableCardList("召唤区卡牌", projectionSnapshot.summonZoneCards, selectedSummonCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedSummonCardId = cardId;
            }
        });

        GUILayout.Label($"已选樱花饼卡牌: {selectedSakuraCakeCardIdSnapshot?.ToString() ?? "(无)"}");
        drawSelectableCardList("樱花饼区卡牌", projectionSnapshot.sakuraCakeCards, selectedSakuraCakeCardIdSnapshot, cardId =>
        {
            lock (stateLock)
            {
                selectedSakuraCakeCardId = cardId;
            }
        });

        GUILayout.Label($"已选防御牌: {selectedDefenseCardIdSnapshot?.ToString() ?? "(无)"}");

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

    private void drawFlowChecklistSection()
    {
        List<DebugFlowStepState> stepStates;
        string recommendedNextStep;
        string lastStepResult;
        string keyProjectionSummary;
        List<string> flowTraceLinesSnapshot;
        DebugChecklistMode checklistModeSnapshot;

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
        }

        GUILayout.Label("流程检查");
        drawChecklistModeSwitcher();
        GUILayout.Label($"当前清单模式: {localizeChecklistModeText(checklistModeSnapshot)}");
        GUILayout.Label($"推荐下一步: {localizeRecommendedNextStepText(recommendedNextStep)}");
        GUILayout.Label($"最近一步结果: {localizeFlowNoteText(lastStepResult)}");
        GUILayout.Label($"关键字段摘要: {keyProjectionSummary}");

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

    private void drawSelectableCardList(
        string title,
        List<ProjectionCardViewModel> cards,
        long? selectedCardId,
        Action<long> onSelect)
    {
        GUILayout.Label(title);
        if (cards.Count == 0)
        {
            GUILayout.Label("(无)");
            return;
        }

        var previousEnabled = GUI.enabled;
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var isSelected = selectedCardId.HasValue && selectedCardId.Value == card.cardInstanceNumericId;
            GUI.enabled = true;
            var labelPrefix = isSelected ? "[已选] " : string.Empty;
            var label = $"{labelPrefix}{card.cardInstanceNumericId} : {card.definitionId} ({localizeZoneKeyText(card.zoneKey)})";
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
            GUILayout.Label("(无)");
            return;
        }

        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var label = $"{card.cardInstanceNumericId} : {card.definitionId} ({localizeZoneKeyText(card.zoneKey)})";
            if (GUILayout.Button(label, GUILayout.Height(24f)))
            {
                onError($"场上牌点击：{card.cardInstanceNumericId}（仅展示，不可操作）。");
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

        GUILayout.Label("结果摘要");
        GUILayout.TextArea(summarySnapshot, GUILayout.Height(85f));

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
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorTextSnapshot = actorPlayerNumericIdText;
        }

        GUILayout.Label("输入上下文");
        GUILayout.Label(
            $"输入上下文ID: {projectionSnapshot.interaction.inputContextNumericId?.ToString() ?? "(空)"} 必需玩家: {projectionSnapshot.interaction.inputRequiredPlayerNumericId?.ToString() ?? "(空)"} 选项数量: {projectionSnapshot.interaction.inputChoiceCount}");
        GUILayout.Label(
            $"输入类型: {projectionSnapshot.interaction.inputTypeKey} 上下文键: {projectionSnapshot.interaction.contextKey}");

        var hasRenderableInputContext = projectionSnapshot.interaction.hasInputContext &&
                                        projectionSnapshot.interaction.inputContextNumericId.HasValue &&
                                        projectionSnapshot.interaction.inputContextNumericId.Value > 0;

        if (!hasRenderableInputContext)
        {
            GUILayout.Label("(当前无输入上下文)");
            return;
        }

        if (projectionSnapshot.interaction.inputRequiredPlayerNumericId.HasValue &&
            long.TryParse(actorTextSnapshot, out var actorPlayerNumericId) &&
            actorPlayerNumericId != projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value)
        {
            GUILayout.Label(
                $"警告：操作者ID（{actorPlayerNumericId}）与必需玩家ID（{projectionSnapshot.interaction.inputRequiredPlayerNumericId.Value}）不一致。");
        }

        var canSend = canSendRequest();
        var previousEnabled = GUI.enabled;
        GUI.enabled = canSend;

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
                if (GUILayout.Button($"选项：{capturedChoiceKey}", GUILayout.Height(28f)))
                {
                    applyViewerAndActor();
                    sendTrackedAction("submitInputChoice", () => bridge?.SendSubmitInputChoice(capturedChoiceKey));
                }
            }
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("手动输入 choiceKey", GUILayout.Width(115f));
        inputChoiceKeyManualText = GUILayout.TextField(inputChoiceKeyManualText, GUILayout.Width(220f));
        if (GUILayout.Button("提交输入选择", GUILayout.Width(150f)))
        {
            if (string.IsNullOrWhiteSpace(inputChoiceKeyManualText))
            {
                onError("提交输入选择失败：choiceKey 不能为空。");
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

    private void drawResponseWindowPopup()
    {
        ProjectionViewModel projectionSnapshot;
        string actorTextSnapshot;
        string responseNoLocalBlockBannerSnapshot;
        lock (stateLock)
        {
            projectionSnapshot = latestProjection.deepClone();
            actorTextSnapshot = actorPlayerNumericIdText;
            responseNoLocalBlockBannerSnapshot = responseNoLocalBlockBanner;
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
        var popupHeight = 220f;
        var popupRect = new Rect(
            (Screen.width - popupWidth) * 0.5f,
            Mathf.Max(24f, (Screen.height - popupHeight) * 0.5f),
            popupWidth,
            popupHeight);

        GUILayout.BeginArea(popupRect, "响应窗口", GUI.skin.window);
        GUILayout.Label($"响应窗口ID: {projectionSnapshot.interaction.responseWindowNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label($"当前响应者ID: {projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId?.ToString() ?? "(空)"}");
        GUILayout.Label($"操作者ID: {actorTextSnapshot}");
        GUILayout.Label($"响应者数量: {projectionSnapshot.interaction.responseResponderCount}");
        var isActorParseSuccess = long.TryParse(actorTextSnapshot, out var parsedActorPlayerNumericId);
        var responderPlayerNumericId = projectionSnapshot.interaction.responseCurrentResponderPlayerNumericId;
        var isActorResponderMatch = isActorParseSuccess &&
                                    responderPlayerNumericId.HasValue &&
                                    parsedActorPlayerNumericId == responderPlayerNumericId.Value;
        GUILayout.Label($"操作者是否匹配当前响应者: {isActorResponderMatch}");

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
        GUI.enabled = canSendRequest();
        if (GUILayout.Button("将操作者切到当前响应者", GUILayout.Height(30f)))
        {
            if (responderPlayerNumericId.HasValue && responderPlayerNumericId.Value > 0)
            {
                actorPlayerNumericIdText = responderPlayerNumericId.Value.ToString();
                applyViewerAndActor();
                lock (stateLock)
                {
                    responseNoLocalBlockBanner = string.Empty;
                }
            }
            else
            {
                onError("切换操作者失败：当前响应者ID缺失。");
            }
        }

        GUILayout.Space(4f);
        if (GUILayout.Button("响应：不响应", GUILayout.Height(34f)))
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

                onError(failureReason);
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

        if (GUILayout.Button("以当前响应者提交“不响应”", GUILayout.Height(30f)))
        {
            if (responderPlayerNumericId.HasValue && responderPlayerNumericId.Value > 0)
            {
                actorPlayerNumericIdText = responderPlayerNumericId.Value.ToString();
                applyViewerAndActor();
                lock (stateLock)
                {
                    responseNoLocalBlockBanner = string.Empty;
                }

                sendTrackedAction("submitResponse", () => bridge?.SendSubmitResponseNoAsActor(responderPlayerNumericId.Value));
            }
            else
            {
                onError("以当前响应者提交失败：当前响应者ID缺失。");
            }
        }

        GUI.enabled = false;
        GUILayout.Button("响应：是（08A 暂不支持）", GUILayout.Height(30f));
        GUI.enabled = previousEnabled;

        GUILayout.EndArea();
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
        }
    }

    private void onProjectionUpdated(ProjectionViewModel projection)
    {
        lock (stateLock)
        {
            var previousProjection = latestProjection.deepClone();
            var responseActionType = currentResponseActionType;
            currentResponseActionType = string.Empty;
            var checklistModeSnapshot = checklistMode;

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
                    checklistModeSnapshot,
                    responseActionType,
                    latestProjection,
                    previousProjection,
                    playSelectionCleared,
                    summonSelectionCleared);
                appendFlowTraceLine(
                    checklistModeSnapshot,
                    responseActionType,
                    latestProjection,
                    previousProjection,
                    projection.isSucceeded);
            }
        }
    }

    private void appendFlowTraceLine(
        DebugChecklistMode mode,
        string responseActionType,
        ProjectionViewModel projection,
        ProjectionViewModel previousProjection,
        bool isSucceeded)
    {
        ensureFlowTraceBuckets();
        var action = string.IsNullOrWhiteSpace(responseActionType) ? "(未知动作)" : localizeActionTypeText(responseActionType);
        var flowTag = localizeChecklistModeTag(mode);
        var eventDelta = projection.eventLog.Count - previousProjection.eventLog.Count;
        var flowTraceLine =
            $"[{flowTag}] {action} | 成功={isSucceeded} | 错误码={projection.errorCode} | 错误信息={projection.errorMessage} | 阶段={localizePhaseText(projection.currentPhase)} | 玩家={projection.currentPlayerNumericId?.ToString() ?? "(空)"} | 手牌={projection.viewerHandCardCount} | 场上={projection.fieldCards.Count} | 召唤区={projection.summonZoneCards.Count} | 樱花饼区={projection.sakuraCakeCards.Count} | 响应窗={projection.interaction.hasResponseWindow}/{projection.interaction.responseWindowNumericId?.ToString() ?? "(空)"} | 输入={projection.interaction.hasInputContext}/{projection.interaction.inputContextNumericId?.ToString() ?? "(空)"} | 事件数={projection.eventLog.Count} | 事件增量={eventDelta}";

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

    private static string buildChecklistKeyProjectionSummary(ProjectionViewModel projection, DebugChecklistMode mode)
    {
        var sharedSummary =
            $"阶段={localizePhaseText(projection.currentPhase)} 当前玩家={projection.currentPlayerNumericId?.ToString() ?? "(空)"} 手牌={projection.viewerHandCardCount} 场上={projection.fieldCards.Count} 弃牌={projection.discardCount} 召唤区={projection.summonZoneCards.Count} 樱花饼区={projection.sakuraCakeCards.Count} 事件数={projection.eventLog.Count}";
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
            "EnterEnd" => "进入结束",
            "StartNextTurn" => "开始下一回合",
            "Next Player EnterAction" => "下一位玩家进入行动",
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
            .Replace("hand count did not increase after draw", "抽牌后手牌数量未增加", StringComparison.Ordinal)
            .Replace("enterEndPhase returned", "enterEndPhase 请求已返回", StringComparison.Ordinal)
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
}
}
