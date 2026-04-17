namespace CrescentWreath.ServerPrototype;

public sealed class ServerSubmitDefenseRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public string defenseTypeKey { get; set; } = string.Empty;
    public long defenseCardInstanceNumericId { get; set; }
}
