namespace CrescentWreath.ServerPrototype;

public sealed class ServerStartNextTurnRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
}
