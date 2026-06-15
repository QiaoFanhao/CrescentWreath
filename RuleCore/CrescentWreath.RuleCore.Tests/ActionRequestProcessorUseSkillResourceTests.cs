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
    public void C008_1_ShouldChooseOpponentThenDamageTypeAndOpenMatchingDamageResponse()
    {
        var actorPlayerId = new PlayerId(1);
        var opponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 8000, mana: 3, skillPoint: 1);
        var opponentState = createPlayerState(opponentPlayerId, opponentTeamId, 8100, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(80001);
        var opponentCharacterId = new CharacterInstanceId(81001);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[opponentPlayerId] = opponentState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, killScore = 3 };
        gameState.teams[opponentTeamId] = new TeamState { teamId = opponentTeamId, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C008", actorPlayerId);
        gameState.characterInstances[opponentCharacterId] =
            createCharacter(opponentCharacterId, "C001", opponentPlayerId, currentHp: 6, maxHp: 6);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, opponentState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 80001,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C008:1",
        });

        Assert.Equal("character:C008:1:targetOpponent", gameState.currentInputContext!.contextKey);
        Assert.Contains("player:2", gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 80002,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.Equal("character:C008:1:damageType", gameState.currentInputContext!.contextKey);
        Assert.Equal(
            new[] { "damageType:physical", "damageType:spell" },
            gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 80003,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "damageType:physical",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(opponentPlayerId, gameState.currentResponseWindow!.currentResponderPlayerId);
        Assert.Equal(opponentCharacterId, gameState.currentResponseWindow.pendingDamageTargetCharacterInstanceId);
        Assert.Equal(3, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal("physical", gameState.currentResponseWindow.pendingDamageTypeKey);
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
    }

    [Fact]
    public void C008_2_ShouldStackPhysicalBoostsAndConsumeAllOnNextPhysicalDamage()
    {
        var actorPlayerId = new PlayerId(1);
        var opponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 8200, mana: 6, skillPoint: 2);
        var opponentState = createPlayerState(opponentPlayerId, opponentTeamId, 8300, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(82001);
        var opponentCharacterId = new CharacterInstanceId(83001);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[opponentPlayerId] = opponentState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, killScore = 3 };
        gameState.teams[opponentTeamId] = new TeamState { teamId = opponentTeamId, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C008", actorPlayerId);
        gameState.characterInstances[opponentCharacterId] =
            createCharacter(opponentCharacterId, "C001", opponentPlayerId, currentHp: 8, maxHp: 8);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, opponentState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);
        var processor = new ActionRequestProcessor();

        for (var requestId = 82001L; requestId <= 82002L; requestId++)
        {
            processor.processActionRequest(gameState, new UseSkillActionRequest
            {
                requestId = requestId,
                actorPlayerId = actorPlayerId,
                characterInstanceId = actorCharacterId,
                skillKey = "C008:2",
            });
        }

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, actorCharacterId, "Barrier"));
        Assert.Equal(
            2,
            gameState.statusInstances.Count(
                status => status.targetPlayerId == actorPlayerId &&
                          status.statusKey == StatusRuntime.StatusKeyPhysicalDamageBoostNext));

        var spellContext = new DamageContext
        {
            damageContextId = new DamageContextId(82003),
            sourcePlayerId = actorPlayerId,
            sourceCharacterInstanceId = actorCharacterId,
            targetPlayerId = opponentPlayerId,
            targetCharacterInstanceId = opponentCharacterId,
            baseDamageValue = 1,
            damageType = "spell",
        };
        new DamageProcessor().resolveDamage(gameState, spellContext);
        Assert.Equal(1, spellContext.finalDamageValue);
        Assert.Equal(
            2,
            gameState.statusInstances.Count(
                status => status.statusKey == StatusRuntime.StatusKeyPhysicalDamageBoostNext));

        var physicalContext = new DamageContext
        {
            damageContextId = new DamageContextId(82004),
            sourcePlayerId = actorPlayerId,
            sourceCharacterInstanceId = actorCharacterId,
            targetPlayerId = opponentPlayerId,
            targetCharacterInstanceId = opponentCharacterId,
            baseDamageValue = 3,
            damageType = "physical",
        };
        new DamageProcessor().resolveDamage(gameState, physicalContext);

        Assert.Equal(5, physicalContext.finalDamageValue);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.statusKey == StatusRuntime.StatusKeyPhysicalDamageBoostNext);
    }

    [Fact]
    public void C008_3_ShouldDrawTwoAndApplyOneSpellDamageBoost()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 8400, mana: 6, skillPoint: 1);
        var actorCharacterId = new CharacterInstanceId(84001);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 1 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C008", actorPlayerId);
        addPlayerOwnedZones(gameState, actorState);
        addDeckTreasureCard(gameState, actorState, new CardInstanceId(84011), "T001");
        addDeckTreasureCard(gameState, actorState, new CardInstanceId(84012), "T002");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        new ActionRequestProcessor().processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 84001,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C008:3",
        });

        Assert.Equal(2, gameState.zones[actorState.handZoneId].cardInstanceIds.Count);
        Assert.True(StatusRuntime.hasStatusOnPlayer(
            gameState,
            actorPlayerId,
            StatusRuntime.StatusKeySpellDamageBoostNext));
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
        Assert.Equal(0, gameState.teams[actorTeamId].leyline);
    }

    [Fact]
    public void C008_4_ShouldResolveSixSpellDamageAgainstEachOpponentInSeatOrder()
    {
        var actorPlayerId = new PlayerId(1);
        var opponentAPlayerId = new PlayerId(2);
        var opponentBPlayerId = new PlayerId(4);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 8500, mana: 7, skillPoint: 1);
        var opponentAState = createPlayerState(opponentAPlayerId, opponentTeamId, 8600, mana: 0, skillPoint: 0);
        var opponentBState = createPlayerState(opponentBPlayerId, opponentTeamId, 8700, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(85001);
        var opponentACharacterId = new CharacterInstanceId(86001);
        var opponentBCharacterId = new CharacterInstanceId(87001);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[opponentAPlayerId] = opponentAState;
        gameState.players[opponentBPlayerId] = opponentBState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 3, killScore = 3 };
        gameState.teams[opponentTeamId] = new TeamState { teamId = opponentTeamId, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C008", actorPlayerId);
        gameState.characterInstances[opponentACharacterId] =
            createCharacter(opponentACharacterId, "C001", opponentAPlayerId, currentHp: 10, maxHp: 10);
        gameState.characterInstances[opponentBCharacterId] =
            createCharacter(opponentBCharacterId, "C002", opponentBPlayerId, currentHp: 10, maxHp: 10);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, opponentAState);
        addPlayerOwnedZones(gameState, opponentBState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);
        var processor = new ActionRequestProcessor();

        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 85001,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C008:4",
        });

        Assert.Equal(opponentAPlayerId, gameState.currentResponseWindow!.currentResponderPlayerId);
        Assert.Equal(6, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal("spell", gameState.currentResponseWindow.pendingDamageTypeKey);

        processor.processActionRequest(gameState, new SubmitResponseActionRequest
        {
            requestId = 85002,
            actorPlayerId = opponentAPlayerId,
            responseWindowId = gameState.currentResponseWindow.responseWindowId,
            shouldRespond = false,
        });

        Assert.Equal(4, gameState.characterInstances[opponentACharacterId].currentHp);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(opponentBPlayerId, gameState.currentResponseWindow!.currentResponderPlayerId);

        processor.processActionRequest(gameState, new SubmitResponseActionRequest
        {
            requestId = 85003,
            actorPlayerId = opponentBPlayerId,
            responseWindowId = gameState.currentResponseWindow.responseWindowId,
            shouldRespond = false,
        });

        Assert.Equal(4, gameState.characterInstances[opponentBCharacterId].currentHp);
        Assert.Null(gameState.currentResponseWindow);
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
        Assert.Equal(2, gameState.teams[actorTeamId].leyline);
    }

    [Fact]
    public void C008PhysicalBoost_ShouldIncreasePendingDamageBeforeDefenseWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var targetTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 8800, mana: 0, skillPoint: 0);
        var targetState = createPlayerState(targetPlayerId, targetTeamId, 8900, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(88001);
        var targetCharacterId = new CharacterInstanceId(89001);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[targetPlayerId] = targetState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, killScore = 3 };
        gameState.teams[targetTeamId] = new TeamState { teamId = targetTeamId, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C008", actorPlayerId);
        gameState.characterInstances[targetCharacterId] =
            createCharacter(targetCharacterId, "C001", targetPlayerId, currentHp: 8, maxHp: 8);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, targetState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);
        StatusRuntime.applyStatus(gameState, new StatusInstance
        {
            statusKey = StatusRuntime.StatusKeyPhysicalDamageBoostNext,
            targetPlayerId = actorPlayerId,
            durationTypeKey = StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt,
        });

        new ActionRequestProcessor().processActionRequest(gameState, new OpenDamageResponseWindowActionRequest
        {
            requestId = 88001,
            actorPlayerId = actorPlayerId,
            sourceCharacterInstanceId = actorCharacterId,
            targetCharacterInstanceId = targetCharacterId,
            baseDamageValue = 3,
            damageTypeKey = "physical",
        });

        Assert.Equal(4, gameState.currentResponseWindow!.pendingDamageBaseDamageValue);
        Assert.DoesNotContain(
            gameState.statusInstances,
            status => status.statusKey == StatusRuntime.StatusKeyPhysicalDamageBoostNext);
    }

    [Fact]
    public void C007_1_ShouldAddDreamMarkerUpToCapAndStillPayCostAtCap()
    {
        var actorPlayerId = new PlayerId(1);
        var actorState = createPlayerState(actorPlayerId, new TeamId(1), 7010, mana: 4, skillPoint: 2);
        var actorCharacterId = new CharacterInstanceId(70101);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C007", actorPlayerId);
        addPlayerOwnedZones(gameState, actorState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorState.teamId);
        var processor = new ActionRequestProcessor();

        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70101,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:1",
        });

        Assert.Equal(1, MarkerRuntime.getMarkerCount(gameState.characterInstances[actorCharacterId], "dream"));
        gameState.characterInstances[actorCharacterId].markerState.markerMap["dream"] = 3;
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70102,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:1",
        });

        Assert.Equal(3, MarkerRuntime.getMarkerCount(gameState.characterInstances[actorCharacterId], "dream"));
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
    }

    [Fact]
    public void C007_2_ShouldDrawForTeammateAndAlsoActorWhenDreamMarkerCountIsThree()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var teamId = new TeamId(1);
        var actorState = createPlayerState(actorPlayerId, teamId, 7020, mana: 4, skillPoint: 1);
        var allyState = createPlayerState(allyPlayerId, teamId, 7030, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(70201);
        var allyCharacterId = new CharacterInstanceId(70301);
        var actorDeckCardId = new CardInstanceId(70211);
        var allyDeckCardId = new CardInstanceId(70311);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[allyPlayerId] = allyState;
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C007", actorPlayerId);
        gameState.characterInstances[actorCharacterId].markerState.markerMap["dream"] = 3;
        gameState.characterInstances[allyCharacterId] = createCharacter(allyCharacterId, "C001", allyPlayerId);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, allyState);
        addDeckTreasureCard(gameState, actorState, actorDeckCardId, "T001");
        addDeckTreasureCard(gameState, allyState, allyDeckCardId, "T002");
        setRunningTurnForPlayer(gameState, actorPlayerId, teamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70201,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:2",
        });

        Assert.Contains(actorDeckCardId, gameState.zones[actorState.handZoneId].cardInstanceIds);
        Assert.Contains(allyDeckCardId, gameState.zones[allyState.handZoneId].cardInstanceIds);
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
    }

    [Fact]
    public void C007_3_ShouldChooseOpponentAndC011ShouldBeValidButUnaffected()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorState = createPlayerState(actorPlayerId, new TeamId(1), 7040, mana: 4, skillPoint: 1);
        var targetState = createPlayerState(targetPlayerId, new TeamId(2), 7050, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(70401);
        var targetCharacterId = new CharacterInstanceId(70501);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[targetPlayerId] = targetState;
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C007", actorPlayerId);
        gameState.characterInstances[actorCharacterId].markerState.markerMap["dream"] = 3;
        gameState.characterInstances[targetCharacterId] = createCharacter(targetCharacterId, "C011", targetPlayerId);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, targetState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorState.teamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70401,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:3",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Contains("player:2", gameState.currentInputContext!.choiceKeys);
        Assert.Equal(0, MarkerRuntime.getMarkerCount(gameState.characterInstances[actorCharacterId], "dream"));

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 70402,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterId, "Charm"));
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Charm"));
        Assert.True(gameState.currentActionChain!.isCompleted);
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
    }

    [Fact]
    public void C007_3_ShouldApplyCharmToSelectedNonC011Opponent()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorState = createPlayerState(actorPlayerId, new TeamId(1), 7055, mana: 4, skillPoint: 1);
        var targetState = createPlayerState(targetPlayerId, new TeamId(2), 7065, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(70551);
        var targetCharacterId = new CharacterInstanceId(70651);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[targetPlayerId] = targetState;
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C007", actorPlayerId);
        gameState.characterInstances[actorCharacterId].markerState.markerMap["dream"] = 3;
        gameState.characterInstances[targetCharacterId] = createCharacter(targetCharacterId, "C002", targetPlayerId);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, targetState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorState.teamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70551,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:3",
        });
        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 70552,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "player:2",
        });

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterId, "Charm"));
        Assert.Equal(0, MarkerRuntime.getMarkerCount(gameState.characterInstances[actorCharacterId], "dream"));
    }

    [Fact]
    public void C007_4_ShouldChooseFriendlyHealTargetThenOpponentAndOpenSpellDamageResponse()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(3);
        var opponentPlayerId = new PlayerId(2);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 7060, mana: 5, skillPoint: 1);
        var allyState = createPlayerState(allyPlayerId, actorTeamId, 7070, mana: 0, skillPoint: 0);
        var opponentState = createPlayerState(opponentPlayerId, opponentTeamId, 7080, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(70601);
        var allyCharacterId = new CharacterInstanceId(70701);
        var opponentCharacterId = new CharacterInstanceId(70801);
        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[allyPlayerId] = allyState;
        gameState.players[opponentPlayerId] = opponentState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 2, killScore = 3 };
        gameState.teams[opponentTeamId] = new TeamState { teamId = opponentTeamId, leyline = 0, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C007", actorPlayerId);
        gameState.characterInstances[allyCharacterId] =
            createCharacter(allyCharacterId, "C001", allyPlayerId, currentHp: 1);
        gameState.characterInstances[opponentCharacterId] =
            createCharacter(opponentCharacterId, "C002", opponentPlayerId, currentHp: 6, maxHp: 6);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, allyState);
        addPlayerOwnedZones(gameState, opponentState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 70601,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C007:4",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("character:C007:4:healTarget", gameState.currentInputContext!.contextKey);
        Assert.Contains("player:1", gameState.currentInputContext.choiceKeys);
        Assert.Contains("player:3", gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 70602,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:3",
        });

        Assert.Equal(4, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("character:C007:4:damageTarget", gameState.currentInputContext!.contextKey);
        Assert.Contains("player:2", gameState.currentInputContext.choiceKeys);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 70603,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.Null(gameState.currentInputContext);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal(opponentPlayerId, gameState.currentResponseWindow!.currentResponderPlayerId);
        Assert.Equal(4, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal("spell", gameState.currentResponseWindow.pendingDamageTypeKey);

        processor.processActionRequest(gameState, new SubmitResponseActionRequest
        {
            requestId = 70604,
            actorPlayerId = opponentPlayerId,
            responseWindowId = gameState.currentResponseWindow.responseWindowId,
            shouldRespond = false,
        });

        Assert.Equal(2, gameState.characterInstances[opponentCharacterId].currentHp);
        Assert.Null(gameState.currentInputContext);
        Assert.Null(gameState.currentResponseWindow);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

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
    public void C021_1_WhenUsed_ShouldHealActorCharacterAndDrawOneCard()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7120, mana: 0, skillPoint: 1);
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

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7130, mana: 4, skillPoint: 1);
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

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7140, mana: 4, skillPoint: 1);
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
        gameState.teams[new TeamId(1)] = new TeamState { teamId = new TeamId(1), leyline = 4 };
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
        Assert.Equal(0, gameState.teams[new TeamId(1)].leyline);
    }

    [Fact]
    public void C018_2_WhenUsed_ShouldDealOneDirectDamageToEveryOtherPlayer()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(2);
        var opponentAPlayerId = new PlayerId(3);
        var opponentBPlayerId = new PlayerId(4);

        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7150, mana: 6, skillPoint: 1);
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
    public void C018_2_WhenFirstTargetHasT029_ShouldPauseAndResumeRemainingTargetsAfterChoice()
    {
        var actorPlayerId = new PlayerId(1);
        var allyPlayerId = new PlayerId(2);
        var opponentAPlayerId = new PlayerId(3);
        var opponentBPlayerId = new PlayerId(4);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7160, mana: 6, skillPoint: 1);
        var allyPlayerState = createPlayerState(allyPlayerId, new TeamId(2), 7260, mana: 0, skillPoint: 0);
        var opponentAPlayerState = createPlayerState(opponentAPlayerId, new TeamId(1), 7360, mana: 0, skillPoint: 0);
        var opponentBPlayerState = createPlayerState(opponentBPlayerId, new TeamId(2), 7460, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(71601);
        var allyCharacterId = new CharacterInstanceId(72601);
        var opponentACharacterId = new CharacterInstanceId(73601);
        var opponentBCharacterId = new CharacterInstanceId(74601);
        var t029CardId = new CardInstanceId(72611);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[allyPlayerId] = allyPlayerState;
        gameState.players[opponentAPlayerId] = opponentAPlayerState;
        gameState.players[opponentBPlayerId] = opponentBPlayerState;
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C018", actorPlayerId);
        gameState.characterInstances[allyCharacterId] = createCharacter(allyCharacterId, "C001", allyPlayerId);
        gameState.characterInstances[opponentACharacterId] = createCharacter(opponentACharacterId, "C002", opponentAPlayerId);
        gameState.characterInstances[opponentBCharacterId] = createCharacter(opponentBCharacterId, "C003", opponentBPlayerId);
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, allyPlayerState);
        addPlayerOwnedZones(gameState, opponentAPlayerState);
        addPlayerOwnedZones(gameState, opponentBPlayerState);
        addHandTreasureCard(gameState, allyPlayerState, t029CardId, "T029");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71601,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C018:2",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(allyPlayerId, gameState.currentInputContext!.requiredPlayerId);
        Assert.Equal(4, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(4, gameState.characterInstances[opponentACharacterId].currentHp);
        Assert.Equal(4, gameState.characterInstances[opponentBCharacterId].currentHp);

        processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 71602,
            actorPlayerId = allyPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = DamageProcessor.ChoiceKeyT029Decline,
        });

        Assert.Null(gameState.currentInputContext);
        Assert.Equal(3, gameState.characterInstances[allyCharacterId].currentHp);
        Assert.Equal(3, gameState.characterInstances[opponentACharacterId].currentHp);
        Assert.Equal(3, gameState.characterInstances[opponentBCharacterId].currentHp);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void C018_3_ShouldDirectKillHpOneOpponentsAndHonorActivatedC019Replacement()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7170, mana: 4, skillPoint: 1);
        var c019PlayerId = new PlayerId(2);
        var otherOpponentPlayerId = new PlayerId(4);
        var c019PlayerState = createPlayerState(c019PlayerId, opponentTeamId, 7270, mana: 0, skillPoint: 0);
        var otherOpponentPlayerState = createPlayerState(otherOpponentPlayerId, opponentTeamId, 7470, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(71701);
        var c019CharacterId = new CharacterInstanceId(72701);
        var otherOpponentCharacterId = new CharacterInstanceId(74701);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorPlayerState;
        gameState.players[c019PlayerId] = c019PlayerState;
        gameState.players[otherOpponentPlayerId] = otherOpponentPlayerState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 0, killScore = 3 };
        gameState.teams[opponentTeamId] = new TeamState { teamId = opponentTeamId, leyline = 0, killScore = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C018", actorPlayerId);
        gameState.characterInstances[c019CharacterId] = createCharacter(c019CharacterId, "C019", c019PlayerId, currentHp: 1);
        gameState.characterInstances[c019CharacterId].isActivated = true;
        gameState.characterInstances[otherOpponentCharacterId] =
            createCharacter(otherOpponentCharacterId, "C003", otherOpponentPlayerId, currentHp: 1);
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, c019PlayerState);
        addPlayerOwnedZones(gameState, otherOpponentPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71701,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C018:3",
        });

        Assert.False(gameState.characterInstances[c019CharacterId].isActivated);
        Assert.Equal(4, gameState.characterInstances[c019CharacterId].currentHp);
        Assert.DoesNotContain(
            events,
            gameEvent => gameEvent is KillRecordedEvent killRecordedEvent &&
                         killRecordedEvent.killedCharacterInstanceId == c019CharacterId);
        Assert.Contains(
            events,
            gameEvent => gameEvent is CharacterActivationChangedEvent activationChangedEvent &&
                         activationChangedEvent.targetCharacterInstanceId == c019CharacterId &&
                         !activationChangedEvent.isActivated);
        Assert.Contains(
            events,
            gameEvent => gameEvent is KillRecordedEvent killRecordedEvent &&
                         killRecordedEvent.killedCharacterInstanceId == otherOpponentCharacterId &&
                         killRecordedEvent.killerPlayerId == actorPlayerId);
        Assert.Equal(2, gameState.teams[opponentTeamId].killScore);
        Assert.Equal(0, gameState.teams[actorTeamId].leyline);
    }

    [Fact]
    public void C018_4_ShouldSetOnlyOpponentsAboveTwoHpToTwoWithoutDamageEvents()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var opponentTeamId = new TeamId(2);
        var actorState = createPlayerState(actorPlayerId, actorTeamId, 7180, mana: 6, skillPoint: 1);
        var opponentAId = new PlayerId(2);
        var opponentBId = new PlayerId(4);
        var opponentAState = createPlayerState(opponentAId, opponentTeamId, 7280, mana: 0, skillPoint: 0);
        var opponentBState = createPlayerState(opponentBId, opponentTeamId, 7480, mana: 0, skillPoint: 0);
        var actorCharacterId = new CharacterInstanceId(71801);
        var opponentACharacterId = new CharacterInstanceId(72801);
        var opponentBCharacterId = new CharacterInstanceId(74801);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players[actorPlayerId] = actorState;
        gameState.players[opponentAId] = opponentAState;
        gameState.players[opponentBId] = opponentBState;
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 3 };
        gameState.characterInstances[actorCharacterId] = createCharacter(actorCharacterId, "C018", actorPlayerId);
        gameState.characterInstances[opponentACharacterId] =
            createCharacter(opponentACharacterId, "C002", opponentAId, currentHp: 4);
        gameState.characterInstances[opponentBCharacterId] =
            createCharacter(opponentBCharacterId, "C003", opponentBId, currentHp: 2);
        addPlayerOwnedZones(gameState, actorState);
        addPlayerOwnedZones(gameState, opponentAState);
        addPlayerOwnedZones(gameState, opponentBState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        var processor = new ActionRequestProcessor();
        var events = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 71801,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterId,
            skillKey = "C018:4",
        });

        Assert.Equal(2, gameState.characterInstances[opponentACharacterId].currentHp);
        Assert.Equal(2, gameState.characterInstances[opponentBCharacterId].currentHp);
        Assert.DoesNotContain(events, gameEvent => gameEvent is DamageResolvedEvent);
        Assert.Contains(
            events,
            gameEvent => gameEvent is HpChangedEvent hpChangedEvent &&
                         hpChangedEvent.targetCharacterInstanceId == opponentACharacterId &&
                         hpChangedEvent.delta == -2);
        Assert.Equal(0, actorState.mana);
        Assert.Equal(0, actorState.skillPoint);
        Assert.Equal(0, gameState.teams[actorTeamId].leyline);
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
    public void C001_3_WhenLeylineIsInsufficient_ShouldThrowAndKeepStateUnchanged()
    {
        var actorPlayerId = new PlayerId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7300, mana: 8, skillPoint: 1);
        var characterInstanceId = new CharacterInstanceId(73001);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.teams[new TeamId(1)] = new TeamState { teamId = new TeamId(1), leyline = 1 };
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

        Assert.Equal("UseSkillActionRequest requires team leyline to be sufficient for skill cost.", exception.Message);
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
    public void C001_1_ShouldOpenOpponentTargetChoiceThenSealSelectedTarget()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7650, mana: 4, skillPoint: 1);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7660, mana: 0, skillPoint: 0);
        var characterInstanceId = new CharacterInstanceId(76501);
        var targetCharacterInstanceId = new CharacterInstanceId(76601);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.characterInstances.Add(characterInstanceId, createCharacter(characterInstanceId, "C001", actorPlayerId));
        gameState.characterInstances.Add(
            targetCharacterInstanceId,
            createCharacter(targetCharacterInstanceId, "C002", targetPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, targetPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId, phaseStepIndex: 1);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 76501,
            actorPlayerId = actorPlayerId,
            characterInstanceId = characterInstanceId,
            skillKey = "C001:1",
        });

        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal("character:C001:1:targetOpponent", gameState.currentInputContext!.contextKey);
        Assert.Contains("player:2", gameState.currentInputContext.choiceKeys);
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Seal"));
        var producedEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 76502,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = "player:2",
        });

        Assert.Contains(
            producedEvents,
            gameEvent =>
                gameEvent is StatusChangedEvent statusChangedEvent &&
                statusChangedEvent.statusKey == "Seal" &&
                statusChangedEvent.targetPlayerId == targetPlayerId &&
                statusChangedEvent.isApplied);
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Seal"));
        Assert.NotNull(gameState.currentActionChain);
        Assert.Null(gameState.currentInputContext);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void C001_1_WhenActivated_ShouldSealTargetAndOpenSpellDamageResponseWindow()
    {
        var actorPlayerId = new PlayerId(1);
        var targetPlayerId = new PlayerId(2);
        var actorPlayerState = createPlayerState(actorPlayerId, new TeamId(1), 7670, mana: 4, skillPoint: 1);
        var targetPlayerState = createPlayerState(targetPlayerId, new TeamId(2), 7680, mana: 0, skillPoint: 0);
        var actorCharacterInstanceId = new CharacterInstanceId(76701);
        var targetCharacterInstanceId = new CharacterInstanceId(76801);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.players.Add(targetPlayerId, targetPlayerState);
        gameState.characterInstances.Add(
            actorCharacterInstanceId,
            createCharacter(actorCharacterInstanceId, "C001", actorPlayerId));
        gameState.characterInstances[actorCharacterInstanceId].isActivated = true;
        gameState.characterInstances.Add(
            targetCharacterInstanceId,
            createCharacter(targetCharacterInstanceId, "C002", targetPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addPlayerOwnedZones(gameState, targetPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorPlayerState.teamId);

        var processor = new ActionRequestProcessor();
        processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 76701,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C001:1",
        });

        Assert.NotNull(gameState.currentInputContext);
        var producedEvents = processor.processActionRequest(gameState, new SubmitInputChoiceActionRequest
        {
            requestId = 76702,
            actorPlayerId = actorPlayerId,
            inputContextId = gameState.currentInputContext!.inputContextId,
            choiceKey = "player:2",
        });

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Seal"));
        Assert.Equal(4, gameState.characterInstances[targetCharacterInstanceId].currentHp);
        Assert.NotNull(gameState.currentResponseWindow);
        Assert.Equal("damageResponse", gameState.currentResponseWindow!.windowTypeKey);
        Assert.Equal(targetPlayerId, gameState.currentResponseWindow.currentResponderPlayerId);
        Assert.Equal(targetCharacterInstanceId, gameState.currentResponseWindow.pendingDamageTargetCharacterInstanceId);
        Assert.Equal(3, gameState.currentResponseWindow.pendingDamageBaseDamageValue);
        Assert.Equal("spell", gameState.currentResponseWindow.pendingDamageTypeKey);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is InteractionWindowEvent interactionWindowEvent &&
                         interactionWindowEvent.eventTypeKey == "responseWindowOpened" &&
                         interactionWindowEvent.isOpened);
        Assert.False(gameState.currentActionChain!.isCompleted);

        processor.processActionRequest(gameState, new SubmitResponseActionRequest
        {
            requestId = 76703,
            actorPlayerId = targetPlayerId,
            responseWindowId = gameState.currentResponseWindow.responseWindowId,
            shouldRespond = false,
        });

        Assert.Null(gameState.currentResponseWindow);
        Assert.Equal(1, gameState.characterInstances[targetCharacterInstanceId].currentHp);
        Assert.True(gameState.currentActionChain!.isCompleted);
    }

    [Fact]
    public void C001_3_WhenAnomalyWasAlreadyResolvedThisTurn_ShouldRejectWithoutPayingOrActivating()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7690, mana: 6, skillPoint: 1);
        var actorCharacterInstanceId = new CharacterInstanceId(76901);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 2 };
        gameState.characterInstances.Add(
            actorCharacterInstanceId,
            createCharacter(actorCharacterInstanceId, "C001", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);
        gameState.turnState!.hasResolvedAnomalyThisTurn = true;

        var processor = new ActionRequestProcessor();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            processor.processActionRequest(gameState, new UseSkillActionRequest
            {
                requestId = 76901,
                actorPlayerId = actorPlayerId,
                characterInstanceId = actorCharacterInstanceId,
                skillKey = "C001:3",
            }));

        Assert.Equal(
            "UseSkillActionRequest C001:3 cannot be used after an anomaly has already been resolved this turn.",
            exception.Message);
        Assert.Equal(6, actorPlayerState.mana);
        Assert.Equal(1, actorPlayerState.skillPoint);
        Assert.Equal(2, gameState.teams[actorTeamId].leyline);
        Assert.False(gameState.characterInstances[actorCharacterInstanceId].isActivated);
    }

    [Fact]
    public void C001_4_WhenDeckHasFewerThanThreeCards_ShouldApplyBarrierAndDefensePlaceAllAvailableCards()
    {
        var actorPlayerId = new PlayerId(1);
        var actorTeamId = new TeamId(1);
        var actorPlayerState = createPlayerState(actorPlayerId, actorTeamId, 7720, mana: 7, skillPoint: 1);
        var actorCharacterInstanceId = new CharacterInstanceId(77201);
        var firstDeckCardId = new CardInstanceId(77211);
        var secondDeckCardId = new CardInstanceId(77212);

        var gameState = new RuleCore.GameState.GameState();
        gameState.players.Add(actorPlayerId, actorPlayerState);
        gameState.teams[actorTeamId] = new TeamState { teamId = actorTeamId, leyline = 3 };
        gameState.characterInstances.Add(
            actorCharacterInstanceId,
            createCharacter(actorCharacterInstanceId, "C001", actorPlayerId));
        addPlayerOwnedZones(gameState, actorPlayerState);
        addDeckTreasureCard(gameState, actorPlayerState, firstDeckCardId, "T001");
        addDeckTreasureCard(gameState, actorPlayerState, secondDeckCardId, "T002");
        setRunningTurnForPlayer(gameState, actorPlayerId, actorTeamId);

        var processor = new ActionRequestProcessor();
        var producedEvents = processor.processActionRequest(gameState, new UseSkillActionRequest
        {
            requestId = 77201,
            actorPlayerId = actorPlayerId,
            characterInstanceId = actorCharacterInstanceId,
            skillKey = "C001:4",
        });

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, actorCharacterInstanceId, "Barrier"));
        Assert.Empty(gameState.zones[actorPlayerState.deckZoneId].cardInstanceIds);
        Assert.Equal(2, gameState.zones[actorPlayerState.fieldZoneId].cardInstanceIds.Count);
        Assert.All(
            new[] { firstDeckCardId, secondDeckCardId },
            cardInstanceId =>
            {
                Assert.Equal(actorPlayerState.fieldZoneId, gameState.cardInstances[cardInstanceId].zoneId);
                Assert.True(gameState.cardInstances[cardInstanceId].isDefensePlacedOnField);
            });
        Assert.Equal(0, actorPlayerState.mana);
        Assert.Equal(0, actorPlayerState.skillPoint);
        Assert.Equal(0, gameState.teams[actorTeamId].leyline);
        Assert.Contains(
            producedEvents,
            gameEvent => gameEvent is StatusChangedEvent statusChangedEvent &&
                         statusChangedEvent.statusKey == "Barrier" &&
                         statusChangedEvent.targetCharacterInstanceId == actorCharacterInstanceId);
        Assert.Equal(
            2,
            producedEvents.Count(gameEvent =>
                gameEvent is CardMovedEvent cardMovedEvent &&
                cardMovedEvent.moveReason == CardMoveReason.defenseLikePlace));
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

    private static void addHandTreasureCard(
        RuleCore.GameState.GameState gameState,
        PlayerState playerState,
        CardInstanceId cardInstanceId,
        string definitionId)
    {
        gameState.zones[playerState.handZoneId].cardInstanceIds.Add(cardInstanceId);
        gameState.cardInstances[cardInstanceId] = new CardInstance
        {
            cardInstanceId = cardInstanceId,
            definitionId = definitionId,
            ownerPlayerId = playerState.playerId,
            zoneId = playerState.handZoneId,
            zoneKey = ZoneKey.hand,
            isFaceUp = false,
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

