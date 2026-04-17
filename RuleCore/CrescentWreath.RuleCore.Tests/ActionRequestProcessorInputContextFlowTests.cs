using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorInputContextFlowTests
{
    [Fact]
    public void OpenAndSubmitInputChoice_HappyPath_ShouldOpenAndCloseInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8101,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testInputContext",
        };
        openRequest.choiceKeys.Add("confirm");

        var openEvents = processor.processActionRequest(gameState, openRequest);

        Assert.NotNull(gameState.currentActionChain);
        Assert.NotNull(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.False(gameState.currentActionChain.isCompleted);
        Assert.Single(openEvents);

        var openEvent = Assert.IsType<InteractionWindowEvent>(openEvents[0]);
        Assert.True(openEvent.isOpened);
        Assert.True(openEvent.inputContextId.HasValue);

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8102,
            actorPlayerId = actorPlayerId,
            inputContextId = openEvent.inputContextId!.Value,
            choiceKey = "confirm",
        };

        var eventsAfterSubmit = processor.processActionRequest(gameState, submitRequest);

        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(2, eventsAfterSubmit.Count);

        var closeEvent = Assert.IsType<InteractionWindowEvent>(eventsAfterSubmit[1]);
        Assert.False(closeEvent.isOpened);
        Assert.Equal(openEvent.inputContextId, closeEvent.inputContextId);
    }

    [Fact]
    public void OpenInputContext_WhenCurrentInputContextAlreadyExists_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var existingActionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(9901),
        };
        existingActionChain.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = 9901,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = existingActionChain.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = new InputContextId(9901),
            isOpened = true,
        });
        gameState.currentActionChain = existingActionChain;

        var existingInputContext = new InputContextState
        {
            inputContextId = new InputContextId(9901),
            requiredPlayerId = actorPlayerId,
            sourceActionChainId = existingActionChain.actionChainId,
            inputTypeKey = "existing",
            contextKey = "existingContext",
        };
        gameState.currentInputContext = existingInputContext;

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8103,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "newContext",
        };
        openRequest.choiceKeys.Add("confirm");

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, openRequest));

        Assert.Equal("OpenInputContextActionRequest cannot open while currentInputContext is active.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(existingInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Same(existingActionChain, gameState.currentActionChain);
        Assert.Single(gameState.currentActionChain!.producedEvents);
    }

    [Fact]
    public void OpenInputContext_WhenMatchStateIsNotRunning_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));
        gameState.matchState = MatchState.initializing;

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8111,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "openGuardMatchState",
        };
        openRequest.choiceKeys.Add("confirm");

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, openRequest));

        Assert.Equal("OpenInputContextActionRequest requires gameState.matchState to be running.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void OpenInputContext_WhenTurnStateIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));
        gameState.turnState = null;

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8121,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "openGuardTurnState",
        };
        openRequest.choiceKeys.Add("confirm");

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, openRequest));

        Assert.Equal("OpenInputContextActionRequest requires gameState.turnState to be initialized.", exception.Message);
        Assert.Null(gameState.currentActionChain);
        Assert.Null(gameState.currentInputContext);
    }

    [Fact]
    public void SubmitInputChoice_WithMismatchedInputContextId_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8201,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testMismatch",
        };
        openRequest.choiceKeys.Add("confirm");

        var openEvents = processor.processActionRequest(gameState, openRequest);
        var openEvent = Assert.IsType<InteractionWindowEvent>(openEvents[0]);
        var pendingContinuationKeyBefore = gameState.currentActionChain!.pendingContinuationKey;

        var mismatchedSubmitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8202,
            actorPlayerId = actorPlayerId,
            inputContextId = new InputContextId(openEvent.inputContextId!.Value.Value + 999),
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, mismatchedSubmitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest inputContextId mismatch.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(openEvent.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
        Assert.Single(gameState.currentActionChain!.producedEvents);
        Assert.Equal(openEvent.inputContextId, ((InteractionWindowEvent)gameState.currentActionChain.producedEvents[0]).inputContextId);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenCurrentActionChainIsNull_ShouldThrowAndKeepInputContextUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8251,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testNullActionChain",
        };
        openRequest.choiceKeys.Add("confirm");

        var openEvents = processor.processActionRequest(gameState, openRequest);
        var openEvent = Assert.IsType<InteractionWindowEvent>(openEvents[0]);
        var preservedInputContext = gameState.currentInputContext!;

        gameState.currentActionChain = null;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8252,
            actorPlayerId = actorPlayerId,
            inputContextId = openEvent.inputContextId!.Value,
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest requires an active currentActionChain.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(preservedInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenCurrentInputContextIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8301,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testNullInputContext",
        };
        openRequest.choiceKeys.Add("confirm");

        processor.processActionRequest(gameState, openRequest);
        var preservedInputContextState = gameState.currentInputContext!;
        var producedEventsBefore = gameState.currentActionChain!.producedEvents.Count;

        gameState.currentInputContext = null;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8302,
            actorPlayerId = actorPlayerId,
            inputContextId = preservedInputContextState.inputContextId,
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest requires an active currentInputContext.", exception.Message);
        Assert.Null(gameState.currentInputContext);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Null(preservedInputContextState.selectedChoiceKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenActorPlayerIdDoesNotMatchRequiredPlayerId_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8401,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testActorMismatch",
        };
        openRequest.choiceKeys.Add("confirm");

        processor.processActionRequest(gameState, openRequest);
        var currentInputContext = gameState.currentInputContext!;
        var producedEventsBefore = gameState.currentActionChain!.producedEvents.Count;
        var pendingContinuationKeyBefore = gameState.currentActionChain.pendingContinuationKey;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8402,
            actorPlayerId = new PlayerId(actorPlayerId.Value + 1),
            inputContextId = currentInputContext.inputContextId,
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(currentInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenChoiceKeyIsNotAllowed_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8501,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testChoiceMismatch",
        };
        openRequest.choiceKeys.Add("confirm");

        processor.processActionRequest(gameState, openRequest);
        var currentInputContext = gameState.currentInputContext!;
        var producedEventsBefore = gameState.currentActionChain!.producedEvents.Count;
        var pendingContinuationKeyBefore = gameState.currentActionChain.pendingContinuationKey;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8502,
            actorPlayerId = actorPlayerId,
            inputContextId = currentInputContext.inputContextId,
            choiceKey = "cancel",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(currentInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenMatchStateIsNotRunning_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8601,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testMatchStateGuard",
        };
        openRequest.choiceKeys.Add("confirm");

        processor.processActionRequest(gameState, openRequest);
        var currentActionChain = gameState.currentActionChain!;
        var currentInputContext = gameState.currentInputContext!;
        var producedEventsBefore = currentActionChain.producedEvents.Count;
        var pendingContinuationKeyBefore = currentActionChain.pendingContinuationKey;

        gameState.matchState = MatchState.initializing;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8602,
            actorPlayerId = actorPlayerId,
            inputContextId = currentInputContext.inputContextId,
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest requires gameState.matchState to be running.", exception.Message);
        Assert.Same(currentActionChain, gameState.currentActionChain);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(currentInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    [Fact]
    public void SubmitInputChoice_WhenTurnStateIsNull_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var gameState = new RuleCore.GameState.GameState();
        var processor = new ActionRequestProcessor();
        setRunningTurnForActor(gameState, actorPlayerId, new TeamId(1));

        var openRequest = new OpenInputContextActionRequest
        {
            requestId = 8701,
            actorPlayerId = actorPlayerId,
            inputTypeKey = "simpleChoice",
            contextKey = "testTurnStateGuard",
        };
        openRequest.choiceKeys.Add("confirm");

        processor.processActionRequest(gameState, openRequest);
        var currentActionChain = gameState.currentActionChain!;
        var currentInputContext = gameState.currentInputContext!;
        var producedEventsBefore = currentActionChain.producedEvents.Count;
        var pendingContinuationKeyBefore = currentActionChain.pendingContinuationKey;

        gameState.turnState = null;

        var submitRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 8702,
            actorPlayerId = actorPlayerId,
            inputContextId = currentInputContext.inputContextId,
            choiceKey = "confirm",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, submitRequest));

        Assert.Equal("SubmitInputChoiceActionRequest requires gameState.turnState to be initialized.", exception.Message);
        Assert.Same(currentActionChain, gameState.currentActionChain);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(currentInputContext.inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Null(gameState.currentInputContext.selectedChoiceKey);
        Assert.Equal(producedEventsBefore, gameState.currentActionChain!.producedEvents.Count);
        Assert.Equal(pendingContinuationKeyBefore, gameState.currentActionChain.pendingContinuationKey);
    }

    private static void setRunningTurnForActor(
        RuleCore.GameState.GameState gameState,
        PlayerId currentPlayerId,
        TeamId currentTeamId)
    {
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = currentPlayerId,
            currentTeamId = currentTeamId,
            currentPhase = TurnPhase.start,
            phaseStepIndex = 0,
        };
    }
}
