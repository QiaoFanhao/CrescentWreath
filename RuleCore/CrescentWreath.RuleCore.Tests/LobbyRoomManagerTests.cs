using System;
using System.Linq;
using CrescentWreath.ServerPrototype;

namespace CrescentWreath.RuleCore.Tests;

public sealed class LobbyRoomManagerTests
{
    [Fact]
    public void create_join_team_ready_starts_game_with_alternating_seats()
    {
        var manager = new LobbyRoomManager(new Random(1));

        var created = manager.createRoom("conn-a1", "Room", "A1");
        var roomId = created.projection.currentRoom!.roomId;
        manager.joinRoom("conn-a2", roomId, "A2");
        manager.joinRoom("conn-b1", roomId, "B1");
        manager.joinRoom("conn-b2", roomId, "B2");

        manager.setTeam("conn-a1", "teamA");
        manager.setTeam("conn-a2", "teamA");
        manager.setTeam("conn-b1", "teamB");
        manager.setTeam("conn-b2", "teamB");

        Assert.False(manager.submitReadyState("conn-a1", true).projection.gameStarted);
        Assert.False(manager.submitReadyState("conn-a2", true).projection.gameStarted);
        Assert.False(manager.submitReadyState("conn-b1", true).projection.gameStarted);
        var started = manager.submitReadyState("conn-b2", true);

        Assert.True(started.isSucceeded);
        Assert.True(started.projection.gameStarted);
        Assert.NotNull(started.gameStartAssignment);
        Assert.Equal(4, started.gameStartAssignment!.assignedPlayerNumericIdsByConnectionId.Count);
        Assert.Equal(new[] { 1L, 2L, 3L, 4L },
            started.gameStartAssignment.assignedPlayerNumericIdsByConnectionId.Values.OrderBy(value => value).ToArray());

        var teamBySeat = started.gameStartAssignment.lobbyTeamByPlayerNumericId;
        Assert.NotEqual(teamBySeat[1], teamBySeat[2]);
        Assert.NotEqual(teamBySeat[2], teamBySeat[3]);
        Assert.NotEqual(teamBySeat[3], teamBySeat[4]);
        Assert.NotEqual(teamBySeat[4], teamBySeat[1]);
        Assert.Equal(teamBySeat[1], teamBySeat[3]);
        Assert.Equal(teamBySeat[2], teamBySeat[4]);
    }

    [Fact]
    public void room_does_not_start_until_four_players_two_teams_and_all_ready()
    {
        var manager = new LobbyRoomManager(new Random(1));

        var created = manager.createRoom("conn-a1", "Room", "A1");
        var roomId = created.projection.currentRoom!.roomId;
        manager.joinRoom("conn-a2", roomId, "A2");
        manager.joinRoom("conn-b1", roomId, "B1");
        manager.joinRoom("conn-b2", roomId, "B2");

        manager.setTeam("conn-a1", "teamA");
        manager.setTeam("conn-a2", "teamA");
        manager.setTeam("conn-b1", "teamB");
        manager.setTeam("conn-b2", "teamB");

        var notAllReady = manager.submitReadyState("conn-a1", true);
        Assert.True(notAllReady.isSucceeded);
        Assert.Equal("notAllReady", notAllReady.projection.currentRoom!.startReasonKey);

        manager.submitReadyState("conn-a2", true);
        manager.submitReadyState("conn-b1", true);
        var started = manager.submitReadyState("conn-b2", true);

        Assert.True(started.projection.gameStarted);
        Assert.Equal("inGame", started.projection.currentRoom!.status);
    }

    [Fact]
    public void team_capacity_is_limited_to_two_players()
    {
        var manager = new LobbyRoomManager(new Random(1));

        var created = manager.createRoom("conn-a1", "Room", "A1");
        var roomId = created.projection.currentRoom!.roomId;
        manager.joinRoom("conn-a2", roomId, "A2");
        manager.joinRoom("conn-a3", roomId, "A3");

        Assert.True(manager.setTeam("conn-a1", "teamA").isSucceeded);
        Assert.True(manager.setTeam("conn-a2", "teamA").isSucceeded);
        var third = manager.setTeam("conn-a3", "teamA");

        Assert.False(third.isSucceeded);
        Assert.Equal("teamFull", third.errorCode);
    }

    [Fact]
    public void one_connection_cannot_remain_in_two_rooms()
    {
        var manager = new LobbyRoomManager(new Random(1));

        var first = manager.createRoom("conn-a", "First", "A");
        var firstRoomId = first.projection.currentRoom!.roomId;
        var second = manager.createRoom("conn-a", "Second", "A");

        Assert.NotEqual(firstRoomId, second.projection.currentRoom!.roomId);
        Assert.DoesNotContain(firstRoomId, second.projection.rooms.Select(room => room.roomId));
    }
}
