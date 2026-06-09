using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Tests;

public sealed class ExtraTurnRuntimeTests
{
    [Fact]
    public void GrantExtraTurn_WhenCalledTwiceInSameTurn_ShouldNotStack()
    {
        var playerId = new PlayerId(1);
        var gameState = createGameState(playerId);

        Assert.True(ExtraTurnRuntime.grantExtraTurn(gameState, playerId));
        Assert.False(ExtraTurnRuntime.grantExtraTurn(gameState, playerId));
        Assert.Equal(playerId, gameState.turnState!.extraTurnFlags.pendingExtraTurnForPlayerId);
    }

    [Fact]
    public void ResolveNextTurnPlayer_ShouldConsumeExtraTurnThenResumeNormalSeatOrder()
    {
        var currentPlayerId = new PlayerId(2);
        var normalNextPlayerId = new PlayerId(3);
        var gameState = createGameState(currentPlayerId, normalNextPlayerId);
        ExtraTurnRuntime.grantExtraTurn(gameState, currentPlayerId);

        var extraTurnPlayerId = ExtraTurnRuntime.resolveNextTurnPlayer(
            gameState.turnState!,
            normalNextPlayerId);

        Assert.Equal(currentPlayerId, extraTurnPlayerId);
        Assert.True(gameState.turnState!.extraTurnFlags.isCurrentTurnExtraTurn);
        Assert.Null(gameState.turnState.extraTurnFlags.pendingExtraTurnForPlayerId);

        var followingPlayerId = ExtraTurnRuntime.resolveNextTurnPlayer(
            gameState.turnState,
            normalNextPlayerId);

        Assert.Equal(normalNextPlayerId, followingPlayerId);
        Assert.False(gameState.turnState.extraTurnFlags.isCurrentTurnExtraTurn);
    }

    private static RuleCore.GameState.GameState createGameState(params PlayerId[] playerIds)
    {
        var gameState = new RuleCore.GameState.GameState
        {
            turnState = new TurnState
            {
                currentPlayerId = playerIds[0],
            },
        };

        foreach (var playerId in playerIds)
        {
            gameState.players[playerId] = new PlayerState
            {
                playerId = playerId,
            };
        }

        return gameState;
    }
}
