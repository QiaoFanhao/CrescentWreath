using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class GameClientNetworkController : MonoBehaviour
{
    private const string DefaultWsUrl = "ws://127.0.0.1:18080/ws";

    [SerializeField]
    private ProjectionViewState? projectionViewState;

    private ServerBridge? bridge;
    private bool requestInFlight;
    private long? pendingRequestId;
    private string connectionState = "disconnected";
    private string lastError = string.Empty;
    private string wsUrl = DefaultWsUrl;
    private long viewerPlayerNumericId = 1;

    public bool IsConnected => bridge?.isConnected == true;
    public bool IsRequestInFlight => requestInFlight;
    public string ConnectionState => connectionState;
    public string LastError => lastError;
    public string WsUrl => wsUrl;
    public long ViewerPlayerNumericId => viewerPlayerNumericId;

    public event Action? StateChanged;

    public void Configure(ProjectionViewState newProjectionViewState)
    {
        projectionViewState = newProjectionViewState;
    }

    private void OnDestroy()
    {
        disposeBridge();
    }

    public bool Connect(string newWsUrl, string viewerPlayerNumericIdText)
    {
        if (IsConnected)
        {
            setError("已经连接。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newWsUrl))
        {
            setError("WebSocket 地址不能为空。");
            return false;
        }

        if (!long.TryParse(viewerPlayerNumericIdText, out var parsedViewerPlayerNumericId) ||
            parsedViewerPlayerNumericId <= 0)
        {
            setError("玩家 ID 必须是正整数。");
            return false;
        }

        wsUrl = newWsUrl.Trim();
        viewerPlayerNumericId = parsedViewerPlayerNumericId;
        requestInFlight = false;
        pendingRequestId = null;
        lastError = string.Empty;
        connectionState = "connecting";
        ensureBridge();
        bridge!.viewerPlayerNumericId = viewerPlayerNumericId;
        bridge.Connect(wsUrl);
        notifyStateChanged();
        return true;
    }

    public void Disconnect()
    {
        requestInFlight = false;
        pendingRequestId = null;
        bridge?.Disconnect();
        notifyStateChanged();
    }

    public bool TryPlayTreasureCard(long cardInstanceNumericId)
    {
        if (cardInstanceNumericId <= 0)
        {
            setError("请选择有效手牌。");
            return false;
        }

        return sendTracked("playTreasureCard", () => bridge!.SendPlayTreasureCard(cardInstanceNumericId));
    }

    public bool TrySubmitCharacterSelection(string characterDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(characterDefinitionId))
        {
            setError("请选择有效角色。");
            return false;
        }

        return sendTracked(
            "submitCharacterSelection",
            () => bridge!.SendSubmitCharacterSelection(characterDefinitionId));
    }

    public bool TryEnterSummonPhase()
    {
        return sendTracked("enterSummonPhase", () => bridge!.SendEnterSummonPhase());
    }

    public bool TryEnterEndPhase()
    {
        return sendTracked("enterEndPhase", () => bridge!.SendEnterEndPhase());
    }

    public bool TrySummonTreasureCard(long cardInstanceNumericId)
    {
        if (cardInstanceNumericId <= 0)
        {
            setError("请选择有效的召唤区卡牌。");
            return false;
        }

        return sendTracked("summonTreasureCard", () => bridge!.SendSummonTreasureCard(cardInstanceNumericId));
    }

    public bool TryUseSkill(long characterInstanceNumericId, string skillKey)
    {
        if (characterInstanceNumericId <= 0 || string.IsNullOrWhiteSpace(skillKey))
        {
            setError("技能请求缺少角色或技能。");
            return false;
        }

        return sendTracked("useSkill", () => bridge!.SendUseSkill(characterInstanceNumericId, skillKey));
    }

    public bool TryResolveAnomaly(long? targetPlayerNumericId = null)
    {
        if (targetPlayerNumericId.HasValue && targetPlayerNumericId.Value <= 0)
        {
            setError("异变解决目标玩家 ID 必须为正数。");
            return false;
        }

        return sendTracked("tryResolveAnomaly", () => bridge!.SendTryResolveAnomaly(targetPlayerNumericId));
    }

    public bool TrySubmitInputChoice(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            setError("请选择有效选项。");
            return false;
        }

        return sendTracked("submitInputChoice", () => bridge!.SendSubmitInputChoice(choiceKey));
    }

    public bool TrySubmitInputChoices(IReadOnlyList<string> choiceKeys)
    {
        if (choiceKeys is null || choiceKeys.Count == 0)
        {
            setError("请选择至少一个选项。");
            return false;
        }

        return sendTracked(
            "submitInputChoice",
            () => bridge!.SendSubmitInputChoices(choiceKeys.Where(choiceKey => !string.IsNullOrWhiteSpace(choiceKey)).ToList()));
    }

    public bool TrySubmitResponseNo()
    {
        return sendTracked("submitResponse", () => bridge!.SendSubmitResponseNo());
    }

    public bool TrySubmitDefenseFormal(string defenseTypeKey, long defenseCardInstanceNumericId)
    {
        if (string.IsNullOrWhiteSpace(defenseTypeKey))
        {
            setError("防御提交缺少伤害类型。");
            return false;
        }

        if (defenseCardInstanceNumericId <= 0)
        {
            setError("请选择有效防御牌。");
            return false;
        }

        return sendTracked("submitDefense", () => bridge!.SendSubmitDefenseFormal(defenseTypeKey, defenseCardInstanceNumericId));
    }

    private bool sendTracked(string actionType, Action sendAction)
    {
        if (!IsConnected || bridge is null)
        {
            setError("尚未连接服务器。");
            return false;
        }

        if (isMatchEnded())
        {
            setError("本局游戏已经结束，不能继续执行操作。");
            return false;
        }

        if (requestInFlight)
        {
            setError("已有请求进行中，请等待服务器响应。");
            return false;
        }

        lastError = string.Empty;
        try
        {
            sendAction();
            notifyStateChanged();
            return true;
        }
        catch (Exception exception)
        {
            requestInFlight = false;
            pendingRequestId = null;
            setError($"{actionType} 发送失败：{exception.Message}");
            return false;
        }
    }

    private void ensureBridge()
    {
        if (bridge is not null)
        {
            return;
        }

        bridge = new ServerBridge();
        bridge.OnRawRequest += handleRawRequest;
        bridge.OnRawResponse += handleRawResponse;
        bridge.OnProjectionUpdated += handleProjectionUpdated;
        bridge.OnConnectionStateChanged += handleConnectionStateChanged;
        bridge.OnError += handleError;
    }

    private void disposeBridge()
    {
        if (bridge is null)
        {
            return;
        }

        bridge.OnRawRequest -= handleRawRequest;
        bridge.OnRawResponse -= handleRawResponse;
        bridge.OnProjectionUpdated -= handleProjectionUpdated;
        bridge.OnConnectionStateChanged -= handleConnectionStateChanged;
        bridge.OnError -= handleError;
        bridge.Dispose();
        bridge = null;
    }

    private void handleRawRequest(string rawJson)
    {
        requestInFlight = true;
        pendingRequestId = tryParseRequestId(rawJson);
        notifyStateChanged();
    }

    private void handleRawResponse(string rawJson)
    {
        var responseRequestId = tryParseRequestId(rawJson);
        if (requestInFlight &&
            pendingRequestId.HasValue &&
            responseRequestId.HasValue &&
            pendingRequestId.Value == responseRequestId.Value)
        {
            requestInFlight = false;
            pendingRequestId = null;
        }

        notifyStateChanged();
    }

    private void handleProjectionUpdated(ProjectionViewModel projection)
    {
        projectionViewState?.Apply(projection.deepClone());
    }

    private void handleConnectionStateChanged(string state)
    {
        connectionState = state;
        if (!string.Equals(state, "connected", StringComparison.Ordinal))
        {
            requestInFlight = false;
            pendingRequestId = null;
        }

        notifyStateChanged();
    }

    private void handleError(string error)
    {
        requestInFlight = false;
        pendingRequestId = null;
        setError(error);
    }

    private void setError(string error)
    {
        lastError = error;
        notifyStateChanged();
    }

    private void notifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private static long? tryParseRequestId(string rawJson)
    {
        const string key = "\"requestId\"";
        var keyIndex = rawJson.IndexOf(key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return null;
        }

        var colonIndex = rawJson.IndexOf(':', keyIndex + key.Length);
        if (colonIndex < 0)
        {
            return null;
        }

        var start = colonIndex + 1;
        while (start < rawJson.Length && char.IsWhiteSpace(rawJson[start]))
        {
            start++;
        }

        var end = start;
        while (end < rawJson.Length && char.IsDigit(rawJson[end]))
        {
            end++;
        }

        return end > start && long.TryParse(rawJson[start..end], out var requestId)
            ? requestId
            : null;
    }

    private bool isMatchEnded()
    {
        return projectionViewState?.Current is { matchState: var state } &&
               string.Equals(state, "ended", StringComparison.OrdinalIgnoreCase);
    }
}
}
