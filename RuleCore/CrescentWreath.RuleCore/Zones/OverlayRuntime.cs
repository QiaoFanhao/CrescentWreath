using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public static class OverlayRuntime
{
    private const long OverlayContainerZoneIdBase = 9_000_000_000_000L;

    public static ZoneId resolveOverlayContainerZoneId(CardInstanceId containerCardInstanceId)
    {
        if (containerCardInstanceId.Value <= 0)
        {
            throw new InvalidOperationException("OverlayRuntime requires containerCardInstanceId to be positive.");
        }

        return new ZoneId(OverlayContainerZoneIdBase + containerCardInstanceId.Value);
    }

    public static ZoneState getOrCreateOverlayContainerZone(
        GameState.GameState gameState,
        CardInstance containerCardInstance)
    {
        var overlayContainerZoneId = resolveOverlayContainerZoneId(containerCardInstance.cardInstanceId);
        if (gameState.zones.TryGetValue(overlayContainerZoneId, out var existingZoneState))
        {
            return existingZoneState;
        }

        var overlayContainerZoneState = new ZoneState
        {
            zoneId = overlayContainerZoneId,
            zoneType = ZoneKey.overlayContainer,
            ownerPlayerId = containerCardInstance.ownerPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
        gameState.zones.Add(overlayContainerZoneId, overlayContainerZoneState);
        return overlayContainerZoneState;
    }

    public static CardMovedEvent overlayCardUnderContainer(
        GameState.GameState gameState,
        ZoneMovementService zoneMovementService,
        CardInstance overlayCardInstance,
        CardInstance containerCardInstance,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var overlayContainerZoneState = getOrCreateOverlayContainerZone(gameState, containerCardInstance);
        var overlayOrderIndex = overlayContainerZoneState.cardInstanceIds.Count;
        var movedEvent = zoneMovementService.moveCard(
            gameState,
            overlayCardInstance,
            overlayContainerZoneState.zoneId,
            CardMoveReason.overlay,
            sourceActionChainId,
            eventId);

        overlayCardInstance.overlayContainerCardInstanceId = containerCardInstance.cardInstanceId;
        overlayCardInstance.overlayOrderIndex = overlayOrderIndex;
        overlayCardInstance.isFaceUp = false;
        overlayCardInstance.isDefensePlacedOnField = false;
        return movedEvent;
    }

    public static int getOverlayCardCount(
        GameState.GameState gameState,
        CardInstanceId containerCardInstanceId)
    {
        var overlayContainerZoneId = resolveOverlayContainerZoneId(containerCardInstanceId);
        return gameState.zones.TryGetValue(overlayContainerZoneId, out var overlayContainerZoneState)
            ? overlayContainerZoneState.cardInstanceIds.Count
            : 0;
    }

    public static IReadOnlyList<CardInstanceId> getOverlayCardInstanceIds(
        GameState.GameState gameState,
        CardInstanceId containerCardInstanceId)
    {
        var overlayContainerZoneId = resolveOverlayContainerZoneId(containerCardInstanceId);
        return gameState.zones.TryGetValue(overlayContainerZoneId, out var overlayContainerZoneState)
            ? overlayContainerZoneState.cardInstanceIds
            : Array.Empty<CardInstanceId>();
    }
}
