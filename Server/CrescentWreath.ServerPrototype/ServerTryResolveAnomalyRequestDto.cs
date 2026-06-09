namespace CrescentWreath.ServerPrototype;

public sealed class ServerTryResolveAnomalyRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long? targetPlayerNumericId { get; set; }
}
