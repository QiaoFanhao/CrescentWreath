using System;
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
    private readonly ServerGameSession session;
    private readonly ServerSocketActionRouter actionRouter;
    private readonly JsonSerializerOptions serializerOptions;

    private HttpListener? listener;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? acceptLoopTask;
    private int hasActiveWebSocketConnection;

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
        hasActiveWebSocketConnection = 0;
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
            if (!string.Equals(context.Request.Url?.AbsolutePath, "/ws", StringComparison.Ordinal))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            if (Interlocked.CompareExchange(ref hasActiveWebSocketConnection, 1, 0) != 0)
            {
                context.Response.StatusCode = 409;
                context.Response.Close();
                return;
            }

            WebSocket? socket = null;
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                socket = webSocketContext.WebSocket;
                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var requestMessage = await readTextMessageAsync(socket, cancellationToken).ConfigureAwait(false);
                    if (requestMessage is null)
                    {
                        break;
                    }

                    var responseEnvelope = actionRouter.routeMessage(requestMessage);
                    var responseJson = JsonSerializer.Serialize(responseEnvelope, serializerOptions);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await socket.SendAsync(
                        new ArraySegment<byte>(responseBytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Interlocked.Exchange(ref hasActiveWebSocketConnection, 0);
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
            }
        }
        catch
        {
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
}
