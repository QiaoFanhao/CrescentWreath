using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Initialization;

public sealed class GameInitializer
{
    private const int BaseKillScore = 10;
    private const int StartingHandCount = 6;
    private const int InitialSummonZoneRevealCount = 6;
    private static readonly PlayerId PublicCardOwnerPlayerId = new(0);
    private static readonly Random SharedRandom = new();
    private static readonly object SharedRandomLock = new();
    private const string MagicCircuitDefinitionId = "starter:magicCircuit";
    private const string KourindouCouponDefinitionId = "starter:kourindouCoupon";
    private const string ActiveCharacterDefinitionId = "starter:activeCharacter";
    private const string SakuraCakeDefinitionId = "S001";
    private const int SakuraCakeInitialCount = 15;

    public GameState.GameState createStandard2v2MatchState(int? publicDeckShuffleSeed = null)
    {
        var gameState = new GameState.GameState();

        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);

        var playerA1Id = new PlayerId(1);
        var playerB1Id = new PlayerId(2);
        var playerA2Id = new PlayerId(3);
        var playerB2Id = new PlayerId(4);

        initializeMatchMeta(gameState, teamAId, teamBId, playerA1Id, playerB1Id, playerA2Id, playerB2Id);
        initializeTeams(gameState, teamAId, teamBId, playerA1Id, playerA2Id, playerB1Id, playerB2Id);
        initializeTurnState(gameState);
        initializeAnomalyState(gameState);

        var playerA1State = createPlayerWithZones(gameState, playerA1Id, teamAId, 1000);
        var playerB1State = createPlayerWithZones(gameState, playerB1Id, teamBId, 2000);
        var playerA2State = createPlayerWithZones(gameState, playerA2Id, teamAId, 3000);
        var playerB2State = createPlayerWithZones(gameState, playerB2Id, teamBId, 4000);

        gameState.publicState = createPublicZones(gameState);
        createInitialActiveCharacters(gameState, playerA1State, playerB1State, playerA2State, playerB2State);

        var nextCardInstanceId = 100000L;
        createStarterDeckAndDraw(gameState, playerA1State, ref nextCardInstanceId);
        createStarterDeckAndDraw(gameState, playerB1State, ref nextCardInstanceId);
        createStarterDeckAndDraw(gameState, playerA2State, ref nextCardInstanceId);
        createStarterDeckAndDraw(gameState, playerB2State, ref nextCardInstanceId);
        initializePublicTreasureDeckAndSummonZone(gameState, ref nextCardInstanceId, publicDeckShuffleSeed);
        initializeSakuraCakeDeck(gameState, ref nextCardInstanceId);

        gameState.matchState = MatchState.running;

        return gameState;
    }

    private static void initializeMatchMeta(
        GameState.GameState gameState,
        TeamId teamAId,
        TeamId teamBId,
        PlayerId playerA1Id,
        PlayerId playerB1Id,
        PlayerId playerA2Id,
        PlayerId playerB2Id)
    {
        var matchMeta = new MatchMeta
        {
            baseKillScore = BaseKillScore,
            mode = MatchMode.standard2v2,
        };

        matchMeta.seatOrder.Add(playerA1Id);
        matchMeta.seatOrder.Add(playerB1Id);
        matchMeta.seatOrder.Add(playerA2Id);
        matchMeta.seatOrder.Add(playerB2Id);

        matchMeta.teamAssignments.Add(playerA1Id, teamAId);
        matchMeta.teamAssignments.Add(playerA2Id, teamAId);
        matchMeta.teamAssignments.Add(playerB1Id, teamBId);
        matchMeta.teamAssignments.Add(playerB2Id, teamBId);

        matchMeta.teamSeatMap.Add(teamAId, new List<PlayerId> { playerA1Id, playerA2Id });
        matchMeta.teamSeatMap.Add(teamBId, new List<PlayerId> { playerB1Id, playerB2Id });

        gameState.matchMeta = matchMeta;
    }

    private static void initializeTurnState(GameState.GameState gameState)
    {
        var firstPlayerId = gameState.matchMeta!.seatOrder[0];
        var firstTeamId = gameState.matchMeta.teamAssignments[firstPlayerId];

        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = firstPlayerId,
            currentTeamId = firstTeamId,
            currentPhase = TurnPhase.start,
            phaseStepIndex = 0,
            hasResolvedAnomalyThisTurn = false,
        };
    }

    private static void initializeAnomalyState(GameState.GameState gameState)
    {
        var anomalyDeckDefinitionIds = new List<string>(AnomalyDefinitionRepository.getInitialDeckDefinitionIds());
        var currentAnomalyDefinitionId = anomalyDeckDefinitionIds.Count > 0
            ? anomalyDeckDefinitionIds[0]
            : null;

        if (currentAnomalyDefinitionId is not null)
        {
            anomalyDeckDefinitionIds.RemoveAt(0);
        }

        var currentAnomalyState = new CurrentAnomalyState
        {
            currentAnomalyDefinitionId = currentAnomalyDefinitionId,
        };
        currentAnomalyState.anomalyDeckDefinitionIds.AddRange(anomalyDeckDefinitionIds);

        gameState.currentAnomalyState = currentAnomalyState;
    }

    private static void initializeTeams(
        GameState.GameState gameState,
        TeamId teamAId,
        TeamId teamBId,
        PlayerId playerA1Id,
        PlayerId playerA2Id,
        PlayerId playerB1Id,
        PlayerId playerB2Id)
    {
        var teamAState = new TeamState
        {
            teamId = teamAId,
            killScore = BaseKillScore,
            leyline = 0,
        };
        teamAState.memberPlayerIds.Add(playerA1Id);
        teamAState.memberPlayerIds.Add(playerA2Id);
        gameState.teams.Add(teamAId, teamAState);

        var teamBState = new TeamState
        {
            teamId = teamBId,
            killScore = BaseKillScore,
            leyline = 0,
        };
        teamBState.memberPlayerIds.Add(playerB1Id);
        teamBState.memberPlayerIds.Add(playerB2Id);
        gameState.teams.Add(teamBId, teamBState);
    }

    private static PlayerState createPlayerWithZones(
        GameState.GameState gameState,
        PlayerId playerId,
        TeamId teamId,
        long zoneIdBase)
    {
        var playerState = new PlayerState
        {
            playerId = playerId,
            teamId = teamId,
            mana = 0,
            skillPoint = 0,
            sigilPreview = 0,
            lockedSigil = null,
            isSigilLocked = false,
            deckZoneId = new ZoneId(zoneIdBase),
            handZoneId = new ZoneId(zoneIdBase + 1),
            discardZoneId = new ZoneId(zoneIdBase + 2),
            fieldZoneId = new ZoneId(zoneIdBase + 3),
            characterSetAsideZoneId = new ZoneId(zoneIdBase + 4),
        };
        gameState.players.Add(playerId, playerState);

        createZone(gameState, playerState.deckZoneId, ZoneKey.deck, playerId, ZonePublicOrPrivate.privateZone);
        createZone(gameState, playerState.handZoneId, ZoneKey.hand, playerId, ZonePublicOrPrivate.privateZone);
        createZone(gameState, playerState.discardZoneId, ZoneKey.discard, playerId, ZonePublicOrPrivate.publicZone);
        createZone(gameState, playerState.fieldZoneId, ZoneKey.field, playerId, ZonePublicOrPrivate.publicZone);
        createZone(gameState, playerState.characterSetAsideZoneId, ZoneKey.characterSetAside, playerId, ZonePublicOrPrivate.privateZone);

        return playerState;
    }

    private static PublicState createPublicZones(GameState.GameState gameState)
    {
        var publicState = new PublicState
        {
            publicTreasureDeckZoneId = new ZoneId(9001),
            anomalyDeckZoneId = new ZoneId(9002),
            sakuraCakeDeckZoneId = new ZoneId(9003),
            summonZoneId = new ZoneId(9004),
            gapZoneId = new ZoneId(9005),
        };

        createZone(gameState, publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        createZone(gameState, publicState.anomalyDeckZoneId, ZoneKey.anomalyDeck, null, ZonePublicOrPrivate.publicZone);
        createZone(gameState, publicState.sakuraCakeDeckZoneId, ZoneKey.sakuraCakeDeck, null, ZonePublicOrPrivate.publicZone);
        createZone(gameState, publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        createZone(gameState, publicState.gapZoneId, ZoneKey.gapZone, null, ZonePublicOrPrivate.publicZone);

        return publicState;
    }

    private static void createInitialActiveCharacters(
        GameState.GameState gameState,
        PlayerState playerA1State,
        PlayerState playerB1State,
        PlayerState playerA2State,
        PlayerState playerB2State)
    {
        var nextCharacterInstanceId = 200000L;
        createInitialActiveCharacter(gameState, playerA1State, ref nextCharacterInstanceId);
        createInitialActiveCharacter(gameState, playerB1State, ref nextCharacterInstanceId);
        createInitialActiveCharacter(gameState, playerA2State, ref nextCharacterInstanceId);
        createInitialActiveCharacter(gameState, playerB2State, ref nextCharacterInstanceId);
    }

    private static void createInitialActiveCharacter(
        GameState.GameState gameState,
        PlayerState playerState,
        ref long nextCharacterInstanceId)
    {
        var characterInstanceId = new CharacterInstanceId(nextCharacterInstanceId++);
        var characterInstance = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = ActiveCharacterDefinitionId,
            ownerPlayerId = playerState.playerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.characterInstances.Add(characterInstanceId, characterInstance);
        playerState.activeCharacterInstanceId = characterInstanceId;
    }

    private static void createZone(
        GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey zoneType,
        PlayerId? ownerPlayerId,
        ZonePublicOrPrivate publicOrPrivate)
    {
        gameState.zones.Add(
            zoneId,
            new ZoneState
            {
                zoneId = zoneId,
                zoneType = zoneType,
                ownerPlayerId = ownerPlayerId,
                publicOrPrivate = publicOrPrivate,
            });
    }

    private static void createStarterDeckAndDraw(
        GameState.GameState gameState,
        PlayerState playerState,
        ref long nextCardInstanceId)
    {
        createStarterCards(gameState, playerState, MagicCircuitDefinitionId, 3, ref nextCardInstanceId);
        createStarterCards(gameState, playerState, KourindouCouponDefinitionId, 7, ref nextCardInstanceId);
        drawCards(gameState, playerState, StartingHandCount);
    }

    private static void createStarterCards(
        GameState.GameState gameState,
        PlayerState playerState,
        string definitionId,
        int count,
        ref long nextCardInstanceId)
    {
        var deckZoneState = gameState.zones[playerState.deckZoneId];

        for (var i = 0; i < count; i++)
        {
            var cardInstanceId = new CardInstanceId(nextCardInstanceId++);
            var cardInstance = new CardInstance
            {
                cardInstanceId = cardInstanceId,
                definitionId = definitionId,
                ownerPlayerId = playerState.playerId,
                zoneId = playerState.deckZoneId,
                zoneKey = ZoneKey.deck,
            };

            gameState.cardInstances.Add(cardInstanceId, cardInstance);
            deckZoneState.cardInstanceIds.Add(cardInstanceId);
        }
    }

    private static void drawCards(GameState.GameState gameState, PlayerState playerState, int drawCount)
    {
        var deckZoneState = gameState.zones[playerState.deckZoneId];
        var handZoneState = gameState.zones[playerState.handZoneId];

        for (var i = 0; i < drawCount; i++)
        {
            var cardInstanceId = deckZoneState.cardInstanceIds[0];
            deckZoneState.cardInstanceIds.RemoveAt(0);
            handZoneState.cardInstanceIds.Add(cardInstanceId);

            var cardInstance = gameState.cardInstances[cardInstanceId];
            cardInstance.zoneId = playerState.handZoneId;
            cardInstance.zoneKey = ZoneKey.hand;
        }
    }

    private static void initializePublicTreasureDeckAndSummonZone(
        GameState.GameState gameState,
        ref long nextCardInstanceId,
        int? publicDeckShuffleSeed)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("GameInitializer requires gameState.publicState to be initialized before public treasure deck setup.");
        }

        var publicTreasureDeckZoneState = gameState.zones[gameState.publicState.publicTreasureDeckZoneId];
        var summonZoneState = gameState.zones[gameState.publicState.summonZoneId];

        var publicDeckDefinitionIds = new List<string>(TreasureDefinitionRepository.getInitialPublicDeckDefinitionIds());
        shuffleDefinitionIdsInPlace(publicDeckDefinitionIds, publicDeckShuffleSeed);

        foreach (var definitionId in publicDeckDefinitionIds)
        {
            var cardInstanceId = new CardInstanceId(nextCardInstanceId++);
            var cardInstance = new CardInstance
            {
                cardInstanceId = cardInstanceId,
                definitionId = definitionId,
                ownerPlayerId = PublicCardOwnerPlayerId,
                zoneId = gameState.publicState.publicTreasureDeckZoneId,
                zoneKey = ZoneKey.publicTreasureDeck,
            };

            gameState.cardInstances.Add(cardInstanceId, cardInstance);
            publicTreasureDeckZoneState.cardInstanceIds.Add(cardInstanceId);
        }

        for (var revealIndex = 0;
             revealIndex < InitialSummonZoneRevealCount && publicTreasureDeckZoneState.cardInstanceIds.Count > 0;
             revealIndex++)
        {
            var topCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
            publicTreasureDeckZoneState.cardInstanceIds.RemoveAt(0);
            summonZoneState.cardInstanceIds.Add(topCardInstanceId);

            var topCardInstance = gameState.cardInstances[topCardInstanceId];
            topCardInstance.zoneId = gameState.publicState.summonZoneId;
            topCardInstance.zoneKey = ZoneKey.summonZone;
        }
    }

    private static void initializeSakuraCakeDeck(GameState.GameState gameState, ref long nextCardInstanceId)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("GameInitializer requires gameState.publicState to be initialized before sakura cake deck setup.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.sakuraCakeDeckZoneId, out var sakuraCakeDeckZoneState))
        {
            throw new InvalidOperationException("GameInitializer requires sakuraCakeDeck zone to exist in gameState.zones before sakura cake deck setup.");
        }

        for (var index = 0; index < SakuraCakeInitialCount; index++)
        {
            var cardInstanceId = new CardInstanceId(nextCardInstanceId++);
            var cardInstance = new CardInstance
            {
                cardInstanceId = cardInstanceId,
                definitionId = SakuraCakeDefinitionId,
                ownerPlayerId = PublicCardOwnerPlayerId,
                zoneId = gameState.publicState.sakuraCakeDeckZoneId,
                zoneKey = ZoneKey.sakuraCakeDeck,
            };

            gameState.cardInstances.Add(cardInstanceId, cardInstance);
            sakuraCakeDeckZoneState.cardInstanceIds.Add(cardInstanceId);
        }
    }

    private static void shuffleDefinitionIdsInPlace(List<string> definitionIds, int? seed)
    {
        var random = seed.HasValue ? new Random(seed.Value) : null;
        for (var index = definitionIds.Count - 1; index > 0; index--)
        {
            var randomIndex = random is not null
                ? random.Next(index + 1)
                : nextSharedRandom(index + 1);
            (definitionIds[index], definitionIds[randomIndex]) = (definitionIds[randomIndex], definitionIds[index]);
        }
    }

    private static int nextSharedRandom(int maxExclusive)
    {
        lock (SharedRandomLock)
        {
            return SharedRandom.Next(maxExclusive);
        }
    }
}
