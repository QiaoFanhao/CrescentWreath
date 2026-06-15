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

    public long localPlayerNumericId
    {
        get => viewerPlayerNumericId;
        set => viewerPlayerNumericId = value;
    }

    public long actorPlayerNumericId
    {
        get => viewerPlayerNumericId;
        set => viewerPlayerNumericId = value;
    }

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
        socketClient.Connect(appendViewerToSocketUrl(wsUrl, viewerPlayerNumericId));
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

    public void SendSubmitCharacterSelection(string characterDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(characterDefinitionId))
        {
            OnError?.Invoke("SubmitCharacterSelection requires a non-empty characterDefinitionId.");
            return;
        }

        sendEnvelope(
            "submitCharacterSelection",
            () => buildSubmitCharacterSelectionPayload(characterDefinitionId));
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

    public void SendUseSkill(
        long characterInstanceNumericId,
        string skillKey,
        long? targetCharacterInstanceNumericId = null,
        long? targetAllyCharacterInstanceNumericId = null,
        long? targetPlayerNumericId = null)
    {
        if (characterInstanceNumericId <= 0)
        {
            OnError?.Invoke("UseSkill requires characterInstanceNumericId to be positive.");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillKey))
        {
            OnError?.Invoke("UseSkill requires a non-empty skillKey.");
            return;
        }

        if ((targetCharacterInstanceNumericId.HasValue && targetCharacterInstanceNumericId.Value <= 0) ||
            (targetAllyCharacterInstanceNumericId.HasValue && targetAllyCharacterInstanceNumericId.Value <= 0) ||
            (targetPlayerNumericId.HasValue && targetPlayerNumericId.Value <= 0))
        {
            OnError?.Invoke("UseSkill target numeric ids must be positive when provided.");
            return;
        }

        sendEnvelope(
            "useSkill",
            () => buildUseSkillPayload(
                characterInstanceNumericId,
                skillKey,
                targetCharacterInstanceNumericId,
                targetAllyCharacterInstanceNumericId,
                targetPlayerNumericId));
    }

    public void SendTryResolveAnomaly(long? targetPlayerNumericId)
    {
        if (targetPlayerNumericId.HasValue && targetPlayerNumericId.Value <= 0)
        {
            OnError?.Invoke("TryResolveAnomaly targetPlayerNumericId must be positive when provided.");
            return;
        }

        sendEnvelope(
            "tryResolveAnomaly",
            () => buildTryResolveAnomalyPayload(targetPlayerNumericId));
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
            () => buildSubmitResponseNoPayload(responseWindowNumericId.Value));
    }

    public void SendSubmitResponseNoAsActor(long overrideActorPlayerNumericId)
    {
        _ = overrideActorPlayerNumericId;
        SendSubmitResponseNo();
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
            () => buildSubmitInputChoicePayload(inputContextNumericId.Value, choiceKey));
    }

    public void SendSubmitInputChoices(List<string> choiceKeys)
    {
        var inputContextNumericId = tryResolveInputContextNumericIdForSubmitInputChoice();
        if (!inputContextNumericId.HasValue)
        {
            OnError?.Invoke("SubmitInputChoice requires an active inputContext with inputContextNumericId.");
            return;
        }

        if (choiceKeys is null)
        {
            OnError?.Invoke("SubmitInputChoice requires choiceKeys to be non-null.");
            return;
        }

        sendEnvelope(
            "submitInputChoice",
            () => buildSubmitInputChoicesPayload(inputContextNumericId.Value, choiceKeys));
    }

    public void SendDebugOpenDamageResponseWindow()
    {
        SendDebugOpenDamageResponseWindow(0, 2, "physical");
    }

    public void SendDebugOpenDamageResponseWindow(long targetCharacterInstanceNumericId, int baseDamageValue, string damageTypeKey)
    {
        if (baseDamageValue <= 0)
        {
            OnError?.Invoke("DebugOpenDamageResponseWindow requires baseDamageValue > 0.");
            return;
        }

        if (string.IsNullOrWhiteSpace(damageTypeKey))
        {
            damageTypeKey = "physical";
        }

        sendEnvelope(
            "debugOpenDamageResponseWindow",
            () => buildDebugOpenDamageResponseWindowPayload(targetCharacterInstanceNumericId, baseDamageValue, damageTypeKey));
    }

    public void SendDebugMoveTreasureToHandByDefinition(string treasureDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(treasureDefinitionId))
        {
            OnError?.Invoke("DebugMoveTreasureToHandByDefinition requires a non-empty treasureDefinitionId.");
            return;
        }

        sendEnvelope(
            "debugMoveTreasureToHandByDefinition",
            () => buildDebugTreasureByDefinitionPayload(treasureDefinitionId));
    }

    public void SendDebugPutTreasureOnTopByDefinition(string treasureDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(treasureDefinitionId))
        {
            OnError?.Invoke("DebugPutTreasureOnTopByDefinition requires a non-empty treasureDefinitionId.");
            return;
        }

        sendEnvelope(
            "debugPutTreasureOnTopByDefinition",
            () => buildDebugTreasureByDefinitionPayload(treasureDefinitionId));
    }

    public void SendDebugPutAnomalyOnTopByDefinition(string anomalyDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(anomalyDefinitionId))
        {
            OnError?.Invoke("DebugPutAnomalyOnTopByDefinition requires a non-empty anomalyDefinitionId.");
            return;
        }

        sendEnvelope(
            "debugPutAnomalyOnTopByDefinition",
            () => buildDebugAnomalyByDefinitionPayload(anomalyDefinitionId));
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
        return "{\"actorPlayerNumericId\":" + localPlayerNumericId + "}";
    }

    private string buildSubmitCharacterSelectionPayload(string characterDefinitionId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"characterDefinitionId\":\"" + escapeJsonString(characterDefinitionId.Trim()) + "\""
            + "}";
    }

    private string buildPlayTreasureCardPayload(long cardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"cardInstanceNumericId\":" + cardInstanceId
            + ",\"playMode\":\"normal\""
            + "}";
    }

    private string buildSummonTreasureCardPayload(long cardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"cardInstanceNumericId\":" + cardInstanceId
            + "}";
    }

    private string buildTryResolveAnomalyPayload(long? targetPlayerNumericId)
    {
        var payload =
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId;
        if (targetPlayerNumericId.HasValue)
        {
            payload += ",\"targetPlayerNumericId\":" + targetPlayerNumericId.Value;
        }

        return payload + "}";
    }

    private string buildUseSkillPayload(
        long characterInstanceNumericId,
        string skillKey,
        long? targetCharacterInstanceNumericId,
        long? targetAllyCharacterInstanceNumericId,
        long? targetPlayerNumericId)
    {
        var payload =
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"characterInstanceNumericId\":" + characterInstanceNumericId
            + ",\"skillKey\":\"" + escapeJsonString(skillKey.Trim()) + "\"";

        if (targetCharacterInstanceNumericId.HasValue)
        {
            payload += ",\"targetCharacterInstanceNumericId\":" + targetCharacterInstanceNumericId.Value;
        }

        if (targetAllyCharacterInstanceNumericId.HasValue)
        {
            payload += ",\"targetAllyCharacterInstanceNumericId\":" + targetAllyCharacterInstanceNumericId.Value;
        }

        if (targetPlayerNumericId.HasValue)
        {
            payload += ",\"targetPlayerNumericId\":" + targetPlayerNumericId.Value;
        }

        return payload + "}";
    }

    private string buildDebugAnomalyByDefinitionPayload(string anomalyDefinitionId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"anomalyDefinitionId\":\"" + escapeJsonString(anomalyDefinitionId.Trim()) + "\""
            + "}";
    }

    private string buildSubmitDefenseFixedPayload()
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"defenseTypeKey\":\"fixedReduce1\""
            + ",\"defenseCardInstanceNumericId\":0"
            + "}";
    }

    private string buildSubmitDefenseFormalPayload(string defenseTypeKey, long defenseCardInstanceId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"defenseTypeKey\":\"" + escapeJsonString(defenseTypeKey) + "\""
            + ",\"defenseCardInstanceNumericId\":" + defenseCardInstanceId
            + "}";
    }

    private string buildSubmitResponseNoPayload(long responseWindowNumericId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"responseWindowNumericId\":" + responseWindowNumericId
            + ",\"shouldRespond\":false"
            + ",\"responseKey\":null"
            + "}";
    }

    private string buildDebugOpenDamageResponseWindowPayload(long targetCharacterInstanceNumericId, int baseDamageValue, string damageTypeKey)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"targetCharacterInstanceNumericId\":" + targetCharacterInstanceNumericId
            + ",\"baseDamageValue\":" + baseDamageValue
            + ",\"damageTypeKey\":\"" + escapeJsonString(damageTypeKey) + "\""
            + "}";
    }

    private string buildDebugResetMatchPayload()
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + "}";
    }

    private string buildDebugTreasureByDefinitionPayload(string treasureDefinitionId)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"treasureDefinitionId\":\"" + escapeJsonString(treasureDefinitionId.Trim()) + "\""
            + "}";
    }

    private string buildSubmitInputChoicePayload(long inputContextNumericId, string choiceKey)
    {
        return
            "{"
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
            + ",\"inputContextNumericId\":" + inputContextNumericId
            + ",\"choiceKey\":\"" + escapeJsonString(choiceKey) + "\""
            + ",\"choiceKeys\":[]"
            + "}";
    }

    private string buildSubmitInputChoicesPayload(long inputContextNumericId, List<string> choiceKeys)
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
            + "\"actorPlayerNumericId\":" + localPlayerNumericId
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

    private static string appendViewerToSocketUrl(string wsUrl, long viewerPlayerNumericId)
    {
        var separator = wsUrl.Contains("?") ? "&" : "?";
        return wsUrl + separator + "viewerPlayerNumericId=" + viewerPlayerNumericId;
    }

}
}
