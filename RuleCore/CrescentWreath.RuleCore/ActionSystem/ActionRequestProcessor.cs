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
    internal const string ContinuationKeyStagedResponseDamage = "continuation:stagedResponseDamage";
    private const string DefenseTypeKeyFixedReduce1 = "fixedReduce1";
    private const string DefenseTypeKeyPhysical = "physical";
    private const string DefenseTypeKeySpell = "spell";
    private const string DefenseTypeKeyDual = "dual";
    private const string DamageTypeKeyPhysical = "physical";
    private const string DamageTypeKeySpell = "spell";
    private const string DamageTypeKeyDirect = "direct";
    private const string DefenseDeclarationPrefixCard = "cardDefense:";
    internal const string DamageResponseStageAwaitDefense = "awaitDefense";
    internal const string DamageResponseStageAwaitCounter = "awaitCounter";
    private const string DamageCounterTypeKeyCancelFixedReduce1 = "cancelFixedReduce1";
    private const string SkillKeyC002_2 = "C002:2";
    private const string SkillKeyC004_1 = "C004:1";
    private const string SkillKeyC001_1 = "C001:1";
    private const string SkillKeyC001_3 = "C001:3";
    private const string SkillKeyC001_4 = "C001:4";
    private const string ContinuationKeyC001SealTarget = "continuation:character:C001:1:targetOpponent";
    private const string InputTypeKeyC001TargetPlayer = "character:C001:targetPlayer";
    private const string ContextKeyC001SealTarget = "character:C001:1:targetOpponent";
    private const string SkillKeyC007_1 = "C007:1";
    private const string SkillKeyC007_2 = "C007:2";
    private const string SkillKeyC007_3 = "C007:3";
    private const string SkillKeyC007_4 = "C007:4";
    private const string ContinuationKeyC007NightmareTarget = "continuation:character:C007:3:targetOpponent";
    private const string ContinuationKeyC007DaydreamHealTarget = "continuation:character:C007:4:healTarget";
    private const string ContinuationKeyC007DaydreamDamageTarget = "continuation:character:C007:4:damageTarget";
    private const string InputTypeKeyC007TargetPlayer = "character:C007:targetPlayer";
    private const string ContextKeyC007NightmareTarget = "character:C007:3:targetOpponent";
    private const string ContextKeyC007DaydreamHealTarget = "character:C007:4:healTarget";
    private const string ContextKeyC007DaydreamDamageTarget = "character:C007:4:damageTarget";
    private const string ChoiceKeyPlayerPrefix = "player:";
    private const string SkillKeyC008_1 = "C008:1";
    private const string SkillKeyC008_2 = "C008:2";
    private const string SkillKeyC008_3 = "C008:3";
    private const string SkillKeyC008_4 = "C008:4";
    private const string ContinuationKeyC008WindKingTarget = "continuation:character:C008:1:targetOpponent";
    private const string ContinuationKeyC008WindKingDamageType = "continuation:character:C008:1:damageType";
    private const string InputTypeKeyC008TargetPlayer = "character:C008:targetPlayer";
    private const string InputTypeKeyC008DamageType = "character:C008:damageType";
    private const string ContextKeyC008WindKingTarget = "character:C008:1:targetOpponent";
    private const string ContextKeyC008WindKingDamageType = "character:C008:1:damageType";
    private const string ChoiceKeyDamageTypePhysical = "damageType:physical";
    private const string ChoiceKeyDamageTypeSpell = "damageType:spell";
    private const string LocalStateKeyC008WindKingTargetPlayerId = "character:C008:1:targetPlayerId";
    private const string LocalStateKeyC008RemainingTargetCharacterIds = "character:C008:4:remainingTargetCharacterIds";
    private const string LocalStateKeyC008ExcaliburSequence = "character:C008:4:sequence";
    private const string SkillKeyC018_2 = "C018:2";
    private const string SkillKeyC018_3 = "C018:3";
    private const string SkillKeyC018_4 = "C018:4";
    private const string LocalStateKeyC018SequenceKind = "character:C018:sequenceKind";
    private const string LocalStateKeyC018RemainingTargetCharacterIds = "character:C018:remainingTargetCharacterIds";
    private const string C018SequenceKindDirectDamage = "directDamageAllOthers";
    private const string C018SequenceKindDirectKill = "directKillHpOneOpponents";
    private const string SkillKeyC021_1 = "C021:1";
    private const string SkillKeyC029_4 = "C029:4";
    private const string MarkerTypeDream = "dream";
    private const string CharacterDefinitionIdC001 = "C001";
    private const string CharacterDefinitionIdC011 = "C011";
    private const string StatusKeyBarrier = "Barrier";
    private const string StatusKeySeal = "Seal";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeyPenetrate = "Penetrate";
    private const string TreasureDefinitionIdT014 = "T014";
    private const string TreasureDefinitionIdT015 = "T015";
    private const string TreasureDefinitionIdT020 = "T020";
    private const string TreasureDefinitionIdT025 = "T025";

    private enum DamageResponseStage
    {
        awaitDefense = 0,
        awaitCounter = 1,
    }

    private readonly struct SubmitInputChoiceContinuationState
    {
        public readonly string? pendingContinuationKey;
        public readonly bool isInputChoiceDamageContinuation;
        public readonly bool isTreasureOnPlayContinuation;
        public readonly bool isTreasureArrivalContinuation;
        public readonly bool isEndPhaseHandDiscardContinuation;
        public readonly bool isTurnStartShackleDiscardContinuation;
        public readonly bool isTurnStartC001BarrierContinuation;
        public readonly bool isAnomalyContinuation;
        public readonly bool isMechanicalJadeContinuation;
        public readonly bool isTreasureDefenseContinuation;
        public readonly bool isT029DamageImmunityContinuation;
        public readonly bool isC001SkillContinuation;
        public readonly bool isC007SkillContinuation;
        public readonly bool isC008SkillContinuation;

        public SubmitInputChoiceContinuationState(string? pendingContinuationKey)
        {
            this.pendingContinuationKey = pendingContinuationKey;
            isInputChoiceDamageContinuation =
                pendingContinuationKey == TemporaryOnPlayProbeResolver.ContinuationKeyInputChoiceDamage;
            isTreasureOnPlayContinuation =
                TreasureOnPlayEffectRuntime.isTreasureOnPlayContinuationKey(pendingContinuationKey);
            isTreasureArrivalContinuation =
                TreasureArrivalEffectRuntime.isTreasureArrivalContinuationKey(pendingContinuationKey);
            isEndPhaseHandDiscardContinuation =
                pendingContinuationKey == EndPhaseProcessor.ContinuationKeyEndPhaseHandDiscard;
            isTurnStartShackleDiscardContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartShackleDiscard;
            isTurnStartC001BarrierContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartC001Barrier;
            isAnomalyContinuation = AnomalyProcessor.isAnomalyContinuationKey(pendingContinuationKey);
            isMechanicalJadeContinuation = MechanicalJadeRuntime.isMechanicalJadeContinuationKey(pendingContinuationKey);
            isTreasureDefenseContinuation = TreasureDefenseEffectRuntime.isTreasureDefenseContinuationKey(pendingContinuationKey);
            isT029DamageImmunityContinuation =
                pendingContinuationKey == DamageProcessor.ContinuationKeyT029DamageImmunity;
            isC001SkillContinuation = isC001SkillContinuationKey(pendingContinuationKey);
            isC007SkillContinuation = isC007SkillContinuationKey(pendingContinuationKey);
            isC008SkillContinuation = isC008SkillContinuationKey(pendingContinuationKey);
        }
    }

    private readonly ZoneMovementService zoneMovementService;
    private readonly DamageProcessor damageProcessor;
    private readonly TreasureOnPlayEffectRuntime treasureOnPlayEffectRuntime;
    private readonly TreasureArrivalEffectRuntime treasureArrivalEffectRuntime;
    private readonly MechanicalJadeRuntime mechanicalJadeRuntime;
    private readonly TreasureBanishEffectRuntime treasureBanishEffectRuntime;
    private readonly TreasureDefenseEffectRuntime treasureDefenseEffectRuntime;
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
        Func<long> nextResponseWindowIdSupplier = () => Interlocked.Increment(ref nextResponseWindowNumericId);
        anomalyProcessor = new AnomalyProcessor(
            zoneMovementService,
            nextInputContextIdSupplier);
        treasureOnPlayEffectRuntime = new TreasureOnPlayEffectRuntime(
            nextInputContextIdSupplier,
            nextResponseWindowIdSupplier,
            zoneMovementService,
            this.damageProcessor,
            anomalyProcessor.forceResolveCurrentAnomalyFromExternalEffect);
        treasureArrivalEffectRuntime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        mechanicalJadeRuntime = new MechanicalJadeRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        treasureBanishEffectRuntime = new TreasureBanishEffectRuntime(zoneMovementService);
        treasureDefenseEffectRuntime = new TreasureDefenseEffectRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        endPhaseProcessor = new EndPhaseProcessor(
            zoneMovementService,
            nextInputContextIdSupplier);
        turnTransitionProcessor = new TurnTransitionProcessor(
            zoneMovementService,
            endPhaseProcessor,
            nextInputContextIdSupplier);
    }

    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        if (actionRequest is SubmitCharacterSelectionActionRequest submitCharacterSelectionActionRequest)
        {
            return CharacterSelectionRuntime.submitSelection(gameState, submitCharacterSelectionActionRequest);
        }

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
        if (gameState.matchState == GameState.MatchState.initializing &&
            gameState.characterSelectionState is { isCompleted: false })
        {
            throw new InvalidOperationException(
                "ActionRequestProcessor cannot accept gameplay action requests before character selection completes.");
        }

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
            actorPlayerState.skillPoint += TreasureResourceValueResolver.resolveSkillPointGainOnPlay(cardInstance.definitionId);
        }

        appendScriptedOnPlayEffectEvents(gameState, actionChainState, playTreasureCardActionRequest, cardInstance);
        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            producedEventsStartIndex: 1);
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            producedEventsStartIndex: 1);
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
        var requiredSigilPayment = Math.Max(1, summonSigilCost - actorPlayerState.summonSigilDiscount);
        if (actorPlayerState.lockedSigil.Value < requiredSigilPayment)
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
        actorPlayerState.lockedSigil = actorPlayerState.lockedSigil.Value - requiredSigilPayment;

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            summonTreasureCardActionRequest.requestId);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.producedEvents.Add(cardMovedEvent);

        var enteredSummonZoneCardInstanceIds = new List<CardInstanceId>();
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
            enteredSummonZoneCardInstanceIds.Add(topPublicTreasureCardInstanceId);
        }

        if (enteredSummonZoneCardInstanceIds.Count > 0 &&
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            treasureArrivalEffectRuntime.tryStartArrivalEffectsForEnteredSummonZoneCards(
                gameState,
                actionChainState,
                summonTreasureCardActionRequest.requestId,
                enteredSummonZoneCardInstanceIds);
        }

        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
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
            var discardCardIdsInCurrentOrder = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
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
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
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

        var (manaCost, leylineCost) = resolveUseSkillResourceCost(
            characterInstance.definitionId,
            useSkillActionRequest.skillKey);

        if (actorPlayerState.skillPoint < 1)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player skillPoint to be at least 1.");
        }

        if (actorPlayerState.mana < manaCost)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player mana to be sufficient for skill cost.");
        }

        if (leylineCost > 0)
        {
            var actorTeamState = gameState.teams[actorPlayerState.teamId];
            if (actorTeamState.leyline < leylineCost)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires team leyline to be sufficient for skill cost.");
            }
        }

        if (isOncePerTurnSkill(useSkillActionRequest.skillKey) &&
            gameState.turnState.usedOncePerTurnSkillKeys.Contains(useSkillActionRequest.skillKey))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires once-per-turn skill to not have been used this turn.");
        }

        if (requiresOpponentTarget(useSkillActionRequest.skillKey))
        {
            if (!useSkillActionRequest.targetCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId for this skill.");
            }

            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.targetCharacterInstanceId.Value,
                    out var opponentTargetInstance))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId to exist in gameState.characterInstances.");
            }

            if (!gameState.players.TryGetValue(opponentTargetInstance.ownerPlayerId, out var opponentTargetPlayerState))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires target character ownerPlayerId to exist in gameState.players.");
            }

            if (opponentTargetPlayerState.teamId == actorPlayerState.teamId)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId to belong to an opponent player.");
            }

            if (!opponentTargetInstance.isAlive || !opponentTargetInstance.isInPlay)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires target character to be alive and in play.");
            }

        }

        if (requiresAllyTarget(useSkillActionRequest.skillKey))
        {
            if (!useSkillActionRequest.targetAllyCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId for this skill.");
            }

            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.targetAllyCharacterInstanceId.Value,
                    out var allyTargetInstance))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId to exist in gameState.characterInstances.");
            }

            if (!gameState.players.TryGetValue(allyTargetInstance.ownerPlayerId, out var allyTargetPlayerState))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires ally target character ownerPlayerId to exist in gameState.players.");
            }

            if (allyTargetPlayerState.teamId != actorPlayerState.teamId)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId to belong to an ally player.");
            }

            if (!allyTargetInstance.isAlive || !allyTargetInstance.isInPlay)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires ally target character to be alive and in play.");
            }
        }

        var hasMarkerCost = tryGetMarkerCost(
            useSkillActionRequest.skillKey,
            out var markerCostType,
            out var markerCostAmount);

        if (hasMarkerCost)
        {
            if (MarkerRuntime.getMarkerCount(characterInstance, markerCostType) < markerCostAmount)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires sufficient markers for skill marker cost.");
            }
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_1, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C001:1 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_3, StringComparison.Ordinal) &&
            collectC007OpponentChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C007:3 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_4, StringComparison.Ordinal))
        {
            if (collectC007FriendlyChoiceKeys(gameState, actorPlayerState).Count == 0 ||
                collectC007OpponentChoiceKeys(gameState, actorPlayerState).Count == 0)
            {
                throw new InvalidOperationException("UseSkillActionRequest C007:4 requires at least one alive friendly target and one alive opponent target.");
            }
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_1, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C008:1 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_3, StringComparison.Ordinal))
        {
            if (gameState.turnState.hasResolvedAnomalyThisTurn)
            {
                throw new InvalidOperationException("UseSkillActionRequest C001:3 cannot be used after an anomaly has already been resolved this turn.");
            }

            if (gameState.currentAnomalyState is null ||
                string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
            {
                throw new InvalidOperationException("UseSkillActionRequest C001:3 requires an active anomaly on the field.");
            }
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
        actorPlayerState.mana -= manaCost;
        actorPlayerState.skillPoint -= 1;
        if (leylineCost > 0)
        {
            gameState.teams[actorPlayerState.teamId].leyline -= leylineCost;
        }

        if (hasMarkerCost)
        {
            actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
                characterInstance,
                markerCostType,
                markerCostAmount,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
        }

        if (isOncePerTurnSkill(useSkillActionRequest.skillKey))
        {
            gameState.turnState.usedOncePerTurnSkillKeys.Add(useSkillActionRequest.skillKey);
        }
        appendUseSkillEffectEvents(
            gameState,
            actionChainState,
            useSkillActionRequest,
            actorPlayerState,
            characterInstance);
        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            useSkillActionRequest.requestId,
            producedEventsStartIndex: 0);
        if (!isSequencedSkill(useSkillActionRequest.skillKey))
        {
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                producedEventsStartIndex: 0);
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
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
            startC018SkillSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState,
                C018SequenceKindDirectDamage);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_3, StringComparison.Ordinal))
        {
            startC018SkillSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState,
                C018SequenceKindDirectKill);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_4, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId == actorPlayerState.teamId)
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

                if (targetCharacterInstance.currentHp <= 2)
                {
                    continue;
                }

                var hpBefore = targetCharacterInstance.currentHp;
                targetCharacterInstance.currentHp = 2;
                actionChainState.producedEvents.Add(new HpChangedEvent
                {
                    eventId = useSkillActionRequest.requestId,
                    eventTypeKey = "hpChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    targetPlayerId = playerState.playerId,
                    targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                    hpBefore = hpBefore,
                    hpAfter = 2,
                    delta = 2 - hpBefore,
                });
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC001TargetPlayer,
                ContextKeyC001SealTarget,
                ContinuationKeyC001SealTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_3, StringComparison.Ordinal))
        {
            actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
                gameState,
                useSkillActionRequest.characterInstanceId,
                true,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
            anomalyProcessor.forceResolveCurrentAnomalyFromExternalEffect(
                gameState,
                actionChainState,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.requestId);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_4, StringComparison.Ordinal))
        {
            var barrierStatus = StatusRuntime.applyStatus(
                gameState,
                new StatusInstance
                {
                    statusKey = StatusKeyBarrier,
                    applierPlayerId = useSkillActionRequest.actorPlayerId,
                    applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    targetPlayerId = useSkillActionRequest.actorPlayerId,
                    targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                    stackCount = 1,
                });
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = barrierStatus.statusKey,
                targetPlayerId = barrierStatus.targetPlayerId,
                targetCharacterInstanceId = barrierStatus.targetCharacterInstanceId,
                isApplied = true,
            });

            var deckZoneState = gameState.zones[actorPlayerState.deckZoneId];
            var deckTopCardIds = new List<CardInstanceId>();
            for (var i = 0; i < deckZoneState.cardInstanceIds.Count && deckTopCardIds.Count < 3; i++)
            {
                deckTopCardIds.Add(deckZoneState.cardInstanceIds[i]);
            }

            foreach (var cardInstanceId in deckTopCardIds)
            {
                var cardInstance = gameState.cardInstances[cardInstanceId];
                var moveEvent = zoneMovementService.moveCard(
                    gameState,
                    cardInstance,
                    actorPlayerState.fieldZoneId,
                    CardMoveReason.defenseLikePlace,
                    actionChainState.actionChainId,
                    useSkillActionRequest.requestId);
                actionChainState.producedEvents.Add(moveEvent);
                cardInstance.isDefensePlacedOnField = true;
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_1, StringComparison.Ordinal))
        {
            if (MarkerRuntime.canAddMarker(sourceCharacterInstance, MarkerTypeDream))
            {
                actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
                    sourceCharacterInstance,
                    MarkerTypeDream,
                    1,
                    actionChainState.actionChainId,
                    useSkillActionRequest.requestId));
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_2, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId != actorPlayerState.teamId)
                {
                    continue;
                }

                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }

                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    playerState,
                    useSkillActionRequest.requestId);
                break;
            }

            if (MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDream) == 3)
            {
                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    actorPlayerState,
                    useSkillActionRequest.requestId);
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007NightmareTarget,
                ContinuationKeyC007NightmareTarget,
                collectC007OpponentChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007DaydreamHealTarget,
                ContinuationKeyC007DaydreamHealTarget,
                collectC007FriendlyChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC008TargetPlayer,
                ContextKeyC008WindKingTarget,
                ContinuationKeyC008WindKingTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_2, StringComparison.Ordinal))
        {
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusKeyBarrier,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                targetCharacterInstanceId: sourceCharacterInstance.characterInstanceId);
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeyPhysicalDamageBoostNext,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_3, StringComparison.Ordinal))
        {
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeySpellDamageBoostNext,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_4, StringComparison.Ordinal))
        {
            startC008ExcaliburSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState);
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

    private void startC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState,
        string sequenceKind)
    {
        var remainingTargetCharacterIds = new List<CharacterInstanceId>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (string.Equals(sequenceKind, C018SequenceKindDirectDamage, StringComparison.Ordinal))
            {
                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }
            }
            else if (playerState.teamId == actorPlayerState.teamId)
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

            if (string.Equals(sequenceKind, C018SequenceKindDirectKill, StringComparison.Ordinal) &&
                targetCharacterInstance.currentHp != 1)
            {
                continue;
            }

            remainingTargetCharacterIds.Add(targetCharacterInstance.characterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC018SequenceKind] = sequenceKind;
        saveC018RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);
        continueC018SkillSequence(gameState, actionChainState, useSkillActionRequest.requestId);
    }

    private void tryResumeC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC018SequenceKind) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        continueC018SkillSequence(gameState, actionChainState, eventId);
    }

    private void continueC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC018SequenceKind, out var sequenceKind))
        {
            clearC018SkillSequence(actionChainState);
            return;
        }

        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var remainingTargetCharacterIds = loadC018RemainingTargetCharacterIds(actionChainState);
            if (remainingTargetCharacterIds.Count == 0)
            {
                clearC018SkillSequence(actionChainState);
                return;
            }

            var targetCharacterInstanceId = remainingTargetCharacterIds[0];
            remainingTargetCharacterIds.RemoveAt(0);
            saveC018RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);

            if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance) ||
                !targetCharacterInstance.isAlive ||
                !targetCharacterInstance.isInPlay)
            {
                continue;
            }

            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            if (string.Equals(sequenceKind, C018SequenceKindDirectDamage, StringComparison.Ordinal))
            {
                actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
                    gameState,
                    new DamageContext
                    {
                        damageContextId = new DamageContextId(eventId),
                        sourcePlayerId = useSkillActionRequest.actorPlayerId,
                        sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = targetCharacterInstance.ownerPlayerId,
                        targetCharacterInstanceId = targetCharacterInstanceId,
                        baseDamageValue = 1,
                        damageType = DamageTypeKeyDirect,
                    }));
            }
            else if (string.Equals(sequenceKind, C018SequenceKindDirectKill, StringComparison.Ordinal))
            {
                if (targetCharacterInstance.currentHp != 1)
                {
                    continue;
                }

                actionChainState.producedEvents.AddRange(damageProcessor.resolveDirectKill(
                    gameState,
                    new KillContext
                    {
                        killContextId = eventId,
                        killerPlayerId = useSkillActionRequest.actorPlayerId,
                        killedCharacterInstanceId = targetCharacterInstanceId,
                        killedPlayerId = targetCharacterInstance.ownerPlayerId,
                    }));
            }
            else
            {
                throw new InvalidOperationException("C018 skill continuation contains an unknown sequence kind.");
            }

            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                eventId,
                producedEventsStartIndex);
        }

        if (gameState.matchState != GameState.MatchState.running)
        {
            clearC018SkillSequence(actionChainState);
        }
    }

    private static List<PlayerId> getPlayersInSeatOrder(GameState.GameState gameState)
    {
        var result = new List<PlayerId>();
        if (gameState.matchMeta is not null)
        {
            foreach (var playerId in gameState.matchMeta.seatOrder)
            {
                if (gameState.players.ContainsKey(playerId))
                {
                    result.Add(playerId);
                }
            }
        }

        var remainingPlayerIds = new List<PlayerId>();
        foreach (var playerId in gameState.players.Keys)
        {
            if (!result.Contains(playerId))
            {
                remainingPlayerIds.Add(playerId);
            }
        }

        remainingPlayerIds.Sort((left, right) => left.Value.CompareTo(right.Value));
        result.AddRange(remainingPlayerIds);
        return result;
    }

    private static void saveC018RemainingTargetCharacterIds(
        ActionChainState actionChainState,
        List<CharacterInstanceId> characterInstanceIds)
    {
        var numericIds = new List<string>();
        foreach (var characterInstanceId in characterInstanceIds)
        {
            numericIds.Add(characterInstanceId.Value.ToString());
        }

        actionChainState.localState[LocalStateKeyC018RemainingTargetCharacterIds] =
            string.Join(",", numericIds);
    }

    private static List<CharacterInstanceId> loadC018RemainingTargetCharacterIds(
        ActionChainState actionChainState)
    {
        var result = new List<CharacterInstanceId>();
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC018RemainingTargetCharacterIds,
                out var serializedIds) ||
            string.IsNullOrWhiteSpace(serializedIds))
        {
            return result;
        }

        foreach (var segment in serializedIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(segment, out var numericId))
            {
                throw new InvalidOperationException("C018 skill continuation contains an invalid target character id.");
            }

            result.Add(new CharacterInstanceId(numericId));
        }

        return result;
    }

    private static void clearC018SkillSequence(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC018SequenceKind);
        actionChainState.localState.Remove(LocalStateKeyC018RemainingTargetCharacterIds);
    }

    private void openCharacterSkillChoiceInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        List<string> choiceKeys)
    {
        if (choiceKeys.Count == 0)
        {
            throw new InvalidOperationException("Character skill input requires at least one legal choice.");
        }

        var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);
        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = continuationKey;
        actionChainState.isCompleted = false;
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });
    }

    private static List<string> collectC007FriendlyChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007PlayerChoiceKeys(
            gameState,
            playerState => playerState.teamId == actorPlayerState.teamId);
    }

    private static List<string> collectC007OpponentChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007PlayerChoiceKeys(
            gameState,
            playerState => playerState.teamId != actorPlayerState.teamId);
    }

    private static List<string> collectC007PlayerChoiceKeys(
        GameState.GameState gameState,
        Func<GameState.PlayerState, bool> playerPredicate)
    {
        var choiceKeys = new List<string>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (!playerPredicate(playerState) ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, playerId, out _))
            {
                continue;
            }

            choiceKeys.Add(ChoiceKeyPlayerPrefix + playerId.Value);
        }

        return choiceKeys;
    }

    private void continueC007SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C007 skill continuation requires a valid UseSkillActionRequest root.");
        }

        var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
        if (!tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                targetPlayerId,
                out var targetCharacterInstance))
        {
            throw new InvalidOperationException("C007 skill continuation requires selected player to have an alive in-play character.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC007NightmareTarget, StringComparison.Ordinal))
        {
            if (!string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC011, StringComparison.Ordinal))
            {
                var appliedStatus = StatusRuntime.applyStatus(
                    gameState,
                    new StatusInstance
                    {
                        statusKey = StatusKeyCharm,
                        applierPlayerId = useSkillActionRequest.actorPlayerId,
                        applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = targetPlayerId,
                        targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                        stackCount = 1,
                    });
                actionChainState.producedEvents.Add(new StatusChangedEvent
                {
                    eventId = request.requestId,
                    eventTypeKey = "statusChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    statusKey = appliedStatus.statusKey,
                    targetPlayerId = targetPlayerId,
                    targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                    isApplied = true,
                });
            }

            actionChainState.pendingContinuationKey = null;
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC007DaydreamHealTarget, StringComparison.Ordinal))
        {
            appendHealOnCharacter(
                actionChainState,
                request.requestId,
                targetCharacterInstance,
                healAmount: 4);
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007DaydreamDamageTarget,
                ContinuationKeyC007DaydreamDamageTarget,
                collectC007OpponentChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC007DaydreamDamageTarget, StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                4,
                DamageTypeKeySpell);
            return;
        }

        throw new InvalidOperationException("C007 skill continuation key is unsupported.");
    }

    private static PlayerId parsePlayerChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyPlayerPrefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(ChoiceKeyPlayerPrefix.Length), out var playerNumericId))
        {
            throw new InvalidOperationException("C007 skill target choice requires player:{id} format.");
        }

        return new PlayerId(playerNumericId);
    }

    private static bool isC007SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC007NightmareTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC007DaydreamHealTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC007DaydreamDamageTarget, StringComparison.Ordinal);
    }

    private static List<string> collectOpponentPlayerChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007OpponentChoiceKeys(gameState, actorPlayerState);
    }

    private void continueC001SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (!string.Equals(continuationKey, ContinuationKeyC001SealTarget, StringComparison.Ordinal) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C001:1 continuation requires a valid UseSkillActionRequest root.");
        }

        var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            targetPlayerState.teamId == actorPlayerState.teamId ||
            !tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                targetPlayerId,
                out var targetCharacterInstance))
        {
            throw new InvalidOperationException("C001:1 requires selected player to be an alive opponent.");
        }

        var sealStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeySeal,
                applierPlayerId = useSkillActionRequest.actorPlayerId,
                applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                stackCount = 1,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = request.requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = sealStatus.statusKey,
            targetPlayerId = sealStatus.targetPlayerId,
            targetCharacterInstanceId = sealStatus.targetCharacterInstanceId,
            isApplied = true,
        });

        actionChainState.pendingContinuationKey = null;
        if (gameState.characterInstances.TryGetValue(
                useSkillActionRequest.characterInstanceId,
                out var sourceCharacterInstance) &&
            sourceCharacterInstance.isActivated)
        {
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                DamageTypeKeySpell);
        }
    }

    private static bool isC001SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC001SealTarget, StringComparison.Ordinal);
    }

    private void continueC008SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C008 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC008WindKingTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out _))
            {
                throw new InvalidOperationException("C008:1 requires selected player to be an alive opponent.");
            }

            actionChainState.localState[LocalStateKeyC008WindKingTargetPlayerId] =
                targetPlayerId.Value.ToString();
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC008DamageType,
                ContextKeyC008WindKingDamageType,
                ContinuationKeyC008WindKingDamageType,
                new List<string>
                {
                    ChoiceKeyDamageTypePhysical,
                    ChoiceKeyDamageTypeSpell,
                });
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC008WindKingDamageType, StringComparison.Ordinal))
        {
            if (!actionChainState.localState.TryGetValue(
                    LocalStateKeyC008WindKingTargetPlayerId,
                    out var targetPlayerIdRaw) ||
                !long.TryParse(targetPlayerIdRaw, out var targetPlayerNumericId))
            {
                throw new InvalidOperationException("C008:1 damage-type continuation requires a saved target player.");
            }

            var damageTypeKey = request.choiceKey switch
            {
                ChoiceKeyDamageTypePhysical => DamageTypeKeyPhysical,
                ChoiceKeyDamageTypeSpell => DamageTypeKeySpell,
                _ => throw new InvalidOperationException("C008:1 damage type must be physical or spell."),
            };
            var targetPlayerId = new PlayerId(targetPlayerNumericId);
            if (!tryFindAliveInPlayCharacterInstanceByOwner(
                    gameState,
                    targetPlayerId,
                    out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C008:1 selected opponent no longer has an alive in-play character.");
            }

            actionChainState.localState.Remove(LocalStateKeyC008WindKingTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                damageTypeKey);
            return;
        }

        throw new InvalidOperationException("C008 skill continuation key is unsupported.");
    }

    private static bool isC008SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC008WindKingTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC008WindKingDamageType, StringComparison.Ordinal);
    }

    private static void applyCharacterStatus(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        string statusKey,
        PlayerId? targetPlayerId = null,
        CharacterInstanceId? targetCharacterInstanceId = null,
        string? durationTypeKey = null)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = statusKey,
                applierPlayerId = useSkillActionRequest.actorPlayerId,
                applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstanceId,
                stackCount = 1,
                durationTypeKey = durationTypeKey ?? string.Empty,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = useSkillActionRequest.requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private void startC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState)
    {
        var targetCharacterInstanceIds = new List<CharacterInstanceId>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (playerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(
                    gameState,
                    playerId,
                    out var targetCharacterInstance))
            {
                continue;
            }

            targetCharacterInstanceIds.Add(targetCharacterInstance.characterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC008ExcaliburSequence] = "active";
        saveC008RemainingTargetCharacterIds(actionChainState, targetCharacterInstanceIds);
        continueC008ExcaliburSequence(gameState, actionChainState, useSkillActionRequest.requestId);
    }

    private void tryResumeC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC008ExcaliburSequence) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        continueC008ExcaliburSequence(gameState, actionChainState, eventId);
    }

    private void continueC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
        {
            clearC008ExcaliburSequence(actionChainState);
            return;
        }

        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var remainingTargetCharacterIds = loadC008RemainingTargetCharacterIds(actionChainState);
            if (remainingTargetCharacterIds.Count == 0)
            {
                clearC008ExcaliburSequence(actionChainState);
                return;
            }

            var targetCharacterInstanceId = remainingTargetCharacterIds[0];
            remainingTargetCharacterIds.RemoveAt(0);
            saveC008RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);
            if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance) ||
                !targetCharacterInstance.isAlive ||
                !targetCharacterInstance.isInPlay)
            {
                continue;
            }

            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                eventId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstanceId,
                6,
                DamageTypeKeySpell);
        }
    }

    private static void saveC008RemainingTargetCharacterIds(
        ActionChainState actionChainState,
        List<CharacterInstanceId> characterInstanceIds)
    {
        var numericIds = new List<string>();
        foreach (var characterInstanceId in characterInstanceIds)
        {
            numericIds.Add(characterInstanceId.Value.ToString());
        }

        actionChainState.localState[LocalStateKeyC008RemainingTargetCharacterIds] =
            string.Join(",", numericIds);
    }

    private static List<CharacterInstanceId> loadC008RemainingTargetCharacterIds(ActionChainState actionChainState)
    {
        var result = new List<CharacterInstanceId>();
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC008RemainingTargetCharacterIds,
                out var serializedIds) ||
            string.IsNullOrWhiteSpace(serializedIds))
        {
            return result;
        }

        foreach (var segment in serializedIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(segment, out var numericId))
            {
                throw new InvalidOperationException("C008:4 continuation contains an invalid target character id.");
            }

            result.Add(new CharacterInstanceId(numericId));
        }

        return result;
    }

    private static void clearC008ExcaliburSequence(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC008ExcaliburSequence);
        actionChainState.localState.Remove(LocalStateKeyC008RemainingTargetCharacterIds);
    }

    private void tryResumeCharacterSkillSequences(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        tryResumeC018SkillSequence(gameState, actionChainState, eventId);
        tryResumeC008ExcaliburSequence(gameState, actionChainState, eventId);
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
            var discardCardIdsInCurrentOrder = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
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

    private void applyT014OnDefensePostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        CardInstance defenseCardInstance,
        int producedEventsStartIndexForDamageResolution,
        CharacterInstanceId? defenderActiveCharacterInstanceIdBeforeResolution)
    {
        var defenderWasKilledInThisResolution = false;
        if (defenderActiveCharacterInstanceIdBeforeResolution.HasValue)
        {
            for (var eventIndex = Math.Max(0, producedEventsStartIndexForDamageResolution);
                 eventIndex < actionChainState.producedEvents.Count;
                 eventIndex++)
            {
                if (actionChainState.producedEvents[eventIndex] is not KillRecordedEvent killRecordedEvent)
                {
                    continue;
                }

                if (killRecordedEvent.killedCharacterInstanceId == defenderActiveCharacterInstanceIdBeforeResolution.Value)
                {
                    defenderWasKilledInThisResolution = true;
                    break;
                }
            }
        }

        if (!defenderWasKilledInThisResolution &&
            defenderActiveCharacterInstanceIdBeforeResolution.HasValue &&
            gameState.characterInstances.TryGetValue(
                defenderActiveCharacterInstanceIdBeforeResolution.Value,
                out var defenderCharacterInstance) &&
            defenderCharacterInstance.isAlive &&
            defenderCharacterInstance.isInPlay)
        {
            appendHealOnCharacter(
                actionChainState,
                requestId,
                defenderCharacterInstance,
                healAmount: 4);
        }

        if (defenseCardInstance.zoneId == defenderPlayerState.fieldZoneId &&
            defenseCardInstance.isDefensePlacedOnField)
        {
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                defenseCardInstance,
                defenderPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                requestId);
            actionChainState.producedEvents.Add(movedEvent);
            defenseCardInstance.isDefensePlacedOnField = false;
        }
    }

    private void applyT015OnDefensePostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        PlayerId damageSourcePlayerId,
        CardInstance defenseCardInstance,
        int producedEventsStartIndexForDamageResolution,
        CharacterInstanceId? defenderActiveCharacterInstanceIdBeforeResolution)
    {
        var defenderWasKilledInThisResolution = false;
        if (defenderActiveCharacterInstanceIdBeforeResolution.HasValue)
        {
            for (var eventIndex = Math.Max(0, producedEventsStartIndexForDamageResolution);
                 eventIndex < actionChainState.producedEvents.Count;
                 eventIndex++)
            {
                if (actionChainState.producedEvents[eventIndex] is not KillRecordedEvent killRecordedEvent)
                {
                    continue;
                }

                if (killRecordedEvent.killedCharacterInstanceId == defenderActiveCharacterInstanceIdBeforeResolution.Value)
                {
                    defenderWasKilledInThisResolution = true;
                    break;
                }
            }
        }

        if (!defenderWasKilledInThisResolution &&
            defenderActiveCharacterInstanceIdBeforeResolution.HasValue &&
            gameState.characterInstances.TryGetValue(
                defenderActiveCharacterInstanceIdBeforeResolution.Value,
                out var defenderCharacterInstance) &&
            defenderCharacterInstance.isAlive &&
            defenderCharacterInstance.isInPlay &&
            tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                damageSourcePlayerId,
                out var damageSourceCharacterInstance))
        {
            var directDamageContext = new DamageContext
            {
                damageContextId = new DamageContextId(requestId),
                sourcePlayerId = defenderPlayerState.playerId,
                sourceCardInstanceId = defenseCardInstance.cardInstanceId,
                targetPlayerId = damageSourcePlayerId,
                targetCharacterInstanceId = damageSourceCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            };
            actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, directDamageContext));
        }

        if (defenseCardInstance.zoneId == defenderPlayerState.fieldZoneId &&
            defenseCardInstance.isDefensePlacedOnField)
        {
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                defenseCardInstance,
                defenderPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                requestId);
            actionChainState.producedEvents.Add(movedEvent);
            defenseCardInstance.isDefensePlacedOnField = false;
        }
    }

    private void moveDefensePlacedCardToDiscardPostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        CardInstance defenseCardInstance)
    {
        if (defenseCardInstance.zoneId != defenderPlayerState.fieldZoneId ||
            !defenseCardInstance.isDefensePlacedOnField)
        {
            return;
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            defenseCardInstance,
            defenderPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(movedEvent);
        defenseCardInstance.isDefensePlacedOnField = false;
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
        var producedEvents = anomalyProcessor.processTryResolveAnomalyActionRequest(
            gameState,
            tryResolveAnomalyActionRequest);
        if (gameState.currentActionChain is not null)
        {
            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                gameState.currentActionChain,
                tryResolveAnomalyActionRequest.requestId,
                producedEventsStartIndex: 0);
        }

        return producedEvents;
    }

    private static (int manaCost, int leylineCost) resolveUseSkillResourceCost(
        string characterDefinitionId,
        string skillKey)
    {
        return CharacterSkillCostResolver.resolveSkillCost(characterDefinitionId, skillKey);
    }

    private static bool isOncePerTurnSkill(string skillKey)
    {
        return string.Equals(skillKey, SkillKeyC018_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC001_4, StringComparison.Ordinal);
    }

    private static bool isSequencedSkill(string skillKey)
    {
        return string.Equals(skillKey, SkillKeyC001_1, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC018_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC018_3, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC008_4, StringComparison.Ordinal);
    }

    private static bool requiresOpponentTarget(string skillKey)
    {
        return false;
    }

    private static bool requiresAllyTarget(string skillKey)
    {
        return false;
    }

    private static bool tryGetMarkerCost(string skillKey, out string markerCostType, out int markerCostAmount)
    {
        if (string.Equals(skillKey, SkillKeyC007_3, StringComparison.Ordinal))
        {
            markerCostType = MarkerTypeDream;
            markerCostAmount = 3;
            return true;
        }

        markerCostType = string.Empty;
        markerCostAmount = 0;
        return false;
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
        if (treasureOnPlayEffectRuntime.tryOpenOnPlayInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest,
                cardInstance))
        {
            return;
        }

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
        var stagedDamageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetPlayerId = pendingDamageDefenderPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = normalizePendingDamageTypeKey(damageTypeKey),
        };
        actionChainState.producedEvents.AddRange(
            damageProcessor.applySourceMatchingDamageBonuses(gameState, stagedDamageContext));
        if (damageProcessor.tryResolveBarrierBeforeDefense(
                gameState,
                stagedDamageContext,
                out var barrierPreventionEvents))
        {
            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            actionChainState.producedEvents.AddRange(barrierPreventionEvents);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                requestId,
                producedEventsStartIndex);
            tryResumeCharacterSkillSequences(gameState, actionChainState, requestId);
            return;
        }

        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId)),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
            pendingDamageTargetCharacterInstanceId = targetCharacterInstanceId,
            pendingDamageBaseDamageValue = stagedDamageContext.baseDamageValue,
            pendingDamageSourcePlayerId = sourcePlayerId,
            pendingDamageSourceCardInstanceId = sourceCardInstanceId,
            pendingDamageSourceCharacterInstanceId = sourceCharacterInstanceId,
            pendingDamageTypeKey = stagedDamageContext.damageType,
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
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
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
        var defenderActiveCharacterInstanceIdBeforeResolution = actorPlayerState.activeCharacterInstanceId;

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

        CardInstance? formalDefenseCardInstance = null;
        var shouldApplyT014OnDefensePostResolution = false;
        var shouldApplyT015OnDefensePostResolution = false;
        var shouldMoveDefenseCardToDiscardPostResolution = false;
        var shouldOpenT025ExtraDiscardInputContext = false;
        var producedEventsCountBeforeDamageResolution = actionChainState.producedEvents.Count;
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
            formalDefenseCardInstance = defenseCardInstance;
            shouldApplyT014OnDefensePostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT014, StringComparison.Ordinal);
            shouldApplyT015OnDefensePostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT015, StringComparison.Ordinal);
            shouldMoveDefenseCardToDiscardPostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT020, StringComparison.Ordinal);
            shouldOpenT025ExtraDiscardInputContext =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT025, StringComparison.Ordinal);

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
            var supportsDeclaredDefenseType = isCardDefinitionDefenseTypeSupportingDeclaredDefenseType(
                definitionDefenseTypeKey,
                submitDefenseActionRequest.defenseTypeKey);
            var effectiveDeclaredDefenseValue = supportsDeclaredDefenseType
                ? definitionDefenseValue.Value
                : 0;

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
                submitDefenseActionRequest.defenseTypeKey,
                effectiveDeclaredDefenseValue);
        }

        if (shouldOpenT025ExtraDiscardInputContext && formalDefenseCardInstance is not null)
        {
            treasureDefenseEffectRuntime.openT025ExtraDiscardInputContext(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance.cardInstanceId);
            return actionChainState.producedEvents;
        }

        var producedEvents = closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitDefenseActionRequest.requestId);
        if (shouldApplyT014OnDefensePostResolution && formalDefenseCardInstance is not null)
        {
            applyT014OnDefensePostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance,
                producedEventsCountBeforeDamageResolution,
                defenderActiveCharacterInstanceIdBeforeResolution);
        }

        if (shouldApplyT015OnDefensePostResolution && formalDefenseCardInstance is not null)
        {
            applyT015OnDefensePostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                pendingDamageSourcePlayerId.Value,
                formalDefenseCardInstance,
                producedEventsCountBeforeDamageResolution,
                defenderActiveCharacterInstanceIdBeforeResolution);
        }

        if (shouldMoveDefenseCardToDiscardPostResolution && formalDefenseCardInstance is not null)
        {
            moveDefensePlacedCardToDiscardPostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance);
        }

        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            submitDefenseActionRequest.requestId,
            producedEventsCountBeforeDamageResolution);
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);

        return producedEvents;
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
            hasAppliedSourceDamageBonuses = true,
        };

        var producedEventsStartIndex = actionChainState.producedEvents.Count;
        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
        if (gameState.currentInputContext is null &&
            string.Equals(
                actionChainState.pendingContinuationKey,
                ContinuationKeyStagedResponseDamage,
                StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = null;
        }

        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            requestId,
            producedEventsStartIndex);
        tryResumeCharacterSkillSequences(gameState, actionChainState, requestId);
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);

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

    private static bool isCardDefinitionDefenseTypeSupportingDeclaredDefenseType(
        string cardDefinitionDefenseTypeKey,
        string declaredDefenseTypeKey)
    {
        if (!isFormalDefenseTypeKey(declaredDefenseTypeKey))
        {
            return false;
        }

        if (cardDefinitionDefenseTypeKey == DefenseTypeKeyDual)
        {
            return true;
        }

        return cardDefinitionDefenseTypeKey == declaredDefenseTypeKey;
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

        var continuationState = new SubmitInputChoiceContinuationState(actionChainState.pendingContinuationKey);
        ensureSubmitInputChoiceActorAllowed(inputContextState, submitInputChoiceActionRequest.actorPlayerId);

        if (continuationState.isTreasureArrivalContinuation &&
            treasureArrivalEffectRuntime.isParallelTreasureArrivalInputContext(inputContextState))
        {
            var producedEventsCountBeforeParallelArrivalContinuation = actionChainState.producedEvents.Count;
            if (!treasureArrivalEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure arrival continuation could not be resolved.");
            }

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelArrivalContinuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isAnomalyContinuation &&
            anomalyProcessor.isParallelAnomalyInputContext(inputContextState))
        {
            var producedEventsCountBeforeParallelAnomalyContinuation = actionChainState.producedEvents.Count;
            if (!anomalyProcessor.tryContinueParallelAnomalyInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest,
                    continuationState.pendingContinuationKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest anomaly parallel continuation could not be resolved.");
            }

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryOpenTreasureArrivalEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isMechanicalJadeContinuation)
        {
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            gameState.currentInputContext = null;
            if (!mechanicalJadeRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest Mechanical Jade continuation could not be resolved.");
            }
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isTreasureDefenseContinuation)
        {
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            gameState.currentInputContext = null;

            continueT025ExtraDiscardDefenseAndResolveDamage(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isT029DamageImmunityContinuation)
        {
            ensureValidT029DamageImmunityChoiceRequest(inputContextState, submitInputChoiceActionRequest);

            var producedEventsCountBeforeT029Continuation = actionChainState.producedEvents.Count;
            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            inputContextState.selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;
            gameState.currentInputContext = null;

            continueT029DamageImmunityChoice(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeT029Continuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeT029Continuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

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
        var producedEventsCountBeforeContinuation = actionChainState.producedEvents.Count;

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
            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            tryOpenTreasureArrivalEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);
            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isTreasureArrivalContinuation)
        {
            if (!treasureArrivalEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure arrival continuation could not be resolved.");
            }
        }
        else if (continuationState.isTreasureOnPlayContinuation)
        {
            if (!treasureOnPlayEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure on-play continuation could not be resolved.");
            }
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

        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryOpenTreasureArrivalEffectsFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryResumeCharacterSkillSequences(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);

        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            actionChainState.pendingContinuationKey is null;
        return actionChainState.producedEvents;
    }

    private void tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (anomalyProcessor.tryOpenA010KillBanishSetAsideInputFromProducedEvents(
                gameState,
                actionChainState,
                eventId,
                producedEventsStartIndex))
        {
            return;
        }

        mechanicalJadeRuntime.tryOpenOverlayAfterKillInputContextFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
    }

    private void tryApplyTreasureBanishEffectsFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        treasureBanishEffectRuntime.applyBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
    }

    private void continueT025ExtraDiscardDefenseAndResolveDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires an active damageResponse currentResponseWindow.");
        }

        if (!tryParseCardDefenseDeclarationKey(
                responseWindowState.pendingDamageDefenseDeclarationKey ?? string.Empty,
                out var defenseTypeKey,
                out var defenseValue))
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires a pending card defense declaration.");
        }

        var extraDiscardCount = treasureDefenseEffectRuntime.continueT025ExtraDiscard(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        responseWindowState.pendingDamageDefenseDeclarationKey = createCardDefenseDeclarationKey(
            defenseTypeKey,
            defenseValue + extraDiscardCount);

        closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitInputChoiceActionRequest.requestId);
    }

    private void continueT029DamageImmunityChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var damageContext = loadT029PendingDamageContextOrThrow(actionChainState);
        clearT029PendingDamageContext(actionChainState);

        if (!string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                DamageProcessor.ChoiceKeyT029Decline,
                StringComparison.Ordinal))
        {
            var t029CardInstanceId = parseT029BanishChoiceKey(submitInputChoiceActionRequest.choiceKey);
            if (gameState.publicState is null)
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires gameState.publicState.");
            }

            if (!gameState.cardInstances.TryGetValue(t029CardInstanceId, out var t029CardInstance))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires selected cardInstanceId to exist.");
            }

            if (!gameState.players.TryGetValue(submitInputChoiceActionRequest.actorPlayerId, out var actorPlayerState))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires actorPlayerId to exist.");
            }

            if (t029CardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId ||
                t029CardInstance.zoneId != actorPlayerState.handZoneId ||
                !string.Equals(t029CardInstance.definitionId, "T029", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires selected T029 to be in actor hand zone.");
            }

            var movedEvent = zoneMovementService.moveCard(
                gameState,
                t029CardInstance,
                gameState.publicState.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(movedEvent);
            damageContext.isPrevented = true;
            damageContext.isImmune = true;
        }

        damageContext.suppressT029DamageImmunityPrompt = true;
        var producedEventsStartIndex = actionChainState.producedEvents.Count;
        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
        actionChainState.pendingContinuationKey = null;
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsStartIndex);
    }

    private static DamageContext loadT029PendingDamageContextOrThrow(ActionChainState actionChainState)
    {
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029DamageContextId)),
            targetCharacterInstanceId = new CharacterInstanceId(loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029TargetCharacterInstanceId)),
            baseDamageValue = (int)loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029BaseDamageValue),
            damageType = loadStringLocalState(actionChainState, DamageProcessor.LocalStateKeyT029DamageType),
            hasAppliedSourceDamageBonuses =
                actionChainState.localState.TryGetValue(
                    DamageProcessor.LocalStateKeyT029HasAppliedSourceDamageBonuses,
                    out var hasAppliedSourceDamageBonusesRaw) &&
                bool.TryParse(hasAppliedSourceDamageBonusesRaw, out var hasAppliedSourceDamageBonuses) &&
                hasAppliedSourceDamageBonuses,
        };

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourcePlayerId, out var sourcePlayerNumericId))
        {
            damageContext.sourcePlayerId = new PlayerId(sourcePlayerNumericId);
        }

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourceCardInstanceId, out var sourceCardNumericId))
        {
            damageContext.sourceCardInstanceId = new CardInstanceId(sourceCardNumericId);
        }

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourceCharacterInstanceId, out var sourceCharacterNumericId))
        {
            damageContext.sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterNumericId);
        }

        if (actionChainState.localState.TryGetValue(
                DamageProcessor.LocalStateKeyT029DefenseDeclarationKey,
                out var defenseDeclarationKey))
        {
            damageContext.defenseDeclarationKey = defenseDeclarationKey;
        }

        return damageContext;
    }

    private static void clearT029PendingDamageContext(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DamageContextId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourcePlayerId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourceCardInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourceCharacterInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029TargetCharacterInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029BaseDamageValue);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DamageType);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DefenseDeclarationKey);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029HasAppliedSourceDamageBonuses);
    }

    private static long loadLongLocalState(ActionChainState actionChainState, string key)
    {
        if (!tryLoadLongLocalState(actionChainState, key, out var value))
        {
            throw new InvalidOperationException($"T029 damage immunity continuation requires localState[{key}].");
        }

        return value;
    }

    private static bool tryLoadLongLocalState(ActionChainState actionChainState, string key, out long value)
    {
        value = 0;
        return actionChainState.localState.TryGetValue(key, out var serializedValue) &&
               long.TryParse(serializedValue, out value);
    }

    private static string loadStringLocalState(ActionChainState actionChainState, string key)
    {
        if (!actionChainState.localState.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"T029 damage immunity continuation requires localState[{key}].");
        }

        return value;
    }

    private static CardInstanceId parseT029BanishChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(DamageProcessor.ChoiceKeyT029BanishPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires choiceKey to be T029:decline or T029:banish:{cardId}.");
        }

        var cardIdSegment = choiceKey.Substring(DamageProcessor.ChoiceKeyT029BanishPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires numeric T029 cardInstanceId.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private void tryOpenTreasureArrivalEffectsFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (producedEventsStartIndex < 0 ||
            producedEventsStartIndex >= actionChainState.producedEvents.Count)
        {
            return;
        }

        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        var enteredSummonZoneCardInstanceIds = new List<CardInstanceId>();
        for (var eventIndex = producedEventsStartIndex; eventIndex < actionChainState.producedEvents.Count; eventIndex++)
        {
            if (actionChainState.producedEvents[eventIndex] is not CardMovedEvent cardMovedEvent)
            {
                continue;
            }

            if (cardMovedEvent.toZoneKey != ZoneKey.summonZone)
            {
                continue;
            }

            enteredSummonZoneCardInstanceIds.Add(cardMovedEvent.cardInstanceId);
        }

        if (enteredSummonZoneCardInstanceIds.Count == 0)
        {
            return;
        }

        treasureArrivalEffectRuntime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId,
            enteredSummonZoneCardInstanceIds);
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
                throw new InvalidOperationException("SubmitInputChoiceActionRequest requires either choiceKey=shackle:decline, or choiceKeys to contain exactly four unique values from currentInputContext.choiceKeys for continuation:turnStartShackleDiscard.");
            }

            return false;
        }

        if (continuationState.isTurnStartC001BarrierContinuation)
        {
            if (!TurnTransitionProcessor.isValidTurnStartC001BarrierChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:turnStartC001Barrier.");
            }

            return false;
        }

        if (continuationState.isC001SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C001 target choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC007SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C007 target choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC008SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C008 choice must be one of currentInputContext.choiceKeys.");
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

        if (TreasureOnPlayEffectRuntime.isT021OverlayCardsContinuationKey(continuationState.pendingContinuationKey))
        {
            TreasureOnPlayEffectRuntime.ensureValidT021OverlayChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (continuationState.isMechanicalJadeContinuation)
        {
            MechanicalJadeRuntime.ensureValidOverlayAfterKillChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (continuationState.isTreasureDefenseContinuation)
        {
            TreasureDefenseEffectRuntime.ensureValidT025ExtraDiscardChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
        }

        return false;
    }

    private static void ensureValidT029DamageImmunityChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(
                inputContextState.contextKey,
                DamageProcessor.ContextKeyT029DamageImmunity,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires currentInputContext.contextKey to match T029 immunity context.");
        }

        if (inputContextState.requiredPlayerId != submitInputChoiceActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires actorPlayerId to match currentInputContext.requiredPlayerId.");
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }
    }

    private static void ensureSubmitInputChoiceActorAllowed(
        InputContextState inputContextState,
        PlayerId actorPlayerId)
    {
        if (inputContextState.requiredPlayerIds.Count > 0)
        {
            for (var index = 0; index < inputContextState.requiredPlayerIds.Count; index++)
            {
                if (inputContextState.requiredPlayerIds[index] == actorPlayerId)
                {
                    return;
                }
            }

            throw new InvalidOperationException("SubmitInputChoiceActionRequest actorPlayerId is not in currentInputContext.requiredPlayerIds.");
        }

        if (inputContextState.requiredPlayerId.HasValue &&
            actorPlayerId == inputContextState.requiredPlayerId.Value)
        {
            return;
        }

        throw new InvalidOperationException("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.");
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

        if (continuationState.isTurnStartC001BarrierContinuation)
        {
            turnTransitionProcessor.continueTurnStartC001BarrierContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isC001SkillContinuation)
        {
            continueC001SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC007SkillContinuation)
        {
            continueC007SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC008SkillContinuation)
        {
            continueC008SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
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
            if (string.Equals(
                    continuationState.pendingContinuationKey,
                    AnomalyProcessor.ContinuationKeyA010ArrivalSetAside,
                    StringComparison.Ordinal))
            {
                treasureArrivalEffectRuntime.resumeArrivalQueueAfterExternalContinuation(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest.requestId);
            }

            return true;
        }

        return false;
    }
}




