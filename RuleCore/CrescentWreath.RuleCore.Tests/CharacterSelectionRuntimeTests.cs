using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Initialization;

namespace CrescentWreath.RuleCore.Tests;

public sealed class CharacterSelectionRuntimeTests
{
    [Fact]
    public void StandardSelectionMatch_ShouldSelectInSeatOrderAndStartAfterFourthPlayer()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(
            requireCharacterSelection: true);
        var processor = new ActionRequestProcessor();
        var definitions = new[] { "C001", "C007", "C008", "C018" };

        Assert.Equal(MatchState.initializing, gameState.matchState);
        Assert.Equal(new PlayerId(1), gameState.characterSelectionState!.currentSelectingPlayerId);
        Assert.All(gameState.players.Values, player => Assert.Null(player.activeCharacterInstanceId));

        for (var index = 0; index < definitions.Length; index++)
        {
            var playerId = new PlayerId(index + 1);
            var events = processor.processActionRequest(
                gameState,
                new SubmitCharacterSelectionActionRequest
                {
                    requestId = 9000 + index,
                    actorPlayerId = playerId,
                    characterDefinitionId = definitions[index],
                });

            var selectedEvent = Assert.IsType<CharacterSelectedEvent>(Assert.Single(events));
            Assert.Equal(playerId, selectedEvent.playerId);
            Assert.Equal(definitions[index], selectedEvent.characterDefinitionId);
            Assert.Equal(definitions[index], gameState.characterInstances[
                gameState.players[playerId].activeCharacterInstanceId!.Value].definitionId);
        }

        Assert.Equal(MatchState.running, gameState.matchState);
        Assert.True(gameState.characterSelectionState.isCompleted);
        Assert.Null(gameState.characterSelectionState.currentSelectingPlayerId);
    }

    [Fact]
    public void SubmitSelection_WhenWrongPlayerDuplicateOrPlaceholder_ShouldRejectWithoutMutation()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(
            requireCharacterSelection: true);
        var processor = new ActionRequestProcessor();

        Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new SubmitCharacterSelectionActionRequest
            {
                requestId = 9100,
                actorPlayerId = new PlayerId(2),
                characterDefinitionId = "C007",
            }));
        Assert.Empty(gameState.characterInstances);

        processor.processActionRequest(
            gameState,
            new SubmitCharacterSelectionActionRequest
            {
                requestId = 9101,
                actorPlayerId = new PlayerId(1),
                characterDefinitionId = "C001",
            });

        Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new SubmitCharacterSelectionActionRequest
            {
                requestId = 9102,
                actorPlayerId = new PlayerId(2),
                characterDefinitionId = "C001",
            }));
        Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(
            gameState,
            new SubmitCharacterSelectionActionRequest
            {
                requestId = 9103,
                actorPlayerId = new PlayerId(2),
                characterDefinitionId = "C002",
            }));

        Assert.Single(gameState.characterInstances);
        Assert.Equal(new PlayerId(2), gameState.characterSelectionState!.currentSelectingPlayerId);
    }

    [Fact]
    public void GameplayAction_BeforeCharacterSelectionCompletes_ShouldReject()
    {
        var gameState = new GameInitializer().createStandard2v2MatchState(
            requireCharacterSelection: true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ActionRequestProcessor().processActionRequest(
                gameState,
                new DrawOneCardActionRequest
                {
                    requestId = 9200,
                    actorPlayerId = new PlayerId(1),
                }));

        Assert.Contains("before character selection completes", exception.Message);
    }
}
