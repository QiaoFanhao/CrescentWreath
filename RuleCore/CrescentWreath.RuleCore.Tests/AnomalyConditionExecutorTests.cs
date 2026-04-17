using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Tests;

public class AnomalyConditionExecutorTests
{
    [Fact]
    public void TryEvaluate_WhenActorManaAtLeastStepIsSatisfied_ShouldPass()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2));
        gameState.players[actorPlayerId].mana = 8;

        var anomalyDefinition = new AnomalyDefinition
        {
            resolveManaCost = 8,
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
            },
        };

        var isPassed = AnomalyConditionExecutor.tryEvaluate(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.True(isPassed);
        Assert.Null(failedReasonKey);
    }

    [Fact]
    public void TryEvaluate_WhenCompositeConditionAndFriendlyHpInsufficient_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2), allyPlayerId);
        gameState.players[actorPlayerId].mana = 8;

        var allyCharacterId = gameState.players[allyPlayerId].activeCharacterInstanceId!.Value;
        gameState.characterInstances[allyCharacterId].currentHp = 1;

        var anomalyDefinition = new AnomalyDefinition
        {
            resolveManaCost = 8,
            resolveFriendlyTeamHpCostPerPlayer = 1,
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "actorManaAtLeast",
                },
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "friendlyTeamActiveCharacterHpAboveCostPerPlayer",
                },
            },
        };

        var isPassed = AnomalyConditionExecutor.tryEvaluate(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isPassed);
        Assert.Equal("insufficientFriendlyHp", failedReasonKey);
    }

    [Fact]
    public void TryEvaluate_WhenTargetPlayerIsOpponentStepAndTargetIsFriendly_ShouldFail()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2), allyPlayerId);

        var anomalyDefinition = new AnomalyDefinition
        {
            conditionSteps =
            {
                new AnomalyConditionStepDefinition
                {
                    conditionStepKey = "targetPlayerIsOpponent",
                },
            },
        };

        var isPassed = AnomalyConditionExecutor.tryEvaluate(
            gameState,
            actorPlayerId,
            allyPlayerId,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isPassed);
        Assert.Equal("targetPlayerMustBeOpponent", failedReasonKey);
    }

    [Fact]
    public void TryEvaluate_WhenConditionStepsIsEmpty_ShouldFallbackToLegacyResolveConditionKey()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2));
        gameState.players[actorPlayerId].mana = 7;

        var anomalyDefinition = new AnomalyDefinition
        {
            resolveConditionKey = "actorManaAtLeastCost",
            resolveManaCost = 8,
        };

        var isPassed = AnomalyConditionExecutor.tryEvaluate(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isPassed);
        Assert.Equal("insufficientMana", failedReasonKey);
    }

    [Fact]
    public void TryEvaluate_WhenLegacyResolveConditionIsUnsupported_ShouldFailWithLegacyReason()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2));

        var anomalyDefinition = new AnomalyDefinition
        {
            resolveConditionKey = "unsupportedLegacyConditionKey",
        };

        var isPassed = AnomalyConditionExecutor.tryEvaluate(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition,
            out var failedReasonKey);

        Assert.False(isPassed);
        Assert.Equal("unsupportedResolveCondition", failedReasonKey);
    }

    [Fact]
    public void EvaluateCondition_WhenConditionFails_ShouldReturnConditionFailureStage()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = createConditionReadyGameState(actorPlayerId, new PlayerId(2), new TeamId(1), new TeamId(2));
        gameState.players[actorPlayerId].mana = 7;

        var anomalyDefinition = new AnomalyDefinition
        {
            resolveConditionKey = "actorManaAtLeastCost",
            resolveManaCost = 8,
        };

        var validationResult = AnomalyResolveConditionRuntime.evaluateCondition(
            gameState,
            actorPlayerId,
            targetPlayerId: null,
            anomalyDefinition);

        Assert.False(validationResult.isPassed);
        Assert.Equal("insufficientMana", validationResult.failedReasonKey);
        Assert.Equal(AnomalyValidationFailureStage.condition, validationResult.failureStage);
    }

    private static RuleCore.GameState.GameState createConditionReadyGameState(
        PlayerId actorPlayerId,
        PlayerId opponentPlayerId,
        TeamId actorTeamId,
        TeamId opponentTeamId,
        PlayerId? allyPlayerId = null)
    {
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
        };

        var actorCharacterId = new CharacterInstanceId(9301);
        var opponentCharacterId = new CharacterInstanceId(9302);

        gameState.players[actorPlayerId] = new PlayerState
        {
            playerId = actorPlayerId,
            teamId = actorTeamId,
            mana = 0,
            activeCharacterInstanceId = actorCharacterId,
        };

        gameState.players[opponentPlayerId] = new PlayerState
        {
            playerId = opponentPlayerId,
            teamId = opponentTeamId,
            mana = 0,
            activeCharacterInstanceId = opponentCharacterId,
        };

        gameState.characterInstances[actorCharacterId] = new CharacterInstance
        {
            characterInstanceId = actorCharacterId,
            definitionId = "test:condition-actor",
            ownerPlayerId = actorPlayerId,
            currentHp = 3,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.characterInstances[opponentCharacterId] = new CharacterInstance
        {
            characterInstanceId = opponentCharacterId,
            definitionId = "test:condition-opponent",
            ownerPlayerId = opponentPlayerId,
            currentHp = 3,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        var actorTeamState = new TeamState
        {
            teamId = actorTeamId,
            killScore = 10,
            leyline = 0,
        };
        actorTeamState.memberPlayerIds.Add(actorPlayerId);

        if (allyPlayerId.HasValue)
        {
            var allyPlayerIdValue = allyPlayerId.Value;
            var allyCharacterId = new CharacterInstanceId(9303);
            gameState.players[allyPlayerIdValue] = new PlayerState
            {
                playerId = allyPlayerIdValue,
                teamId = actorTeamId,
                mana = 0,
                activeCharacterInstanceId = allyCharacterId,
            };

            gameState.characterInstances[allyCharacterId] = new CharacterInstance
            {
                characterInstanceId = allyCharacterId,
                definitionId = "test:condition-ally",
                ownerPlayerId = allyPlayerIdValue,
                currentHp = 3,
                maxHp = 4,
                isAlive = true,
                isInPlay = true,
            };

            actorTeamState.memberPlayerIds.Add(allyPlayerIdValue);
        }

        gameState.teams[actorTeamId] = actorTeamState;

        var opponentTeamState = new TeamState
        {
            teamId = opponentTeamId,
            killScore = 10,
            leyline = 0,
        };
        opponentTeamState.memberPlayerIds.Add(opponentPlayerId);
        gameState.teams[opponentTeamId] = opponentTeamState;

        return gameState;
    }
}
