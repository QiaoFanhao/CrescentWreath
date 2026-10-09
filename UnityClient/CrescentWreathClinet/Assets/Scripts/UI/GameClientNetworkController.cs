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
    private const string DefaultLobbyWsUrl = "ws://127.0.0.1:18080/ws?mode=lobby";
    private const int DefaultLobbyPort = 18080;
    private const string DefaultLobbyPath = "ws";

    [SerializeField]
    private ProjectionViewState? projectionViewState;

    private ServerBridge? bridge;
    private bool requestInFlight;
    private long? pendingRequestId;
    private string connectionState = "disconnected";
    private string lastError = string.Empty;
    private string wsUrl = DefaultWsUrl;
    private string lobbyWsUrl = DefaultLobbyWsUrl;
    private string lobbyDisplayName = "Player";
    private long viewerPlayerNumericId = 1;
    private LobbyProjectionViewModel? lobbyProjection;
    private readonly Dictionary<long, PendingRequestTelemetry> pendingTelemetryByRequestId = new();
    private string lastNetworkDiagnostics = "RTT - / projection -";
    private double lastRawResponseRealtime;
    private int mainThreadId;
    private readonly Queue<Action> pendingMainThreadActions = new();
    private readonly object pendingMainThreadActionsSyncRoot = new();

    public bool IsConnected => bridge?.isConnected == true;
    public bool IsRequestInFlight => requestInFlight;
    public string ConnectionState => connectionState;
    public string LastError => lastError;
    public string WsUrl => wsUrl;
    public string LobbyWsUrl => lobbyWsUrl;
    public string LobbyDisplayName => lobbyDisplayName;
    public long ViewerPlayerNumericId => viewerPlayerNumericId;
    public LobbyProjectionViewModel? CurrentLobbyProjection => lobbyProjection;
    public string LastNetworkDiagnostics => lastNetworkDiagnostics;

    public event Action? StateChanged;

    private void Awake()
    {
        mainThreadId = Environment.CurrentManagedThreadId;
    }

    private void Update()
    {
        flushPendingMainThreadActions();
    }

    public void Configure(ProjectionViewState newProjectionViewState)
    {
        ensureMainThreadId();
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
            setError("Already connected.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newWsUrl))
        {
            setError("WebSocket URL is required.");
            return false;
        }

        if (!long.TryParse(viewerPlayerNumericIdText, out var parsedViewerPlayerNumericId) ||
            parsedViewerPlayerNumericId <= 0)
        {
            setError("Viewer player id must be a positive integer.");
            return false;
        }

        wsUrl = sanitizeWebSocketUrl(newWsUrl);
        viewerPlayerNumericId = parsedViewerPlayerNumericId;
        requestInFlight = false;
        pendingRequestId = null;
        pendingTelemetryByRequestId.Clear();
        lastError = string.Empty;
        connectionState = "connecting";
        ensureBridge();
        bridge!.viewerPlayerNumericId = viewerPlayerNumericId;
        Debug.Log($"[ClientNet][connect.begin] mode=legacy url={wsUrl} viewer={viewerPlayerNumericId}");
        bridge.Connect(wsUrl);
        notifyStateChanged();
        return true;
    }

    public bool ConnectLobby(string newWsUrl, string displayName)
    {
        if (IsConnected)
        {
            setError("Already connected.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newWsUrl))
        {
            setError("Server address is required.");
            return false;
        }

        if (!tryNormalizeLobbyWebSocketUrl(newWsUrl, out var normalizedLobbyWsUrl, out var normalizeError))
        {
            setError(normalizeError);
            return false;
        }

        lobbyWsUrl = normalizedLobbyWsUrl;
        lobbyDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();
        requestInFlight = false;
        pendingRequestId = null;
        pendingTelemetryByRequestId.Clear();
        lastError = string.Empty;
        connectionState = "connecting";
        lobbyProjection = null;
        ensureBridge();
        Debug.Log($"[ClientNet][connect.begin] mode=lobby url={lobbyWsUrl} displayName={lobbyDisplayName}");
        bridge!.ConnectLobby(lobbyWsUrl);
        notifyStateChanged();
        return true;
    }

    public static string NormalizeLobbyWebSocketUrl(string rawUrl)
    {
        return tryNormalizeLobbyWebSocketUrl(rawUrl, out var normalizedUrl, out _)
            ? normalizedUrl
            : DefaultLobbyWsUrl;
    }

    public void Disconnect()
    {
        requestInFlight = false;
        pendingRequestId = null;
        pendingTelemetryByRequestId.Clear();
        lobbyProjection = null;
        bridge?.Disconnect();
        notifyStateChanged();
    }

    public bool TryLobbyRefresh()
    {
        return sendTracked("lobbyRefresh", () => bridge!.SendLobbyRefresh(), allowBeforeGameStart: true);
    }

    public bool TryLobbyCreateRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            roomName = lobbyDisplayName + " Room";
        }

        return sendTracked(
            "lobbyCreateRoom",
            () => bridge!.SendLobbyCreateRoom(roomName, lobbyDisplayName),
            allowBeforeGameStart: true);
    }

    public bool TryLobbyJoinRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            setError("Room id is required.");
            return false;
        }

        return sendTracked(
            "lobbyJoinRoom",
            () => bridge!.SendLobbyJoinRoom(roomId, lobbyDisplayName),
            allowBeforeGameStart: true);
    }

    public bool TryLobbyLeaveRoom()
    {
        return sendTracked("lobbyLeaveRoom", () => bridge!.SendLobbyLeaveRoom(), allowBeforeGameStart: true);
    }

    public bool TryLobbySetTeam(string teamKey)
    {
        if (string.IsNullOrWhiteSpace(teamKey))
        {
            setError("Team is required.");
            return false;
        }

        return sendTracked(
            "lobbySetTeam",
            () => bridge!.SendLobbySetTeam(teamKey),
            allowBeforeGameStart: true);
    }

    public bool TryLobbySubmitReadyState(bool isReady)
    {
        return sendTracked(
            "lobbySubmitReadyState",
            () => bridge!.SendLobbySubmitReadyState(isReady),
            allowBeforeGameStart: true);
    }

    public bool TryPlayTreasureCard(long cardInstanceNumericId)
    {
        if (cardInstanceNumericId <= 0)
        {
            setError("Please select a valid hand card.");
            return false;
        }

        return sendTracked("playTreasureCard", () => bridge!.SendPlayTreasureCard(cardInstanceNumericId));
    }

    public bool TrySubmitCharacterSelection(string characterDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(characterDefinitionId))
        {
            setError("Please select a valid character.");
            return false;
        }

        var currentProjection = projectionViewState?.Current;
        if (currentProjection is not null &&
            currentProjection.characterSelection.isActive &&
            !currentProjection.characterSelection.isCompleted)
        {
            var currentSelectingPlayerNumericId = currentProjection.characterSelection.currentSelectingPlayerNumericId;
            if (currentSelectingPlayerNumericId.HasValue &&
                currentSelectingPlayerNumericId.Value != viewerPlayerNumericId)
            {
                setError($"当前轮到 Player {currentSelectingPlayerNumericId.Value} 选择角色。");
                return false;
            }

            if (currentProjection.characterSelection.selections.Any(selection =>
                    selection.playerNumericId == viewerPlayerNumericId))
            {
                setError("你已经选择过角色，请等待其他玩家。");
                return false;
            }
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
            setError("Please select a valid summon-zone card.");
            return false;
        }

        return sendTracked("summonTreasureCard", () => bridge!.SendSummonTreasureCard(cardInstanceNumericId));
    }

    public bool TryUseSkill(long characterInstanceNumericId, string skillKey)
    {
        if (characterInstanceNumericId <= 0 || string.IsNullOrWhiteSpace(skillKey))
        {
            setError("Skill request is missing character or skill key.");
            return false;
        }

        return sendTracked("useSkill", () => bridge!.SendUseSkill(characterInstanceNumericId, skillKey));
    }

    public bool TryResolveAnomaly(long? targetPlayerNumericId = null)
    {
        if (targetPlayerNumericId.HasValue && targetPlayerNumericId.Value <= 0)
        {
            setError("Anomaly resolve target player id must be positive.");
            return false;
        }

        return sendTracked("tryResolveAnomaly", () => bridge!.SendTryResolveAnomaly(targetPlayerNumericId));
    }

    public bool TrySubmitInputChoice(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            setError("Please select a valid choice.");
            return false;
        }

        return sendTracked("submitInputChoice", () => bridge!.SendSubmitInputChoice(choiceKey));
    }

    public bool TrySubmitInputChoices(IReadOnlyList<string> choiceKeys)
    {
        if (choiceKeys is null || choiceKeys.Count == 0)
        {
            setError("Please select at least one choice.");
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
            setError("Defense submit is missing damage type.");
            return false;
        }

        if (defenseCardInstanceNumericId <= 0)
        {
            setError("Please select a valid defense card.");
            return false;
        }

        return sendTracked("submitDefense", () => bridge!.SendSubmitDefenseFormal(defenseTypeKey, defenseCardInstanceNumericId));
    }

    private bool sendTracked(string actionType, Action sendAction, bool allowBeforeGameStart = false)
    {
        if (!IsConnected || bridge is null)
        {
            setError("Not connected to server.");
            return false;
        }

        if (!allowBeforeGameStart && isMatchEnded())
        {
            setError("This match has ended. No more actions can be submitted.");
            return false;
        }

        if (requestInFlight)
        {
            setError("A request is already in flight. Please wait for server response.");
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
            pendingTelemetryByRequestId.Clear();
            setError($"{actionType} send failed: {exception.Message}");
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
        bridge.OnLobbyUpdated += handleLobbyUpdated;
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
        bridge.OnLobbyUpdated -= handleLobbyUpdated;
        bridge.OnConnectionStateChanged -= handleConnectionStateChanged;
        bridge.OnError -= handleError;
        bridge.Dispose();
        bridge = null;
    }

    private void handleRawRequest(string rawJson)
    {
        if (dispatchToMainThreadIfNeeded(() => handleRawRequest(rawJson)))
        {
            return;
        }

        requestInFlight = true;
        pendingRequestId = tryParseRequestId(rawJson);
        if (pendingRequestId.HasValue)
        {
            var actionType = tryParseStringField(rawJson, "actionType") ?? "unknown";
            pendingTelemetryByRequestId[pendingRequestId.Value] = new PendingRequestTelemetry(
                pendingRequestId.Value,
                actionType,
                Time.realtimeSinceStartupAsDouble,
                rawJson.Length);
            Debug.Log(
                $"[ClientNet][request.sent] requestId={pendingRequestId.Value} action={actionType} bytes={rawJson.Length}");
        }

        notifyStateChanged();
    }

    private void handleRawResponse(string rawJson)
    {
        if (dispatchToMainThreadIfNeeded(() => handleRawResponse(rawJson)))
        {
            return;
        }

        lastRawResponseRealtime = Time.realtimeSinceStartupAsDouble;
        var responseRequestId = tryParseRequestId(rawJson);
        var succeeded = tryParseBoolField(rawJson, "isSucceeded");
        var errorCode = tryParseStringField(rawJson, "errorCode") ?? string.Empty;
        if (requestInFlight &&
            pendingRequestId.HasValue &&
            responseRequestId.HasValue &&
            pendingRequestId.Value == responseRequestId.Value)
        {
            requestInFlight = false;
            pendingRequestId = null;
        }

        if (responseRequestId.HasValue &&
            pendingTelemetryByRequestId.TryGetValue(responseRequestId.Value, out var telemetry))
        {
            var rttMs = (lastRawResponseRealtime - telemetry.sentRealtime) * 1000.0;
            lastNetworkDiagnostics =
                $"RTT {rttMs:0}ms / projection pending / {telemetry.actionType} #{responseRequestId.Value}";
            pendingTelemetryByRequestId.Remove(responseRequestId.Value);
            Debug.Log(
                $"[ClientNet][response.received] requestId={responseRequestId.Value} action={telemetry.actionType} " +
                $"succeeded={succeeded?.ToString() ?? "unknown"} error={errorCode} rttMs={rttMs:0} " +
                $"requestBytes={telemetry.requestBytes} responseBytes={rawJson.Length}");
        }
        else
        {
            lastNetworkDiagnostics = $"push/direct response bytes {rawJson.Length}";
            Debug.Log(
                $"[ClientNet][response.received] requestId={responseRequestId?.ToString() ?? "none"} " +
                $"succeeded={succeeded?.ToString() ?? "unknown"} error={errorCode} responseBytes={rawJson.Length}");
        }

        notifyStateChanged();
    }

    private void handleProjectionUpdated(ProjectionViewModel projection)
    {
        if (dispatchToMainThreadIfNeeded(() => handleProjectionUpdated(projection)))
        {
            return;
        }

        var projectionStartRealtime = Time.realtimeSinceStartupAsDouble;
        if (projection.viewerPlayerNumericId > 0)
        {
            viewerPlayerNumericId = projection.viewerPlayerNumericId;
            syncBridgePlayerNumericId(viewerPlayerNumericId);
        }

        projectionViewState?.Apply(projection.deepClone());
        var projectionMs = (Time.realtimeSinceStartupAsDouble - projectionStartRealtime) * 1000.0;
        var sinceResponseMs = lastRawResponseRealtime > 0
            ? (projectionStartRealtime - lastRawResponseRealtime) * 1000.0
            : 0.0;
        lastNetworkDiagnostics =
            $"projection {projectionMs:0}ms / after response {sinceResponseMs:0}ms / turn {projection.turnNumber}";
        Debug.Log(
            $"[ClientNet][projection.applied] viewer={projection.viewerPlayerNumericId} localViewer={viewerPlayerNumericId} " +
            $"turn={projection.turnNumber} phase={projection.currentPhase} " +
            $"{buildCharacterSelectionDiagnostics(projection)} applyMs={projectionMs:0} afterResponseMs={sinceResponseMs:0}");
        notifyStateChanged();
    }

    private void handleLobbyUpdated(LobbyProjectionViewModel projection)
    {
        if (dispatchToMainThreadIfNeeded(() => handleLobbyUpdated(projection)))
        {
            return;
        }

        lobbyProjection = projection;
        if (projection.assignedPlayerNumericId > 0)
        {
            viewerPlayerNumericId = projection.assignedPlayerNumericId;
            syncBridgePlayerNumericId(viewerPlayerNumericId);
        }

        Debug.Log(
            $"[ClientNet][lobby.applied] connectedClient={projection.connectedClientId} assigned={projection.assignedPlayerNumericId} " +
            $"gameStarted={projection.gameStarted} currentRoom={projection.currentRoom?.roomId ?? "(none)"}");

        if (!string.IsNullOrWhiteSpace(projection.error))
        {
            lastError = projection.error;
        }

        notifyStateChanged();
    }

    private void handleConnectionStateChanged(string state)
    {
        if (dispatchToMainThreadIfNeeded(() => handleConnectionStateChanged(state)))
        {
            return;
        }

        connectionState = state;
        if (!string.Equals(state, "connected", StringComparison.Ordinal))
        {
            requestInFlight = false;
            pendingRequestId = null;
            pendingTelemetryByRequestId.Clear();
        }

        Debug.Log($"[ClientNet][connection.state] state={state} lobbyUrl={lobbyWsUrl} legacyUrl={wsUrl} viewer={viewerPlayerNumericId}");
        notifyStateChanged();
    }

    private void handleError(string error)
    {
        if (dispatchToMainThreadIfNeeded(() => handleError(error)))
        {
            return;
        }

        requestInFlight = false;
        pendingRequestId = null;
        pendingTelemetryByRequestId.Clear();
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

    private void syncBridgePlayerNumericId(long playerNumericId)
    {
        if (bridge is null || playerNumericId <= 0)
        {
            return;
        }

        bridge.viewerPlayerNumericId = playerNumericId;
        bridge.localPlayerNumericId = playerNumericId;
        bridge.actorPlayerNumericId = playerNumericId;
    }

    private bool dispatchToMainThreadIfNeeded(Action action)
    {
        ensureMainThreadId();
        if (Environment.CurrentManagedThreadId == mainThreadId)
        {
            return false;
        }

        lock (pendingMainThreadActionsSyncRoot)
        {
            pendingMainThreadActions.Enqueue(action);
        }

        return true;
    }

    private void flushPendingMainThreadActions()
    {
        ensureMainThreadId();
        while (true)
        {
            Action action;
            lock (pendingMainThreadActionsSyncRoot)
            {
                if (pendingMainThreadActions.Count == 0)
                {
                    return;
                }

                action = pendingMainThreadActions.Dequeue();
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ClientNet][mainThread.dispatch.error] {exception}");
            }
        }
    }

    private void ensureMainThreadId()
    {
        if (mainThreadId == 0)
        {
            mainThreadId = Environment.CurrentManagedThreadId;
        }
    }

    private static string buildCharacterSelectionDiagnostics(ProjectionViewModel projection)
    {
        if (!projection.characterSelection.isActive)
        {
            return "characterSelection=inactive";
        }

        var currentSelecting = projection.characterSelection.currentSelectingPlayerNumericId?.ToString() ?? "(none)";
        var selections = projection.characterSelection.selections.Count == 0
            ? "(none)"
            : string.Join(
                ",",
                projection.characterSelection.selections.Select(selection =>
                    $"P{selection.playerNumericId}:{selection.characterDefinitionId}"));
        return $"characterSelection=current:{currentSelecting},completed:{projection.characterSelection.isCompleted},selections:{selections}";
    }

    private static bool tryNormalizeLobbyWebSocketUrl(
        string rawUrl,
        out string normalizedUrl,
        out string error)
    {
        normalizedUrl = string.Empty;
        error = string.Empty;

        var value = sanitizeWebSocketUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Server address is required.";
            return false;
        }

        if (!value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
        {
            value = "ws://" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)))
        {
            error = "Invalid server address. Example: 26.x.x.x or ws://26.x.x.x:18080/ws?mode=lobby";
            return false;
        }

        var builder = new UriBuilder(uri);
        if (builder.Port < 0)
        {
            builder.Port = DefaultLobbyPort;
        }

        if (string.IsNullOrWhiteSpace(builder.Path) || string.Equals(builder.Path, "/", StringComparison.Ordinal))
        {
            builder.Path = DefaultLobbyPath;
        }

        var query = builder.Query;
        if (query.StartsWith("?", StringComparison.Ordinal))
        {
            query = query[1..];
        }

        var hasLobbyMode = query
            .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.StartsWith("mode=", StringComparison.OrdinalIgnoreCase));
        if (!hasLobbyMode)
        {
            query = string.IsNullOrWhiteSpace(query)
                ? "mode=lobby"
                : query + "&mode=lobby";
        }

        builder.Query = query;
        normalizedUrl = builder.Uri.ToString();
        return true;
    }

    private static string sanitizeWebSocketUrl(string rawUrl)
    {
        return (rawUrl ?? string.Empty)
            .Replace("\u200B", string.Empty)
            .Replace("\u200C", string.Empty)
            .Replace("\u200D", string.Empty)
            .Replace("\uFEFF", string.Empty)
            .Trim();
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

    private static string? tryParseStringField(string rawJson, string fieldName)
    {
        var key = "\"" + fieldName + "\"";
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

        var quoteStart = rawJson.IndexOf('"', colonIndex + 1);
        if (quoteStart < 0)
        {
            return null;
        }

        var quoteEnd = rawJson.IndexOf('"', quoteStart + 1);
        return quoteEnd > quoteStart
            ? rawJson.Substring(quoteStart + 1, quoteEnd - quoteStart - 1)
            : null;
    }

    private static bool? tryParseBoolField(string rawJson, string fieldName)
    {
        var key = "\"" + fieldName + "\"";
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

        if (rawJson.IndexOf("true", start, StringComparison.OrdinalIgnoreCase) == start)
        {
            return true;
        }

        if (rawJson.IndexOf("false", start, StringComparison.OrdinalIgnoreCase) == start)
        {
            return false;
        }

        return null;
    }

    private bool isMatchEnded()
    {
        return projectionViewState?.Current is { matchState: var state } &&
               string.Equals(state, "ended", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct PendingRequestTelemetry
    {
        public PendingRequestTelemetry(long requestId, string actionType, double sentRealtime, int requestBytes)
        {
            this.requestId = requestId;
            this.actionType = actionType;
            this.sentRealtime = sentRealtime;
            this.requestBytes = requestBytes;
        }

        public readonly long requestId;
        public readonly string actionType;
        public readonly double sentRealtime;
        public readonly int requestBytes;
    }
}
}


