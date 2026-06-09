using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public static class CharacterSetAsideRuntime
{
    public static CardMovedEvent setAsideCardUnderCharacter(
        GameState.GameState gameState,
        ZoneMovementService zoneMovementService,
        CardInstance cardInstance,
        CharacterInstance characterInstance,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var ownerPlayerState = gameState.players[characterInstance.ownerPlayerId];
        var moveEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            ownerPlayerState.characterSetAsideZoneId,
            CardMoveReason.setAside,
            sourceActionChainId,
            eventId);

        cardInstance.ownerPlayerId = characterInstance.ownerPlayerId;
        cardInstance.isFaceUp = false;
        cardInstance.isSetAside = true;
        cardInstance.isDefensePlacedOnField = false;
        return moveEvent;
    }

    public static CardMovedEvent banishSetAsideCard(
        GameState.GameState gameState,
        ZoneMovementService zoneMovementService,
        CardInstance cardInstance,
        ZoneId gapZoneId,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        var moveEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            gapZoneId,
            CardMoveReason.banish,
            sourceActionChainId,
            eventId);

        cardInstance.isSetAside = false;
        cardInstance.isDefensePlacedOnField = false;
        return moveEvent;
    }
}
