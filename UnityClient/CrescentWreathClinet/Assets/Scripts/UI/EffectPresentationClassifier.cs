using System;

namespace CrescentWreath.Client.UI
{
public static class EffectPresentationClassifier
{
    public static string Classify(string eventTypeKey, string eventLine)
    {
        var text = $"{eventTypeKey} {eventLine}";
        if (containsAny(text, "directDamage", "????"))
        {
            return EffectCategoryKeys.DirectDamage;
        }

        if (containsAny(text, "damage", "Damage", "??", "??", "??"))
        {
            return EffectCategoryKeys.Damage;
        }

        if (containsAny(text, "heal", "recover", "??", "??"))
        {
            return EffectCategoryKeys.Heal;
        }

        if (containsAny(text, "defense", "Defense", "barrier", "??", "??"))
        {
            return EffectCategoryKeys.Defense;
        }

        if (containsAny(text, "draw", "Draw", "?", "?"))
        {
            return EffectCategoryKeys.Draw;
        }

        if (containsAny(text, "resource", "leyline", "mana", "sigil", "??", "??", "??"))
        {
            return EffectCategoryKeys.GainResource;
        }

        if (containsAny(text, "seal", "shackle", "silence", "charm", "??", "??", "??", "??"))
        {
            return EffectCategoryKeys.Seal;
        }

        if (containsAny(text, "banish", "gap", "Banish", "??", "??"))
        {
            return EffectCategoryKeys.Banish;
        }

        if (containsAny(text, "anomaly", "Anomaly", "??", "??"))
        {
            return EffectCategoryKeys.Anomaly;
        }

        if (containsAny(text, "turn", "phase", "??", "??"))
        {
            return EffectCategoryKeys.TurnChange;
        }

        if (containsAny(text, "play", "summon", "skill", "cardMoved", "??", "??", "??"))
        {
            return EffectCategoryKeys.CardActivate;
        }

        return EffectCategoryKeys.GenericEffect;
    }

    private static bool containsAny(string text, params string[] values)
    {
        foreach (var value in values)
        {
            if (text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
}
