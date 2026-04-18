using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class ActionRequestProcessorUseSkillResourceTests
{
    [Fact]
    public void C004_1_WhenResourceIsSufficient_ShouldDeductManaAndSkillPointAndCompleteChain()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7100, mana: 5, skillPoint: 0);
        var characterInstanceId = new CharacterInstanceId(71001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: TurnPhase.start);

        var processor = new ActionRequestProcessor();
        var enterActionEvents = processor.processActionRequest(gameState, new EnterActionPhaseActionRequest
        {
            requestId = 71100,
            actorPlayerId = actorPlayerId,
        });

        Assert.Empty(enterActionEvents);
        Assert.Equal(TurnPhase.action, gameState.turnState!.currentPhase);
        Assert.Equal(1, actorPlayerState.skillPoint);

        var request = new UseSkillActionRequest
        {
            requestId = 71101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C004:1",
        };

        var producedEvents = processor.processActionRequest(gameState, request);

        var statusChangedEvent = Assert.Single(producedEvents);
        var appliedStatusChangedEvent = Assert.IsType<StatusChangedEvent>(statusChangedEvent);
        Assert.Equal("Penetrate", appliedStatusChangedEvent.statusKey);
        Assert.True(appliedStatusChangedEvent.isApplied);
        Assert.Equal(actorPlayerId, appliedStatusChangedEvent.targetPlayerId);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.skillPoint);
        Assert.NotNull(gameState.currentActionChain);
        Assert.IsType<UseSkillActionRequest>(gameState.currentActionChain!.rootActionRequest);
        Assert.Equal(1, gameState.currentActionChain.currentFrameIndex);
        Assert.True(gameState.currentActionChain.isCompleted);
        Assert.Equal(0, gameState.turnState!.phaseStepIndex);
        var penetrateStatuses = gameState.statusInstances.FindAll(status => status.statusKey == "Penetrate" && status.targetPlayerId == actorPlayerId);
        var penetrateStatus = Assert.Single(penetrateStatuses);
        Assert.Equal(StatusRuntime.DurationTypeKeyNextDamageAttempt, penetrateStatus.durationTypeKey);
    }

    [Fact]
    public void C021_1_WhenUsed_ShouldHealActorCharacterAndDrawOneCard()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7120, mana: 0, skillPoint: 0);
        var actorCharacterInstanceId = new CharacterInstanceId(71201);
        var deckCardInstanceId = new CardInstanceId(71211);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(actorCharacterInstanceId, createCharacter(actorCharacterInstanceId, "C021", actorPlayerId, currentHp: 2));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addDeckTreasureCard(gameState, actorPlayerState, deckCardInstanceId, "T001");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: TurnPhase.action);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71201,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C021:1",
        });

        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetCharacterInstanceId == actorCharacterInstanceId &&
                         hpChangedEvent.hpBefore == 2 &&
                         hpChangedEvent.hpAfter == 3);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == deckCardInstanceId &&
                         cardMovedEvent.toZoneKey == ZoneKey.hand);
        Assert.Equal(3, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Equal(actorPlayerState.handZoneId, gameState.cardInstances[deckCardInstanceId].zoneId);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
    }

    [Fact]
    public void C002_2_WhenUsed_ShouldDrawOneCardAndHealEveryOtherPlayerByTwo()
    {
        var actorPlayerId = new PlayerId(1);
        var opponentAPlayerId = new PlayerId(2);
        var allyPlayerId = new PlayerId(3);
        var opponentBPlayerId = new PlayerId(4);

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7130, mana: 4, skillPoint: 0);
        var opponentAPlayerState = createPlayerState(opponentAPlayerId, new TeamId(2), 7230, mana: 0, skillPoint: 0);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 7330, mana: 0, skillPoint: 0);
        var opponentBPlayerState = createPlayerState(opponentBPlayerId, new TeamId(2), 7430, mana: 0, skillPoint: 0);

        var actorCharacterInstanceId = new CharacterInstanceId(71301);
        var opponentACharacterInstanceId = new CharacterInstanceId(72301);
        var allyCharacterInstanceId = new CharacterInstanceId(73301);
        var opponentBCharacterInstanceId = new CharacterInstanceId(74301);
        var deckCardInstanceId = new CardInstanceId(71311);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(opponentAPlayerId, opponentAPlayerState);
        gameState.players.Add(allyPlayerId, allyPlayerState);
        gameState.players.Add(opponentBPlayerId, opponentBPlayerState);
        gameState.characterInstances.Add(actorCharacterInstanceId, createCharacter(actorCharacterInstanceId, "C002", actorPlayerId));
        gameState.characterInstances.Add(opponentACharacterInstanceId, createCharacter(opponentACharacterInstanceId, "C001", opponentAPlayerId, currentHp: 1));
        gameState.characterInstances.Add(allyCharacterInstanceId, createCharacter(allyCharacterInstanceId, "C004", allyPlayerId, currentHp: 1));
        gameState.characterInstances.Add(opponentBCharacterInstanceId, createCharacter(opponentBCharacterInstanceId, "C008", opponentBPlayerId, currentHp: 1));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, opponentAPlayerState);
        addPlayerOwnedZones(gameState, allyPlayerState);
        addPlayerOwnedZones(gameState, opponentBPlayerState);
        addDeckTreasureCard(gameState, actorPlayerState, deckCardInstanceId, "T002");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: TurnPhase.action);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71301,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C002:2",
        });

        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is CardMovedEvent cardMovedEvent &&
                         cardMovedEvent.cardInstanceId == deckCardInstanceId &&
                         cardMovedEvent.toZoneKey == ZoneKey.hand);
        var hpChangedEvents = producedEvents.FindAll(gameEvent => gameEvent is HpChangedEvent);
        Assert.Equal(3, hpChangedEvents.Count);
        Assert.Equal(3, gameState.characterInstances[opponentACharacterInstanceId].currentHp);
        Assert.Equal(3, gameState.characterInstances[allyCharacterInstanceId].currentHp);
        Assert.Equal(3, gameState.characterInstances[opponentBCharacterInstanceId].currentHp);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
    }

    [Fact]
    public void C029_4_WhenUsed_ShouldApplyCharmToOpponentsAndDealDirectDamageToSelf()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(2);
        var opponentAPlayerId = new PlayerId(3);
        var opponentBPlayerId = new PlayerId(4);

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7140, mana: 4, skillPoint: 4);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 7240, mana: 0, skillPoint: 0);
        var opponentAPlayerState = createPlayerState(opponentAPlayerId, new TeamId(2), 7340, mana: 0, skillPoint: 0);
        var opponentBPlayerState = createPlayerState(opponentBPlayerId, new TeamId(2), 7440, mana: 0, skillPoint: 0);

        var actorCharacterInstanceId = new CharacterInstanceId(71401);
        var allyCharacterInstanceId = new CharacterInstanceId(72401);
        var opponentACharacterInstanceId = new CharacterInstanceId(73401);
        var opponentBCharacterInstanceId = new CharacterInstanceId(74401);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(allyPlayerId, allyPlayerState);
        gameState.players.Add(opponentAPlayerId, opponentAPlayerState);
        gameState.players.Add(opponentBPlayerId, opponentBPlayerState);
        gameState.characterInstances.Add(actorCharacterInstanceId, createCharacter(actorCharacterInstanceId, "C029", actorPlayerId));
        gameState.characterInstances.Add(allyCharacterInstanceId, createCharacter(allyCharacterInstanceId, "C001", allyPlayerId));
        gameState.characterInstances.Add(opponentACharacterInstanceId, createCharacter(opponentACharacterInstanceId, "C002", opponentAPlayerId));
        gameState.characterInstances.Add(opponentBCharacterInstanceId, createCharacter(opponentBCharacterInstanceId, "C003", opponentBPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, allyPlayerState);
        addPlayerOwnedZones(gameState, opponentAPlayerState);
        addPlayerOwnedZones(gameState, opponentBPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: TurnPhase.action);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71401,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C029:4",
        });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, opponentAPlayerId, "Charm"));
        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, opponentBPlayerId, "Charm"));
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, allyPlayerId, "Charm"));
        Assert.Equal(3, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.isApplied &&
                         statusChangedEvent.statusKey == "Charm" &&
                         statusChangedEvent.targetPlayerId == opponentAPlayerId);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.isApplied &&
                         statusChangedEvent.statusKey == "Charm" &&
                         statusChangedEvent.targetPlayerId == opponentBPlayerId);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                         damageResolvedEvent.finalDamageValue == 1 &&
                         damageResolvedEvent.didDealDamage);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
    }

    [Fact]
    public void C018_2_WhenUsed_ShouldDealOneDirectDamageToEveryOtherPlayer()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(2);
        var opponentAPlayerId = new PlayerId(3);
        var opponentBPlayerId = new PlayerId(4);

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7150, mana: 6, skillPoint: 0);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(1), 7250, mana: 0, skillPoint: 0);
        var opponentAPlayerState = createPlayerState(opponentAPlayerId, new TeamId(2), 7350, mana: 0, skillPoint: 0);
        var opponentBPlayerState = createPlayerState(opponentBPlayerId, new TeamId(2), 7450, mana: 0, skillPoint: 0);

        var actorCharacterInstanceId = new CharacterInstanceId(71501);
        var allyCharacterInstanceId = new CharacterInstanceId(72501);
        var opponentACharacterInstanceId = new CharacterInstanceId(73501);
        var opponentBCharacterInstanceId = new CharacterInstanceId(74501);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(allyPlayerId, allyPlayerState);
        gameState.players.Add(opponentAPlayerId, opponentAPlayerState);
        gameState.players.Add(opponentBPlayerId, opponentBPlayerState);
        gameState.characterInstances.Add(actorCharacterInstanceId, createCharacter(actorCharacterInstanceId, "C018", actorPlayerId));
        gameState.characterInstances.Add(allyCharacterInstanceId, createCharacter(allyCharacterInstanceId, "C001", allyPlayerId));
        gameState.characterInstances.Add(opponentACharacterInstanceId, createCharacter(opponentACharacterInstanceId, "C002", opponentAPlayerId));
        gameState.characterInstances.Add(opponentBCharacterInstanceId, createCharacter(opponentBCharacterInstanceId, "C003", opponentBPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, allyPlayerState);
        addPlayerOwnedZones(gameState, opponentAPlayerState);
        addPlayerOwnedZones(gameState, opponentBPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: TurnPhase.action);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71501,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C018:2",
        });

        var damageResolvedEvents = producedEvents.FindAll(gameEvent => gameEvent is DamageResolvedEvent);
        Assert.Equal(3, damageResolvedEvents.Count);
        Assert.Equal(4, gameState.characterInstances[actorCharacterInstanceId].currentHp);
        Assert.Equal(3, gameState.characterInstances[allyCharacterInstanceId].currentHp);
        Assert.Equal(3, gameState.characterInstances[opponentACharacterInstanceId].currentHp);
        Assert.Equal(3, gameState.characterInstances[opponentBCharacterInstanceId].currentHp);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == allyPlayerId &&
                         hpChangedEvent.delta == -1);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == opponentAPlayerId &&
                         hpChangedEvent.delta == -1);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetPlayerId == opponentBPlayerId &&
                         hpChangedEvent.delta == -1);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                         damageResolvedEvent.finalDamageValue == 1 &&
                         damageResolvedEvent.didDealDamage);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                         damageResolvedEvent.finalDamageValue == 1 &&
                         damageResolvedEvent.didDealDamage);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is DamageResolvedEvent damageResolvedEvent &&
                         damageResolvedEvent.finalDamageValue == 1 &&
                         damageResolvedEvent.didDealDamage);
    }

    [Fact]
    public void C004_1_AfterSuccessfulUseSkill_NextFullyDefendedNonDirectDamageShouldDealOneAndConsumePenetrate()
    {
        var sourcePlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var sourcePlayerState = createPlayerState(sourcePlayerId, new TeamId(1), 7150, mana: 5, skillPoint: 1);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7250, mana: 0, skillPoint: 0);
        var sourceCharacterInstanceId = new CharacterInstanceId(71501);
        var targetCharacterInstanceId = new CharacterInstanceId(72501);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(sourcePlayerId, sourcePlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.characterInstances.Add(sourceCharacterInstanceId, createCharacter(sourceCharacterInstanceId, "C004", sourcePlayerId));
        gameState.characterInstances.Add(targetCharacterInstanceId, createCharacter(targetCharacterInstanceId, "C001", targetPlayerId));
        addPlayerOwnedZones(gameState, sourcePlayerState);
        addPlayerOwnedZones(gameState, targetPlayerState);
        setRunningTurnForPlayer(gameState, sourcePlayerId, sourcePlayerState.teamId, currentPhase: TurnPhase.action);

        var actionRequestProcessor = new ActionRequestProcessor();
        actionRequestProcessor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71502,
            actorPlayerId = sourcePlayerId,
            characterInstanceId = sourceCharacterInstanceId,
            skillKey = "C004:1",
        });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, sourcePlayerId, "Penetrate"));

        var damageProcessor = new DamageProcessor();
        var producedDamageEvents = damageProcessor.resolveDamage(gameState, new DamageContext
        {
            damageContextId = new DamageContextId(71503),
            sourcePlayerId = sourcePlayerId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetPlayerId = targetPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = 0,
            damageType = "physical",
        });

        var damageResolvedEvent = Assert.IsType<DamageResolvedEvent>(producedDamageEvents[0]);
        Assert.Equal(1, damageResolvedEvent.finalDamageValue);
        Assert.Equal(3, gameState.characterInstances[targetCharacterInstanceId].currentHp);
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, sourcePlayerId, "Penetrate"));
        Assert.Contains(
            producedDamageEvents,
            gameEvent =>
                gameEvent is StatusChangedEvent statusChangedEvent &&
                statusChangedEvent.statusKey == "Penetrate" &&
                !statusChangedEvent.isApplied &&
                statusChangedEvent.targetPlayerId == sourcePlayerId);
    }
    [Fact]
    public void C004_1_WhenManaIsInsufficient_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7200, mana: 4, skillPoint: 2);
        var characterInstanceId = new CharacterInstanceId(72001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addFieldTreasureCard(gameState, actorPlayerState, new CardInstanceId(72011), "T022");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;
        var phaseStepIndexBefore = gameState.turnState!.phaseStepIndex;

        var request = new UseSkillActionRequest
        {
            requestId = 72101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C004:1",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("UseSkillActionRequest requires actor player mana to be sufficient for skill cost.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
        Assert.Equal(phaseStepIndexBefore, gameState.turnState!.phaseStepIndex);
    }

    [Fact]
    public void C001_3_WhenSkillPointIsInsufficient_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7300, mana: 8, skillPoint: 1);
        var characterInstanceId = new CharacterInstanceId(73001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C001", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, phaseStepIndex: 1);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;

        var request = new UseSkillActionRequest
        {
            requestId = 73101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C001:3",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("UseSkillActionRequest requires actor player skillPoint to be sufficient for skill cost.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void C004_1_WhenCharacterOwnershipMismatch_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var ownerPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7400, mana: 8, skillPoint: 2);
        var ownerPlayerState = createPlayerState(ownerPlayerId, new TeamId(2), 7500, mana: 0, skillPoint: 0);
        var characterInstanceId = new CharacterInstanceId(74001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(ownerPlayerId, ownerPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", ownerPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, ownerPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;

        var request = new UseSkillActionRequest
        {
            requestId = 74101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C004:1",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("UseSkillActionRequest requires characterInstance.ownerPlayerId to equal actorPlayerId.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    [Fact]
    public void WhenSkillKeyIsUnsupported_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7600, mana: 8, skillPoint: 2);
        var characterInstanceId = new CharacterInstanceId(76001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;

        var request = new UseSkillActionRequest
        {
            requestId = 76101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C004:999",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<NotSupportedException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("UseSkillActionRequest requires skillKey to exist in character definition.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }
    [Fact]
    public void C001_1_WhenSkillIsDefinedButNoEffectMapping_ShouldDeductCostAndCompleteWithoutEvents()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7650, mana: 4, skillPoint: 1);
        var characterInstanceId = new CharacterInstanceId(76501);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C001", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, phaseStepIndex: 1);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 76501,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C001:1",
        });

        Assert.Empty(producedEvents);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.skillPoint);
        Assert.NotNull(gameState.currentActionChain);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }
    [Theory]
    [InlineData(TurnPhase.start)]
    [InlineData(TurnPhase.summon)]
    [InlineData(TurnPhase.end)]
    public void C004_1_WhenCurrentPhaseIsNotAction_ShouldThrowAndKeepStateUnchanged(TurnPhase currentPhase)
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7700, mana: 5, skillPoint: 1);
        var characterInstanceId = new CharacterInstanceId(77001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, currentPhase: currentPhase, phaseStepIndex: 4);
        var sentinelChain = createSentinelActionChain();
        gameState.currentActionChain = sentinelChain;
        var producedEventsBefore = sentinelChain.producedEvents.Count;
        var manaBefore = actorPlayerState.mana;
        var skillPointBefore = actorPlayerState.skillPoint;
        var phaseStepIndexBefore = gameState.turnState!.phaseStepIndex;

        var request = new UseSkillActionRequest
        {
            requestId = 77102,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C004:1",
        };

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(
            () => processor.processActionRequest(gameState, request));

        Assert.Equal("UseSkillActionRequest requires gameState.turnState.currentPhase to be action.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Equal(phaseStepIndexBefore, gameState.turnState.phaseStepIndex);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
    }

    private static ActionChainState createSentinelActionChain()
    {
        var sentinelChain = new ActionChainState
        {
            actionChainId = new ActionChainId(76991),
            isCompleted = true,
            currentFrameIndex = 1,
        };
        sentinelChain.producedEvents.Add(new ActionAcceptedEvent
        {
            eventId = 76991,
            eventTypeKey = "actionAccepted",
            requestId = 76991,
            actorPlayerId = new PlayerId(1),
            requestTypeKey = "sentinel",
        });
        return sentinelChain;
    }

    private static PlayerState createPlayerState(PlayerId playerId, TeamId teamId, long zoneIdBase, int mana, int skillPoint)
    {
        return new PlayerState
        {
            playerId = playerId,
            teamId = teamId,
            mana = mana,
            skillPoint = skillPoint,
            deckZoneId = new ZoneId(zoneIdBase),
            handZoneId = new ZoneId(zoneIdBase + 1),
            discardZoneId = new ZoneId(zoneIdBase + 2),
            fieldZoneId = new ZoneId(zoneIdBase + 3),
            characterSetAsideZoneId = new ZoneId(zoneIdBase + 4),
        };
    }

    private static void addPlayerOwnedZones(RuleCore.GameState.GameState gameState, PlayerState playerState)
    {
        gameState.zones[playerState.deckZoneId] = createOwnedZone(playerState.deckZoneId, ZoneKey.deck, playerState.playerId);
        gameState.zones[playerState.handZoneId] = createOwnedZone(playerState.handZoneId, ZoneKey.hand, playerState.playerId);
        gameState.zones[playerState.discardZoneId] = createOwnedZone(playerState.discardZoneId, ZoneKey.discard, playerState.playerId);
        gameState.zones[playerState.fieldZoneId] = createOwnedZone(playerState.fieldZoneId, ZoneKey.field, playerState.playerId);
        gameState.zones[playerState.characterSetAsideZoneId] = createOwnedZone(playerState.characterSetAsideZoneId, ZoneKey.characterSetAside, playerState.playerId);
    }

    private static ZoneState createOwnedZone(ZoneId zoneId, ZoneKey zoneKey, PlayerId ownerPlayerId)
    {
        return new ZoneState
        {
            zoneId = zoneId,
            zoneType = zoneKey,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = ZonePublicOrPrivate.privateZone,
        };
    }

    private static void addFieldTreasureCard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        var fieldZoneState = gameState.zones[playerState.fieldZoneId];
        fieldZoneState.cardInstanceIds.Add(cardInstanceId);
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.fieldZoneId,
            zoneKey = ZoneKey.field,
            isFaceUp = true,
            isSetAside = false,
        };
    }

    private static void addDeckTreasureCard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        var deckZoneState = gameState.zones[playerState.deckZoneId];
        deckZoneState.cardInstanceIds.Add(cardInstanceId);
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.deckZoneId,
            zoneKey = ZoneKey.deck,
            isFaceUp = false,
            isSetAside = false,
        };
    }

    private static CharacterInstance createCharacter(
        CharacterInstanceId characterInstanceId,
        string definitionId,
        PlayerId ownerPlayerId,
        int currentHp = 4,
        int maxHp = 4)
    {
        return new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerId,
            currentHp = currentHp,
            maxHp = maxHp,
            isAlive = true,
            isInPlay = true,
        };
    }

    private static void setRunningTurnForPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        TeamId teamId,
        TurnPhase currentPhase = TurnPhase.action,
        int phaseStepIndex = 0)
    {
        gameState.matchState = MatchState.running;
        gameState.turnState = new TurnState
        {
            turnNumber = 1,
            currentPlayerId = playerId,
            currentTeamId = teamId,
            currentPhase = currentPhase,
            phaseStepIndex = phaseStepIndex,
        };
    }
}












