using System;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Initialization;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerGameSession
{
    private readonly ActionRequestProcessor actionRequestProcessor;

    public RuleCore.GameState.GameState gameState { get; }

    public ServerGameSession(RuleCore.GameState.GameState gameState, ActionRequestProcessor actionRequestProcessor)
    {
        this.gameState = gameState;
        this.actionRequestProcessor = actionRequestProcessor;
    }

    public static ServerGameSession createStandard2v2()
    {
        var gameInitializer = new GameInitializer();
        var initializedGameState = gameInitializer.createStandard2v2MatchState();
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
        }
    }

    public ServerActionProcessResult processPlayTreasureCard(ServerPlayTreasureCardRequestDto requestDto)
    {
        if (!string.Equals(requestDto.playMode, "normal", StringComparison.Ordinal))
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = "ServerPlayTreasureCardRequestDto playMode must be normal.",
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
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
            return new ServerActionProcessResult
            {
                isSucceeded = true,
                updatedState = gameState,
                producedEvents = producedEvents,
                errorMessage = null,
            };
        }
        catch (Exception exception)
        {
            return new ServerActionProcessResult
            {
                isSucceeded = false,
                updatedState = gameState,
                producedEvents = new(),
                errorMessage = exception.Message,
            };
        }
    }
}





