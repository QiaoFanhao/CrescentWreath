using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class TreasureArrivalEffectRuntimeTests
{
    [Fact]
    public void T005ArrivalQueue_WhenTwoCardsEnterSummonZone_ShouldResolveInEntryOrderAfterAllPlayersDiscardEachCard()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7000);
        var player2State = createPlayerState(player2, new TeamId(2), 8000);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        var t005CardA = new CardInstanceId(91001);
        var t005CardB = new CardInstanceId(91002);
        createPublicCardInZone(gameState, t005CardA, "T005", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, t005CardB, "T005", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerHand(gameState, player1State, new CardInstanceId(91003), "T001");
        createCardInPlayerHand(gameState, player1State, new CardInstanceId(91004), "T002");
        createCardInPlayerHand(gameState, player2State, new CardInstanceId(91005), "T003");
        createCardInPlayerHand(gameState, player2State, new CardInstanceId(91006), "T004");

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97001),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 300000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97002,
            enteredSummonZoneCardInstanceIds: new[] { t005CardA, t005CardB });

        Assert.True(opened);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Null(gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(2, gameState.currentInputContext.requiredPlayerIds.Count);
        Assert.Contains(player1, gameState.currentInputContext.requiredPlayerIds);
        Assert.Contains(player2, gameState.currentInputContext.requiredPlayerIds);
        Assert.Equal(
            TreasureArrivalEffectRuntime.ContinuationKeyT005ArrivalAllPlayersDiscard1,
            actionChainState.pendingContinuationKey);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97003, actorPlayerId: player1);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(1, gameState.currentInputContext!.submittedPlayerIds.Count);
        Assert.Contains(player1, gameState.currentInputContext.submittedPlayerIds);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97004, actorPlayerId: player2);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(0, gameState.currentInputContext!.submittedPlayerIds.Count);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97005, actorPlayerId: player1);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(1, gameState.currentInputContext!.submittedPlayerIds.Count);
        Assert.Contains(player1, gameState.currentInputContext.submittedPlayerIds);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97006, actorPlayerId: player2);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Equal(0, gameState.zones[player1State.handZoneId].cardInstanceIds.Count);
        Assert.Equal(0, gameState.zones[player2State.handZoneId].cardInstanceIds.Count);
        Assert.Equal(2, gameState.zones[player1State.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(2, gameState.zones[player2State.discardZoneId].cardInstanceIds.Count);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.moveReason == CardMoveReason.discard);
    }

    [Fact]
    public void T005ArrivalQueue_WhenRequiredPlayerHasNoHandCards_ShouldStillRequireNoCardConfirmationInput()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7100);
        var player2State = createPlayerState(player2, new TeamId(2), 8100);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        createPublicCardInZone(gameState, new CardInstanceId(91101), "T005", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerHand(gameState, player1State, new CardInstanceId(91102), "T001");
        // player2 intentionally has no hand cards.

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97101),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 310000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97102,
            enteredSummonZoneCardInstanceIds: new[] { new CardInstanceId(91101) });
        Assert.True(opened);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(player1, gameState.currentInputContext!.requiredPlayerIds);
        Assert.Contains(player2, gameState.currentInputContext.requiredPlayerIds);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97103, actorPlayerId: player1);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Single(gameState.currentInputContext!.choiceKeysByRequiredPlayerNumericId[player2.Value]);
        Assert.Equal(
            TreasureArrivalEffectRuntime.ChoiceKeyDiscardNoCard,
            gameState.currentInputContext.choiceKeysByRequiredPlayerNumericId[player2.Value][0]);

        submitArrivalDiscard(runtime, gameState, actionChainState, requestId: 97104, actorPlayerId: player2);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Empty(gameState.zones[player1State.handZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player2State.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player1State.discardZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player2State.discardZoneId].cardInstanceIds);
    }

    [Fact]
    public void T005ArrivalQueue_WhenActorIsNotRequiredPlayer_ShouldRejectSubmission()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var outsider = new PlayerId(99);
        var player1State = createPlayerState(player1, new TeamId(1), 7200);
        var player2State = createPlayerState(player2, new TeamId(2), 8200);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        createPublicCardInZone(gameState, new CardInstanceId(91201), "T005", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerHand(gameState, player1State, new CardInstanceId(91202), "T001");
        createCardInPlayerHand(gameState, player2State, new CardInstanceId(91203), "T002");

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97201),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 320000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        Assert.True(runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97202,
            enteredSummonZoneCardInstanceIds: new[] { new CardInstanceId(91201) }));
        Assert.NotNull(gameState.currentInputContext);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            runtime.tryContinueOnSubmitInputChoice(
                gameState,
                actionChainState,
                gameState.currentInputContext!,
                new SubmitInputChoiceActionRequest
                {
                    requestId = 97203,
                    actorPlayerId = outsider,
                    inputContextId = gameState.currentInputContext!.inputContextId,
                    choiceKey = TreasureArrivalEffectRuntime.ChoiceKeyDiscardNoCard,
                }));

        Assert.Equal(
            "T005 arrival continuation requires submitInputChoiceActionRequest.actorPlayerId to be one of currentInputContext.requiredPlayerIds.",
            exception.Message);
    }

    [Fact]
    public void T010ArrivalQueue_WhenCardEntersSummonZone_ShouldDrawOneCardForAllPlayersWithoutOpeningInputContext()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7300);
        var player2State = createPlayerState(player2, new TeamId(2), 8300);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.deckZoneId, ZoneKey.deck, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.deckZoneId, ZoneKey.deck, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        var t010Card = new CardInstanceId(91301);
        createPublicCardInZone(gameState, t010Card, "T010", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        gameState.cardInstances[new CardInstanceId(91304)] = new CardInstance
        {
            cardInstanceId = new CardInstanceId(91304),
            definitionId = "T003",
            ownerPlayerId = player1,
            zoneId = player1State.deckZoneId,
            zoneKey = ZoneKey.deck,
        };
        gameState.zones[player1State.deckZoneId].cardInstanceIds.Add(new CardInstanceId(91304));
        gameState.cardInstances[new CardInstanceId(91305)] = new CardInstance
        {
            cardInstanceId = new CardInstanceId(91305),
            definitionId = "T004",
            ownerPlayerId = player2,
            zoneId = player2State.deckZoneId,
            zoneKey = ZoneKey.deck,
        };
        gameState.zones[player2State.deckZoneId].cardInstanceIds.Add(new CardInstanceId(91305));

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97301),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 330000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97302,
            enteredSummonZoneCardInstanceIds: new[] { t010Card });

        Assert.True(opened);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Single(gameState.zones[player1State.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player2State.handZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player1State.deckZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player2State.deckZoneId].cardInstanceIds);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.moveReason == CardMoveReason.draw);
    }

    [Fact]
    public void T014ArrivalQueue_WhenCardEntersSummonZone_ShouldHealAllPlayersByTwoWithoutOpeningInputContext()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7150);
        var player2State = createPlayerState(player2, new TeamId(2), 8150);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        createPublicCardInZone(gameState, new CardInstanceId(91301), "T014", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        addActiveCharacter(gameState, player1State, characterNumericId: 90301, currentHp: 1, maxHp: 4);
        addActiveCharacter(gameState, player2State, characterNumericId: 90302, currentHp: 3, maxHp: 4);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97301),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 330000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97302,
            enteredSummonZoneCardInstanceIds: new[] { new CardInstanceId(91301) });

        Assert.True(opened);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Equal(3, gameState.characterInstances[player1State.activeCharacterInstanceId!.Value].currentHp);
        Assert.Equal(4, gameState.characterInstances[player2State.activeCharacterInstanceId!.Value].currentHp);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == player1 &&
                         hpChangedEvent.delta == 2);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == player2 &&
                         hpChangedEvent.delta == 1);
    }

    [Fact]
    public void T029ArrivalQueue_WhenAyaExists_ShouldApplyBarrierToAyaAndDrawOneForOtherPlayers()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player3 = new PlayerId(3);
        var player1State = createPlayerState(player1, new TeamId(1), 7160);
        var player2State = createPlayerState(player2, new TeamId(2), 8160);
        var player3State = createPlayerState(player3, new TeamId(1), 9160);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.matchMeta.seatOrder.Add(player3);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.players[player3] = player3State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.deckZoneId, ZoneKey.deck, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player3State.deckZoneId, ZoneKey.deck, player3, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player3State.handZoneId, ZoneKey.hand, player3, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player3State.discardZoneId, ZoneKey.discard, player3, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        var t029CardInstanceId = new CardInstanceId(91311);
        createPublicCardInZone(gameState, t029CardInstanceId, "T029", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerDeck(gameState, player1State, new CardInstanceId(91312), "T001");
        createCardInPlayerDeck(gameState, player3State, new CardInstanceId(91313), "T002");
        addActiveCharacter(gameState, player2State, characterNumericId: 90312, currentHp: 4, maxHp: 4, definitionId: "C016");

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97311),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 331000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97312,
            enteredSummonZoneCardInstanceIds: new[] { t029CardInstanceId });

        Assert.True(opened);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, player2State.activeCharacterInstanceId!.Value, "Barrier"));
        Assert.Single(gameState.zones[player1State.handZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[player2State.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player3State.handZoneId].cardInstanceIds);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Barrier" &&
                         statusChangedEvent.targetCharacterInstanceId == player2State.activeCharacterInstanceId &&
                         statusChangedEvent.isApplied);
        Assert.Equal(
            2,
            actionChainState.producedEvents.Count(gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                                               cardMovedEvent.moveReason == CardMoveReason.draw));
    }

    [Fact]
    public void T029ArrivalQueue_WhenAyaDoesNotExist_ShouldStillDrawOneForAllPlayers()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player3 = new PlayerId(3);
        var player1State = createPlayerState(player1, new TeamId(1), 7170);
        var player2State = createPlayerState(player2, new TeamId(2), 8170);
        var player3State = createPlayerState(player3, new TeamId(1), 9170);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.matchMeta.seatOrder.Add(player3);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.players[player3] = player3State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.deckZoneId, ZoneKey.deck, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.deckZoneId, ZoneKey.deck, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player3State.deckZoneId, ZoneKey.deck, player3, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player3State.handZoneId, ZoneKey.hand, player3, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player3State.discardZoneId, ZoneKey.discard, player3, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);

        var t029CardInstanceId = new CardInstanceId(91321);
        createPublicCardInZone(gameState, t029CardInstanceId, "T029", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerDeck(gameState, player1State, new CardInstanceId(91322), "T001");
        createCardInPlayerDeck(gameState, player2State, new CardInstanceId(91323), "T002");
        createCardInPlayerDeck(gameState, player3State, new CardInstanceId(91324), "T003");
        addActiveCharacter(gameState, player1State, characterNumericId: 90321, currentHp: 4, maxHp: 4, definitionId: "C001");
        addActiveCharacter(gameState, player2State, characterNumericId: 90322, currentHp: 4, maxHp: 4, definitionId: "C005");
        addActiveCharacter(gameState, player3State, characterNumericId: 90323, currentHp: 4, maxHp: 4, definitionId: "C010");

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97321),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 332000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        var opened = runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97322,
            enteredSummonZoneCardInstanceIds: new[] { t029CardInstanceId });

        Assert.True(opened);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Single(gameState.zones[player1State.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player2State.handZoneId].cardInstanceIds);
        Assert.Single(gameState.zones[player3State.handZoneId].cardInstanceIds);
        Assert.Equal(
            3,
            actionChainState.producedEvents.Count(gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                                               cardMovedEvent.moveReason == CardMoveReason.draw));
        Assert.DoesNotContain(
            actionChainState.producedEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Barrier");
    }

    [Fact]
    public void T017ArrivalQueue_ShouldGrantLeylineToEachTeamAndForceFateStayNightAsCurrentAnomaly()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var team1 = new TeamId(1);
        var team2 = new TeamId(2);
        var player1State = createPlayerState(player1, team1, 7180);
        var player2State = createPlayerState(player2, team2, 8180);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = team1,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A001",
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.teams[team1] = new TeamState { teamId = team1, leyline = 2 };
        gameState.teams[team2] = new TeamState { teamId = team2, leyline = 5 };
        gameState.publicState = createPublicState();
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A002");
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A010");

        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        var t017CardInstanceId = new CardInstanceId(91331);
        createPublicCardInZone(gameState, t017CardInstanceId, "T017", gameState.publicState.summonZoneId, ZoneKey.summonZone);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97331),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 333000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        Assert.True(runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97332,
            enteredSummonZoneCardInstanceIds: new[] { t017CardInstanceId }));

        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Equal(3, gameState.teams[team1].leyline);
        Assert.Equal(6, gameState.teams[team2].leyline);
        Assert.Equal("A010", gameState.currentAnomalyState!.currentAnomalyDefinitionId);
        Assert.DoesNotContain("A010", gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal(9, gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count);
        Assert.Contains(
            actionChainState.producedEvents,
            gameEvent => gameEvent is AnomalyFlippedEvent anomalyFlippedEvent &&
                         anomalyFlippedEvent.anomalyDefinitionId == "A010");
    }

    [Fact]
    public void T020ArrivalQueue_ShouldForceAllAlivePlayersCurrentHpToOne()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7190);
        var player2State = createPlayerState(player2, new TeamId(2), 8190);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addActiveCharacter(gameState, player1State, characterNumericId: 90331, currentHp: 4, maxHp: 4);
        addActiveCharacter(gameState, player2State, characterNumericId: 90332, currentHp: 2, maxHp: 4);
        var t020CardInstanceId = new CardInstanceId(91341);
        createPublicCardInZone(gameState, t020CardInstanceId, "T020", gameState.publicState.summonZoneId, ZoneKey.summonZone);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97341),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 334000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        Assert.True(runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97342,
            enteredSummonZoneCardInstanceIds: new[] { t020CardInstanceId }));

        Assert.Equal(1, gameState.characterInstances[player1State.activeCharacterInstanceId!.Value].currentHp);
        Assert.Equal(1, gameState.characterInstances[player2State.activeCharacterInstanceId!.Value].currentHp);
        Assert.Equal(
            2,
            actionChainState.producedEvents.Count(gameEvent => gameEvent is HpChangedEvent));
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
    }

    [Fact]
    public void T018ArrivalQueue_ShouldOpenParallelOptionalBanishInputAndResolveAfterAllPlayersSubmit()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7400);
        var player2State = createPlayerState(player2, new TeamId(2), 8400);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.gapZoneId, ZoneKey.gapZone, null, ZonePublicOrPrivate.publicZone);

        createPublicCardInZone(gameState, new CardInstanceId(91401), "T018", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createCardInPlayerHand(gameState, player1State, new CardInstanceId(91402), "T001");
        createCardInPlayerHand(gameState, player2State, new CardInstanceId(91403), "T002");

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97401),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 340000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        Assert.True(runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97402,
            enteredSummonZoneCardInstanceIds: new[] { new CardInstanceId(91401) }));
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureArrivalEffectRuntime.ContinuationKeyT018ArrivalAllPlayersOptionalBanish1,
            actionChainState.pendingContinuationKey);
        Assert.Contains(player1, gameState.currentInputContext!.requiredPlayerIds);
        Assert.Contains(player2, gameState.currentInputContext.requiredPlayerIds);
        Assert.Contains(
            TreasureArrivalEffectRuntime.ChoiceKeyDeclineOptionalBanish,
            gameState.currentInputContext.choiceKeysByRequiredPlayerNumericId[player1.Value]);

        var firstSubmitResolved = runtime.tryContinueOnSubmitInputChoice(
            gameState,
            actionChainState,
            gameState.currentInputContext,
            new SubmitInputChoiceActionRequest
            {
                requestId = 97403,
                actorPlayerId = player1,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = $"{TreasureArrivalEffectRuntime.ChoiceKeyBanishCardPrefix}91402",
            });
        Assert.True(firstSubmitResolved);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(player1, gameState.currentInputContext!.submittedPlayerIds);
        Assert.Contains(new CardInstanceId(91402), gameState.zones[gameState.publicState.gapZoneId].cardInstanceIds);

        var secondSubmitResolved = runtime.tryContinueOnSubmitInputChoice(
            gameState,
            actionChainState,
            gameState.currentInputContext,
            new SubmitInputChoiceActionRequest
            {
                requestId = 97404,
                actorPlayerId = player2,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = TreasureArrivalEffectRuntime.ChoiceKeyDeclineOptionalBanish,
            });
        Assert.True(secondSubmitResolved);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Contains(new CardInstanceId(91403), gameState.zones[player2State.handZoneId].cardInstanceIds);
    }

    [Fact]
    public void T026ArrivalQueue_ShouldLetCurrentPlayerOptionallyBanishDiscardThenSummonZoneAndRefill()
    {
        var player1 = new PlayerId(1);
        var player2 = new PlayerId(2);
        var player1State = createPlayerState(player1, new TeamId(1), 7410);
        var player2State = createPlayerState(player2, new TeamId(2), 8410);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = player1,
                currentTeamId = player1State.teamId,
                currentPhase = TurnPhase.summon,
                phaseStepIndex = 0,
            },
        };
        gameState.matchMeta = new MatchMeta();
        gameState.matchMeta.seatOrder.Add(player1);
        gameState.matchMeta.seatOrder.Add(player2);
        gameState.players[player1] = player1State;
        gameState.players[player2] = player2State;
        gameState.publicState = createPublicState();

        addZone(gameState, player1State.handZoneId, ZoneKey.hand, player1, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player1State.discardZoneId, ZoneKey.discard, player1, ZonePublicOrPrivate.publicZone);
        addZone(gameState, player2State.handZoneId, ZoneKey.hand, player2, ZonePublicOrPrivate.privateZone);
        addZone(gameState, player2State.discardZoneId, ZoneKey.discard, player2, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.gapZoneId, ZoneKey.gapZone, null, ZonePublicOrPrivate.publicZone);

        var t026CardInstanceId = new CardInstanceId(91501);
        var discardCardInstanceId = new CardInstanceId(91502);
        var summonZoneCardInstanceId = new CardInstanceId(91503);
        var refillCardInstanceId = new CardInstanceId(91504);
        createPublicCardInZone(gameState, t026CardInstanceId, "T026", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, discardCardInstanceId, "T001", player1State.discardZoneId, ZoneKey.discard);
        createPublicCardInZone(gameState, summonZoneCardInstanceId, "T002", gameState.publicState.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, refillCardInstanceId, "T003", gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(97501),
            actorPlayerId = player1,
            isCompleted = false,
            currentFrameIndex = 0,
        };
        gameState.currentActionChain = actionChainState;

        var runtime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier: () => 350000 + actionChainState.producedEvents.Count + 1,
            zoneMovementService: new ZoneMovementService());

        Assert.True(runtime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId: 97502,
            enteredSummonZoneCardInstanceIds: new[] { t026CardInstanceId }));

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(TreasureArrivalEffectRuntime.ContinuationKeyT026ArrivalOptionalBanishDiscard, actionChainState.pendingContinuationKey);
        Assert.Contains(
            $"{TreasureArrivalEffectRuntime.ChoiceKeyBanishDiscardCardPrefix}{discardCardInstanceId.Value}",
            gameState.currentInputContext!.choiceKeys);

        Assert.True(runtime.tryContinueOnSubmitInputChoice(
            gameState,
            actionChainState,
            gameState.currentInputContext,
            new SubmitInputChoiceActionRequest
            {
                requestId = 97503,
                actorPlayerId = player1,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = $"{TreasureArrivalEffectRuntime.ChoiceKeyBanishDiscardCardPrefix}{discardCardInstanceId.Value}",
            }));

        Assert.Contains(discardCardInstanceId, gameState.zones[gameState.publicState.gapZoneId].cardInstanceIds);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(TreasureArrivalEffectRuntime.ContinuationKeyT026ArrivalOptionalBanishSummonZone, actionChainState.pendingContinuationKey);
        Assert.Contains(
            $"{TreasureArrivalEffectRuntime.ChoiceKeyBanishSummonZoneCardPrefix}{summonZoneCardInstanceId.Value}",
            gameState.currentInputContext!.choiceKeys);

        Assert.True(runtime.tryContinueOnSubmitInputChoice(
            gameState,
            actionChainState,
            gameState.currentInputContext,
            new SubmitInputChoiceActionRequest
            {
                requestId = 97504,
                actorPlayerId = player1,
                inputContextId = gameState.currentInputContext.inputContextId,
                choiceKey = $"{TreasureArrivalEffectRuntime.ChoiceKeyBanishSummonZoneCardPrefix}{summonZoneCardInstanceId.Value}",
            }));

        Assert.Null(gameState.currentInputContext);
        Assert.Null(actionChainState.pendingContinuationKey);
        Assert.Contains(summonZoneCardInstanceId, gameState.zones[gameState.publicState.gapZoneId].cardInstanceIds);
        Assert.Contains(refillCardInstanceId, gameState.zones[gameState.publicState.summonZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[gameState.publicState.publicTreasureDeckZoneId].cardInstanceIds);
    }

    private static void submitArrivalDiscard(
        TreasureArrivalEffectRuntime runtime,
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId)
    {
        Assert.NotNull(gameState.currentInputContext);
        var inputContextState = gameState.currentInputContext!;
        Assert.True(inputContextState.choiceKeysByRequiredPlayerNumericId.ContainsKey(actorPlayerId.Value));
        var choiceKey = inputContextState.choiceKeysByRequiredPlayerNumericId[actorPlayerId.Value][0];

        var resolved = runtime.tryContinueOnSubmitInputChoice(
            gameState,
            actionChainState,
            inputContextState,
            new SubmitInputChoiceActionRequest
            {
                requestId = requestId,
                actorPlayerId = actorPlayerId,
                inputContextId = inputContextState.inputContextId,
                choiceKey = choiceKey,
            });

        Assert.True(resolved);
    }

    private static PlayerState createPlayerState(PlayerId playerId, TeamId teamId, long zoneIdBase)
    {
        return new PlayerState
        {
            playerId = playerId,
            teamId = teamId,
            deckZoneId = new ZoneId(zoneIdBase),
            handZoneId = new ZoneId(zoneIdBase + 1),
            discardZoneId = new ZoneId(zoneIdBase + 2),
            fieldZoneId = new ZoneId(zoneIdBase + 3),
            characterSetAsideZoneId = new ZoneId(zoneIdBase + 4),
        };
    }

    private static PublicState createPublicState()
    {
        return new PublicState
        {
            publicTreasureDeckZoneId = new ZoneId(9001),
            anomalyDeckZoneId = new ZoneId(9002),
            sakuraCakeDeckZoneId = new ZoneId(9003),
            summonZoneId = new ZoneId(9004),
            gapZoneId = new ZoneId(9005),
        };
    }

    private static void addZone(
        RuleCore.GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey zoneType,
        PlayerId? ownerPlayerId,
        ZonePublicOrPrivate publicOrPrivate)
    {
        gameState.zones[zoneId] = new ZoneState
        {
            zoneId = zoneId,
            zoneType = zoneType,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = publicOrPrivate,
        };
    }

    private static void createCardInPlayerHand(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.handZoneId,
            zoneKey = ZoneKey.hand,
        };
        gameState.zones[playerState.handZoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static void createCardInPlayerDeck(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.deckZoneId,
            zoneKey = ZoneKey.deck,
        };
        gameState.zones[playerState.deckZoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static void createCardInPlayerDiscard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.discardZoneId,
            zoneKey = ZoneKey.discard,
        };
        gameState.zones[playerState.discardZoneId].cardInstanceIds.Add(cardInstanceId);
    }

    private static void addActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        long characterNumericId,
        int currentHp,
        int maxHp,
        string definitionId = "test:character")
    {
        var characterInstanceId = new CharacterInstanceId(characterNumericId);
        playerState.activeCharacterInstanceId = characterInstanceId;
        gameState.characterInstances[characterInstanceId] = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            currentHp = currentHp,
            maxHp = maxHp,
            isAlive = true,
            isInPlay = true,
        };
    }

    private static void createPublicCardInZone(
        RuleCore.GameState.GameState gameState,
        CardInstanceId cardInstanceId,
        string definitionId,
        ZoneId zoneId,
        ZoneKey zoneKey)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = new PlayerId(0),
            zoneId = zoneId,
            zoneKey = zoneKey,
        };
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
    }
}
