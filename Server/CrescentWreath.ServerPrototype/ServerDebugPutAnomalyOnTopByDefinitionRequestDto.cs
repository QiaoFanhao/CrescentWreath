namespace CrescentWreath.ServerPrototype;

public sealed class ServerDebugPutAnomalyOnTopByDefinitionRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public string anomalyDefinitionId { get; set; } = string.Empty;
}
