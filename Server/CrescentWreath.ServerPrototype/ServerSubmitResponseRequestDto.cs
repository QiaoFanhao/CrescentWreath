namespace CrescentWreath.ServerPrototype;

public sealed class ServerSubmitResponseRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long responseWindowNumericId { get; set; }
    public bool shouldRespond { get; set; }
    public string? responseKey { get; set; }
}
