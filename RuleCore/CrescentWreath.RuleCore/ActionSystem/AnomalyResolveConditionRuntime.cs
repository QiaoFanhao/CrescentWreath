using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyResolveConditionRuntime
{
    public static AnomalyValidationResult evaluateCondition(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        return AnomalyConditionExecutor.evaluate(
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
        var validationResult = evaluateCondition(
            gameState,
            actorPlayerId,
            targetPlayerId,
            anomalyDefinition);
        failedReasonKey = validationResult.failedReasonKey;
        return validationResult.isPassed;
    }
}
