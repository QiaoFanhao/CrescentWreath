namespace CrescentWreath.RuleCore.Events;

public sealed class AnomalyResolveAttemptedEvent : GameEvent
{
    public string anomalyDefinitionId { get; set; } = string.Empty;
    public bool isSucceeded { get; set; }
    public string? failedReasonKey { get; set; }
}
