using System;
using System.Collections.Generic;

namespace CrescentWreath.ServerPrototype;

public enum LobbyRoomState
{
    waiting,
    starting,
    inGame,
    closed,
}

public enum LobbyTeamKey
{
    none,
    teamA,
    teamB,
}

public sealed class LobbyRoom
{
    public string roomId { get; set; } = string.Empty;
    public string roomName { get; set; } = string.Empty;
    public string hostConnectionId { get; set; } = string.Empty;
    public LobbyRoomState roomState { get; set; } = LobbyRoomState.waiting;
    public List<LobbyPlayer> players { get; } = new();
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public string? matchSessionId { get; set; }
}

public sealed class LobbyPlayer
{
    public string connectionId { get; set; } = string.Empty;
    public string displayName { get; set; } = string.Empty;
    public LobbyTeamKey selectedTeam { get; set; } = LobbyTeamKey.none;
    public bool isReady { get; set; }
    public bool isHost { get; set; }
    public long? assignedPlayerNumericId { get; set; }
}

public sealed class LobbyProjection
{
    public string connectedClientId { get; set; } = string.Empty;
    public List<LobbyRoomSummaryProjection> rooms { get; set; } = new();
    public LobbyRoomProjection? currentRoom { get; set; }
    public string? error { get; set; }
    public string? eventType { get; set; }
    public bool gameStarted { get; set; }
    public long? assignedPlayerNumericId { get; set; }
    public string? gameSessionId { get; set; }
}

public sealed class LobbyRoomSummaryProjection
{
    public string roomId { get; set; } = string.Empty;
    public string roomName { get; set; } = string.Empty;
    public int playerCount { get; set; }
    public int teamACount { get; set; }
    public int teamBCount { get; set; }
    public int readyCount { get; set; }
    public string status { get; set; } = string.Empty;
}

public sealed class LobbyRoomProjection
{
    public string roomId { get; set; } = string.Empty;
    public string roomName { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public List<LobbyPlayerProjection> players { get; set; } = new();
    public bool canStart { get; set; }
    public string startReasonKey { get; set; } = string.Empty;
}

public sealed class LobbyPlayerProjection
{
    public string connectionId { get; set; } = string.Empty;
    public string displayName { get; set; } = string.Empty;
    public string selectedTeam { get; set; } = string.Empty;
    public bool isReady { get; set; }
    public bool isHost { get; set; }
    public long? assignedPlayerNumericId { get; set; }
}

public sealed class GameStartAssignmentResult
{
    public string roomId { get; set; } = string.Empty;
    public string gameSessionId { get; set; } = string.Empty;
    public Dictionary<string, long> assignedPlayerNumericIdsByConnectionId { get; } = new();
    public Dictionary<long, LobbyTeamKey> lobbyTeamByPlayerNumericId { get; } = new();
}

public sealed class LobbyActionResult
{
    public bool isSucceeded { get; set; }
    public string? errorCode { get; set; }
    public string? errorMessage { get; set; }
    public LobbyProjection projection { get; set; } = new();
    public GameStartAssignmentResult? gameStartAssignment { get; set; }
}
