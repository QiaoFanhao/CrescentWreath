using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyRewardExecutor
{
    private const string DurationTypeUntilEndOfTurn = "untilEndOfTurn";
    private const string RaceTagNonHuman = "nonHuman";

    public static bool tryValidateRewardContext(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition,
        out string? failedReasonKey)
    {
        if (!AnomalyRewardActionFactory.tryCreateActions(
                anomalyDefinition,
                out var rewardActions,
                out failedReasonKey))
        {
            return false;
        }

        foreach (var rewardAction in rewardActions)
        {
            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyTeamDelta,
                    StringComparison.Ordinal) ||
                string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyHealFriendlyNonHumanActiveCharactersToMaxHp,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyApplyStatusToTargetOpponent,
                    StringComparison.Ordinal))
            {
                if (!targetPlayerId.HasValue)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    failedReasonKey = AnomalyValidationFailureKeys.TargetPlayerRequired;
                    return false;
                }

                if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
                    return false;
                }

                if (!gameState.players.TryGetValue(targetPlayerId.Value, out var targetPlayerState))
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    failedReasonKey = AnomalyValidationFailureKeys.TargetPlayerMissing;
                    return false;
                }

                if (actorPlayerState.teamId == targetPlayerState.teamId)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    failedReasonKey = AnomalyValidationFailureKeys.TargetPlayerMustBeOpponent;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(rewardAction.statusKey))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardStatusKeyMissing;
                    return false;
                }

                var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(rewardAction.statusKey);
                var statusPolicy = StatusPolicyTable.resolvePolicy(
                    normalizedStatusKey,
                    new StatusInstance
                    {
                        statusKey = normalizedStatusKey,
                        targetPlayerId = targetPlayerId.Value,
                    });

                if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.character)
                {
                    if (!targetPlayerState.activeCharacterInstanceId.HasValue)
                    {
                        if (rewardAction.isOptional)
                        {
                            continue;
                        }

                        failedReasonKey = AnomalyValidationFailureKeys.ActiveCharacterMissing;
                        return false;
                    }

                    var targetCharacterInstanceId = targetPlayerState.activeCharacterInstanceId.Value;
                    if (!gameState.characterInstances.ContainsKey(targetCharacterInstanceId))
                    {
                        if (rewardAction.isOptional)
                        {
                            continue;
                        }

                        failedReasonKey = AnomalyValidationFailureKeys.ActiveCharacterMissing;
                        return false;
                    }
                }

                continue;
            }

            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyMoveCardFromZoneToZone,
                    StringComparison.Ordinal))
            {
                if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.ActorPlayerStateMissing;
                    return false;
                }

                if (!tryResolveActorOwnedZoneId(actorPlayerState, rewardAction.fromZoneKey, out var fromZoneId))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneUnsupported;
                    return false;
                }

                if (!tryResolveActorOwnedZoneId(actorPlayerState, rewardAction.toZoneKey, out var toZoneId))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardTargetZoneUnsupported;
                    return false;
                }

                if (!gameState.zones.TryGetValue(fromZoneId, out var fromZoneState))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardSourceZoneMissing;
                    return false;
                }

                if (!gameState.zones.ContainsKey(toZoneId))
                {
                    failedReasonKey = AnomalyValidationFailureKeys.RewardTargetZoneMissing;
                    return false;
                }

                if (fromZoneState.cardInstanceIds.Count == 0)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    failedReasonKey = AnomalyValidationFailureKeys.RewardSourceCardMissing;
                    return false;
                }

                var sourceCardInstanceId = fromZoneState.cardInstanceIds[0];
                if (!gameState.cardInstances.ContainsKey(sourceCardInstanceId))
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    failedReasonKey = AnomalyValidationFailureKeys.RewardSourceCardMissing;
                    return false;
                }

                continue;
            }

            failedReasonKey = AnomalyValidationFailureKeys.UnsupportedResolveReward;
            return false;
        }

        failedReasonKey = null;
        return true;
    }

    public static void executeSteps(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        PlayerId? targetPlayerId,
        AnomalyDefinition anomalyDefinition)
    {
        if (!AnomalyRewardActionFactory.tryCreateActions(
                anomalyDefinition,
                out var rewardActions,
                out _))
        {
            if (anomalyDefinition.rewardSteps.Count > 0)
            {
                throw new InvalidOperationException("TryResolveAnomalyActionRequest currently only supports rewardStepKey=teamDelta, rewardStepKey=applyStatusToTargetOpponent, rewardStepKey=healFriendlyNonHumanActiveCharactersToMaxHp, or rewardStepKey=moveCardFromZoneToZone.");
            }

            throw new InvalidOperationException("TryResolveAnomalyActionRequest currently only supports resolveRewardKey=none, resolveRewardKey=teamDeltaReward, or resolveRewardKey=applyStatusToTargetOpponent.");
        }

        foreach (var rewardAction in rewardActions)
        {
            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyTeamDelta,
                    StringComparison.Ordinal))
            {
                applyTeamDeltaAction(gameState, actorPlayerId, rewardAction);
                continue;
            }

            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyHealFriendlyNonHumanActiveCharactersToMaxHp,
                    StringComparison.Ordinal))
            {
                applyHealFriendlyNonHumanActiveCharactersToMaxHpAction(gameState, actorPlayerId);
                continue;
            }

            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyApplyStatusToTargetOpponent,
                    StringComparison.Ordinal))
            {
                if (!targetPlayerId.HasValue)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires targetPlayerId for applyStatusToTargetOpponent reward.");
                }

                if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor player state for reward application.");
                }

                if (!gameState.players.TryGetValue(targetPlayerId.Value, out var targetPlayerState))
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires target player state for applyStatusToTargetOpponent reward.");
                }

                if (actorPlayerState.teamId == targetPlayerState.teamId)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires targetPlayerId to belong to opponent team for applyStatusToTargetOpponent reward.");
                }

                if (string.IsNullOrWhiteSpace(rewardAction.statusKey))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward statusKey for applyStatusToTargetOpponent reward.");
                }

                var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(rewardAction.statusKey);
                var statusPolicy = StatusPolicyTable.resolvePolicy(
                    normalizedStatusKey,
                    new StatusInstance
                    {
                        statusKey = normalizedStatusKey,
                        targetPlayerId = targetPlayerId.Value,
                    });

                var statusInstance = new StatusInstance
                {
                    statusKey = normalizedStatusKey,
                    applierPlayerId = actorPlayerId,
                    durationTypeKey = DurationTypeUntilEndOfTurn,
                    stackCount = 1,
                };

                if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.character)
                {
                    if (!targetPlayerState.activeCharacterInstanceId.HasValue)
                    {
                        if (rewardAction.isOptional)
                        {
                            continue;
                        }

                        throw new InvalidOperationException("TryResolveAnomalyActionRequest requires target player active character for applyStatusToTargetOpponent reward.");
                    }

                    var targetCharacterInstanceId = targetPlayerState.activeCharacterInstanceId.Value;
                    if (!gameState.characterInstances.ContainsKey(targetCharacterInstanceId))
                    {
                        if (rewardAction.isOptional)
                        {
                            continue;
                        }

                        throw new InvalidOperationException("TryResolveAnomalyActionRequest requires target player active character for applyStatusToTargetOpponent reward.");
                    }

                    statusInstance.targetCharacterInstanceId = targetCharacterInstanceId;
                }
                else
                {
                    statusInstance.targetPlayerId = targetPlayerId.Value;
                }

                StatusRuntime.applyStatus(gameState, statusInstance);
                continue;
            }

            if (string.Equals(
                    rewardAction.rewardActionKey,
                    AnomalyRewardActionFactory.RewardActionKeyMoveCardFromZoneToZone,
                    StringComparison.Ordinal))
            {
                if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor player state for reward application.");
                }

                if (!tryResolveActorOwnedZoneId(actorPlayerState, rewardAction.fromZoneKey, out var fromZoneId))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward source zone to be one of deck/hand/discard/field/characterSetAside.");
                }

                if (!tryResolveActorOwnedZoneId(actorPlayerState, rewardAction.toZoneKey, out var toZoneId))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward target zone to be one of deck/hand/discard/field/characterSetAside.");
                }

                if (!gameState.zones.TryGetValue(fromZoneId, out var fromZoneState))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward source zone state.");
                }

                if (!gameState.zones.ContainsKey(toZoneId))
                {
                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward target zone state.");
                }

                if (fromZoneState.cardInstanceIds.Count == 0)
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward source zone card.");
                }

                var sourceCardInstanceId = fromZoneState.cardInstanceIds[0];
                if (!gameState.cardInstances.TryGetValue(sourceCardInstanceId, out var sourceCardInstance))
                {
                    if (rewardAction.isOptional)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("TryResolveAnomalyActionRequest requires reward source zone card.");
                }

                var zoneMovementService = new ZoneMovementService();
                _ = zoneMovementService.moveCard(
                    gameState,
                    sourceCardInstance,
                    toZoneId,
                    CardMoveReason.returnToSource,
                    new ActionChainId(0),
                    eventId: 0);
                continue;
            }

            throw new InvalidOperationException("TryResolveAnomalyActionRequest currently only supports rewardStepKey=teamDelta, rewardStepKey=applyStatusToTargetOpponent, rewardStepKey=healFriendlyNonHumanActiveCharactersToMaxHp, or rewardStepKey=moveCardFromZoneToZone.");
        }
    }

    private static void applyTeamDeltaAction(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId,
        AnomalyRewardAction rewardAction)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor player state for reward application.");
        }

        if (!gameState.teams.TryGetValue(actorPlayerState.teamId, out var actorTeamState))
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor team state for reward application.");
        }

        var foundOpponentTeam = false;
        RuleCore.GameState.TeamState? opponentTeamState = null;
        foreach (var teamStateEntry in gameState.teams)
        {
            if (teamStateEntry.Key == actorPlayerState.teamId)
            {
                continue;
            }

            opponentTeamState = teamStateEntry.Value;
            foundOpponentTeam = true;
            break;
        }

        if (!foundOpponentTeam || opponentTeamState is null)
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires opponent team state for reward application.");
        }

        actorTeamState.leyline += rewardAction.actorTeamLeylineDelta;
        var nextOpponentKillScore = opponentTeamState.killScore + rewardAction.opponentTeamKillScoreDelta;
        opponentTeamState.killScore = Math.Max(0, nextOpponentKillScore);
    }

    private static void applyHealFriendlyNonHumanActiveCharactersToMaxHpAction(
        RuleCore.GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("TryResolveAnomalyActionRequest requires actor player state for reward application.");
        }

        var actorTeamId = actorPlayerState.teamId;
        foreach (var playerEntry in gameState.players)
        {
            var friendlyPlayerState = playerEntry.Value;
            if (friendlyPlayerState.teamId != actorTeamId || !friendlyPlayerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            var characterInstanceId = friendlyPlayerState.activeCharacterInstanceId.Value;
            if (!gameState.characterInstances.TryGetValue(characterInstanceId, out var characterInstance))
            {
                continue;
            }

            if (!isNonHumanCharacter(characterInstance))
            {
                continue;
            }

            characterInstance.currentHp = characterInstance.maxHp;
        }
    }

    private static bool isNonHumanCharacter(CharacterInstance characterInstance)
    {
        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        foreach (var raceTag in characterDefinition.raceTags)
        {
            if (string.Equals(raceTag, RaceTagNonHuman, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool tryResolveActorOwnedZoneId(
        RuleCore.GameState.PlayerState actorPlayerState,
        string zoneKey,
        out ZoneId zoneId)
    {
        if (string.Equals(zoneKey, ZoneKey.deck.ToString(), StringComparison.Ordinal))
        {
            zoneId = actorPlayerState.deckZoneId;
            return true;
        }

        if (string.Equals(zoneKey, ZoneKey.hand.ToString(), StringComparison.Ordinal))
        {
            zoneId = actorPlayerState.handZoneId;
            return true;
        }

        if (string.Equals(zoneKey, ZoneKey.discard.ToString(), StringComparison.Ordinal))
        {
            zoneId = actorPlayerState.discardZoneId;
            return true;
        }

        if (string.Equals(zoneKey, ZoneKey.field.ToString(), StringComparison.Ordinal))
        {
            zoneId = actorPlayerState.fieldZoneId;
            return true;
        }

        if (string.Equals(zoneKey, ZoneKey.characterSetAside.ToString(), StringComparison.Ordinal))
        {
            zoneId = actorPlayerState.characterSetAsideZoneId;
            return true;
        }

        zoneId = default;
        return false;
    }
}
