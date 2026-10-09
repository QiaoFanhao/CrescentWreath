using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.Net
{
public static class LobbyProjectionParser
{
    public static bool TryParse(string rawJson, out LobbyProjectionViewModel projection, out long assignedPlayerNumericId)
    {
        projection = new LobbyProjectionViewModel();
        assignedPlayerNumericId = 0;
        try
        {
            if (!hasNonNullLobbyProjection(rawJson))
            {
                return false;
            }

            var response = JsonUtility.FromJson<LobbyResponseEnvelopeDto>(rawJson);
            if (response is null || response.lobbyProjection is null)
            {
                return false;
            }

            assignedPlayerNumericId = response.assignedPlayerNumericId > 0
                ? response.assignedPlayerNumericId
                : response.lobbyProjection.assignedPlayerNumericId;
            projection = map(response.lobbyProjection);
            if (assignedPlayerNumericId > 0)
            {
                projection.assignedPlayerNumericId = assignedPlayerNumericId;
            }

            if (string.Equals(response.messageType, "gameStarted", StringComparison.OrdinalIgnoreCase))
            {
                projection.gameStarted = true;
            }

            return true;
        }
        catch (Exception)
        {
            projection = new LobbyProjectionViewModel();
            assignedPlayerNumericId = 0;
            return false;
        }
    }

    private static LobbyProjectionViewModel map(LobbyProjectionDto dto)
    {
        var projection = new LobbyProjectionViewModel
        {
            connectedClientId = dto.connectedClientId ?? string.Empty,
            eventType = dto.eventType ?? string.Empty,
            error = dto.error ?? string.Empty,
            gameStarted = dto.gameStarted,
            assignedPlayerNumericId = dto.assignedPlayerNumericId,
            gameSessionId = dto.gameSessionId ?? string.Empty,
            rooms = new List<LobbyRoomSummaryViewModel>(),
        };

        if (dto.rooms is not null)
        {
            foreach (var room in dto.rooms)
            {
                if (room is null)
                {
                    continue;
                }

                projection.rooms.Add(new LobbyRoomSummaryViewModel
                {
                    roomId = room.roomId ?? string.Empty,
                    roomName = room.roomName ?? string.Empty,
                    playerCount = room.playerCount,
                    teamACount = room.teamACount,
                    teamBCount = room.teamBCount,
                    readyCount = room.readyCount,
                    status = room.status ?? string.Empty,
                });
            }
        }

        if (dto.currentRoom is not null && !string.IsNullOrWhiteSpace(dto.currentRoom.roomId))
        {
            projection.currentRoom = new LobbyRoomViewModel
            {
                roomId = dto.currentRoom.roomId ?? string.Empty,
                roomName = dto.currentRoom.roomName ?? string.Empty,
                status = dto.currentRoom.status ?? string.Empty,
                canStart = dto.currentRoom.canStart,
                startReasonKey = dto.currentRoom.startReasonKey ?? string.Empty,
                players = new List<LobbyPlayerViewModel>(),
            };

            if (dto.currentRoom.players is not null)
            {
                foreach (var player in dto.currentRoom.players)
                {
                    if (player is null)
                    {
                        continue;
                    }

                    projection.currentRoom.players.Add(new LobbyPlayerViewModel
                    {
                        connectionId = player.connectionId ?? string.Empty,
                        displayName = player.displayName ?? string.Empty,
                        selectedTeam = player.selectedTeam ?? string.Empty,
                        isReady = player.isReady,
                        isHost = player.isHost,
                        assignedPlayerNumericId = player.assignedPlayerNumericId,
                    });
                }
            }
        }

        return projection;
    }

    private static bool hasNonNullLobbyProjection(string rawJson)
    {
        const string key = "\"lobbyProjection\"";
        var keyIndex = rawJson.IndexOf(key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return false;
        }

        var colonIndex = rawJson.IndexOf(':', keyIndex + key.Length);
        if (colonIndex < 0)
        {
            return false;
        }

        var valueIndex = colonIndex + 1;
        while (valueIndex < rawJson.Length && char.IsWhiteSpace(rawJson[valueIndex]))
        {
            valueIndex++;
        }

        return valueIndex < rawJson.Length && rawJson[valueIndex] != 'n';
    }

    [Serializable]
    private sealed class LobbyResponseEnvelopeDto
    {
        public string messageType = string.Empty;
        public long assignedPlayerNumericId;
        public LobbyProjectionDto? lobbyProjection;
    }

    [Serializable]
    private sealed class LobbyProjectionDto
    {
        public string connectedClientId = string.Empty;
        public List<LobbyRoomSummaryDto>? rooms;
        public LobbyRoomDto? currentRoom;
        public string error = string.Empty;
        public string eventType = string.Empty;
        public bool gameStarted;
        public long assignedPlayerNumericId;
        public string gameSessionId = string.Empty;
    }

    [Serializable]
    private sealed class LobbyRoomSummaryDto
    {
        public string roomId = string.Empty;
        public string roomName = string.Empty;
        public int playerCount;
        public int teamACount;
        public int teamBCount;
        public int readyCount;
        public string status = string.Empty;
    }

    [Serializable]
    private sealed class LobbyRoomDto
    {
        public string roomId = string.Empty;
        public string roomName = string.Empty;
        public string status = string.Empty;
        public List<LobbyPlayerDto>? players;
        public bool canStart;
        public string startReasonKey = string.Empty;
    }

    [Serializable]
    private sealed class LobbyPlayerDto
    {
        public string connectionId = string.Empty;
        public string displayName = string.Empty;
        public string selectedTeam = string.Empty;
        public bool isReady;
        public bool isHost;
        public long assignedPlayerNumericId;
    }
}
}
