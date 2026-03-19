using System.Collections.Generic;
using CrescentWreath.RuleCore.Events;

namespace CrescentWreath.RuleCore.DamageSystem;

public sealed class DamageProcessor
{
    public List<GameEvent> resolveDamage(GameState.GameState gameState, DamageContext damageContext)
    {
        var targetCharacterInstanceId = damageContext.targetCharacterInstanceId!.Value;
        var targetCharacter = gameState.characterInstances[targetCharacterInstanceId];
        var targetPlayer = gameState.players[targetCharacter.ownerPlayerId];

        var hpBefore = targetPlayer.hp;
        var finalDamageValue = damageContext.baseDamageValue;
        targetPlayer.hp -= finalDamageValue;
        var hpAfter = targetPlayer.hp;

        damageContext.finalDamageValue = finalDamageValue;
        damageContext.didDealDamage = finalDamageValue > 0;

        var damageResolvedEvent = new DamageResolvedEvent
        {
            eventId = damageContext.damageContextId.Value,
            eventTypeKey = "damageResolved",
            damageContextId = damageContext.damageContextId,
            finalDamageValue = damageContext.finalDamageValue,
            didDealDamage = damageContext.didDealDamage,
        };

        var hpChangedEvent = new HpChangedEvent
        {
            eventId = damageContext.damageContextId.Value,
            eventTypeKey = "hpChanged",
            targetPlayerId = targetCharacter.ownerPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        };

        return new List<GameEvent> { damageResolvedEvent, hpChangedEvent };
    }
}
