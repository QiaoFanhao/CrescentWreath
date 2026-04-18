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
    public async Task RouteMessage_WhenViewerDiffers_ShouldProjectHiddenOpponentHand()
    {
        await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
        var wsUri = await hostRuntime.startAsync();
        var currentPlayerId = hostRuntime.gameSession.gameState.turnState!.currentPlayerId;
        var otherPlayerId = hostRuntime.gameSession.gameState.players.Keys.First(playerId => playerId != currentPlayerId);

        var currentViewerResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700004L,
            viewerPlayerNumericId = currentPlayerId.Value,
            actionType = "unknownActionType",
            payload = new { },
        });

        var otherViewerResponse = await sendRequestAsync(wsUri, new
        {
            requestId = 700005L,
            viewerPlayerNumericId = otherPlayerId.Value,
            actionType = "unknownActionType",
            payload = new { },
        });

        Assert.NotNull(currentViewerResponse.stateProjection);
        Assert.NotNull(otherViewerResponse.stateProjection);
        var currentViewerCurrentPlayerProjection = currentViewerResponse.stateProjection!.players
            .Single(player => player.playerNumericId == currentPlayerId.Value);
        var otherViewerCurrentPlayerProjection = otherViewerResponse.stateProjection!.players
            .Single(player => player.playerNumericId == currentPlayerId.Value);

        Assert.True(currentViewerCurrentPlayerProjection.handZone.isContentVisible);
        Assert.False(otherViewerCurrentPlayerProjection.handZone.isContentVisible);
        Assert.Empty(otherViewerCurrentPlayerProjection.handZone.cards);
        Assert.Equal(otherViewerCurrentPlayerProjection.handZone.cardCount, otherViewerCurrentPlayerProjection.handZone.hiddenCardCount);
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
