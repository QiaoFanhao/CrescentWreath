using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(fileName = "SkillPresentationSettings", menuName = "Crescent Wreath/Skill Presentation Settings")]
public sealed class SkillPresentationSettings : ScriptableObject
{
    public float activeDuration = 1.05f;
    public float responseDuration = 0.92f;
    public float automaticDuration = 0.72f;
    [Range(0f, 1f)] public float dimAlpha = 0.68f;
    public float enterDuration = 0.16f;
    public float exitDuration = 0.15f;
    public float cardScale = 1.05f;
    public string activeVfxKey = "skillActiveGold";
    public string responseVfxKey = "skillResponseBlue";
    public string automaticVfxKey = "skillAutomaticCyan";
    public string activeSfxKey = EffectCategoryKeys.CardActivate;
    public string responseSfxKey = EffectCategoryKeys.Defense;
    public string automaticSfxKey = EffectCategoryKeys.GainResource;

    public static SkillPresentationSettings CreateRuntimeDefault()
    {
        return CreateInstance<SkillPresentationSettings>();
    }

    public float ResolveDuration(string triggerTypeKey)
    {
        return triggerTypeKey switch
        {
            "response" => Mathf.Max(0.1f, responseDuration),
            "automatic" or "arrival" or "passiveTriggered" => Mathf.Max(0.1f, automaticDuration),
            _ => Mathf.Max(0.1f, activeDuration),
        };
    }

    public string ResolveSfxKey(string triggerTypeKey)
    {
        return triggerTypeKey switch
        {
            "response" => responseSfxKey,
            "automatic" or "arrival" or "passiveTriggered" => automaticSfxKey,
            _ => activeSfxKey,
        };
    }

    public string ResolveVfxKey(string triggerTypeKey)
    {
        return triggerTypeKey switch
        {
            "response" => responseVfxKey,
            "automatic" or "arrival" or "passiveTriggered" => automaticVfxKey,
            _ => activeVfxKey,
        };
    }
}
}
