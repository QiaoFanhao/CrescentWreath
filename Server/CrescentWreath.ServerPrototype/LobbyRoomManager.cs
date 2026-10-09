using System;
using System.Collections.Generic;
using System.Linq;

namespace CrescentWreath.ServerPrototype;

public sealed class LobbyRoomManager
{
    private const int MaxPlayersPerRoom = 4;
    private readonly object syncRoot = new();
    private readonly Dictionary<string, LobbyRoom> roomsById = new();
    private readonly Dictionary<string, string> roomIdByConnectionId = new();
    private readonly Random random;

    public LobbyRoomManager()
        : this(new Random())
    {
    }

    public LobbyRoomManager(Random random)
    {
        this.random = random;
    }

    public LobbyActionResult createInitialProjection(string connectionId)
    {
        lock (syncRoot)
        {
            return success(connectionId, "lobbyConnected");
        }
    }

    public LobbyActionResult listRooms(string connectionId)
    {
        lock (syncRoot)
        {
            return success(connectionId, "lobbyListRooms");
        }
    }

    public LobbyActionResult refresh(string connectionId)
    {
        lock (syncRoot)
        {
            return success(connectionId, "lobbyRefresh");
        }
    }

    public LobbyActionResult createRoom(string connectionId, string roomName, string displayName)
    {
        lock (syncRoot)
        {
            leaveRoomInternal(connectionId);

            var now = DateTime.UtcNow;
            var room = new LobbyRoom
            {
                roomId = Guid.NewGuid().ToString("N"),
                roomName = sanitizeRoomName(roomName),
                hostConnectionId = connectionId,
                roomState = LobbyRoomState.waiting,
                createdAt = now,
                updatedAt = now,
            };

            room.players.Add(new LobbyPlayer
            {
                connectionId = connectionId,
                displayName = sanitizeDisplayName(displayName, connectionId),
                isHost = true,
            });

            roomsById[room.roomId] = room;
            roomIdByConnectionId[connectionId] = room.roomId;
            return success(connectionId, "lobbyCreateRoom");
        }
    }

    public LobbyActionResult joinRoom(string connectionId, string roomId, string displayName)
    {
        lock (syncRoot)
        {
            if (!roomsById.TryGetValue(roomId, out var room))
            {
                return failure(connectionId, "roomNotFound", "Room not found.");
            }

            if (room.roomState != LobbyRoomState.waiting)
            {
                return failure(connectionId, "roomNotJoinable", "Room is not waiting for players.");
            }

            if (room.players.Count >= MaxPlayersPerRoom)
            {
                return failure(connectionId, "roomFull", "Room is full.");
            }

            leaveRoomInternal(connectionId);
            room.players.Add(new LobbyPlayer
            {
                connectionId = connectionId,
                displayName = sanitizeDisplayName(displayName, connectionId),
                isHost = false,
            });
            room.updatedAt = DateTime.UtcNow;
            roomIdByConnectionId[connectionId] = room.roomId;
            return success(connectionId, "lobbyJoinRoom");
        }
    }

    public LobbyActionResult leaveRoom(string connectionId)
    {
        lock (syncRoot)
        {
            leaveRoomInternal(connectionId);
            return success(connectionId, "lobbyLeaveRoom");
        }
    }

    public LobbyActionResult setTeam(string connectionId, string teamKey)
    {
        lock (syncRoot)
        {
            if (!tryResolveCurrentPlayer(connectionId, out var room, out var player, out var failureResult))
            {
                return failureResult!;
            }

            if (room!.roomState != LobbyRoomState.waiting)
            {
                return failure(connectionId, "roomAlreadyStarted", "Room has already started.");
            }

            if (!tryParseTeamKey(teamKey, out var parsedTeam))
            {
                return failure(connectionId, "invalidTeam", "teamKey must be teamA or teamB.");
            }

            var sameTeamCount = room.players.Count(candidate =>
                !string.Equals(candidate.connectionId, connectionId, StringComparison.Ordinal) &&
                candidate.selectedTeam == parsedTeam);
            if (sameTeamCount >= 2)
            {
                return failure(connectionId, "teamFull", "Selected team is full.");
            }

            player!.selectedTeam = parsedTeam;
            player.isReady = false;
            room.updatedAt = DateTime.UtcNow;
            return success(connectionId, "lobbySetTeam");
        }
    }

    public LobbyActionResult submitReadyState(string connectionId, bool isReady)
    {
        lock (syncRoot)
        {
            if (!tryResolveCurrentPlayer(connectionId, out var room, out var player, out var failureResult))
            {
                return failureResult!;
            }

            if (room!.roomState != LobbyRoomState.waiting)
            {
                return failure(connectionId, "roomAlreadyStarted", "Room has already started.");
            }

            if (isReady && player!.selectedTeam == LobbyTeamKey.none)
            {
                return failure(connectionId, "teamNotSelected", "Select a team before ready.");
            }

            player!.isReady = isReady;
            room.updatedAt = DateTime.UtcNow;
            var startReason = resolveStartReason(room);
            if (startReason == "ready")
            {
                var assignment = startGameInternal(room);
                return success(connectionId, "gameStarted", assignment);
            }

            return success(connectionId, "lobbySubmitReadyState");
        }
    }

    public void disconnect(string connectionId)
    {
        lock (syncRoot)
        {
            leaveRoomInternal(connectionId);
        }
    }

    public List<string> getCurrentRoomConnectionIds(string connectionId)
    {
        lock (syncRoot)
        {
            if (!roomIdByConnectionId.TryGetValue(connectionId, out var roomId) ||
                !roomsById.TryGetValue(roomId, out var room))
            {
                return new List<string>();
            }

            return room.players.Select(player => player.connectionId).ToList();
        }
    }

    public LobbyProjection buildProjection(string connectionId, string? eventType = null, string? error = null)
    {
        lock (syncRoot)
        {
            return buildProjectionNoLock(connectionId, eventType, error);
        }
    }

    private LobbyProjection buildProjectionNoLock(string connectionId, string? eventType = null, string? error = null)
    {
        var rooms = roomsById.Values
            .OrderBy(room => room.createdAt)
            .Select(buildRoomSummaryProjection)
            .ToList();

        LobbyRoomProjection? currentRoom = null;
        if (roomIdByConnectionId.TryGetValue(connectionId, out var roomId) &&
            roomsById.TryGetValue(roomId, out var room))
        {
            currentRoom = buildRoomProjection(room);
        }

        return new LobbyProjection
        {
            connectedClientId = connectionId,
            rooms = rooms,
            currentRoom = currentRoom,
            error = error,
            eventType = eventType,
            gameStarted = currentRoom?.status == LobbyRoomState.inGame.ToString(),
            assignedPlayerNumericId = currentRoom?.players
                .FirstOrDefault(player => string.Equals(player.connectionId, connectionId, StringComparison.Ordinal))
                ?.assignedPlayerNumericId,
            gameSessionId = currentRoom is null || !roomIdByConnectionId.TryGetValue(connectionId, out var currentRoomId)
                ? null
                : roomsById.TryGetValue(currentRoomId, out var resolvedRoom) ? resolvedRoom.matchSessionId : null,
        };
    }

    private bool tryResolveCurrentPlayer(
        string connectionId,
        out LobbyRoom? room,
        out LobbyPlayer? player,
        out LobbyActionResult? failureResult)
    {
        room = null;
        player = null;
        failureResult = null;
        if (!roomIdByConnectionId.TryGetValue(connectionId, out var roomId) ||
            !roomsById.TryGetValue(roomId, out room))
        {
            failureResult = failure(connectionId, "notInRoom", "Connection is not in a room.");
            return false;
        }

        player = room.players.FirstOrDefault(candidate =>
            string.Equals(candidate.connectionId, connectionId, StringComparison.Ordinal));
        if (player is null)
        {
            failureResult = failure(connectionId, "notInRoom", "Connection is not in a room.");
            return false;
        }

        return true;
    }

    private void leaveRoomInternal(string connectionId)
    {
        if (!roomIdByConnectionId.TryGetValue(connectionId, out var roomId))
        {
            return;
        }

        roomIdByConnectionId.Remove(connectionId);
        if (!roomsById.TryGetValue(roomId, out var room))
        {
            return;
        }

        room.players.RemoveAll(player => string.Equals(player.connectionId, connectionId, StringComparison.Ordinal));
        room.updatedAt = DateTime.UtcNow;
        if (room.players.Count == 0 && room.roomState != LobbyRoomState.inGame)
        {
            roomsById.Remove(roomId);
            return;
        }

        if (string.Equals(room.hostConnectionId, connectionId, StringComparison.Ordinal) && room.players.Count > 0)
        {
            room.hostConnectionId = room.players[0].connectionId;
            foreach (var player in room.players)
            {
                player.isHost = string.Equals(player.connectionId, room.hostConnectionId, StringComparison.Ordinal);
            }
        }
    }

    private GameStartAssignmentResult startGameInternal(LobbyRoom room)
    {
        room.roomState = LobbyRoomState.starting;
        var teamAPlayers = shuffled(room.players.Where(player => player.selectedTeam == LobbyTeamKey.teamA).ToList());
        var teamBPlayers = shuffled(room.players.Where(player => player.selectedTeam == LobbyTeamKey.teamB).ToList());
        var teamAUsesOddSeats = random.Next(0, 2) == 0;

        var assignment = new GameStartAssignmentResult
        {
            roomId = room.roomId,
            gameSessionId = Guid.NewGuid().ToString("N"),
        };

        if (teamAUsesOddSeats)
        {
            assign(assignment, teamAPlayers[0], 1, LobbyTeamKey.teamA);
            assign(assignment, teamBPlayers[0], 2, LobbyTeamKey.teamB);
            assign(assignment, teamAPlayers[1], 3, LobbyTeamKey.teamA);
            assign(assignment, teamBPlayers[1], 4, LobbyTeamKey.teamB);
        }
        else
        {
            assign(assignment, teamBPlayers[0], 1, LobbyTeamKey.teamB);
            assign(assignment, teamAPlayers[0], 2, LobbyTeamKey.teamA);
            assign(assignment, teamBPlayers[1], 3, LobbyTeamKey.teamB);
            assign(assignment, teamAPlayers[1], 4, LobbyTeamKey.teamA);
        }

        foreach (var player in room.players)
        {
            player.assignedPlayerNumericId = assignment.assignedPlayerNumericIdsByConnectionId[player.connectionId];
        }

        room.matchSessionId = assignment.gameSessionId;
        room.roomState = LobbyRoomState.inGame;
        room.updatedAt = DateTime.UtcNow;
        return assignment;
    }

    private void assign(GameStartAssignmentResult assignment, LobbyPlayer player, long playerNumericId, LobbyTeamKey teamKey)
    {
        assignment.assignedPlayerNumericIdsByConnectionId[player.connectionId] = playerNumericId;
        assignment.lobbyTeamByPlayerNumericId[playerNumericId] = teamKey;
    }

    private List<LobbyPlayer> shuffled(List<LobbyPlayer> players)
    {
        for (var index = players.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (players[index], players[swapIndex]) = (players[swapIndex], players[index]);
        }

        return players;
    }

    private LobbyActionResult success(string connectionId, string eventType, GameStartAssignmentResult? assignment = null)
    {
        var projection = buildProjectionNoLock(connectionId, eventType);
        if (assignment is not null &&
            assignment.assignedPlayerNumericIdsByConnectionId.TryGetValue(connectionId, out var assignedPlayerNumericId))
        {
            projection.gameStarted = true;
            projection.assignedPlayerNumericId = assignedPlayerNumericId;
            projection.gameSessionId = assignment.gameSessionId;
        }

        return new LobbyActionResult
        {
            isSucceeded = true,
            projection = projection,
            gameStartAssignment = assignment,
        };
    }

    private LobbyActionResult failure(string connectionId, string errorCode, string errorMessage)
    {
        return new LobbyActionResult
        {
            isSucceeded = false,
            errorCode = errorCode,
            errorMessage = errorMessage,
            projection = buildProjectionNoLock(connectionId, "lobbyError", errorMessage),
        };
    }

    private static LobbyRoomSummaryProjection buildRoomSummaryProjection(LobbyRoom room)
    {
        return new LobbyRoomSummaryProjection
        {
            roomId = room.roomId,
            roomName = room.roomName,
            playerCount = room.players.Count,
            teamACount = room.players.Count(player => player.selectedTeam == LobbyTeamKey.teamA),
            teamBCount = room.players.Count(player => player.selectedTeam == LobbyTeamKey.teamB),
            readyCount = room.players.Count(player => player.isReady),
            status = room.roomState.ToString(),
        };
    }

    private static LobbyRoomProjection buildRoomProjection(LobbyRoom room)
    {
        return new LobbyRoomProjection
        {
            roomId = room.roomId,
            roomName = room.roomName,
            status = room.roomState.ToString(),
            players = room.players.Select(player => new LobbyPlayerProjection
            {
                connectionId = player.connectionId,
                displayName = player.displayName,
                selectedTeam = player.selectedTeam.ToString(),
                isReady = player.isReady,
                isHost = player.isHost,
                assignedPlayerNumericId = player.assignedPlayerNumericId,
            }).ToList(),
            canStart = resolveStartReason(room) == "ready",
            startReasonKey = resolveStartReason(room),
        };
    }

    private static string resolveStartReason(LobbyRoom room)
    {
        if (room.roomState != LobbyRoomState.waiting)
        {
            return "roomAlreadyStarted";
        }

        if (room.players.Count != MaxPlayersPerRoom)
        {
            return "notEnoughPlayers";
        }

        if (room.players.Count(player => player.selectedTeam == LobbyTeamKey.teamA) != 2 ||
            room.players.Count(player => player.selectedTeam == LobbyTeamKey.teamB) != 2)
        {
            return "teamCountInvalid";
        }

        if (room.players.Any(player => !player.isReady))
        {
            return "notAllReady";
        }

        return "ready";
    }

    private static bool tryParseTeamKey(string teamKey, out LobbyTeamKey parsedTeam)
    {
        parsedTeam = LobbyTeamKey.none;
        if (string.Equals(teamKey, "teamA", StringComparison.OrdinalIgnoreCase))
        {
            parsedTeam = LobbyTeamKey.teamA;
            return true;
        }

        if (string.Equals(teamKey, "teamB", StringComparison.OrdinalIgnoreCase))
        {
            parsedTeam = LobbyTeamKey.teamB;
            return true;
        }

        return false;
    }

    private static string sanitizeRoomName(string roomName)
    {
        return string.IsNullOrWhiteSpace(roomName)
            ? "LAN Room"
            : roomName.Trim();
    }

    private static string sanitizeDisplayName(string displayName, string connectionId)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return "Player-" + connectionId[..Math.Min(4, connectionId.Length)];
    }
}
