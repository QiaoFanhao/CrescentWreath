using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.DamageSystem;

public sealed class DamageProcessor
{
    public const string ContinuationKeyT029DamageImmunity = "continuation:damage:T029:damageImmunity";
    public const string InputTypeKeyT029DamageImmunityChoice = "damage:T029:immunityChoice";
    public const string ContextKeyT029DamageImmunity = "damage:T029:immunity";
    public const string ChoiceKeyT029Decline = "T029:decline";
    public const string ChoiceKeyT029BanishPrefix = "T029:banish:";
    public const string LocalStateKeyT029DamageContextId = "damage:T029:damageContextId";
    public const string LocalStateKeyT029SourcePlayerId = "damage:T029:sourcePlayerId";
    public const string LocalStateKeyT029SourceCardInstanceId = "damage:T029:sourceCardInstanceId";
    public const string LocalStateKeyT029SourceCharacterInstanceId = "damage:T029:sourceCharacterInstanceId";
    public const string LocalStateKeyT029TargetCharacterInstanceId = "damage:T029:targetCharacterInstanceId";
    public const string LocalStateKeyT029BaseDamageValue = "damage:T029:baseDamageValue";
    public const string LocalStateKeyT029DamageType = "damage:T029:damageType";
    public const string LocalStateKeyT029DefenseDeclarationKey = "damage:T029:defenseDeclarationKey";

    private const string ResponseKeyCommitKill = "commitKill";
    private const string ResponseKeyReplaceKill = "replaceKill";
    private const string EndedExternalResolutionRejectedMessage = "DamageProcessor cannot accept external resolution calls when gameState.matchState is ended.";
    private const string DamageTypeKeyDirect = "direct";
    private const string DefinitionIdT029 = "T029";
    private const string StatusKeyBarrier = "Barrier";
    private const string StatusKeyBarrierLegacy = "status:barrier";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeyCharmLegacy = "status:charm";
    private const string StatusKeyPenetrate = "Penetrate";
    private const string StatusKeyPenetrateLegacy = "status:penetrate";

    public List<GameEvent> resolveDamage(GameState.GameState gameState, DamageContext damageContext)
    {
        ensureCanAcceptExternalResolution(gameState);

        var targetCharacterInstanceId = damageContext.targetCharacterInstanceId!.Value;
        var targetCharacter = gameState.characterInstances[targetCharacterInstanceId];

        if (tryOpenT029DamageImmunityInputContext(
                gameState,
                damageContext,
                targetCharacter,
                out var t029ImmunityEvents))
        {
            return t029ImmunityEvents;
        }

        var consumedShortEffectKeys = consumeSourceShortEffects(gameState, damageContext.sourcePlayerId);
        foreach (var shortEffectKey in consumedShortEffectKeys)
        {
            damageContext.appliedShortEffectKeys.Add(shortEffectKey);
        }

        var hasPenetrate = containsPenetrateEffect(consumedShortEffectKeys);
        var consumedCharmStatusKey = consumeCharmOnTargetPlayer(gameState, targetCharacter.ownerPlayerId);
        var consumedBarrierStatusKey = consumeBarrierOnTargetCharacter(gameState, targetCharacterInstanceId);
        var isBarrierPrevented = consumedBarrierStatusKey is not null;
        damageContext.isPrevented = damageContext.isPrevented || isBarrierPrevented;

        var hpBefore = targetCharacter.currentHp;
        var finalDamageValue = damageContext.baseDamageValue;
        if (damageContext.isPrevented)
        {
            finalDamageValue = 0;
        }
        else if (hasPenetrate &&
                 !string.Equals(damageContext.damageType, DamageTypeKeyDirect, StringComparison.Ordinal) &&
                 finalDamageValue <= 0)
        {
            finalDamageValue = 1;
        }

        targetCharacter.currentHp -= finalDamageValue;
        var hpAfter = targetCharacter.currentHp;

        damageContext.finalDamageValue = finalDamageValue;
        damageContext.didDealDamage = finalDamageValue > 0;

        var damageResolvedEvent = new DamageResolvedEvent
        {
            eventId = damageContext.damageContextId.Value,
            eventTypeKey = "damageResolved",
            damageContextId = damageContext.damageContextId,
            finalDamageValue = damageContext.finalDamageValue,
            didDealDamage = damageContext.didDealDamage,
        };

        var hpChangedEvent = new HpChangedEvent
        {
            eventId = damageContext.damageContextId.Value,
            eventTypeKey = "hpChanged",
            targetPlayerId = targetCharacter.ownerPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        };

        var producedEvents = new List<GameEvent> { damageResolvedEvent, hpChangedEvent };
        applyLeylineRewardIfDamageDealt(gameState, damageContext);
        tryEnterLethalAdjudicationFromDamage(
            gameState,
            producedEvents,
            damageContext,
            hpBefore,
            hpAfter,
            targetCharacterInstanceId,
            targetCharacter.ownerPlayerId);

        appendStatusChangedEventsForConsumedStatuses(
            producedEvents,
            damageContext,
            targetCharacterInstanceId,
            targetCharacter.ownerPlayerId,
            consumedShortEffectKeys,
            consumedCharmStatusKey,
            consumedBarrierStatusKey);
        return producedEvents;
    }

    public List<GameEvent> resolveDirectKill(GameState.GameState gameState, KillContext directKillContext)
    {
        ensureCanAcceptExternalResolution(gameState);

        if (!directKillContext.killedCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("resolveDirectKill requires directKillContext.killedCharacterInstanceId.");
        }

        var targetCharacter = gameState.characterInstances[directKillContext.killedCharacterInstanceId.Value];
        directKillContext.killedPlayerId = targetCharacter.ownerPlayerId;
        directKillContext.causedByDamage = false;
        directKillContext.sourceDamageContextId = null;

        var producedEvents = new List<GameEvent>();
        enterLethalAdjudication(
            gameState,
            producedEvents,
            directKillContext,
            directKillContext.killContextId);

        return producedEvents;
    }

    public List<GameEvent> resolvePendingKillResponse(
        GameState.GameState gameState,
        ResponseWindowState responseWindowState,
        string responseKey,
        long eventId)
    {
        ensureCanAcceptExternalResolution(gameState);
        throw new NotSupportedException("resolvePendingKillResponse is no longer supported because onKilledResponse is auto-resolved in lethal adjudication.");
    }

    private static void tryEnterLethalAdjudicationFromDamage(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        DamageContext damageContext,
        int hpBefore,
        int hpAfter,
        CharacterInstanceId targetCharacterInstanceId,
        PlayerId targetPlayerId)
    {
        var didTriggerKillByDamage = damageContext.didDealDamage && hpBefore > 0 && hpAfter <= 0;
        if (!didTriggerKillByDamage)
        {
            return;
        }

        var killContext = new KillContext
        {
            killContextId = damageContext.damageContextId.Value,
            killerPlayerId = damageContext.sourcePlayerId,
            killedCharacterInstanceId = targetCharacterInstanceId,
            killedPlayerId = targetPlayerId,
            causedByDamage = true,
            sourceDamageContextId = damageContext.damageContextId,
        };

        enterLethalAdjudication(
            gameState,
            producedEvents,
            killContext,
            damageContext.damageContextId.Value);
    }

    private static void enterLethalAdjudication(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        KillContext killContext,
        long eventId)
    {
        var killResponseWindowState = openPreKillResponseWindow(gameState, producedEvents, killContext, eventId);
        var responseKey = resolveAutomaticPreKillDecisionResponseKey(gameState, killContext);
        closePreKillResponseWindow(gameState, producedEvents, eventId, killResponseWindowState);
        applyPendingKillDecision(
            gameState,
            producedEvents,
            responseKey,
            killContext.killContextId,
            killContext.killerPlayerId,
            killContext.killedCharacterInstanceId!.Value,
            killContext.sourceDamageContextId,
            eventId);
    }

    private static string resolveAutomaticPreKillDecisionResponseKey(
        GameState.GameState gameState,
        KillContext killContext)
    {
        var targetCharacter = gameState.characterInstances[killContext.killedCharacterInstanceId!.Value];
        return targetCharacter.hasPendingOnKilledReplacement
            ? ResponseKeyReplaceKill
            : ResponseKeyCommitKill;
    }

    private static void applyPendingKillDecision(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        string responseKey,
        long killContextId,
        PlayerId? killerPlayerId,
        CharacterInstanceId killedCharacterInstanceId,
        DamageContextId? sourceDamageContextId,
        long eventId)
    {
        if (responseKey == ResponseKeyReplaceKill)
        {
            appendReplaceKillStabilization(
                gameState,
                producedEvents,
                killedCharacterInstanceId,
                eventId);
            return;
        }

        appendCommittedKillSuffix(
            gameState,
            producedEvents,
            killContextId,
            killerPlayerId,
            killedCharacterInstanceId,
            sourceDamageContextId,
            eventId);
    }

    private static void appendReplaceKillStabilization(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        CharacterInstanceId killedCharacterInstanceId,
        long eventId)
    {
        var targetCharacter = gameState.characterInstances[killedCharacterInstanceId];
        targetCharacter.hasPendingOnKilledReplacement = false;
        var hpBeforeRestoreWhenReplaced = targetCharacter.currentHp;
        targetCharacter.currentHp = targetCharacter.maxHp;

        if (hpBeforeRestoreWhenReplaced == targetCharacter.currentHp)
        {
            return;
        }

        producedEvents.Add(new HpChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "hpChanged",
            targetPlayerId = targetCharacter.ownerPlayerId,
            targetCharacterInstanceId = killedCharacterInstanceId,
            hpBefore = hpBeforeRestoreWhenReplaced,
            hpAfter = targetCharacter.currentHp,
            delta = targetCharacter.currentHp - hpBeforeRestoreWhenReplaced,
        });
    }

    private static void appendCommittedKillSuffix(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        long killContextId,
        PlayerId? killerPlayerId,
        CharacterInstanceId killedCharacterInstanceId,
        DamageContextId? sourceDamageContextId,
        long eventId)
    {
        var targetCharacter = gameState.characterInstances[killedCharacterInstanceId];
        var killedPlayerId = targetCharacter.ownerPlayerId;

        producedEvents.Add(new KillRecordedEvent
        {
            eventId = eventId,
            eventTypeKey = "killRecorded",
            killContextId = killContextId,
            killerPlayerId = killerPlayerId,
            killedCharacterInstanceId = killedCharacterInstanceId,
            sourceDamageContextId = sourceDamageContextId,
        });

        appendStatusClearedEventsForKilledCharacter(
            gameState,
            producedEvents,
            killedCharacterInstanceId,
            killedPlayerId,
            eventId);

        var killedTeamId = gameState.players[killedPlayerId].teamId;
        var killedTeamState = gameState.teams[killedTeamId];
        killedTeamState.killScore -= 1;
        applyMatchEndIfThresholdReached(gameState, killedTeamId, killedTeamState);

        // Even when matchState is written to ended here, this in-flight kill suffix must
        // continue to append reward draw / restore hp before returning to caller.
        producedEvents.AddRange(drawOneForKillReward(gameState, killerPlayerId, eventId));

        var hpBeforeRestore = targetCharacter.currentHp;
        targetCharacter.currentHp = targetCharacter.maxHp;

        producedEvents.Add(new HpChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "hpChanged",
            targetPlayerId = targetCharacter.ownerPlayerId,
            targetCharacterInstanceId = killedCharacterInstanceId,
            hpBefore = hpBeforeRestore,
            hpAfter = targetCharacter.currentHp,
            delta = targetCharacter.currentHp - hpBeforeRestore,
        });
    }

    private static ResponseWindowState openPreKillResponseWindow(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        KillContext killContext,
        long eventId)
    {
        if (!killContext.killedCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("openPreKillResponseWindow requires killContext.killedCharacterInstanceId.");
        }

        var killedCharacterInstanceId = killContext.killedCharacterInstanceId.Value;
        var killedPlayerId = gameState.characterInstances[killedCharacterInstanceId].ownerPlayerId;
        killContext.killedPlayerId = killedPlayerId;

        var killResponseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(killContext.killContextId),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "onKilledResponse",
            sourceActionChainId = null,
            currentResponderPlayerId = killedPlayerId,
            pendingKillTargetCharacterInstanceId = killedCharacterInstanceId,
            pendingKillKillerPlayerId = killContext.killerPlayerId,
            pendingKillSourceDamageContextId = killContext.sourceDamageContextId,
        };
        killResponseWindowState.responderPlayerIds.Add(killedPlayerId);

        gameState.currentResponseWindow = killResponseWindowState;

        producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = null,
            windowKindKey = "responseWindow",
            responseWindowId = killResponseWindowState.responseWindowId,
            responseWindowOriginType = killResponseWindowState.originType,
            isOpened = true,
        });

        return killResponseWindowState;
    }

    private static void ensureCanAcceptExternalResolution(GameState.GameState gameState)
    {
        if (gameState.matchState == GameState.MatchState.ended)
        {
            throw new InvalidOperationException(EndedExternalResolutionRejectedMessage);
        }
    }

    private static void applyLeylineRewardIfDamageDealt(
        GameState.GameState gameState,
        DamageContext damageContext)
    {
        if (!damageContext.didDealDamage ||
            !damageContext.sourcePlayerId.HasValue ||
            !damageContext.targetCharacterInstanceId.HasValue)
        {
            return;
        }

        var sourcePlayerId = damageContext.sourcePlayerId.Value;
        if (!gameState.players.TryGetValue(sourcePlayerId, out var sourcePlayerState))
        {
            return;
        }

        if (!gameState.teams.TryGetValue(sourcePlayerState.teamId, out var sourceTeamState))
        {
            return;
        }

        var targetCharacter = gameState.characterInstances[damageContext.targetCharacterInstanceId.Value];
        if (!gameState.players.TryGetValue(targetCharacter.ownerPlayerId, out var targetPlayerState))
        {
            return;
        }

        if (targetPlayerState.teamId.Equals(sourcePlayerState.teamId))
        {
            return;
        }

        sourceTeamState.leyline += 1;
    }

    private static bool tryOpenT029DamageImmunityInputContext(
        GameState.GameState gameState,
        DamageContext damageContext,
        CharacterInstance targetCharacter,
        out List<GameEvent> producedEvents)
    {
        producedEvents = new List<GameEvent>();
        if (damageContext.suppressT029DamageImmunityPrompt ||
            damageContext.baseDamageValue <= 0 ||
            !string.IsNullOrWhiteSpace(damageContext.defenseDeclarationKey) ||
            damageContext.sourcePlayerId is null ||
            !damageContext.targetCharacterInstanceId.HasValue ||
            gameState.currentInputContext is not null ||
            gameState.currentActionChain is null)
        {
            return false;
        }

        if (hasBarrierStatusOnTarget(gameState, damageContext.targetCharacterInstanceId.Value))
        {
            return false;
        }

        if (!gameState.players.TryGetValue(damageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.players.TryGetValue(targetCharacter.ownerPlayerId, out var targetPlayerState) ||
            sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            return false;
        }

        if (!gameState.zones.TryGetValue(targetPlayerState.handZoneId, out var targetHandZoneState))
        {
            return false;
        }

        var t029ChoiceKeys = new List<string>
        {
            ChoiceKeyT029Decline,
        };
        foreach (var handCardInstanceId in targetHandZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(handCardInstanceId, out var handCardInstance))
            {
                continue;
            }

            if (handCardInstance.ownerPlayerId != targetCharacter.ownerPlayerId ||
                !string.Equals(handCardInstance.definitionId, DefinitionIdT029, StringComparison.Ordinal))
            {
                continue;
            }

            t029ChoiceKeys.Add(ChoiceKeyT029BanishPrefix + handCardInstanceId.Value);
        }

        if (t029ChoiceKeys.Count <= 1)
        {
            return false;
        }

        var actionChainState = gameState.currentActionChain;
        saveT029DamageContext(actionChainState, damageContext);

        var inputContextId = new InputContextId(damageContext.damageContextId.Value);
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = targetCharacter.ownerPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = InputTypeKeyT029DamageImmunityChoice,
            contextKey = ContextKeyT029DamageImmunity,
        };
        inputContextState.choiceKeys.AddRange(t029ChoiceKeys);

        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = ContinuationKeyT029DamageImmunity;
        actionChainState.isCompleted = false;
        producedEvents.Add(new InteractionWindowEvent
        {
            eventId = damageContext.damageContextId.Value,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });

        return true;
    }

    private static bool hasBarrierStatusOnTarget(GameState.GameState gameState, CharacterInstanceId targetCharacterInstanceId)
    {
        return StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, StatusKeyBarrier) ||
               StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, StatusKeyBarrierLegacy);
    }

    private static void saveT029DamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        actionChainState.localState[LocalStateKeyT029DamageContextId] = damageContext.damageContextId.Value.ToString();
        if (damageContext.sourcePlayerId.HasValue)
        {
            actionChainState.localState[LocalStateKeyT029SourcePlayerId] = damageContext.sourcePlayerId.Value.Value.ToString();
        }

        if (damageContext.sourceCardInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyT029SourceCardInstanceId] = damageContext.sourceCardInstanceId.Value.Value.ToString();
        }

        if (damageContext.sourceCharacterInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyT029SourceCharacterInstanceId] = damageContext.sourceCharacterInstanceId.Value.Value.ToString();
        }

        actionChainState.localState[LocalStateKeyT029TargetCharacterInstanceId] =
            damageContext.targetCharacterInstanceId!.Value.Value.ToString();
        actionChainState.localState[LocalStateKeyT029BaseDamageValue] = damageContext.baseDamageValue.ToString();
        actionChainState.localState[LocalStateKeyT029DamageType] = damageContext.damageType;
        if (!string.IsNullOrWhiteSpace(damageContext.defenseDeclarationKey))
        {
            actionChainState.localState[LocalStateKeyT029DefenseDeclarationKey] = damageContext.defenseDeclarationKey;
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyT029DefenseDeclarationKey);
        }
    }

    private static void applyMatchEndIfThresholdReached(
        GameState.GameState gameState,
        TeamId killedTeamId,
        RuleCore.GameState.TeamState killedTeamState)
    {
        if (killedTeamState.killScore > 0)
        {
            return;
        }

        gameState.matchState = GameState.MatchState.ended;
        gameState.winnerTeamId = gameState.teams.Keys.First(teamId => !teamId.Equals(killedTeamId));
    }

    private static void closePreKillResponseWindow(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        long eventId,
        ResponseWindowState killResponseWindowState)
    {
        gameState.currentResponseWindow = null;

        producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = null,
            windowKindKey = "responseWindow",
            responseWindowId = killResponseWindowState.responseWindowId,
            responseWindowOriginType = killResponseWindowState.originType,
            isOpened = false,
        });
    }

    private static List<string> consumeSourceShortEffects(
        GameState.GameState gameState,
        PlayerId? sourcePlayerId)
    {
        if (!sourcePlayerId.HasValue)
        {
            return new List<string>();
        }

        return StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, sourcePlayerId.Value);
    }

    private static string? consumeBarrierOnTargetCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        var consumedBarrierStatusKey = consumeFirstMatchingStatusOnCharacter(
            gameState,
            targetCharacterInstanceId,
            StatusKeyBarrier);
        if (consumedBarrierStatusKey is not null)
        {
            return consumedBarrierStatusKey;
        }

        return consumeFirstMatchingStatusOnCharacter(
            gameState,
            targetCharacterInstanceId,
            StatusKeyBarrierLegacy);
    }

    private static string? consumeCharmOnTargetPlayer(
        GameState.GameState gameState,
        PlayerId targetPlayerId)
    {
        var consumedCharmStatusKey = consumeFirstMatchingStatusOnPlayer(
            gameState,
            targetPlayerId,
            StatusKeyCharm);
        if (consumedCharmStatusKey is not null)
        {
            return consumedCharmStatusKey;
        }

        return consumeFirstMatchingStatusOnPlayer(
            gameState,
            targetPlayerId,
            StatusKeyCharmLegacy);
    }

    private static string? consumeFirstMatchingStatusOnCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        string statusKey)
    {
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            if (statusInstance.targetCharacterInstanceId != targetCharacterInstanceId)
            {
                continue;
            }

            if (!string.Equals(statusInstance.statusKey, statusKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (statusInstance.stackCount > 1)
            {
                statusInstance.stackCount -= 1;
            }
            else
            {
                gameState.statusInstances.RemoveAt(index);
            }

            return statusInstance.statusKey;
        }

        return null;
    }

    private static string? consumeFirstMatchingStatusOnPlayer(
        GameState.GameState gameState,
        PlayerId targetPlayerId,
        string statusKey)
    {
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            if (statusInstance.targetPlayerId != targetPlayerId)
            {
                continue;
            }

            if (!string.Equals(statusInstance.statusKey, statusKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (statusInstance.stackCount > 1)
            {
                statusInstance.stackCount -= 1;
            }
            else
            {
                gameState.statusInstances.RemoveAt(index);
            }

            return statusInstance.statusKey;
        }

        return null;
    }

    private static bool containsPenetrateEffect(List<string> consumedShortEffectKeys)
    {
        foreach (var consumedShortEffectKey in consumedShortEffectKeys)
        {
            if (string.Equals(consumedShortEffectKey, StatusKeyPenetrate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(consumedShortEffectKey, StatusKeyPenetrateLegacy, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void appendStatusChangedEventsForConsumedStatuses(
        List<GameEvent> producedEvents,
        DamageContext damageContext,
        CharacterInstanceId targetCharacterInstanceId,
        PlayerId targetPlayerId,
        List<string> consumedShortEffectKeys,
        string? consumedCharmStatusKey,
        string? consumedBarrierStatusKey)
    {
        var eventId = damageContext.damageContextId.Value;
        if (consumedBarrierStatusKey is not null)
        {
            producedEvents.Add(new StatusChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = null,
                statusKey = normalizeConsumedStatusKey(consumedBarrierStatusKey),
                targetCharacterInstanceId = targetCharacterInstanceId,
                isApplied = false,
            });
        }

        if (consumedCharmStatusKey is not null)
        {
            producedEvents.Add(new StatusChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = null,
                statusKey = normalizeConsumedStatusKey(consumedCharmStatusKey),
                targetPlayerId = targetPlayerId,
                isApplied = false,
            });
        }

        if (!damageContext.sourcePlayerId.HasValue)
        {
            return;
        }

        foreach (var consumedShortEffectKey in consumedShortEffectKeys)
        {
            if (!string.Equals(consumedShortEffectKey, StatusKeyPenetrate, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(consumedShortEffectKey, StatusKeyPenetrateLegacy, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            producedEvents.Add(new StatusChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = null,
                statusKey = StatusKeyPenetrate,
                targetPlayerId = damageContext.sourcePlayerId.Value,
                isApplied = false,
            });
        }
    }

    private static string normalizeConsumedStatusKey(string consumedStatusKey)
    {
        if (string.Equals(consumedStatusKey, StatusKeyBarrierLegacy, StringComparison.OrdinalIgnoreCase))
        {
            return StatusKeyBarrier;
        }

        if (string.Equals(consumedStatusKey, StatusKeyPenetrateLegacy, StringComparison.OrdinalIgnoreCase))
        {
            return StatusKeyPenetrate;
        }

        if (string.Equals(consumedStatusKey, StatusKeyCharmLegacy, StringComparison.OrdinalIgnoreCase))
        {
            return StatusKeyCharm;
        }

        return consumedStatusKey;
    }

    private static List<GameEvent> drawOneForKillReward(
        GameState.GameState gameState,
        PlayerId? killerPlayerId,
        long eventId)
    {
        var producedEvents = new List<GameEvent>();
        if (!killerPlayerId.HasValue)
        {
            return producedEvents;
        }

        var killerPlayerState = gameState.players[killerPlayerId.Value];
        var deckZoneState = gameState.zones[killerPlayerState.deckZoneId];
        var handZoneState = gameState.zones[killerPlayerState.handZoneId];
        var discardZoneState = gameState.zones[killerPlayerState.discardZoneId];

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardInstanceIds = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
            foreach (var discardedCardInstanceId in discardCardInstanceIds)
            {
                var discardedCardInstance = gameState.cardInstances[discardedCardInstanceId];
                producedEvents.Add(moveCardForKillReward(
                    gameState,
                    discardedCardInstance,
                    deckZoneState.zoneId,
                    CardMoveReason.returnToSource,
                    eventId));
            }
        }

        if (deckZoneState.cardInstanceIds.Count == 0)
        {
            return producedEvents;
        }

        var topDeckCardInstanceId = deckZoneState.cardInstanceIds[0];
        var topDeckCardInstance = gameState.cardInstances[topDeckCardInstanceId];
        producedEvents.Add(moveCardForKillReward(
            gameState,
            topDeckCardInstance,
            handZoneState.zoneId,
            CardMoveReason.draw,
            eventId));

        return producedEvents;
    }

    private static CardMovedEvent moveCardForKillReward(
        GameState.GameState gameState,
        CardInstance cardInstance,
        ZoneId targetZoneId,
        CardMoveReason moveReason,
        long eventId)
    {
        var fromZoneState = gameState.zones[cardInstance.zoneId];
        var toZoneState = gameState.zones[targetZoneId];

        fromZoneState.cardInstanceIds.Remove(cardInstance.cardInstanceId);
        toZoneState.cardInstanceIds.Add(cardInstance.cardInstanceId);
        cardInstance.zoneId = targetZoneId;
        cardInstance.zoneKey = toZoneState.zoneType;

        return new CardMovedEvent
        {
            eventId = eventId,
            eventTypeKey = "cardMoved",
            sourceActionChainId = null,
            cardInstanceId = cardInstance.cardInstanceId,
            fromZoneKey = fromZoneState.zoneType,
            toZoneKey = toZoneState.zoneType,
            moveReason = moveReason,
        };
    }

    private static void appendStatusClearedEventsForKilledCharacter(
        GameState.GameState gameState,
        List<GameEvent> producedEvents,
        CharacterInstanceId killedCharacterInstanceId,
        PlayerId killedPlayerId,
        long eventId)
    {
        var characterStatuses = StatusRuntime.queryStatusesForCharacter(gameState, killedCharacterInstanceId);
        if (characterStatuses.Count <= 0)
        {
            return;
        }

        var distinctStatusKeys = characterStatuses
            .Select(statusInstance => statusInstance.statusKey)
            .Where(statusKey => !string.IsNullOrWhiteSpace(statusKey))
            .Distinct(StringComparer.Ordinal);

        foreach (var statusKey in distinctStatusKeys)
        {
            var removedStatuses = StatusRuntime.removeStatusesOnCharacter(
                gameState,
                killedCharacterInstanceId,
                statusKey);
            foreach (var removedStatus in removedStatuses)
            {
                producedEvents.Add(new StatusChangedEvent
                {
                    eventId = eventId,
                    eventTypeKey = "statusChanged",
                    sourceActionChainId = null,
                    statusKey = removedStatus.statusKey,
                    targetPlayerId = killedPlayerId,
                    targetCharacterInstanceId = killedCharacterInstanceId,
                    isApplied = false,
                });
            }
        }
    }
}
