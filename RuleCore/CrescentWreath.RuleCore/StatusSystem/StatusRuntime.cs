using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.StatusSystem;

public static class StatusRuntime
{
    public const string DurationTypeKeyNextDamageAttempt = "nextDamageAttempt";
    private static readonly HashSet<string> ShortStatusKeysClearedAtTurnEnd = new(StringComparer.Ordinal)
    {
        "Silence",
        "Charm",
        "Penetrate",
    };

    public static StatusInstance applyStatus(
        GameState.GameState gameState,
        StatusInstance statusInstance)
    {
        if (string.IsNullOrWhiteSpace(statusInstance.statusKey))
        {
            throw new InvalidOperationException("StatusRuntime.applyStatus requires statusInstance.statusKey.");
        }

        if (!statusInstance.targetCharacterInstanceId.HasValue &&
            !statusInstance.targetCardInstanceId.HasValue &&
            !statusInstance.targetPlayerId.HasValue)
        {
            throw new InvalidOperationException("StatusRuntime.applyStatus requires at least one target: targetCharacterInstanceId, targetCardInstanceId, or targetPlayerId.");
        }

        var materializedStatusInstance = cloneStatusInstance(statusInstance);
        materializedStatusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(materializedStatusInstance.statusKey);

        var statusPolicy = StatusPolicyTable.resolvePolicy(materializedStatusInstance.statusKey, materializedStatusInstance);
        ensureRequiredTargetForPolicy(materializedStatusInstance, statusPolicy);

        if (statusPolicy.stackPolicy == StatusPolicyTable.StatusStackPolicy.stack)
        {
            normalizeStackCountForNonStackingPolicies(materializedStatusInstance, statusPolicy);
            gameState.statusInstances.Add(materializedStatusInstance);
            return materializedStatusInstance;
        }

        var existingStatusInstance = findExistingStatusByPolicy(gameState, materializedStatusInstance, statusPolicy);
        if (existingStatusInstance is null)
        {
            normalizeStackCountForNonStackingPolicies(materializedStatusInstance, statusPolicy);
            gameState.statusInstances.Add(materializedStatusInstance);
            return materializedStatusInstance;
        }

        if (statusPolicy.stackPolicy == StatusPolicyTable.StatusStackPolicy.ignoreIfExists)
        {
            return existingStatusInstance;
        }

        refreshExistingStatus(existingStatusInstance, materializedStatusInstance);
        return existingStatusInstance;
    }

    public static List<StatusInstance> queryStatusesForCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        return gameState.statusInstances
            .Where(statusInstance => statusInstance.targetCharacterInstanceId == targetCharacterInstanceId)
            .ToList();
    }

    public static int removeStatusOnCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        string statusKey)
    {
        return removeStatusesOnCharacter(gameState, targetCharacterInstanceId, statusKey).Count;
    }

    public static List<StatusInstance> removeStatusesOnCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        string statusKey)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
        {
            throw new InvalidOperationException("StatusRuntime.removeStatusOnCharacter requires statusKey.");
        }

        var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(statusKey);
        var removedStatuses = new List<StatusInstance>();
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            statusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey);
            if (statusInstance.targetCharacterInstanceId != targetCharacterInstanceId)
            {
                continue;
            }

            if (!string.Equals(
                    statusInstance.statusKey,
                    normalizedStatusKey,
                    StringComparison.Ordinal))
            {
                continue;
            }

            removedStatuses.Add(cloneStatusInstance(statusInstance));
            gameState.statusInstances.RemoveAt(index);
        }

        removedStatuses.Reverse();
        return removedStatuses;
    }

    public static bool hasStatusOnCharacter(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        string statusKey)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
        {
            throw new InvalidOperationException("StatusRuntime.hasStatusOnCharacter requires statusKey.");
        }

        var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(statusKey);
        foreach (var statusInstance in gameState.statusInstances)
        {
            statusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey);
            if (statusInstance.targetCharacterInstanceId != targetCharacterInstanceId)
            {
                continue;
            }

            if (string.Equals(statusInstance.statusKey, normalizedStatusKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool hasStatusOnPlayer(
        GameState.GameState gameState,
        PlayerId targetPlayerId,
        string statusKey)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
        {
            throw new InvalidOperationException("StatusRuntime.hasStatusOnPlayer requires statusKey.");
        }

        var normalizedStatusKey = StatusPolicyTable.normalizeStatusKey(statusKey);
        foreach (var statusInstance in gameState.statusInstances)
        {
            statusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey);
            if (statusInstance.targetPlayerId != targetPlayerId)
            {
                continue;
            }

            if (string.Equals(statusInstance.statusKey, normalizedStatusKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static List<string> consumeNextDamageEffectsOnAttempt(
        GameState.GameState gameState,
        PlayerId sourcePlayerId)
    {
        var consumedStatusKeys = new List<string>();
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            statusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey);
            if (statusInstance.targetPlayerId != sourcePlayerId)
            {
                continue;
            }

            if (!string.Equals(
                    statusInstance.durationTypeKey,
                    DurationTypeKeyNextDamageAttempt,
                    StringComparison.Ordinal))
            {
                continue;
            }

            consumedStatusKeys.Add(statusInstance.statusKey);
            if (statusInstance.stackCount > 1)
            {
                statusInstance.stackCount -= 1;
                continue;
            }

            gameState.statusInstances.RemoveAt(index);
        }

        consumedStatusKeys.Reverse();
        return consumedStatusKeys;
    }

    public static List<StatusInstance> clearShortStatusesAtTurnEnd(
        GameState.GameState gameState)
    {
        var removedStatuses = new List<StatusInstance>();
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            statusInstance.statusKey = StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey);
            if (!ShortStatusKeysClearedAtTurnEnd.Contains(statusInstance.statusKey))
            {
                continue;
            }

            removedStatuses.Add(cloneStatusInstance(statusInstance));
            gameState.statusInstances.RemoveAt(index);
        }

        removedStatuses.Reverse();
        return removedStatuses;
    }

    private static StatusInstance cloneStatusInstance(StatusInstance sourceStatusInstance)
    {
        var clonedStatusInstance = new StatusInstance
        {
            statusKey = sourceStatusInstance.statusKey,
            applierPlayerId = sourceStatusInstance.applierPlayerId,
            applierCharacterInstanceId = sourceStatusInstance.applierCharacterInstanceId,
            applierCardInstanceId = sourceStatusInstance.applierCardInstanceId,
            targetCardInstanceId = sourceStatusInstance.targetCardInstanceId,
            targetCharacterInstanceId = sourceStatusInstance.targetCharacterInstanceId,
            targetPlayerId = sourceStatusInstance.targetPlayerId,
            stackCount = sourceStatusInstance.stackCount,
            durationTypeKey = sourceStatusInstance.durationTypeKey,
            remainingDuration = sourceStatusInstance.remainingDuration,
        };

        foreach (var parameter in sourceStatusInstance.parameters)
        {
            clonedStatusInstance.parameters[parameter.Key] = parameter.Value;
        }

        return clonedStatusInstance;
    }

    private static void normalizeStackCountForNonStackingPolicies(
        StatusInstance statusInstance,
        StatusPolicyTable.StatusPolicy statusPolicy)
    {
        if (statusPolicy.stackPolicy == StatusPolicyTable.StatusStackPolicy.stack)
        {
            if (statusInstance.stackCount <= 0)
            {
                statusInstance.stackCount = 1;
            }

            return;
        }

        statusInstance.stackCount = 1;
    }

    private static void ensureRequiredTargetForPolicy(
        StatusInstance statusInstance,
        StatusPolicyTable.StatusPolicy statusPolicy)
    {
        if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.character &&
            !statusInstance.targetCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException(
                $"StatusRuntime.applyStatus requires targetCharacterInstanceId for statusKey={statusPolicy.canonicalStatusKey}.");
        }

        if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.player &&
            !statusInstance.targetPlayerId.HasValue)
        {
            throw new InvalidOperationException(
                $"StatusRuntime.applyStatus requires targetPlayerId for statusKey={statusPolicy.canonicalStatusKey}.");
        }

        if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.card &&
            !statusInstance.targetCardInstanceId.HasValue)
        {
            throw new InvalidOperationException(
                $"StatusRuntime.applyStatus requires targetCardInstanceId for statusKey={statusPolicy.canonicalStatusKey}.");
        }
    }

    private static StatusInstance? findExistingStatusByPolicy(
        GameState.GameState gameState,
        StatusInstance incomingStatusInstance,
        StatusPolicyTable.StatusPolicy statusPolicy)
    {
        foreach (var existingStatusInstance in gameState.statusInstances)
        {
            var normalizedExistingStatusKey = StatusPolicyTable.normalizeStatusKey(existingStatusInstance.statusKey);
            existingStatusInstance.statusKey = normalizedExistingStatusKey;
            if (!string.Equals(
                    normalizedExistingStatusKey,
                    statusPolicy.canonicalStatusKey,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.character &&
                existingStatusInstance.targetCharacterInstanceId == incomingStatusInstance.targetCharacterInstanceId)
            {
                return existingStatusInstance;
            }

            if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.player &&
                existingStatusInstance.targetPlayerId == incomingStatusInstance.targetPlayerId)
            {
                return existingStatusInstance;
            }

            if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.card &&
                existingStatusInstance.targetCardInstanceId == incomingStatusInstance.targetCardInstanceId)
            {
                return existingStatusInstance;
            }

            if (statusPolicy.identityScope == StatusPolicyTable.StatusIdentityScope.generic &&
                existingStatusInstance.targetCharacterInstanceId == incomingStatusInstance.targetCharacterInstanceId &&
                existingStatusInstance.targetPlayerId == incomingStatusInstance.targetPlayerId &&
                existingStatusInstance.targetCardInstanceId == incomingStatusInstance.targetCardInstanceId)
            {
                return existingStatusInstance;
            }
        }

        return null;
    }

    private static void refreshExistingStatus(
        StatusInstance existingStatusInstance,
        StatusInstance incomingStatusInstance)
    {
        existingStatusInstance.statusKey = incomingStatusInstance.statusKey;
        existingStatusInstance.applierPlayerId = incomingStatusInstance.applierPlayerId;
        existingStatusInstance.applierCharacterInstanceId = incomingStatusInstance.applierCharacterInstanceId;
        existingStatusInstance.applierCardInstanceId = incomingStatusInstance.applierCardInstanceId;
        existingStatusInstance.durationTypeKey = incomingStatusInstance.durationTypeKey;
        existingStatusInstance.remainingDuration = incomingStatusInstance.remainingDuration;
        existingStatusInstance.stackCount = 1;
        existingStatusInstance.parameters.Clear();
        foreach (var parameter in incomingStatusInstance.parameters)
        {
            existingStatusInstance.parameters[parameter.Key] = parameter.Value;
        }
    }
}
