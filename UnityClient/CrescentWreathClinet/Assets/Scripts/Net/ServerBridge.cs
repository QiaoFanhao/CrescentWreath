using System;
using System.Collections.Generic;
using System.Text;
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
    public long? inputContextNumericId { get; set; }
    public int inputChoiceCount { get; set; }
    public long? responseWindowNumericId { get; set; }
    public long? responseCurrentResponderPlayerNumericId { get; set; }
    public int responseResponderCount { get; set; }
}

public sealed class ServerBridge : IDisposable
{
    private readonly ITextSocketClient socketClient;
    private ProjectionViewModel? lastProjectionModel;
    private ProjectionViewModel? latestProjectionForUi;
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
        lastProjectionModel = null;
        latestProjectionForUi = null;
        socketClient.Connect(wsUrl);
    }

    public void Disconnect()
    {
        if (disposed)
        {
            return;
        }

        lastProjectionModel = null;
        latestProjectionForUi = null;
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

    public void SendSubmitResponseNo()
    {
        var responseWindowNumericId = tryResolveResponseWindowNumericIdForSubmitResponseNo();
        if (!responseWindowNumericId.HasValue)
        {
            OnError?.Invoke("SubmitResponseNo requires an active responseWindow with responseWindowNumericId.");
            return;
        }

        sendEnvelope(
            "submitResponse",
            () => buildSubmitResponseNoPayload(actorPlayerNumericId, responseWindowNumericId.Value));
    }

    public void SendSubmitResponseNoAsActor(long overrideActorPlayerNumericId)
    {
        var responseWindowNumericId = tryResolveResponseWindowNumericIdForSubmitResponseNo();
        if (!responseWindowNumericId.HasValue)
        {
            OnError?.Invoke("SubmitResponseNo requires an active responseWindow with responseWindowNumericId.");
            return;
        }

        sendEnvelope(
            "submitResponse",
            () => buildSubmitResponseNoPayload(overrideActorPlayerNumericId, responseWindowNumericId.Value));
    }

    public void SendSubmitInputChoice(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            OnError?.Invoke("SubmitInputChoice requires a non-empty choiceKey.");
            return;
        }

        var inputContextNumericId = tryResolveInputContextNumericIdForSubmitInputChoice();
        if (!inputContextNumericId.HasValue)
        {
            OnError?.Invoke("SubmitInputChoice requires an active inputContext with inputContextNumericId.");
            return;
        }

        sendEnvelope(
            "submitInputChoice",
            () => buildSubmitInputChoicePayload(actorPlayerNumericId, inputContextNumericId.Value, choiceKey));
    }

    public void SendSubmitInputChoices(List<string> choiceKeys)
    {
        var inputContextNumericId = tryResolveInputContextNumericIdForSubmitInputChoice();
        if (!inputContextNumericId.HasValue)
        {
            OnError?.Invoke("SubmitInputChoice requires an active inputContext with inputContextNumericId.");
            return;
        }

        if (choiceKeys is null || choiceKeys.Count == 0)
        {
            OnError?.Invoke("SubmitInputChoice requires at least one choiceKey in choiceKeys.");
            return;
        }

        sendEnvelope(
            "submitInputChoice",
            () => buildSubmitInputChoicesPayload(actorPlayerNumericId, inputContextNumericId.Value, choiceKeys));
    }

    public void SendDebugOpenDamageResponseWindow()
    {
        sendEnvelope(
            "debugOpenDamageResponseWindow",
            () => buildDebugOpenDamageResponseWindowPayload());
    }

    public void SendDebugResetMatch()
    {
        sendEnvelope(
            "debugResetMatch",
            () => buildDebugResetMatchPayload());
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
            inputContextNumericId = parsedProjection.interaction.inputContextNumericId,
            hasResponseWindow = parsedProjection.interaction.hasResponseWindow,
            inputRequiredPlayerNumericId = parsedProjection.interaction.inputRequiredPlayerNumericId,
            inputChoiceCount = parsedProjection.interaction.inputChoiceCount,
            responseWindowNumericId = parsedProjection.interaction.responseWindowNumericId,
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

        latestProjectionForUi = projectionForUi.deepClone();
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

    private string buildSubmitResponseNoPayload(long payloadActorPlayerNumericId, long responseWindowNumericId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + payloadActorPlayerNumericId
            + ",\"responseWindowNumericId\":" + responseWindowNumericId
            + ",\"shouldRespond\":false"
            + ",\"responseKey\":null"
            + "}";
    }

    private string buildDebugOpenDamageResponseWindowPayload()
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + ",\"targetCharacterInstanceNumericId\":0"
            + ",\"baseDamageValue\":2"
            + ",\"damageTypeKey\":\"physical\""
            + "}";
    }

    private string buildDebugResetMatchPayload()
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + actorPlayerNumericId
            + "}";
    }

    private string buildSubmitInputChoicePayload(long payloadActorPlayerNumericId, long inputContextNumericId, string choiceKey)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + payloadActorPlayerNumericId
            + ",\"inputContextNumericId\":" + inputContextNumericId
            + ",\"choiceKey\":\"" + escapeJsonString(choiceKey) + "\""
            + ",\"choiceKeys\":[]"
            + "}";
    }

    private string buildSubmitInputChoicesPayload(long payloadActorPlayerNumericId, long inputContextNumericId, List<string> choiceKeys)
    {
        var choiceKeysJsonBuilder = new StringBuilder();
        choiceKeysJsonBuilder.Append("[");
        var appendedCount = 0;
        foreach (var choiceKey in choiceKeys)
        {
            if (string.IsNullOrWhiteSpace(choiceKey))
            {
                continue;
            }

            if (appendedCount > 0)
            {
                choiceKeysJsonBuilder.Append(",");
            }

            choiceKeysJsonBuilder.Append("\"");
            choiceKeysJsonBuilder.Append(escapeJsonString(choiceKey));
            choiceKeysJsonBuilder.Append("\"");
            appendedCount++;
        }
        choiceKeysJsonBuilder.Append("]");

        return
            "{"
            + "\"actorPlayerNumericId\":" + payloadActorPlayerNumericId
            + ",\"inputContextNumericId\":" + inputContextNumericId
            + ",\"choiceKey\":\"\""
            + ",\"choiceKeys\":" + choiceKeysJsonBuilder
            + "}";
    }

    private long? tryResolveInputContextNumericIdForSubmitInputChoice()
    {
        var projection = latestProjectionForUi;
        if (projection is null ||
            !projection.interaction.hasInputContext ||
            !projection.interaction.inputContextNumericId.HasValue ||
            projection.interaction.inputContextNumericId.Value <= 0)
        {
            return null;
        }

        return projection.interaction.inputContextNumericId.Value;
    }

    private long? tryResolveResponseWindowNumericIdForSubmitResponseNo()
    {
        var projection = latestProjectionForUi;
        if (projection is null ||
            !projection.interaction.hasResponseWindow ||
            !projection.interaction.responseWindowNumericId.HasValue ||
            projection.interaction.responseWindowNumericId.Value <= 0)
        {
            return null;
        }

        return projection.interaction.responseWindowNumericId.Value;
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
