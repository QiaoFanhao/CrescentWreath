using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class AnomalyRewardExecutorTests
{
    [Fact]
    public void TryValidateRewardContext_WhenTargetedStatusStepHasNoTarget_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var gameState = createRewardExecutionReadyGameState(actorPlayerId, new PlayerId(2), actorTeamId, new TeamId(2));

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardStatusKey = string.Empty,
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Charm",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isValid);
        Assert.Equal("targetPlayerRequired", failedReasonKey);
    }

    [Fact]
    public void EvaluateRewardContext_WhenValidationFails_ShouldReturnRewardContextFailureStage()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var gameState = createRewardExecutionReadyGameState(actorPlayerId, new PlayerId(2), actorTeamId, new TeamId(2));

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardStatusKey = string.Empty,
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Charm",
                },
            },
        };

        var validationResult = AnomalyResolveRewardRuntime.evaluateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition);

        Assert.False(validationResult.isPassed);
        Assert.Equal("targetPlayerRequired", validationResult.failedReasonKey);
        Assert.Equal(AnomalyValidationFailureStage.rewardContext, validationResult.failureStage);
    }

    [Fact]
    public void ExecuteSteps_WhenRewardStepsContainTeamDeltaAndApplyStatus_ShouldApplyBoth()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var targetTeamId = new TeamId(2);
        var gameState = createRewardExecutionReadyGameState(actorPlayerId, targetPlayerId, actorTeamId, targetTeamId);
        gameState.teams[actorTeamId].leyline = 1;
        gameState.teams[targetTeamId].killScore = 3;

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 2,
                    opponentTeamKillScoreDelta = -1,
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Charm",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition,
            out var failedReasonKey);
        Assert.True(isValid);
        Assert.Null(failedReasonKey);

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId, anomalyDefinition);

        Assert.Equal(3, gameState.teams[actorTeamId].leyline);
        Assert.Equal(2, gameState.teams[targetTeamId].killScore);
        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Charm"));
    }

    [Fact]
    public void ExecuteSteps_WhenOptionalTargetedStatusStepHasNoTarget_ShouldSkipAndKeepOtherStepsApplied()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var targetTeamId = new TeamId(2);
        var gameState = createRewardExecutionReadyGameState(actorPlayerId, targetPlayerId, actorTeamId, targetTeamId);
        gameState.teams[actorTeamId].leyline = 1;
        gameState.teams[targetTeamId].killScore = 3;

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "teamDelta",
                    actorTeamLeylineDelta = 2,
                    opponentTeamKillScoreDelta = -1,
                },
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Charm",
                    isOptional = true,
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);
        Assert.True(isValid);
        Assert.Null(failedReasonKey);

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId: null, anomalyDefinition);

        Assert.Equal(3, gameState.teams[actorTeamId].leyline);
        Assert.Equal(2, gameState.teams[targetTeamId].killScore);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Charm"));
    }

    [Fact]
    public void ExecuteSteps_WhenStatusIsCharacterScoped_ShouldApplyToTargetActiveCharacter()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var targetTeamId = new TeamId(2);
        var gameState = createRewardExecutionReadyGameState(actorPlayerId, targetPlayerId, actorTeamId, targetTeamId);

        var targetCharacterInstanceId = gameState.players[targetPlayerId].activeCharacterInstanceId!.Value;

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "applyStatusToTargetOpponent",
                    statusKey = "Shackle",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition,
            out var failedReasonKey);
        Assert.True(isValid);
        Assert.Null(failedReasonKey);

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId, anomalyDefinition);

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Shackle"));
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Shackle"));
    }

    [Fact]
    public void ExecuteSteps_WhenRewardStepIsHealFriendlyNonHumanActiveCharactersToMaxHp_ShouldOnlyHealFriendlyNonHuman()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var gameState = createHealStepReadyGameState(
            actorPlayerId,
            allyPlayerId,
            enemyPlayerId,
            actorTeamId,
            enemyTeamId);

        var actorCharacterInstanceId = gameState.players[actorPlayerId].activeCharacterInstanceId!.Value;
        var allyCharacterInstanceId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        var enemyCharacterInstanceId = gameState.players[enemyPlayerId].activeCharacterInstanceId!.Value;

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "test:anomaly",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "healFriendlyNonHumanActiveCharactersToMaxHp",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);
        Assert.True(isValid);
        Assert.Null(failedReasonKey);

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId: null, anomalyDefinition);

        Assert.Equal(2, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Equal(4, gameState.characterInstances[allyCharacterInstanceId].currentHp);
        Assert.Equal(1, gameState.characterInstances[enemyCharacterInstanceId].currentHp);
    }

    [Fact]
    public void TryValidateRewardContext_WhenMoveCardFromZoneToZoneAndSourceZoneIsUnsupported_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "gapZone",
                    toZoneKey = "hand",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isValid);
        Assert.Equal("rewardSourceZoneUnsupported", failedReasonKey);
    }

    [Fact]
    public void TryValidateRewardContext_WhenMoveCardFromZoneToZoneAndTargetZoneStateMissing_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);
        gameState.zones.Remove(gameState.players[actorPlayerId].handZoneId);

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isValid);
        Assert.Equal("rewardTargetZoneMissing", failedReasonKey);
    }

    [Fact]
    public void ExecuteSteps_WhenRewardStepIsMoveCardFromZoneToZone_ShouldMoveSourceTopCard()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);
        var discardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var handZoneId = gameState.players[actorPlayerId].handZoneId;
        var sourceTopCardId = gameState.zones[discardZoneId].cardInstanceIds[0];

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);
        Assert.True(isValid);
        Assert.Null(failedReasonKey);

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId: null, anomalyDefinition);

        Assert.DoesNotContain(sourceTopCardId, gameState.zones[discardZoneId].cardInstanceIds);
        Assert.Contains(sourceTopCardId, gameState.zones[handZoneId].cardInstanceIds);
        Assert.Equal(handZoneId, gameState.cardInstances[sourceTopCardId].zoneId);
        Assert.Equal(ZoneKey.hand, gameState.cardInstances[sourceTopCardId].zoneKey);
    }

    [Fact]
    public void TryValidateRewardContext_WhenOptionalMoveCardFromZoneToZoneAndSourceZoneIsEmpty_ShouldPass()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);
        var discardZoneId = gameState.players[actorPlayerId].discardZoneId;
        gameState.zones[discardZoneId].cardInstanceIds.Clear();

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                    isOptional = true,
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.True(isValid);
        Assert.Null(failedReasonKey);
    }

    [Fact]
    public void ExecuteSteps_WhenOptionalMoveCardFromZoneToZoneAndSourceZoneIsEmpty_ShouldNoOp()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);
        var discardZoneId = gameState.players[actorPlayerId].discardZoneId;
        var handZoneId = gameState.players[actorPlayerId].handZoneId;
        gameState.zones[discardZoneId].cardInstanceIds.Clear();
        var handCountBefore = gameState.zones[handZoneId].cardInstanceIds.Count;

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                    isOptional = true,
                },
            },
        };

        AnomalyRewardExecutor.executeSteps(gameState, actorPlayerId, targetPlayerId: null, anomalyDefinition);

        Assert.Empty(gameState.zones[discardZoneId].cardInstanceIds);
        Assert.Equal(handCountBefore, gameState.zones[handZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void TryValidateRewardContext_WhenNonOptionalMoveCardFromZoneToZoneAndSourceZoneIsEmpty_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createMoveRewardExecutionReadyGameState(actorPlayerId);
        var discardZoneId = gameState.players[actorPlayerId].discardZoneId;
        gameState.zones[discardZoneId].cardInstanceIds.Clear();

        var anomalyDefinition = new AnomalyDefinition
        {
            definitionId = "A005",
            resolveRewardKey = "none",
            rewardSteps =
            {
                new AnomalyRewardStepDefinition
                {
                    rewardStepKey = "moveCardFromZoneToZone",
                    fromZoneKey = "discard",
                    toZoneKey = "hand",
                },
            },
        };

        var isValid = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isValid);
        Assert.Equal("rewardSourceCardMissing", failedReasonKey);
    }

    private static RuleCore.GameState.GameState createRewardExecutionReadyGameState(
        PlayerId actorPlayerId,
        PlayerId targetPlayerId,
        TeamId actorTeamId,
        TeamId targetTeamId)
    {
        var actorCharacterInstanceId = new CharacterInstanceId(9201);
        var targetCharacterInstanceId = new CharacterInstanceId(9202);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = actorCharacterInstanceId,
        };
        gameState.players[targetPlayerId] = new PlayerState
        {
            playerId = targetPlayerId,
            teamId = targetTeamId,
            activeCharacterInstanceId = targetCharacterInstanceId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:anomaly-actor",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[targetCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = targetCharacterInstanceId,
            definitionId = "test:anomaly-target",
            ownerPlayerId = targetPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
        };
        gameState.teams[targetTeamId] = new TeamState
        {
            teamId = targetTeamId,
            killScore = 10,
            leyline = 0,
        };

        return gameState;
    }

    private static RuleCore.GameState.GameState createHealStepReadyGameState(
        PlayerId actorPlayerId,
        PlayerId allyPlayerId,
        PlayerId enemyPlayerId,
        TeamId actorTeamId,
        TeamId enemyTeamId)
    {
        var actorCharacterInstanceId = new CharacterInstanceId(9301);
        var allyCharacterInstanceId = new CharacterInstanceId(9302);
        var enemyCharacterInstanceId = new CharacterInstanceId(9303);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = actorCharacterInstanceId,
        };
        gameState.players[allyPlayerId] = new PlayerState
        {
            playerId = allyPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = allyCharacterInstanceId,
        };
        gameState.players[enemyPlayerId] = new PlayerState
        {
            playerId = enemyPlayerId,
            teamId = enemyTeamId,
            activeCharacterInstanceId = enemyCharacterInstanceId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = actorPlayerId,
            currentHp = 2,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[allyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = allyCharacterInstanceId,
            definitionId = "test:nonHuman",
            ownerPlayerId = allyPlayerId,
            currentHp = 1,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
        gameState.characterInstances[enemyCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = enemyCharacterInstanceId,
            definitionId = "test:nonHuman",
            ownerPlayerId = enemyPlayerId,
            currentHp = 1,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                actorPlayerId,
                allyPlayerId,
            },
        };
        gameState.teams[enemyTeamId] = new TeamState
        {
            teamId = enemyTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                enemyPlayerId,
            },
        };

        return gameState;
    }

    private static RuleCore.GameState.GameState createMoveRewardExecutionReadyGameState(PlayerId actorPlayerId)
    {
        var actorTeamId = new TeamId(1);
        var actorCharacterInstanceId = new CharacterInstanceId(9401);
        var actorDeckZoneId = new ZoneId(9501);
        var actorHandZoneId = new ZoneId(9502);
        var actorDiscardZoneId = new ZoneId(9503);
        var actorFieldZoneId = new ZoneId(9504);
        var actorCharacterSetAsideZoneId = new ZoneId(9505);
        var discardedCardA = new CardInstanceId(9601);
        var discardedCardB = new CardInstanceId(9602);

        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
        };

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            activeCharacterInstanceId = actorCharacterInstanceId,
            deckZoneId = actorDeckZoneId,
            handZoneId = actorHandZoneId,
            discardZoneId = actorDiscardZoneId,
            fieldZoneId = actorFieldZoneId,
            characterSetAsideZoneId = actorCharacterSetAsideZoneId,
        };

        gameState.characterInstances[actorCharacterInstanceId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterInstanceId,
            definitionId = "test:human",
            ownerPlayerId = actorPlayerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.zones[actorDeckZoneId] = new ZoneState
        {
            zoneId = actorDeckZoneId,
            zoneType = ZoneKey.deck,
            ownerPlayerId = actorPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
        gameState.zones[actorHandZoneId] = new ZoneState
        {
            zoneId = actorHandZoneId,
            zoneType = ZoneKey.hand,
            ownerPlayerId = actorPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
        gameState.zones[actorDiscardZoneId] = new ZoneState
        {
            zoneId = actorDiscardZoneId,
            zoneType = ZoneKey.discard,
            ownerPlayerId = actorPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[actorFieldZoneId] = new ZoneState
        {
            zoneId = actorFieldZoneId,
            zoneType = ZoneKey.field,
            ownerPlayerId = actorPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.publicZone,
        };
        gameState.zones[actorCharacterSetAsideZoneId] = new ZoneState
        {
            zoneId = actorCharacterSetAsideZoneId,
            zoneType = ZoneKey.characterSetAside,
            ownerPlayerId = actorPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };

        gameState.cardInstances[discardedCardA] = new CardInstance
        {
            cardInstanceId = discardedCardA,
            definitionId = "test:card-a",
            ownerPlayerId = actorPlayerId,
            zoneId = actorDiscardZoneId,
            zoneKey = ZoneKey.discard,
            isFaceUp = true,
        };
        gameState.cardInstances[discardedCardB] = new CardInstance
        {
            cardInstanceId = discardedCardB,
            definitionId = "test:card-b",
            ownerPlayerId = actorPlayerId,
            zoneId = actorDiscardZoneId,
            zoneKey = ZoneKey.discard,
            isFaceUp = true,
        };
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(discardedCardA);
        gameState.zones[actorDiscardZoneId].cardInstanceIds.Add(discardedCardB);

        gameState.teams[actorTeamId] = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
            memberPlayerIds =
            {
                actorPlayerId,
            },
        };

        return gameState;
    }
}
