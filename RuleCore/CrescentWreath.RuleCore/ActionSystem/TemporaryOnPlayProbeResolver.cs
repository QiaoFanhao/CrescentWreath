namespace CrescentWreath.RuleCore.ActionSystem;

public static class TemporaryOnPlayProbeResolver
{
    public const string ScriptedOnPlayChooseDamageContextKey = "scripted:onPlayChooseDamage";
    public const string ContinuationKeyInputChoiceDamage = "continuation:inputChoiceDamage";
    public const string ScriptedOnPlayChoiceInputTypeKey = "scriptedOnPlayChoice";
    public const string ScriptedOnPlayDeal1ChoiceKey = "deal1";

    public enum ProbeEffectKind
    {
        none = 0,
        immediateDamage1 = 1,
        chooseDamage1 = 2,
        waitResponseDamage1 = 3,
    }

    public static ProbeEffectKind resolveOnPlayProbeEffectKind(string? definitionId)
    {
        return definitionId switch
        {
            "test:onPlayDeal1" => ProbeEffectKind.immediateDamage1,
            "test:onPlayChooseDamage" => ProbeEffectKind.chooseDamage1,
            "test:onPlayWaitResponseDamage" => ProbeEffectKind.waitResponseDamage1,
            _ => ProbeEffectKind.none,
        };
    }
}
