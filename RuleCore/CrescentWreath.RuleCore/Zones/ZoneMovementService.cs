using System;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public sealed class ZoneMovementService
{
    private const string SakuraCakeDefinitionId = "S001";

    public CardMovedEvent moveCard(
        GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneId targetZoneId,
        CardMoveReason moveReason,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var fromZoneId = cardInstance.zoneId;
        var fromZoneState = gameState.zones[fromZoneId];
        var requestedToZoneState = gameState.zones[targetZoneId];

        ZoneMovementRuleGuard.ensureCoreMoveReasonRouteOrThrow(fromZoneState.zoneType, requestedToZoneState.zoneType, moveReason);
        TreasureStaticMovementRuleGuard.ensureTreasureStaticMovementRestrictionsOrThrow(
            cardInstance,
            fromZoneState.zoneType,
            requestedToZoneState.zoneType,
            moveReason);

        targetZoneId = resolveBanishTargetZoneId(gameState, cardInstance, targetZoneId, moveReason);
        var toZoneState = gameState.zones[targetZoneId];

        fromZoneState.cardInstanceIds.Remove(cardInstance.cardInstanceId);
        toZoneState.cardInstanceIds.Add(cardInstance.cardInstanceId);
        cardInstance.zoneId = targetZoneId;
        cardInstance.zoneKey = toZoneState.zoneType;
        if (moveReason == CardMoveReason.summon && toZoneState.ownerPlayerId.HasValue)
        {
            cardInstance.ownerPlayerId = toZoneState.ownerPlayerId.Value;
        }
        if (toZoneState.zoneType == ZoneKey.sakuraCakeDeck)
        {
            cardInstance.ownerPlayerId = new PlayerId(0);
        }
        if (toZoneState.zoneType != ZoneKey.overlayContainer)
        {
            cardInstance.overlayContainerCardInstanceId = null;
            cardInstance.overlayOrderIndex = null;
        }
        if (toZoneState.zoneType != ZoneKey.characterSetAside)
        {
            cardInstance.isSetAside = false;
        }

        return new CardMovedEvent
        {
            eventId = eventId,
            eventTypeKey = "cardMoved",
            sourceActionChainId = sourceActionChainId,
            cardInstanceId = cardInstance.cardInstanceId,
            fromZoneKey = fromZoneState.zoneType,
            toZoneKey = toZoneState.zoneType,
            moveReason = moveReason,
        };
    }

    private static ZoneId resolveBanishTargetZoneId(
        GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneId requestedTargetZoneId,
        CardMoveReason moveReason)
    {
        if (moveReason != CardMoveReason.banish ||
            !string.Equals(cardInstance.definitionId, SakuraCakeDefinitionId, StringComparison.Ordinal) ||
            gameState.publicState is null ||
            !requestedTargetZoneId.Equals(gameState.publicState.gapZoneId))
        {
            return requestedTargetZoneId;
        }

        return gameState.publicState.sakuraCakeDeckZoneId;
    }
}

