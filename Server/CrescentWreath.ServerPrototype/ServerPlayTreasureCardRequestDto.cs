namespace CrescentWreath.ServerPrototype;

public sealed class ServerPlayTreasureCardRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long cardInstanceNumericId { get; set; }
    public string playMode { get; set; } = "normal";
}
