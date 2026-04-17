namespace CrescentWreath.RuleCore.Events;

public sealed class AnomalyResolvedEvent : GameEvent
{
    public string anomalyDefinitionId { get; set; } = string.Empty;
}
