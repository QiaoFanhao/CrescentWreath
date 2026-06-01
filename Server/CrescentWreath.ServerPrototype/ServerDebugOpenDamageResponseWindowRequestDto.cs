namespace CrescentWreath.ServerPrototype;

public sealed class ServerDebugOpenDamageResponseWindowRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long targetCharacterInstanceNumericId { get; set; }
    public int baseDamageValue { get; set; }
    public string? damageTypeKey { get; set; }
}
