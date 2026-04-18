using System.Collections.Generic;
using System.Text.Json;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerSocketRequestEnvelope
{
    public long requestId { get; set; }
    public long viewerPlayerNumericId { get; set; }
    public string actionType { get; set; } = string.Empty;
    public JsonElement payload { get; set; }
}

public sealed class ServerSocketResponseEnvelope
{
    public long requestId { get; set; }
    public long viewerPlayerNumericId { get; set; }
    public bool isSucceeded { get; set; }
    public ServerErrorProjection? error { get; set; }
    public ServerStateProjection? stateProjection { get; set; }
    public List<ServerEventLogEntry> eventLog { get; set; } = new();
    public ServerInteractionProjection? interaction { get; set; }
}
