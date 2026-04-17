namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyValidationResult
{
    public bool isPassed { get; set; }

    public string? failedReasonKey { get; set; }

    public AnomalyValidationFailureStage? failureStage { get; set; }

    public static AnomalyValidationResult passed()
    {
        return new AnomalyValidationResult
        {
            isPassed = true,
            failedReasonKey = null,
            failureStage = null,
        };
    }

    public static AnomalyValidationResult failed(AnomalyValidationFailureStage failureStage, string failedReasonKey)
    {
        return new AnomalyValidationResult
        {
            isPassed = false,
            failedReasonKey = failedReasonKey,
            failureStage = failureStage,
        };
    }
}
