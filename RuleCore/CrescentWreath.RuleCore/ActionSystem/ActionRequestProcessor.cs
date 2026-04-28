using System;
using System.Collections.Generic;
using System.Threading;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class ActionRequestProcessor
{
    private const string ContinuationKeyStagedResponseDamage = "continuation:stagedResponseDamage";
    private const string DefenseTypeKeyFixedReduce1 = "fixedReduce1";
    private const string DefenseTypeKeyPhysical = "physical";
    private const string DefenseTypeKeySpell = "spell";
    private const string DefenseTypeKeyDual = "dual";
    private const string DamageTypeKeyPhysical = "physical";
    private const string DamageTypeKeySpell = "spell";
    private const string DamageTypeKeyDirect = "direct";
    private const string DefenseDeclarationPrefixCard = "cardDefense:";
    private const string DamageResponseStageAwaitDefense = "awaitDefense";
    private const string DamageResponseStageAwaitCounter = "awaitCounter";
    private const string DamageCounterTypeKeyCancelFixedReduce1 = "cancelFixedReduce1";
    private const string SkillKeyC002_2 = "C002:2";
    private const string SkillKeyC004_1 = "C004:1";
    private const string SkillKeyC018_2 = "C018:2";
    private const string SkillKeyC021_1 = "C021:1";
    private const string SkillKeyC029_4 = "C029:4";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeyPenetrate = "Penetrate";

    private enum DamageResponseStage
    {
        awaitDefense = 0,
        awaitCounter = 1,
    }

    private readonly struct SubmitInputChoiceContinuationState
    {
        public readonly string? pendingContinuationKey;
        public readonly bool isInputChoiceDamageContinuation;
        public readonly bool isEndPhaseHandDiscardContinuation;
        public readonly bool isTurnStartShackleDiscardContinuation;
        public readonly bool isAnomalyContinuation;

        public SubmitInputChoiceContinuationState(string? pendingContinuationKey)
        {
            this.pendingContinuationKey = pendingContinuationKey;
            isInputChoiceDamageContinuation =
                pendingContinuationKey == TemporaryOnPlayProbeResolver.ContinuationKeyInputChoiceDamage;
            isEndPhaseHandDiscardContinuation =
                pendingContinuationKey == EndPhaseProcessor.ContinuationKeyEndPhaseHandDiscard;
            isTurnStartShackleDiscardContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartShackleDiscard;
            isAnomalyContinuation = AnomalyProcessor.isAnomalyContinuationKey(pendingContinuationKey);
        }
    }

    private readonly ZoneMovementService zoneMovementService;
    private readonly DamageProcessor damageProcessor;
    private readonly TurnTransitionProcessor turnTransitionProcessor;
    private readonly EndPhaseProcessor endPhaseProcessor;
    private readonly AnomalyProcessor anomalyProcessor;
    private long nextResponseWindowNumericId;
    private long nextInputContextNumericId;

    public ActionRequestProcessor()
        : this(new ZoneMovementService(), new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService)
        : this(zoneMovementService, new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService, DamageProcessor damageProcessor)
    {
        this.zoneMovementService = zoneMovementService;
        this.damageProcessor = damageProcessor;
        Func<long> nextInputContextIdSupplier = () => Interlocked.Increment(ref nextInputContextNumericId);
        endPhaseProcessor = new EndPhaseProcessor(
            zoneMovementService,
            nextInputContextIdSupplier);
        turnTransitionProcessor = new TurnTransitionProcessor(
            zoneMovementService,
            endPhaseProcessor,
            nextInputContextIdSupplier);
        anomalyProcessor = new AnomalyProcessor(
            zoneMovementService,
            nextInputContextIdSupplier);
    }

    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        ensureCanAcceptExternalActionRequest(gameState);

        if (actionRequest is PlayTreasureCardActionRequest playTreasureCardActionRequest)
        {
            return processPlayTreasureCardActionRequest(gameState, playTreasureCardActionRequest);
        }

        if (actionRequest is SummonTreasureCardActionRequest summonTreasureCardActionRequest)
        {
            return processSummonTreasureCardActionRequest(gameState, summonTreasureCardActionRequest);
        }

        if (actionRequest is DrawOneCardActionRequest drawOneCardActionRequest)
        {
            return processDrawOneCardActionRequest(gameState, drawOneCardActionRequest);
        }

        if (actionRequest is UseSkillActionRequest useSkillActionRequest)
        {
            return processUseSkillActionRequest(gameState, useSkillActionRequest);
        }

        if (actionRequest is EnterActionPhaseActionRequest enterActionPhaseActionRequest)
        {
            return processEnterActionPhaseActionRequest(gameState, enterActionPhaseActionRequest);
        }

        if (actionRequest is EnterSummonPhaseActionRequest enterSummonPhaseActionRequest)
        {
            return processEnterSummonPhaseActionRequest(gameState, enterSummonPhaseActionRequest);
        }

        if (actionRequest is EnterEndPhaseActionRequest enterEndPhaseActionRequest)
        {
            return processEnterEndPhaseActionRequest(gameState, enterEndPhaseActionRequest);
        }

        if (actionRequest is StartNextTurnActionRequest startNextTurnActionRequest)
        {
            return processStartNextTurnActionRequest(gameState, startNextTurnActionRequest);
        }

        if (actionRequest is TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
        {
            return processTryResolveAnomalyActionRequest(gameState, tryResolveAnomalyActionRequest);
        }

        if (actionRequest is SubmitDefenseActionRequest submitDefenseActionRequest)
        {
            return processSubmitDefenseActionRequest(gameState, submitDefenseActionRequest);
        }

        if (actionRequest is SubmitDamageCounterActionRequest submitDamageCounterActionRequest)
        {
            return processSubmitDamageCounterActionRequest(gameState, submitDamageCounterActionRequest);
        }

        if (actionRequest is SubmitResponseActionRequest submitResponseActionRequest)
        {
            return processSubmitResponseActionRequest(gameState, submitResponseActionRequest);
        }

        if (actionRequest is OpenDamageResponseWindowActionRequest openDamageResponseWindowActionRequest)
        {
            return processOpenDamageResponseWindowActionRequest(gameState, openDamageResponseWindowActionRequest);
        }

        if (actionRequest is OpenInputContextActionRequest openInputContextActionRequest)
        {
            return processOpenInputContextActionRequest(gameState, openInputContextActionRequest);
        }

        if (actionRequest is SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
        {
            return processSubmitInputChoiceActionRequest(gameState, submitInputChoiceActionRequest);
        }

        throw new NotSupportedException("Only PlayTreasureCardActionRequest, SummonTreasureCardActionRequest, DrawOneCardActionRequest, UseSkillActionRequest, EnterActionPhaseActionRequest, EnterSummonPhaseActionRequest, EnterEndPhaseActionRequest, StartNextTurnActionRequest, TryResolveAnomalyActionRequest, SubmitDefenseActionRequest, SubmitDamageCounterActionRequest, SubmitResponseActionRequest, OpenDamageResponseWindowActionRequest, OpenInputContextActionRequest, and SubmitInputChoiceActionRequest are supported in current RuleCore flow.");
    }

    private static void ensureCanAcceptExternalActionRequest(GameState.GameState gameState)
    {
        if (gameState.matchState == GameState.MatchState.ended)
        {
            throw new InvalidOperationException("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.");
        }
    }

    private List<GameEvent> processPlayTreasureCardActionRequest(
        GameState.GameState gameState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        ensurePlayTreasureCardActionRequestGuard(gameState, playTreasureCardActionRequest);
        var playMoveReason = resolvePlayTreasureCardMoveReason(playTreasureCardActionRequest.playMode);

        var cardInstance = gameState.cardInstances[playTreasureCardActionRequest.cardInstanceId];
        var actorPlayerState = gameState.players[playTreasureCardActionRequest.actorPlayerId];
        var sourceZoneState = gameState.zones[cardInstance.zoneId];
        var targetZoneState = gameState.zones[actorPlayerState.fieldZoneId];

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(playTreasureCardActionRequest.requestId),
            actorPlayerId = playTreasureCardActionRequest.actorPlayerId,
            rootActionRequest = playTreasureCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "moveCard",
            movingCardInstanceId = playTreasureCardActionRequest.cardInstanceId,
            fromZoneKey = sourceZoneState.zoneType,
            toZoneKey = targetZoneState.zoneType,
            moveReason = playMoveReason,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            actorPlayerState.fieldZoneId,
            playMoveReason,
            actionChainState.actionChainId,
            playTreasureCardActionRequest.requestId);

        actionChainState.currentFrameIndex = 1;
        actionChainState.producedEvents.Add(cardMovedEvent);
        if (cardInstance.zoneId == actorPlayerState.fieldZoneId)
        {
            cardInstance.isDefensePlacedOnField = playMoveReason == CardMoveReason.defensePlace;
        }

        if (playMoveReason == CardMoveReason.play && cardInstance.zoneId == actorPlayerState.fieldZoneId)
        {
            actorPlayerState.mana += TreasureResourceValueResolver.resolveManaGainOnEnterField(cardInstance.definitionId);
            actorPlayerState.sigilPreview += TreasureResourceValueResolver.resolveSigilPreviewGainOnEnterField(cardInstance.definitionId);
        }

        appendScriptedOnPlayEffectEvents(gameState, actionChainState, playTreasureCardActionRequest, cardInstance);
        if (gameState.currentInputContext is null && gameState.currentResponseWindow is null)
        {
            actionChainState.isCompleted = true;
        }

        return actionChainState.producedEvents;
    }

    private static CardMoveReason resolvePlayTreasureCardMoveReason(string playMode)
    {
        if (playMode == "normal" || playMode == "play")
        {
            return CardMoveReason.play;
        }

        if (playMode == "defense")
        {
            return CardMoveReason.defensePlace;
        }

        throw new InvalidOperationException("PlayTreasureCardActionRequest playMode must be normal or defense (legacy play is still accepted).");
    }

    private List<GameEvent> processSummonTreasureCardActionRequest(
        GameState.GameState gameState,
        SummonTreasureCardActionRequest summonTreasureCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.summon)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.turnState.currentPhase to be summon.");
        }

        if (summonTreasureCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.publicState to be initialized.");
        }

        if (!gameState.cardInstances.TryGetValue(summonTreasureCardActionRequest.cardInstanceId, out var cardInstance))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires cardInstanceId to exist in gameState.cardInstances.");
        }

        var isSummonZoneSource = cardInstance.zoneId == gameState.publicState.summonZoneId;
        var isSakuraCakeDeckSource = cardInstance.zoneId == gameState.publicState.sakuraCakeDeckZoneId;
        if (!isSummonZoneSource && !isSakuraCakeDeckSource)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires cardInstance.zoneId to be in gameState.publicState.summonZoneId or gameState.publicState.sakuraCakeDeckZoneId.");
        }

        var actorPlayerState = gameState.players[summonTreasureCardActionRequest.actorPlayerId];
        if (!actorPlayerState.isSigilLocked)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player sigil to be locked.");
        }

        if (!actorPlayerState.lockedSigil.HasValue)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player lockedSigil to be initialized.");
        }

        var summonSigilCost = TreasureResourceValueResolver.resolveSummonSigilCost(cardInstance.definitionId);
        if (actorPlayerState.lockedSigil.Value < summonSigilCost)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player lockedSigil to be sufficient for summon cost.");
        }

        if (!gameState.zones.TryGetValue(cardInstance.zoneId, out var sourceZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires summon source zone to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var targetZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor discard zone to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires publicTreasureDeck zone to exist in gameState.zones.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(summonTreasureCardActionRequest.requestId),
            actorPlayerId = summonTreasureCardActionRequest.actorPlayerId,
            rootActionRequest = summonTreasureCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "summonTreasure",
            movingCardInstanceId = summonTreasureCardActionRequest.cardInstanceId,
            fromZoneKey = sourceZoneState.zoneType,
            toZoneKey = targetZoneState.zoneType,
            moveReason = CardMoveReason.summon,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;
        actorPlayerState.lockedSigil = actorPlayerState.lockedSigil.Value - summonSigilCost;

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            summonTreasureCardActionRequest.requestId);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.producedEvents.Add(cardMovedEvent);

        if (isSummonZoneSource && publicTreasureDeckZoneState.cardInstanceIds.Count > 0)
        {
            var topPublicTreasureCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
            var topPublicTreasureCardInstance = gameState.cardInstances[topPublicTreasureCardInstanceId];
            var refillSummonZoneEvent = zoneMovementService.moveCard(
                gameState,
                topPublicTreasureCardInstance,
                gameState.publicState.summonZoneId,
                CardMoveReason.reveal,
                actionChainState.actionChainId,
                summonTreasureCardActionRequest.requestId);
            actionChainState.producedEvents.Add(refillSummonZoneEvent);
        }

        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processDrawOneCardActionRequest(
        GameState.GameState gameState,
        DrawOneCardActionRequest drawOneCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (drawOneCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (!gameState.players.TryGetValue(drawOneCardActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.handZoneId))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's discardZoneId to exist in gameState.zones.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(drawOneCardActionRequest.requestId),
            actorPlayerId = drawOneCardActionRequest.actorPlayerId,
            rootActionRequest = drawOneCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        actionChainState.effectFrames.Add(new EffectFrame
        {
            effectKey = "drawOneCard",
            sourcePlayerId = drawOneCardActionRequest.actorPlayerId,
            fromZoneKey = ZoneKey.deck,
            toZoneKey = ZoneKey.hand,
            moveReason = CardMoveReason.draw,
        });
        gameState.currentActionChain = actionChainState;

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardIdsInCurrentOrder = new List<CardInstanceId>(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
            {
                var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    actorPlayerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    drawOneCardActionRequest.requestId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count > 0)
        {
            var topCardInstanceId = deckZoneState.cardInstanceIds[0];
            var topCardInstance = gameState.cardInstances[topCardInstanceId];
            var drawEvent = zoneMovementService.moveCard(
                gameState,
                topCardInstance,
                actorPlayerState.handZoneId,
                CardMoveReason.draw,
                actionChainState.actionChainId,
                drawOneCardActionRequest.requestId);
            actionChainState.producedEvents.Add(drawEvent);
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processUseSkillActionRequest(
        GameState.GameState gameState,
        UseSkillActionRequest useSkillActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.turnState to be initialized.");
        }

        if (useSkillActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("UseSkillActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.action)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.turnState.currentPhase to be action.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (!gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var characterInstance))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires characterInstanceId to exist in gameState.characterInstances.");
        }

        if (characterInstance.ownerPlayerId != useSkillActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires characterInstance.ownerPlayerId to equal actorPlayerId.");
        }

        var (manaCost, skillPointCost) = resolveUseSkillResourceCost(
            characterInstance.definitionId,
            useSkillActionRequest.skillKey);
        var (availableMana, availableSkillPoint) =
            computeUseSkillResourceSnapshot(actorPlayerState);

        if (availableMana < manaCost)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player mana to be sufficient for skill cost.");
        }

        if (availableSkillPoint < skillPointCost)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player skillPoint to be sufficient for skill cost.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(useSkillActionRequest.requestId),
            actorPlayerId = useSkillActionRequest.actorPlayerId,
            rootActionRequest = useSkillActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        actionChainState.effectFrames.Add(new EffectFrame
        {
            effectKey = "useSkill",
            sourcePlayerId = useSkillActionRequest.actorPlayerId,
            sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
            contextKey = useSkillActionRequest.skillKey,
        });

        gameState.currentActionChain = actionChainState;
        actorPlayerState.mana = availableMana - manaCost;
        actorPlayerState.skillPoint = availableSkillPoint - skillPointCost;
        appendUseSkillEffectEvents(
            gameState,
            actionChainState,
            useSkillActionRequest,
            actorPlayerState,
            characterInstance);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    private void appendUseSkillEffectEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState,
        CharacterInstance sourceCharacterInstance)
    {
        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_1, StringComparison.Ordinal))
        {
            var appliedStatus = StatusRuntime.applyStatus(
                gameState,
                new StatusInstance
                {
                    statusKey = StatusKeyPenetrate,
                    applierPlayerId = useSkillActionRequest.actorPlayerId,
                    applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    targetPlayerId = useSkillActionRequest.actorPlayerId,
                    stackCount = 1,
                    durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                });
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = appliedStatus.statusKey,
                targetPlayerId = appliedStatus.targetPlayerId,
                isApplied = true,
            });
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_1, StringComparison.Ordinal))
        {
            appendHealOnCharacter(
                actionChainState,
                useSkillActionRequest.requestId,
                sourceCharacterInstance,
                healAmount: 1);
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC002_2, StringComparison.Ordinal))
        {
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }

                if (!tryFindAliveInPlayCharacterInstanceByOwner(
                        gameState,
                        playerState.playerId,
                        out var targetCharacterInstance))
                {
                    continue;
                }

                appendHealOnCharacter(
                    actionChainState,
                    useSkillActionRequest.requestId,
                    targetCharacterInstance,
                    healAmount: 2);
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_2, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }

                if (!tryFindAliveInPlayCharacterInstanceByOwner(
                        gameState,
                        playerState.playerId,
                        out var targetCharacterInstance))
                {
                    continue;
                }

                var damageEvents = damageProcessor.resolveDamage(
                    gameState,
                    new DamageContext
                    {
                        damageContextId = new DamageContextId(useSkillActionRequest.requestId),
                        sourcePlayerId = useSkillActionRequest.actorPlayerId,
                        sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = playerState.playerId,
                        targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                        baseDamageValue = 1,
                        damageType = DamageTypeKeyDirect,
                    });
                actionChainState.producedEvents.AddRange(damageEvents);
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC029_4, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId == actorPlayerState.teamId)
                {
                    continue;
                }

                var appliedStatus = StatusRuntime.applyStatus(
                    gameState,
                    new StatusInstance
                    {
                        statusKey = StatusKeyCharm,
                        applierPlayerId = useSkillActionRequest.actorPlayerId,
                        applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = playerState.playerId,
                        stackCount = 1,
                    });
                actionChainState.producedEvents.Add(new StatusChangedEvent
                {
                    eventId = useSkillActionRequest.requestId,
                    eventTypeKey = "statusChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    statusKey = appliedStatus.statusKey,
                    targetPlayerId = appliedStatus.targetPlayerId,
                    isApplied = true,
                });
            }

            if (sourceCharacterInstance.isAlive && sourceCharacterInstance.isInPlay)
            {
                var damageEvents = damageProcessor.resolveDamage(
                    gameState,
                    new DamageContext
                    {
                        damageContextId = new DamageContextId(useSkillActionRequest.requestId),
                        sourcePlayerId = useSkillActionRequest.actorPlayerId,
                        sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = useSkillActionRequest.actorPlayerId,
                        targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                        baseDamageValue = 1,
                        damageType = DamageTypeKeyDirect,
                    });
                actionChainState.producedEvents.AddRange(damageEvents);
            }
        }
    }

    private void appendDrawOneCardEffectEventsForPlayer(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        GameState.PlayerState playerState,
        long requestId)
    {
        if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(playerState.handZoneId))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player discardZoneId to exist in gameState.zones.");
        }

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardIdsInCurrentOrder = new List<CardInstanceId>(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
            {
                var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    playerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    requestId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count <= 0)
        {
            return;
        }

        var topCardInstanceId = deckZoneState.cardInstanceIds[0];
        var topCardInstance = gameState.cardInstances[topCardInstanceId];
        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topCardInstance,
            playerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(drawEvent);
    }

    private static bool tryFindAliveInPlayCharacterInstanceByOwner(
        GameState.GameState gameState,
        PlayerId ownerPlayerId,
        out CharacterInstance characterInstance)
    {
        CharacterInstance? selectedCharacter = null;
        long selectedCharacterNumericId = long.MaxValue;
        foreach (var candidateCharacterInstance in gameState.characterInstances.Values)
        {
            if (candidateCharacterInstance.ownerPlayerId != ownerPlayerId ||
                !candidateCharacterInstance.isAlive ||
                !candidateCharacterInstance.isInPlay)
            {
                continue;
            }

            if (candidateCharacterInstance.characterInstanceId.Value >= selectedCharacterNumericId)
            {
                continue;
            }

            selectedCharacterNumericId = candidateCharacterInstance.characterInstanceId.Value;
            selectedCharacter = candidateCharacterInstance;
        }

        if (selectedCharacter is null)
        {
            characterInstance = null!;
            return false;
        }

        characterInstance = selectedCharacter;
        return true;
    }

    private static void appendHealOnCharacter(
        ActionChainState actionChainState,
        long requestId,
        CharacterInstance characterInstance,
        int healAmount)
    {
        if (healAmount <= 0)
        {
            return;
        }

        var hpBefore = characterInstance.currentHp;
        var hpAfter = Math.Min(characterInstance.maxHp, hpBefore + healAmount);
        if (hpAfter == hpBefore)
        {
            return;
        }

        characterInstance.currentHp = hpAfter;
        actionChainState.producedEvents.Add(new HpChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "hpChanged",
            sourceActionChainId = actionChainState.actionChainId,
            targetPlayerId = characterInstance.ownerPlayerId,
            targetCharacterInstanceId = characterInstance.characterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        });
    }
    private List<GameEvent> processEnterActionPhaseActionRequest(
        GameState.GameState gameState,
        EnterActionPhaseActionRequest enterActionPhaseActionRequest)
    {
        return turnTransitionProcessor.processEnterActionPhaseActionRequest(
            gameState,
            enterActionPhaseActionRequest);
    }

    private List<GameEvent> processEnterSummonPhaseActionRequest(
        GameState.GameState gameState,
        EnterSummonPhaseActionRequest enterSummonPhaseActionRequest)
    {
        return turnTransitionProcessor.processEnterSummonPhaseActionRequest(
            gameState,
            enterSummonPhaseActionRequest);
    }

    private List<GameEvent> processEnterEndPhaseActionRequest(
        GameState.GameState gameState,
        EnterEndPhaseActionRequest enterEndPhaseActionRequest)
    {
        return endPhaseProcessor.processEnterEndPhaseActionRequest(
            gameState,
            enterEndPhaseActionRequest);
    }

    private List<GameEvent> processStartNextTurnActionRequest(
        GameState.GameState gameState,
        StartNextTurnActionRequest startNextTurnActionRequest)
    {
        return turnTransitionProcessor.processStartNextTurnActionRequest(
            gameState,
            startNextTurnActionRequest);
    }

    private List<GameEvent> processTryResolveAnomalyActionRequest(
        GameState.GameState gameState,
        TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
    {
        return anomalyProcessor.processTryResolveAnomalyActionRequest(
            gameState,
            tryResolveAnomalyActionRequest);
    }

    private static (int manaCost, int skillPointCost) resolveUseSkillResourceCost(
        string characterDefinitionId,
        string skillKey)
    {
        return CharacterSkillCostResolver.resolveSkillCost(characterDefinitionId, skillKey);
    }

    private static (int mana, int skillPoint) computeUseSkillResourceSnapshot(
        GameState.PlayerState actorPlayerState)
    {
        return (actorPlayerState.mana, actorPlayerState.skillPoint);
    }

    private static void ensurePlayTreasureCardActionRequestGuard(
        GameState.GameState gameState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (playTreasureCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.action)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.turnState.currentPhase to be action.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        var cardInstance = gameState.cardInstances[playTreasureCardActionRequest.cardInstanceId];
        if (cardInstance.ownerPlayerId != playTreasureCardActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires cardInstance.ownerPlayerId to equal actorPlayerId.");
        }

        var actorPlayerState = gameState.players[playTreasureCardActionRequest.actorPlayerId];
        if (cardInstance.zoneId != actorPlayerState.handZoneId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires cardInstance.zoneId to equal actor player's handZoneId.");
        }

        var sourceZoneState = gameState.zones[cardInstance.zoneId];
        if (sourceZoneState.zoneType != ZoneKey.hand)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires sourceZoneState.zoneType to be hand.");
        }
    }

    private void appendScriptedOnPlayEffectEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest,
        CardInstance cardInstance)
    {
        var onPlayProbeEffectKind = TemporaryOnPlayProbeResolver.resolveOnPlayProbeEffectKind(cardInstance.definitionId);
        switch (onPlayProbeEffectKind)
        {
            case TemporaryOnPlayProbeResolver.ProbeEffectKind.immediateDamage1:
            {
                var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                    gameState,
                    playTreasureCardActionRequest.actorPlayerId);

                appendResponseWindowAndDamageEventsToExistingActionChain(
                    gameState,
                    actionChainState,
                    playTreasureCardActionRequest.requestId,
                    playTreasureCardActionRequest.actorPlayerId,
                    playTreasureCardActionRequest.cardInstanceId,
                    targetCharacterInstanceId,
                    1);
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.waitResponseDamage1:
            {
                var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                    gameState,
                    playTreasureCardActionRequest.actorPlayerId);

                openDamageResponseWindowWithPendingDamage(
                    gameState,
                    actionChainState,
                    playTreasureCardActionRequest.requestId,
                    playTreasureCardActionRequest.actorPlayerId,
                    playTreasureCardActionRequest.cardInstanceId,
                    null,
                    targetCharacterInstanceId,
                    1,
                    DamageTypeKeyPhysical);
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.chooseDamage1:
            {
                if (gameState.currentInputContext is not null)
                {
                    throw new InvalidOperationException("Scripted on-play choose damage requires currentInputContext to be null.");
                }

                var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
                var inputContextState = new InputContextState
                {
                    inputContextId = inputContextId,
                    requiredPlayerId = playTreasureCardActionRequest.actorPlayerId,
                    sourceActionChainId = actionChainState.actionChainId,
                    inputTypeKey = TemporaryOnPlayProbeResolver.ScriptedOnPlayChoiceInputTypeKey,
                    contextKey = TemporaryOnPlayProbeResolver.ScriptedOnPlayChooseDamageContextKey,
                };
                inputContextState.choiceKeys.Add(TemporaryOnPlayProbeResolver.ScriptedOnPlayDeal1ChoiceKey);
                gameState.currentInputContext = inputContextState;
                actionChainState.pendingContinuationKey = TemporaryOnPlayProbeResolver.ContinuationKeyInputChoiceDamage;

                actionChainState.producedEvents.Add(new InteractionWindowEvent
                {
                    eventId = playTreasureCardActionRequest.requestId,
                    eventTypeKey = "inputContextOpened",
                    sourceActionChainId = actionChainState.actionChainId,
                    windowKindKey = "inputContext",
                    inputContextId = inputContextId,
                    isOpened = true,
                });
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.none:
            default:
                return;
        }
    }

    private CharacterInstanceId findDeterministicScriptedDamageTarget(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        CharacterInstanceId? selectedTargetCharacterInstanceId = null;
        long selectedTargetNumericId = long.MaxValue;

        foreach (var characterInstance in gameState.characterInstances.Values)
        {
            if (!characterInstance.isAlive || !characterInstance.isInPlay)
            {
                continue;
            }

            if (characterInstance.ownerPlayerId == actorPlayerId)
            {
                continue;
            }

            var currentCharacterNumericId = characterInstance.characterInstanceId.Value;
            if (currentCharacterNumericId < selectedTargetNumericId)
            {
                selectedTargetNumericId = currentCharacterNumericId;
                selectedTargetCharacterInstanceId = characterInstance.characterInstanceId;
            }
        }

        if (!selectedTargetCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Scripted on-play damage requires an alive in-play enemy character target.");
        }

        return selectedTargetCharacterInstanceId.Value;
    }

    private void appendResponseWindowAndDamageEventsToExistingActionChain(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue)
    {
        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId)),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
        };
        gameState.currentResponseWindow = responseWindowState;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = true,
        });

        gameState.currentResponseWindow = null;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = false,
        });

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = DamageTypeKeyDirect,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
    }

    private void appendDamageEventsToExistingActionChain(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue)
    {
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = DamageTypeKeyDirect,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
    }

    private void openDamageResponseWindowWithPendingDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId? sourceCharacterInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue,
        string? damageTypeKey)
    {
        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("Staged damage response window cannot open while currentResponseWindow is active.");
        }

        if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var pendingDamageTargetCharacter))
        {
            throw new InvalidOperationException("Staged damage response window requires pending damage target character to exist in gameState.characterInstances.");
        }

        var pendingDamageDefenderPlayerId = pendingDamageTargetCharacter.ownerPlayerId;
        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId)),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
            pendingDamageTargetCharacterInstanceId = targetCharacterInstanceId,
            pendingDamageBaseDamageValue = baseDamageValue,
            pendingDamageSourcePlayerId = sourcePlayerId,
            pendingDamageSourceCardInstanceId = sourceCardInstanceId,
            pendingDamageSourceCharacterInstanceId = sourceCharacterInstanceId,
            pendingDamageTypeKey = normalizePendingDamageTypeKey(damageTypeKey),
            pendingDamageDefenseDeclarationKey = null,
            pendingDamageDefenderPlayerId = pendingDamageDefenderPlayerId,
        };
        setDamageResponseStage(
            responseWindowState,
            DamageResponseStage.awaitDefense,
            pendingDamageDefenderPlayerId);
        gameState.currentResponseWindow = responseWindowState;
        actionChainState.pendingContinuationKey = ContinuationKeyStagedResponseDamage;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = true,
        });
    }

    private List<GameEvent> processOpenDamageResponseWindowActionRequest(
        GameState.GameState gameState,
        OpenDamageResponseWindowActionRequest openDamageResponseWindowActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("OpenDamageResponseWindowActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("OpenDamageResponseWindowActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(openDamageResponseWindowActionRequest.requestId),
            actorPlayerId = openDamageResponseWindowActionRequest.actorPlayerId,
            rootActionRequest = openDamageResponseWindowActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "openDamageResponseWindow",
            sourcePlayerId = openDamageResponseWindowActionRequest.actorPlayerId,
            sourceCardInstanceId = openDamageResponseWindowActionRequest.sourceCardInstanceId,
            sourceCharacterInstanceId = openDamageResponseWindowActionRequest.sourceCharacterInstanceId,
            targetCharacterInstanceId = openDamageResponseWindowActionRequest.targetCharacterInstanceId,
            contextKey = "stagedDamageResponse",
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            openDamageResponseWindowActionRequest.requestId,
            openDamageResponseWindowActionRequest.actorPlayerId,
            openDamageResponseWindowActionRequest.sourceCardInstanceId,
            openDamageResponseWindowActionRequest.sourceCharacterInstanceId,
            openDamageResponseWindowActionRequest.targetCharacterInstanceId,
            openDamageResponseWindowActionRequest.baseDamageValue,
            openDamageResponseWindowActionRequest.damageTypeKey);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processSubmitResponseActionRequest(
        GameState.GameState gameState,
        SubmitResponseActionRequest submitResponseActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires an active currentResponseWindow.");
        }

        if (submitResponseActionRequest.responseWindowId != responseWindowState.responseWindowId)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest responseWindowId mismatch.");
        }

        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        if (submitResponseActionRequest.actorPlayerId != responseWindowState.currentResponderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.");
        }

        if (responseWindowState.windowTypeKey == "damageResponse")
        {
            if (submitResponseActionRequest.shouldRespond &&
                StatusRuntime.hasStatusOnPlayer(gameState, submitResponseActionRequest.actorPlayerId, "Silence"))
            {
                throw new InvalidOperationException("SubmitResponseActionRequest actorPlayerId cannot respond while Silence status is active.");
            }

            if (submitResponseActionRequest.shouldRespond || submitResponseActionRequest.responseKey is not null)
            {
                throw new NotSupportedException("SubmitResponseActionRequest currently only supports no-response continuation (shouldRespond=false, responseKey=null).");
            }

            if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
            }

            var pendingDamageTargetCharacterInstanceId = responseWindowState.pendingDamageTargetCharacterInstanceId;
            if (!pendingDamageTargetCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage target data in currentResponseWindow.");
            }

            var pendingDamageBaseDamageValue = responseWindowState.pendingDamageBaseDamageValue;
            if (!pendingDamageBaseDamageValue.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage value data in currentResponseWindow.");
            }

            var pendingDamageResponseStage = getDamageResponseStageOrThrow(
                responseWindowState,
                "SubmitResponseActionRequest requires valid pending damage response stage data in currentResponseWindow.");

            var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
            if (!pendingDamageDefenderPlayerId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage defender data in currentResponseWindow.");
            }

            var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
            if (!pendingDamageSourcePlayerId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage source player data in currentResponseWindow.");
            }

            ensureDamageResponseStateConsistency(
                responseWindowState,
                pendingDamageResponseStage,
                pendingDamageDefenderPlayerId.Value,
                pendingDamageSourcePlayerId.Value,
                "SubmitResponseActionRequest");

            return closeDamageResponseWindowAndResolveDamage(
                gameState,
                actionChainState,
                responseWindowState,
                submitResponseActionRequest.requestId);
        }

        throw new NotSupportedException("SubmitResponseActionRequest currentResponseWindow.windowTypeKey is not supported.");
    }

    private List<GameEvent> processSubmitDefenseActionRequest(
        GameState.GameState gameState,
        SubmitDefenseActionRequest submitDefenseActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires an active damageResponse currentResponseWindow.");
        }

        if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
        }

        var pendingDamageResponseStage = getDamageResponseStageOrThrow(
            responseWindowState,
            "SubmitDefenseActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitDefense.");
        if (pendingDamageResponseStage != DamageResponseStage.awaitDefense)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitDefense.");
        }

        var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
        if (!pendingDamageDefenderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires pending damage defender data in currentResponseWindow.");
        }

        if (submitDefenseActionRequest.actorPlayerId != pendingDamageDefenderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest actorPlayerId must equal pending damage defender player.");
        }

        if (!gameState.players.TryGetValue(submitDefenseActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires actorPlayerId to exist in gameState.players.");
        }

        var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
        if (!pendingDamageSourcePlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires pending damage source player data in currentResponseWindow.");
        }

        ensureDamageResponseStateConsistency(
            responseWindowState,
            pendingDamageResponseStage,
            pendingDamageDefenderPlayerId.Value,
            pendingDamageSourcePlayerId.Value,
            "SubmitDefenseActionRequest");

        if (StatusRuntime.hasStatusOnPlayer(gameState, submitDefenseActionRequest.actorPlayerId, "Charm"))
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest actorPlayerId cannot defend while Charm status is active.");
        }

        if (submitDefenseActionRequest.defenseTypeKey == DefenseTypeKeyFixedReduce1)
        {
            responseWindowState.pendingDamageDefenseDeclarationKey = DefenseTypeKeyFixedReduce1;
        }
        else
        {
            if (!isFormalDefenseTypeKey(submitDefenseActionRequest.defenseTypeKey))
            {
                throw new NotSupportedException("SubmitDefenseActionRequest currently only supports defenseTypeKey=fixedReduce1.");
            }

            if (!gameState.cardInstances.TryGetValue(submitDefenseActionRequest.defenseCardInstanceId, out var defenseCardInstance))
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defenseCardInstanceId to exist in gameState.cardInstances.");
            }

            if (defenseCardInstance.ownerPlayerId != submitDefenseActionRequest.actorPlayerId)
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defenseCardInstance.ownerPlayerId to equal actorPlayerId.");
            }

            var isDefenseCardInActorHand = defenseCardInstance.zoneId == actorPlayerState.handZoneId;
            var isDefenseCardAlreadyDefensePlacedOnField =
                defenseCardInstance.zoneId == actorPlayerState.fieldZoneId &&
                defenseCardInstance.isDefensePlacedOnField;
            if (!isDefenseCardInActorHand && !isDefenseCardAlreadyDefensePlacedOnField)
            {
                throw new InvalidOperationException(
                    "SubmitDefenseActionRequest requires defense card to be in actor hand zone, or already defense-placed in actor field zone.");
            }

            var definitionDefenseValue = DefenseValueRuntimeResolver.resolveEffectiveDefenseValue(
                gameState,
                defenseCardInstance);
            var definitionDefenseTypeKey = TreasureResourceValueResolver.resolveDefenseTypeKey(defenseCardInstance.definitionId);
            if (!definitionDefenseValue.HasValue || string.IsNullOrWhiteSpace(definitionDefenseTypeKey))
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defense card definition to provide defenseValue and defenseTypeKey.");
            }

            if (submitDefenseActionRequest.defenseTypeKey != definitionDefenseTypeKey)
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest defenseTypeKey must match defense card definitionTypeKey.");
            }

            if (isDefenseCardInActorHand)
            {
                var defensePlaceMovedEvent = zoneMovementService.moveCard(
                    gameState,
                    defenseCardInstance,
                    actorPlayerState.fieldZoneId,
                    CardMoveReason.defensePlace,
                    actionChainState.actionChainId,
                    submitDefenseActionRequest.requestId);
                actionChainState.producedEvents.Add(defensePlaceMovedEvent);
                defenseCardInstance.isDefensePlacedOnField = true;
            }

            responseWindowState.pendingDamageDefenseDeclarationKey = createCardDefenseDeclarationKey(
                definitionDefenseTypeKey,
                definitionDefenseValue.Value);
        }

        setDamageResponseStage(
            responseWindowState,
            DamageResponseStage.awaitCounter,
            pendingDamageSourcePlayerId.Value);
        actionChainState.isCompleted = false;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processSubmitDamageCounterActionRequest(
        GameState.GameState gameState,
        SubmitDamageCounterActionRequest submitDamageCounterActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires an active damageResponse currentResponseWindow.");
        }

        if (submitDamageCounterActionRequest.responseWindowId != responseWindowState.responseWindowId)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest responseWindowId mismatch.");
        }

        if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
        }

        var pendingDamageResponseStage = getDamageResponseStageOrThrow(
            responseWindowState,
            "SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitCounter.");
        if (pendingDamageResponseStage != DamageResponseStage.awaitCounter)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitCounter.");
        }

        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        if (submitDamageCounterActionRequest.actorPlayerId != responseWindowState.currentResponderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.");
        }

        var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
        if (!pendingDamageSourcePlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires pending damage source player data in currentResponseWindow.");
        }

        var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
        if (!pendingDamageDefenderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires pending damage defender data in currentResponseWindow.");
        }

        ensureDamageResponseStateConsistency(
            responseWindowState,
            pendingDamageResponseStage,
            pendingDamageDefenderPlayerId.Value,
            pendingDamageSourcePlayerId.Value,
            "SubmitDamageCounterActionRequest");

        if (submitDamageCounterActionRequest.actorPlayerId != pendingDamageSourcePlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest actorPlayerId must equal pending damage source player.");
        }

        if (submitDamageCounterActionRequest.counterTypeKey != DamageCounterTypeKeyCancelFixedReduce1)
        {
            throw new NotSupportedException("SubmitDamageCounterActionRequest currently only supports counterTypeKey=cancelFixedReduce1.");
        }

        if (responseWindowState.pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be fixedReduce1.");
        }

        responseWindowState.pendingDamageDefenseDeclarationKey = null;
        return closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitDamageCounterActionRequest.requestId);
    }

    private List<GameEvent> closeDamageResponseWindowAndResolveDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        long requestId)
    {
        var pendingDamageTargetCharacterInstanceId = responseWindowState.pendingDamageTargetCharacterInstanceId;
        if (!pendingDamageTargetCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Damage response resolution requires pending damage target data in currentResponseWindow.");
        }

        var pendingDamageBaseDamageValue = responseWindowState.pendingDamageBaseDamageValue;
        if (!pendingDamageBaseDamageValue.HasValue)
        {
            throw new InvalidOperationException("Damage response resolution requires pending damage value data in currentResponseWindow.");
        }

        var pendingDamageTypeKey = normalizePendingDamageTypeKey(responseWindowState.pendingDamageTypeKey);
        var defenseReduction = resolveDamageReductionFromWindowState(responseWindowState, pendingDamageTypeKey);
        var effectiveDamage = Math.Max(0, pendingDamageBaseDamageValue.Value - defenseReduction);

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = false,
        });

        gameState.currentResponseWindow = null;

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = responseWindowState.pendingDamageSourcePlayerId,
            sourceCardInstanceId = responseWindowState.pendingDamageSourceCardInstanceId,
            sourceCharacterInstanceId = responseWindowState.pendingDamageSourceCharacterInstanceId,
            targetCharacterInstanceId = pendingDamageTargetCharacterInstanceId.Value,
            baseDamageValue = effectiveDamage,
            damageType = pendingDamageTypeKey,
            defenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
        actionChainState.pendingContinuationKey = null;
        actionChainState.isCompleted = true;

        return actionChainState.producedEvents;
    }

    private static DamageResponseStage getDamageResponseStageOrThrow(
        ResponseWindowState responseWindowState,
        string invalidStageMessage)
    {
        return responseWindowState.pendingDamageResponseStageKey switch
        {
            DamageResponseStageAwaitDefense => DamageResponseStage.awaitDefense,
            DamageResponseStageAwaitCounter => DamageResponseStage.awaitCounter,
            _ => throw new InvalidOperationException(invalidStageMessage),
        };
    }

    private static void setDamageResponseStage(
        ResponseWindowState responseWindowState,
        DamageResponseStage stage,
        PlayerId responderPlayerId)
    {
        responseWindowState.pendingDamageResponseStageKey = stage switch
        {
            DamageResponseStage.awaitDefense => DamageResponseStageAwaitDefense,
            DamageResponseStage.awaitCounter => DamageResponseStageAwaitCounter,
            _ => throw new InvalidOperationException("Unsupported damageResponse stage."),
        };
        responseWindowState.currentResponderPlayerId = responderPlayerId;
    }

    private static void ensureDamageResponseStateConsistency(
        ResponseWindowState responseWindowState,
        DamageResponseStage stage,
        PlayerId pendingDamageDefenderPlayerId,
        PlayerId pendingDamageSourcePlayerId,
        string actionRequestTypeKey)
    {
        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        var pendingDamageDefenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey;
        if (pendingDamageDefenseDeclarationKey is not null &&
            pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1 &&
            !tryParseCardDefenseDeclarationKey(
                pendingDamageDefenseDeclarationKey,
                out _,
                out _))
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be null, fixedReduce1, or a supported card defense declaration.");
        }

        if (stage == DamageResponseStage.awaitDefense)
        {
            if (responseWindowState.currentResponderPlayerId.Value != pendingDamageDefenderPlayerId)
            {
                throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to match pending damage defender while stage is awaitDefense.");
            }

            if (pendingDamageDefenseDeclarationKey is not null)
            {
                throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be null while stage is awaitDefense.");
            }

            return;
        }

        if (responseWindowState.currentResponderPlayerId.Value != pendingDamageSourcePlayerId)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to match pending damage source player while stage is awaitCounter.");
        }

        if (pendingDamageDefenseDeclarationKey is null)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be initialized while stage is awaitCounter.");
        }

        if (pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1 &&
            !tryParseCardDefenseDeclarationKey(
                pendingDamageDefenseDeclarationKey,
                out _,
                out _))
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be fixedReduce1 or a supported card defense declaration while stage is awaitCounter.");
        }
    }

    private static int resolveDamageReductionFromWindowState(
        ResponseWindowState responseWindowState,
        string damageTypeKey)
    {
        var defenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey;
        if (defenseDeclarationKey is null)
        {
            return 0;
        }

        if (defenseDeclarationKey == DefenseTypeKeyFixedReduce1)
        {
            return 1;
        }

        if (!tryParseCardDefenseDeclarationKey(
                defenseDeclarationKey,
                out var defenseTypeKey,
                out var defenseValue))
        {
            throw new InvalidOperationException("Damage response resolution only supports pendingDamageDefenseDeclarationKey null, fixedReduce1, or a supported card defense declaration.");
        }

        if (!isDefenseTypeMatchingDamageType(damageTypeKey, defenseTypeKey))
        {
            return 0;
        }

        return defenseValue;
    }

    private static bool isDefenseTypeMatchingDamageType(string damageTypeKey, string defenseTypeKey)
    {
        if (damageTypeKey == DamageTypeKeyPhysical)
        {
            return defenseTypeKey == DefenseTypeKeyPhysical || defenseTypeKey == DefenseTypeKeyDual;
        }

        if (damageTypeKey == DamageTypeKeySpell)
        {
            return defenseTypeKey == DefenseTypeKeySpell || defenseTypeKey == DefenseTypeKeyDual;
        }

        return false;
    }

    private static string normalizePendingDamageTypeKey(string? damageTypeKey)
    {
        if (string.IsNullOrWhiteSpace(damageTypeKey))
        {
            return DamageTypeKeyPhysical;
        }

        return damageTypeKey;
    }

    private static bool isFormalDefenseTypeKey(string defenseTypeKey)
    {
        return defenseTypeKey == DefenseTypeKeyPhysical ||
               defenseTypeKey == DefenseTypeKeySpell ||
               defenseTypeKey == DefenseTypeKeyDual;
    }

    private static string createCardDefenseDeclarationKey(string defenseTypeKey, int defenseValue)
    {
        return $"{DefenseDeclarationPrefixCard}{defenseTypeKey}:{defenseValue}";
    }

    private static bool tryParseCardDefenseDeclarationKey(
        string defenseDeclarationKey,
        out string defenseTypeKey,
        out int defenseValue)
    {
        defenseTypeKey = string.Empty;
        defenseValue = 0;

        if (!defenseDeclarationKey.StartsWith(DefenseDeclarationPrefixCard, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = defenseDeclarationKey.Substring(DefenseDeclarationPrefixCard.Length);
        var separatorIndex = payload.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= payload.Length - 1)
        {
            return false;
        }

        var parsedDefenseTypeKey = payload.Substring(0, separatorIndex);
        var parsedDefenseValueText = payload.Substring(separatorIndex + 1);
        if (!isFormalDefenseTypeKey(parsedDefenseTypeKey))
        {
            return false;
        }

        if (!int.TryParse(parsedDefenseValueText, out var parsedDefenseValue))
        {
            return false;
        }

        if (parsedDefenseValue < 0)
        {
            return false;
        }

        defenseTypeKey = parsedDefenseTypeKey;
        defenseValue = parsedDefenseValue;
        return true;
    }

    private List<GameEvent> processOpenInputContextActionRequest(
        GameState.GameState gameState,
        OpenInputContextActionRequest openInputContextActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest requires gameState.turnState to be initialized.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest cannot open while currentInputContext is active.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(openInputContextActionRequest.requestId),
            actorPlayerId = openInputContextActionRequest.actorPlayerId,
            rootActionRequest = openInputContextActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "openInputContext",
            sourcePlayerId = openInputContextActionRequest.actorPlayerId,
            contextKey = openInputContextActionRequest.contextKey,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = openInputContextActionRequest.actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = openInputContextActionRequest.inputTypeKey,
            contextKey = openInputContextActionRequest.contextKey,
        };
        inputContextState.choiceKeys.AddRange(openInputContextActionRequest.choiceKeys);

        gameState.currentInputContext = inputContextState;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = openInputContextActionRequest.requestId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processSubmitInputChoiceActionRequest(
        GameState.GameState gameState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires an active currentActionChain.");
        }

        var inputContextState = gameState.currentInputContext;

        if (inputContextState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires an active currentInputContext.");
        }

        if (submitInputChoiceActionRequest.inputContextId != inputContextState.inputContextId)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest inputContextId mismatch.");
        }

        if (submitInputChoiceActionRequest.actorPlayerId != inputContextState.requiredPlayerId)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.");
        }

        var continuationState = new SubmitInputChoiceContinuationState(actionChainState.pendingContinuationKey);
        var shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation =
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

        if (!continuationState.isTurnStartShackleDiscardContinuation &&
            !(continuationState.isAnomalyContinuation &&
              shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation))
        {
            inputContextState.selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;
        }

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = submitInputChoiceActionRequest.requestId,
            eventTypeKey = "inputContextClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = false,
        });

        var shouldContinueScriptedOnPlayChooseDamage =
            continuationState.isInputChoiceDamageContinuation &&
            submitInputChoiceActionRequest.choiceKey == TemporaryOnPlayProbeResolver.ScriptedOnPlayDeal1ChoiceKey;

        gameState.currentInputContext = null;

        if (tryContinueSubmitInputChoiceByContinuationGroup(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState))
        {
            return actionChainState.producedEvents;
        }

        if (shouldContinueScriptedOnPlayChooseDamage)
        {
            var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                gameState,
                submitInputChoiceActionRequest.actorPlayerId);
            appendDamageEventsToExistingActionChain(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                null,
                targetCharacterInstanceId,
                1);
            actionChainState.pendingContinuationKey = null;
        }

        actionChainState.isCompleted = true;
        return actionChainState.producedEvents;
    }

    private bool ensureValidSubmitInputChoiceByContinuationGroup(
        GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        SubmitInputChoiceContinuationState continuationState)
    {
        if (continuationState.isTurnStartShackleDiscardContinuation)
        {
            if (!TurnTransitionProcessor.isValidTurnStartShackleDiscardChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKeys to contain exactly four unique values from currentInputContext.choiceKeys for continuation:turnStartShackleDiscard.");
            }

            return false;
        }

        if (continuationState.isAnomalyContinuation)
        {
            return anomalyProcessor.ensureValidAnomalyContinuationBeforeClosingInputContext(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey);
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
        }

        return false;
    }

    private bool tryContinueSubmitInputChoiceByContinuationGroup(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        SubmitInputChoiceContinuationState continuationState)
    {
        if (continuationState.isEndPhaseHandDiscardContinuation)
        {
            endPhaseProcessor.continueEndPhaseHandDiscardContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isTurnStartShackleDiscardContinuation)
        {
            turnTransitionProcessor.continueTurnStartShackleDiscardContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isAnomalyContinuation)
        {
            anomalyProcessor.continueAnomalyContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey);
            return true;
        }

        return false;
    }
}




