namespace CrescentWreath.RuleCore.Events;

public sealed class AnomalyFlippedEvent : GameEvent
{
    public string anomalyDefinitionId { get; set; } = string.Empty;
}
