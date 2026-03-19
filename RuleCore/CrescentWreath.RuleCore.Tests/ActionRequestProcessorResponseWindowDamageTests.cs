using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorResponseWindowDamageTests
{
    [Fact]
    public void ProcessDealDamageActionRequest_ShouldOpenAndCloseWindowThenResolveDamage()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var targetCharacterInstanceId = new CharacterInstanceId(2001);
        var requestId = 7001L;

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(
            sourcePlayerId,
            new PlayerState
            {
                playerId = sourcePlayerId,
                teamId = new TeamId(1),
                hp = 10,
                leyline = 0,
                killScore = 0,
            });
        var targetPlayerState = new PlayerState
        {
            playerId = targetPlayerId,
            teamId = new TeamId(2),
            hp = 10,
            leyline = 0,
            killScore = 0,
        };
        gameState.players.Add(targetPlayerId, targetPlayerState);

        gameState.characterInstances.Add(
            targetCharacterInstanceId,
            new CharacterInstance
            {
                characterInstanceId = targetCharacterInstanceId,
                definitionId = "target-character",
                ownerPlayerId = targetPlayerId,
                isAlive = true,
                isInPlay = true,
            });

        var previousActionChain = new ActionChainState
        {
            actionChainId = new ActionChainId(999),
        };
        gameState.currentActionChain = previousActionChain;

        var request = new DealDamageActionRequest
        {
            requestId = requestId,
            actorPlayerId = sourcePlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 3,
        };

        var processor = new ActionRequestProcessor();

        var producedEvents = processor.processActionRequest(gameState, request);

        Assert.NotNull(gameState.currentActionChain);
        Assert.NotSame(previousActionChain, gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.effectFrames.Count >= 1);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(7, targetPlayerState.hp);

        Assert.Equal(4, producedEvents.Count);

        var openedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[0]);
        Assert.True(openedEvent.isOpened);
        Assert.True(openedEvent.responseWindowId.HasValue);
        Assert.NotEqual(new ResponseWindowId(requestId), openedEvent.responseWindowId.Value);

        var closedEvent = Assert.IsType<InteractionWindowEvent>(producedEvents[1]);
        Assert.False(closedEvent.isOpened);
        Assert.Equal(openedEvent.responseWindowId, closedEvent.responseWindowId);

        Assert.IsType<DamageResolvedEvent>(producedEvents[2]);
        Assert.IsType<HpChangedEvent>(producedEvents[3]);
    }
}
