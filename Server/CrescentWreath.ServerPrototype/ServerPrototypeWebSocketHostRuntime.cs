using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerPrototypeWebSocketHostRuntime : IAsyncDisposable
{
    private const int MaxLoggedMessageLength = 280;
    private const int MaxConcurrentConnections = 4;
    private const long MinViewerPlayerNumericId = 1;
    private const long MaxViewerPlayerNumericId = 4;

    private readonly ServerGameSession session;
    private readonly ServerSocketActionRouter actionRouter;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly SemaphoreSlim actionExecutionLock = new(1, 1);
    private readonly object connectionLock = new();
    private readonly Dictionary<long, WebSocket> activeConnectionsByViewerPlayerId = new();

    private HttpListener? listener;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? acceptLoopTask;

    public ServerPrototypeWebSocketHostRuntime()
        : this(ServerGameSession.createStandard2v2())
    {
    }

    public ServerPrototypeWebSocketHostRuntime(ServerGameSession session)
    {
        this.session = session;
        actionRouter = new ServerSocketActionRouter(session);
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
        var prefix = $"http://127.0.0.1:{resolvedPort}/";
        listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        acceptLoopTask = Task.Run(() => acceptLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        return new Uri($"ws://127.0.0.1:{resolvedPort}/ws");
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
            activeConnectionsByViewerPlayerId.Clear();
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

                        var directResponseJson = JsonSerializer.Serialize(routeOutcome.responseEnvelope, serializerOptions);
                        await sendTextAsync(socket, directResponseJson, cancellationToken).ConfigureAwait(false);
                        logInfo(
                            $"Sent response: requestId={routeOutcome.responseEnvelope.requestId}, isSucceeded={routeOutcome.responseEnvelope.isSucceeded}, viewerPlayerNumericId={routeOutcome.responseEnvelope.viewerPlayerNumericId}, bytes={Encoding.UTF8.GetByteCount(directResponseJson)}.");

                        if (routeOutcome.actionResult is { isSucceeded: true } actionResult)
                        {
                            await broadcastSuccessfulActionAsync(
                                viewerPlayerNumericId,
                                routeOutcome.actionType,
                                actionResult,
                                cancellationToken).ConfigureAwait(false);
                        }
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
            return activeConnectionsByViewerPlayerId.Count;
        }
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
