using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class CharacterSelectionRuntime
{
    private const long CharacterInstanceNumericIdBase = 200000;

    public static List<GameEvent> submitSelection(
        GameState.GameState gameState,
        SubmitCharacterSelectionActionRequest request)
    {
        var selectionState = gameState.characterSelectionState
            ?? throw new InvalidOperationException("Character selection is not active.");
        if (gameState.matchState != MatchState.initializing || selectionState.isCompleted)
        {
            throw new InvalidOperationException("Character selection has already completed.");
        }

        if (selectionState.currentSelectingPlayerId != request.actorPlayerId)
        {
            throw new InvalidOperationException(
                $"Character selection currently requires Player {selectionState.currentSelectingPlayerId?.Value.ToString() ?? "(none)"}.");
        }

        if (!gameState.players.TryGetValue(request.actorPlayerId, out var playerState))
        {
            throw new InvalidOperationException("Character selection actor does not exist.");
        }

        var definition = CharacterDefinitionRepository.resolveByDefinitionId(request.characterDefinitionId);
        if (string.IsNullOrWhiteSpace(definition.characterName))
        {
            throw new InvalidOperationException($"Unknown character definition: {request.characterDefinitionId}.");
        }

        if (!definition.isImplemented)
        {
            throw new InvalidOperationException($"{definition.definitionId} is currently a placeholder and cannot be selected.");
        }

        if (selectionState.selectedCharacterDefinitionIds.Values.Contains(
                definition.definitionId,
                StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"{definition.definitionId} has already been selected.");
        }

        var seatIndex = gameState.matchMeta!.seatOrder.IndexOf(request.actorPlayerId);
        if (seatIndex < 0)
        {
            throw new InvalidOperationException("Character selection actor is not in match seat order.");
        }

        var characterInstanceId = new CharacterInstanceId(CharacterInstanceNumericIdBase + seatIndex);
        var characterInstance = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = definition.definitionId,
            ownerPlayerId = request.actorPlayerId,
            currentHp = definition.baseMaxHp,
            maxHp = definition.baseMaxHp,
            isAlive = true,
            isInPlay = true,
        };
        characterInstance.raceTags.AddRange(definition.raceTags);

        gameState.characterInstances[characterInstanceId] = characterInstance;
        playerState.activeCharacterInstanceId = characterInstanceId;
        selectionState.selectedCharacterDefinitionIds[request.actorPlayerId] = definition.definitionId;

        var producedEvent = new CharacterSelectedEvent
        {
            eventId = request.requestId,
            eventTypeKey = "characterSelected",
            playerId = request.actorPlayerId,
            characterInstanceId = characterInstanceId,
            characterDefinitionId = definition.definitionId,
        };

        var nextPlayerId = gameState.matchMeta.seatOrder
            .FirstOrDefault(playerId => !selectionState.selectedCharacterDefinitionIds.ContainsKey(playerId));
        if (nextPlayerId.Value > 0)
        {
            selectionState.currentSelectingPlayerId = nextPlayerId;
        }
        else
        {
            selectionState.currentSelectingPlayerId = null;
            selectionState.isCompleted = true;
            gameState.matchState = MatchState.running;
        }

        return new List<GameEvent> { producedEvent };
    }
}
