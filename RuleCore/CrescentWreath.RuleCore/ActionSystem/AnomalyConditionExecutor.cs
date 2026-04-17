using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyConditionExecutor
{
    public static AnomalyValidationResult evaluate(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (anomalyDefinition.conditionSteps.Count > 0)
        {
            return evaluateConditionSteps(
                gameState,
                actorPlayerId,
                targetPlayerId,
                anomalyDefinition);
        }

        return evaluateLegacyResolveConditionKey(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
    }

    public static bool tryEvaluate(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition,
        out string? failedReasonKey)
    {
        var validationResult = evaluate(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
        failedReasonKey = validationResult.failedReasonKey;
        return validationResult.isPassed;
    }

    private static AnomalyValidationResult evaluateConditionSteps(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        foreach (var conditionStep in anomalyDefinition.conditionSteps)
        {
            var conditionStepKey = conditionStep.conditionStepKey;
            if (conditionStepKey == "actorManaAtLeast")
            {
                var validationResult = evaluateActorManaAtLeast(gameState, actorPlayerId, anomalyDefinition);
                if (!validationResult.isPassed)
                {
                    return validationResult;
                }

                continue;
            }

            if (conditionStepKey == "friendlyTeamActiveCharacterHpAboveCostPerPlayer")
            {
                var validationResult = evaluateFriendlyTeamActiveCharacterHpAboveCostPerPlayer(
                    gameState,
                    actorPlayerId,
                    anomalyDefinition);
                if (!validationResult.isPassed)
                {
                    return validationResult;
                }

                continue;
            }

            if (conditionStepKey == "targetPlayerIsOpponent")
            {
                var validationResult = evaluateTargetPlayerIsOpponent(gameState, actorPlayerId, targetPlayerId);
                if (!validationResult.isPassed)
                {
                    return validationResult;
                }

                continue;
            }

            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.UnsupportedResolveConditionStep);
        }

        return AnomalyValidationResult.passed();
    }

    private static AnomalyValidationResult evaluateLegacyResolveConditionKey(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (anomalyDefinition.resolveConditionKey == "legacyAutoSuccess" ||
            anomalyDefinition.resolveConditionKey == "alwaysTrue")
        {
            return AnomalyValidationResult.passed();
        }

        if (anomalyDefinition.resolveConditionKey == "actorManaAtLeastCost")
        {
            return evaluateActorManaAtLeast(
                gameState,
                actorPlayerId,
                anomalyDefinition);
        }

        if (anomalyDefinition.resolveConditionKey == "actorManaAndFriendlyTeamHpAtLeastCost")
        {
            var manaValidationResult = evaluateActorManaAtLeast(gameState, actorPlayerId, anomalyDefinition);
            if (!manaValidationResult.isPassed)
            {
                return manaValidationResult;
            }

            return evaluateFriendlyTeamActiveCharacterHpAboveCostPerPlayer(
                gameState,
                actorPlayerId,
                anomalyDefinition);
        }

        if (anomalyDefinition.resolveConditionKey == "targetPlayerIsOpponent")
        {
            return evaluateTargetPlayerIsOpponent(gameState, actorPlayerId, targetPlayerId);
        }

        return AnomalyValidationResult.failed(
            AnomalyValidationFailureStage.condition,
            AnomalyValidationFailureKeys.UnsupportedResolveCondition);
    }

    private static AnomalyValidationResult evaluateActorManaAtLeast(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.ActorPlayerStateMissing);
        }

        if (!anomalyDefinition.resolveManaCost.HasValue)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.ResolveManaCostMissing);
        }

        if (actorPlayerState.mana < anomalyDefinition.resolveManaCost.Value)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.InsufficientMana);
        }

        return AnomalyValidationResult.passed();
    }

    private static AnomalyValidationResult evaluateFriendlyTeamActiveCharacterHpAboveCostPerPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (!anomalyDefinition.resolveFriendlyTeamHpCostPerPlayer.HasValue)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.ResolveFriendlyTeamHpCostPerPlayerMissing);
        }

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.ActorPlayerStateMissing);
        }

        var friendlyTeamPlayerIds = resolveFriendlyTeamPlayerIds(gameState, actorPlayerState.teamId);
        if (friendlyTeamPlayerIds.Count == 0)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.FriendlyTeamPlayerMissing);
        }

        var hpCostPerPlayer = anomalyDefinition.resolveFriendlyTeamHpCostPerPlayer.Value;
        foreach (var friendlyPlayerId in friendlyTeamPlayerIds)
        {
            if (!gameState.players.TryGetValue(friendlyPlayerId, out var friendlyPlayerState))
            {
                return AnomalyValidationResult.failed(
                    AnomalyValidationFailureStage.condition,
                    AnomalyValidationFailureKeys.FriendlyPlayerStateMissing);
            }

            if (!friendlyPlayerState.activeCharacterInstanceId.HasValue)
            {
                return AnomalyValidationResult.failed(
                    AnomalyValidationFailureStage.condition,
                    AnomalyValidationFailureKeys.ActiveCharacterMissing);
            }

            var activeCharacterInstanceId = friendlyPlayerState.activeCharacterInstanceId.Value;
            if (!gameState.characterInstances.TryGetValue(activeCharacterInstanceId, out var activeCharacterInstance))
            {
                return AnomalyValidationResult.failed(
                    AnomalyValidationFailureStage.condition,
                    AnomalyValidationFailureKeys.ActiveCharacterMissing);
            }

            if (activeCharacterInstance.currentHp <= hpCostPerPlayer)
            {
                return AnomalyValidationResult.failed(
                    AnomalyValidationFailureStage.condition,
                    AnomalyValidationFailureKeys.InsufficientFriendlyHp);
            }
        }

        return AnomalyValidationResult.passed();
    }

    private static AnomalyValidationResult evaluateTargetPlayerIsOpponent(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId)
    {
        if (!targetPlayerId.HasValue)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.TargetPlayerRequired);
        }

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.ActorPlayerStateMissing);
        }

        var targetPlayerIdValue = targetPlayerId.Value;
        if (!gameState.players.TryGetValue(targetPlayerIdValue, out var targetPlayerState))
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.TargetPlayerMissing);
        }

        if (targetPlayerState.teamId == actorPlayerState.teamId)
        {
            return AnomalyValidationResult.failed(
                AnomalyValidationFailureStage.condition,
                AnomalyValidationFailureKeys.TargetPlayerMustBeOpponent);
        }

        return AnomalyValidationResult.passed();
    }

    private static List<PlayerId> resolveFriendlyTeamPlayerIds(
        RuleCore.GameState.GameState gameState,
        TeamId actorTeamId)
    {
        if (gameState.teams.TryGetValue(actorTeamId, out var actorTeamState) &&
            actorTeamState.memberPlayerIds.Count > 0)
        {
            return new List<PlayerId>(actorTeamState.memberPlayerIds);
        }

        var friendlyTeamPlayerIds = new List<PlayerId>();
        foreach (var playerStateEntry in gameState.players)
        {
            if (playerStateEntry.Value.teamId == actorTeamId)
            {
                friendlyTeamPlayerIds.Add(playerStateEntry.Key);
            }
        }

        return friendlyTeamPlayerIds;
    }
}
