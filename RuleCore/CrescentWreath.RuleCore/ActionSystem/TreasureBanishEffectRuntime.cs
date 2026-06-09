using System;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TreasureBanishEffectRuntime
{
    public const string DefinitionIdT024 = "T024";

    private readonly ZoneMovementService zoneMovementService;

    public TreasureBanishEffectRuntime(ZoneMovementService zoneMovementService)
    {
        this.zoneMovementService = zoneMovementService;
    }

    public void applyBanishEffectsFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (gameState.matchState != MatchState.running ||
            gameState.turnState is null ||
            producedEventsStartIndex < 0 ||
            producedEventsStartIndex >= actionChainState.producedEvents.Count)
        {
            return;
        }

        for (var eventIndex = producedEventsStartIndex; eventIndex < actionChainState.producedEvents.Count; eventIndex++)
        {
            if (actionChainState.producedEvents[eventIndex] is not CardMovedEvent cardMovedEvent ||
                cardMovedEvent.moveReason != CardMoveReason.banish)
            {
                continue;
            }

            if (!gameState.cardInstances.TryGetValue(cardMovedEvent.cardInstanceId, out var banishedCardInstance) ||
                !string.Equals(banishedCardInstance.definitionId, DefinitionIdT024, StringComparison.Ordinal))
            {
                continue;
            }

            applyT024CurrentTurnPlayerHealAndDraw(gameState, actionChainState, eventId);
        }
    }

    private void applyT024CurrentTurnPlayerHealAndDraw(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.turnState is null ||
            !gameState.players.TryGetValue(gameState.turnState.currentPlayerId, out var currentPlayerState))
        {
            return;
        }

        applyHealToCurrentTurnPlayerActiveCharacter(gameState, actionChainState, eventId, currentPlayerState);
        drawOneForCurrentTurnPlayer(gameState, actionChainState, eventId, currentPlayerState);
    }

    private static void applyHealToCurrentTurnPlayerActiveCharacter(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerState currentPlayerState)
    {
        if (!currentPlayerState.activeCharacterInstanceId.HasValue ||
            !gameState.characterInstances.TryGetValue(
                currentPlayerState.activeCharacterInstanceId.Value,
                out var activeCharacterInstance) ||
            !activeCharacterInstance.isAlive ||
            !activeCharacterInstance.isInPlay)
        {
            return;
        }

        var hpBefore = activeCharacterInstance.currentHp;
        var hpAfter = Math.Min(activeCharacterInstance.maxHp, hpBefore + 3);
        if (hpAfter == hpBefore)
        {
            return;
        }

        activeCharacterInstance.currentHp = hpAfter;
        actionChainState.producedEvents.Add(new HpChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "hpChanged",
            sourceActionChainId = actionChainState.actionChainId,
            targetPlayerId = currentPlayerState.playerId,
            targetCharacterInstanceId = activeCharacterInstance.characterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        });
    }

    private void drawOneForCurrentTurnPlayer(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerState currentPlayerState)
    {
        if (!gameState.zones.TryGetValue(currentPlayerState.deckZoneId, out var deckZoneState) ||
            !gameState.zones.TryGetValue(currentPlayerState.discardZoneId, out var discardZoneState) ||
            !gameState.zones.ContainsKey(currentPlayerState.handZoneId))
        {
            return;
        }

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var shuffledDiscardCardInstanceIds =
                PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in shuffledDiscardCardInstanceIds)
            {
                var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    currentPlayerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    eventId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var topCardInstanceId = deckZoneState.cardInstanceIds[0];
        var topCardInstance = gameState.cardInstances[topCardInstanceId];
        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topCardInstance,
            currentPlayerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            eventId);
        actionChainState.producedEvents.Add(drawEvent);
    }
}
