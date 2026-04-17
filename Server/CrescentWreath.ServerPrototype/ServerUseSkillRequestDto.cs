namespace CrescentWreath.ServerPrototype;

public sealed class ServerUseSkillRequestDto
{
    public long requestId { get; set; }
    public long actorPlayerNumericId { get; set; }
    public long characterInstanceNumericId { get; set; }
    public string skillKey { get; set; } = string.Empty;
}
