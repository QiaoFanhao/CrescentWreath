using System.Linq;
using System.Text.Json;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.ServerPrototype;

namespace CrescentWreath.RuleCore.Tests;

public class ServerSocketActionRouterTests
{
    [Fact]
    public void RouteMessage_SubmitCharacterSelection_ShouldUseExistingEnvelopeAndReturnProjection()
    {
        var session = ServerGameSession.createStandard2v2(requireCharacterSelection: true);
        var router = new ServerSocketActionRouter(session);
        var response = router.routeMessage(serializeEnvelope(new
        {
            requestId = 88001,
            viewerPlayerNumericId = 1,
            actionType = "submitCharacterSelection",
            payload = new
            {
                actorPlayerNumericId = 1,
                characterDefinitionId = "C001",
            },
        }));

        Assert.True(response.isSucceeded);
        Assert.Equal(1, response.stateProjection!.characterSelection!.selections.Count);
        Assert.Equal(2, response.stateProjection.characterSelection.currentSelectingPlayerNumericId);
    }

    [Fact]
    public void RouteMessage_WhenRequestEnvelopeIsInvalidJson_ShouldReturnInvalidRequestEnvelope()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);

        var response = router.routeMessage("{ invalid json }");

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidRequestEnvelope, response.error!.code);
    }

    [Fact]
    public void RouteMessage_WhenActionTypeIsUnknown_ShouldReturnUnsupportedActionType()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910001L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "unknownActionType",
            payload = new
            {
                actorPlayerNumericId,
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeUnsupportedActionType, response.error!.code);
    }

    [Fact]
    public void RouteMessage_WhenPayloadIsInvalid_ShouldReturnInvalidPayload()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910002L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId,
                inputContextNumericId = 100L,
                choiceKey = "",
                choiceKeys = Array.Empty<string>(),
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
    }

    [Fact]
    public void RouteMessage_WhenPayloadIsValidButRuleCoreRejects_ShouldReturnRequestRejected()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910003L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId,
                responseWindowNumericId = 1L,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal("request_rejected", response.error!.code);
    }

    [Fact]
    public void RouteMessageDetailed_WhenBoundActorDiffersFromPayloadActor_ShouldReturnRequestRejectedWithoutMutatingState()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var currentPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var otherPlayerNumericId = session.gameState.players.Keys.First(playerId => playerId.Value != currentPlayerNumericId).Value;
        var currentHandZoneId = session.gameState.players[new PlayerId(currentPlayerNumericId)].handZoneId;
        var handCountBefore = session.gameState.zones[currentHandZoneId].cardInstanceIds.Count;

        var routeOutcome = router.routeMessageDetailed(
            serializeEnvelope(new
            {
                requestId = 910030L,
                viewerPlayerNumericId = currentPlayerNumericId,
                actionType = "drawOneCard",
                payload = new
                {
                    actorPlayerNumericId = currentPlayerNumericId,
                },
            }),
            new ServerSocketRouteOptions
            {
                forcedViewerPlayerNumericId = otherPlayerNumericId,
                requiredActorPlayerNumericId = otherPlayerNumericId,
            });

        Assert.False(routeOutcome.responseEnvelope.isSucceeded);
        Assert.NotNull(routeOutcome.responseEnvelope.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeRequestRejected, routeOutcome.responseEnvelope.error!.code);
        Assert.Null(routeOutcome.actionResult);
        Assert.Equal(handCountBefore, session.gameState.zones[currentHandZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void RouteMessageDetailed_WhenForcedViewerIsProvided_ShouldUseForcedViewerProjection()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        const long forcedViewerPlayerNumericId = 2;

        var routeOutcome = router.routeMessageDetailed(
            serializeEnvelope(new
            {
                requestId = 910031L,
                viewerPlayerNumericId = 1L,
                actionType = "debugResetMatch",
                payload = new
                {
                    actorPlayerNumericId = forcedViewerPlayerNumericId,
                },
            }),
            new ServerSocketRouteOptions
            {
                forcedViewerPlayerNumericId = forcedViewerPlayerNumericId,
                requiredActorPlayerNumericId = forcedViewerPlayerNumericId,
            });

        Assert.True(routeOutcome.responseEnvelope.isSucceeded);
        Assert.Equal(forcedViewerPlayerNumericId, routeOutcome.responseEnvelope.viewerPlayerNumericId);
        Assert.Equal(forcedViewerPlayerNumericId, routeOutcome.resolvedViewerPlayerNumericId);
        Assert.NotNull(routeOutcome.actionResult);
    }

    [Fact]
    public void RouteMessage_WhenDebugOpenDamageResponseWindowIsValid_ShouldSucceed()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910004L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                actorPlayerNumericId,
                targetCharacterInstanceNumericId = 0L,
                baseDamageValue = 2,
                damageTypeKey = "physical",
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.True(response.isSucceeded);
        Assert.NotNull(response.interaction);
        Assert.NotNull(response.interaction!.responseWindow);
        Assert.True(response.interaction.responseWindow!.responseWindowNumericId > 0);
    }

    [Fact]
    public void RouteMessage_WhenTryResolveAnomalyPayloadIsValid_ShouldReachRuleCoreAndReturnProjection()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910104L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "tryResolveAnomaly",
            payload = new
            {
                actorPlayerNumericId,
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.NotNull(response.stateProjection);
        Assert.NotNull(response.stateProjection!.currentAnomaly);
        Assert.False(string.IsNullOrWhiteSpace(response.stateProjection.currentAnomaly!.definitionId));
        Assert.NotEqual(ServerSocketActionRouter.ErrorCodeUnsupportedActionType, response.error?.code);
        Assert.NotEqual(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error?.code);
    }

    [Fact]
    public void RouteMessage_WhenTryResolveAnomalyPayloadIsMissingActor_ShouldReturnInvalidPayload()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;
        var requestJson = serializeEnvelope(new
        {
            requestId = 910105L,
            viewerPlayerNumericId = actorPlayerNumericId,
            actionType = "tryResolveAnomaly",
            payload = new
            {
                targetPlayerNumericId = 2L,
            },
        });

        var response = router.routeMessage(requestJson);

        Assert.False(response.isSucceeded);
        Assert.NotNull(response.error);
        Assert.Equal(ServerSocketActionRouter.ErrorCodeInvalidPayload, response.error!.code);
    }

    [Fact]
    public void RouteMessage_WhenDebugWindowOpenedThenSubmitResponseNo_ShouldSucceedAndCloseWindow()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var sourcePlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;

        var openResponse = router.routeMessage(serializeEnvelope(new
        {
            requestId = 910005L,
            viewerPlayerNumericId = sourcePlayerNumericId,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                actorPlayerNumericId = sourcePlayerNumericId,
                targetCharacterInstanceNumericId = 0L,
                baseDamageValue = 2,
                damageTypeKey = "physical",
            },
        }));
        Assert.True(openResponse.isSucceeded);
        Assert.NotNull(openResponse.interaction);
        Assert.NotNull(openResponse.interaction!.responseWindow);
        var responseWindow = openResponse.interaction.responseWindow!;
        Assert.True(responseWindow.currentResponderPlayerNumericId.HasValue);

        var submitResponse = router.routeMessage(serializeEnvelope(new
        {
            requestId = 910006L,
            viewerPlayerNumericId = responseWindow.currentResponderPlayerNumericId!.Value,
            actionType = "submitResponse",
            payload = new
            {
                actorPlayerNumericId = responseWindow.currentResponderPlayerNumericId.Value,
                responseWindowNumericId = responseWindow.responseWindowNumericId,
                shouldRespond = false,
                responseKey = (string?)null,
            },
        }));

        Assert.True(submitResponse.isSucceeded);
        Assert.NotNull(submitResponse.interaction);
        Assert.Null(submitResponse.interaction!.responseWindow);
        Assert.Contains(
            submitResponse.eventLog,
            entry => string.Equals(entry.eventTypeKey, "damageResolved", StringComparison.Ordinal));
    }

    [Fact]
    public void RouteMessage_WhenSubmitDefenseUsesUnsupportedDeclaredType_ShouldStillSucceed()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var sourcePlayerNumericId = session.gameState.turnState!.currentPlayerId.Value;

        var openResponse = router.routeMessage(serializeEnvelope(new
        {
            requestId = 910009L,
            viewerPlayerNumericId = sourcePlayerNumericId,
            actionType = "debugOpenDamageResponseWindow",
            payload = new
            {
                actorPlayerNumericId = sourcePlayerNumericId,
                targetCharacterInstanceNumericId = 0L,
                baseDamageValue = 2,
                damageTypeKey = "physical",
            },
        }));
        Assert.True(openResponse.isSucceeded);
        Assert.NotNull(openResponse.interaction);
        Assert.NotNull(openResponse.interaction!.responseWindow);

        var responseWindow = openResponse.interaction.responseWindow!;
        Assert.True(responseWindow.currentResponderPlayerNumericId.HasValue);
        var defenderPlayerNumericId = responseWindow.currentResponderPlayerNumericId!.Value;
        var defenderPlayerId = new PlayerId(defenderPlayerNumericId);
        var defenderPlayerState = session.gameState.players[defenderPlayerId];
        var defenseCardInstanceId = new CardInstanceId(910099);
        session.gameState.zones[defenderPlayerState.handZoneId].cardInstanceIds.Add(defenseCardInstanceId);
        session.gameState.cardInstances[defenseCardInstanceId] = new CardInstance
        {
            cardInstanceId = defenseCardInstanceId,
            definitionId = "T002",
            ownerPlayerId = defenderPlayerId,
            zoneId = defenderPlayerState.handZoneId,
            zoneKey = RuleCore.Zones.ZoneKey.hand,
        };

        var submitDefense = router.routeMessage(serializeEnvelope(new
        {
            requestId = 910010L,
            viewerPlayerNumericId = defenderPlayerNumericId,
            actionType = "submitDefense",
            payload = new
            {
                actorPlayerNumericId = defenderPlayerNumericId,
                defenseTypeKey = "physical",
                defenseCardInstanceNumericId = defenseCardInstanceId.Value,
            },
        }));

        Assert.True(submitDefense.isSucceeded);
        Assert.Null(submitDefense.error);
        Assert.NotNull(submitDefense.eventLog);
        Assert.Contains(submitDefense.eventLog!, entry => entry.eventTypeKey == "damageResolved");
        Assert.Contains(submitDefense.eventLog!, entry => entry.eventTypeKey == "hpChanged");
        Assert.Equal(defenderPlayerState.fieldZoneId, session.gameState.cardInstances[defenseCardInstanceId].zoneId);
        Assert.True(session.gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField);
    }

    [Fact]
    public void RouteMessage_WhenSubmitInputChoiceIsValid_ShouldSucceedAndCloseInputContext()
    {
        var session = ServerGameSession.createStandard2v2();
        var router = new ServerSocketActionRouter(session);
        var actorPlayerId = session.gameState.turnState!.currentPlayerId;
        var inputContextId = prepareInputContextForSubmitInputChoice(session, actorPlayerId, 910100L, "confirm", "decline");

        var response = router.routeMessage(serializeEnvelope(new
        {
            requestId = 910007L,
            viewerPlayerNumericId = actorPlayerId.Value,
            actionType = "submitInputChoice",
            payload = new
            {
                actorPlayerNumericId = actorPlayerId.Value,
                inputContextNumericId = inputContextId.Value,
                choiceKey = "confirm",
                choiceKeys = Array.Empty<string>(),
            },
        }));

        Assert.True(response.isSucceeded);
        Assert.NotNull(response.interaction);
        Assert.Null(response.interaction!.inputContext);
    }

    private static string serializeEnvelope(object envelope)
    {
        return JsonSerializer.Serialize(envelope);
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
            inputTypeKey = "routerTest",
            contextKey = "router:submitInputChoice",
            selectedChoiceKey = null,
        };

        foreach (var choiceKey in choiceKeys)
        {
            inputContextState.choiceKeys.Add(choiceKey);
        }

        session.gameState.currentInputContext = inputContextState;
        return inputContextId;
    }
}
