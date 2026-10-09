using System;

namespace CrescentWreath.Client.UI
{
public static class EffectCategoryKeys
{
    public const string CardActivate = "cardActivate";
    public const string Damage = "damage";
    public const string DirectDamage = "directDamage";
    public const string Heal = "heal";
    public const string Defense = "defense";
    public const string Draw = "draw";
    public const string GainResource = "gainResource";
    public const string Seal = "seal";
    public const string Banish = "banish";
    public const string Anomaly = "anomaly";
    public const string TurnChange = "turnChange";
    public const string GenericEffect = "genericEffect";

    public static bool IsKnown(string categoryKey)
    {
        return string.Equals(categoryKey, CardActivate, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Damage, StringComparison.Ordinal) ||
            string.Equals(categoryKey, DirectDamage, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Heal, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Defense, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Draw, StringComparison.Ordinal) ||
            string.Equals(categoryKey, GainResource, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Seal, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Banish, StringComparison.Ordinal) ||
            string.Equals(categoryKey, Anomaly, StringComparison.Ordinal) ||
            string.Equals(categoryKey, TurnChange, StringComparison.Ordinal) ||
            string.Equals(categoryKey, GenericEffect, StringComparison.Ordinal);
    }
}
}
