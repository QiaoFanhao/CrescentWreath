namespace CrescentWreath.ServerPrototype;

public sealed class ServerSubmitCharacterSelectionRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public string characterDefinitionId { get; set; } = string.Empty;
}
