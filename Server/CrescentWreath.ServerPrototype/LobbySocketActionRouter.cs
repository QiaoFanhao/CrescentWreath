using System;
using System.Text.Json;

namespace CrescentWreath.ServerPrototype;

public sealed class LobbySocketActionRouter
{
    private readonly LobbyRoomManager roomManager;

    public LobbySocketActionRouter(LobbyRoomManager roomManager)
    {
        this.roomManager = roomManager;
    }

    public LobbyActionResult routeMessage(string connectionId, string rawJson)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ServerSocketRequestEnvelope>(rawJson);
            if (envelope is null)
            {
                return buildMalformedResult(connectionId, "Request envelope could not be parsed.");
            }

            return envelope.actionType switch
            {
                "lobbyListRooms" => roomManager.listRooms(connectionId),
                "lobbyRefresh" => roomManager.refresh(connectionId),
                "lobbyCreateRoom" => roomManager.createRoom(
                    connectionId,
                    readString(envelope.payload, "roomName"),
                    readString(envelope.payload, "displayName")),
                "lobbyJoinRoom" => roomManager.joinRoom(
                    connectionId,
                    readString(envelope.payload, "roomId"),
                    readString(envelope.payload, "displayName")),
                "lobbyLeaveRoom" => roomManager.leaveRoom(connectionId),
                "lobbySetTeam" => roomManager.setTeam(connectionId, readString(envelope.payload, "teamKey")),
                "lobbySubmitReadyState" => roomManager.submitReadyState(
                    connectionId,
                    readBool(envelope.payload, "isReady")),
                _ => buildUnknownActionResult(connectionId, envelope.actionType),
            };
        }
        catch (JsonException exception)
        {
            return buildMalformedResult(connectionId, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return buildMalformedResult(connectionId, exception.Message);
        }
    }

    private LobbyActionResult buildUnknownActionResult(string connectionId, string actionType)
    {
        return new LobbyActionResult
        {
            isSucceeded = false,
            errorCode = "unknown_lobby_action",
            errorMessage = $"Unknown lobby actionType '{actionType}'.",
            projection = roomManager.buildProjection(connectionId, "lobbyError", $"Unknown lobby actionType '{actionType}'."),
        };
    }

    private LobbyActionResult buildMalformedResult(string connectionId, string message)
    {
        return new LobbyActionResult
        {
            isSucceeded = false,
            errorCode = "malformed_request",
            errorMessage = message,
            projection = roomManager.buildProjection(connectionId, "lobbyError", message),
        };
    }

    private static string readString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static bool readBool(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => false,
        };
    }
}
