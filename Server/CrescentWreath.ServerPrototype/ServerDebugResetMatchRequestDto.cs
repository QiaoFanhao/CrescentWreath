namespace CrescentWreath.ServerPrototype;

public sealed class ServerDebugResetMatchRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public int? publicDeckShuffleSeed { get; set; }
}
