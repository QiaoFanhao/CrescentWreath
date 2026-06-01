using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
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

    private static void addActiveCharacter(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        long characterNumericId,
        int currentHp,
        int maxHp)
    {
        var characterInstanceId = new CharacterInstanceId(characterNumericId);
        playerState.activeCharacterInstanceId = characterInstanceId;
        gameState.characterInstances[characterInstanceId] = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = "test:character",
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
