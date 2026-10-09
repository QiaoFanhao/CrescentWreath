using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class BattlefieldZoneMotionAnchorMap
{
    public static CardPose CreateSourcePose(
        string targetZoneViewKey,
        BattlefieldZoneLayout targetLayout,
        CardPose targetPose,
        IReadOnlyDictionary<string, BattlefieldZoneLayout>? layoutByZoneKey)
    {
        if (targetZoneViewKey.StartsWith("playerField:", StringComparison.OrdinalIgnoreCase) &&
            tryCreateHandSourcePose(targetZoneViewKey, targetPose, out var handSourcePose))
        {
            return handSourcePose;
        }

        if (tryResolveSourceZoneKey(targetZoneViewKey, out var sourceZoneKey) &&
            tryCreatePoseFromZone(sourceZoneKey, targetLayout, targetPose, layoutByZoneKey, out var sourcePose))
        {
            return sourcePose;
        }

        return new CardPose(
            targetPose.Position + Vector3.up * 0.28f,
            targetPose.Rotation,
            targetPose.Scale * 0.88f);
    }

    public static CardPose CreateSinkPose(
        string previousZoneViewKey,
        BattlefieldZoneLayout previousLayout,
        CardPose startPose,
        IReadOnlyDictionary<string, BattlefieldZoneLayout>? layoutByZoneKey)
    {
        if (tryResolveSinkZoneKey(previousZoneViewKey, out var sinkZoneKey) &&
            tryCreatePoseFromZone(sinkZoneKey, previousLayout, startPose, layoutByZoneKey, out var sinkPose))
        {
            return sinkPose;
        }

        return new CardPose(
            startPose.Position + Vector3.up * 0.18f,
            startPose.Rotation,
            startPose.Scale * 0.82f);
    }

    private static bool tryResolveSourceZoneKey(string targetZoneViewKey, out string sourceZoneKey)
    {
        sourceZoneKey = string.Empty;
        if (isSummonZoneViewKey(targetZoneViewKey))
        {
            sourceZoneKey = "publicTreasureDeck";
            return true;
        }

        if (string.Equals(targetZoneViewKey, "currentAnomaly", StringComparison.OrdinalIgnoreCase))
        {
            sourceZoneKey = "anomalyDeck";
            return true;
        }

        if (string.Equals(targetZoneViewKey, "sakuraCakeZone", StringComparison.OrdinalIgnoreCase))
        {
            sourceZoneKey = "sakuraCakeZone";
            return true;
        }

        return false;
    }

    private static bool tryResolveSinkZoneKey(string previousZoneViewKey, out string sinkZoneKey)
    {
        sinkZoneKey = string.Empty;
        if (isSummonZoneViewKey(previousZoneViewKey))
        {
            sinkZoneKey = "publicTreasureDeck";
            return true;
        }

        if (string.Equals(previousZoneViewKey, "currentAnomaly", StringComparison.OrdinalIgnoreCase))
        {
            sinkZoneKey = "resolvedAnomaly";
            return true;
        }

        if (previousZoneViewKey.StartsWith("playerField:", StringComparison.OrdinalIgnoreCase) &&
            tryGetSeatKey(previousZoneViewKey, out var seatKey))
        {
            sinkZoneKey = $"playerDiscard:{seatKey}";
            return true;
        }

        return false;
    }

    private static bool isSummonZoneViewKey(string zoneViewKey)
    {
        return string.Equals(zoneViewKey, "summonZone", StringComparison.OrdinalIgnoreCase) ||
               zoneViewKey.StartsWith("summonZone:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool tryCreateHandSourcePose(
        string targetZoneViewKey,
        CardPose targetPose,
        out CardPose pose)
    {
        pose = targetPose;
        if (!tryGetSeatKey(targetZoneViewKey, out _))
        {
            return false;
        }

        // Player field anchors are rotated per seat; local -Z points toward that
        // player's table edge, which is the best non-UI proxy for their hand.
        var sourcePosition = new Vector3(
            targetPose.Position.x * 0.55f,
            targetPose.Position.y + 0.08f,
            -1.85f);
        pose = new CardPose(
            sourcePosition,
            targetPose.Rotation,
            targetPose.Scale * 0.92f);
        return true;
    }

    private static bool tryCreatePoseFromZone(
        string sourceZoneKey,
        BattlefieldZoneLayout targetLayout,
        CardPose targetPose,
        IReadOnlyDictionary<string, BattlefieldZoneLayout>? layoutByZoneKey,
        out CardPose pose)
    {
        pose = targetPose;
        if (layoutByZoneKey is null ||
            !layoutByZoneKey.TryGetValue(sourceZoneKey, out var sourceLayout) ||
            sourceLayout == null)
        {
            return false;
        }

        var sourceAnchor = sourceLayout.Anchor;
        var sourceWorldPosition = sourceAnchor is null
            ? sourceLayout.transform.position
            : sourceAnchor.transform.TransformPoint(Vector3.zero);
        var localPosition = targetLayout.transform.InverseTransformPoint(sourceWorldPosition);
        var sourceRotation = sourceAnchor is null
            ? targetPose.Rotation
            : Quaternion.Euler(sourceAnchor.LocalRotation);
        pose = new CardPose(
            localPosition + Vector3.up * 0.06f,
            sourceRotation,
            targetPose.Scale * 0.88f);
        return true;
    }

    private static bool tryGetSeatKey(string zoneViewKey, out string seatKey)
    {
        seatKey = string.Empty;
        var separatorIndex = zoneViewKey.IndexOf(':');
        if (separatorIndex < 0 || separatorIndex + 1 >= zoneViewKey.Length)
        {
            return false;
        }

        seatKey = zoneViewKey[(separatorIndex + 1)..];
        return !string.IsNullOrWhiteSpace(seatKey);
    }
}
}
