using System;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(fileName = "EffectPresentationSettings", menuName = "Crescent Wreath/Effect Presentation Settings")]
public sealed class EffectPresentationSettings : ScriptableObject
{
    public float defaultEffectDelaySeconds = 1.0f;
    public float inputContextDelayAfterEffectSeconds = 0.8f;
    public float responseWindowDelayAfterEffectSeconds = 0.8f;
    public float anomalyArrivalDelaySeconds = 1.2f;
    public float noEffectDelaySeconds = 0.0f;
    public float cardActivateDelaySeconds = 0.45f;
    public float smallEffectDelaySeconds = 0.45f;

    public static EffectPresentationSettings CreateRuntimeDefault()
    {
        var settings = CreateInstance<EffectPresentationSettings>();
        settings.name = "RuntimeDefaultEffectPresentationSettings";
        return settings;
    }

    public float ResolveDelaySeconds(string categoryKey, float catalogDelaySeconds)
    {
        var categoryDelaySeconds = resolveCategoryDelaySeconds(categoryKey);
        return Mathf.Max(0f, categoryDelaySeconds, catalogDelaySeconds);
    }

    private float resolveCategoryDelaySeconds(string categoryKey)
    {
        if (string.Equals(categoryKey, EffectCategoryKeys.Anomaly, StringComparison.Ordinal))
        {
            return anomalyArrivalDelaySeconds;
        }

        if (string.Equals(categoryKey, EffectCategoryKeys.Damage, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.DirectDamage, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.Defense, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.Banish, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.Seal, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.GenericEffect, StringComparison.Ordinal))
        {
            return defaultEffectDelaySeconds;
        }

        if (string.Equals(categoryKey, EffectCategoryKeys.CardActivate, StringComparison.Ordinal))
        {
            return cardActivateDelaySeconds;
        }

        if (string.Equals(categoryKey, EffectCategoryKeys.Heal, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.Draw, StringComparison.Ordinal) ||
            string.Equals(categoryKey, EffectCategoryKeys.GainResource, StringComparison.Ordinal))
        {
            return smallEffectDelaySeconds;
        }

        return noEffectDelaySeconds;
    }
}
}
