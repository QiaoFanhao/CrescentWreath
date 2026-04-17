using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;
using CrescentWreath.ServerPrototype;

namespace CrescentWreath.RuleCore.Tests;

public class ServerGameSessionTests
{
    [Fact]
    public void CreateStandard2v2_ShouldInitializeSingleSessionWithGameState()
    {
        var session = ServerGameSession.createStandard2v2();

        Assert.NotNull(session.gameState);
        Assert.Equal(RuleCore.GameState.MatchState.running, session.gameState.matchState);
        Assert.NotNull(session.gameState.turnState);
        Assert.Equal(4, session.gameState.players.Count);
    }

    [Fact]
    public void ProcessDrawOneCard_WhenRequestIsValid_ShouldReturnSuccessAndProducedEvents()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var handZoneState = session.gameState.zones[currentPlayerState.handZoneId];
        var deckZoneState = session.gameState.zones[currentPlayerState.deckZoneId];

        var handCountBefore = handZoneState.cardInstanceIds.Count;
        var deckCountBefore = deckZoneState.cardInstanceIds.Count;

        var result = session.processDrawOneCard(new ServerDrawOneCardRequestDto
        {
            requestId = 990001,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.NotEmpty(result.producedEvents);
        Assert.Contains(
            result.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.moveReason == CardMoveReason.draw);
        Assert.Equal(handCountBefore + 1, handZoneState.cardInstanceIds.Count);
        Assert.Equal(deckCountBefore - 1, deckZoneState.cardInstanceIds.Count);
    }

    [Fact]
    public void ProcessDrawOneCard_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var handZoneState = session.gameState.zones[currentPlayerState.handZoneId];
        var handCountBefore = handZoneState.cardInstanceIds.Count;

        var result = session.processDrawOneCard(new ServerDrawOneCardRequestDto
        {
            requestId = 990002,
            actorPlayerNumericId = 2,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "DrawOneCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(handCountBefore, handZoneState.cardInstanceIds.Count);
        Assert.Null(session.gameState.currentActionChain);
    }

    [Fact]
    public void ProcessPlayTreasureCard_WhenRequestIsValidNormal_ShouldReturnSuccessAndMoveCardToField()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var handZoneState = session.gameState.zones[currentPlayerState.handZoneId];
        var fieldZoneState = session.gameState.zones[currentPlayerState.fieldZoneId];
        var handCardInstanceId = handZoneState.cardInstanceIds[0];
        var handCardInstance = session.gameState.cardInstances[handCardInstanceId];
        var manaBefore = currentPlayerState.mana;
        var sigilPreviewBefore = currentPlayerState.sigilPreview;
        var expectedManaGain = TreasureResourceValueResolver.resolveManaGainOnEnterField(handCardInstance.definitionId);
        var expectedSigilPreviewGain = TreasureResourceValueResolver.resolveSigilPreviewGainOnEnterField(handCardInstance.definitionId);

        var result = session.processPlayTreasureCard(new ServerPlayTreasureCardRequestDto
        {
            requestId = 990101,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = handCardInstanceId.Value,
            playMode = "normal",
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Contains(
            result.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == handCardInstanceId &&
                         cardMovedEvent.moveReason == CardMoveReason.play);
        Assert.DoesNotContain(handCardInstanceId, handZoneState.cardInstanceIds);
        Assert.Contains(handCardInstanceId, fieldZoneState.cardInstanceIds);
        Assert.Equal(currentPlayerState.fieldZoneId, handCardInstance.zoneId);
        Assert.Equal(ZoneKey.field, handCardInstance.zoneKey);
        Assert.Equal(manaBefore + expectedManaGain, currentPlayerState.mana);
        Assert.Equal(sigilPreviewBefore + expectedSigilPreviewGain, currentPlayerState.sigilPreview);
    }

    [Fact]
    public void ProcessPlayTreasureCard_WhenPlayModeIsDefense_ShouldReturnFailureWithoutCallingRuleCore()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var handZoneState = session.gameState.zones[currentPlayerState.handZoneId];
        var fieldZoneState = session.gameState.zones[currentPlayerState.fieldZoneId];
        var cardInstanceId = handZoneState.cardInstanceIds[0];
        var handCountBefore = handZoneState.cardInstanceIds.Count;
        var fieldCountBefore = fieldZoneState.cardInstanceIds.Count;

        var result = session.processPlayTreasureCard(new ServerPlayTreasureCardRequestDto
        {
            requestId = 990102,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = cardInstanceId.Value,
            playMode = "defense",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal("ServerPlayTreasureCardRequestDto playMode must be normal.", result.errorMessage);
        Assert.Equal(handCountBefore, handZoneState.cardInstanceIds.Count);
        Assert.Equal(fieldCountBefore, fieldZoneState.cardInstanceIds.Count);
        Assert.Null(session.gameState.currentActionChain);
    }

    [Fact]
    public void ProcessPlayTreasureCard_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var handCardInstanceId = session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds[0];
        var handCountBefore = session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds.Count;

        var result = session.processPlayTreasureCard(new ServerPlayTreasureCardRequestDto
        {
            requestId = 990103,
            actorPlayerNumericId = otherPlayerId.Value,
            cardInstanceNumericId = handCardInstanceId.Value,
            playMode = "normal",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "PlayTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(handCountBefore, session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds.Count);
    }

    [Fact]
    public void ProcessPlayTreasureCard_WhenCardIsNotInActorHand_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var deckZoneState = session.gameState.zones[currentPlayerState.deckZoneId];
        var handZoneState = session.gameState.zones[currentPlayerState.handZoneId];
        var cardInstanceIdInDeck = deckZoneState.cardInstanceIds[0];
        var handCountBefore = handZoneState.cardInstanceIds.Count;
        var deckCountBefore = deckZoneState.cardInstanceIds.Count;

        var result = session.processPlayTreasureCard(new ServerPlayTreasureCardRequestDto
        {
            requestId = 990104,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = cardInstanceIdInDeck.Value,
            playMode = "normal",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "PlayTreasureCardActionRequest requires cardInstance.zoneId to equal actor player's handZoneId.",
            result.errorMessage);
        Assert.Equal(handCountBefore, handZoneState.cardInstanceIds.Count);
        Assert.Equal(deckCountBefore, deckZoneState.cardInstanceIds.Count);
        Assert.Contains(cardInstanceIdInDeck, deckZoneState.cardInstanceIds);
    }

    [Fact]
    public void ProcessEnterSummonPhase_WhenRequestIsValid_ShouldReturnSuccessAndLockSigil()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];

        currentPlayerState.sigilPreview = 3;
        currentPlayerState.lockedSigil = 0;
        currentPlayerState.isSigilLocked = false;
        var expectedLockedSigil = SigilSnapshotCalculator.recomputeSigilPreviewFromCurrentFieldState(session.gameState, currentPlayerState);

        var result = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990150,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);
        Assert.True(currentPlayerState.isSigilLocked);
        Assert.Equal(expectedLockedSigil, currentPlayerState.lockedSigil);
        Assert.Equal(0, currentPlayerState.sigilPreview);
    }

    [Fact]
    public void ProcessEnterSummonPhase_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var phaseBefore = session.gameState.turnState.currentPhase;
        var lockedSigilBefore = currentPlayerState.lockedSigil;
        var sigilPreviewBefore = currentPlayerState.sigilPreview;
        var isSigilLockedBefore = currentPlayerState.isSigilLocked;

        var result = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990151,
            actorPlayerNumericId = otherPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterSummonPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(phaseBefore, session.gameState.turnState.currentPhase);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
        Assert.Equal(sigilPreviewBefore, currentPlayerState.sigilPreview);
        Assert.Equal(isSigilLockedBefore, currentPlayerState.isSigilLocked);
    }

    [Fact]
    public void ProcessEnterSummonPhase_WhenCurrentPhaseIsNotAction_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        session.gameState.turnState.currentPhase = RuleCore.GameState.TurnPhase.summon;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var lockedSigilBefore = currentPlayerState.lockedSigil;

        var result = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990152,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterSummonPhaseActionRequest requires gameState.turnState.currentPhase to be action.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
    }

    [Fact]
    public void ProcessEnterSummonPhaseThenSummonTreasureCard_WhenRequestsAreValid_ShouldSucceedWithoutManualPhaseMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.sigilPreview = 2;

        var enterSummonResult = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990160,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(enterSummonResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);
        currentPlayerState.lockedSigil = 1;
        currentPlayerState.isSigilLocked = true;

        var summonZoneId = session.gameState.publicState!.summonZoneId;
        var summonedCardInstanceId = new CardInstanceId(991090);
        createPublicCardInZone(session.gameState, summonedCardInstanceId, "test-summon-card", summonZoneId, ZoneKey.summonZone);

        var summonResult = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990161,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = summonedCardInstanceId.Value,
        });

        Assert.True(summonResult.isSucceeded);
        Assert.Contains(
            summonResult.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == summonedCardInstanceId &&
                         cardMovedEvent.moveReason == CardMoveReason.summon);
    }
    [Fact]
    public void ProcessEnterEndPhase_WhenRequestIsValid_ShouldReturnSuccessAndAdvanceToEnd()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.mana = 4;
        currentPlayerState.lockedSigil = 2;
        currentPlayerState.isSigilLocked = true;

        var result = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990300,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);
        Assert.Equal(0, currentPlayerState.mana);
        Assert.Null(currentPlayerState.lockedSigil);
        Assert.False(currentPlayerState.isSigilLocked);
    }

    [Fact]
    public void ProcessEnterEndPhase_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var phaseBefore = session.gameState.turnState.currentPhase;
        var manaBefore = currentPlayerState.mana;

        var result = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990301,
            actorPlayerNumericId = otherPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterEndPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(phaseBefore, session.gameState.turnState.currentPhase);
        Assert.Equal(manaBefore, currentPlayerState.mana);
    }

    [Fact]
    public void ProcessEnterEndPhase_WhenCurrentPhaseIsInvalid_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        session.gameState.turnState.currentPhase = RuleCore.GameState.TurnPhase.end;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        var manaBefore = currentPlayerState.mana;

        var result = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990302,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterEndPhaseActionRequest requires gameState.turnState.currentPhase to be action or summon.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);
        Assert.Equal(manaBefore, currentPlayerState.mana);
    }

    [Fact]
    public void ProcessEnterSummonPhaseThenEnterEndPhase_WhenRequestsAreValid_ShouldSucceedWithoutManualPhaseMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var enterSummonResult = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990303,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterSummonResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990304,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(enterEndResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessStartNextTurn_WhenRequestIsValid_ShouldAdvanceTurnAndSwitchCurrentPlayer()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var turnNumberBefore = session.gameState.turnState.turnNumber;
        var seatOrder = session.gameState.matchMeta!.seatOrder;
        var currentSeatIndex = seatOrder.IndexOf(currentPlayerId);
        var expectedNextPlayerId = seatOrder[(currentSeatIndex + 1) % seatOrder.Count];

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990400,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990401,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(startNextTurnResult.isSucceeded);
        Assert.Null(startNextTurnResult.errorMessage);
        Assert.Same(session.gameState, startNextTurnResult.updatedState);
        Assert.Equal(turnNumberBefore + 1, session.gameState.turnState.turnNumber);
        Assert.Equal(expectedNextPlayerId, session.gameState.turnState.currentPlayerId);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessStartNextTurn_WhenDefensePlacedCardExistsOnNextPlayerField_ShouldReturnCardToHandAndClearFlag()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var seatOrder = session.gameState.matchMeta!.seatOrder;
        var currentSeatIndex = seatOrder.IndexOf(currentPlayerId);
        var nextPlayerId = seatOrder[(currentSeatIndex + 1) % seatOrder.Count];
        var nextPlayerState = session.gameState.players[nextPlayerId];
        var nextPlayerFieldZoneState = session.gameState.zones[nextPlayerState.fieldZoneId];
        var nextPlayerHandZoneState = session.gameState.zones[nextPlayerState.handZoneId];

        var defensePlacedCardInstanceId = new CardInstanceId(992001);
        var defensePlacedCardInstance = new CardInstance
        {
            cardInstanceId = defensePlacedCardInstanceId,
            definitionId = "test-defense-card",
            ownerPlayerId = nextPlayerId,
            zoneId = nextPlayerState.fieldZoneId,
            zoneKey = ZoneKey.field,
            isDefensePlacedOnField = true,
        };
        session.gameState.cardInstances.Add(defensePlacedCardInstanceId, defensePlacedCardInstance);
        nextPlayerFieldZoneState.cardInstanceIds.Add(defensePlacedCardInstanceId);
        var nextPlayerHandCountBefore = nextPlayerHandZoneState.cardInstanceIds.Count;

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990402,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990403,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(startNextTurnResult.isSucceeded);
        Assert.DoesNotContain(defensePlacedCardInstanceId, nextPlayerFieldZoneState.cardInstanceIds);
        Assert.Contains(defensePlacedCardInstanceId, nextPlayerHandZoneState.cardInstanceIds);
        Assert.Equal(nextPlayerState.handZoneId, defensePlacedCardInstance.zoneId);
        Assert.Equal(ZoneKey.hand, defensePlacedCardInstance.zoneKey);
        Assert.False(defensePlacedCardInstance.isDefensePlacedOnField);
        Assert.Equal(nextPlayerHandCountBefore + 1, nextPlayerHandZoneState.cardInstanceIds.Count);
        Assert.Contains(
            startNextTurnResult.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == defensePlacedCardInstanceId &&
                         cardMovedEvent.moveReason == CardMoveReason.returnToSource);
    }

    [Fact]
    public void ProcessStartNextTurn_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990404,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);

        var phaseBefore = session.gameState.turnState.currentPhase;
        var turnNumberBefore = session.gameState.turnState.turnNumber;

        var result = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990405,
            actorPlayerNumericId = otherPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "StartNextTurnActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(phaseBefore, session.gameState.turnState.currentPhase);
        Assert.Equal(turnNumberBefore, session.gameState.turnState.turnNumber);
    }

    [Fact]
    public void ProcessStartNextTurn_WhenCurrentPhaseIsNotEnd_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var turnNumberBefore = session.gameState.turnState.turnNumber;

        var result = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990406,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "StartNextTurnActionRequest requires gameState.turnState.currentPhase to be end.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);
        Assert.Equal(turnNumberBefore, session.gameState.turnState.turnNumber);
    }

    [Fact]
    public void ProcessEnterSummonPhaseThenEnterEndThenStartNextTurn_WhenRequestsAreValid_ShouldAdvanceTurnFlowWithoutManualMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var seatOrder = session.gameState.matchMeta!.seatOrder;
        var currentSeatIndex = seatOrder.IndexOf(currentPlayerId);
        var expectedNextPlayerId = seatOrder[(currentSeatIndex + 1) % seatOrder.Count];

        var enterSummonResult = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990407,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterSummonResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990408,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990409,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(startNextTurnResult.isSucceeded);
        Assert.Equal(expectedNextPlayerId, session.gameState.turnState.currentPlayerId);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessEnterActionPhase_WhenRequestIsValid_ShouldReturnSuccessAndAdvanceToAction()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.start;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var result = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990500,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);
        Assert.Equal(currentPlayerId, session.gameState.turnState.currentPlayerId);
    }

    [Fact]
    public void ProcessEnterActionPhase_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.start;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var phaseBefore = session.gameState.turnState.currentPhase;

        var result = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990501,
            actorPlayerNumericId = otherPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterActionPhaseActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(phaseBefore, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessEnterActionPhase_WhenCurrentPhaseIsNotStart_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var result = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990502,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "EnterActionPhaseActionRequest requires gameState.turnState.currentPhase to be start.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessStartNextTurnThenEnterActionThenEnterSummon_WhenRequestsAreValid_ShouldAdvancePhasesWithoutManualMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990503,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990504,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(startNextTurnResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);

        var nextPlayerId = session.gameState.turnState.currentPlayerId;
        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990505,
            actorPlayerNumericId = nextPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);

        var enterSummonResult = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = 990506,
            actorPlayerNumericId = nextPlayerId.Value,
        });

        Assert.True(enterSummonResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);
    }

    [Fact]
    public void ProcessUseSkill_WhenRequestIsValid_ShouldReturnSuccessAndApplyPenetrateStatus()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var actorPlayerState = session.gameState.players[currentPlayerId];
        var actorCharacterInstanceId = ensureActiveCharacterDefinitionId(session, currentPlayerId, "C004");
        actorPlayerState.mana = 5;
        actorPlayerState.skillPoint = 0;

        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990600,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);

        var result = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990601,
            actorPlayerNumericId = currentPlayerId.Value,
            characterInstanceNumericId = actorCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Contains(
            result.producedEvents,
            gameEvent =>
                gameEvent is StatusChangedEvent statusChangedEvent &&
                statusChangedEvent.statusKey == "Penetrate" &&
                statusChangedEvent.isApplied &&
                statusChangedEvent.targetPlayerId == currentPlayerId);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
        Assert.Contains(
            session.gameState.statusInstances,
            status => status.statusKey == "Penetrate" && status.targetPlayerId == currentPlayerId);
    }

    [Fact]
    public void ProcessUseSkill_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var actorPlayerState = session.gameState.players[currentPlayerId];
        var actorCharacterInstanceId = ensureActiveCharacterDefinitionId(session, currentPlayerId, "C004");
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);

        actorPlayerState.mana = 5;
        actorPlayerState.skillPoint = 0;
        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990602,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);

        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;

        var result = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990603,
            actorPlayerNumericId = otherPlayerId.Value,
            characterInstanceNumericId = actorCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "UseSkillActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
    }

    [Fact]
    public void ProcessUseSkill_WhenCurrentPhaseIsNotAction_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var actorPlayerState = session.gameState.players[currentPlayerId];
        var actorCharacterInstanceId = ensureActiveCharacterDefinitionId(session, currentPlayerId, "C004");
        actorPlayerState.mana = 5;
        actorPlayerState.skillPoint = 1;

        var result = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990604,
            actorPlayerNumericId = currentPlayerId.Value,
            characterInstanceNumericId = actorCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "UseSkillActionRequest requires gameState.turnState.currentPhase to be action.",
            result.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);
        Assert.Equal(5, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.skillPoint);
    }

    [Fact]
    public void ProcessUseSkill_WhenManaIsInsufficient_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var actorPlayerState = session.gameState.players[currentPlayerId];
        var actorCharacterInstanceId = ensureActiveCharacterDefinitionId(session, currentPlayerId, "C004");

        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990605,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);

        actorPlayerState.mana = 4;
        actorPlayerState.skillPoint = 1;

        var result = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990606,
            actorPlayerNumericId = currentPlayerId.Value,
            characterInstanceNumericId = actorCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "UseSkillActionRequest requires actor player mana to be sufficient for skill cost.",
            result.errorMessage);
        Assert.Equal(4, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.skillPoint);
    }

    [Fact]
    public void ProcessUseSkill_WhenCharacterOwnershipMismatch_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var actorPlayerState = session.gameState.players[currentPlayerId];
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var otherCharacterInstanceId = session.gameState.players[otherPlayerId].activeCharacterInstanceId!.Value;
        actorPlayerState.mana = 5;
        actorPlayerState.skillPoint = 0;

        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990607,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);

        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;

        var result = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990608,
            actorPlayerNumericId = currentPlayerId.Value,
            characterInstanceNumericId = otherCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "UseSkillActionRequest requires characterInstance.ownerPlayerId to equal actorPlayerId.",
            result.errorMessage);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.DoesNotContain(
            session.gameState.statusInstances,
            status => status.statusKey == "Penetrate" && status.targetPlayerId == currentPlayerId);
    }

    [Fact]
    public void ProcessStartNextTurnThenEnterActionThenUseSkill_WhenRequestsAreValid_ShouldSucceedWithoutManualPhaseMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990609,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.end, session.gameState.turnState.currentPhase);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990610,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(startNextTurnResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);

        var nextPlayerId = session.gameState.turnState.currentPlayerId;
        var nextPlayerState = session.gameState.players[nextPlayerId];
        var nextPlayerCharacterInstanceId = ensureActiveCharacterDefinitionId(session, nextPlayerId, "C004");
        nextPlayerState.mana = 5;
        nextPlayerState.skillPoint = 0;

        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990611,
            actorPlayerNumericId = nextPlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);

        var useSkillResult = session.processUseSkill(new ServerUseSkillRequestDto
        {
            requestId = 990612,
            actorPlayerNumericId = nextPlayerId.Value,
            characterInstanceNumericId = nextPlayerCharacterInstanceId.Value,
            skillKey = "C004:1",
        });

        Assert.True(useSkillResult.isSucceeded);
        Assert.Contains(
            useSkillResult.producedEvents,
            gameEvent =>
                gameEvent is StatusChangedEvent statusChangedEvent &&
                statusChangedEvent.statusKey == "Penetrate" &&
                statusChangedEvent.isApplied &&
                statusChangedEvent.targetPlayerId == nextPlayerId);
    }

    [Fact]
    public void ProcessSubmitDefense_WhenFixedReduce1IsValid_ShouldReturnSuccessWithoutCardMovement()
    {
        var session = ServerGameSession.createStandard2v2();
        var sourcePlayerId = session.gameState.turnState!.currentPlayerId;
        var defenderPlayerId = session.gameState.players.Keys.First(playerId => playerId != sourcePlayerId);
        prepareDamageResponseWindowForDefense(session, sourcePlayerId, defenderPlayerId, 993000);

        var result = session.processSubmitDefense(new ServerSubmitDefenseRequestDto
        {
            requestId = 990700,
            actorPlayerNumericId = defenderPlayerId.Value,
            defenseTypeKey = "fixedReduce1",
            defenseCardInstanceNumericId = 0,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.DoesNotContain(result.producedEvents, gameEvent => gameEvent is CardMovedEvent);
        Assert.NotNull(session.gameState.currentResponseWindow);
        Assert.Equal("awaitCounter", session.gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal("fixedReduce1", session.gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
        Assert.Equal(sourcePlayerId, session.gameState.currentResponseWindow.currentResponderPlayerId);
    }

    [Fact]
    public void ProcessSubmitDefense_WhenFormalDefenseIsValid_ShouldReturnSuccessAndMoveDefenseCardToField()
    {
        var session = ServerGameSession.createStandard2v2();
        var sourcePlayerId = session.gameState.turnState!.currentPlayerId;
        var defenderPlayerId = session.gameState.players.Keys.First(playerId => playerId != sourcePlayerId);
        var defenderPlayerState = session.gameState.players[defenderPlayerId];
        var defenseCardInstanceId = new CardInstanceId(993011);
        createCardInPlayerHand(session.gameState, defenderPlayerState, defenseCardInstanceId, "test:defensePhysical2");
        prepareDamageResponseWindowForDefense(session, sourcePlayerId, defenderPlayerId, 993010);

        var result = session.processSubmitDefense(new ServerSubmitDefenseRequestDto
        {
            requestId = 990701,
            actorPlayerNumericId = defenderPlayerId.Value,
            defenseTypeKey = "physical",
            defenseCardInstanceNumericId = defenseCardInstanceId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Contains(
            result.producedEvents,
            gameEvent =>
                gameEvent is CardMovedEvent cardMovedEvent &&
                cardMovedEvent.cardInstanceId == defenseCardInstanceId &&
                cardMovedEvent.moveReason == CardMoveReason.defensePlace &&
                cardMovedEvent.fromZoneKey == ZoneKey.hand &&
                cardMovedEvent.toZoneKey == ZoneKey.field);
        Assert.Equal(defenderPlayerState.fieldZoneId, session.gameState.cardInstances[defenseCardInstanceId].zoneId);
        Assert.True(session.gameState.cardInstances[defenseCardInstanceId].isDefensePlacedOnField);
        Assert.Equal("awaitCounter", session.gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.StartsWith("cardDefense:", session.gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
    }

    [Fact]
    public void ProcessSubmitDefense_WhenActorIsNotPendingDefender_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var sourcePlayerId = session.gameState.turnState!.currentPlayerId;
        var defenderPlayerId = session.gameState.players.Keys.First(playerId => playerId != sourcePlayerId);
        prepareDamageResponseWindowForDefense(session, sourcePlayerId, defenderPlayerId, 993020);

        var result = session.processSubmitDefense(new ServerSubmitDefenseRequestDto
        {
            requestId = 990702,
            actorPlayerNumericId = sourcePlayerId.Value,
            defenseTypeKey = "fixedReduce1",
            defenseCardInstanceNumericId = 0,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SubmitDefenseActionRequest actorPlayerId must equal pending damage defender player.",
            result.errorMessage);
        Assert.NotNull(session.gameState.currentResponseWindow);
        Assert.Equal("awaitDefense", session.gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Null(session.gameState.currentResponseWindow.pendingDamageDefenseDeclarationKey);
    }

    [Fact]
    public void ProcessSubmitDefense_WhenCurrentResponseWindowIsMissing_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;

        var result = session.processSubmitDefense(new ServerSubmitDefenseRequestDto
        {
            requestId = 990703,
            actorPlayerNumericId = currentPlayerId.Value,
            defenseTypeKey = "fixedReduce1",
            defenseCardInstanceNumericId = 0,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SubmitDefenseActionRequest requires an active currentActionChain.",
            result.errorMessage);
        Assert.Null(session.gameState.currentResponseWindow);
    }

    [Fact]
    public void ProcessStartNextTurnThenEnterActionThenSubmitDefense_WhenDefenseWindowPrepared_ShouldSucceedWithoutManualPhaseMutation()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState.currentPlayerId;

        var enterEndResult = session.processEnterEndPhase(new ServerEnterEndPhaseRequestDto
        {
            requestId = 990704,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(enterEndResult.isSucceeded);

        var startNextTurnResult = session.processStartNextTurn(new ServerStartNextTurnRequestDto
        {
            requestId = 990705,
            actorPlayerNumericId = currentPlayerId.Value,
        });
        Assert.True(startNextTurnResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.start, session.gameState.turnState.currentPhase);

        var sourcePlayerId = session.gameState.turnState.currentPlayerId;
        var enterActionResult = session.processEnterActionPhase(new ServerEnterActionPhaseRequestDto
        {
            requestId = 990706,
            actorPlayerNumericId = sourcePlayerId.Value,
        });
        Assert.True(enterActionResult.isSucceeded);
        Assert.Equal(RuleCore.GameState.TurnPhase.action, session.gameState.turnState.currentPhase);

        var defenderPlayerId = session.gameState.players.Keys.First(playerId => playerId != sourcePlayerId);
        prepareDamageResponseWindowForDefense(session, sourcePlayerId, defenderPlayerId, 993030);

        var submitDefenseResult = session.processSubmitDefense(new ServerSubmitDefenseRequestDto
        {
            requestId = 990707,
            actorPlayerNumericId = defenderPlayerId.Value,
            defenseTypeKey = "fixedReduce1",
            defenseCardInstanceNumericId = 0,
        });

        Assert.True(submitDefenseResult.isSucceeded);
        Assert.Equal("awaitCounter", session.gameState.currentResponseWindow!.pendingDamageResponseStageKey);
        Assert.Equal(sourcePlayerId, session.gameState.currentResponseWindow.currentResponderPlayerId);
    }

    [Fact]
    public void ProcessSummonTreasureCard_WhenRequestIsValid_ShouldReturnSuccessAndMoveCardToDiscard()
    {
        var session = ServerGameSession.createStandard2v2();
        enterSummonPhaseOrThrow(session, 990190);
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.isSigilLocked = true;
        currentPlayerState.lockedSigil = 1;
        currentPlayerState.sigilPreview = 5;

        var summonZoneId = session.gameState.publicState!.summonZoneId;
        var publicTreasureDeckZoneId = session.gameState.publicState.publicTreasureDeckZoneId;
        var summonZoneState = session.gameState.zones[summonZoneId];
        var publicTreasureDeckZoneState = session.gameState.zones[publicTreasureDeckZoneId];
        var discardZoneState = session.gameState.zones[currentPlayerState.discardZoneId];

        var summonedCardInstanceId = new CardInstanceId(991001);
        var refillCardInstanceId = new CardInstanceId(991002);

        createPublicCardInZone(session.gameState, summonedCardInstanceId, "test-summon-card", summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(session.gameState, refillCardInstanceId, "test-public-deck-card", publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);

        var result = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990201,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = summonedCardInstanceId.Value,
        });

        Assert.True(result.isSucceeded);
        Assert.Null(result.errorMessage);
        Assert.Same(session.gameState, result.updatedState);
        Assert.Contains(
            result.producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == summonedCardInstanceId &&
                         cardMovedEvent.moveReason == CardMoveReason.summon);
        Assert.DoesNotContain(summonedCardInstanceId, summonZoneState.cardInstanceIds);
        Assert.Contains(summonedCardInstanceId, discardZoneState.cardInstanceIds);
        Assert.Equal(currentPlayerState.discardZoneId, session.gameState.cardInstances[summonedCardInstanceId].zoneId);
        Assert.Equal(ZoneKey.discard, session.gameState.cardInstances[summonedCardInstanceId].zoneKey);
        Assert.Equal(0, currentPlayerState.lockedSigil);
        Assert.DoesNotContain(refillCardInstanceId, publicTreasureDeckZoneState.cardInstanceIds);
        Assert.Contains(refillCardInstanceId, summonZoneState.cardInstanceIds);
    }

    [Fact]
    public void ProcessSummonTreasureCard_WhenActorIsNotCurrentPlayer_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        enterSummonPhaseOrThrow(session, 990190);
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var otherPlayerId = session.gameState.players.Keys.First(playerId => playerId != currentPlayerId);
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.isSigilLocked = true;
        currentPlayerState.lockedSigil = 1;

        var summonZoneId = session.gameState.publicState!.summonZoneId;
        var summonedCardInstanceId = new CardInstanceId(991011);
        createPublicCardInZone(session.gameState, summonedCardInstanceId, "test-summon-card", summonZoneId, ZoneKey.summonZone);
        var lockedSigilBefore = currentPlayerState.lockedSigil;
        var discardCountBefore = session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count;

        var result = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990202,
            actorPlayerNumericId = otherPlayerId.Value,
            cardInstanceNumericId = summonedCardInstanceId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SummonTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.",
            result.errorMessage);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
        Assert.Equal(discardCountBefore, session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Contains(summonedCardInstanceId, session.gameState.zones[summonZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessSummonTreasureCard_WhenCurrentPhaseIsNotSummon_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.isSigilLocked = true;
        currentPlayerState.lockedSigil = 1;

        var summonZoneId = session.gameState.publicState!.summonZoneId;
        var summonedCardInstanceId = new CardInstanceId(991021);
        createPublicCardInZone(session.gameState, summonedCardInstanceId, "test-summon-card", summonZoneId, ZoneKey.summonZone);
        var lockedSigilBefore = currentPlayerState.lockedSigil;

        var result = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990203,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = summonedCardInstanceId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SummonTreasureCardActionRequest requires gameState.turnState.currentPhase to be summon.",
            result.errorMessage);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
        Assert.Contains(summonedCardInstanceId, session.gameState.zones[summonZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessSummonTreasureCard_WhenCardIsNotInSummonZone_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        enterSummonPhaseOrThrow(session, 990190);
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.isSigilLocked = true;
        currentPlayerState.lockedSigil = 1;

        var cardInHand = session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds[0];
        var handCountBefore = session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds.Count;
        var discardCountBefore = session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count;
        var lockedSigilBefore = currentPlayerState.lockedSigil;

        var result = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990204,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = cardInHand.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SummonTreasureCardActionRequest requires cardInstance.zoneId to equal gameState.publicState.summonZoneId.",
            result.errorMessage);
        Assert.Equal(handCountBefore, session.gameState.zones[currentPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(discardCountBefore, session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
    }

    [Fact]
    public void ProcessSummonTreasureCard_WhenLockedSigilIsInsufficient_ShouldReturnFailureAndKeepStateUnchanged()
    {
        var session = ServerGameSession.createStandard2v2();
        enterSummonPhaseOrThrow(session, 990190);
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var currentPlayerState = session.gameState.players[currentPlayerId];
        currentPlayerState.isSigilLocked = true;
        currentPlayerState.lockedSigil = 0;

        var summonZoneId = session.gameState.publicState!.summonZoneId;
        var summonedCardInstanceId = new CardInstanceId(991031);
        createPublicCardInZone(session.gameState, summonedCardInstanceId, "test-summon-card", summonZoneId, ZoneKey.summonZone);
        var discardCountBefore = session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count;
        var lockedSigilBefore = currentPlayerState.lockedSigil;

        var result = session.processSummonTreasureCard(new ServerSummonTreasureCardRequestDto
        {
            requestId = 990205,
            actorPlayerNumericId = currentPlayerId.Value,
            cardInstanceNumericId = summonedCardInstanceId.Value,
        });

        Assert.False(result.isSucceeded);
        Assert.Empty(result.producedEvents);
        Assert.Equal(
            "SummonTreasureCardActionRequest requires actor player lockedSigil to be sufficient for summon cost.",
            result.errorMessage);
        Assert.Equal(discardCountBefore, session.gameState.zones[currentPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Equal(lockedSigilBefore, currentPlayerState.lockedSigil);
        Assert.Contains(summonedCardInstanceId, session.gameState.zones[summonZoneId].cardInstanceIds);
    }

    private static void enterSummonPhaseOrThrow(ServerGameSession session, long requestId)
    {
        session.gameState.turnState!.currentPhase = RuleCore.GameState.TurnPhase.action;
        var currentPlayerId = session.gameState.turnState!.currentPlayerId;
        var enterSummonResult = session.processEnterSummonPhase(new ServerEnterSummonPhaseRequestDto
        {
            requestId = requestId,
            actorPlayerNumericId = currentPlayerId.Value,
        });

        Assert.True(enterSummonResult.isSucceeded);
        Assert.Null(enterSummonResult.errorMessage);
        Assert.Equal(RuleCore.GameState.TurnPhase.summon, session.gameState.turnState.currentPhase);
    }
    private static CharacterInstanceId ensureActiveCharacterDefinitionId(
        ServerGameSession session,
        PlayerId playerId,
        string definitionId)
    {
        var playerState = session.gameState.players[playerId];
        Assert.NotNull(playerState.activeCharacterInstanceId);
        var characterInstanceId = playerState.activeCharacterInstanceId!.Value;
        session.gameState.characterInstances[characterInstanceId].definitionId = definitionId;
        return characterInstanceId;
    }
    private static void createCardInPlayerHand(
        RuleCore.GameState.GameState gameState,
        RuleCore.GameState.PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        var handZoneState = gameState.zones[playerState.handZoneId];
        handZoneState.cardInstanceIds.Add(cardInstanceId);
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.handZoneId,
            zoneKey = ZoneKey.hand,
        };
    }

    private static void prepareDamageResponseWindowForDefense(
        ServerGameSession session,
        PlayerId sourcePlayerId,
        PlayerId defenderPlayerId,
        long idBase)
    {
        var sourcePlayerState = session.gameState.players[sourcePlayerId];
        var defenderPlayerState = session.gameState.players[defenderPlayerId];
        Assert.NotNull(sourcePlayerState.activeCharacterInstanceId);
        Assert.NotNull(defenderPlayerState.activeCharacterInstanceId);

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(idBase),
            actorPlayerId = sourcePlayerId,
            pendingContinuationKey = "continuation:stagedResponseDamage",
            isCompleted = false,
            currentFrameIndex = 0,
        };
        session.gameState.currentActionChain = actionChainState;

        session.gameState.currentResponseWindow = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(idBase + 1),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
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
    }

    private static void createPublicCardInZone(
        RuleCore.GameState.GameState gameState,
        CardInstanceId cardInstanceId,
        string definitionId,
        ZoneId zoneId,
        ZoneKey zoneKey)
    {
        var cardInstance = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = new PlayerId(0),
            zoneId = zoneId,
            zoneKey = zoneKey,
        };

        gameState.cardInstances.Add(cardInstanceId, cardInstance);
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
    }
}



















