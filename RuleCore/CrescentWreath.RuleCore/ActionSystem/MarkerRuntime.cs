using System;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class MarkerRuntime
{
    public static bool canAddMarker(CharacterInstance characterInstance, string markerTypeKey)
    {
        if (string.IsNullOrWhiteSpace(markerTypeKey))
        {
            return false;
        }

        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        if (!characterDefinition.allowedMarkerTypes.Contains(markerTypeKey, StringComparer.Ordinal))
        {
            return false;
        }

        var cap = getMarkerCap(characterDefinition, markerTypeKey);
        return getMarkerCount(characterInstance, markerTypeKey) < cap;
    }

    public static int getMarkerCount(CharacterInstance characterInstance, string markerTypeKey)
    {
        return characterInstance.markerState.markerMap.TryGetValue(markerTypeKey, out var count)
            ? Math.Max(0, count)
            : 0;
    }

    public static int getMarkerCap(CharacterInstance characterInstance, string markerTypeKey)
    {
        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        return getMarkerCap(characterDefinition, markerTypeKey);
    }

    public static MarkerChangedEvent addMarker(
        CharacterInstance characterInstance,
        string markerTypeKey,
        int delta,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        if (delta <= 0)
        {
            throw new InvalidOperationException("AddMarker requires positive delta.");
        }

        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        if (!characterDefinition.allowedMarkerTypes.Contains(markerTypeKey, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("AddMarker requires markerTypeKey to be allowed by character definition.");
        }

        var beforeCount = getMarkerCount(characterInstance, markerTypeKey);
        var cap = getMarkerCap(characterDefinition, markerTypeKey);
        var afterCount = Math.Min(cap, beforeCount + delta);
        characterInstance.markerState.markerMap[markerTypeKey] = afterCount;

        return new MarkerChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "markerChanged",
            sourceActionChainId = sourceActionChainId,
            targetCharacterInstanceId = characterInstance.characterInstanceId,
            targetPlayerId = characterInstance.ownerPlayerId,
            markerTypeKey = markerTypeKey,
            beforeCount = beforeCount,
            afterCount = afterCount,
            delta = afterCount - beforeCount,
        };
    }

    public static MarkerChangedEvent removeMarker(
        CharacterInstance characterInstance,
        string markerTypeKey,
        int delta,
        ActionChainId sourceActionChainId,
        long eventId)
    {
        if (delta <= 0)
        {
            throw new InvalidOperationException("RemoveMarker requires positive delta.");
        }

        var beforeCount = getMarkerCount(characterInstance, markerTypeKey);
        var afterCount = Math.Max(0, beforeCount - delta);
        if (afterCount == 0)
        {
            characterInstance.markerState.markerMap.Remove(markerTypeKey);
        }
        else
        {
            characterInstance.markerState.markerMap[markerTypeKey] = afterCount;
        }

        return new MarkerChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "markerChanged",
            sourceActionChainId = sourceActionChainId,
            targetCharacterInstanceId = characterInstance.characterInstanceId,
            targetPlayerId = characterInstance.ownerPlayerId,
            markerTypeKey = markerTypeKey,
            beforeCount = beforeCount,
            afterCount = afterCount,
            delta = afterCount - beforeCount,
        };
    }

    private static int getMarkerCap(CharacterDefinition characterDefinition, string markerTypeKey)
    {
        if (characterDefinition.markerCaps.TryGetValue(markerTypeKey, out var cap) && cap > 0)
        {
            return cap;
        }

        return 999;
    }
}
