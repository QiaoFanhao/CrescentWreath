using System.Collections.Generic;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerSubmitInputChoiceRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long inputContextNumericId { get; set; }
    public string? choiceKey { get; set; }
    public List<string> choiceKeys { get; set; } = new();
}
