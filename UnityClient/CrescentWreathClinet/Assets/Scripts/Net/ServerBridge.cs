using System;
using UnityEngine;

namespace CrescentWreath.Client.Net
{
public sealed class ServerResponseSummary
{
    public long? viewerPlayerNumericId { get; set; }
    public bool isSucceeded { get; set; }
    public string errorCode { get; set; } = string.Empty;
    public string errorMessage { get; set; } = string.Empty;
    public string currentPhase { get; set; } = string.Empty;
    public long? currentPlayerNumericId { get; set; }
    public int myHandCount { get; set; }
    public string recentEventTypeKey { get; set; } = string.Empty;
    public bool hasInputContext { get; set; }
    public bool hasResponseWindow { get; set; }
    public long? inputRequiredPlayerNumericId { get; set; }
    public int inputChoiceCount { get; set; }
    public long? responseCurrentResponderPlayerNumericId { get; set; }
    public int responseResponderCount { get; set; }
}

public sealed class ServerBridge : IDisposable
{
    private readonly ITextSocketClient socketClient;
    private ProjectionViewModel? lastProjectionModel;
    private long nextRequestId = 1;
    private bool disposed;

    public long viewerPlayerNumericId { get; set; } = 1;
    public long actorPlayerNumericId { get; set; } = 1;

    public event Action<string>? OnRawRequest;
    public event Action<string>? OnRawResponse;
    public event Action<ServerResponseSummary>? OnSummaryUpdated;
    public event Action<ProjectionViewModel>? OnProjectionUpdated;
    public event Action<string>? OnConnectionStateChanged;
    public event Action<string>? OnError;

    public ServerBridge()
        : this(new SocketClient())
    {
    }

    public ServerBridge(ITextSocketClient socketClient)
    {
        this.socketClient = socketClient;
        this.socketClient.OnConnected += handleConnected;
        this.socketClient.OnDisconnected += handleDisconnected;
        this.socketClient.OnTextMessage += handleTextMessage;
        this.socketClient.OnError += handleSocketError;
    }

    public bool isConnected => socketClient.isConnected;

    public void Connect(string wsUrl)
    {
        throwIfDisposed();
        socketClient.Connect(wsUrl);
    }

    public void Disconnect()
    {
        if (disposed)
        {
            return;
        }

        socketClient.Disconnect();
    }

    public void SendDrawOneCard()
    {
        sendEnvelope(
            "drawOneCard",
            () => buildActorOnlyPayload());
    }

    public void SendPlayTreasureCard(long cardInstanceId)
    {
        sendEnvelope(
            "playTreasureCard",
            () => buildPlayTreasureCardPayload(cardInstanceId));
    }

    public void SendEnterActionPhase()
    {
        sendEnvelope(
            "enterActionPhase",
            () => buildActorOnlyPayload());
    }

    public void SendEnterSummonPhase()
    {
        sendEnvelope(
            "enterSummonPhase",
            () => buildActorOnlyPayload());
    }

    public void SendSummonTreasureCard(long cardInstanceId)
    {
        sendEnvelope(
            "summonTreasureCard",
            () => buildSummonTreasureCardPayload(cardInstanceId));
    }

    public void SendEnterEndPhase()
    {
        sendEnvelope(
            "enterEndPhase",
            () => buildActorOnlyPayload());
    }

    public void SendStartNextTurn()
    {
        sendEnvelope(
            "startNextTurn",
            () => buildActorOnlyPayload());
    }

    public void SendSubmitDefenseFixedReduce1()
    {
        sendEnvelope(
            "submitDefense",
            () => buildSubmitDefenseFixedPayload());
    }

    public void SendSubmitDefenseFormal(string defenseTypeKey, long defenseCardInstanceId)
    {
        sendEnvelope(
            "submitDefense",
            () => buildSubmitDefenseFormalPayload(defenseTypeKey, defenseCardInstanceId));
    }

    public static ServerResponseSummary ParseSummary(string rawJson, long fallbackViewerPlayerNumericId)
    {
        var parsedProjection = ProjectionParser.Parse(rawJson, fallbackViewerPlayerNumericId);
        return new ServerResponseSummary
        {
            viewerPlayerNumericId = parsedProjection.viewerPlayerNumericId,
            isSucceeded = parsedProjection.isSucceeded,
            errorCode = parsedProjection.errorCode,
            errorMessage = parsedProjection.errorMessage,
            currentPhase = parsedProjection.currentPhase,
            currentPlayerNumericId = parsedProjection.currentPlayerNumericId,
            myHandCount = parsedProjection.viewerHandCardCount,
            recentEventTypeKey = parsedProjection.recentEventTypeKey,
            hasInputContext = parsedProjection.interaction.hasInputContext,
            hasResponseWindow = parsedProjection.interaction.hasResponseWindow,
            inputRequiredPlayerNumericId = parsedProjection.interaction.inputRequiredPlayerNumericId,
            inputChoiceCount = parsedProjection.interaction.inputChoiceCount,
            responseCurrentResponderPlayerNumericId = parsedProjection.interaction.responseCurrentResponderPlayerNumericId,
            responseResponderCount = parsedProjection.interaction.responseResponderCount,
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        socketClient.OnConnected -= handleConnected;
        socketClient.OnDisconnected -= handleDisconnected;
        socketClient.OnTextMessage -= handleTextMessage;
        socketClient.OnError -= handleSocketError;
        socketClient.Dispose();
    }

    private void sendEnvelope(string actionType, Func<string> buildPayload)
    {
        throwIfDisposed();
        var requestId = nextRequestId++;
        var messageJson =
            "{"
            + "\"requestId\":" + requestId
            + ",\"viewerPlayerNumericId\":" + viewerPlayerNumericId
            + ",\"actionType\":\"" + escapeJsonString(actionType) + "\""
            + ",\"payload\":" + buildPayload()
            + "}";
        OnRawRequest?.Invoke(messageJson);
        socketClient.SendText(messageJson);
    }

    private void handleConnected()
    {
        OnConnectionStateChanged?.Invoke("connected");
    }

    private void handleDisconnected(string reason)
    {
        OnConnectionStateChanged?.Invoke("disconnected:" + reason);
    }

    private void handleTextMessage(string rawJson)
    {
        OnRawResponse?.Invoke(rawJson);
        var summary = ParseSummary(rawJson, viewerPlayerNumericId);
        OnSummaryUpdated?.Invoke(summary);

        var parsedProjection = ProjectionParser.Parse(rawJson, viewerPlayerNumericId);
        ProjectionViewModel projectionForUi;
        if (parsedProjection.isSucceeded && parsedProjection.hasStateProjection)
        {
            lastProjectionModel = parsedProjection.deepClone();
            projectionForUi = lastProjectionModel.deepClone();
        }
        else if (lastProjectionModel is not null)
        {
            projectionForUi = ProjectionViewModel.mergeLatestWithIncomingFailure(lastProjectionModel, parsedProjection);
        }
        else
        {
            if (parsedProjection.hasStateProjection)
            {
                lastProjectionModel = parsedProjection.deepClone();
            }

            projectionForUi = parsedProjection;
        }

        OnProjectionUpdated?.Invoke(projectionForUi);
    }

    private void handleSocketError(string error)
    {
        OnError?.Invoke(error);
    }

    private void throwIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ServerBridge));
        }
    }

    private string buildActorOnlyPayload()
    {
        return "{\"actorPlayerNumericId\":" + actorPlayerNumericId + "}";
    }

    private string buildPlayTreasureCardPayload(long cardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + ",\"cardInstanceNumericId\":" + cardInstanceId
            + ",\"playMode\":\"normal\""
            + "}";
    }

    private string buildSummonTreasureCardPayload(long cardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + ",\"cardInstanceNumericId\":" + cardInstanceId
            + "}";
    }

    private string buildSubmitDefenseFixedPayload()
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + ",\"defenseTypeKey\":\"fixedReduce1\""
            + ",\"defenseCardInstanceNumericId\":0"
            + "}";
    }

    private string buildSubmitDefenseFormalPayload(string defenseTypeKey, long defenseCardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + ",\"defenseTypeKey\":\"" + escapeJsonString(defenseTypeKey) + "\""
            + ",\"defenseCardInstanceNumericId\":" + defenseCardInstanceId
            + "}";
    }

    private static string escapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

}
}
