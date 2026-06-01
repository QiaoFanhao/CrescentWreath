using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Initialization;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerGameSession
{
    private const string ErrorCodeRequestRejected = "request_rejected";
    private readonly ActionRequestProcessor actionRequestProcessor;
    private readonly TurnFlowAutoAdvanceService turnFlowAutoAdvanceService;
    private readonly ZoneMovementService debugZoneMovementService;

    public RuleCore.GameState.GameState gameState { get; private set; }

    public ServerGameSession(RuleCore.GameState.GameState gameState, ActionRequestProcessor actionRequestProcessor)
    {
        this.gameState = gameState;
        this.actionRequestProcessor = actionRequestProcessor;
        turnFlowAutoAdvanceService = new TurnFlowAutoAdvanceService();
        debugZoneMovementService = new ZoneMovementService();
    }

    public static ServerGameSession createStandard2v2(int? publicDeckShuffleSeed = null)
    {
        var gameInitializer = new GameInitializer();
        var initializedGameState = gameInitializer.createStandard2v2MatchState(publicDeckShuffleSeed);
        return new ServerGameSession(initializedGameState, new ActionRequestProcessor());
    }

    public ServerActionProcessResult processDrawOneCard(ServerDrawOneCardRequestDto requestDto)
    {
        try
        {
            var drawOneCardActionRequest = new DrawOneCardActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                sourceKey = "server:d1-m0",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, drawOneCardActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processPlayTreasureCard(ServerPlayTreasureCardRequestDto requestDto)
    {
        if (!string.Equals(requestDto.playMode, "normal", StringComparison.Ordinal))
        {
            return buildFailureResult(
                requestDto.requestId,
                requestDto.actorPlayerNumericId,
                "ServerPlayTreasureCardRequestDto playMode must be normal.");
        }

        try
        {
            var playTreasureCardActionRequest = new PlayTreasureCardActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                cardInstanceId = new CardInstanceId(requestDto.cardInstanceNumericId),
                playMode = requestDto.playMode,
                sourceKey = "server:d1-m1",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, playTreasureCardActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processEnterSummonPhase(ServerEnterSummonPhaseRequestDto requestDto)
    {
        try
        {
            var enterSummonPhaseActionRequest = new EnterSummonPhaseActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                sourceKey = "server:d1-m3",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, enterSummonPhaseActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processEnterActionPhase(ServerEnterActionPhaseRequestDto requestDto)
    {
        try
        {
            var enterActionPhaseActionRequest = new EnterActionPhaseActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                sourceKey = "server:d1-m6",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, enterActionPhaseActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processUseSkill(ServerUseSkillRequestDto requestDto)
    {
        try
        {
            var useSkillActionRequest = new UseSkillActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                characterInstanceId = new CharacterInstanceId(requestDto.characterInstanceNumericId),
                skillKey = requestDto.skillKey,
                sourceKey = "server:d1-m7",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, useSkillActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processSubmitDefense(ServerSubmitDefenseRequestDto requestDto)
    {
        try
        {
            var submitDefenseActionRequest = new SubmitDefenseActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                defenseTypeKey = requestDto.defenseTypeKey,
                defenseCardInstanceId = new CardInstanceId(requestDto.defenseCardInstanceNumericId),
                sourceKey = "server:d1-m8",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, submitDefenseActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processSubmitResponse(ServerSubmitResponseRequestDto requestDto)
    {
        try
        {
            var submitResponseActionRequest = new SubmitResponseActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                responseWindowId = new ResponseWindowId(requestDto.responseWindowNumericId),
                shouldRespond = requestDto.shouldRespond,
                responseKey = requestDto.responseKey,
                sourceKey = "server:d1-m9",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, submitResponseActionRequest);
            producedEvents = appendAutoAdvanceEventsIfAny(requestDto.requestId, producedEvents);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processSubmitInputChoice(ServerSubmitInputChoiceRequestDto requestDto)
    {
        try
        {
            var submitInputChoiceActionRequest = new SubmitInputChoiceActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                inputContextId = new InputContextId(requestDto.inputContextNumericId),
                choiceKey = requestDto.choiceKey ?? string.Empty,
                sourceKey = "server:d1-m10",
            };

            if (requestDto.choiceKeys is not null)
            {
                foreach (var choiceKey in requestDto.choiceKeys)
                {
                    submitInputChoiceActionRequest.choiceKeys.Add(choiceKey);
                }
            }

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, submitInputChoiceActionRequest);
            producedEvents = appendAutoAdvanceEventsIfAny(requestDto.requestId, producedEvents);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult debugResetMatch(ServerDebugResetMatchRequestDto requestDto)
    {
        try
        {
            var gameInitializer = new GameInitializer();
            gameState = gameInitializer.createStandard2v2MatchState(requestDto.publicDeckShuffleSeed);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, new List<GameEvent>());
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult debugOpenDamageResponseWindow(ServerDebugOpenDamageResponseWindowRequestDto requestDto)
    {
        try
        {
            var actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId);
            var targetCharacterInstanceId = resolveDebugDamageTargetCharacterInstanceId(
                actorPlayerId,
                requestDto.targetCharacterInstanceNumericId);
            var sourceCharacterInstanceId = tryResolveSourceCharacterInstanceId(actorPlayerId);
            var baseDamageValue = requestDto.baseDamageValue > 0 ? requestDto.baseDamageValue : 2;
            var damageTypeKey = string.IsNullOrWhiteSpace(requestDto.damageTypeKey)
                ? "physical"
                : requestDto.damageTypeKey;

            var openDamageResponseWindowActionRequest = new OpenDamageResponseWindowActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = actorPlayerId,
                sourceCharacterInstanceId = sourceCharacterInstanceId,
                targetCharacterInstanceId = targetCharacterInstanceId,
                baseDamageValue = baseDamageValue,
                damageTypeKey = damageTypeKey,
                sourceKey = "server:debugOnly:openDamageResponseWindow",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, openDamageResponseWindowActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    // Debug-only helper to inject a specific treasure definition from publicTreasureDeck into actor hand.
    public ServerActionProcessResult debugMoveTreasureToHandByDefinition(ServerDebugMoveTreasureToHandByDefinitionRequestDto requestDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requestDto.treasureDefinitionId))
            {
                throw new InvalidOperationException("debugMoveTreasureToHandByDefinition requires non-empty treasureDefinitionId.");
            }

            var actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId);
            if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
            {
                throw new InvalidOperationException("debugMoveTreasureToHandByDefinition requires actorPlayerNumericId to exist in gameState.players.");
            }

            var normalizedDefinitionId = requestDto.treasureDefinitionId.Trim();
            var publicTreasureDeckZoneState = getRequiredPublicTreasureDeckZoneState();
            CardInstance? selectedCardInstance = null;
            foreach (var cardInstanceId in publicTreasureDeckZoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
                {
                    continue;
                }

                if (!string.Equals(cardInstance.definitionId, normalizedDefinitionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                selectedCardInstance = cardInstance;
                break;
            }

            if (selectedCardInstance is null)
            {
                throw new InvalidOperationException(
                    $"debugMoveTreasureToHandByDefinition requires treasureDefinitionId={normalizedDefinitionId} to exist in publicTreasureDeck.");
            }

            var cardMovedEvent = debugZoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                actorPlayerState.handZoneId,
                CardMoveReason.draw,
                new ActionChainId(requestDto.requestId),
                requestDto.requestId * 1000 + 1);
            selectedCardInstance.ownerPlayerId = actorPlayerId;
            selectedCardInstance.isFaceUp = false;
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, new List<GameEvent> { cardMovedEvent });
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    // Debug-only helper to force a specific treasure definition to the top of publicTreasureDeck.
    public ServerActionProcessResult debugPutTreasureOnTopByDefinition(ServerDebugPutTreasureOnTopByDefinitionRequestDto requestDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requestDto.treasureDefinitionId))
            {
                throw new InvalidOperationException("debugPutTreasureOnTopByDefinition requires non-empty treasureDefinitionId.");
            }

            var normalizedDefinitionId = requestDto.treasureDefinitionId.Trim();
            var publicTreasureDeckZoneState = getRequiredPublicTreasureDeckZoneState();

            var sourceZoneState = resolveZoneContainingPublicTreasureDefinition(normalizedDefinitionId);
            if (sourceZoneState is null)
            {
                throw new InvalidOperationException(
                    $"debugPutTreasureOnTopByDefinition requires treasureDefinitionId={normalizedDefinitionId} to exist in public treasure zones.");
            }

            CardInstance? selectedCardInstance = null;
            foreach (var cardInstanceId in sourceZoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
                {
                    continue;
                }

                if (!string.Equals(cardInstance.definitionId, normalizedDefinitionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                selectedCardInstance = cardInstance;
                break;
            }

            if (selectedCardInstance is null)
            {
                throw new InvalidOperationException(
                    $"debugPutTreasureOnTopByDefinition cannot resolve card instance for definitionId={normalizedDefinitionId}.");
            }

            var producedEvents = new List<GameEvent>();
            if (selectedCardInstance.zoneId != gameState.publicState!.publicTreasureDeckZoneId)
            {
                var cardMovedEvent = debugZoneMovementService.moveCard(
                    gameState,
                    selectedCardInstance,
                    gameState.publicState.publicTreasureDeckZoneId,
                    CardMoveReason.returnToSource,
                    new ActionChainId(requestDto.requestId),
                    requestDto.requestId * 1000 + 1);
                producedEvents.Add(cardMovedEvent);
            }

            if (publicTreasureDeckZoneState.cardInstanceIds.Count > 0 &&
                publicTreasureDeckZoneState.cardInstanceIds[0] != selectedCardInstance.cardInstanceId)
            {
                publicTreasureDeckZoneState.cardInstanceIds.Remove(selectedCardInstance.cardInstanceId);
                publicTreasureDeckZoneState.cardInstanceIds.Insert(0, selectedCardInstance.cardInstanceId);
            }

            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult processEnterEndPhase(ServerEnterEndPhaseRequestDto requestDto)
    {
        try
        {
            var enterEndPhaseActionRequest = new EnterEndPhaseActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                sourceKey = "server:d1-m4",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, enterEndPhaseActionRequest);
            producedEvents = appendAutoAdvanceEventsIfAny(requestDto.requestId, producedEvents);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }
    public ServerActionProcessResult processStartNextTurn(ServerStartNextTurnRequestDto requestDto)
    {
        try
        {
            var startNextTurnActionRequest = new StartNextTurnActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                sourceKey = "server:d1-m5",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, startNextTurnActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }
    public ServerActionProcessResult processSummonTreasureCard(ServerSummonTreasureCardRequestDto requestDto)
    {
        try
        {
            var summonTreasureCardActionRequest = new SummonTreasureCardActionRequest
            {
                requestId = requestDto.requestId,
                actorPlayerId = new PlayerId(requestDto.actorPlayerNumericId),
                cardInstanceId = new CardInstanceId(requestDto.cardInstanceNumericId),
                sourceKey = "server:d1-m2",
            };

            var producedEvents = actionRequestProcessor.processActionRequest(gameState, summonTreasureCardActionRequest);
            return buildSuccessResult(requestDto.requestId, requestDto.actorPlayerNumericId, producedEvents);
        }
        catch (Exception exception)
        {
            return buildFailureResult(requestDto.requestId, requestDto.actorPlayerNumericId, exception.Message);
        }
    }

    public ServerActionProcessResult projectResultForViewer(ServerActionProcessResult originalResult, long viewerPlayerNumericId)
    {
        var resolvedViewerPlayerId = ServerProjectionBuilder.resolveViewerPlayerId(gameState, viewerPlayerNumericId);
        return new ServerActionProcessResult
        {
            requestId = originalResult.requestId,
            isSucceeded = originalResult.isSucceeded,
            viewerPlayerNumericId = resolvedViewerPlayerId.Value,
            error = originalResult.error is null
                ? null
                : new ServerErrorProjection
                {
                    code = originalResult.error.code,
                    message = originalResult.error.message,
                },
            stateProjection = ServerProjectionBuilder.buildStateProjection(gameState, resolvedViewerPlayerId),
            eventLog = originalResult.isSucceeded
                ? ServerProjectionBuilder.buildEventLog(originalResult.producedEvents)
                : new List<ServerEventLogEntry>(),
            interaction = ServerProjectionBuilder.buildInteractionProjection(gameState, resolvedViewerPlayerId),
            updatedState = gameState,
            producedEvents = originalResult.producedEvents,
            errorMessage = originalResult.errorMessage,
        };
    }

    private ServerActionProcessResult buildSuccessResult(
        long requestId,
        long viewerPlayerNumericId,
        List<GameEvent> producedEvents)
    {
        var resolvedViewerPlayerId = ServerProjectionBuilder.resolveViewerPlayerId(gameState, viewerPlayerNumericId);
        return new ServerActionProcessResult
        {
            requestId = requestId,
            isSucceeded = true,
            viewerPlayerNumericId = resolvedViewerPlayerId.Value,
            error = null,
            stateProjection = ServerProjectionBuilder.buildStateProjection(gameState, resolvedViewerPlayerId),
            eventLog = ServerProjectionBuilder.buildEventLog(producedEvents),
            interaction = ServerProjectionBuilder.buildInteractionProjection(gameState, resolvedViewerPlayerId),
            updatedState = gameState,
            producedEvents = producedEvents,
            errorMessage = null,
        };
    }

    private ServerActionProcessResult buildFailureResult(
        long requestId,
        long viewerPlayerNumericId,
        string errorMessage)
    {
        var resolvedViewerPlayerId = ServerProjectionBuilder.resolveViewerPlayerId(gameState, viewerPlayerNumericId);
        return new ServerActionProcessResult
        {
            requestId = requestId,
            isSucceeded = false,
            viewerPlayerNumericId = resolvedViewerPlayerId.Value,
            error = new ServerErrorProjection
            {
                code = ErrorCodeRequestRejected,
                message = errorMessage,
            },
            stateProjection = ServerProjectionBuilder.buildStateProjection(gameState, resolvedViewerPlayerId),
            eventLog = new(),
            interaction = ServerProjectionBuilder.buildInteractionProjection(gameState, resolvedViewerPlayerId),
            updatedState = gameState,
            producedEvents = new(),
            errorMessage = errorMessage,
        };
    }

    private List<GameEvent> appendAutoAdvanceEventsIfAny(long requestId, List<GameEvent> producedEvents)
    {
        var mergedEvents = new List<GameEvent>(producedEvents);
        var autoAdvancedEvents = turnFlowAutoAdvanceService.tryAutoAdvanceUntilPlayerActionRequired(
            gameState,
            actionRequestProcessor,
            requestId);
        mergedEvents.AddRange(autoAdvancedEvents);
        return mergedEvents;
    }

    private CharacterInstanceId resolveDebugDamageTargetCharacterInstanceId(PlayerId actorPlayerId, long targetCharacterInstanceNumericId)
    {
        if (targetCharacterInstanceNumericId > 0)
        {
            var requestedCharacterInstanceId = new CharacterInstanceId(targetCharacterInstanceNumericId);
            if (!gameState.characterInstances.ContainsKey(requestedCharacterInstanceId))
            {
                throw new InvalidOperationException("debugOpenDamageResponseWindow requires targetCharacterInstanceNumericId to exist in gameState.characterInstances.");
            }

            return requestedCharacterInstanceId;
        }

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("debugOpenDamageResponseWindow requires actorPlayerNumericId to exist in gameState.players.");
        }

        var targetCharacter = gameState.characterInstances.Values
            .Where(character => character.isAlive && character.isInPlay)
            .Where(character => character.ownerPlayerId != actorPlayerId)
            .Where(character => gameState.players.TryGetValue(character.ownerPlayerId, out var ownerPlayerState) &&
                                ownerPlayerState.teamId != actorPlayerState.teamId)
            .OrderBy(character => character.characterInstanceId.Value)
            .FirstOrDefault();

        if (targetCharacter is null)
        {
            throw new InvalidOperationException("debugOpenDamageResponseWindow requires at least one alive in-play enemy character.");
        }

        return targetCharacter.characterInstanceId;
    }

    private CharacterInstanceId? tryResolveSourceCharacterInstanceId(PlayerId actorPlayerId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            return null;
        }

        return actorPlayerState.activeCharacterInstanceId;
    }

    private ZoneState getRequiredPublicTreasureDeckZoneState()
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("Debug treasure injection requires gameState.publicState.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException("Debug treasure injection requires publicTreasureDeck zone to exist.");
        }

        return publicTreasureDeckZoneState;
    }

    private ZoneState? resolveZoneContainingPublicTreasureDefinition(string definitionId)
    {
        if (gameState.publicState is null)
        {
            return null;
        }

        var candidateZoneIds = new[]
        {
            gameState.publicState.publicTreasureDeckZoneId,
            gameState.publicState.summonZoneId,
            gameState.publicState.gapZoneId,
        };

        foreach (var zoneId in candidateZoneIds)
        {
            if (!gameState.zones.TryGetValue(zoneId, out var zoneState))
            {
                continue;
            }

            foreach (var cardInstanceId in zoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
                {
                    continue;
                }

                if (string.Equals(cardInstance.definitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                {
                    return zoneState;
                }
            }
        }

        return null;
    }
}
