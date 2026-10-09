using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerPrototypeWebSocketHostRuntime : IAsyncDisposable
{
    private const int MaxLoggedMessageLength = 280;
    private const int MaxConcurrentConnections = 4;
    private const long MinViewerPlayerNumericId = 1;
    private const long MaxViewerPlayerNumericId = 4;

    private ServerGameSession session;
    private ServerSocketActionRouter actionRouter;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly SemaphoreSlim actionExecutionLock = new(1, 1);
    private readonly object connectionLock = new();
    private readonly Dictionary<long, WebSocket> activeConnectionsByViewerPlayerId = new();
    private readonly Dictionary<string, WebSocket> activeLobbyConnectionsByConnectionId = new();
    private readonly Dictionary<string, long> assignedPlayerNumericIdByLobbyConnectionId = new();
    private readonly LobbyRoomManager lobbyRoomManager;
    private readonly LobbySocketActionRouter lobbyActionRouter;

    private HttpListener? listener;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? acceptLoopTask;

    public ServerPrototypeWebSocketHostRuntime()
        : this(ServerGameSession.createStandard2v2(requireCharacterSelection: true))
    {
    }

    public ServerPrototypeWebSocketHostRuntime(ServerGameSession session)
    {
        this.session = session;
        actionRouter = new ServerSocketActionRouter(session);
        lobbyRoomManager = new LobbyRoomManager();
        lobbyActionRouter = new LobbySocketActionRouter(lobbyRoomManager);
        serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
        };
    }

    public ServerGameSession gameSession => session;

    public bool isRunning => listener is { IsListening: true };

    public async Task<Uri> startAsync(int port = 0, CancellationToken cancellationToken = default)
    {
        if (isRunning)
        {
            throw new InvalidOperationException("ServerPrototypeWebSocketHostRuntime is already running.");
        }

        var resolvedPort = port > 0 ? port : reserveDynamicPort();
        var bindHost = getConfiguredBindHost();
        var prefix = $"http://{bindHost}:{resolvedPort}/";
        listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        acceptLoopTask = Task.Run(() => acceptLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        var displayHost = getConfiguredPublicHost() ?? resolvePreferredDisplayHost();
        return new Uri($"ws://{displayHost}:{resolvedPort}/ws");
    }

    public async Task stopAsync()
    {
        if (listener is null)
        {
            return;
        }

        cancellationTokenSource?.Cancel();
        listener.Close();
        listener = null;

        if (acceptLoopTask is not null)
        {
            try
            {
                await acceptLoopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        acceptLoopTask = null;
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;

        List<WebSocket> socketsToClose;
        lock (connectionLock)
        {
            socketsToClose = new List<WebSocket>(activeConnectionsByViewerPlayerId.Values);
            socketsToClose.AddRange(activeLobbyConnectionsByConnectionId.Values);
            socketsToClose = socketsToClose.Distinct().ToList();
            activeConnectionsByViewerPlayerId.Clear();
            activeLobbyConnectionsByConnectionId.Clear();
            assignedPlayerNumericIdByLobbyConnectionId.Clear();
        }

        foreach (var socket in socketsToClose)
        {
            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "host-stopped",
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (WebSocketException)
            {
            }
            finally
            {
                socket.Dispose();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await stopAsync().ConfigureAwait(false);
    }

    private async Task acceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                if (listener is null)
                {
                    break;
                }

                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            if (context is null)
            {
                continue;
            }

            _ = Task.Run(() => processContextAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task processContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var remoteEndpoint = context.Request.RemoteEndPoint?.ToString() ?? "unknown";
            var requestPath = context.Request.Url?.AbsolutePath ?? string.Empty;
            if (!string.Equals(requestPath, "/ws", StringComparison.Ordinal))
            {
                logInfo($"Rejected request from {remoteEndpoint}: unsupported path '{requestPath}' (404).");
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            if (!context.Request.IsWebSocketRequest)
            {
                logInfo($"Rejected request from {remoteEndpoint}: not a websocket request (400).");
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            if (isLobbyModeRequest(context.Request))
            {
                await processLobbyWebSocketAsync(context, remoteEndpoint, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!tryParseViewerPlayerNumericId(context.Request, out var viewerPlayerNumericId, out var viewerParseFailureReason))
            {
                logInfo($"Rejected websocket upgrade from {remoteEndpoint}: {viewerParseFailureReason} (400).");
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            WebSocket? socket = null;
            var connectionRegistered = false;
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                socket = webSocketContext.WebSocket;

                if (!tryRegisterConnection(viewerPlayerNumericId, socket, out var registerFailureReason))
                {
                    logInfo($"Rejected websocket from {remoteEndpoint}: {registerFailureReason} (409).");
                    await socket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        registerFailureReason,
                        CancellationToken.None).ConfigureAwait(false);
                    socket.Dispose();
                    return;
                }

                connectionRegistered = true;
                logInfo(
                    $"WebSocket connected: remote={remoteEndpoint}, path={requestPath}, viewerPlayerNumericId={viewerPlayerNumericId}, activeConnections={getActiveConnectionCount()}.");

                var initialSnapshotEnvelope = createInitialSnapshotEnvelope(viewerPlayerNumericId);
                var initialSnapshotJson = JsonSerializer.Serialize(initialSnapshotEnvelope, serializerOptions);
                await sendTextAsync(socket, initialSnapshotJson, cancellationToken).ConfigureAwait(false);
                logInfo(
                    $"Sent initial snapshot: requestId={initialSnapshotEnvelope.requestId}, viewerPlayerNumericId={initialSnapshotEnvelope.viewerPlayerNumericId}, bytes={Encoding.UTF8.GetByteCount(initialSnapshotJson)}.");

                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var requestMessage = await readTextMessageAsync(socket, cancellationToken).ConfigureAwait(false);
                    if (requestMessage is null)
                    {
                        logInfo($"WebSocket message loop ended: remote={remoteEndpoint}, viewerPlayerNumericId={viewerPlayerNumericId}.");
                        break;
                    }

                    tryExtractRequestInfo(requestMessage, out var requestId, out var actionType);
                    logInfo(
                        $"Received message: remote={remoteEndpoint}, viewerPlayerNumericId={viewerPlayerNumericId}, requestId={requestId}, actionType={actionType}, payload={truncateForLog(requestMessage)}");

                    using var traceScope = ServerTraceLogger.BeginRequest(
                        "legacy",
                        remoteEndpoint,
                        $"viewer:{viewerPlayerNumericId}",
                        viewerPlayerNumericId,
                        viewerPlayerNumericId,
                        requestId,
                        actionType);
                    ServerTraceLogger.Trace(
                        "request.begin",
                        ("payloadBytes", Encoding.UTF8.GetByteCount(requestMessage)),
                        ("payloadPreview", truncateForLog(requestMessage)));

                    await actionExecutionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var routeOutcome = actionRouter.routeMessageDetailed(
                            requestMessage,
                            new ServerSocketRouteOptions
                            {
                                forcedViewerPlayerNumericId = viewerPlayerNumericId,
                                requiredActorPlayerNumericId = viewerPlayerNumericId,
                            });

                        logInfo(
                            $"Routed message: requestId={routeOutcome.responseEnvelope.requestId}, actionType={routeOutcome.actionType}, isSucceeded={routeOutcome.responseEnvelope.isSucceeded}, errorCode={routeOutcome.responseEnvelope.error?.code ?? "(none)"}, viewerPlayerNumericId={routeOutcome.responseEnvelope.viewerPlayerNumericId}.");
                        ServerTraceLogger.Trace(
                            "request.routed",
                            ("succeeded", routeOutcome.responseEnvelope.isSucceeded),
                            ("errorCode", routeOutcome.responseEnvelope.error?.code ?? "(none)"),
                            ("errorMessage", routeOutcome.responseEnvelope.error?.message ?? "(none)"));

                        var directResponseJson = JsonSerializer.Serialize(routeOutcome.responseEnvelope, serializerOptions);
                        await sendTextAsync(socket, directResponseJson, cancellationToken).ConfigureAwait(false);
                        logInfo(
                            $"Sent response: requestId={routeOutcome.responseEnvelope.requestId}, isSucceeded={routeOutcome.responseEnvelope.isSucceeded}, viewerPlayerNumericId={routeOutcome.responseEnvelope.viewerPlayerNumericId}, bytes={Encoding.UTF8.GetByteCount(directResponseJson)}.");
                        ServerTraceLogger.Trace(
                            "projection.direct",
                            ("bytes", Encoding.UTF8.GetByteCount(directResponseJson)),
                            ("projection", summarizeEnvelope(routeOutcome.responseEnvelope)));

                        if (routeOutcome.actionResult is { isSucceeded: true } actionResult)
                        {
                            await broadcastSuccessfulActionAsync(
                                viewerPlayerNumericId,
                                routeOutcome.actionType,
                                actionResult,
                                cancellationToken).ConfigureAwait(false);
                        }
                        ServerTraceLogger.Trace(
                            "request.end",
                            ("succeeded", routeOutcome.responseEnvelope.isSucceeded),
                            ("errorCode", routeOutcome.responseEnvelope.error?.code ?? "(none)"));
                    }
                    finally
                    {
                        actionExecutionLock.Release();
                    }
                }
            }
            catch (Exception exception)
            {
                logError($"WebSocket processing failed for remote={remoteEndpoint}, viewerPlayerNumericId={viewerPlayerNumericId}: {exception.Message}");
                throw;
            }
            finally
            {
                if (connectionRegistered)
                {
                    unregisterConnection(viewerPlayerNumericId, socket);
                }

                if (socket is not null)
                {
                    try
                    {
                        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                        {
                            await socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "closed",
                                CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    catch (WebSocketException)
                    {
                    }

                    socket.Dispose();
                }

                logInfo(
                    $"WebSocket disconnected: remote={remoteEndpoint}, viewerPlayerNumericId={viewerPlayerNumericId}, activeConnections={getActiveConnectionCount()}.");
            }
        }
        catch (Exception exception)
        {
            logError($"Unhandled websocket context exception: {exception.Message}");
            if (context.Response.OutputStream.CanWrite)
            {
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                }
            }
        }
    }

    private async Task processLobbyWebSocketAsync(
        HttpListenerContext context,
        string remoteEndpoint,
        CancellationToken cancellationToken)
    {
        WebSocket? socket = null;
        var connectionId = Guid.NewGuid().ToString("N");
        var connectionRegistered = false;
        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
            socket = webSocketContext.WebSocket;
            registerLobbyConnection(connectionId, socket);
            connectionRegistered = true;
            logInfo($"Lobby websocket connected: remote={remoteEndpoint}, connectionId={connectionId}, activeConnections={getActiveConnectionCount()}.");

            var initialLobbyResult = lobbyRoomManager.createInitialProjection(connectionId);
            await sendLobbyResponseAsync(socket, 0, 0, initialLobbyResult, cancellationToken).ConfigureAwait(false);

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var requestMessage = await readTextMessageAsync(socket, cancellationToken).ConfigureAwait(false);
                if (requestMessage is null)
                {
                    logInfo($"Lobby websocket message loop ended: remote={remoteEndpoint}, connectionId={connectionId}.");
                    break;
                }

                tryExtractRequestInfo(requestMessage, out var requestId, out var actionType);
                logInfo(
                    $"Received lobby message: remote={remoteEndpoint}, connectionId={connectionId}, requestId={requestId}, actionType={actionType}, payload={truncateForLog(requestMessage)}");

                var assignedForTrace = tryResolveAssignedLobbyPlayerId(connectionId, out var assignedTracePlayerId)
                    ? assignedTracePlayerId
                    : (long?)null;
                using var traceScope = ServerTraceLogger.BeginRequest(
                    "lobby",
                    remoteEndpoint,
                    connectionId,
                    assignedForTrace,
                    assignedForTrace,
                    requestId,
                    actionType);
                ServerTraceLogger.Trace(
                    "request.begin",
                    ("payloadBytes", Encoding.UTF8.GetByteCount(requestMessage)),
                    ("payloadPreview", truncateForLog(requestMessage)));

                await actionExecutionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (tryResolveAssignedLobbyPlayerId(connectionId, out var assignedPlayerNumericId) &&
                        !isLobbyActionType(actionType))
                    {
                        var routeOutcome = actionRouter.routeMessageDetailed(
                            requestMessage,
                            new ServerSocketRouteOptions
                            {
                                forcedViewerPlayerNumericId = assignedPlayerNumericId,
                                requiredActorPlayerNumericId = assignedPlayerNumericId,
                            });

                        var directResponseJson = JsonSerializer.Serialize(routeOutcome.responseEnvelope, serializerOptions);
                        await sendTextAsync(socket, directResponseJson, cancellationToken).ConfigureAwait(false);
                        ServerTraceLogger.Trace(
                            "projection.direct",
                            ("bytes", Encoding.UTF8.GetByteCount(directResponseJson)),
                            ("projection", summarizeEnvelope(routeOutcome.responseEnvelope)));
                        if (routeOutcome.actionResult is { isSucceeded: true } actionResult)
                        {
                            await broadcastSuccessfulActionAsync(
                                assignedPlayerNumericId,
                                routeOutcome.actionType,
                                actionResult,
                                cancellationToken).ConfigureAwait(false);
                        }
                        ServerTraceLogger.Trace(
                            "request.end",
                            ("succeeded", routeOutcome.responseEnvelope.isSucceeded),
                            ("errorCode", routeOutcome.responseEnvelope.error?.code ?? "(none)"));

                        continue;
                    }

                    var lobbyResult = lobbyActionRouter.routeMessage(connectionId, requestMessage);
                    logInfo(
                        $"Routed lobby message: requestId={requestId}, actionType={actionType}, isSucceeded={lobbyResult.isSucceeded}, errorCode={lobbyResult.errorCode ?? "(none)"}, connectionId={connectionId}.");
                    ServerTraceLogger.Trace(
                        "lobby.routed",
                        ("succeeded", lobbyResult.isSucceeded),
                        ("errorCode", lobbyResult.errorCode ?? "(none)"),
                        ("room", lobbyResult.projection.currentRoom?.roomId ?? "(none)"),
                        ("eventType", lobbyResult.projection.eventType));
                    if (lobbyResult.gameStartAssignment is not null)
                    {
                        startLobbyGame(lobbyResult.gameStartAssignment);
                    }

                    await sendLobbyResponseAsync(socket, requestId, 0, lobbyResult, cancellationToken).ConfigureAwait(false);
                    await broadcastLobbyUpdateAsync(connectionId, requestId, lobbyResult, cancellationToken).ConfigureAwait(false);
                    ServerTraceLogger.Trace(
                        "request.end",
                        ("succeeded", lobbyResult.isSucceeded),
                        ("errorCode", lobbyResult.errorCode ?? "(none)"));
                }
                finally
                {
                    actionExecutionLock.Release();
                }
            }
        }
        catch (Exception exception)
        {
            logError($"Lobby websocket processing failed for remote={remoteEndpoint}, connectionId={connectionId}: {exception.Message}");
            throw;
        }
        finally
        {
            if (connectionRegistered)
            {
                unregisterLobbyConnection(connectionId, socket);
                lobbyRoomManager.disconnect(connectionId);
            }

            if (socket is not null)
            {
                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "closed",
                            CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (WebSocketException)
                {
                }

                socket.Dispose();
            }

            logInfo($"Lobby websocket disconnected: remote={remoteEndpoint}, connectionId={connectionId}, activeConnections={getActiveConnectionCount()}.");
        }
    }

    private void startLobbyGame(GameStartAssignmentResult assignment)
    {
        session = ServerGameSession.createStandard2v2(requireCharacterSelection: true);
        actionRouter = new ServerSocketActionRouter(session);
        applyLobbyTeamAssignmentsToGameState(assignment);

        lock (connectionLock)
        {
            activeConnectionsByViewerPlayerId.Clear();
            assignedPlayerNumericIdByLobbyConnectionId.Clear();
            foreach (var assignmentEntry in assignment.assignedPlayerNumericIdsByConnectionId)
            {
                assignedPlayerNumericIdByLobbyConnectionId[assignmentEntry.Key] = assignmentEntry.Value;
                if (activeLobbyConnectionsByConnectionId.TryGetValue(assignmentEntry.Key, out var socket))
                {
                    activeConnectionsByViewerPlayerId[assignmentEntry.Value] = socket;
                }
            }
        }
    }

    private void applyLobbyTeamAssignmentsToGameState(GameStartAssignmentResult assignment)
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        if (!session.gameState.teams.TryGetValue(teamAId, out var teamAState) ||
            !session.gameState.teams.TryGetValue(teamBId, out var teamBState) ||
            session.gameState.matchMeta is null)
        {
            return;
        }

        teamAState.memberPlayerIds.Clear();
        teamBState.memberPlayerIds.Clear();
        session.gameState.matchMeta.teamAssignments.Clear();
        session.gameState.matchMeta.teamSeatMap.Clear();

        foreach (var entry in assignment.lobbyTeamByPlayerNumericId.OrderBy(entry => entry.Key))
        {
            var playerId = new PlayerId(entry.Key);
            var teamId = entry.Value == LobbyTeamKey.teamA ? teamAId : teamBId;
            if (session.gameState.players.TryGetValue(playerId, out var playerState))
            {
                playerState.teamId = teamId;
            }

            session.gameState.matchMeta.teamAssignments[playerId] = teamId;
            if (teamId == teamAId)
            {
                teamAState.memberPlayerIds.Add(playerId);
            }
            else
            {
                teamBState.memberPlayerIds.Add(playerId);
            }
        }

        session.gameState.matchMeta.teamSeatMap[teamAId] = new List<PlayerId>(teamAState.memberPlayerIds);
        session.gameState.matchMeta.teamSeatMap[teamBId] = new List<PlayerId>(teamBState.memberPlayerIds);
        var turnState = session.gameState.turnState;
        if (turnState is not null &&
            turnState.currentPlayerId.Value > 0 &&
            session.gameState.players.TryGetValue(turnState.currentPlayerId, out var currentPlayerState))
        {
            turnState.currentTeamId = currentPlayerState.teamId;
        }
    }

    private async Task broadcastLobbyUpdateAsync(
        string sourceConnectionId,
        long requestId,
        LobbyActionResult sourceResult,
        CancellationToken cancellationToken)
    {
        var connectionIds = sourceResult.gameStartAssignment is not null
            ? sourceResult.gameStartAssignment.assignedPlayerNumericIdsByConnectionId.Keys.ToList()
            : getLobbyBroadcastConnectionIds(sourceResult);
        if (connectionIds.Count == 0)
        {
            return;
        }

        foreach (var connectionId in connectionIds)
        {
            if (string.Equals(connectionId, sourceConnectionId, StringComparison.Ordinal))
            {
                continue;
            }

            WebSocket? socket;
            lock (connectionLock)
            {
                activeLobbyConnectionsByConnectionId.TryGetValue(connectionId, out socket);
            }

            if (socket is null || socket.State != WebSocketState.Open)
            {
                continue;
            }

            var projection = lobbyRoomManager.buildProjection(connectionId, sourceResult.projection.eventType);
            if (sourceResult.gameStartAssignment is not null &&
                sourceResult.gameStartAssignment.assignedPlayerNumericIdsByConnectionId.TryGetValue(connectionId, out var assignedPlayerNumericId))
            {
                projection.gameStarted = true;
                projection.assignedPlayerNumericId = assignedPlayerNumericId;
                projection.gameSessionId = sourceResult.gameStartAssignment.gameSessionId;
            }

            var response = buildLobbyResponse(requestId, projection.assignedPlayerNumericId ?? 0, true, null, projection);
            if (projection.gameStarted && projection.assignedPlayerNumericId.HasValue)
            {
                response.assignedPlayerNumericId = projection.assignedPlayerNumericId;
                response.messageType = "gameStarted";
                var initialSnapshot = createInitialSnapshotEnvelope(projection.assignedPlayerNumericId.Value);
                response.stateProjection = initialSnapshot.stateProjection;
                response.interaction = initialSnapshot.interaction;
                response.eventLog = initialSnapshot.eventLog;
            }

            var json = JsonSerializer.Serialize(response, serializerOptions);
            await sendTextAsync(socket, json, cancellationToken).ConfigureAwait(false);
            ServerTraceLogger.Trace(
                "projection.push",
                ("targetConnectionId", connectionId),
                ("viewer", response.viewerPlayerNumericId),
                ("bytes", Encoding.UTF8.GetByteCount(json)),
                ("projection", summarizeEnvelope(response)));
        }
    }

    private List<string> getLobbyBroadcastConnectionIds(LobbyActionResult sourceResult)
    {
        if (!sourceResult.isSucceeded)
        {
            return new List<string>();
        }

        lock (connectionLock)
        {
            return activeLobbyConnectionsByConnectionId.Keys.ToList();
        }
    }

    private async Task sendLobbyResponseAsync(
        WebSocket socket,
        long requestId,
        long viewerPlayerNumericId,
        LobbyActionResult lobbyResult,
        CancellationToken cancellationToken)
    {
        var response = buildLobbyResponse(
            requestId,
            lobbyResult.projection.assignedPlayerNumericId ?? viewerPlayerNumericId,
            lobbyResult.isSucceeded,
            lobbyResult.isSucceeded ? null : new ServerErrorProjection
            {
                code = lobbyResult.errorCode ?? "lobby_error",
                message = lobbyResult.errorMessage ?? "Lobby request failed.",
            },
            lobbyResult.projection);

        if (lobbyResult.gameStartAssignment is not null &&
            lobbyResult.gameStartAssignment.assignedPlayerNumericIdsByConnectionId.TryGetValue(
                lobbyResult.projection.connectedClientId,
                out var assignedPlayerNumericId))
        {
            response.assignedPlayerNumericId = assignedPlayerNumericId;
            response.viewerPlayerNumericId = assignedPlayerNumericId;
            response.messageType = "gameStarted";
            var initialSnapshot = createInitialSnapshotEnvelope(assignedPlayerNumericId);
            response.stateProjection = initialSnapshot.stateProjection;
            response.interaction = initialSnapshot.interaction;
            response.eventLog = initialSnapshot.eventLog;
        }

        var json = JsonSerializer.Serialize(response, serializerOptions);
        await sendTextAsync(socket, json, cancellationToken).ConfigureAwait(false);
        ServerTraceLogger.Trace(
            "projection.direct",
            ("bytes", Encoding.UTF8.GetByteCount(json)),
            ("projection", summarizeEnvelope(response)));
    }

    private ServerSocketResponseEnvelope buildLobbyResponse(
        long requestId,
        long viewerPlayerNumericId,
        bool isSucceeded,
        ServerErrorProjection? error,
        LobbyProjection projection)
    {
        return new ServerSocketResponseEnvelope
        {
            requestId = requestId,
            viewerPlayerNumericId = viewerPlayerNumericId,
            isSucceeded = isSucceeded,
            error = error,
            lobbyProjection = projection,
            assignedPlayerNumericId = projection.assignedPlayerNumericId,
            messageType = projection.gameStarted ? "gameStarted" : "lobbyUpdate",
        };
    }

    private async Task broadcastSuccessfulActionAsync(
        long requesterViewerPlayerNumericId,
        string actionType,
        ServerActionProcessResult actionResult,
        CancellationToken cancellationToken)
    {
        List<KeyValuePair<long, WebSocket>> connectionSnapshot;
        lock (connectionLock)
        {
            connectionSnapshot = new List<KeyValuePair<long, WebSocket>>(activeConnectionsByViewerPlayerId);
        }

        if (connectionSnapshot.Count <= 1)
        {
            return;
        }

        var broadcastCount = 0;
        foreach (var connection in connectionSnapshot)
        {
            var viewerPlayerNumericId = connection.Key;
            if (viewerPlayerNumericId == requesterViewerPlayerNumericId)
            {
                continue;
            }

            var socket = connection.Value;
            if (socket.State != WebSocketState.Open)
            {
                continue;
            }

            var viewerScopedActionResult = session.projectResultForViewer(actionResult, viewerPlayerNumericId);
            var broadcastEnvelope = ServerSocketActionRouter.convertActionResultToSocketResponse(viewerScopedActionResult);
            var broadcastJson = JsonSerializer.Serialize(broadcastEnvelope, serializerOptions);
            try
            {
                await sendTextAsync(socket, broadcastJson, cancellationToken).ConfigureAwait(false);
                ServerTraceLogger.Trace(
                    "projection.push",
                    ("targetViewer", viewerPlayerNumericId),
                    ("bytes", Encoding.UTF8.GetByteCount(broadcastJson)),
                    ("projection", summarizeEnvelope(broadcastEnvelope)));
                broadcastCount++;
            }
            catch (WebSocketException exception)
            {
                logError(
                    $"Failed to broadcast actionType={actionType} requestId={actionResult.requestId} to viewerPlayerNumericId={viewerPlayerNumericId}: {exception.Message}");
            }
        }

        if (broadcastCount > 0)
        {
            logInfo(
                $"Broadcasted actionType={actionType}, requestId={actionResult.requestId} to {broadcastCount} peer connection(s).");
        }
    }

    private static async Task sendTextAsync(WebSocket socket, string responseJson, CancellationToken cancellationToken)
    {
        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
        await socket.SendAsync(
            new ArraySegment<byte>(responseBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken).ConfigureAwait(false);
    }

    private ServerSocketResponseEnvelope createInitialSnapshotEnvelope(long viewerPlayerNumericId)
    {
        var initialSnapshotResult = new ServerActionProcessResult
        {
            requestId = 0,
            isSucceeded = true,
            viewerPlayerNumericId = viewerPlayerNumericId,
            error = null,
            producedEvents = new List<RuleCore.Events.GameEvent>(),
            updatedState = session.gameState,
            errorMessage = null,
        };

        var viewerScopedResult = session.projectResultForViewer(initialSnapshotResult, viewerPlayerNumericId);
        return ServerSocketActionRouter.convertActionResultToSocketResponse(viewerScopedResult);
    }

    private static string summarizeEnvelope(ServerSocketResponseEnvelope envelope)
    {
        var state = envelope.stateProjection;
        var interaction = envelope.interaction;
        var viewerHand = state?.players
            .FirstOrDefault(player => player.playerNumericId == envelope.viewerPlayerNumericId)
            ?.handZone?.cardCount;
        var input = interaction?.inputContext is null
            ? "(none)"
            : $"id={interaction.inputContext.inputContextNumericId},context={interaction.inputContext.contextKey},required={interaction.inputContext.requiredPlayerNumericId}";
        var response = interaction?.responseWindow is null
            ? "(none)"
            : $"id={interaction.responseWindow.responseWindowNumericId},type={interaction.responseWindow.windowTypeKey},stage={interaction.responseWindow.pendingDamageResponseStageKey},responder={interaction.responseWindow.currentResponderPlayerNumericId}";
        return $"messageType={envelope.messageType ?? "(none)"};succeeded={envelope.isSucceeded};viewer={envelope.viewerPlayerNumericId};assigned={envelope.assignedPlayerNumericId?.ToString() ?? "(none)"};hasState={state is not null};hand={viewerHand?.ToString() ?? "(unknown)"};events={envelope.eventLog.Count};input={input};response={response};error={envelope.error?.code ?? "(none)"};lobby={envelope.lobbyProjection?.eventType ?? "(none)"}";
    }

    private bool tryRegisterConnection(long viewerPlayerNumericId, WebSocket socket, out string failureReason)
    {
        lock (connectionLock)
        {
            if (activeConnectionsByViewerPlayerId.ContainsKey(viewerPlayerNumericId))
            {
                failureReason = $"viewerPlayerNumericId {viewerPlayerNumericId} is already occupied by an active connection";
                return false;
            }

            if (activeConnectionsByViewerPlayerId.Count >= MaxConcurrentConnections)
            {
                failureReason = $"active connection limit ({MaxConcurrentConnections}) reached";
                return false;
            }

            activeConnectionsByViewerPlayerId[viewerPlayerNumericId] = socket;
            failureReason = string.Empty;
            return true;
        }
    }

    private void unregisterConnection(long viewerPlayerNumericId, WebSocket? socket)
    {
        lock (connectionLock)
        {
            if (!activeConnectionsByViewerPlayerId.TryGetValue(viewerPlayerNumericId, out var registeredSocket))
            {
                return;
            }

            if (socket is not null && !ReferenceEquals(registeredSocket, socket))
            {
                return;
            }

            activeConnectionsByViewerPlayerId.Remove(viewerPlayerNumericId);
        }
    }

    private int getActiveConnectionCount()
    {
        lock (connectionLock)
        {
            return activeConnectionsByViewerPlayerId.Count + activeLobbyConnectionsByConnectionId.Count;
        }
    }

    private void registerLobbyConnection(string connectionId, WebSocket socket)
    {
        lock (connectionLock)
        {
            activeLobbyConnectionsByConnectionId[connectionId] = socket;
        }
    }

    private void unregisterLobbyConnection(string connectionId, WebSocket? socket)
    {
        lock (connectionLock)
        {
            if (activeLobbyConnectionsByConnectionId.TryGetValue(connectionId, out var registeredSocket) &&
                (socket is null || ReferenceEquals(registeredSocket, socket)))
            {
                activeLobbyConnectionsByConnectionId.Remove(connectionId);
            }

            if (assignedPlayerNumericIdByLobbyConnectionId.TryGetValue(connectionId, out var assignedPlayerNumericId) &&
                activeConnectionsByViewerPlayerId.TryGetValue(assignedPlayerNumericId, out var assignedSocket) &&
                (socket is null || ReferenceEquals(assignedSocket, socket)))
            {
                activeConnectionsByViewerPlayerId.Remove(assignedPlayerNumericId);
            }

            assignedPlayerNumericIdByLobbyConnectionId.Remove(connectionId);
        }
    }

    private bool tryResolveAssignedLobbyPlayerId(string connectionId, out long assignedPlayerNumericId)
    {
        lock (connectionLock)
        {
            return assignedPlayerNumericIdByLobbyConnectionId.TryGetValue(connectionId, out assignedPlayerNumericId);
        }
    }

    private static bool isLobbyModeRequest(HttpListenerRequest request)
    {
        return string.Equals(request.QueryString["mode"], "lobby", StringComparison.OrdinalIgnoreCase);
    }

    private static bool isLobbyActionType(string actionType)
    {
        return actionType.StartsWith("lobby", StringComparison.Ordinal);
    }

    private static bool tryParseViewerPlayerNumericId(HttpListenerRequest request, out long viewerPlayerNumericId, out string failureReason)
    {
        viewerPlayerNumericId = 0;
        var viewerText = request.QueryString["viewerPlayerNumericId"];
        if (string.IsNullOrWhiteSpace(viewerText))
        {
            failureReason = "query parameter viewerPlayerNumericId is required";
            return false;
        }

        if (!long.TryParse(viewerText, out viewerPlayerNumericId))
        {
            failureReason = "query parameter viewerPlayerNumericId must be an integer";
            return false;
        }

        if (viewerPlayerNumericId < MinViewerPlayerNumericId || viewerPlayerNumericId > MaxViewerPlayerNumericId)
        {
            failureReason = $"query parameter viewerPlayerNumericId must be in range [{MinViewerPlayerNumericId}, {MaxViewerPlayerNumericId}]";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private static async Task<string?> readTextMessageAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var memoryStream = new System.IO.MemoryStream();
        while (true)
        {
            var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (receiveResult.MessageType != WebSocketMessageType.Text)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.InvalidMessageType,
                    "Only text messages are supported.",
                    cancellationToken).ConfigureAwait(false);
                return null;
            }

            memoryStream.Write(buffer, 0, receiveResult.Count);
            if (receiveResult.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static int reserveDynamicPort()
    {
        using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();
        return port;
    }

    private static string getConfiguredBindHost()
    {
        var configured = Environment.GetEnvironmentVariable("CRESCENT_WS_BIND_HOST");
        return string.IsNullOrWhiteSpace(configured)
            ? "+"
            : configured.Trim();
    }

    private static string? getConfiguredPublicHost()
    {
        var configured = Environment.GetEnvironmentVariable("CRESCENT_WS_PUBLIC_HOST");
        return string.IsNullOrWhiteSpace(configured)
            ? null
            : configured.Trim();
    }

    private static string resolvePreferredDisplayHost()
    {
        var addresses = getUsableIPv4Addresses().ToList();
        return addresses.FirstOrDefault(address => address.StartsWith("26.", StringComparison.Ordinal))
               ?? addresses.FirstOrDefault(address => address.StartsWith("192.168.", StringComparison.Ordinal) ||
                                                      address.StartsWith("10.", StringComparison.Ordinal) ||
                                                      address.StartsWith("172.", StringComparison.Ordinal))
               ?? "127.0.0.1";
    }

    private static IEnumerable<string> getUsableIPv4Addresses()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
            {
                var address = unicastAddress.Address;
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    yield return address.ToString();
                }
            }
        }
    }

    private static bool tryExtractRequestInfo(string requestMessage, out long requestId, out string actionType)
    {
        requestId = 0;
        actionType = "(unknown)";
        try
        {
            using var document = JsonDocument.Parse(requestMessage);
            var root = document.RootElement;
            if (root.TryGetProperty("requestId", out var requestIdElement) && requestIdElement.TryGetInt64(out var parsedRequestId))
            {
                requestId = parsedRequestId;
            }

            if (root.TryGetProperty("actionType", out var actionTypeElement) && actionTypeElement.ValueKind == JsonValueKind.String)
            {
                actionType = actionTypeElement.GetString() ?? "(unknown)";
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string truncateForLog(string message)
    {
        if (message.Length <= MaxLoggedMessageLength)
        {
            return message;
        }

        return $"{message.Substring(0, MaxLoggedMessageLength)}...(truncated)";
    }

    private static void logInfo(string message)
    {
        Console.WriteLine($"[ServerWsHost][INFO] {message}");
    }

    private static void logError(string message)
    {
        Console.WriteLine($"[ServerWsHost][ERROR] {message}");
    }
}

