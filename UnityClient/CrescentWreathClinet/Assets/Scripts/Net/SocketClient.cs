using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CrescentWreath.Client.Net
{

public interface ITextSocketClient : IDisposable
{
    event Action? OnConnected;
    event Action<string>? OnDisconnected;
    event Action<string>? OnTextMessage;
    event Action<string>? OnError;

    bool isConnected { get; }

    void Connect(string url);
    void Disconnect();
    void SendText(string text);
}

public sealed class SocketClient : ITextSocketClient
{
    private readonly object syncRoot = new();
    private ITextSocketBackend? backend;
    private bool disposed;
    private bool connected;

    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<string>? OnTextMessage;
    public event Action<string>? OnError;

    public bool isConnected
    {
        get
        {
            lock (syncRoot)
            {
                return connected;
            }
        }
    }

    public void Connect(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            emitError("Socket URL is empty.");
            return;
        }

        lock (syncRoot)
        {
            throwIfDisposed();
            if (backend is not null)
            {
                emitError("Socket is already connecting or connected.");
                return;
            }

            var bestPackageFound = false;
            var fallbackReason = string.Empty;
            if (BestHttpWebSocketBackend.TryCreate(
                    url,
                    handleConnected,
                    handleDisconnected,
                    handleTextMessage,
                    handleError,
                    out var bestHttpBackend))
            {
                bestPackageFound = true;
                backend = bestHttpBackend;
                backend.Connect();
                if (!bestHttpBackend.hasFatalSetupFailure)
                {
                    return;
                }

                backend.Dispose();
                backend = null;
                fallbackReason = "Best.WebSockets callback binding failed. Fallback to ClientWebSocket backend.";
            }

            backend = new NativeClientWebSocketBackend(
                url,
                handleConnected,
                handleDisconnected,
                handleTextMessage,
                handleError);
            backend.Connect();
            if (!bestPackageFound)
            {
                handleError("Best.WebSockets package not found. Fallback to ClientWebSocket backend.");
                return;
            }

            if (!string.IsNullOrEmpty(fallbackReason))
            {
                handleError(fallbackReason);
            }
        }
    }

    public void Disconnect()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            backend?.Disconnect();
            backend?.Dispose();
            backend = null;
            connected = false;
        }
    }

    public void SendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            emitError("Message text is empty.");
            return;
        }

        lock (syncRoot)
        {
            throwIfDisposed();
            if (backend is null)
            {
                emitError("Socket is not connected.");
                return;
            }

            backend.SendText(text);
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            backend?.Disconnect();
            backend?.Dispose();
            backend = null;
            connected = false;
        }
    }

    private void handleConnected()
    {
        lock (syncRoot)
        {
            connected = true;
        }

        OnConnected?.Invoke();
    }

    private void handleDisconnected(string reason)
    {
        lock (syncRoot)
        {
            connected = false;
            backend?.Dispose();
            backend = null;
        }

        OnDisconnected?.Invoke(reason);
    }

    private void handleTextMessage(string message)
    {
        OnTextMessage?.Invoke(message);
    }

    private void handleError(string error)
    {
        OnError?.Invoke(error);
    }

    private void emitError(string message)
    {
        OnError?.Invoke(message);
    }

    private void throwIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SocketClient));
        }
    }
}

internal interface ITextSocketBackend : IDisposable
{
    void Connect();
    void Disconnect();
    void SendText(string text);
}

internal sealed class NativeClientWebSocketBackend : ITextSocketBackend
{
    private readonly Uri serverUri;
    private readonly Action onConnected;
    private readonly Action<string> onDisconnected;
    private readonly Action<string> onTextMessage;
    private readonly Action<string> onError;

    private ClientWebSocket? socket;
    private CancellationTokenSource? cancellationTokenSource;

    public NativeClientWebSocketBackend(
        string url,
        Action onConnected,
        Action<string> onDisconnected,
        Action<string> onTextMessage,
        Action<string> onError)
    {
        serverUri = new Uri(url);
        this.onConnected = onConnected;
        this.onDisconnected = onDisconnected;
        this.onTextMessage = onTextMessage;
        this.onError = onError;
    }

    public void Connect()
    {
        socket = new ClientWebSocket();
        cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(connectAndReceiveLoopAsync);
    }

    public void Disconnect()
    {
        if (socket is null)
        {
            return;
        }

        try
        {
            cancellationTokenSource?.Cancel();
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client_disconnect", CancellationToken.None).Wait();
            }
        }
        catch (Exception exception)
        {
            onError($"Native websocket close failed: {exception.Message}");
        }
    }

    public void SendText(string text)
    {
        if (socket is null || socket.State != WebSocketState.Open)
        {
            onError("Socket isn't open.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception exception)
            {
                onError($"Native websocket send failed: {exception.Message}");
            }
        });
    }

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;

        socket?.Dispose();
        socket = null;
    }

    private async Task connectAndReceiveLoopAsync()
    {
        if (socket is null)
        {
            return;
        }

        try
        {
            await socket.ConnectAsync(serverUri, cancellationTokenSource?.Token ?? CancellationToken.None);
            onConnected();

            var buffer = new byte[4096];
            while (socket.State == WebSocketState.Open)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource?.Token ?? CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    onDisconnected("server_closed");
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                onTextMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            onDisconnected("cancelled");
        }
        catch (Exception exception)
        {
            onError($"Native websocket connect/receive failed: {exception.Message}");
            onDisconnected("connect_failed");
        }
    }
}

internal sealed class BestHttpWebSocketBackend : ITextSocketBackend
{
    private readonly Type webSocketType;
    private readonly string url;
    private readonly Action onConnected;
    private readonly Action<string> onDisconnected;
    private readonly Action<string> onTextMessage;
    private readonly Action<string> onError;

    private object? socketInstance;
    private readonly List<(EventInfoProxy callbackBinding, Delegate handler)> subscriptions = new();

    internal bool hasFatalSetupFailure { get; private set; }

    private sealed class EventInfoProxy
    {
        public string Name { get; set; } = string.Empty;
        public Action<object, Delegate> AddHandler { get; set; } = (_, _) => { };
        public Action<object, Delegate> RemoveHandler { get; set; } = (_, _) => { };
        public Type HandlerType { get; set; } = typeof(Delegate);
    }

    private BestHttpWebSocketBackend(
        Type webSocketType,
        string url,
        Action onConnected,
        Action<string> onDisconnected,
        Action<string> onTextMessage,
        Action<string> onError)
    {
        this.webSocketType = webSocketType;
        this.url = url;
        this.onConnected = onConnected;
        this.onDisconnected = onDisconnected;
        this.onTextMessage = onTextMessage;
        this.onError = onError;
    }

    public static bool TryCreate(
        string url,
        Action onConnected,
        Action<string> onDisconnected,
        Action<string> onTextMessage,
        Action<string> onError,
        out BestHttpWebSocketBackend? backend)
    {
        backend = null;
        var webSocketType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Best.WebSockets.WebSocket", false))
            .FirstOrDefault(type => type is not null);

        if (webSocketType is null)
        {
            return false;
        }

        backend = new BestHttpWebSocketBackend(
            webSocketType,
            url,
            onConnected,
            onDisconnected,
            onTextMessage,
            onError);
        return true;
    }

    public void Connect()
    {
        try
        {
            hasFatalSetupFailure = false;
            socketInstance = Activator.CreateInstance(webSocketType, new Uri(url));
            if (socketInstance is null)
            {
                hasFatalSetupFailure = true;
                onError("Failed to create Best.WebSockets.WebSocket instance.");
                return;
            }

            var openBound = subscribeCallback("OnOpen", _ => onConnected());
            var messageBound = subscribeCallback("OnMessage", args =>
            {
                var message = args.Length > 1 ? args[1]?.ToString() ?? string.Empty : string.Empty;
                onTextMessage(message);
            });
            var closedBound = subscribeCallback("OnClosed", args =>
            {
                var reason = args.Length > 2 ? args[2]?.ToString() ?? "closed" : "closed";
                onDisconnected(reason);
            });

            if (!openBound || !messageBound || !closedBound)
            {
                hasFatalSetupFailure = true;
                onError("Best.WebSockets.WebSocket callback binding failed. Required callbacks: OnOpen, OnMessage, OnClosed.");
                removeEventSubscriptions();
                socketInstance = null;
                return;
            }

            var openMethod = webSocketType.GetMethod("Open", Type.EmptyTypes);
            if (openMethod is null)
            {
                hasFatalSetupFailure = true;
                onError("Best.WebSockets.WebSocket.Open() not found.");
                return;
            }

            openMethod.Invoke(socketInstance, null);
        }
        catch (Exception exception)
        {
            hasFatalSetupFailure = true;
            onError($"Best websocket connect failed: {exception.Message}");
            onDisconnected("connect_failed");
        }
    }

    public void Disconnect()
    {
        if (socketInstance is null)
        {
            return;
        }

        try
        {
            var closeMethod = webSocketType.GetMethod("Close", Type.EmptyTypes);
            closeMethod?.Invoke(socketInstance, null);
        }
        catch (Exception exception)
        {
            onError($"Best websocket close failed: {exception.Message}");
        }

        removeEventSubscriptions();
        socketInstance = null;
    }

    public void SendText(string text)
    {
        if (socketInstance is null)
        {
            onError("Socket isn't open.");
            return;
        }

        try
        {
            var sendMethod = webSocketType.GetMethod("Send", new[] { typeof(string) });
            if (sendMethod is null)
            {
                onError("Best.WebSockets.WebSocket.Send(string) not found.");
                return;
            }

            sendMethod.Invoke(socketInstance, new object[] { text });
        }
        catch (Exception exception)
        {
            onError($"Best websocket send failed: {exception.Message}");
        }
    }

    public void Dispose()
    {
        removeEventSubscriptions();
        socketInstance = null;
    }

    private bool subscribeCallback(string callbackName, Action<object?[]> handler)
    {
        if (socketInstance is null)
        {
            return false;
        }

        if (!tryCreateCallbackBinding(callbackName, out var callbackBinding) || callbackBinding is null)
        {
            return false;
        }

        var adapter = createAdapter(callbackBinding.HandlerType, handler);
        callbackBinding.AddHandler(socketInstance, adapter);
        subscriptions.Add((callbackBinding, adapter));
        return true;
    }

    private bool tryCreateCallbackBinding(string callbackName, out EventInfoProxy? callbackBinding)
    {
        callbackBinding = null;

        var eventInfo = webSocketType.GetEvent(callbackName);
        if (eventInfo?.EventHandlerType is not null)
        {
            callbackBinding = new EventInfoProxy
            {
                Name = callbackName,
                HandlerType = eventInfo.EventHandlerType,
                AddHandler = (target, eventHandler) => eventInfo.AddEventHandler(target, eventHandler),
                RemoveHandler = (target, eventHandler) => eventInfo.RemoveEventHandler(target, eventHandler),
            };
            return true;
        }

        var fieldInfo = webSocketType.GetField(callbackName, BindingFlags.Instance | BindingFlags.Public);
        if (fieldInfo?.FieldType is null || !typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
        {
            return false;
        }

        callbackBinding = new EventInfoProxy
        {
            Name = callbackName,
            HandlerType = fieldInfo.FieldType,
            AddHandler = (target, eventHandler) =>
            {
                var existingHandler = fieldInfo.GetValue(target) as Delegate;
                var combinedHandler = Delegate.Combine(existingHandler, eventHandler);
                fieldInfo.SetValue(target, combinedHandler);
            },
            RemoveHandler = (target, eventHandler) =>
            {
                var existingHandler = fieldInfo.GetValue(target) as Delegate;
                if (existingHandler is null)
                {
                    return;
                }

                var removedHandler = Delegate.Remove(existingHandler, eventHandler);
                fieldInfo.SetValue(target, removedHandler);
            },
        };
        return true;
    }

    private void removeEventSubscriptions()
    {
        if (socketInstance is null)
        {
            subscriptions.Clear();
            return;
        }

        foreach (var subscription in subscriptions)
        {
            try
            {
                subscription.callbackBinding.RemoveHandler(socketInstance, subscription.handler);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to unsubscribe websocket event {subscription.callbackBinding.Name}: {exception.Message}");
            }
        }

        subscriptions.Clear();
    }

    private static Delegate createAdapter(Type eventHandlerType, Action<object?[]> handler)
    {
        var invokeMethod = eventHandlerType.GetMethod("Invoke")
                           ?? throw new InvalidOperationException($"Event handler type {eventHandlerType.FullName} has no Invoke method.");
        var parameters = invokeMethod.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();

        var objectArray = Expression.NewArrayInit(
            typeof(object),
            parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));

        var body = Expression.Call(
            Expression.Constant(handler),
            typeof(Action<object?[]>).GetMethod(nameof(Action<object?[]>.Invoke))!,
            objectArray);

        return Expression.Lambda(eventHandlerType, body, parameters).Compile();
    }
}
}
