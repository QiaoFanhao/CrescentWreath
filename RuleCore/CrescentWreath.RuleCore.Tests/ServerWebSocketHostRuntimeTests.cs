using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.ServerPrototype;

namespace CrescentWreath.RuleCore.Tests;

public class ServerWebSocketHostRuntimeTests
{
    [Fact]
    public async Task RouteMessage_WhenDrawOneCardRequestIsValid_ShouldReturnSucceededResponseWithStateProjection()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700001L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
            },
        });

        Assert.True(response.isSucceeded);
        Assert.Null(response.error);
        Assert.NotNull(response.stateProjection);
        Assert.NotEmpty(response.eventLog);
        Assert.NotNull(response.stateProjection!.turn);
        Assert.True(response.stateProjection.turn!.turnNumber >= 1);
        Assert.True(response.stateProjection.turn.currentPlayerNumericId > 0);
        Assert.False(string.IsNullOrWhiteSpace(response.stateProjection.turn.currentPhase));
        Assert.NotNull(response.stateProjection.publicZones);
        Assert.True(response.stateProjection.publicZones!.summonZone.cardCount >= 0);
        Assert.True(response.stateProjection.publicZones.sakuraCakeDeckZone.cardCount >= 0);
        Assert.NotNull(response.interaction);
        Assert.True(
            response.interaction!.responseWindow is null ||
            response.interaction.responseWindow.responseWindowNumericId > 0);
        Assert.True(
            response.interaction.inputContext is null ||
            response.interaction.inputContext.inputContextNumericId > 0);
    }

    [Fact]
    public async Task RouteMessage_WhenActionTypeIsUnknown_ShouldReturnUnsupportedActionTypeAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var viewerPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var turnNumberBefore = hostRuntime.gameSession.gameState.turnState!.turnNumber;
        var currentPlayerBefore = hostRuntime.gameSession.gameState.turnState.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700002L,
            viewerPlayerNumericId = viewerPlayerId.Value,
            actionType = "unknownActionType",
            payload = new { },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeUnsupportedActionType, response.error!.code);
        Assert.Equal(turnNumberBefore, hostRuntime.gameSession.gameState.turnState!.turnNumber);
        Assert.Equal(currentPlayerBefore, hostRuntime.gameSession.gameState.turnState.currentPlayerId);
    }

    [Fact]
    public async Task RouteMessage_WhenPayloadIsInvalid_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var viewerPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var handCountBefore = getHandCount(hostRuntime.gameSession, viewerPlayerId);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700003L,
            viewerPlayerNumericId = viewerPlayerId.Value,
            actionType = "drawOneCard",
            payload = new { },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Equal(handCountBefore, getHandCount(hostRuntime.gameSession, viewerPlayerId));
    }

    [Fact]
    public async Task RouteMessage_WhenPayloadIsNotObject_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var viewerPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var handCountBefore = getHandCount(hostRuntime.gameSession, viewerPlayerId);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700006L,
            viewerPlayerNumericId = viewerPlayerId.Value,
            actionType = "drawOneCard",
            payload = "not-an-object",
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Equal(handCountBefore, getHandCount(hostRuntime.gameSession, viewerPlayerId));
    }

    [Fact]
    public async Task RouteMessage_WhenPayloadFieldTypeIsInvalid_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var viewerPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var handCountBefore = getHandCount(hostRuntime.gameSession, viewerPlayerId);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700007L,
            viewerPlayerNumericId = viewerPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = "abc",
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Equal(handCountBefore, getHandCount(hostRuntime.gameSession, viewerPlayerId));
    }

    [Fact]
    public async Task RouteMessage_WhenPlayTreasureCardPayloadIsValidButRuleRejected_ShouldReturnRequestRejected()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        hostRuntime.gameSession.gameState.turnState.currentPhase = CrescentWreath.RuleCore.GameState.TurnPhase.action;
        var actorState = hostRuntime.gameSession.gameState.players[actorPlayerId];
        var deckCardId = hostRuntime.gameSession.gameState.zones[actorState.deckZoneId].cardInstanceIds[0];

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700008L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "playTreasureCard",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                cardInstanceNumericId = deckCardId.Value,
                playMode = "normal",
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("handZoneId", response.error.message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RouteMessage_WhenSummonTreasureCardPayloadIsValidButRuleRejected_ShouldReturnRequestRejected()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        hostRuntime.gameSession.gameState.turnState.currentPhase = CrescentWreath.RuleCore.GameState.TurnPhase.summon;
        var actorState = hostRuntime.gameSession.gameState.players[actorPlayerId];
        actorState.isSigilLocked = true;
        actorState.lockedSigil = 10;

        var handCardId = hostRuntime.gameSession.gameState.zones[actorState.handZoneId].cardInstanceIds[0];

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700009L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "summonTreasureCard",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                cardInstanceNumericId = handCardId.Value,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("summonZoneId", response.error.message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitDefensePayloadIsValidButWindowMissing_ShouldReturnRequestRejected()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700010L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitDefense",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                defenseTypeKey = "fixedReduce1",
                defenseCardInstanceNumericId = 0,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("currentActionChain", response.error.message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitResponsePayloadMissingResponseWindowId_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700012L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Null(hostRuntime.gameSession.gameState.currentResponseWindow);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitResponsePayloadIsValidButWindowMissing_ShouldReturnRequestRejected()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700013L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                responseWindowNumericId = 123L,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("currentActionChain", response.error.message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitResponsePayloadIsValidAndResponderMatches_ShouldReturnSucceededAndCloseWindow()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var preparedResponse = prepareDamageResponseWindowForSubmitResponse(hostRuntime.gameSession, 770001L);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700014L,
            viewerPlayerNumericId = preparedResponse.defenderPlayerId.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = preparedResponse.defenderPlayerId.Value,
                responseWindowNumericId = preparedResponse.responseWindowId.Value,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        Assert.True(response.isSucceeded);
        Assert.Null(hostRuntime.gameSession.gameState.currentResponseWindow);
        Assert.Contains(
            response.eventLog,
            entry => string.Equals(entry.eventTypeKey, "damageResolved", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitResponseActorIsNotCurrentResponder_ShouldReturnRequestRejectedAndKeepWindow()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var preparedResponse = prepareDamageResponseWindowForSubmitResponse(hostRuntime.gameSession, 770101L);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700015L,
            viewerPlayerNumericId = preparedResponse.sourcePlayerId.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = preparedResponse.sourcePlayerId.Value,
                responseWindowNumericId = preparedResponse.responseWindowId.Value,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("currentResponderPlayerId", response.error.message, StringComparison.Ordinal);
        Assert.NotNull(hostRuntime.gameSession.gameState.currentResponseWindow);
        Assert.Equal(preparedResponse.responseWindowId, hostRuntime.gameSession.gameState.currentResponseWindow!.responseWindowId);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitInputChoicePayloadMissingInputContextId_ShouldReturnInvalidPayload()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700020L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                choiceKey = "confirm",
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitInputChoicePayloadIsValidButInputContextMissing_ShouldReturnRequestRejected()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700021L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                inputContextNumericId = 123L,
                choiceKey = "confirm",
                choiceKeys = Array.Empty<string>(),
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("currentActionChain", response.error.message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitInputChoicePayloadIsValidAndRequiredPlayerMatches_ShouldReturnSucceededAndCloseInputContext()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var inputContextId = prepareInputContextForSubmitInputChoice(hostRuntime.gameSession, actorPlayerId, 770201L, "confirm", "decline");

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700022L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                inputContextNumericId = inputContextId.Value,
                choiceKey = "confirm",
                choiceKeys = Array.Empty<string>(),
            },
        });

        Assert.True(response.isSucceeded);
        Assert.Null(hostRuntime.gameSession.gameState.currentInputContext);
        Assert.NotNull(response.interaction);
        Assert.Null(response.interaction!.inputContext);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitInputChoiceActorIsNotRequiredPlayer_ShouldReturnRequestRejectedAndKeepInputContext()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var requiredPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var nonRequiredPlayerId = hostRuntime.gameSession.gameState.players.Keys.First(playerId => playerId != requiredPlayerId);
        var inputContextId = prepareInputContextForSubmitInputChoice(hostRuntime.gameSession, requiredPlayerId, 770301L, "confirm");

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700023L,
            viewerPlayerNumericId = nonRequiredPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = nonRequiredPlayerId.Value,
                inputContextNumericId = inputContextId.Value,
                choiceKey = "confirm",
                choiceKeys = Array.Empty<string>(),
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("requiredPlayerId", response.error.message, StringComparison.Ordinal);
        Assert.NotNull(hostRuntime.gameSession.gameState.currentInputContext);
    }

    [Fact]
    public async Task RouteMessage_WhenSubmitInputChoiceChoiceKeyIsInvalid_ShouldReturnRequestRejectedAndKeepInputContext()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var inputContextId = prepareInputContextForSubmitInputChoice(hostRuntime.gameSession, actorPlayerId, 770401L, "confirm");

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700024L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                inputContextNumericId = inputContextId.Value,
                choiceKey = "unknownChoice",
                choiceKeys = Array.Empty<string>(),
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("choiceKey", response.error.message, StringComparison.Ordinal);
        Assert.NotNull(hostRuntime.gameSession.gameState.currentInputContext);
    }

    [Fact]
    public async Task RouteMessage_WhenDebugOpenDamageResponseWindowPayloadIsValid_ShouldOpenResponseWindow()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var sourcePlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700016L,
            viewerPlayerNumericId = sourcePlayerId.Value,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                actorPlayerNumericId = sourcePlayerId.Value,
                targetCharacterInstanceNumericId = 0,
                baseDamageValue = 2,
                damageTypeKey = "physical",
            },
        });

        Assert.True(response.isSucceeded);
        Assert.NotNull(response.interaction);
        Assert.NotNull(response.interaction!.responseWindow);
        Assert.True(response.interaction.responseWindow!.responseWindowNumericId > 0);
        Assert.NotNull(hostRuntime.gameSession.gameState.currentResponseWindow);
    }

    [Fact]
    public async Task RouteMessage_WhenDebugResetMatchPayloadIsValid_ShouldReinitializeSessionState()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var drawResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700025L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
            },
        });

        Assert.True(drawResponse.isSucceeded);
        var actorHandZoneIdBeforeReset = hostRuntime.gameSession.gameState.players[actorPlayerId].handZoneId;
        Assert.Equal(7, hostRuntime.gameSession.gameState.zones[actorHandZoneIdBeforeReset].cardInstanceIds.Count);

        var resetResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700026L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "debugResetMatch",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
            },
        });

        Assert.True(resetResponse.isSucceeded);
        Assert.NotNull(resetResponse.stateProjection);
        Assert.Equal(1, hostRuntime.gameSession.gameState.turnState!.turnNumber);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, hostRuntime.gameSession.gameState.turnState.currentPhase);
        var currentPlayerAfterReset = hostRuntime.gameSession.gameState.turnState.currentPlayerId;
        var currentPlayerHandZoneIdAfterReset = hostRuntime.gameSession.gameState.players[currentPlayerAfterReset].handZoneId;
        Assert.Equal(6, hostRuntime.gameSession.gameState.zones[currentPlayerHandZoneIdAfterReset].cardInstanceIds.Count);
        Assert.Equal(6, resetResponse.stateProjection!.publicZones!.summonZone.cardCount);
        Assert.Equal(15, resetResponse.stateProjection.publicZones.sakuraCakeDeckZone.cardCount);
    }

    [Fact]
    public async Task RouteMessage_WhenDebugResetMatchPayloadIsInvalid_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var currentPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var currentPlayerHandZoneId = hostRuntime.gameSession.gameState.players[currentPlayerId].handZoneId;
        var handCountBefore = hostRuntime.gameSession.gameState.zones[currentPlayerHandZoneId].cardInstanceIds.Count;
        var turnNumberBefore = hostRuntime.gameSession.gameState.turnState.turnNumber;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700027L,
            viewerPlayerNumericId = currentPlayerId.Value,
            actionType = "debugResetMatch",
            payload = new { },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Equal(turnNumberBefore, hostRuntime.gameSession.gameState.turnState.turnNumber);
        Assert.Equal(handCountBefore, hostRuntime.gameSession.gameState.zones[currentPlayerHandZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public async Task RouteMessage_WhenDebugOpenDamageResponseWindowPayloadIsInvalid_ShouldReturnInvalidPayloadAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var responseWindowBefore = hostRuntime.gameSession.gameState.currentResponseWindow;

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700017L,
            viewerPlayerNumericId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId.Value,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                baseDamageValue = 2,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
        Assert.Equal(responseWindowBefore, hostRuntime.gameSession.gameState.currentResponseWindow);
    }

    [Fact]
    public async Task RouteMessage_WhenDebugWindowOpenedThenSubmitResponseNo_ShouldCloseWindowAndResolveDamage()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var sourcePlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        var openResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700018L,
            viewerPlayerNumericId = sourcePlayerId.Value,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                actorPlayerNumericId = sourcePlayerId.Value,
                targetCharacterInstanceNumericId = 0,
                baseDamageValue = 2,
                damageTypeKey = "physical",
            },
        });

        Assert.True(openResponse.isSucceeded);
        Assert.NotNull(openResponse.interaction);
        Assert.NotNull(openResponse.interaction!.responseWindow);
        var responseWindowId = openResponse.interaction.responseWindow!.responseWindowNumericId;
        var currentResponderPlayerNumericId = openResponse.interaction.responseWindow.currentResponderPlayerNumericId;

        Assert.True(responseWindowId > 0);
        Assert.True(currentResponderPlayerNumericId.HasValue);

        var submitResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700019L,
            viewerPlayerNumericId = currentResponderPlayerNumericId!.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = currentResponderPlayerNumericId.Value,
                responseWindowNumericId = responseWindowId,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        Assert.True(submitResponse.isSucceeded);
        Assert.Null(hostRuntime.gameSession.gameState.currentResponseWindow);
        Assert.Contains(
            submitResponse.eventLog,
            entry => string.Equals(entry.eventTypeKey, "damageResolved", StringComparison.Ordinal));
        Assert.Contains(
            submitResponse.eventLog,
            entry => string.Equals(entry.eventTypeKey, "hpChanged", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RouteMessage_WhenActorIsNotCurrentPlayer_ShouldReturnRequestRejectedAndKeepStateUnchanged()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var currentPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var otherPlayerId = hostRuntime.gameSession.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var currentPlayerHandCountBefore = getHandCount(hostRuntime.gameSession, currentPlayerId);

        var response = await sendRequestAsync(wsUri, new
        {
            requestId = 700011L,
            viewerPlayerNumericId = currentPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = otherPlayerId.Value,
            },
        });

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
        Assert.Contains("bound viewerPlayerNumericId", response.error.message, StringComparison.Ordinal);
        Assert.Equal(currentPlayerHandCountBefore, getHandCount(hostRuntime.gameSession, currentPlayerId));
    }

    [Fact]
    public async Task RouteMessage_WhenViewerDiffers_ShouldProjectHiddenOpponentHand()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var viewerOnePlayerId = new PlayerId(1);
        var viewerTwoPlayerId = new PlayerId(2);

        var currentViewerRawResponse = await sendRawRequestAsync(wsUri, new
        {
            requestId = 700004L,
            viewerPlayerNumericId = viewerOnePlayerId.Value,
            actionType = "debugResetMatch",
            payload = new
            {
                actorPlayerNumericId = viewerOnePlayerId.Value,
            },
        });

        var otherViewerRawResponse = await sendRawRequestAsync(wsUri, new
        {
            requestId = 700005L,
            viewerPlayerNumericId = viewerTwoPlayerId.Value,
            actionType = "debugResetMatch",
            payload = new
            {
                actorPlayerNumericId = viewerTwoPlayerId.Value,
            },
        });

        using var currentViewerDocument = JsonDocument.Parse(currentViewerRawResponse);
        using var otherViewerDocument = JsonDocument.Parse(otherViewerRawResponse);

        var currentViewerRoot = currentViewerDocument.RootElement;
        var otherViewerRoot = otherViewerDocument.RootElement;

        Assert.True(currentViewerRoot.GetProperty("isSucceeded").GetBoolean());
        Assert.True(otherViewerRoot.GetProperty("isSucceeded").GetBoolean());

        var currentViewerPlayers = currentViewerRoot.GetProperty("stateProjection").GetProperty("players");
        var otherViewerPlayers = otherViewerRoot.GetProperty("stateProjection").GetProperty("players");

        const long projectedPlayerId = 1;
        var currentViewerPlayer = currentViewerPlayers.EnumerateArray()
            .Single(player => player.GetProperty("playerNumericId").GetInt64() == projectedPlayerId);
        var otherViewerPlayer = otherViewerPlayers.EnumerateArray()
            .Single(player => player.GetProperty("playerNumericId").GetInt64() == projectedPlayerId);

        var currentViewerHandZone = currentViewerPlayer.GetProperty("handZone");
        var otherViewerHandZone = otherViewerPlayer.GetProperty("handZone");

        Assert.True(currentViewerHandZone.GetProperty("isContentVisible").GetBoolean());
        Assert.False(otherViewerHandZone.GetProperty("isContentVisible").GetBoolean());
        Assert.Equal(
            otherViewerHandZone.GetProperty("cardCount").GetInt32(),
            otherViewerHandZone.GetProperty("hiddenCardCount").GetInt32());
        Assert.Equal(0, otherViewerHandZone.GetProperty("cards").GetArrayLength());
    }

    [Fact]
    public async Task HostRuntime_WhenFourViewerConnectionsEstablished_ShouldRejectAdditionalDuplicateViewerConnection()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();

        using var viewerSocket1 = await connectViewerSocketAsync(wsUri, 1);
        using var viewerSocket2 = await connectViewerSocketAsync(wsUri, 2);
        using var viewerSocket3 = await connectViewerSocketAsync(wsUri, 3);
        using var viewerSocket4 = await connectViewerSocketAsync(wsUri, 4);

        var duplicateViewerSocket = new ClientWebSocket();
        var duplicateViewerUri = buildViewerScopedWsUri(wsUri, 1);
        var duplicateException = await Assert.ThrowsAnyAsync<Exception>(
            async () => await duplicateViewerSocket.ConnectAsync(duplicateViewerUri, CancellationToken.None));
        Assert.NotNull(duplicateException);
        duplicateViewerSocket.Dispose();

        await closeSocketAsync(viewerSocket1);
        await closeSocketAsync(viewerSocket2);
        await closeSocketAsync(viewerSocket3);
        await closeSocketAsync(viewerSocket4);
    }

    [Fact]
    public async Task HostRuntime_WhenViewerQueryIsMissing_ShouldRejectWebSocketHandshake()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();

        using var webSocket = new ClientWebSocket();
        var missingQueryException = await Assert.ThrowsAnyAsync<Exception>(
            async () => await webSocket.ConnectAsync(wsUri, CancellationToken.None));
        Assert.NotNull(missingQueryException);
    }

    [Fact]
    public async Task HostRuntime_WhenViewerConnects_ShouldReceiveInitialSnapshotPush()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();

        using var webSocket = new ClientWebSocket();
        var viewerScopedWsUri = buildViewerScopedWsUri(wsUri, 1);
        await webSocket.ConnectAsync(viewerScopedWsUri, CancellationToken.None);

        var initialSnapshotJson = await readTextMessageAsync(webSocket);
        var initialSnapshot = JsonSerializer.Deserialize<ServerSocketResponseEnvelope>(
            initialSnapshotJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        Assert.NotNull(initialSnapshot);
        Assert.True(initialSnapshot!.isSucceeded);
        Assert.Equal(0, initialSnapshot.requestId);
        Assert.Equal(1L, initialSnapshot.viewerPlayerNumericId);
        Assert.NotNull(initialSnapshot.stateProjection);
        Assert.NotNull(initialSnapshot.stateProjection!.turn);
        Assert.True(initialSnapshot.stateProjection.turn!.currentPlayerNumericId > 0);
        Assert.NotNull(initialSnapshot.stateProjection.publicZones);
        Assert.True(initialSnapshot.stateProjection.publicZones!.summonZone.cardCount >= 0);
        Assert.True(initialSnapshot.stateProjection.publicZones.sakuraCakeDeckZone.cardCount >= 0);
        Assert.Empty(initialSnapshot.eventLog);

        await closeSocketAsync(webSocket);
    }

    [Fact]
    public async Task RouteMessage_WhenRequesterSucceeds_ShouldBroadcastViewerScopedProjectionToPeerConnections()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var actorPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;

        using var viewerSocket1 = await connectViewerSocketAsync(wsUri, 1);
        using var viewerSocket2 = await connectViewerSocketAsync(wsUri, 2);

        var requestJson = JsonSerializer.Serialize(new
        {
            requestId = 700028L,
            viewerPlayerNumericId = 1L,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
            },
        });
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await viewerSocket1.SendAsync(
            new ArraySegment<byte>(requestBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var requesterResponseJson = await readTextMessageAsync(viewerSocket1);
        var peerBroadcastJson = await readTextMessageAsync(viewerSocket2);

        using var requesterResponseDocument = JsonDocument.Parse(requesterResponseJson);
        using var peerBroadcastDocument = JsonDocument.Parse(peerBroadcastJson);

        Assert.True(requesterResponseDocument.RootElement.GetProperty("isSucceeded").GetBoolean());
        Assert.Equal(1L, requesterResponseDocument.RootElement.GetProperty("viewerPlayerNumericId").GetInt64());

        Assert.True(peerBroadcastDocument.RootElement.GetProperty("isSucceeded").GetBoolean());
        Assert.Equal(2L, peerBroadcastDocument.RootElement.GetProperty("viewerPlayerNumericId").GetInt64());
        Assert.Equal(
            requesterResponseDocument.RootElement.GetProperty("requestId").GetInt64(),
            peerBroadcastDocument.RootElement.GetProperty("requestId").GetInt64());

        var peerPlayers = peerBroadcastDocument.RootElement.GetProperty("stateProjection").GetProperty("players");
        var actorProjectionForPeer = peerPlayers.EnumerateArray()
            .Single(player => player.GetProperty("playerNumericId").GetInt64() == actorPlayerId.Value);
        var actorHandZoneForPeer = actorProjectionForPeer.GetProperty("handZone");
        Assert.False(actorHandZoneForPeer.GetProperty("isContentVisible").GetBoolean());
        Assert.Equal(0, actorHandZoneForPeer.GetProperty("cards").GetArrayLength());

        await closeSocketAsync(viewerSocket1);
        await closeSocketAsync(viewerSocket2);
    }

    private static int getHandCount(ServerGameSession session, PlayerId playerId)
    {
        var handZoneId = session.gameState.players[playerId].handZoneId;
        return session.gameState.zones[handZoneId].cardInstanceIds.Count;
    }

    private static (PlayerId sourcePlayerId, PlayerId defenderPlayerId, ResponseWindowId responseWindowId) prepareDamageResponseWindowForSubmitResponse(
        ServerGameSession session,
        long idBase)
    {
        var sourcePlayerId = session.gameState.turnState!.currentPlayerId;
        var defenderPlayerId = session.gameState.players.Keys.First(playerId => playerId != sourcePlayerId);
        var sourcePlayerState = session.gameState.players[sourcePlayerId];
        var defenderPlayerState = session.gameState.players[defenderPlayerId];
        var actionChainId = new ActionChainId(idBase);
        var responseWindowId = new ResponseWindowId(idBase + 1);

        session.gameState.currentActionChain = new ActionChainState
        {
            actionChainId = actionChainId,
            actorPlayerId = sourcePlayerId,
            pendingContinuationKey = "continuation:stagedResponseDamage",
            currentFrameIndex = 0,
            isCompleted = false,
        };

        session.gameState.currentResponseWindow = new ResponseWindowState
        {
            responseWindowId = responseWindowId,
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainId,
            pendingDamageTargetCharacterInstanceId = defenderPlayerState.activeCharacterInstanceId!.Value,
            pendingDamageBaseDamageValue = 2,
            pendingDamageSourcePlayerId = sourcePlayerId,
            pendingDamageSourceCharacterInstanceId = sourcePlayerState.activeCharacterInstanceId!.Value,
            pendingDamageTypeKey = "physical",
            pendingDamageResponseStageKey = "awaitDefense",
            pendingDamageDefenseDeclarationKey = null,
            pendingDamageDefenderPlayerId = defenderPlayerId,
            currentResponderPlayerId = defenderPlayerId,
        };

        return (sourcePlayerId, defenderPlayerId, responseWindowId);
    }

    private static InputContextId prepareInputContextForSubmitInputChoice(
        ServerGameSession session,
        PlayerId requiredPlayerId,
        long idBase,
        params string[] choiceKeys)
    {
        var actionChainId = new ActionChainId(idBase);
        var inputContextId = new InputContextId(idBase + 1);

        session.gameState.currentActionChain = new ActionChainState
        {
            actionChainId = actionChainId,
            actorPlayerId = requiredPlayerId,
            pendingContinuationKey = null,
            currentFrameIndex = 0,
            isCompleted = false,
        };

        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            inputTypeKey = "serverWsTest",
            contextKey = "ws:submitInputChoice",
            selectedChoiceKey = null,
        };
        foreach (var choiceKey in choiceKeys)
        {
            inputContextState.choiceKeys.Add(choiceKey);
        }

        session.gameState.currentInputContext = inputContextState;
        return inputContextId;
    }

    private static async Task<ServerSocketResponseEnvelope> sendRequestAsync(Uri wsUri, object requestEnvelope)
    {
        var requestJson = JsonSerializer.Serialize(requestEnvelope);
        var viewerPlayerNumericId = extractViewerPlayerNumericId(requestJson);
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(buildViewerScopedWsUri(wsUri, viewerPlayerNumericId), CancellationToken.None);
        await readAndValidateInitialSnapshotAsync(webSocket, viewerPlayerNumericId);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

        var responseJson = await readTextMessageAsync(webSocket);
        var response = JsonSerializer.Deserialize<ServerSocketResponseEnvelope>(
            responseJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        Assert.NotNull(response);

        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test-complete", CancellationToken.None);
        }

        return response!;
    }

    private static async Task<string> sendRawRequestAsync(Uri wsUri, object requestEnvelope)
    {
        var requestJson = JsonSerializer.Serialize(requestEnvelope);
        var viewerPlayerNumericId = extractViewerPlayerNumericId(requestJson);
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(buildViewerScopedWsUri(wsUri, viewerPlayerNumericId), CancellationToken.None);
        await readAndValidateInitialSnapshotAsync(webSocket, viewerPlayerNumericId);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

        var responseJson = await readTextMessageAsync(webSocket);

        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test-complete", CancellationToken.None);
        }

        return responseJson;
    }

    private static async Task<string> readTextMessageAsync(ClientWebSocket webSocket)
    {
        var buffer = new byte[4096];
        using var memoryStream = new MemoryStream();
        while (true)
        {
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("WebSocket closed before a response message was received.");
            }

            memoryStream.Write(buffer, 0, receiveResult.Count);
            if (receiveResult.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static async Task<ClientWebSocket> connectViewerSocketAsync(Uri wsUri, long viewerPlayerNumericId)
    {
        var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(buildViewerScopedWsUri(wsUri, viewerPlayerNumericId), CancellationToken.None);
        await readAndValidateInitialSnapshotAsync(webSocket, viewerPlayerNumericId);
        return webSocket;
    }

    private static async Task closeSocketAsync(ClientWebSocket webSocket)
    {
        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test-complete", CancellationToken.None);
        }
    }

    private static long extractViewerPlayerNumericId(string requestJson)
    {
        using var document = JsonDocument.Parse(requestJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("viewerPlayerNumericId", out var viewerPlayerNumericIdElement) ||
            !viewerPlayerNumericIdElement.TryGetInt64(out var viewerPlayerNumericId))
        {
            throw new InvalidOperationException("Request envelope must include numeric viewerPlayerNumericId.");
        }

        return viewerPlayerNumericId;
    }

    private static Uri buildViewerScopedWsUri(Uri wsUri, long viewerPlayerNumericId)
    {
        var separator = string.IsNullOrEmpty(wsUri.Query) ? "?" : "&";
        return new Uri(wsUri + separator + "viewerPlayerNumericId=" + viewerPlayerNumericId);
    }

    private static async Task readAndValidateInitialSnapshotAsync(ClientWebSocket webSocket, long viewerPlayerNumericId)
    {
        var initialSnapshotJson = await readTextMessageAsync(webSocket);
        var initialSnapshot = JsonSerializer.Deserialize<ServerSocketResponseEnvelope>(
            initialSnapshotJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        Assert.NotNull(initialSnapshot);
        Assert.Equal(0, initialSnapshot!.requestId);
        Assert.Equal(viewerPlayerNumericId, initialSnapshot.viewerPlayerNumericId);
        Assert.True(initialSnapshot.isSucceeded);
        Assert.NotNull(initialSnapshot.stateProjection);
    }
}
