using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ResponseSystem;

public sealed class ResponseWindowState
{
    public ResponseWindowId responseWindowId { get; set; }
    public ResponseWindowOriginType originType { get; set; }
    public string? windowTypeKey { get; set; }
    public ActionChainId? sourceActionChainId { get; set; }
    public List<PlayerId> responderPlayerIds { get; } = new();
    public List<string> usedResponseKeys { get; } = new();
    public PlayerId? currentResponderPlayerId { get; set; }
    public CharacterInstanceId? pendingDamageTargetCharacterInstanceId { get; set; }
    public int? pendingDamageBaseDamageValue { get; set; }
    public PlayerId? pendingDamageSourcePlayerId { get; set; }
    public CardInstanceId? pendingDamageSourceCardInstanceId { get; set; }
    public CharacterInstanceId? pendingDamageSourceCharacterInstanceId { get; set; }
    public string? pendingDamageTypeKey { get; set; }
    public string? pendingDamageResponseStageKey { get; set; }
    public string? pendingDamageDefenseDeclarationKey { get; set; }
    public PlayerId? pendingDamageDefenderPlayerId { get; set; }
    public CharacterInstanceId? pendingKillTargetCharacterInstanceId { get; set; }
    public PlayerId? pendingKillKillerPlayerId { get; set; }
    public DamageContextId? pendingKillSourceDamageContextId { get; set; }
}
