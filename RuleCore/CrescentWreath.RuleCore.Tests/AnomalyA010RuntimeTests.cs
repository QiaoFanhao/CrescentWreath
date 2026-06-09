using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public sealed class AnomalyA010RuntimeTests
{
    [Fact]
    public void ArrivalSetAside_ShouldMoveSelectedCardToCharacterSetAsideFaceDown()
    {
        var gameState = createA010GameState();
        var runtime = new AnomalyA010Runtime(new ZoneMovementService(), nextInputId);
        var actionChainState = createActionChain(new PlayerId(1));

        var opened = runtime.tryOpenArrivalSetAsideInput(gameState, actionChainState, 1);

        Assert.True(opened);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(new PlayerId(1), gameState.currentInputContext!.requiredPlayerId);

        var selectedChoiceKey = gameState.currentInputContext.choiceKeys.First();
        var request = new SubmitInputChoiceActionRequest
        {
            requestId = 2,
            actorPlayerId = new PlayerId(1),
            inputContextId = gameState.currentInputContext.inputContextId,
            choiceKey = selectedChoiceKey,
        };
        runtime.ensureValidArrivalSetAsideChoice(gameState, gameState.currentInputContext, request);
        var closedInput = gameState.currentInputContext;
        gameState.currentInputContext = null;

        runtime.continueArrivalSetAside(gameState, actionChainState, closedInput, request);

        var setAsideZone = gameState.zones[gameState.players[new PlayerId(1)].characterSetAsideZoneId];
        Assert.Single(setAsideZone.cardInstanceIds);
        var setAsideCard = gameState.cardInstances[setAsideZone.cardInstanceIds[0]];
        Assert.True(setAsideCard.isSetAside);
        Assert.False(setAsideCard.isFaceUp);
        Assert.Equal(ZoneKey.characterSetAside, setAsideCard.zoneKey);
        Assert.NotNull(gameState.currentInputContext);
        Assert.Equal(new PlayerId(2), gameState.currentInputContext!.requiredPlayerId);
    }

    [Fact]
    public void FateStayNightActive_ShouldBlockAnomalyDeckMutation()
    {
        var gameState = createA010GameState();

        Assert.True(AnomalyA010Runtime.shouldBlockAnomalyDeckMutation(gameState));

        gameState.resolvedAnomalyDefinitionIds.Add("A010");

        Assert.False(AnomalyA010Runtime.shouldBlockAnomalyDeckMutation(gameState));
        Assert.True(AnomalyA010Runtime.shouldSkipT017ForcedFateStayNight(gameState));
    }

    [Fact]
    public void KillBanishFlow_WhenKillerTeamClearsSetAside_ShouldResolveAndApplyTwoRewards()
    {
        var gameState = createA010GameState();
        var runtime = new AnomalyA010Runtime(new ZoneMovementService(), nextInputId);
        var actionChainState = createActionChain(new PlayerId(1));
        runtime.tryOpenArrivalSetAsideInput(gameState, actionChainState, 1);
        while (gameState.currentInputContext is not null)
        {
            var inputContext = gameState.currentInputContext;
            var request = new SubmitInputChoiceActionRequest
            {
                requestId = 10 + inputContext.requiredPlayerId!.Value.Value,
                actorPlayerId = inputContext.requiredPlayerId.Value,
                inputContextId = inputContext.inputContextId,
                choiceKey = inputContext.choiceKeys[0],
            };
            runtime.ensureValidArrivalSetAsideChoice(gameState, inputContext, request);
            gameState.currentInputContext = null;
            runtime.continueArrivalSetAside(gameState, actionChainState, inputContext, request);
        }

        actionChainState.producedEvents.Add(new KillRecordedEvent
        {
            eventId = 20,
            eventTypeKey = "killRecorded",
            killerPlayerId = new PlayerId(1),
        });
        Assert.True(runtime.tryOpenKillBanishSetAsideInputFromProducedEvents(
            gameState,
            actionChainState,
            20,
            actionChainState.producedEvents.Count - 1));
        submitCurrentSingleChoice(gameState, runtime, actionChainState, 21, isKillChoice: true);
        Assert.Null(gameState.currentInputContext);

        actionChainState.producedEvents.Add(new KillRecordedEvent
        {
            eventId = 22,
            eventTypeKey = "killRecorded",
            killerPlayerId = new PlayerId(1),
        });
        Assert.True(runtime.tryOpenKillBanishSetAsideInputFromProducedEvents(
            gameState,
            actionChainState,
            22,
            actionChainState.producedEvents.Count - 1));
        submitCurrentSingleChoice(gameState, runtime, actionChainState, 23, isKillChoice: true);

        Assert.Equal(AnomalyA010Runtime.ContextKeyRewardChooseTwo, gameState.currentInputContext!.contextKey);
        var rewardInput = gameState.currentInputContext;
        var rewardRequest = new SubmitInputChoiceActionRequest
        {
            requestId = 24,
            actorPlayerId = new PlayerId(1),
            inputContextId = rewardInput.inputContextId,
        };
        rewardRequest.choiceKeys.Add(AnomalyA010Runtime.ChoiceRewardLeyline);
        rewardRequest.choiceKeys.Add(AnomalyA010Runtime.ChoiceRewardKillScore);
        AnomalyA010Runtime.ensureValidRewardChooseTwoChoice(rewardInput, rewardRequest);
        gameState.currentInputContext = null;
        runtime.continueRewardChooseTwo(
            gameState,
            actionChainState,
            rewardInput,
            rewardRequest,
            () =>
            {
                gameState.currentAnomalyState!.currentAnomalyDefinitionId = null;
                return false;
            });

        Assert.Equal(3, gameState.teams[new TeamId(1)].leyline);
        Assert.Equal(11, gameState.teams[new TeamId(1)].killScore);
        Assert.Contains("A010", gameState.resolvedAnomalyDefinitionIds);
        Assert.Equal(4, gameState.zones[gameState.publicState!.gapZoneId].cardInstanceIds.Count);
        Assert.All(
            gameState.players.Values,
            player => Assert.Empty(gameState.zones[player.characterSetAsideZoneId].cardInstanceIds));
    }

    private static void submitCurrentSingleChoice(
        RuleCore.GameState.GameState gameState,
        AnomalyA010Runtime runtime,
        ActionChainState actionChainState,
        long requestId,
        bool isKillChoice)
    {
        var inputContext = gameState.currentInputContext!;
        var request = new SubmitInputChoiceActionRequest
        {
            requestId = requestId,
            actorPlayerId = inputContext.requiredPlayerId!.Value,
            inputContextId = inputContext.inputContextId,
            choiceKey = inputContext.choiceKeys[0],
        };
        runtime.ensureValidKillBanishSetAsideChoice(gameState, inputContext, request);
        gameState.currentInputContext = null;
        runtime.continueKillBanishSetAside(gameState, actionChainState, inputContext, request);
        _ = isKillChoice;
    }

    private static long nextInputId()
    {
        return ++nextInputContextNumericId;
    }

    private static long nextInputContextNumericId;

    private static ActionChainState createActionChain(PlayerId actorPlayerId)
    {
        return new ActionChainState
        {
            actionChainId = new ActionChainId(1),
            actorPlayerId = actorPlayerId,
            isCompleted = false,
        };
    }

    private static RuleCore.GameState.GameState createA010GameState()
    {
        var gameState = new RuleCore.GameState.GameState
        {
            matchState = MatchState.running,
            turnState = new TurnState
            {
                turnNumber = 1,
                currentPlayerId = new PlayerId(1),
                currentTeamId = new TeamId(1),
                currentPhase = TurnPhase.action,
            },
            publicState = new PublicState
            {
                publicTreasureDeckZoneId = new ZoneId(9001),
                summonZoneId = new ZoneId(9002),
                gapZoneId = new ZoneId(9003),
                sakuraCakeDeckZoneId = new ZoneId(9004),
                anomalyDeckZoneId = new ZoneId(9005),
            },
            currentAnomalyState = new CurrentAnomalyState
            {
                currentAnomalyDefinitionId = "A010",
            },
        };

        addZone(gameState, gameState.publicState.publicTreasureDeckZoneId, ZoneKey.publicTreasureDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.summonZoneId, ZoneKey.summonZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.gapZoneId, ZoneKey.gapZone, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.sakuraCakeDeckZoneId, ZoneKey.sakuraCakeDeck, null, ZonePublicOrPrivate.publicZone);
        addZone(gameState, gameState.publicState.anomalyDeckZoneId, ZoneKey.anomalyDeck, null, ZonePublicOrPrivate.publicZone);

        addPlayer(gameState, new PlayerId(1), new TeamId(1), 1000, new CharacterInstanceId(101), new CardInstanceId(10001));
        addPlayer(gameState, new PlayerId(2), new TeamId(2), 2000, new CharacterInstanceId(102), new CardInstanceId(10002));
        addPlayer(gameState, new PlayerId(3), new TeamId(1), 3000, new CharacterInstanceId(103), new CardInstanceId(10003));
        addPlayer(gameState, new PlayerId(4), new TeamId(2), 4000, new CharacterInstanceId(104), new CardInstanceId(10004));

        var team1 = new TeamState { teamId = new TeamId(1), killScore = 10 };
        team1.memberPlayerIds.Add(new PlayerId(1));
        team1.memberPlayerIds.Add(new PlayerId(3));
        gameState.teams[new TeamId(1)] = team1;

        var team2 = new TeamState { teamId = new TeamId(2), killScore = 10 };
        team2.memberPlayerIds.Add(new PlayerId(2));
        team2.memberPlayerIds.Add(new PlayerId(4));
        gameState.teams[new TeamId(2)] = team2;

        return gameState;
    }

    private static void addPlayer(
        RuleCore.GameState.GameState gameState,
        PlayerId playerId,
        TeamId teamId,
        long zoneBase,
        CharacterInstanceId characterInstanceId,
        CardInstanceId handCardInstanceId)
    {
        var playerState = new PlayerState
        {
            playerId = playerId,
            teamId = teamId,
            deckZoneId = new ZoneId(zoneBase),
            handZoneId = new ZoneId(zoneBase + 1),
            discardZoneId = new ZoneId(zoneBase + 2),
            fieldZoneId = new ZoneId(zoneBase + 3),
            characterSetAsideZoneId = new ZoneId(zoneBase + 4),
            activeCharacterInstanceId = characterInstanceId,
        };
        gameState.players[playerId] = playerState;
        addZone(gameState, playerState.deckZoneId, ZoneKey.deck, playerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, playerState.handZoneId, ZoneKey.hand, playerId, ZonePublicOrPrivate.privateZone);
        addZone(gameState, playerState.discardZoneId, ZoneKey.discard, playerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, playerState.fieldZoneId, ZoneKey.field, playerId, ZonePublicOrPrivate.publicZone);
        addZone(gameState, playerState.characterSetAsideZoneId, ZoneKey.characterSetAside, playerId, ZonePublicOrPrivate.privateZone);

        gameState.characterInstances[characterInstanceId] = new CharacterInstance
        {
            characterInstanceId = characterInstanceId,
            definitionId = "test:character",
            ownerPlayerId = playerId,
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };

        gameState.cardInstances[handCardInstanceId] = new CardInstance
        {
            cardInstanceId = handCardInstanceId,
            definitionId = "T001",
            ownerPlayerId = playerId,
            zoneId = playerState.handZoneId,
            zoneKey = ZoneKey.hand,
            isFaceUp = true,
        };
        gameState.zones[playerState.handZoneId].cardInstanceIds.Add(handCardInstanceId);
    }

    private static void addZone(
        RuleCore.GameState.GameState gameState,
        ZoneId zoneId,
        ZoneKey zoneKey,
        PlayerId? ownerPlayerId,
        ZonePublicOrPrivate publicOrPrivate)
    {
        gameState.zones[zoneId] = new ZoneState
        {
            zoneId = zoneId,
            zoneType = zoneKey,
            ownerPlayerId = ownerPlayerId,
            publicOrPrivate = publicOrPrivate,
        };
    }
}
