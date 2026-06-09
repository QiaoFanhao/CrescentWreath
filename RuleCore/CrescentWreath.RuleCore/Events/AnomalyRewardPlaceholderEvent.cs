namespace CrescentWreath.RuleCore.Events;

public sealed class AnomalyRewardPlaceholderEvent : GameEvent
{
    public string anomalyDefinitionId { get; set; } = string.Empty;

    public string anomalyName { get; set; } = string.Empty;

    public string message { get; set; } = string.Empty;
}
