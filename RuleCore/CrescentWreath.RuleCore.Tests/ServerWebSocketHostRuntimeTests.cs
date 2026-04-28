using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CrescentWreath.RuleCore.Ids;
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
        Assert.Contains("currentPlayerId", response.error.message, StringComparison.Ordinal);
        Assert.Equal(currentPlayerHandCountBefore, getHandCount(hostRuntime.gameSession, currentPlayerId));
    }

    [Fact]
    public async Task RouteMessage_WhenViewerDiffers_ShouldProjectHiddenOpponentHand()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var currentPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var otherPlayerId = hostRuntime.gameSession.gameState.players.Keys.First(playerId => playerId != currentPlayerId);

        var currentViewerRawResponse = await sendRawRequestAsync(wsUri, new
        {
            requestId = 700004L,
            viewerPlayerNumericId = currentPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = currentPlayerId.Value,
            },
        });

        var otherViewerRawResponse = await sendRawRequestAsync(wsUri, new
        {
            requestId = 700005L,
            viewerPlayerNumericId = otherPlayerId.Value,
            actionType = "drawOneCard",
            payload = new
            {
                actorPlayerNumericId = currentPlayerId.Value,
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

        var projectedPlayerId = currentViewerPlayers.EnumerateArray().First().GetProperty("playerNumericId").GetInt64();
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

    private static int getHandCount(ServerGameSession session, PlayerId playerId)
    {
        var handZoneId = session.gameState.players[playerId].handZoneId;
        return session.gameState.zones[handZoneId].cardInstanceIds.Count;
    }

    private static async Task<ServerSocketResponseEnvelope> sendRequestAsync(Uri wsUri, object requestEnvelope)
    {
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(wsUri, CancellationToken.None);
        var requestJson = JsonSerializer.Serialize(requestEnvelope);
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
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(wsUri, CancellationToken.None);
        var requestJson = JsonSerializer.Serialize(requestEnvelope);
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
}
