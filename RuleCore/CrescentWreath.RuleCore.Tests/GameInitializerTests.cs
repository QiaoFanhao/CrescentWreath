using System.Linq;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Initialization;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class GameInitializerTests
{
    [Fact]
    public void CreateStandard2v2MatchState_ShouldSatisfyInitializationStructuralInvariants()
    {
        var initializer = new GameInitializer();
        var gameState = initializer.createStandard2v2MatchState();

        Assert.NotNull(gameState.matchMeta);
        Assert.NotNull(gameState.turnState);
        Assert.NotNull(gameState.publicState);
        Assert.Equal(MatchState.running, gameState.matchState);

        var matchMeta = gameState.matchMeta!;
        var publicState = gameState.publicState!;
        var currentAnomalyState = gameState.currentAnomalyState;

        Assert.NotNull(currentAnomalyState);
        Assert.Equal("A001", currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(9, currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A002", currentAnomalyState.anomalyDeckDefinitionIds[0]);
        Assert.Equal("A010", currentAnomalyState.anomalyDeckDefinitionIds[8]);

        Assert.Equal(4, matchMeta.seatOrder.Count);
        Assert.Equal(4, matchMeta.seatOrder.Distinct().Count());

        Assert.Equal(4, matchMeta.teamAssignments.Count);
        Assert.Equal(2, matchMeta.teamSeatMap.Count);

        var playersFromSeatMap = matchMeta.teamSeatMap.Values.SelectMany(playerIds => playerIds).ToList();
        Assert.Equal(4, playersFromSeatMap.Count);
        Assert.Equal(4, playersFromSeatMap.Distinct().Count());

        foreach (var playerId in matchMeta.seatOrder)
        {
            Assert.True(matchMeta.teamAssignments.ContainsKey(playerId));
            var assignedTeamId = matchMeta.teamAssignments[playerId];
            Assert.True(matchMeta.teamSeatMap.ContainsKey(assignedTeamId));
            Assert.Contains(playerId, matchMeta.teamSeatMap[assignedTeamId]);
        }

        foreach (var teamEntry in matchMeta.teamSeatMap)
        {
            foreach (var playerId in teamEntry.Value)
            {
                Assert.True(matchMeta.teamAssignments.ContainsKey(playerId));
                Assert.Equal(teamEntry.Key, matchMeta.teamAssignments[playerId]);
            }
        }

        foreach (var playerEntry in gameState.players)
        {
            Assert.True(matchMeta.teamAssignments.ContainsKey(playerEntry.Key));
            Assert.Equal(playerEntry.Value.teamId, matchMeta.teamAssignments[playerEntry.Key]);
        }

        var activeCharacterIds = gameState.players.Values.Select(playerState => playerState.activeCharacterInstanceId).ToList();
        Assert.All(activeCharacterIds, activeCharacterId => Assert.True(activeCharacterId.HasValue));

        var concreteActiveCharacterIds = activeCharacterIds.Select(activeCharacterId => activeCharacterId!.Value).ToList();
        Assert.Equal(4, concreteActiveCharacterIds.Count);
        Assert.Equal(4, concreteActiveCharacterIds.Distinct().Count());

        foreach (var playerState in gameState.players.Values)
        {
            var activeCharacterId = playerState.activeCharacterInstanceId!.Value;
            Assert.True(gameState.characterInstances.ContainsKey(activeCharacterId));
            Assert.Equal(playerState.playerId, gameState.characterInstances[activeCharacterId].ownerPlayerId);
        }

        var allPlayerZoneIds = new List<ZoneId>();

        foreach (var playerState in gameState.players.Values)
        {
            var playerZoneIds = new[]
            {
                playerState.deckZoneId,
                playerState.handZoneId,
                playerState.discardZoneId,
                playerState.fieldZoneId,
                playerState.characterSetAsideZoneId,
            };

            Assert.Equal(5, playerZoneIds.Distinct().Count());
            allPlayerZoneIds.AddRange(playerZoneIds);
        }

        Assert.Equal(20, allPlayerZoneIds.Count);
        Assert.Equal(20, allPlayerZoneIds.Distinct().Count());

        var publicZoneIds = new[]
        {
            publicState.publicTreasureDeckZoneId,
            publicState.anomalyDeckZoneId,
            publicState.sakuraCakeDeckZoneId,
            publicState.summonZoneId,
            publicState.gapZoneId,
        };

        Assert.Equal(5, publicZoneIds.Distinct().Count());
        Assert.Empty(publicZoneIds.Intersect(allPlayerZoneIds));

        assertPublicZoneReference(gameState, publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);
        assertPublicZoneReference(gameState, publicState.anomalyDeckZoneId, ZoneKey.anomalyDeck);
        assertPublicZoneReference(gameState, publicState.sakuraCakeDeckZoneId, ZoneKey.sakuraCakeDeck);
        assertPublicZoneReference(gameState, publicState.summonZoneId, ZoneKey.summonZone);
        assertPublicZoneReference(gameState, publicState.gapZoneId, ZoneKey.gapZone);
    }

    [Fact]
    public void CreateStandard2v2MatchState_ShouldBuildStandardBaselineState()
    {
        var initializer = new GameInitializer();

        var gameState = initializer.createStandard2v2MatchState();

        Assert.Equal(MatchState.running, gameState.matchState);
        Assert.Equal(4, gameState.players.Count);
        Assert.Equal(2, gameState.teams.Count);
        Assert.Equal(40, gameState.cardInstances.Count);
        Assert.Equal(4, gameState.characterInstances.Count);

        Assert.NotNull(gameState.matchMeta);
        Assert.Equal(10, gameState.matchMeta!.baseKillScore);
        Assert.Equal(MatchMode.standard2v2, gameState.matchMeta.mode);
        Assert.Equal(
            new[] { new PlayerId(1), new PlayerId(2), new PlayerId(3), new PlayerId(4) },
            gameState.matchMeta.seatOrder);
        Assert.Equal(new TeamId(1), gameState.matchMeta.teamAssignments[new PlayerId(1)]);
        Assert.Equal(new TeamId(2), gameState.matchMeta.teamAssignments[new PlayerId(2)]);
        Assert.Equal(new TeamId(1), gameState.matchMeta.teamAssignments[new PlayerId(3)]);
        Assert.Equal(new TeamId(2), gameState.matchMeta.teamAssignments[new PlayerId(4)]);

        Assert.Equal(new[] { new PlayerId(1), new PlayerId(3) }, gameState.matchMeta.teamSeatMap[new TeamId(1)]);
        Assert.Equal(new[] { new PlayerId(2), new PlayerId(4) }, gameState.matchMeta.teamSeatMap[new TeamId(2)]);

        Assert.NotNull(gameState.turnState);
        Assert.Equal(1, gameState.turnState!.turnNumber);
        Assert.Equal(new PlayerId(1), gameState.turnState.currentPlayerId);
        Assert.Equal(new TeamId(1), gameState.turnState.currentTeamId);
        Assert.Equal(TurnPhase.start, gameState.turnState.currentPhase);
        Assert.Equal(0, gameState.turnState.phaseStepIndex);
        Assert.False(gameState.turnState.hasResolvedAnomalyThisTurn);

        Assert.NotNull(gameState.currentAnomalyState);
        Assert.Equal("A001", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.Equal(9, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Equal("A002", gameState.currentAnomalyState.anomalyDeckDefinitionIds[0]);

        Assert.NotNull(gameState.publicState);
        assertPublicZoneReference(gameState, gameState.publicState!.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);
        assertPublicZoneReference(gameState, gameState.publicState.anomalyDeckZoneId, ZoneKey.anomalyDeck);
        assertPublicZoneReference(gameState, gameState.publicState.sakuraCakeDeckZoneId, ZoneKey.sakuraCakeDeck);
        assertPublicZoneReference(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone);
        assertPublicZoneReference(gameState, gameState.publicState.gapZoneId, ZoneKey.gapZone);

        Assert.Equal(4, gameState.zones.Values.Count(zone => zone.zoneType == ZoneKey.deck));
        Assert.Equal(4, gameState.zones.Values.Count(zone => zone.zoneType == ZoneKey.hand));
        Assert.Equal(4, gameState.zones.Values.Count(zone => zone.zoneType == ZoneKey.discard));
        Assert.Equal(4, gameState.zones.Values.Count(zone => zone.zoneType == ZoneKey.field));
        Assert.Equal(4, gameState.zones.Values.Count(zone => zone.zoneType == ZoneKey.characterSetAside));

        assertSinglePublicZone(gameState, ZoneKey.publicTreasureDeck);
        assertSinglePublicZone(gameState, ZoneKey.anomalyDeck);
        assertSinglePublicZone(gameState, ZoneKey.sakuraCakeDeck);
        assertSinglePublicZone(gameState, ZoneKey.summonZone);
        assertSinglePublicZone(gameState, ZoneKey.gapZone);

        foreach (var playerState in gameState.players.Values)
        {
            Assert.True(gameState.zones.ContainsKey(playerState.deckZoneId));
            Assert.True(gameState.zones.ContainsKey(playerState.handZoneId));
            Assert.True(gameState.zones.ContainsKey(playerState.discardZoneId));
            Assert.True(gameState.zones.ContainsKey(playerState.fieldZoneId));
            Assert.True(gameState.zones.ContainsKey(playerState.characterSetAsideZoneId));

            Assert.Equal(0, playerState.mana);
            Assert.Equal(0, playerState.skillPoint);
            Assert.Equal(0, playerState.sigilPreview);
            Assert.Null(playerState.lockedSigil);
            Assert.False(playerState.isSigilLocked);

            Assert.True(playerState.activeCharacterInstanceId.HasValue);
            Assert.True(gameState.characterInstances.ContainsKey(playerState.activeCharacterInstanceId!.Value));
            var activeCharacter = gameState.characterInstances[playerState.activeCharacterInstanceId.Value];
            Assert.Equal(playerState.playerId, activeCharacter.ownerPlayerId);
            Assert.Equal(4, activeCharacter.currentHp);
            Assert.Equal(4, activeCharacter.maxHp);
            Assert.True(activeCharacter.isAlive);
            Assert.True(activeCharacter.isInPlay);

            Assert.Equal(4, gameState.zones[playerState.deckZoneId].cardInstanceIds.Count);
            Assert.Equal(6, gameState.zones[playerState.handZoneId].cardInstanceIds.Count);
        }

        foreach (var teamState in gameState.teams.Values)
        {
            Assert.Equal(10, teamState.killScore);
            Assert.Equal(0, teamState.leyline);
        }
    }

    private static void assertSinglePublicZone(RuleCore.GameState.GameState gameState, ZoneKey zoneKey)
    {
        var zones = gameState.zones.Values.Where(zone => zone.zoneType == zoneKey).ToList();
        Assert.Single(zones);
        Assert.Null(zones[0].ownerPlayerId);
        Assert.Equal(ZonePublicOrPrivate.publicZone, zones[0].publicOrPrivate);
    }

    private static void assertPublicZoneReference(
        RuleCore.GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey expectedZoneKey)
    {
        Assert.True(gameState.zones.ContainsKey(zoneId));
        var zoneState = gameState.zones[zoneId];
        Assert.Equal(expectedZoneKey, zoneState.zoneType);
    }
}
