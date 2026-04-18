using System;
using System.Text.Json;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerSocketActionRouter
{
    public const string ErrorCodeInvalidRequestEnvelope = "invalid_request_envelope";
    public const string ErrorCodeUnsupportedActionType = "unsupported_action_type";
    public const string ErrorCodeInvalidPayload = "invalid_payload";

    private readonly ServerGameSession session;
    private readonly JsonSerializerOptions serializerOptions;

    public ServerSocketActionRouter(ServerGameSession session)
    {
        this.session = session;
        serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public ServerSocketResponseEnvelope routeMessage(string messageJson)
    {
        if (!tryDeserializeEnvelope(messageJson, out var envelope))
        {
            return buildHostErrorResponse(0, 0, ErrorCodeInvalidRequestEnvelope, "Request envelope is invalid JSON.");
        }

        if (string.IsNullOrWhiteSpace(envelope.actionType))
        {
            return buildHostErrorResponse(
                envelope.requestId,
                envelope.viewerPlayerNumericId,
                ErrorCodeInvalidRequestEnvelope,
                "Request envelope requires a non-empty actionType.");
        }

        return envelope.actionType switch
        {
            "drawOneCard" => routeByPayload<ServerDrawOneCardRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processDrawOneCard(dto);
                }),
            "playTreasureCard" => routeByPayload<ServerPlayTreasureCardRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.cardInstanceNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.playMode),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processPlayTreasureCard(dto);
                }),
            "enterSummonPhase" => routeByPayload<ServerEnterSummonPhaseRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterSummonPhase(dto);
                }),
            "summonTreasureCard" => routeByPayload<ServerSummonTreasureCardRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.cardInstanceNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSummonTreasureCard(dto);
                }),
            "enterEndPhase" => routeByPayload<ServerEnterEndPhaseRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterEndPhase(dto);
                }),
            "startNextTurn" => routeByPayload<ServerStartNextTurnRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processStartNextTurn(dto);
                }),
            "enterActionPhase" => routeByPayload<ServerEnterActionPhaseRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterActionPhase(dto);
                }),
            "useSkill" => routeByPayload<ServerUseSkillRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.characterInstanceNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.skillKey),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processUseSkill(dto);
                }),
            "submitDefense" => routeByPayload<ServerSubmitDefenseRequestDto>(
                envelope,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.defenseTypeKey) &&
                       (string.Equals(dto.defenseTypeKey, "fixedReduce1", StringComparison.Ordinal) || dto.defenseCardInstanceNumericId > 0),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSubmitDefense(dto);
                }),
            _ => buildHostErrorResponse(
                envelope.requestId,
                envelope.viewerPlayerNumericId,
                ErrorCodeUnsupportedActionType,
                $"Unsupported actionType: {envelope.actionType}"),
        };
    }

    private ServerSocketResponseEnvelope routeByPayload<TPayload>(
        ServerSocketRequestEnvelope envelope,
        Func<TPayload, bool> payloadValidator,
        Func<TPayload, ServerActionProcessResult> processAction)
        where TPayload : class
    {
        if (!tryDeserializePayload(envelope.payload, out TPayload? payload) || payload is null || !payloadValidator(payload))
        {
            return buildHostErrorResponse(
                envelope.requestId,
                envelope.viewerPlayerNumericId,
                ErrorCodeInvalidPayload,
                $"Payload is invalid for actionType: {envelope.actionType}");
        }

        var actionResult = processAction(payload);
        var viewerScopedResult = session.projectResultForViewer(actionResult, envelope.viewerPlayerNumericId);
        return convertToSocketResponse(viewerScopedResult);
    }

    private ServerSocketResponseEnvelope convertToSocketResponse(ServerActionProcessResult actionResult)
    {
        return new ServerSocketResponseEnvelope
        {
            requestId = actionResult.requestId,
            viewerPlayerNumericId = actionResult.viewerPlayerNumericId,
            isSucceeded = actionResult.isSucceeded,
            error = actionResult.error,
            stateProjection = actionResult.stateProjection,
            eventLog = actionResult.eventLog,
            interaction = actionResult.interaction,
        };
    }

    private ServerSocketResponseEnvelope buildHostErrorResponse(
        long requestId,
        long viewerPlayerNumericId,
        string errorCode,
        string errorMessage)
    {
        var resolvedViewerPlayerId = ServerProjectionBuilder.resolveViewerPlayerId(session.gameState, viewerPlayerNumericId);
        return new ServerSocketResponseEnvelope
        {
            requestId = requestId,
            viewerPlayerNumericId = resolvedViewerPlayerId.Value,
            isSucceeded = false,
            error = new ServerErrorProjection
            {
                code = errorCode,
                message = errorMessage,
            },
            stateProjection = ServerProjectionBuilder.buildStateProjection(session.gameState, resolvedViewerPlayerId),
            eventLog = new(),
            interaction = ServerProjectionBuilder.buildInteractionProjection(session.gameState, resolvedViewerPlayerId),
        };
    }

    private bool tryDeserializeEnvelope(string messageJson, out ServerSocketRequestEnvelope envelope)
    {
        envelope = new ServerSocketRequestEnvelope();
        try
        {
            var parsedEnvelope = JsonSerializer.Deserialize<ServerSocketRequestEnvelope>(messageJson, serializerOptions);
            if (parsedEnvelope is null)
            {
                return false;
            }

            envelope = parsedEnvelope;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool tryDeserializePayload<TPayload>(JsonElement payloadElement, out TPayload? payload)
        where TPayload : class
    {
        payload = null;
        if (payloadElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(payloadElement.GetRawText(), serializerOptions);
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
