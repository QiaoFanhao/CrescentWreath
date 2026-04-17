using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyArrivalRuntime
{
    public const string ArrivalStepKeyLegacyNoop = "legacyNoop";
    public const string ArrivalStepKeyA003SelectOpponentApplyShackleAndGrantYuukaTeamLeyline = "selectOpponentApplyShackleAndGrantYuukaTeamLeyline";
    public const string ArrivalStepKeyBanishSummonZoneToGap = "banishSummonZoneToGap";
    public const string LegacyArrivalStepKeyBanishPublicTreasureDeckToGap = "banishPublicTreasureDeckToGap";
    public const string ArrivalStepKeyApplySealToLeadingTeamActiveCharacters = "applySealToLeadingTeamActiveCharacters";
    public const string ArrivalStepKeyDirectSummonFromSummonZoneWithInput = "directSummonFromSummonZoneWithInput";
    public const string ArrivalStepKeyApplyOptionalBanishForAllPlayersAndExtraOptionalForC007 = "applyOptionalBanishForAllPlayersAndExtraOptionalForC007";
    public const string ArrivalStepKeyApplyA001RaceFlowWithRemiliaHealInput = "applyA001RaceFlowWithRemiliaHealInput";
    public const string ArrivalStepKeyApplyA006RaceFlowWithDefenseDiscardInput = "applyA006RaceFlowWithDefenseDiscardInput";
    private const string KazamiYuukaDefinitionId = "C023";

    private static readonly HashSet<string> SealArrivalExemptCharacterDefinitionIds = new(StringComparer.Ordinal)
    {
        "C009",
    };

    public static bool executeOnFlip(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        ZoneMovementService zoneMovementService,
        AnomalyDefinition anomalyDefinition,
        AnomalyArrivalInputRuntime anomalyArrivalInputRuntime,
        string a003ArrivalContinuationKey,
        string a005ArrivalContinuationKey,
        string a007ArrivalContinuationKey,
        string a001ArrivalContinuationKey,
        string a006ArrivalContinuationKey)
    {
        if (gameState is null)
        {
            throw new ArgumentNullException(nameof(gameState));
        }

        if (actionChainState is null)
        {
            throw new ArgumentNullException(nameof(actionChainState));
        }

        if (zoneMovementService is null)
        {
            throw new ArgumentNullException(nameof(zoneMovementService));
        }

        if (anomalyArrivalInputRuntime is null)
        {
            throw new ArgumentNullException(nameof(anomalyArrivalInputRuntime));
        }

        foreach (var arrivalStep in anomalyDefinition.arrivalSteps)
        {
            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyLegacyNoop, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyBanishSummonZoneToGap, StringComparison.Ordinal) ||
                string.Equals(arrivalStep.arrivalStepKey, LegacyArrivalStepKeyBanishPublicTreasureDeckToGap, StringComparison.Ordinal))
            {
                executeBanishSummonZoneToGap(
                    gameState,
                    actionChainState,
                    requestId,
                    zoneMovementService);
                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyA003SelectOpponentApplyShackleAndGrantYuukaTeamLeyline, StringComparison.Ordinal))
            {
                if (!actionChainState.actorPlayerId.HasValue)
                {
                    throw new InvalidOperationException("Anomaly arrival runtime requires actionChain.actorPlayerId for arrivalStepKey: selectOpponentApplyShackleAndGrantYuukaTeamLeyline.");
                }

                executeGrantYuukaTeamLeyline(gameState);
                var isSuspendedByInput = anomalyArrivalInputRuntime.tryOpenA003ArrivalSelectOpponentShackleInputContext(
                    gameState,
                    actionChainState,
                    actionChainState.actorPlayerId.Value,
                    a003ArrivalContinuationKey,
                    requestId);
                if (isSuspendedByInput)
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyApplySealToLeadingTeamActiveCharacters, StringComparison.Ordinal))
            {
                executeApplySealToLeadingTeamActiveCharacters(
                    gameState,
                    actionChainState);
                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyDirectSummonFromSummonZoneWithInput, StringComparison.Ordinal))
            {
                if (!actionChainState.actorPlayerId.HasValue)
                {
                    throw new InvalidOperationException("Anomaly arrival runtime requires actionChain.actorPlayerId for arrivalStepKey: directSummonFromSummonZoneWithInput.");
                }

                var isSuspendedByInput = anomalyArrivalInputRuntime.tryOpenA005ArrivalDirectSummonInputContext(
                    gameState,
                    actionChainState,
                    actionChainState.actorPlayerId.Value,
                    a005ArrivalContinuationKey,
                    requestId);
                if (isSuspendedByInput)
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyApplyOptionalBanishForAllPlayersAndExtraOptionalForC007, StringComparison.Ordinal))
            {
                var isSuspendedByInput = anomalyArrivalInputRuntime.tryOpenA007ArrivalOptionalBanishInputContext(
                    gameState,
                    actionChainState,
                    a007ArrivalContinuationKey,
                    requestId);
                if (isSuspendedByInput)
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyApplyA001RaceFlowWithRemiliaHealInput, StringComparison.Ordinal))
            {
                var isSuspendedByInput = anomalyArrivalInputRuntime.executeA001ArrivalNonHumanDrawAndMaybeOpenHumanDiscardInputContext(
                    gameState,
                    actionChainState,
                    a001ArrivalContinuationKey,
                    requestId);
                if (isSuspendedByInput)
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(arrivalStep.arrivalStepKey, ArrivalStepKeyApplyA006RaceFlowWithDefenseDiscardInput, StringComparison.Ordinal))
            {
                var isSuspendedByInput = anomalyArrivalInputRuntime.executeA006ArrivalMaybeOpenHumanDefenseDiscardInputContextAndMaybeDrawNonHuman(
                    gameState,
                    actionChainState,
                    a006ArrivalContinuationKey,
                    requestId);
                if (isSuspendedByInput)
                {
                    return true;
                }

                continue;
            }

            throw new InvalidOperationException(
                "Anomaly arrival runtime does not support arrivalStepKey: " + arrivalStep.arrivalStepKey + ".");
        }

        return false;
    }

    private static void executeBanishSummonZoneToGap(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        ZoneMovementService zoneMovementService)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("Anomaly arrival runtime requires gameState.publicState for arrivalStepKey: banishSummonZoneToGap.");
        }

        var publicState = gameState.publicState;
        if (!gameState.zones.TryGetValue(publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("Anomaly arrival runtime requires summonZoneId to exist for arrivalStepKey: banishSummonZoneToGap.");
        }

        if (!gameState.zones.ContainsKey(publicState.gapZoneId))
        {
            throw new InvalidOperationException("Anomaly arrival runtime requires gapZoneId to exist for arrivalStepKey: banishSummonZoneToGap.");
        }

        if (summonZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var cardInstanceIdsToBanish = new List<CardInstanceId>(summonZoneState.cardInstanceIds);
        foreach (var cardInstanceId in cardInstanceIdsToBanish)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                throw new InvalidOperationException("Anomaly arrival runtime requires all summonZone card instances to exist for arrivalStepKey: banishSummonZoneToGap.");
            }

            var movedEvent = zoneMovementService.moveCard(
                gameState,
                cardInstance,
                publicState.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                requestId);
            actionChainState.producedEvents.Add(movedEvent);
        }
    }

    private static void executeApplySealToLeadingTeamActiveCharacters(
        RuleCore.GameState.GameState gameState,
        ActionChainState actionChainState)
    {
        if (gameState.teams.Count == 0)
        {
            return;
        }

        TeamState? leadingTeamState = null;
        var highestKillScore = int.MinValue;
        var hasTieAtHighest = false;

        foreach (var teamStateEntry in gameState.teams)
        {
            var teamState = teamStateEntry.Value;
            if (teamState.killScore > highestKillScore)
            {
                highestKillScore = teamState.killScore;
                leadingTeamState = teamState;
                hasTieAtHighest = false;
                continue;
            }

            if (teamState.killScore == highestKillScore)
            {
                hasTieAtHighest = true;
            }
        }

        if (leadingTeamState is null || hasTieAtHighest)
        {
            return;
        }

        foreach (var leadingPlayerId in leadingTeamState.memberPlayerIds)
        {
            if (!gameState.players.TryGetValue(leadingPlayerId, out var leadingPlayerState))
            {
                throw new InvalidOperationException("Anomaly arrival runtime requires leading team players to exist for arrivalStepKey: applySealToLeadingTeamActiveCharacters.");
            }

            if (!leadingPlayerState.activeCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("Anomaly arrival runtime requires activeCharacterInstanceId for leading team players for arrivalStepKey: applySealToLeadingTeamActiveCharacters.");
            }

            var activeCharacterInstanceId = leadingPlayerState.activeCharacterInstanceId.Value;
            if (!gameState.characterInstances.TryGetValue(activeCharacterInstanceId, out var activeCharacterInstance))
            {
                throw new InvalidOperationException("Anomaly arrival runtime requires active character instances to exist for arrivalStepKey: applySealToLeadingTeamActiveCharacters.");
            }

            if (!activeCharacterInstance.isInPlay || !activeCharacterInstance.isAlive)
            {
                continue;
            }

            if (SealArrivalExemptCharacterDefinitionIds.Contains(activeCharacterInstance.definitionId))
            {
                continue;
            }

            StatusRuntime.applyStatus(gameState, new StatusInstance
            {
                statusKey = "Seal",
                applierPlayerId = actionChainState.actorPlayerId,
                targetCharacterInstanceId = activeCharacterInstanceId,
                stackCount = 1,
            });
        }
    }

    private static void executeGrantYuukaTeamLeyline(RuleCore.GameState.GameState gameState)
    {
        var grantedTeamIds = new HashSet<TeamId>();
        foreach (var playerStateEntry in gameState.players)
        {
            var playerState = playerStateEntry.Value;
            if (!playerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
            {
                continue;
            }

            if (!string.Equals(activeCharacterInstance.definitionId, KazamiYuukaDefinitionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!gameState.teams.TryGetValue(playerState.teamId, out var teamState))
            {
                continue;
            }

            if (grantedTeamIds.Add(playerState.teamId))
            {
                teamState.leyline += 1;
            }
        }
    }
}


