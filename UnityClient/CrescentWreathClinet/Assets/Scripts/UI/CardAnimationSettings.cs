using System;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[Serializable]
public sealed class CardAnimationSettings
{
    [Min(0f)]
    public float moveDuration = 0.22f;

    [Min(0f)]
    public float enterDuration = 0.18f;

    [Min(0f)]
    public float exitDuration = 0.16f;

    [Min(0f)]
    public float moveHeightArc = 0.04f;

    [Min(0f)]
    public float enterHeightOffset = 0.28f;

    [Min(0f)]
    public float exitHeightOffset = 0.18f;

    [Min(0f)]
    public float staggerInterval = 0.015f;

    [Min(0f)]
    public float sameZoneMoveDuration = 0.16f;

    [Min(0f)]
    public float crossZoneFlightDuration = 0.36f;

    [Min(0f)]
    public float enterFromSourceDuration = 0.32f;

    [Min(0f)]
    public float exitToSinkDuration = 0.26f;

    [Min(0f)]
    public float arcHeightSmall = 0.04f;

    [Min(0f)]
    public float arcHeightLarge = 0.18f;

    [Min(1f)]
    public float flightScalePeak = 1.06f;

    [Min(0f)]
    public float settleDuration = 0.06f;

    [Min(0f)]
    public float snapDistanceEpsilon = 0.01f;

    [Min(0f)]
    public float rotationEpsilonDegrees = 1f;

    public bool useArcMotion = true;

    public static CardAnimationSettings Default { get; } = new();
}
}
