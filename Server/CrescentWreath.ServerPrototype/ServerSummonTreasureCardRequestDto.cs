namespace CrescentWreath.ServerPrototype;

public sealed class ServerSummonTreasureCardRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long cardInstanceNumericId { get; set; }
}
