namespace CrescentWreath.ServerPrototype;

public sealed class ServerDebugMoveTreasureToHandByDefinitionRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public string treasureDefinitionId { get; set; } = string.Empty;
}
