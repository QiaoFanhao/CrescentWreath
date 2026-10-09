using System;
using System.Collections.Generic;

namespace CrescentWreath.RuleCore.StatusSystem;

public static class StatusPolicyTable
{
    public enum StatusIdentityScope
    {
        character = 0,
        player = 1,
        card = 2,
        generic = 3,
    }

    public enum StatusStackPolicy
    {
        stack = 0,
        ignoreIfExists = 1,
        refresh = 2,
    }

    public sealed class StatusPolicy
    {
        public string canonicalStatusKey { get; set; } = string.Empty;
        public StatusIdentityScope identityScope { get; set; } = StatusIdentityScope.generic;
        public StatusStackPolicy stackPolicy { get; set; } = StatusStackPolicy.stack;
    }

    private static readonly Dictionary<string, string> CanonicalKeyByAlias =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Barrier"] = "Barrier",
            ["status:barrier"] = "Barrier",
            ["Seal"] = "Seal",
            ["status:seal"] = "Seal",
            ["Shackle"] = "Shackle",
            ["status:shackle"] = "Shackle",
            ["Silence"] = "Silence",
            ["status:silence"] = "Silence",
            ["Charm"] = "Charm",
            ["status:charm"] = "Charm",
            ["Penetrate"] = "Penetrate",
            ["status:penetrate"] = "Penetrate",
            ["PhysicalDamageBoostNext"] = "PhysicalDamageBoostNext",
            ["status:physicalDamageBoostNext"] = "PhysicalDamageBoostNext",
            ["SpellDamageBoostNext"] = "SpellDamageBoostNext",
            ["status:spellDamageBoostNext"] = "SpellDamageBoostNext",
            ["PhysicalOrSpellDamageBoostNext"] = "PhysicalOrSpellDamageBoostNext",
            ["status:physicalOrSpellDamageBoostNext"] = "PhysicalOrSpellDamageBoostNext",
            ["C011AllDamageBoostThisTurn"] = "C011AllDamageBoostThisTurn",
            ["status:c011AllDamageBoostThisTurn"] = "C011AllDamageBoostThisTurn",
            ["C011EndTurnSelfShackle"] = "C011EndTurnSelfShackle",
            ["status:c011EndTurnSelfShackle"] = "C011EndTurnSelfShackle",
            ["C021BlackKeyProjectionFreeMana"] = "C021BlackKeyProjectionFreeMana",
            ["status:c021BlackKeyProjectionFreeMana"] = "C021BlackKeyProjectionFreeMana",
            ["C014SkipSummonPhase"] = "C014SkipSummonPhase",
            ["status:c014SkipSummonPhase"] = "C014SkipSummonPhase",
        };

    private static readonly Dictionary<string, StatusPolicy> PolicyByCanonicalKey =
        new(StringComparer.Ordinal)
        {
            ["Barrier"] = new StatusPolicy
            {
                canonicalStatusKey = "Barrier",
                identityScope = StatusIdentityScope.character,
                stackPolicy = StatusStackPolicy.ignoreIfExists,
            },
            ["Seal"] = new StatusPolicy
            {
                canonicalStatusKey = "Seal",
                identityScope = StatusIdentityScope.character,
                stackPolicy = StatusStackPolicy.ignoreIfExists,
            },
            ["Shackle"] = new StatusPolicy
            {
                canonicalStatusKey = "Shackle",
                identityScope = StatusIdentityScope.character,
                stackPolicy = StatusStackPolicy.ignoreIfExists,
            },
            ["Silence"] = new StatusPolicy
            {
                canonicalStatusKey = "Silence",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.refresh,
            },
            ["Charm"] = new StatusPolicy
            {
                canonicalStatusKey = "Charm",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.refresh,
            },
            ["Penetrate"] = new StatusPolicy
            {
                canonicalStatusKey = "Penetrate",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.refresh,
            },
            ["PhysicalDamageBoostNext"] = new StatusPolicy
            {
                canonicalStatusKey = "PhysicalDamageBoostNext",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.stack,
            },
            ["SpellDamageBoostNext"] = new StatusPolicy
            {
                canonicalStatusKey = "SpellDamageBoostNext",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.stack,
            },
            ["PhysicalOrSpellDamageBoostNext"] = new StatusPolicy
            {
                canonicalStatusKey = "PhysicalOrSpellDamageBoostNext",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.stack,
            },
            ["C011AllDamageBoostThisTurn"] = new StatusPolicy
            {
                canonicalStatusKey = "C011AllDamageBoostThisTurn",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.stack,
            },
            ["C011EndTurnSelfShackle"] = new StatusPolicy
            {
                canonicalStatusKey = "C011EndTurnSelfShackle",
                identityScope = StatusIdentityScope.character,
                stackPolicy = StatusStackPolicy.refresh,
            },
            ["C021BlackKeyProjectionFreeMana"] = new StatusPolicy
            {
                canonicalStatusKey = "C021BlackKeyProjectionFreeMana",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.refresh,
            },
            ["C014SkipSummonPhase"] = new StatusPolicy
            {
                canonicalStatusKey = "C014SkipSummonPhase",
                identityScope = StatusIdentityScope.player,
                stackPolicy = StatusStackPolicy.refresh,
            },
        };

    public static string normalizeStatusKey(string statusKey)
    {
        if (CanonicalKeyByAlias.TryGetValue(statusKey, out var canonicalStatusKey))
        {
            return canonicalStatusKey;
        }

        return statusKey;
    }

    public static StatusPolicy resolvePolicy(string normalizedStatusKey, StatusInstance statusInstance)
    {
        if (PolicyByCanonicalKey.TryGetValue(normalizedStatusKey, out var statusPolicy))
        {
            return statusPolicy;
        }

        return new StatusPolicy
        {
            canonicalStatusKey = normalizedStatusKey,
            identityScope = inferDefaultIdentityScope(statusInstance),
            stackPolicy = StatusStackPolicy.stack,
        };
    }

    private static StatusIdentityScope inferDefaultIdentityScope(StatusInstance statusInstance)
    {
        if (statusInstance.targetCharacterInstanceId.HasValue)
        {
            return StatusIdentityScope.character;
        }

        if (statusInstance.targetPlayerId.HasValue)
        {
            return StatusIdentityScope.player;
        }

        if (statusInstance.targetCardInstanceId.HasValue)
        {
            return StatusIdentityScope.card;
        }

        return StatusIdentityScope.generic;
    }
}
