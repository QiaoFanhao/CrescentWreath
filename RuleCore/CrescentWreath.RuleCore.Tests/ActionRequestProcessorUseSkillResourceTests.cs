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
        Assert.Equal(0, actorPlayerState.skillPoint);
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
    public void C004_1_WhenSkillPointIsInsufficient_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7300, mana: 8, skillPoint: 0);
        var characterInstanceId = new CharacterInstanceId(73001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C004", actorPlayerId));
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
            skillKey = "C004:1",
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

        Assert.Equal("UseSkillActionRequest currently supports only skillKey C004:1 for resource closure.", exception.Message);
        Assert.Equal(manaBefore, actorPlayerState.mana);
        Assert.Equal(skillPointBefore, actorPlayerState.skillPoint);
        Assert.Same(sentinelChain, gameState.currentActionChain);
        Assert.Equal(producedEventsBefore, sentinelChain.producedEvents.Count);
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

    private static CharacterInstance createCharacter(CharacterInstanceId characterInstanceId, string definitionId, PlayerId ownerPlayerId)
    {
        return new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = definitionId,
            ownerPlayerId = ownerPlayerId,
            currentHp = 4,
            maxHp = 4,
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









