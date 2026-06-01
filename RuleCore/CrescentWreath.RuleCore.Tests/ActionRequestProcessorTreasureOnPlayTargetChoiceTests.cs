using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorTreasureOnPlayTargetChoiceTests
{
    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T002_ShouldOpenInputContextAndHealSelectedTarget()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1000);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 2000);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5001, currentHp: 4, maxHp: 4);
        addPlayer(gameState, targetPlayerState, activeCharacterNumericId: 5002, currentHp: 2, maxHp: 4);
        var cardInstanceId = new CardInstanceId(9001);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "T002");

        var processor = new ActionRequestProcessor();

        var playEvents = processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9101,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT002OnPlayTargetHeal1,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("player:1", gameState.currentInputContext.choiceKeys);
        Assert.Contains("player:2", gameState.currentInputContext.choiceKeys);
        Assert.Equal(2, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.sigilPreview);
        Assert.Contains(playEvents, gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                                                 interactionWindowEvent.eventTypeKey == "inputContextOpened");

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9102,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        var targetCharacter = gameState.characterInstances[targetPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(3, targetCharacter.currentHp);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(2, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.sigilPreview);
        Assert.Contains(submitEvents, gameEvent => gameEvent is HpChangedEvent);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T002_WhenTargetIsFullHp_ShouldNotOverheal()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1100);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 2100);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5101, currentHp: 4, maxHp: 4);
        addPlayer(gameState, targetPlayerState, activeCharacterNumericId: 5102, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9002), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9111,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9002),
            playMode = "normal",
        });

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9112,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "player:2",
        });

        var targetCharacter = gameState.characterInstances[targetPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(4, targetCharacter.currentHp);
        Assert.DoesNotContain(submitEvents, gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                                                         hpChangedEvent.targetPlayerId == targetPlayerId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T002_WhenSubmitByWrongActor_ShouldThrowAndKeepInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var wrongActorPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1200);
        var wrongActorPlayerState = createPlayerState(wrongActorPlayerId, new TeamId(2), 2200);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5201, currentHp: 4, maxHp: 4);
        addPlayer(gameState, wrongActorPlayerState, activeCharacterNumericId: 5202, currentHp: 3, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9003), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9121,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9003),
            playMode = "normal",
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9122,
            actorPlayerId = wrongActorPlayerId,
            inputContextId = inputContextId,
            choiceKey = "player:2",
        }));

        Assert.Equal(
            "SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.",
            exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextId, gameState.currentInputContext!.inputContextId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T009_ShouldOnlyAllowOpponentTargetsAndApplySeal()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1300);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2300);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3300);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5301, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5302, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5303, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9004), "T009");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9131,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9004),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains("player:2", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain("player:1", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("player:3", gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9132,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        var enemyCharacterId = enemyPlayerState.activeCharacterInstanceId!.Value;
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, enemyCharacterId, "Seal"));
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.isApplied &&
                         statusChangedEvent.statusKey == "Seal" &&
                         statusChangedEvent.targetPlayerId == enemyPlayerId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T009_WhenSelectingAlly_ShouldThrowAndKeepInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1400);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2400);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3400);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5401, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5402, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5403, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9005), "T009");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9141,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9005),
            playMode = "normal",
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9142,
            actorPlayerId = actorPlayerId,
            inputContextId = inputContextId,
            choiceKey = "player:3",
        }));

        Assert.Equal("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextId, gameState.currentInputContext!.inputContextId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T014_ShouldOnlyAllowFriendlyTargetsAndApplyBarrier()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1410);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2410);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3410);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5411, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5412, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5413, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90051), "T014");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9145,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90051),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains("player:1", gameState.currentInputContext!.choiceKeys);
        Assert.Contains("player:3", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("player:2", gameState.currentInputContext.choiceKeys);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT014OnPlayTargetFriendlyBarrier,
            gameState.currentActionChain!.pendingContinuationKey);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9146,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:3",
        });

        var allyCharacterId = allyPlayerState.activeCharacterInstanceId!.Value;
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, allyCharacterId, "Barrier"));
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.isApplied &&
                         statusChangedEvent.statusKey == "Barrier" &&
                         statusChangedEvent.targetPlayerId == allyPlayerId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T010_WhenSelectedDiscardCardSummonCostIsLessThan5_ShouldAllowOptionalMoveToHand()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1462);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2462);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5463, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6463, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90060), "T010");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(90061), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9149,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90060),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT010OnPlaySelectDiscardToDeckTop,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix}90061", gameState.currentInputContext!.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9150,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix}90061",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT010OnPlayOptionalMoveSelectedToHand,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyAcceptMoveToHand, gameState.currentInputContext!.choiceKeys);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyDeclineMoveToHand, gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9151,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = TreasureOnPlayEffectRuntime.ChoiceKeyAcceptMoveToHand,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(new CardInstanceId(90061), gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(new CardInstanceId(90061), gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.DoesNotContain(new CardInstanceId(90061), gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T018_ShouldGainSkillPointAndBanishSelectedCardFromHandOrDiscard()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1464);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2464);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5465, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6465, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9955);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90062), "T018");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(90063), "T001");
        var skillPointBefore = actorPlayerState.skillPoint;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9152,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90062),
            playMode = "normal",
        });

        Assert.Equal(skillPointBefore + 1, actorPlayerState.skillPoint);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT018OnPlayGainSkillPointThenBanish1,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}90063", gameState.currentInputContext.choiceKeys);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOptionalBanish, gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9153,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}90063",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.DoesNotContain(new CardInstanceId(90063), gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(new CardInstanceId(90063), gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == new CardInstanceId(90063) &&
                         cardMovedEvent.moveReason == CardMoveReason.banish);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T018_WhenDeclineBanish_ShouldKeepCardsInHandOrDiscard()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1465);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2465);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5466, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6466, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9956);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90065), "T018");
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90066), "T001");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(90067), "T002");
        var handBefore = gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count;
        var discardBefore = gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 91535,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90065),
            playMode = "normal",
        });

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 91536,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOptionalBanish,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(handBefore - 1, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(discardBefore, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Empty(gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.DoesNotContain(
            submitEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.moveReason == CardMoveReason.banish);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T019_ShouldOpenOpponentTargetInputAndThenOpenSpellDamageResponseWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1466);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2466);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3466);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5467, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6467, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 7467, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90064), "T019");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9154,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90064),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains($"player:{enemyPlayerId.Value}", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain($"player:{actorPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"player:{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);

        var hpBefore = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp;
        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9155,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("damageResponse", gameState.currentResponseWindow!.windowTypeKey);
        Assert.Equal("awaitDefense", gameState.currentResponseWindow.pendingDamageResponseStageKey);
        Assert.Equal(enemyPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(2, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal("spell", gameState.currentResponseWindow.pendingDamageTypeKey);
        Assert.Equal(hpBefore, gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp);
        Assert.Equal("continuation:stagedResponseDamage", gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "responseWindowOpened" &&
                         interactionWindowEvent.windowKindKey == "responseWindow");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T005_ShouldGainSkillPointThenRequireSelectedOpponentDiscardOneCard()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1450);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2450);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3450);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5451, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5452, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5453, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90055), "T005");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(90056), "T002");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(90057), "T001");
        var skillPointBefore = actorPlayerState.skillPoint;
        var enemyHandCountBefore = gameState.zones[enemyPlayerState.handZoneId].cardInstanceIds.Count;
        var enemyDiscardCountBefore = gameState.zones[enemyPlayerState.discardZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9143,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90055),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Contains($"player:{enemyPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"player:{actorPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"player:{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Equal(skillPointBefore + 1, actorPlayerState.skillPoint);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT005OnPlaySelectTargetOpponentDiscard,
            gameState.currentActionChain!.pendingContinuationKey);

        var firstSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9144,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(enemyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.All(
            gameState.currentInputContext.choiceKeys,
            choiceKey => Assert.StartsWith(TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix, choiceKey, StringComparison.Ordinal));
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT005OnPlayTargetOpponentDiscardCard,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            firstSubmitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "inputContextOpened");

        var discardChoiceKey = gameState.currentInputContext.choiceKeys[0];
        var secondSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9145,
            actorPlayerId = enemyPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = discardChoiceKey,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(skillPointBefore + 1, actorPlayerState.skillPoint);
        Assert.Equal(enemyHandCountBefore - 1, gameState.zones[enemyPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(enemyDiscardCountBefore + 1, gameState.zones[enemyPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Contains(secondSubmitEvents, gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                                         cardMovedEvent.moveReason == CardMoveReason.discard);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T005_WhenTargetDiscardStepSubmittedByWrongActor_ShouldThrowAndKeepTargetInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1460);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2460);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5461, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5462, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90058), "T005");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(90059), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9146,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90058),
            playMode = "normal",
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9147,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        var targetInputContextId = gameState.currentInputContext!.inputContextId;
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9148,
            actorPlayerId = actorPlayerId,
            inputContextId = targetInputContextId,
            choiceKey = gameState.currentInputContext.choiceKeys[0],
        }));

        Assert.Equal(
            "SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.",
            exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(targetInputContextId, gameState.currentInputContext!.inputContextId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T008_ShouldOnlyAllowOpponentTargetsAndDealDirectDamageWithoutResponseWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1500);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2500);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3500);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5501, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5502, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5503, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9006), "T008");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9151,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9006),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT008OnPlayTargetDirectDamage1,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains($"player:{enemyPlayerId.Value}", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain($"player:{actorPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"player:{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9152,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        var targetCharacter = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(3, targetCharacter.currentHp);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Contains(submitEvents, gameEvent => gameEvent is DamageResolvedEvent);
        Assert.Contains(submitEvents, gameEvent => gameEvent is HpChangedEvent);
        Assert.DoesNotContain(
            submitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.windowKindKey == "responseWindow");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T008_WhenSelectingAlly_ShouldThrowAndKeepInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1600);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2600);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3600);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5601, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5602, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5603, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9007), "T008");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9161,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9007),
            playMode = "normal",
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9162,
            actorPlayerId = actorPlayerId,
            inputContextId = inputContextId,
            choiceKey = $"player:{allyPlayerId.Value}",
        }));

        Assert.Equal("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextId, gameState.currentInputContext!.inputContextId);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T027_ShouldDrawTwoThenRequireTwoDiscardsViaInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1700);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2700);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5701, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5702, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9008), "T027");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9009), "T001");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9010), "T006");

        var processor = new ActionRequestProcessor();
        var playEvents = processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9171,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9008),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT027OnPlayDraw2Discard2Step1,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(2, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.sigilPreview);
        Assert.Equal(2, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.All(
            gameState.currentInputContext!.choiceKeys,
            choiceKey => Assert.StartsWith(TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix, choiceKey, StringComparison.Ordinal));
        Assert.Contains(playEvents, gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                                                 interactionWindowEvent.eventTypeKey == "inputContextOpened");

        var firstDiscardChoiceKey = gameState.currentInputContext.choiceKeys[0];
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9172,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = firstDiscardChoiceKey,
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT027OnPlayDraw2Discard2Step2,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Single(gameState.currentInputContext!.choiceKeys);
        Assert.Equal(1, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(1, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);

        var secondDiscardChoiceKey = gameState.currentInputContext.choiceKeys[0];
        var secondDiscardEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9173,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = secondDiscardChoiceKey,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(0, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(2, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Contains(secondDiscardEvents, gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                                                          interactionWindowEvent.eventTypeKey == "inputContextClosed");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T022_WhenDecliningOptionalBanish_ShouldApplySelfShackleOnly()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1800);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2800);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5801, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5802, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9805);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9011), "T022");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(9012), "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9181,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9011),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOptionalBanish, gameState.currentInputContext!.choiceKeys);
        Assert.Contains(
            gameState.currentInputContext.choiceKeys,
            choiceKey => choiceKey == $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9012");

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9182,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOptionalBanish,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, actorPlayerState.activeCharacterInstanceId!.Value, "Shackle"));
        Assert.Empty(gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Shackle" &&
                         statusChangedEvent.isApplied);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T022_WhenSelectingBanishCard_ShouldMoveCardToGapAndApplySelfShackle()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1900);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2900);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5901, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5902, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9905);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9013), "T022");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(9014), "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9191,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9013),
            playMode = "normal",
        });

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9192,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9014",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.DoesNotContain(new CardInstanceId(9014), gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(new CardInstanceId(9014), gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Equal(gameState.publicState.gapZoneId, gameState.cardInstances[new CardInstanceId(9014)].zoneId);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, actorPlayerState.activeCharacterInstanceId!.Value, "Shackle"));
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.moveReason == CardMoveReason.banish &&
                         cardMovedEvent.cardInstanceId == new CardInstanceId(9014));
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Shackle" &&
                         statusChangedEvent.isApplied);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T001_ShouldSilenceOpponentAndDrawOneCard()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2000);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3000);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 4000);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6001, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6002, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 6003, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9015), "T001");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9016), "T006");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9201,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9015),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains($"player:{enemyPlayerId.Value}", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain($"player:{actorPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"player:{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9202,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, enemyPlayerId, "Silence"));
        Assert.Single(gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Silence" &&
                         statusChangedEvent.targetPlayerId == enemyPlayerId &&
                         statusChangedEvent.isApplied);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T003_ShouldSupportOptionalBanishThenOpenPhysicalDamageResponseWindowForOpponent()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2100);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3100);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6101, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6102, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9915);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9017), "T003");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(9018), "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9211,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9017),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT003OnPlayOptionalBanishStep,
            gameState.currentActionChain!.pendingContinuationKey);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9212,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9018",
        });

        Assert.Contains(new CardInstanceId(9018), gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains($"player:{enemyPlayerId.Value}", gameState.currentInputContext!.choiceKeys);

        var hpBefore = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp;
        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9213,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("damageResponse", gameState.currentResponseWindow!.windowTypeKey);
        Assert.Equal("awaitDefense", gameState.currentResponseWindow.pendingDamageResponseStageKey);
        Assert.Equal(enemyPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(hpBefore, gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId.Value].currentHp);
        Assert.Equal(
            "continuation:stagedResponseDamage",
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "responseWindowOpened" &&
                         interactionWindowEvent.windowKindKey == "responseWindow");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T004_ShouldRemoveShackleOrSealThenRequireSecondTargetSelectionAndOpenResponseWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2200);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3200);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6201, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6202, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9019), "T004");

        var enemyCharacterInstanceId = enemyPlayerState.activeCharacterInstanceId!.Value;
        StatusRuntime.applyStatus(gameState, new StatusInstance
        {
            statusKey = "Shackle",
            targetCharacterInstanceId = enemyCharacterInstanceId,
            stackCount = 1,
        });

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9221,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9019),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep,
            gameState.currentActionChain!.pendingContinuationKey);
        var hpBefore = gameState.characterInstances[enemyCharacterInstanceId].currentHp;
        var firstSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9222,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, enemyCharacterInstanceId, "Shackle"));
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            firstSubmitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Shackle" &&
                         !statusChangedEvent.isApplied);

        var secondSubmitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9223,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("damageResponse", gameState.currentResponseWindow!.windowTypeKey);
        Assert.Equal("awaitDefense", gameState.currentResponseWindow.pendingDamageResponseStageKey);
        Assert.Equal(enemyPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(hpBefore, gameState.characterInstances[enemyCharacterInstanceId].currentHp);
        Assert.Equal(
            "continuation:stagedResponseDamage",
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            secondSubmitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "responseWindowOpened" &&
                         interactionWindowEvent.windowKindKey == "responseWindow");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T006_ShouldDealSelfDirectDamageAndOptionallyBanishSummonCard()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2300);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3300);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6301, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6302, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9925);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9020), "T006");
        createPublicCardInZone(gameState, new CardInstanceId(9021), "T001", gameState.publicState!.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, new CardInstanceId(9027), "T002", gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);

        var actorCharacterInstanceId = actorPlayerState.activeCharacterInstanceId!.Value;
        var hpBefore = gameState.characterInstances[actorCharacterInstanceId].currentHp;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9231,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9020),
            playMode = "normal",
        });

        Assert.Equal(hpBefore - 1, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(
            $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9021",
            gameState.currentInputContext!.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9232,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9021",
        });

        Assert.Contains(new CardInstanceId(9021), gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Contains(new CardInstanceId(9027), gameState.zones[gameState.publicState.summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(new CardInstanceId(9027), gameState.zones[gameState.publicState.publicTreasureDeckZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T011_ShouldIncreaseSkillPointAndDealSpellDamageToSelectedOpponent()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2400);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3400);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6401, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6402, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9022), "T011");
        var skillPointBefore = actorPlayerState.skillPoint;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9241,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9022),
            playMode = "normal",
        });

        Assert.Equal(skillPointBefore + 1, actorPlayerState.skillPoint);
        Assert.NotNull(gameState.currentInputContext);
        var hpBefore = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp;
        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9242,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("damageResponse", gameState.currentResponseWindow!.windowTypeKey);
        Assert.Equal("awaitDefense", gameState.currentResponseWindow.pendingDamageResponseStageKey);
        Assert.Equal(enemyPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(hpBefore, gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp);
        Assert.Equal("continuation:stagedResponseDamage", gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "responseWindowOpened" &&
                         interactionWindowEvent.windowKindKey == "responseWindow");
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T011_WhenTargetUsesT002SpellDefense_ShouldResolveZeroDamage()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2450);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3450);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6451, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6452, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9030), "T011");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(9031), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9243,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9030),
            playMode = "normal",
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9244,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        var hpBeforeDefense = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp;
        var finalEvents = processor.processActionRequest(gameState, new SubmitDefenseActionRequest
        {
            requestId = 9245,
            actorPlayerId = enemyPlayerId,
            defenseCardInstanceId = new CardInstanceId(9031),
            defenseTypeKey = "spell",
        });

        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(hpBeforeDefense, gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value].currentHp);
        Assert.Contains(
            finalEvents,
            gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                         damageResolvedEvent.finalDamageValue == 0);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T012_ShouldDrawTwoAndAllowOptionalSummonZoneBanish()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2500);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3500);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6501, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6502, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9935);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9023), "T012");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9024), "T001");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9025), "T006");
        createPublicCardInZone(gameState, new CardInstanceId(9026), "T002", gameState.publicState!.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, new CardInstanceId(9028), "T003", gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);

        var handBefore = gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count;
        var deckBefore = gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count;

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9251,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9023),
            playMode = "normal",
        });

        Assert.Equal(handBefore + 1, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(deckBefore - 2, gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds.Count);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains(
            TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOptionalBanish,
            gameState.currentInputContext!.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9252,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9026",
        });

        Assert.Contains(new CardInstanceId(9026), gameState.zones[gameState.publicState.gapZoneId].cardInstanceIds);
        Assert.Contains(new CardInstanceId(9028), gameState.zones[gameState.publicState.summonZoneId].cardInstanceIds);
        Assert.DoesNotContain(new CardInstanceId(9028), gameState.zones[gameState.publicState.publicTreasureDeckZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T006BanishSummonZone_WhenRefillIsT005_ShouldOpenArrivalDiscardInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2600);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3600);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6601, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6602, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9945);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9032), "T006");
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9033), "T001");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(9034), "T002");
        createPublicCardInZone(gameState, new CardInstanceId(9035), "T003", gameState.publicState!.summonZoneId, ZoneKey.summonZone);
        createPublicCardInZone(gameState, new CardInstanceId(9036), "T005", gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9253,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9032),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9254,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}9035",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureArrivalEffectRuntime.ContinuationKeyT005ArrivalAllPlayersDiscard1,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Null(gameState.currentInputContext!.requiredPlayerId);
        Assert.Contains(actorPlayerId, gameState.currentInputContext.requiredPlayerIds);
        Assert.Contains(enemyPlayerId, gameState.currentInputContext.requiredPlayerIds);
        Assert.All(
            gameState.currentInputContext.choiceKeysByRequiredPlayerNumericId[actorPlayerId.Value],
            choiceKey => Assert.StartsWith(TreasureArrivalEffectRuntime.ChoiceKeyDiscardCardPrefix, choiceKey, StringComparison.Ordinal));
    }

    private static RuleCore.GameState.GameState createRunningActionPhaseState(
        PlayerId currentPlayerId,
        TeamId currentTeamId)
    {
        return new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = currentPlayerId,
                currentTeamId = currentTeamId,
                currentPhase = TurnPhase.action,
                phaseStepIndex = 0,
            },
        };
    }

    private static void ensurePublicZones(RuleCore.GameState.GameState gameState, long gapZoneNumericId)
    {
        var publicTreasureDeckZoneId = new ZoneId(gapZoneNumericId - 4);
        var anomalyDeckZoneId = new ZoneId(gapZoneNumericId - 3);
        var sakuraCakeDeckZoneId = new ZoneId(gapZoneNumericId - 2);
        var summonZoneId = new ZoneId(gapZoneNumericId - 1);
        var gapZoneId = new ZoneId(gapZoneNumericId);

        gameState.publicState = new PublicState
        {
            publicTreasureDeckZoneId = publicTreasureDeckZoneId,
            anomalyDeckZoneId = anomalyDeckZoneId,
            sakuraCakeDeckZoneId = sakuraCakeDeckZoneId,
            summonZoneId = summonZoneId,
            gapZoneId = gapZoneId,
        };

        gameState.zones[publicTreasureDeckZoneId] = new ZoneState
        {
            zoneId = publicTreasureDeckZoneId,
            zoneType = ZoneKey.publicTreasureDeck,
        };
        gameState.zones[anomalyDeckZoneId] = new ZoneState
        {
            zoneId = anomalyDeckZoneId,
            zoneType = ZoneKey.anomalyDeck,
        };
        gameState.zones[sakuraCakeDeckZoneId] = new ZoneState
        {
            zoneId = sakuraCakeDeckZoneId,
            zoneType = ZoneKey.sakuraCakeDeck,
        };
        gameState.zones[summonZoneId] = new ZoneState
        {
            zoneId = summonZoneId,
            zoneType = ZoneKey.summonZone,
        };
        gameState.zones[gapZoneId] = new ZoneState
        {
            zoneId = gapZoneId,
            zoneType = ZoneKey.gapZone,
        };
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
            mana = 0,
            skillPoint = 0,
            sigilPreview = 0,
        };
    }

    private static void addPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        long activeCharacterNumericId,
        int currentHp,
        int maxHp)
    {
        gameState.players[playerState.playerId] = playerState;
        gameState.zones[playerState.deckZoneId] = new ZoneState
        {
            zoneId = playerState.deckZoneId,
            zoneType = ZoneKey.deck,
            ownerPlayerId = playerState.playerId,
        };
        gameState.zones[playerState.handZoneId] = new ZoneState
        {
            zoneId = playerState.handZoneId,
            zoneType = ZoneKey.hand,
            ownerPlayerId = playerState.playerId,
        };
        gameState.zones[playerState.discardZoneId] = new ZoneState
        {
            zoneId = playerState.discardZoneId,
            zoneType = ZoneKey.discard,
            ownerPlayerId = playerState.playerId,
        };
        gameState.zones[playerState.fieldZoneId] = new ZoneState
        {
            zoneId = playerState.fieldZoneId,
            zoneType = ZoneKey.field,
            ownerPlayerId = playerState.playerId,
        };
        gameState.zones[playerState.characterSetAsideZoneId] = new ZoneState
        {
            zoneId = playerState.characterSetAsideZoneId,
            zoneType = ZoneKey.characterSetAside,
            ownerPlayerId = playerState.playerId,
        };

        var characterInstanceId = new CharacterInstanceId(activeCharacterNumericId);
        gameState.characterInstances[characterInstanceId] = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = "tester:activeCharacter",
            ownerPlayerId = playerState.playerId,
            currentHp = currentHp,
            maxHp = maxHp,
            isAlive = true,
            isInPlay = true,
        };
        playerState.activeCharacterInstanceId = characterInstanceId;
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
