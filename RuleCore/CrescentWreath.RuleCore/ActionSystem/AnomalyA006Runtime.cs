using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class AnomalyA006Runtime
{
    public const string ConditionActivationContinuationKey = "continuation:anomalyA006ConditionOpponentActivation";
    public const string RewardHumanDiscardContinuationKey = "continuation:anomalyA006RewardOpponentHumanDiscard";

    private const string ConditionActivationContextKey = "anomaly:A006:conditionOpponentActivation";
    private const string ConditionActivationInputTypeKey = "anomalyA006ConditionOpponentActivation";
    private const string RewardHumanDiscardContextKey = "anomaly:A006:rewardOpponentHumanDiscard";
    private const string RewardHumanDiscardInputTypeKey = "anomalyA006RewardOpponentHumanDiscard";
    private const string ActivationAcceptChoiceKey = "activation:accept";
    private const string ActivationDeclineChoiceKey = "activation:decline";
    private const string HandCardChoicePrefix = "handCard:";
    private const string SelectedChoiceStatePrefix = "anomaly:A006:selectedChoice:";
    private const string RaceTagHuman = "human";
    private const string RaceTagNonHuman = "nonHuman";
    private const string TmFactionKey = "TM";

    private readonly ZoneMovementService zoneMovementService;
    private readonly Func<long> nextInputContextIdSupplier;

    public AnomalyA006Runtime(
        ZoneMovementService zoneMovementService,
        Func<long> nextInputContextIdSupplier)
    {
        this.zoneMovementService = zoneMovementService;
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
    }

    public bool isParallelInputContext(InputContextState? inputContextState)
    {
        return inputContextState is not null &&
               inputContextState.requiredPlayerIds.Count > 0 &&
               (string.Equals(inputContextState.contextKey, ConditionActivationContextKey, StringComparison.Ordinal) ||
                string.Equals(inputContextState.contextKey, RewardHumanDiscardContextKey, StringComparison.Ordinal));
    }

    public List<PlayerId> collectOpponentTmActivationPlayers(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorTeamId = resolvePlayerTeamId(gameState, actorPlayerId);
        return gameState.players.Values
            .Where(player => player.teamId != actorTeamId)
            .Where(player => tryGetActiveCharacter(gameState, player.playerId, out _))
            .Where(player =>
            {
                tryGetActiveCharacter(gameState, player.playerId, out var character);
                var definition = CharacterDefinitionRepository.resolveByDefinitionId(character.definitionId);
                return string.Equals(definition.factionKey, TmFactionKey, StringComparison.Ordinal) &&
                       !character.isActivated &&
                       CharacterActivationRuntime.canCharacterActivate(character.characterInstanceId, gameState);
            })
            .Select(player => player.playerId)
            .OrderBy(playerId => playerId.Value)
            .ToList();
    }

    public void openConditionActivationInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        long eventId)
    {
        var inputContextState = createParallelInputContext(
            actionChainState,
            ConditionActivationInputTypeKey,
            ConditionActivationContextKey);
        foreach (var playerId in requiredPlayerIds)
        {
            inputContextState.requiredPlayerIds.Add(playerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[playerId.Value] = new List<string>
            {
                ActivationDeclineChoiceKey,
                ActivationAcceptChoiceKey,
            };
        }

        openInputContext(
            gameState,
            actionChainState,
            inputContextState,
            ConditionActivationContinuationKey,
            eventId);
    }

    public bool continueConditionActivationChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureSingleParallelChoiceValid(inputContextState, request, ConditionActivationContextKey);
        recordSingleChoice(actionChainState, inputContextState, request);
        if (!allRequiredPlayersSubmitted(inputContextState))
        {
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            if (!string.Equals(resolveSelectedChoice(actionChainState, playerId), ActivationAcceptChoiceKey, StringComparison.Ordinal))
            {
                continue;
            }

            if (!tryGetActiveCharacter(gameState, playerId, out var character))
            {
                throw new InvalidOperationException("A006 activation choice requires selected player active character.");
            }

            actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
                gameState,
                character.characterInstanceId,
                true,
                actionChainState.actionChainId,
                request.requestId));
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    public List<PlayerId> collectOpponentHumanPlayersWithHandCards(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorTeamId = resolvePlayerTeamId(gameState, actorPlayerId);
        return gameState.players.Values
            .Where(player => player.teamId != actorTeamId)
            .Where(player => isActiveCharacterRace(gameState, player.playerId, RaceTagHuman))
            .Where(player => gameState.zones.TryGetValue(player.handZoneId, out var handZone) &&
                             handZone.cardInstanceIds.Count > 0)
            .Select(player => player.playerId)
            .OrderBy(playerId => playerId.Value)
            .ToList();
    }

    public void openRewardHumanDiscardInput(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        IReadOnlyList<PlayerId> requiredPlayerIds,
        long eventId)
    {
        var inputContextState = createParallelInputContext(
            actionChainState,
            RewardHumanDiscardInputTypeKey,
            RewardHumanDiscardContextKey);
        foreach (var playerId in requiredPlayerIds)
        {
            var playerState = gameState.players[playerId];
            var handZone = gameState.zones[playerState.handZoneId];
            inputContextState.requiredPlayerIds.Add(playerId);
            inputContextState.choiceKeysByRequiredPlayerNumericId[playerId.Value] =
                handZone.cardInstanceIds
                    .Select(cardInstanceId => HandCardChoicePrefix + cardInstanceId.Value)
                    .ToList();
        }

        openInputContext(
            gameState,
            actionChainState,
            inputContextState,
            RewardHumanDiscardContinuationKey,
            eventId);
    }

    public bool continueRewardHumanDiscardChoice(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        ensureSingleParallelChoiceValid(inputContextState, request, RewardHumanDiscardContextKey);
        var selectedCardInstanceId = parseHandCardChoiceKey(request.choiceKey);
        var playerState = gameState.players[request.actorPlayerId];
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCard) ||
            selectedCard.ownerPlayerId != request.actorPlayerId ||
            selectedCard.zoneId != playerState.handZoneId)
        {
            throw new InvalidOperationException("A006 reward discard requires selected card to remain in actor hand.");
        }

        recordSingleChoice(actionChainState, inputContextState, request);
        if (!allRequiredPlayersSubmitted(inputContextState))
        {
            return false;
        }

        foreach (var playerId in resolvePlayerOrder(inputContextState.requiredPlayerIds))
        {
            var cardInstanceId = parseHandCardChoiceKey(resolveSelectedChoice(actionChainState, playerId));
            var discardPlayerState = gameState.players[playerId];
            var cardInstance = gameState.cardInstances[cardInstanceId];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                cardInstance,
                discardPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId));
        }

        closeInputContext(gameState, actionChainState, inputContextState, request.requestId);
        return true;
    }

    public void healFriendlyNonHumanActiveCharactersToMax(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId actorPlayerId,
        long eventId)
    {
        var actorTeamId = resolvePlayerTeamId(gameState, actorPlayerId);
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (playerState.teamId != actorTeamId ||
                !isActiveCharacterRace(gameState, playerState.playerId, RaceTagNonHuman) ||
                !tryGetActiveCharacter(gameState, playerState.playerId, out var character) ||
                character.currentHp >= character.maxHp)
            {
                continue;
            }

            var hpBefore = character.currentHp;
            character.currentHp = character.maxHp;
            actionChainState.producedEvents.Add(new HpChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "hpChanged",
                sourceActionChainId = actionChainState.actionChainId,
                targetPlayerId = playerState.playerId,
                targetCharacterInstanceId = character.characterInstanceId,
                hpBefore = hpBefore,
                hpAfter = character.currentHp,
                delta = character.currentHp - hpBefore,
            });
        }
    }

    private InputContextState createParallelInputContext(
        ActionChainState actionChainState,
        string inputTypeKey,
        string contextKey)
    {
        return new InputContextState
        {
            inputContextId = new InputContextId(nextInputContextIdSupplier()),
            requiredPlayerId = null,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
    }

    private static void openInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        string continuationKey,
        long eventId)
    {
        if (gameState.currentInputContext is not null || inputContextState.requiredPlayerIds.Count == 0)
        {
            throw new InvalidOperationException("A006 parallel input requires no active input and at least one required player.");
        }

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = continuationKey;
        actionChainState.isCompleted = false;
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = true,
        });
    }

    private static void ensureSingleParallelChoiceValid(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string expectedContextKey)
    {
        if (!string.Equals(inputContextState.contextKey, expectedContextKey, StringComparison.Ordinal) ||
            !inputContextState.requiredPlayerIds.Contains(request.actorPlayerId) ||
            inputContextState.submittedPlayerIds.Contains(request.actorPlayerId) ||
            request.choiceKeys.Count != 0 ||
            !inputContextState.choiceKeysByRequiredPlayerNumericId.TryGetValue(
                request.actorPlayerId.Value,
                out var allowedChoices) ||
            !allowedChoices.Contains(request.choiceKey))
        {
            throw new InvalidOperationException("A006 parallel input choice is not valid for actorPlayerId.");
        }
    }

    private static void recordSingleChoice(
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        inputContextState.submittedPlayerIds.Add(request.actorPlayerId);
        actionChainState.localState[SelectedChoiceStatePrefix + request.actorPlayerId.Value] = request.choiceKey;
    }

    private static bool allRequiredPlayersSubmitted(InputContextState inputContextState)
    {
        return inputContextState.submittedPlayerIds.Count == inputContextState.requiredPlayerIds.Count;
    }

    private static string resolveSelectedChoice(ActionChainState actionChainState, PlayerId playerId)
    {
        if (!actionChainState.localState.TryGetValue(
                SelectedChoiceStatePrefix + playerId.Value,
                out var selectedChoice))
        {
            throw new InvalidOperationException("A006 parallel input requires every selected choice to be recorded.");
        }

        return selectedChoice;
    }

    private static void closeInputContext(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        long eventId)
    {
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = false,
        });
        gameState.currentInputContext = null;
        actionChainState.pendingContinuationKey = null;
    }

    private static TeamId resolvePlayerTeamId(RuleCore.GameState.GameState gameState, PlayerId playerId)
    {
        if (!gameState.players.TryGetValue(playerId, out var playerState))
        {
            throw new InvalidOperationException("A006 requires player state.");
        }

        return playerState.teamId;
    }

    private static bool tryGetActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        out CrescentWreath.RuleCore.Entities.CharacterInstance character)
    {
        character = null!;
        return gameState.players.TryGetValue(playerId, out var playerState) &&
               playerState.activeCharacterInstanceId.HasValue &&
               gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out character) &&
               character.isAlive &&
               character.isInPlay;
    }

    private static bool isActiveCharacterRace(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        string raceTag)
    {
        return tryGetActiveCharacter(gameState, playerId, out var character) &&
               character.raceTags.Any(tag => string.Equals(tag, raceTag, StringComparison.OrdinalIgnoreCase));
    }

    private static List<PlayerId> resolvePlayerOrder(IReadOnlyList<PlayerId> playerIds)
    {
        return playerIds.OrderBy(playerId => playerId.Value).ToList();
    }

    private static CardInstanceId parseHandCardChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(HandCardChoicePrefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(HandCardChoicePrefix.Length), out var numericId))
        {
            throw new InvalidOperationException("A006 hand choice must use handCard:{cardInstanceNumericId}.");
        }

        return new CardInstanceId(numericId);
    }
}
