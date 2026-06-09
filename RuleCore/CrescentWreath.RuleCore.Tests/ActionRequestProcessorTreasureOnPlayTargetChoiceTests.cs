using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.DamageSystem;
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
    public void ProcessPlayTreasureCardActionRequest_T015_ShouldOnlyAllowOpponentTargetsAndApplySilence()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 15100);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 25100);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 35100);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 55101, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 55102, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 55103, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(95101), "T015");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 95111,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(95101),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT015OnPlayTargetOpponentSilence,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("player:2", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain("player:1", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("player:3", gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 95112,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, enemyPlayerId, "Silence"));
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.isApplied &&
                         statusChangedEvent.statusKey == "Silence" &&
                         statusChangedEvent.targetPlayerId == enemyPlayerId);
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
    public void ProcessPlayTreasureCardActionRequest_T020_ShouldApplyImplementedAnomalyRewardAndBanishSelf()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1468);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2468);
        var t020CardInstanceId = new CardInstanceId(90068);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5469, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6469, currentHp: 4, maxHp: 4);
        addTeam(gameState, actorPlayerState.teamId);
        addTeam(gameState, enemyPlayerState.teamId);
        actorPlayerState.mana = 7;
        gameState.teams[actorPlayerState.teamId].leyline = 4;
        gameState.teams[enemyPlayerState.teamId].killScore = 2;
        ensurePublicZones(gameState, gapZoneNumericId: 9958);
        gameState.currentAnomalyState = new CurrentAnomalyState
        {
            currentAnomalyDefinitionId = "A003",
            anomalyDeckDefinitionIds =
            {
                "A004",
            },
        };
        createCardInPlayerHand(gameState, actorPlayerState, t020CardInstanceId, "T020");

        var processor = new ActionRequestProcessor();
        var playEvents = processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9156,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t020CardInstanceId,
            playMode = "normal",
        });

        Assert.Equal("A004", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Empty(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal(7, actorPlayerState.mana);
        Assert.Equal(5, gameState.teams[actorPlayerState.teamId].leyline);
        Assert.Equal(1, gameState.teams[enemyPlayerState.teamId].killScore);
        Assert.False(StatusRuntime.hasStatusOnCharacter(
            gameState,
            actorPlayerState.activeCharacterInstanceId!.Value,
            "Shackle"));
        Assert.Contains(t020CardInstanceId, gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);

        var t020CardInstance = gameState.cardInstances[t020CardInstanceId];
        Assert.Equal(gameState.publicState.gapZoneId, t020CardInstance.zoneId);
        Assert.Equal(ZoneKey.gapZone, t020CardInstance.zoneKey);

        Assert.Contains(
            playEvents,
            gameEvent => gameEvent is AnomalyResolvedEvent anomalyResolvedEvent &&
                         anomalyResolvedEvent.anomalyDefinitionId == "A003");
        Assert.DoesNotContain(playEvents, gameEvent => gameEvent is AnomalyRewardPlaceholderEvent);
        Assert.Contains(
            playEvents,
            gameEvent => gameEvent is AnomalyFlippedEvent anomalyFlippedEvent &&
                         anomalyFlippedEvent.anomalyDefinitionId == "A004");
        Assert.Contains(
            playEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == t020CardInstanceId &&
                         cardMovedEvent.toZoneKey == ZoneKey.gapZone &&
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
    public void ProcessPlayTreasureCardActionRequest_T007_ShouldAllowGivingThisCardToTargetTeammateHand()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1470);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2470);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3470);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5471, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5472, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5473, currentHp: 4, maxHp: 4);
        var bellCardInstanceId = new CardInstanceId(90070);
        createCardInPlayerHand(gameState, actorPlayerState, bellCardInstanceId, "T007");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9160,
            actorPlayerId = actorPlayerId,
            cardInstanceId = bellCardInstanceId,
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyT007GiveToAllyPrefix}{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyT007AllyDiscardForManaPrefix}{allyPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"{TreasureOnPlayEffectRuntime.ChoiceKeyT007GiveToAllyPrefix}{actorPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain($"{TreasureOnPlayEffectRuntime.ChoiceKeyT007GiveToAllyPrefix}{enemyPlayerId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Equal(1, actorPlayerState.sigilPreview);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9161,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyT007GiveToAllyPrefix}{allyPlayerId.Value}",
        });

        var bellCardInstance = gameState.cardInstances[bellCardInstanceId];
        Assert.Equal(allyPlayerId, bellCardInstance.ownerPlayerId);
        Assert.Equal(allyPlayerState.handZoneId, bellCardInstance.zoneId);
        Assert.Contains(bellCardInstanceId, gameState.zones[allyPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(bellCardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Contains(submitEvents, gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                                   cardMovedEvent.cardInstanceId == bellCardInstanceId &&
                                                   cardMovedEvent.toZoneKey == ZoneKey.hand);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T007_ShouldAskTargetTeammateToDiscardThenGrantSourceMana()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1480);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3480);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5481, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5483, currentHp: 4, maxHp: 4);
        var bellCardInstanceId = new CardInstanceId(90080);
        var allyDiscardCardInstanceId = new CardInstanceId(90081);
        createCardInPlayerHand(gameState, actorPlayerState, bellCardInstanceId, "T007");
        createCardInPlayerHand(gameState, allyPlayerState, allyDiscardCardInstanceId, "T002");
        createCardInPlayerHand(gameState, allyPlayerState, new CardInstanceId(90082), "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9170,
            actorPlayerId = actorPlayerId,
            cardInstanceId = bellCardInstanceId,
            playMode = "normal",
        });

        var manaAfterPlay = actorPlayerState.mana;
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9171,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyT007AllyDiscardForManaPrefix}{allyPlayerId.Value}",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyDeclineDiscard, gameState.currentInputContext.choiceKeys);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix}{allyDiscardCardInstanceId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT007OnPlayTargetAllyDiscardCard,
            gameState.currentActionChain!.pendingContinuationKey);

        var allyHandCountBeforeDiscard = gameState.zones[allyPlayerState.handZoneId].cardInstanceIds.Count;
        var allyDiscardCountBeforeDiscard = gameState.zones[allyPlayerState.discardZoneId].cardInstanceIds.Count;
        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9172,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyDiscardCardPrefix}{allyDiscardCardInstanceId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(manaAfterPlay + 3, actorPlayerState.mana);
        Assert.Equal(allyHandCountBeforeDiscard - 1, gameState.zones[allyPlayerState.handZoneId].cardInstanceIds.Count);
        Assert.Equal(allyDiscardCountBeforeDiscard + 1, gameState.zones[allyPlayerState.discardZoneId].cardInstanceIds.Count);
        Assert.Contains(submitEvents, gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                                   cardMovedEvent.cardInstanceId == allyDiscardCardInstanceId &&
                                                   cardMovedEvent.moveReason == CardMoveReason.discard);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T007_WhenTargetTeammateDeclinesDiscard_ShouldNotGrantMana()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1490);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 3490);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5491, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 5493, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(90090), "T007");
        createCardInPlayerHand(gameState, allyPlayerState, new CardInstanceId(90091), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9180,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(90090),
            playMode = "normal",
        });

        var manaAfterPlay = actorPlayerState.mana;
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9181,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyT007AllyDiscardForManaPrefix}{allyPlayerId.Value}",
        });

        var allyHandCountBeforeDecline = gameState.zones[allyPlayerState.handZoneId].cardInstanceIds.Count;
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9182,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = TreasureOnPlayEffectRuntime.ChoiceKeyDeclineDiscard,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(manaAfterPlay, actorPlayerState.mana);
        Assert.Equal(allyHandCountBeforeDecline, gameState.zones[allyPlayerState.handZoneId].cardInstanceIds.Count);
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
    public void ProcessPlayTreasureCardActionRequest_T008_WhenTargetHasT029_ShouldOpenDamageImmunityInputBeforeHpLoss()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1550);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2550);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5551, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5552, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9056), "T008");
        createCardInPlayerHand(gameState, enemyPlayerState, new CardInstanceId(9057), "T029");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9156,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9056),
            playMode = "normal",
        });

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9157,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"player:{enemyPlayerId.Value}",
        });

        var targetCharacter = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(4, targetCharacter.currentHp);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(DamageProcessor.ContextKeyT029DamageImmunity, gameState.currentInputContext!.contextKey);
        Assert.Equal(enemyPlayerId, gameState.currentInputContext.requiredPlayerId);
        Assert.Equal(DamageProcessor.ContinuationKeyT029DamageImmunity, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(DamageProcessor.ChoiceKeyT029Decline, gameState.currentInputContext.choiceKeys);
        Assert.Contains(DamageProcessor.ChoiceKeyT029BanishPrefix + "9057", gameState.currentInputContext.choiceKeys);
        Assert.Contains(submitEvents, gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                                                   interactionWindowEvent.windowKindKey == "inputContext" &&
                                                   interactionWindowEvent.isOpened);
        Assert.DoesNotContain(submitEvents, gameEvent => gameEvent is DamageResolvedEvent);
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
    public void ProcessPlayTreasureCardActionRequest_T022_WhenBanishingT024_ShouldHealAndDrawForCurrentTurnPlayer()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 1910);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 2910);
        var t022CardInstanceId = new CardInstanceId(9015);
        var t024CardInstanceId = new CardInstanceId(9016);
        var deckCardInstanceId = new CardInstanceId(9017);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 5911, currentHp: 1, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 5912, currentHp: 4, maxHp: 4);
        ensurePublicZones(gameState, gapZoneNumericId: 9915);
        createCardInPlayerHand(gameState, actorPlayerState, t022CardInstanceId, "T022");
        createCardInPlayerDiscard(gameState, actorPlayerState, t024CardInstanceId, "T024");
        createCardInPlayerDeck(gameState, actorPlayerState, deckCardInstanceId, "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9193,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t022CardInstanceId,
            playMode = "normal",
        });

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9194,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyBanishCardPrefix}{t024CardInstanceId.Value}",
        });

        var actorActiveCharacter =
            gameState.characterInstances[actorPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(4, actorActiveCharacter.currentHp);
        Assert.Contains(deckCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(t024CardInstanceId, gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == actorPlayerId &&
                         hpChangedEvent.hpBefore == 1 &&
                         hpChangedEvent.hpAfter == 4);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == deckCardInstanceId &&
                         cardMovedEvent.moveReason == CardMoveReason.draw);
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

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T013_WhenDeclaredCardMatchesDrawnCard_ShouldRevealAndCharmSelectedOpponent()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2700);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3700);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 4700);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6701, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6702, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 6703, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9040), "T013");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9041), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9260,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9040),
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT013OnPlayDeclareCardName,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("declareCardName:T001", gameState.currentInputContext!.choiceKeys);
        Assert.Contains("declareCardName:starter:magicCircuit", gameState.currentInputContext.choiceKeys);
        Assert.Contains("declareCardName:starter:kourindouCoupon", gameState.currentInputContext.choiceKeys);
        Assert.Contains("declareCardName:S001", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("declareCardName:test-summon-card", gameState.currentInputContext.choiceKeys);

        var declareEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9261,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "declareCardName:T002",
        });

        Assert.Contains(new CardInstanceId(9041), gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(declareEvents, gameEvent => gameEvent is CardRevealedEvent cardRevealedEvent &&
                                                    cardRevealedEvent.cardInstanceId == new CardInstanceId(9041) &&
                                                    cardRevealedEvent.definitionId == "T002");
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT013OnPlayTargetOpponentCharm,
            gameState.currentActionChain.pendingContinuationKey);
        Assert.Contains("player:2", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain("player:1", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("player:3", gameState.currentInputContext.choiceKeys);

        var charmEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9262,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, enemyPlayerId, "Charm"));
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Contains(charmEvents, gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                                                  statusChangedEvent.statusKey == "Charm" &&
                                                  statusChangedEvent.targetPlayerId == enemyPlayerId &&
                                                  statusChangedEvent.isApplied);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T023_ShouldIncreaseLeylineHealHumanThenDamageEnemyNonHuman()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 2920);
        var enemyPlayerState = createPlayerState(enemyPlayerId, enemyTeamId, 3920);
        var allyPlayerState = createPlayerState(allyPlayerId, actorTeamId, 4920);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorTeamId);
        addTeam(gameState, actorTeamId);
        addTeam(gameState, enemyTeamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6921, currentHp: 4, maxHp: 4, "human");
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6922, currentHp: 3, maxHp: 4, "nonHuman");
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 6923, currentHp: 1, maxHp: 4, "human");
        var cardInstanceId = new CardInstanceId(9045);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "T023");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9290,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        });

        Assert.Equal(1, gameState.teams[actorTeamId].leyline);
        Assert.Equal(2, actorPlayerState.mana);
        Assert.Equal(2, actorPlayerState.sigilPreview);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(TreasureOnPlayEffectRuntime.ContinuationKeyT023OnPlayTargetHumanHeal2, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(TreasureOnPlayEffectRuntime.InputTypeKeyTreasureOnPlayTargetCharacterChoice, gameState.currentInputContext!.inputTypeKey);
        Assert.Contains("character:6921", gameState.currentInputContext.choiceKeys);
        Assert.Contains("character:6923", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("character:6922", gameState.currentInputContext.choiceKeys);

        var healEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9291,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "character:6923",
        });

        var allyCharacter = gameState.characterInstances[allyPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(3, allyCharacter.currentHp);
        Assert.Contains(healEvents, gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                                                 hpChangedEvent.targetCharacterInstanceId == allyCharacter.characterInstanceId &&
                                                 hpChangedEvent.hpBefore == 1 &&
                                                 hpChangedEvent.hpAfter == 3);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(TreasureOnPlayEffectRuntime.ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1, gameState.currentActionChain.pendingContinuationKey);
        Assert.Contains("character:6922", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain("character:6921", gameState.currentInputContext.choiceKeys);
        Assert.DoesNotContain("character:6923", gameState.currentInputContext.choiceKeys);

        var damageEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9292,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "character:6922",
        });

        var enemyCharacter = gameState.characterInstances[enemyPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(2, enemyCharacter.currentHp);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentResponseWindow);
        Assert.Null(gameState.currentActionChain.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(2, gameState.teams[actorTeamId].leyline);
        Assert.Contains(damageEvents, gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                                                   damageResolvedEvent.finalDamageValue == 1 &&
                                                   damageResolvedEvent.didDealDamage);
        Assert.Contains(damageEvents, gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                                                   hpChangedEvent.targetCharacterInstanceId == enemyCharacter.characterInstanceId &&
                                                   hpChangedEvent.hpBefore == 3 &&
                                                   hpChangedEvent.hpAfter == 2);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T023_WhenNoHumanTarget_ShouldSkipHealAndOpenEnemyNonHumanDamage()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 2930);
        var enemyPlayerState = createPlayerState(enemyPlayerId, enemyTeamId, 3930);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorTeamId);
        addTeam(gameState, actorTeamId);
        addTeam(gameState, enemyTeamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6931, currentHp: 4, maxHp: 4, "nonHuman");
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6932, currentHp: 4, maxHp: 4, "nonHuman");
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9046), "T023");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9293,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9046),
            playMode = "normal",
        });

        Assert.Equal(1, gameState.teams[actorTeamId].leyline);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(TreasureOnPlayEffectRuntime.ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("character:6932", gameState.currentInputContext!.choiceKeys);
        Assert.DoesNotContain("character:6931", gameState.currentInputContext.choiceKeys);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T023_WhenNoLegalTargets_ShouldOnlyIncreaseLeylineAndComplete()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var enemyTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 2940);
        var enemyPlayerState = createPlayerState(enemyPlayerId, enemyTeamId, 3940);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorTeamId);
        addTeam(gameState, actorTeamId);
        addTeam(gameState, enemyTeamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6941, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6942, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9047), "T023");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9294,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9047),
            playMode = "normal",
        });

        Assert.Equal(1, gameState.teams[actorTeamId].leyline);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentResponseWindow);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T028_ShouldApplyNextDamagePenetrateToActor()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2900);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6901, currentHp: 4, maxHp: 4);
        var cardInstanceId = new CardInstanceId(9044);
        createCardInPlayerHand(gameState, actorPlayerState, cardInstanceId, "T028");

        var processor = new ActionRequestProcessor();
        var playEvents = processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9280,
            actorPlayerId = actorPlayerId,
            cardInstanceId = cardInstanceId,
            playMode = "normal",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentResponseWindow);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(3, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.sigilPreview);
        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, actorPlayerId, "Penetrate"));

        var penetrateStatus = Assert.Single(
            gameState.statusInstances,
            status => status.statusKey == "Penetrate" &&
                      status.targetPlayerId == actorPlayerId);
        Assert.Equal(actorPlayerId, penetrateStatus.applierPlayerId);
        Assert.Equal(cardInstanceId, penetrateStatus.applierCardInstanceId);
        Assert.Equal(StatusRuntime.DurationTypeKeyNextDamageAttempt, penetrateStatus.durationTypeKey);

        Assert.Contains(playEvents, gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                                                 statusChangedEvent.statusKey == "Penetrate" &&
                                                 statusChangedEvent.targetPlayerId == actorPlayerId &&
                                                 statusChangedEvent.isApplied);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T013_WhenDeclaredCardDoesNotMatch_ShouldRevealAndCompleteWithoutCharmInput()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2800);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3800);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6801, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6802, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9042), "T013");
        createCardInPlayerDeck(gameState, actorPlayerState, new CardInstanceId(9043), "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9270,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9042),
            playMode = "normal",
        });

        var declareEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9271,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "declareCardName:T001",
        });

        Assert.Contains(new CardInstanceId(9043), gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(declareEvents, gameEvent => gameEvent is CardRevealedEvent cardRevealedEvent &&
                                                    cardRevealedEvent.definitionId == "T002");
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, enemyPlayerId, "Charm"));
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T013_WhenDeckEmptyAndDiscardHasDeclaredCard_ShouldRebuildDrawAndOpenCharmInput()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 2900);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 3900);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 6901, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 6902, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9044), "T013");
        createCardInPlayerDiscard(gameState, actorPlayerState, new CardInstanceId(9045), "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9280,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9044),
            playMode = "normal",
        });

        var declareEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9281,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "declareCardName:T001",
        });

        Assert.Contains(new CardInstanceId(9045), gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(declareEvents, gameEvent => gameEvent is CardRevealedEvent cardRevealedEvent &&
                                                    cardRevealedEvent.cardInstanceId == new CardInstanceId(9045));
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT013OnPlayTargetOpponentCharm,
            gameState.currentActionChain!.pendingContinuationKey);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T013_WhenDeckAndDiscardAreEmpty_ShouldCompleteAfterDeclaration()
    {
        var actorPlayerId = new PlayerId(1);
        var enemyPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3000);
        var enemyPlayerState = createPlayerState(enemyPlayerId, new TeamId(2), 4000);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7001, currentHp: 4, maxHp: 4);
        addPlayer(gameState, enemyPlayerState, activeCharacterNumericId: 7002, currentHp: 4, maxHp: 4);
        createCardInPlayerHand(gameState, actorPlayerState, new CardInstanceId(9046), "T013");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9290,
            actorPlayerId = actorPlayerId,
            cardInstanceId = new CardInstanceId(9046),
            playMode = "normal",
        });

        var declareEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9291,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "declareCardName:T001",
        });

        Assert.DoesNotContain(declareEvents, gameEvent => gameEvent is CardRevealedEvent);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, enemyPlayerId, "Charm"));
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T021_ShouldOverlayUpToTwoHandCardsAndGainMana()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3100);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7101, currentHp: 4, maxHp: 4);
        var t021CardInstanceId = new CardInstanceId(9050);
        var firstOverlayCardInstanceId = new CardInstanceId(9051);
        var secondOverlayCardInstanceId = new CardInstanceId(9052);
        createCardInPlayerHand(gameState, actorPlayerState, t021CardInstanceId, "T021");
        createCardInPlayerHand(gameState, actorPlayerState, firstOverlayCardInstanceId, "T001");
        createCardInPlayerHand(gameState, actorPlayerState, secondOverlayCardInstanceId, "T002");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9301,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t021CardInstanceId,
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT021OnPlayOverlayCardsForMana,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(TreasureOnPlayEffectRuntime.InputTypeKeyTreasureOnPlayOverlayCardsChoice, gameState.currentInputContext!.inputTypeKey);
        Assert.Contains(TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOverlay, gameState.currentInputContext.choiceKeys);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{firstOverlayCardInstanceId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{secondOverlayCardInstanceId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Equal(2, actorPlayerState.mana);
        Assert.Equal(2, actorPlayerState.sigilPreview);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9302,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKeys =
            {
                $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{firstOverlayCardInstanceId.Value}",
                $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{secondOverlayCardInstanceId.Value}",
            },
        });

        var overlayContainerZoneId = OverlayRuntime.resolveOverlayContainerZoneId(t021CardInstanceId);
        Assert.Contains(t021CardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.DoesNotContain(firstOverlayCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.DoesNotContain(secondOverlayCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(firstOverlayCardInstanceId, gameState.zones[overlayContainerZoneId].cardInstanceIds);
        Assert.Contains(secondOverlayCardInstanceId, gameState.zones[overlayContainerZoneId].cardInstanceIds);
        Assert.Equal(ZoneKey.overlayContainer, gameState.cardInstances[firstOverlayCardInstanceId].zoneKey);
        Assert.Equal(ZoneKey.overlayContainer, gameState.cardInstances[secondOverlayCardInstanceId].zoneKey);
        Assert.Equal(t021CardInstanceId, gameState.cardInstances[firstOverlayCardInstanceId].overlayContainerCardInstanceId);
        Assert.Equal(t021CardInstanceId, gameState.cardInstances[secondOverlayCardInstanceId].overlayContainerCardInstanceId);
        Assert.False(gameState.cardInstances[firstOverlayCardInstanceId].isFaceUp);
        Assert.False(gameState.cardInstances[secondOverlayCardInstanceId].isFaceUp);
        Assert.Equal(6, actorPlayerState.mana);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Contains(
            submitEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == firstOverlayCardInstanceId &&
                         cardMovedEvent.toZoneKey == ZoneKey.overlayContainer);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T021_WhenDeclined_ShouldCompleteWithoutOverlay()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3200);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7201, currentHp: 4, maxHp: 4);
        var t021CardInstanceId = new CardInstanceId(9060);
        var overlayCandidateCardInstanceId = new CardInstanceId(9061);
        createCardInPlayerHand(gameState, actorPlayerState, t021CardInstanceId, "T021");
        createCardInPlayerHand(gameState, actorPlayerState, overlayCandidateCardInstanceId, "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9311,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t021CardInstanceId,
            playMode = "normal",
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9312,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = TreasureOnPlayEffectRuntime.ChoiceKeyDeclineOverlay,
        });

        Assert.Equal(2, actorPlayerState.mana);
        Assert.Contains(overlayCandidateCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T021_WhenSelectingMoreThanTwoCards_ShouldThrowAndKeepInputContext()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3300);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7301, currentHp: 4, maxHp: 4);
        var t021CardInstanceId = new CardInstanceId(9070);
        var firstOverlayCardInstanceId = new CardInstanceId(9071);
        var secondOverlayCardInstanceId = new CardInstanceId(9072);
        var thirdOverlayCardInstanceId = new CardInstanceId(9073);
        createCardInPlayerHand(gameState, actorPlayerState, t021CardInstanceId, "T021");
        createCardInPlayerHand(gameState, actorPlayerState, firstOverlayCardInstanceId, "T001");
        createCardInPlayerHand(gameState, actorPlayerState, secondOverlayCardInstanceId, "T002");
        createCardInPlayerHand(gameState, actorPlayerState, thirdOverlayCardInstanceId, "T003");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9321,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t021CardInstanceId,
            playMode = "normal",
        });

        var inputContextId = gameState.currentInputContext!.inputContextId;
        var exception = Assert.Throws<InvalidOperationException>(() => processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9322,
            actorPlayerId = actorPlayerId,
            inputContextId = inputContextId,
            choiceKeys =
            {
                $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{firstOverlayCardInstanceId.Value}",
                $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{secondOverlayCardInstanceId.Value}",
                $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{thirdOverlayCardInstanceId.Value}",
            },
        }));

        Assert.Equal("T021 overlay continuation supports at most two selected overlayCard choiceKeys.", exception.Message);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(inputContextId, gameState.currentInputContext!.inputContextId);
        Assert.Contains(firstOverlayCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(secondOverlayCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
        Assert.Contains(thirdOverlayCardInstanceId, gameState.zones[actorPlayerState.handZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessEnterEndPhaseActionRequest_WhenT021ContainerLeavesField_ShouldDiscardOverlayCards()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3400);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7401, currentHp: 4, maxHp: 4);
        var t021CardInstanceId = new CardInstanceId(9080);
        var overlayCardInstanceId = new CardInstanceId(9081);
        createCardInPlayerHand(gameState, actorPlayerState, t021CardInstanceId, "T021");
        createCardInPlayerHand(gameState, actorPlayerState, overlayCardInstanceId, "T001");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9331,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t021CardInstanceId,
            playMode = "normal",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9332,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{overlayCardInstanceId.Value}",
        });
        for (var cardIndex = 0; cardIndex < 6; cardIndex++)
        {
            createCardInPlayerHand(
                gameState,
                actorPlayerState,
                new CardInstanceId(9090 + cardIndex),
                "T001");
        }

        processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 9333,
            actorPlayerId = actorPlayerId,
        });

        Assert.Contains(t021CardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Contains(overlayCardInstanceId, gameState.zones[actorPlayerState.discardZoneId].cardInstanceIds);
        Assert.Equal(ZoneKey.discard, gameState.cardInstances[overlayCardInstanceId].zoneKey);
        Assert.Null(gameState.cardInstances[overlayCardInstanceId].overlayContainerCardInstanceId);
        Assert.Null(gameState.cardInstances[overlayCardInstanceId].overlayOrderIndex);
    }

    [Fact]
    public void ProcessEnterEndPhaseActionRequest_WhenT016IsOnField_ShouldKeepMechanicalJadeAndOverlayCards()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3500);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7501, currentHp: 4, maxHp: 4);
        var t016CardInstanceId = new CardInstanceId(9100);
        var overlayCardInstanceId = new CardInstanceId(9101);
        createCardInPlayerField(gameState, actorPlayerState, t016CardInstanceId, "T016");
        createCardInPlayerHand(gameState, actorPlayerState, overlayCardInstanceId, "T001");
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            new ZoneMovementService(),
            gameState.cardInstances[overlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9440),
            eventId: 9440);
        for (var cardIndex = 0; cardIndex < 6; cardIndex++)
        {
            createCardInPlayerHand(
                gameState,
                actorPlayerState,
                new CardInstanceId(9110 + cardIndex),
                "T001");
        }

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new EnterEndPhaseActionRequest
        {
            requestId = 9441,
            actorPlayerId = actorPlayerId,
        });

        Assert.Contains(t016CardInstanceId, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds);
        Assert.Equal(1, OverlayRuntime.getOverlayCardCount(gameState, t016CardInstanceId));
        Assert.Equal(t016CardInstanceId, gameState.cardInstances[overlayCardInstanceId].overlayContainerCardInstanceId);
    }

    [Fact]
    public void ProcessEnterActionPhaseActionRequest_WhenT016HasOverlays_ShouldGrantTurnStartManaAndSigilPreview()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3600);
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = actorPlayerId,
                currentTeamId = actorPlayerState.teamId,
                currentPhase = TurnPhase.start,
                phaseStepIndex = 0,
            },
        };
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7601, currentHp: 4, maxHp: 4);
        var t016CardInstanceId = new CardInstanceId(9120);
        var firstOverlayCardInstanceId = new CardInstanceId(9121);
        var secondOverlayCardInstanceId = new CardInstanceId(9122);
        createCardInPlayerField(gameState, actorPlayerState, t016CardInstanceId, "T016");
        createCardInPlayerHand(gameState, actorPlayerState, firstOverlayCardInstanceId, "T001");
        createCardInPlayerHand(gameState, actorPlayerState, secondOverlayCardInstanceId, "T002");
        var movementService = new ZoneMovementService();
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[firstOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9450),
            eventId: 9450);
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[secondOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9450),
            eventId: 9450);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new EnterActionPhaseActionRequest
        {
            requestId = 9451,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, actorPlayerState.mana);
        Assert.Equal(4, actorPlayerState.sigilPreview);
        Assert.Equal(TurnPhase.action, gameState.turnState!.currentPhase);
    }

    [Fact]
    public void ProcessEnterSummonPhaseActionRequest_WhenT016HasOverlays_ShouldIncludeOverlaySigilInLockedSigil()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3700);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7701, currentHp: 4, maxHp: 4);
        var t016CardInstanceId = new CardInstanceId(9130);
        var firstOverlayCardInstanceId = new CardInstanceId(9131);
        var secondOverlayCardInstanceId = new CardInstanceId(9132);
        createCardInPlayerField(gameState, actorPlayerState, t016CardInstanceId, "T016");
        createCardInPlayerHand(gameState, actorPlayerState, firstOverlayCardInstanceId, "T001");
        createCardInPlayerHand(gameState, actorPlayerState, secondOverlayCardInstanceId, "T002");
        var movementService = new ZoneMovementService();
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[firstOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9460),
            eventId: 9460);
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[secondOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9460),
            eventId: 9460);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new EnterSummonPhaseActionRequest
        {
            requestId = 9461,
            actorPlayerId = actorPlayerId,
        });

        Assert.Equal(4, actorPlayerState.lockedSigil);
        Assert.True(actorPlayerState.isSigilLocked);
        Assert.Equal(0, actorPlayerState.sigilPreview);
    }

    [Fact]
    public void SubmitInputChoice_WhenT016OwnerKillsOpponent_ShouldOpenOverlayChoiceAndApplyResourceDelta()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 3800);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 3900);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        ensurePublicZones(gameState, gapZoneNumericId: 9965);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7801, currentHp: 4, maxHp: 4);
        addPlayer(gameState, targetPlayerState, activeCharacterNumericId: 7802, currentHp: 1, maxHp: 4);
        addTeam(gameState, actorPlayerState.teamId);
        addTeam(gameState, targetPlayerState.teamId);
        var t016CardInstanceId = new CardInstanceId(9140);
        var t008CardInstanceId = new CardInstanceId(9141);
        var overlayCandidateCardInstanceId = new CardInstanceId(9142);
        var gapCandidateCardInstanceId = new CardInstanceId(9143);
        createCardInPlayerField(gameState, actorPlayerState, t016CardInstanceId, "T016");
        createCardInPlayerHand(gameState, actorPlayerState, t008CardInstanceId, "T008");
        createCardInPlayerDiscard(gameState, actorPlayerState, overlayCandidateCardInstanceId, "T001");
        createOwnedCardInPublicZone(
            gameState,
            actorPlayerId,
            gapCandidateCardInstanceId,
            "T002",
            gameState.publicState!.gapZoneId,
            ZoneKey.gapZone);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9471,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t008CardInstanceId,
            playMode = "normal",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9472,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "player:2",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(MechanicalJadeRuntime.ContextKeyOverlayAfterKill, gameState.currentInputContext!.contextKey);
        Assert.Equal(MechanicalJadeRuntime.ContinuationKeyOverlayAfterKill, gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{overlayCandidateCardInstanceId.Value}", gameState.currentInputContext.choiceKeys);
        Assert.Contains($"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{gapCandidateCardInstanceId.Value}", gameState.currentInputContext.choiceKeys);

        var manaBeforeOverlay = actorPlayerState.mana;
        var sigilBeforeOverlay = actorPlayerState.sigilPreview;
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9473,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeyOverlayCardPrefix}{overlayCandidateCardInstanceId.Value}",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.Equal(1, OverlayRuntime.getOverlayCardCount(gameState, t016CardInstanceId));
        Assert.Equal(t016CardInstanceId, gameState.cardInstances[overlayCandidateCardInstanceId].overlayContainerCardInstanceId);
        Assert.Equal(manaBeforeOverlay + 2, actorPlayerState.mana);
        Assert.Equal(sigilBeforeOverlay + 2, actorPlayerState.sigilPreview);
    }

    [Fact]
    public void SubmitInputChoice_WhenT016AlreadyHasTwoOverlays_ShouldNotOpenOverlayChoiceAfterKill()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4000);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 4100);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 7901, currentHp: 4, maxHp: 4);
        addPlayer(gameState, targetPlayerState, activeCharacterNumericId: 7902, currentHp: 1, maxHp: 4);
        addTeam(gameState, actorPlayerState.teamId);
        addTeam(gameState, targetPlayerState.teamId);
        var t016CardInstanceId = new CardInstanceId(9150);
        var firstOverlayCardInstanceId = new CardInstanceId(9151);
        var secondOverlayCardInstanceId = new CardInstanceId(9152);
        var t008CardInstanceId = new CardInstanceId(9153);
        createCardInPlayerField(gameState, actorPlayerState, t016CardInstanceId, "T016");
        createCardInPlayerHand(gameState, actorPlayerState, firstOverlayCardInstanceId, "T001");
        createCardInPlayerHand(gameState, actorPlayerState, secondOverlayCardInstanceId, "T002");
        createCardInPlayerHand(gameState, actorPlayerState, t008CardInstanceId, "T008");
        var movementService = new ZoneMovementService();
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[firstOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9480),
            eventId: 9480);
        OverlayRuntime.overlayCardUnderContainer(
            gameState,
            movementService,
            gameState.cardInstances[secondOverlayCardInstanceId],
            gameState.cardInstances[t016CardInstanceId],
            new ActionChainId(9480),
            eventId: 9480);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9481,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t008CardInstanceId,
            playMode = "normal",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9482,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "player:2",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(2, OverlayRuntime.getOverlayCardCount(gameState, t016CardInstanceId));
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T017_ShouldMoveSummonZoneCardToFriendlyDeckTopAndRefillSummonZone()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4200);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 4300);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        ensurePublicZones(gameState, gapZoneNumericId: 9975);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 8001, currentHp: 4, maxHp: 4);
        addPlayer(gameState, allyPlayerState, activeCharacterNumericId: 8003, currentHp: 4, maxHp: 4);

        var t017CardInstanceId = new CardInstanceId(9160);
        var summonZoneCardInstanceId = new CardInstanceId(9161);
        var refillCardInstanceId = new CardInstanceId(9162);
        createCardInPlayerHand(gameState, actorPlayerState, t017CardInstanceId, "T017");
        createPublicCardInZone(
            gameState,
            summonZoneCardInstanceId,
            "T001",
            gameState.publicState!.summonZoneId,
            ZoneKey.summonZone);
        createPublicCardInZone(
            gameState,
            refillCardInstanceId,
            "T002",
            gameState.publicState.publicTreasureDeckZoneId,
            ZoneKey.publicTreasureDeck);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9491,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t017CardInstanceId,
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT017OnPlaySelectSummonZoneCard,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains(
            $"{TreasureOnPlayEffectRuntime.ChoiceKeySummonZoneCardPrefix}{summonZoneCardInstanceId.Value}",
            gameState.currentInputContext!.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9492,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = $"{TreasureOnPlayEffectRuntime.ChoiceKeySummonZoneCardPrefix}{summonZoneCardInstanceId.Value}",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT017OnPlayTargetFriendlyDeckTop,
            gameState.currentActionChain.pendingContinuationKey);
        Assert.Contains("player:1", gameState.currentInputContext!.choiceKeys);
        Assert.Contains("player:3", gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 9493,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:3",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(summonZoneCardInstanceId, gameState.zones[allyPlayerState.deckZoneId].cardInstanceIds[0]);
        Assert.Equal(allyPlayerId, gameState.cardInstances[summonZoneCardInstanceId].ownerPlayerId);
        Assert.Equal(ZoneKey.deck, gameState.cardInstances[summonZoneCardInstanceId].zoneKey);
        Assert.False(gameState.cardInstances[summonZoneCardInstanceId].isFaceUp);
        Assert.Contains(refillCardInstanceId, gameState.zones[gameState.publicState.summonZoneId].cardInstanceIds);
        Assert.Empty(gameState.zones[gameState.publicState.publicTreasureDeckZoneId].cardInstanceIds);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T020_ShouldForceResolveCurrentAnomalyAndBanishSelf()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4400);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        ensurePublicZones(gameState, gapZoneNumericId: 9985);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 8101, currentHp: 4, maxHp: 4);
        addTeam(gameState, actorPlayerState.teamId);
        var enemyTeamId = new TeamId(2);
        addTeam(gameState, enemyTeamId);
        gameState.currentAnomalyState = new CurrentAnomalyState
        {
            currentAnomalyDefinitionId = "A001",
        };
        gameState.currentAnomalyState.anomalyDeckDefinitionIds.Add("A002");
        var t020CardInstanceId = new CardInstanceId(9170);
        createCardInPlayerHand(gameState, actorPlayerState, t020CardInstanceId, "T020");

        var processor = new ActionRequestProcessor();

        var events = processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 9501,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t020CardInstanceId,
            playMode = "normal",
        });

        Assert.Equal("A002", gameState.currentAnomalyState.currentAnomalyDefinitionId);
        Assert.Empty(gameState.currentAnomalyState.anomalyDeckDefinitionIds);
        Assert.Equal(9, gameState.teams[enemyTeamId].killScore);
        Assert.True(gameState.turnState!.hasResolvedAnomalyThisTurn);
        Assert.Contains(t020CardInstanceId, gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds);
        Assert.Equal(ZoneKey.gapZone, gameState.cardInstances[t020CardInstanceId].zoneKey);
        Assert.Contains(events, gameEvent => gameEvent is AnomalyResolvedEvent anomalyResolvedEvent &&
                                             anomalyResolvedEvent.anomalyDefinitionId == "A001");
        Assert.Contains(events, gameEvent => gameEvent is AnomalyFlippedEvent anomalyFlippedEvent &&
                                             anomalyFlippedEvent.anomalyDefinitionId == "A002");
        Assert.Contains(events, gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                                             cardMovedEvent.cardInstanceId == t020CardInstanceId &&
                                             cardMovedEvent.toZoneKey == ZoneKey.gapZone &&
                                             cardMovedEvent.moveReason == CardMoveReason.banish);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T026_ShouldOpenMarkerGrantInputAndAddSelectedMarker()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4500);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 5500);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 8501, currentHp: 4, maxHp: 4);
        addPlayer(gameState, targetPlayerState, activeCharacterNumericId: 8502, currentHp: 4, maxHp: 4);
        gameState.characterInstances[actorPlayerState.activeCharacterInstanceId!.Value].definitionId = "C005";
        gameState.characterInstances[targetPlayerState.activeCharacterInstanceId!.Value].definitionId = "C017";
        var t026CardInstanceId = new CardInstanceId(9050);
        createCardInPlayerHand(gameState, actorPlayerState, t026CardInstanceId, "T026");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 99501,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t026CardInstanceId,
            playMode = "normal",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(actorPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(
            TreasureOnPlayEffectRuntime.ContinuationKeyT026OnPlayGrantMarker,
            gameState.currentActionChain!.pendingContinuationKey);
        Assert.Contains("markerGrant:decline", gameState.currentInputContext.choiceKeys);
        Assert.Contains("markerGrant:1:swordAura", gameState.currentInputContext.choiceKeys);
        Assert.Contains("markerGrant:2:fire", gameState.currentInputContext.choiceKeys);

        var submitEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 99502,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "markerGrant:2:fire",
        });

        var targetCharacter = gameState.characterInstances[targetPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(1, MarkerRuntime.getMarkerCount(targetCharacter, "fire"));
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentActionChain!.pendingContinuationKey);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Contains(submitEvents, gameEvent => gameEvent is MarkerChangedEvent markerChangedEvent &&
                                                 markerChangedEvent.targetPlayerId == targetPlayerId &&
                                                 markerChangedEvent.markerTypeKey == "fire" &&
                                                 markerChangedEvent.afterCount == 1);
    }

    [Fact]
    public void ProcessPlayTreasureCardActionRequest_T026_WhenDecline_ShouldCompleteWithoutAddingMarker()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 4600);
        var gameState = createRunningActionPhaseState(actorPlayerId, actorPlayerState.teamId);
        addPlayer(gameState, actorPlayerState, activeCharacterNumericId: 8601, currentHp: 4, maxHp: 4);
        gameState.characterInstances[actorPlayerState.activeCharacterInstanceId!.Value].definitionId = "C005";
        var t026CardInstanceId = new CardInstanceId(9051);
        createCardInPlayerHand(gameState, actorPlayerState, t026CardInstanceId, "T026");

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new PlayTreasureCardActionRequest
        {
            requestId = 99511,
            actorPlayerId = actorPlayerId,
            cardInstanceId = t026CardInstanceId,
            playMode = "normal",
        });

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 99512,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "markerGrant:decline",
        });

        var actorCharacter = gameState.characterInstances[actorPlayerState.activeCharacterInstanceId!.Value];
        Assert.Equal(0, MarkerRuntime.getMarkerCount(actorCharacter, "swordAura"));
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
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
        int maxHp,
        params string[] raceTags)
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
        var characterInstance = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = "tester:activeCharacter",
            ownerPlayerId = playerState.playerId,
            currentHp = currentHp,
            maxHp = maxHp,
            isAlive = true,
            isInPlay = true,
        };
        foreach (var raceTag in raceTags)
        {
            characterInstance.raceTags.Add(raceTag);
        }

        gameState.characterInstances[characterInstanceId] = characterInstance;
        playerState.activeCharacterInstanceId = characterInstanceId;
    }

    private static void addTeam(RuleCore.GameState.GameState gameState, TeamId teamId)
    {
        gameState.teams[teamId] = new TeamState
        {
            teamId = teamId,
            leyline = 0,
            killScore = 10,
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

    private static void createCardInPlayerField(
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
            zoneId = playerState.fieldZoneId,
            zoneKey = ZoneKey.field,
            isFaceUp = true,
        };
        gameState.zones[playerState.fieldZoneId].cardInstanceIds.Add(cardInstanceId);
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

    private static void createOwnedCardInPublicZone(
        RuleCore.GameState.GameState gameState,
        PlayerId ownerPlayerId,
        CardInstanceId cardInstanceId,
        string definitionId,
        ZoneId zoneId,
        ZoneKey zoneKey)
    {
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerId,
            zoneId = zoneId,
            zoneKey = zoneKey,
        };
        gameState.zones[zoneId].cardInstanceIds.Add(cardInstanceId);
    }
}
