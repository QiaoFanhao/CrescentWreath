using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyResolveRewardRuntime
{
    public static AnomalyValidationResult evaluateRewardContext(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        var isPassed = AnomalyRewardExecutor.tryValidateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition,
            out var failedReasonKey);

        if (isPassed)
        {
            return AnomalyValidationResult.passed();
        }

        return AnomalyValidationResult.failed(
            AnomalyValidationFailureStage.rewardContext,
            failedReasonKey ?? AnomalyValidationFailureKeys.UnsupportedResolveReward);
    }

    public static bool tryValidateRewardContext(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition,
        out string? failedReasonKey)
    {
        var validationResult = evaluateRewardContext(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
        failedReasonKey = validationResult.failedReasonKey;
        return validationResult.isPassed;
    }

    public static void applyRewardOrThrow(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        AnomalyRewardExecutor.executeSteps(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
    }
}
