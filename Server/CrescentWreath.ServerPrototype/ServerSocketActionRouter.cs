using System;
using System.Text.Json;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerSocketRouteOptions
{
    public long? forcedViewerPlayerNumericId { get; init; }
    public long? requiredActorPlayerNumericId { get; init; }
}

public sealed class ServerSocketRouteOutcome
{
    public string actionType { get; set; } = string.Empty;
    public long resolvedViewerPlayerNumericId { get; set; }
    public ServerSocketResponseEnvelope responseEnvelope { get; set; } = new();
    public ServerActionProcessResult? actionResult { get; set; }
}

public sealed class ServerSocketActionRouter
{
    public const string ErrorCodeInvalidRequestEnvelope = "invalid_request_envelope";
    public const string ErrorCodeUnsupportedActionType = "unsupported_action_type";
    public const string ErrorCodeInvalidPayload = "invalid_payload";
    public const string ErrorCodeRequestRejected = "request_rejected";

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
        return routeMessageDetailed(messageJson).responseEnvelope;
    }

    public ServerSocketRouteOutcome routeMessageDetailed(
        string messageJson,
        ServerSocketRouteOptions? routeOptions = null)
    {
        if (!tryDeserializeEnvelope(messageJson, out var envelope))
        {
            return buildHostErrorOutcome(
                actionType: string.Empty,
                requestId: 0,
                viewerPlayerNumericId: routeOptions?.forcedViewerPlayerNumericId ?? 0,
                ErrorCodeInvalidRequestEnvelope,
                "Request envelope is invalid JSON.");
        }

        var actionType = envelope.actionType ?? string.Empty;
        var resolvedViewerPlayerNumericId = routeOptions?.forcedViewerPlayerNumericId ?? envelope.viewerPlayerNumericId;
        if (string.IsNullOrWhiteSpace(actionType))
        {
            return buildHostErrorOutcome(
                actionType,
                envelope.requestId,
                resolvedViewerPlayerNumericId,
                ErrorCodeInvalidRequestEnvelope,
                "Request envelope requires a non-empty actionType.");
        }

        var requiredActorPlayerNumericId = routeOptions?.requiredActorPlayerNumericId;

        return actionType switch
        {
            "submitCharacterSelection" => routeByPayload<ServerSubmitCharacterSelectionRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.characterDefinitionId),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSubmitCharacterSelection(dto);
                }),
            "drawOneCard" => routeByPayload<ServerDrawOneCardRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processDrawOneCard(dto);
                }),
            "playTreasureCard" => routeByPayload<ServerPlayTreasureCardRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
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
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterSummonPhase(dto);
                }),
            "summonTreasureCard" => routeByPayload<ServerSummonTreasureCardRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.cardInstanceNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSummonTreasureCard(dto);
                }),
            "enterEndPhase" => routeByPayload<ServerEnterEndPhaseRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterEndPhase(dto);
                }),
            "startNextTurn" => routeByPayload<ServerStartNextTurnRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processStartNextTurn(dto);
                }),
            "enterActionPhase" => routeByPayload<ServerEnterActionPhaseRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processEnterActionPhase(dto);
                }),
            "useSkill" => routeByPayload<ServerUseSkillRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.characterInstanceNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.skillKey) &&
                       (!dto.targetCharacterInstanceNumericId.HasValue ||
                        dto.targetCharacterInstanceNumericId.Value > 0) &&
                       (!dto.targetAllyCharacterInstanceNumericId.HasValue ||
                        dto.targetAllyCharacterInstanceNumericId.Value > 0) &&
                       (!dto.targetPlayerNumericId.HasValue ||
                        dto.targetPlayerNumericId.Value > 0),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processUseSkill(dto);
                }),
            "tryResolveAnomaly" => routeByPayload<ServerTryResolveAnomalyRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       (!dto.targetPlayerNumericId.HasValue || dto.targetPlayerNumericId.Value >= 0),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processTryResolveAnomaly(dto);
                }),
            "submitDefense" => routeByPayload<ServerSubmitDefenseRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.defenseTypeKey) &&
                       (string.Equals(dto.defenseTypeKey, "fixedReduce1", StringComparison.Ordinal) || dto.defenseCardInstanceNumericId > 0),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSubmitDefense(dto);
                }),
            "submitResponse" => routeByPayload<ServerSubmitResponseRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.responseWindowNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSubmitResponse(dto);
                }),
            "submitInputChoice" => routeByPayload<ServerSubmitInputChoiceRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.inputContextNumericId > 0 &&
                       hasValidSubmitInputChoicePayload(dto),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.processSubmitInputChoice(dto);
                }),
            // Debug-only route used by Unity debug panel to reinitialize the current single match session.
            "debugResetMatch" => routeByPayload<ServerDebugResetMatchRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.debugResetMatch(dto);
                }),
            // Debug-only route used by Unity debug panel to create a staged damage response window for submitResponse smoke tests.
            "debugOpenDamageResponseWindow" => routeByPayload<ServerDebugOpenDamageResponseWindowRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       dto.targetCharacterInstanceNumericId >= 0 &&
                       dto.baseDamageValue >= 0,
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.debugOpenDamageResponseWindow(dto);
                }),
            // Debug-only route used by Unity debug panel to inject one treasure by definitionId from publicTreasureDeck into actor hand.
            "debugMoveTreasureToHandByDefinition" => routeByPayload<ServerDebugMoveTreasureToHandByDefinitionRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.treasureDefinitionId),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.debugMoveTreasureToHandByDefinition(dto);
                }),
            // Debug-only route used by Unity debug panel to put one treasure by definitionId to the top of publicTreasureDeck.
            "debugPutTreasureOnTopByDefinition" => routeByPayload<ServerDebugPutTreasureOnTopByDefinitionRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.treasureDefinitionId),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.debugPutTreasureOnTopByDefinition(dto);
                }),
            // Debug-only route used by Unity debug panel to put one unopened anomaly by definitionId to the top of anomalyDeck.
            "debugPutAnomalyOnTopByDefinition" => routeByPayload<ServerDebugPutAnomalyOnTopByDefinitionRequestDto>(
                envelope,
                actionType,
                resolvedViewerPlayerNumericId,
                requiredActorPlayerNumericId,
                dto => dto.actorPlayerNumericId > 0 &&
                       !string.IsNullOrWhiteSpace(dto.anomalyDefinitionId),
                dto =>
                {
                    dto.requestId = envelope.requestId;
                    return session.debugPutAnomalyOnTopByDefinition(dto);
                }),
            _ => buildHostErrorOutcome(
                actionType,
                envelope.requestId,
                resolvedViewerPlayerNumericId,
                ErrorCodeUnsupportedActionType,
                $"Unsupported actionType: {actionType}"),
        };
    }

    public static ServerSocketResponseEnvelope convertActionResultToSocketResponse(ServerActionProcessResult actionResult)
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

    private ServerSocketRouteOutcome routeByPayload<TPayload>(
        ServerSocketRequestEnvelope envelope,
        string actionType,
        long resolvedViewerPlayerNumericId,
        long? requiredActorPlayerNumericId,
        Func<TPayload, bool> payloadValidator,
        Func<TPayload, ServerActionProcessResult> processAction)
        where TPayload : class
    {
        if (!tryDeserializePayload(envelope.payload, out TPayload? payload) || payload is null || !payloadValidator(payload))
        {
            return buildHostErrorOutcome(
                actionType,
                envelope.requestId,
                resolvedViewerPlayerNumericId,
                ErrorCodeInvalidPayload,
                $"Payload is invalid for actionType: {actionType}");
        }

        if (requiredActorPlayerNumericId.HasValue)
        {
            if (!tryResolveActorPlayerNumericId(payload, out var actorPlayerNumericId))
            {
                return buildHostErrorOutcome(
                    actionType,
                    envelope.requestId,
                    resolvedViewerPlayerNumericId,
                    ErrorCodeInvalidPayload,
                    $"Payload is missing actorPlayerNumericId for actionType: {actionType}");
            }

            if (actorPlayerNumericId != requiredActorPlayerNumericId.Value)
            {
                return buildHostErrorOutcome(
                    actionType,
                    envelope.requestId,
                    resolvedViewerPlayerNumericId,
                    ErrorCodeRequestRejected,
                    $"actorPlayerNumericId ({actorPlayerNumericId}) must match bound viewerPlayerNumericId ({requiredActorPlayerNumericId.Value}).");
            }
        }

        var actionResult = processAction(payload);
        var viewerScopedResult = session.projectResultForViewer(actionResult, resolvedViewerPlayerNumericId);
        return new ServerSocketRouteOutcome
        {
            actionType = actionType,
            resolvedViewerPlayerNumericId = viewerScopedResult.viewerPlayerNumericId,
            responseEnvelope = convertActionResultToSocketResponse(viewerScopedResult),
            actionResult = actionResult,
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

    private static bool tryResolveActorPlayerNumericId<TPayload>(TPayload payload, out long actorPlayerNumericId)
        where TPayload : class
    {
        actorPlayerNumericId = 0;
        switch (payload)
        {
            case ServerSubmitCharacterSelectionRequestDto submitCharacterSelectionRequestDto:
                actorPlayerNumericId = submitCharacterSelectionRequestDto.actorPlayerNumericId;
                return true;
            case ServerDrawOneCardRequestDto drawOneCardRequestDto:
                actorPlayerNumericId = drawOneCardRequestDto.actorPlayerNumericId;
                return true;
            case ServerPlayTreasureCardRequestDto playTreasureCardRequestDto:
                actorPlayerNumericId = playTreasureCardRequestDto.actorPlayerNumericId;
                return true;
            case ServerEnterSummonPhaseRequestDto enterSummonPhaseRequestDto:
                actorPlayerNumericId = enterSummonPhaseRequestDto.actorPlayerNumericId;
                return true;
            case ServerSummonTreasureCardRequestDto summonTreasureCardRequestDto:
                actorPlayerNumericId = summonTreasureCardRequestDto.actorPlayerNumericId;
                return true;
            case ServerEnterEndPhaseRequestDto enterEndPhaseRequestDto:
                actorPlayerNumericId = enterEndPhaseRequestDto.actorPlayerNumericId;
                return true;
            case ServerStartNextTurnRequestDto startNextTurnRequestDto:
                actorPlayerNumericId = startNextTurnRequestDto.actorPlayerNumericId;
                return true;
            case ServerEnterActionPhaseRequestDto enterActionPhaseRequestDto:
                actorPlayerNumericId = enterActionPhaseRequestDto.actorPlayerNumericId;
                return true;
            case ServerUseSkillRequestDto useSkillRequestDto:
                actorPlayerNumericId = useSkillRequestDto.actorPlayerNumericId;
                return true;
            case ServerTryResolveAnomalyRequestDto tryResolveAnomalyRequestDto:
                actorPlayerNumericId = tryResolveAnomalyRequestDto.actorPlayerNumericId;
                return true;
            case ServerSubmitDefenseRequestDto submitDefenseRequestDto:
                actorPlayerNumericId = submitDefenseRequestDto.actorPlayerNumericId;
                return true;
            case ServerSubmitResponseRequestDto submitResponseRequestDto:
                actorPlayerNumericId = submitResponseRequestDto.actorPlayerNumericId;
                return true;
            case ServerSubmitInputChoiceRequestDto submitInputChoiceRequestDto:
                actorPlayerNumericId = submitInputChoiceRequestDto.actorPlayerNumericId;
                return true;
            case ServerDebugResetMatchRequestDto debugResetMatchRequestDto:
                actorPlayerNumericId = debugResetMatchRequestDto.actorPlayerNumericId;
                return true;
            case ServerDebugOpenDamageResponseWindowRequestDto debugOpenDamageResponseWindowRequestDto:
                actorPlayerNumericId = debugOpenDamageResponseWindowRequestDto.actorPlayerNumericId;
                return true;
            case ServerDebugMoveTreasureToHandByDefinitionRequestDto debugMoveTreasureToHandByDefinitionRequestDto:
                actorPlayerNumericId = debugMoveTreasureToHandByDefinitionRequestDto.actorPlayerNumericId;
                return true;
            case ServerDebugPutTreasureOnTopByDefinitionRequestDto debugPutTreasureOnTopByDefinitionRequestDto:
                actorPlayerNumericId = debugPutTreasureOnTopByDefinitionRequestDto.actorPlayerNumericId;
                return true;
            case ServerDebugPutAnomalyOnTopByDefinitionRequestDto debugPutAnomalyOnTopByDefinitionRequestDto:
                actorPlayerNumericId = debugPutAnomalyOnTopByDefinitionRequestDto.actorPlayerNumericId;
                return true;
            default:
                return false;
        }
    }

    private ServerSocketRouteOutcome buildHostErrorOutcome(
        string actionType,
        long requestId,
        long viewerPlayerNumericId,
        string errorCode,
        string errorMessage)
    {
        var response = buildHostErrorResponse(requestId, viewerPlayerNumericId, errorCode, errorMessage);
        return new ServerSocketRouteOutcome
        {
            actionType = actionType,
            resolvedViewerPlayerNumericId = response.viewerPlayerNumericId,
            responseEnvelope = response,
            actionResult = null,
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

    private static bool hasValidSubmitInputChoicePayload(ServerSubmitInputChoiceRequestDto requestDto)
    {
        if (!string.IsNullOrWhiteSpace(requestDto.choiceKey))
        {
            return true;
        }

        if (requestDto.choiceKeys is null || requestDto.choiceKeys.Count == 0)
        {
            return false;
        }

        foreach (var choiceKey in requestDto.choiceKeys)
        {
            if (string.IsNullOrWhiteSpace(choiceKey))
            {
                return false;
            }
        }

        return true;
    }
}
