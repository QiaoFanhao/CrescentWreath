using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class AnomalyArrivalRuntimeTests
{
    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepsIsEmpty_ShouldNoOp()
    {
        var gameState = new RuleCore.GameState.GameState();
        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99001),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A-test",
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88001,
            new ZoneMovementService(),
            anomalyDefinition);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepIsLegacyNoop_ShouldNoOp()
    {
        var gameState = new RuleCore.GameState.GameState();
        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99002),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A-test",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "legacyNoop",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88002,
            new ZoneMovementService(),
            anomalyDefinition);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepbanishSummonZoneToGap_ShouldMoveAllCardsAndAppendEvents()
    {
        var summonZoneId = new ZoneId(101);
        var gapZoneId = new ZoneId(102);
        var cardAId = new CardInstanceId(1001);
        var cardBId = new CardInstanceId(1002);

        var gameState = new RuleCore.GameState.GameState
        {
            publicState = new RuleCore.GameState.PublicState
            {
                summonZoneId = summonZoneId,
                gapZoneId = gapZoneId,
            },
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[gapZoneId] = new ZoneState
        {
            zoneId = gapZoneId,
            zoneType = ZoneKey.gapZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.cardInstances[cardAId] = new CardInstance
        {
            cardInstanceId = cardAId,
            definitionId = "test:arrival-a",
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
            isFaceUp = true,
        };
        gameState.cardInstances[cardBId] = new CardInstance
        {
            cardInstanceId = cardBId,
            definitionId = "test:arrival-b",
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
            isFaceUp = true,
        };
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardAId);
        gameState.zones[summonZoneId].cardInstanceIds.Add(cardBId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99004),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A009",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "banishSummonZoneToGap",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88004,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.Empty(gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Equal(2, gameState.zones[gapZoneId].cardInstanceIds.Count);
        Assert.Equal(gapZoneId, gameState.cardInstances[cardAId].zoneId);
        Assert.Equal(gapZoneId, gameState.cardInstances[cardBId].zoneId);
        Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[cardAId].zoneKey);
        Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[cardBId].zoneKey);
        Assert.Equal(2, actionChainState.producedEvents.Count);
        var movedEventA = Assert.IsType<CrescentWreath.RuleCore.Events.CardMovedEvent>(actionChainState.producedEvents[0]);
        var movedEventB = Assert.IsType<CrescentWreath.RuleCore.Events.CardMovedEvent>(actionChainState.producedEvents[1]);
        Assert.Equal(CardMoveReason.banish, movedEventA.moveReason);
        Assert.Equal(CardMoveReason.banish, movedEventB.moveReason);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepbanishSummonZoneToGapAndSourceZoneIsEmpty_ShouldNoOp()
    {
        var summonZoneId = new ZoneId(201);
        var gapZoneId = new ZoneId(202);
        var gameState = new RuleCore.GameState.GameState
        {
            publicState = new RuleCore.GameState.PublicState
            {
                summonZoneId = summonZoneId,
                gapZoneId = gapZoneId,
            },
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[gapZoneId] = new ZoneState
        {
            zoneId = gapZoneId,
            zoneType = ZoneKey.gapZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99005),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A009",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "banishSummonZoneToGap",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88005,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.Empty(actionChainState.producedEvents);
        Assert.Empty(gameState.zones[summonZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[gapZoneId].cardInstanceIds);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepbanishSummonZoneToGapAndGapZoneMissing_ShouldThrow()
    {
        var summonZoneId = new ZoneId(301);
        var gapZoneId = new ZoneId(302);
        var gameState = new RuleCore.GameState.GameState
        {
            publicState = new RuleCore.GameState.PublicState
            {
                summonZoneId = summonZoneId,
                gapZoneId = gapZoneId,
            },
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99006),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A009",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "banishSummonZoneToGap",
                },
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            executeArrivalOnFlip(
                gameState,
                actionChainState,
                88006,
                new ZoneMovementService(),
                anomalyDefinition));

        Assert.Equal(
            "Anomaly arrival runtime requires gapZoneId to exist for arrivalStepKey: banishSummonZoneToGap.",
            exception.Message);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepApplySealToLeadingTeamActiveCharacters_ShouldApplySealToLeadingTeamOnly()
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var playerAId = new PlayerId(1);
        var playerBId = new PlayerId(2);
        var characterAId = new CharacterInstanceId(401);
        var characterBId = new CharacterInstanceId(402);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[teamAId] = new RuleCore.GameState.TeamState
        {
            teamId = teamAId,
            killScore = 6,
            memberPlayerIds = { playerAId },
        };
        gameState.teams[teamBId] = new RuleCore.GameState.TeamState
        {
            teamId = teamBId,
            killScore = 4,
            memberPlayerIds = { playerBId },
        };
        gameState.players[playerAId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerAId,
            teamId = teamAId,
            activeCharacterInstanceId = characterAId,
        };
        gameState.players[playerBId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerBId,
            teamId = teamBId,
            activeCharacterInstanceId = characterBId,
        };
        gameState.characterInstances[characterAId] = new CharacterInstance
        {
            characterInstanceId = characterAId,
            definitionId = "test:a",
            ownerPlayerId = playerAId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[characterBId] = new CharacterInstance
        {
            characterInstanceId = characterBId,
            definitionId = "test:b",
            ownerPlayerId = playerBId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99007),
            actorPlayerId = playerBId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A008",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88007,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, characterAId, "Seal"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterBId, "Seal"));
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepApplySealToLeadingTeamActiveCharactersAndLeadingTeamContainsC009_ShouldSkipC009ButSealOtherLeadingCharacters()
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var playerA1Id = new PlayerId(1);
        var playerA2Id = new PlayerId(3);
        var playerBId = new PlayerId(2);
        var characterA1Id = new CharacterInstanceId(451);
        var characterA2Id = new CharacterInstanceId(452);
        var characterBId = new CharacterInstanceId(453);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[teamAId] = new RuleCore.GameState.TeamState
        {
            teamId = teamAId,
            killScore = 8,
            memberPlayerIds = { playerA1Id, playerA2Id },
        };
        gameState.teams[teamBId] = new RuleCore.GameState.TeamState
        {
            teamId = teamBId,
            killScore = 4,
            memberPlayerIds = { playerBId },
        };
        gameState.players[playerA1Id] = new RuleCore.GameState.PlayerState
        {
            playerId = playerA1Id,
            teamId = teamAId,
            activeCharacterInstanceId = characterA1Id,
        };
        gameState.players[playerA2Id] = new RuleCore.GameState.PlayerState
        {
            playerId = playerA2Id,
            teamId = teamAId,
            activeCharacterInstanceId = characterA2Id,
        };
        gameState.players[playerBId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerBId,
            teamId = teamBId,
            activeCharacterInstanceId = characterBId,
        };
        gameState.characterInstances[characterA1Id] = new CharacterInstance
        {
            characterInstanceId = characterA1Id,
            definitionId = "C009",
            ownerPlayerId = playerA1Id,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[characterA2Id] = new CharacterInstance
        {
            characterInstanceId = characterA2Id,
            definitionId = "test:leading-non-exempt",
            ownerPlayerId = playerA2Id,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[characterBId] = new CharacterInstance
        {
            characterInstanceId = characterBId,
            definitionId = "test:non-leading",
            ownerPlayerId = playerBId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(990071),
            actorPlayerId = playerBId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A008",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            880071,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterA1Id, "Seal"));
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, characterA2Id, "Seal"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterBId, "Seal"));
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepApplySealToLeadingTeamActiveCharactersAndLeadingTeamOnlyC009_ShouldNoOp()
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var playerAId = new PlayerId(1);
        var playerBId = new PlayerId(2);
        var characterAId = new CharacterInstanceId(471);
        var characterBId = new CharacterInstanceId(472);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[teamAId] = new RuleCore.GameState.TeamState
        {
            teamId = teamAId,
            killScore = 9,
            memberPlayerIds = { playerAId },
        };
        gameState.teams[teamBId] = new RuleCore.GameState.TeamState
        {
            teamId = teamBId,
            killScore = 3,
            memberPlayerIds = { playerBId },
        };
        gameState.players[playerAId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerAId,
            teamId = teamAId,
            activeCharacterInstanceId = characterAId,
        };
        gameState.players[playerBId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerBId,
            teamId = teamBId,
            activeCharacterInstanceId = characterBId,
        };
        gameState.characterInstances[characterAId] = new CharacterInstance
        {
            characterInstanceId = characterAId,
            definitionId = "C009",
            ownerPlayerId = playerAId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[characterBId] = new CharacterInstance
        {
            characterInstanceId = characterBId,
            definitionId = "test:non-leading",
            ownerPlayerId = playerBId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(990072),
            actorPlayerId = playerBId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A008",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            880072,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterAId, "Seal"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterBId, "Seal"));
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepApplySealToLeadingTeamActiveCharactersAndKillScoreTied_ShouldNoOp()
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var playerAId = new PlayerId(1);
        var playerBId = new PlayerId(2);
        var characterAId = new CharacterInstanceId(501);
        var characterBId = new CharacterInstanceId(502);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[teamAId] = new RuleCore.GameState.TeamState
        {
            teamId = teamAId,
            killScore = 5,
            memberPlayerIds = { playerAId },
        };
        gameState.teams[teamBId] = new RuleCore.GameState.TeamState
        {
            teamId = teamBId,
            killScore = 5,
            memberPlayerIds = { playerBId },
        };
        gameState.players[playerAId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerAId,
            teamId = teamAId,
            activeCharacterInstanceId = characterAId,
        };
        gameState.players[playerBId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerBId,
            teamId = teamBId,
            activeCharacterInstanceId = characterBId,
        };
        gameState.characterInstances[characterAId] = new CharacterInstance
        {
            characterInstanceId = characterAId,
            definitionId = "test:a",
            ownerPlayerId = playerAId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[characterBId] = new CharacterInstance
        {
            characterInstanceId = characterBId,
            definitionId = "test:b",
            ownerPlayerId = playerBId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99008),
            actorPlayerId = playerAId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A008",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
        };

        executeArrivalOnFlip(
            gameState,
            actionChainState,
            88008,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterAId, "Seal"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, characterBId, "Seal"));
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepApplySealToLeadingTeamActiveCharactersAndLeadingPlayerMissingActiveCharacter_ShouldThrow()
    {
        var teamAId = new TeamId(1);
        var teamBId = new TeamId(2);
        var playerAId = new PlayerId(1);
        var playerBId = new PlayerId(2);
        var characterBId = new CharacterInstanceId(602);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[teamAId] = new RuleCore.GameState.TeamState
        {
            teamId = teamAId,
            killScore = 7,
            memberPlayerIds = { playerAId },
        };
        gameState.teams[teamBId] = new RuleCore.GameState.TeamState
        {
            teamId = teamBId,
            killScore = 4,
            memberPlayerIds = { playerBId },
        };
        gameState.players[playerAId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerAId,
            teamId = teamAId,
            activeCharacterInstanceId = null,
        };
        gameState.players[playerBId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerBId,
            teamId = teamBId,
            activeCharacterInstanceId = characterBId,
        };
        gameState.characterInstances[characterBId] = new CharacterInstance
        {
            characterInstanceId = characterBId,
            definitionId = "test:b",
            ownerPlayerId = playerBId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99009),
            actorPlayerId = playerBId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A008",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applySealToLeadingTeamActiveCharacters",
                },
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            executeArrivalOnFlip(
                gameState,
                actionChainState,
                88009,
                new ZoneMovementService(),
                anomalyDefinition));

        Assert.Equal(
            "Anomaly arrival runtime requires activeCharacterInstanceId for leading team players for arrivalStepKey: applySealToLeadingTeamActiveCharacters.",
            exception.Message);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepDirectSummonFromSummonZoneWithInputAndSummonZoneHasCards_ShouldOpenInputContextAndSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var summonZoneId = new ZoneId(711);
        var summonCardId = new CardInstanceId(7121);

        var gameState = new RuleCore.GameState.GameState
        {
            publicState = new RuleCore.GameState.PublicState
            {
                summonZoneId = summonZoneId,
            },
        };
        gameState.players[actorPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = actorPlayerId,
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.cardInstances[summonCardId] = new CardInstance
        {
            cardInstanceId = summonCardId,
            definitionId = "test:arrival-summon",
            ownerPlayerId = actorPlayerId,
            zoneId = summonZoneId,
            zoneKey = ZoneKey.summonZone,
            isFaceUp = true,
        };
        gameState.zones[summonZoneId].cardInstanceIds.Add(summonCardId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99010),
            actorPlayerId = actorPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "directSummonFromSummonZoneWithInput",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            88010,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.True(isSuspendedByInput);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A005:arrivalDirectSummonFromSummonZone", gameState.currentInputContext!.contextKey);
        Assert.Equal("anomalyA005ArrivalDirectSummonFromSummonZone", gameState.currentInputContext.inputTypeKey);
        Assert.Equal(actorPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.Single(gameState.currentInputContext.choiceKeys);
        Assert.Equal("summonCard:7121", gameState.currentInputContext.choiceKeys[0]);
        Assert.Equal(
            AnomalyProcessor.ContinuationKeyA005ArrivalDirectSummonFromSummonZone,
            actionChainState.pendingContinuationKey);
        var openedEvent = Assert.IsType<InteractionWindowEvent>(actionChainState.producedEvents[0]);
        Assert.Equal("inputContextOpened", openedEvent.eventTypeKey);
        Assert.False(actionChainState.isCompleted);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepDirectSummonFromSummonZoneWithInputAndSummonZoneEmpty_ShouldNoOpWithoutSuspending()
    {
        var actorPlayerId = new PlayerId(1);
        var summonZoneId = new ZoneId(721);

        var gameState = new RuleCore.GameState.GameState
        {
            publicState = new RuleCore.GameState.PublicState
            {
                summonZoneId = summonZoneId,
            },
        };
        gameState.players[actorPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = actorPlayerId,
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99011),
            actorPlayerId = actorPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "directSummonFromSummonZoneWithInput",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            88011,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(isSuspendedByInput);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Empty(actionChainState.producedEvents);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepA003SelectOpponentShackleAndTeamHasC023_ShouldGrantLeylineAndOpenInput()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var opponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorCharacterId = new CharacterInstanceId(8101);
        var allyYuukaCharacterId = new CharacterInstanceId(8102);
        var opponentCharacterId = new CharacterInstanceId(8103);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[actorTeamId] = new RuleCore.GameState.TeamState
        {
            teamId = actorTeamId,
            killScore = 0,
            leyline = 0,
            memberPlayerIds = { actorPlayerId, allyPlayerId },
        };
        gameState.teams[opponentTeamId] = new RuleCore.GameState.TeamState
        {
            teamId = opponentTeamId,
            killScore = 0,
            leyline = 0,
            memberPlayerIds = { opponentPlayerId },
        };
        gameState.players[actorPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = actorCharacterId,
        };
        gameState.players[allyPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = allyPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = allyYuukaCharacterId,
        };
        gameState.players[opponentPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = opponentPlayerId,
            teamId = opponentTeamId,
            activeCharacterInstanceId = opponentCharacterId,
        };
        gameState.characterInstances[actorCharacterId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterId,
            definitionId = "test:actor",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[allyYuukaCharacterId] = new CharacterInstance
        {
            characterInstanceId = allyYuukaCharacterId,
            definitionId = "C023",
            ownerPlayerId = allyPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[opponentCharacterId] = new CharacterInstance
        {
            characterInstanceId = opponentCharacterId,
            definitionId = "test:opponent",
            ownerPlayerId = opponentPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.matchMeta = new RuleCore.GameState.MatchMeta();
        gameState.matchMeta.seatOrder.Add(actorPlayerId);
        gameState.matchMeta.seatOrder.Add(opponentPlayerId);
        gameState.matchMeta.seatOrder.Add(allyPlayerId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99012),
            actorPlayerId = actorPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A003",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "selectOpponentApplyShackleAndGrantYuukaTeamLeyline",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            88012,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.True(isSuspendedByInput);
        Assert.Equal(1, gameState.teams[actorTeamId].leyline);
        Assert.Equal(0, gameState.teams[opponentTeamId].leyline);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A003:arrivalSelectOpponentShackle", gameState.currentInputContext!.contextKey);
        Assert.Equal("anomalyA003ArrivalSelectOpponentShackle", gameState.currentInputContext.inputTypeKey);
        Assert.Equal(actorPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.Contains("opponentPlayer:2", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("opponentPlayer:3", gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle, actionChainState.pendingContinuationKey);
        Assert.Single(actionChainState.producedEvents);
        Assert.IsType<InteractionWindowEvent>(actionChainState.producedEvents[0]);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepA001RaceFlowWithRemiliaHealAndNoHumanDiscardCandidate_ShouldDrawNonHumanAndHealC003WithoutSuspend()
    {
        var humanPlayerId = new PlayerId(1);
        var nonHumanPlayerId = new PlayerId(2);
        var remiliaPlayerId = new PlayerId(3);
        var humanCharacterId = new CharacterInstanceId(9111);
        var nonHumanCharacterId = new CharacterInstanceId(9112);
        var remiliaCharacterId = new CharacterInstanceId(9113);

        var gameState = new RuleCore.GameState.GameState
        {
            matchMeta = new RuleCore.GameState.MatchMeta(),
        };
        gameState.matchMeta.seatOrder.Add(humanPlayerId);
        gameState.matchMeta.seatOrder.Add(nonHumanPlayerId);
        gameState.matchMeta.seatOrder.Add(remiliaPlayerId);

        addPlayerWithStandardZones(gameState, humanPlayerId);
        addPlayerWithStandardZones(gameState, nonHumanPlayerId);
        addPlayerWithStandardZones(gameState, remiliaPlayerId);

        gameState.players[humanPlayerId].activeCharacterInstanceId = humanCharacterId;
        gameState.players[nonHumanPlayerId].activeCharacterInstanceId = nonHumanCharacterId;
        gameState.players[remiliaPlayerId].activeCharacterInstanceId = remiliaCharacterId;

        gameState.characterInstances[humanCharacterId] = new CharacterInstance
        {
            characterInstanceId = humanCharacterId,
            definitionId = "test:human",
            ownerPlayerId = humanPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[nonHumanCharacterId] = new CharacterInstance
        {
            characterInstanceId = nonHumanCharacterId,
            definitionId = "test:nonHuman",
            ownerPlayerId = nonHumanPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[remiliaCharacterId] = new CharacterInstance
        {
            characterInstanceId = remiliaCharacterId,
            definitionId = "C003",
            ownerPlayerId = remiliaPlayerId,
            currentHp = 1,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var nonHumanDeckCardId = new CardInstanceId(9121);
        var nonHumanDeckZoneId = gameState.players[nonHumanPlayerId].deckZoneId;
        gameState.cardInstances[nonHumanDeckCardId] = new CardInstance
        {
            cardInstanceId = nonHumanDeckCardId,
            definitionId = "test:a001-deck-card",
            ownerPlayerId = nonHumanPlayerId,
            zoneId = nonHumanDeckZoneId,
            zoneKey = ZoneKey.deck,
            isFaceUp = false,
        };
        gameState.zones[nonHumanDeckZoneId].cardInstanceIds.Add(nonHumanDeckCardId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(990131),
            actorPlayerId = humanPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A001",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applyA001RaceFlowWithRemiliaHealInput",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            880131,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(isSuspendedByInput);
        Assert.Null(gameState.currentInputContext);
        Assert.Equal(4, gameState.characterInstances[remiliaCharacterId].currentHp);
        Assert.Empty(gameState.zones[nonHumanDeckZoneId].cardInstanceIds);
        Assert.Contains(nonHumanDeckCardId, gameState.zones[gameState.players[nonHumanPlayerId].handZoneId].cardInstanceIds);
        Assert.Single(actionChainState.producedEvents);
        var drawEvent = Assert.IsType<CrescentWreath.RuleCore.Events.CardMovedEvent>(actionChainState.producedEvents[0]);
        Assert.Equal(CardMoveReason.draw, drawEvent.moveReason);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepA006RaceFlowWithDefenseDiscardInputAndHumanHasDefensePlacedCard_ShouldOpenInputAndSuspend()
    {
        var humanPlayerId = new PlayerId(1);
        var nonHumanPlayerId = new PlayerId(2);
        var humanCharacterId = new CharacterInstanceId(9211);
        var nonHumanCharacterId = new CharacterInstanceId(9212);
        var humanFieldCardId = new CardInstanceId(9221);

        var gameState = new RuleCore.GameState.GameState
        {
            matchMeta = new RuleCore.GameState.MatchMeta(),
        };
        gameState.matchMeta.seatOrder.Add(humanPlayerId);
        gameState.matchMeta.seatOrder.Add(nonHumanPlayerId);

        addPlayerWithStandardZones(gameState, humanPlayerId);
        addPlayerWithStandardZones(gameState, nonHumanPlayerId);

        gameState.players[humanPlayerId].activeCharacterInstanceId = humanCharacterId;
        gameState.players[nonHumanPlayerId].activeCharacterInstanceId = nonHumanCharacterId;

        gameState.characterInstances[humanCharacterId] = new CharacterInstance
        {
            characterInstanceId = humanCharacterId,
            definitionId = "test:human",
            ownerPlayerId = humanPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[nonHumanCharacterId] = new CharacterInstance
        {
            characterInstanceId = nonHumanCharacterId,
            definitionId = "test:nonHuman",
            ownerPlayerId = nonHumanPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var humanFieldZoneId = gameState.players[humanPlayerId].fieldZoneId;
        gameState.cardInstances[humanFieldCardId] = new CardInstance
        {
            cardInstanceId = humanFieldCardId,
            definitionId = "test:a006-defense-placed",
            ownerPlayerId = humanPlayerId,
            zoneId = humanFieldZoneId,
            zoneKey = ZoneKey.field,
            isFaceUp = true,
            isDefensePlacedOnField = true,
        };
        gameState.zones[humanFieldZoneId].cardInstanceIds.Add(humanFieldCardId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(990132),
            actorPlayerId = humanPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A006",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "applyA006RaceFlowWithDefenseDiscardInput",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            880132,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.True(isSuspendedByInput);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("anomaly:A006:arrivalHumanDefenseDiscardFlow", gameState.currentInputContext!.contextKey);
        Assert.Equal("anomalyA006ArrivalHumanDefenseDiscardOne", gameState.currentInputContext.inputTypeKey);
        Assert.Equal(humanPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.Contains("fieldCard:9221", gameState.currentInputContext.choiceKeys);
        Assert.Equal(AnomalyProcessor.ContinuationKeyA006ArrivalHumanDefenseDiscardFlow, actionChainState.pendingContinuationKey);
        Assert.Single(actionChainState.producedEvents);
        Assert.IsType<InteractionWindowEvent>(actionChainState.producedEvents[0]);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepA003HasNoOpponentTargets_ShouldGrantLeylineAndNotSuspend()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorCharacterId = new CharacterInstanceId(8201);

        var gameState = new RuleCore.GameState.GameState();
        gameState.teams[actorTeamId] = new RuleCore.GameState.TeamState
        {
            teamId = actorTeamId,
            killScore = 0,
            leyline = 0,
            memberPlayerIds = { actorPlayerId },
        };
        gameState.players[actorPlayerId] = new RuleCore.GameState.PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = actorCharacterId,
        };
        gameState.characterInstances[actorCharacterId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterId,
            definitionId = "C023",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99013),
            actorPlayerId = actorPlayerId,
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A003",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "selectOpponentApplyShackleAndGrantYuukaTeamLeyline",
                },
            },
        };

        var isSuspendedByInput = executeArrivalOnFlip(
            gameState,
            actionChainState,
            88013,
            new ZoneMovementService(),
            anomalyDefinition);

        Assert.False(isSuspendedByInput);
        Assert.Equal(1, gameState.teams[actorTeamId].leyline);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Empty(actionChainState.producedEvents);
    }

    [Fact]
    public void ExecuteOnFlip_WhenArrivalStepIsUnsupported_ShouldThrow()
    {
        var gameState = new RuleCore.GameState.GameState();
        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(99003),
        };
        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A-test",
            arrivalSteps =
            {
                new AnomalyArrivalStepDefinition
                {
                    arrivalStepKey = "unsupportedArrivalStep",
                },
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            executeArrivalOnFlip(
                gameState,
                actionChainState,
                88003,
                new ZoneMovementService(),
                anomalyDefinition));

        Assert.Equal(
            "Anomaly arrival runtime does not support arrivalStepKey: unsupportedArrivalStep.",
            exception.Message);
    }

    private static bool executeArrivalOnFlip(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        ZoneMovementService zoneMovementService,
        AnomalyDefinition anomalyDefinition)
    {
        var arrivalInputRuntime = new AnomalyArrivalInputRuntime(zoneMovementService, () => 89000);
        return AnomalyArrivalRuntime.executeOnFlip(
            gameState,
            actionChainState,
            requestId,
            zoneMovementService,
            anomalyDefinition,
            arrivalInputRuntime,
            AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle,
            AnomalyProcessor.ContinuationKeyA005ArrivalDirectSummonFromSummonZone,
            AnomalyProcessor.ContinuationKeyA007ArrivalOptionalBanishFlow,
            AnomalyProcessor.ContinuationKeyA001ArrivalHumanDiscardFlow,
            AnomalyProcessor.ContinuationKeyA006ArrivalHumanDefenseDiscardFlow);
    }

    private static void addPlayerWithStandardZones(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId)
    {
        var deckZoneId = new ZoneId(10000 + (playerId.Value * 10) + 1);
        var handZoneId = new ZoneId(10000 + (playerId.Value * 10) + 2);
        var discardZoneId = new ZoneId(10000 + (playerId.Value * 10) + 3);
        var fieldZoneId = new ZoneId(10000 + (playerId.Value * 10) + 4);

        gameState.players[playerId] = new RuleCore.GameState.PlayerState
        {
            playerId = playerId,
            teamId = new TeamId(1),
            deckZoneId = deckZoneId,
            handZoneId = handZoneId,
            discardZoneId = discardZoneId,
            fieldZoneId = fieldZoneId,
        };

        gameState.zones[deckZoneId] = new ZoneState
        {
            zoneId = deckZoneId,
            zoneType = ZoneKey.deck,
            ownerPlayerId = playerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
        gameState.zones[handZoneId] = new ZoneState
        {
            zoneId = handZoneId,
            zoneType = ZoneKey.hand,
            ownerPlayerId = playerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
        gameState.zones[discardZoneId] = new ZoneState
        {
            zoneId = discardZoneId,
            zoneType = ZoneKey.discard,
            ownerPlayerId = playerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[fieldZoneId] = new ZoneState
        {
            zoneId = fieldZoneId,
            zoneType = ZoneKey.field,
            ownerPlayerId = playerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
    }
}
