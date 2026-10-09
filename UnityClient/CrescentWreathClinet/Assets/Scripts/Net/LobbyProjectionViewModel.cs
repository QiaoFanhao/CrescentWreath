using System.Collections.Generic;

namespace CrescentWreath.Client.Net
{
public sealed class LobbyProjectionViewModel
{
    public string connectedClientId = string.Empty;
    public string eventType = string.Empty;
    public string error = string.Empty;
    public bool gameStarted;
    public long assignedPlayerNumericId;
    public string gameSessionId = string.Empty;
    public List<LobbyRoomSummaryViewModel> rooms = new();
    public LobbyRoomViewModel? currentRoom;
}

public sealed class LobbyRoomSummaryViewModel
{
    public string roomId = string.Empty;
    public string roomName = string.Empty;
    public int playerCount;
    public int teamACount;
    public int teamBCount;
    public int readyCount;
    public string status = string.Empty;
}

public sealed class LobbyRoomViewModel
{
    public string roomId = string.Empty;
    public string roomName = string.Empty;
    public string status = string.Empty;
    public List<LobbyPlayerViewModel> players = new();
    public bool canStart;
    public string startReasonKey = string.Empty;
}

public sealed class LobbyPlayerViewModel
{
    public string connectionId = string.Empty;
    public string displayName = string.Empty;
    public string selectedTeam = string.Empty;
    public bool isReady;
    public bool isHost;
    public long assignedPlayerNumericId;
}
}
