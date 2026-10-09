using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldZoneLayout : MonoBehaviour
{
    [SerializeField]
    private BattlefieldZoneAnchor? anchor;

    [SerializeField]
    private CardAnimationSettings animationSettings = new();

    private readonly BattlefieldCardRegistry localRegistry = new();
    private readonly Dictionary<string, WorldCardView> viewByVisualKey = new();
    private readonly Dictionary<string, WorldCardView> exitingViewByVisualKey = new();
    private readonly List<WorldCardView> orderedViews = new();

    public BattlefieldZoneAnchor? Anchor => anchor;
    public IReadOnlyList<WorldCardView> OrderedViews => orderedViews;

    public void Configure(BattlefieldZoneAnchor newAnchor)
    {
        anchor = newAnchor;
    }

    public void Reconcile(IReadOnlyList<WorldCardViewModel> models)
    {
        var localContextKey = string.IsNullOrWhiteSpace(anchor?.ZoneViewKey)
            ? gameObject.name
            : anchor!.ZoneViewKey;
        Reconcile(models, localRegistry, localContextKey, null);
    }

    public void Reconcile(
        IReadOnlyList<WorldCardViewModel> models,
        BattlefieldCardRegistry registry,
        string visualContextKey,
        IReadOnlyDictionary<string, BattlefieldZoneLayout>? layoutByZoneKey)
    {
        var activeAnchor = anchor;
        if (activeAnchor is null)
        {
            return;
        }

        var settings = activeAnimationSettings();
        removeDestroyedViewReferences();
        removeUntrackedChildViews();

        var zoneViewKey = activeAnchor.ZoneViewKey;
        var incomingKeys = new HashSet<string>(models.Select(model => buildVisualKey(visualContextKey, model)));
        foreach (var existingKey in viewByVisualKey.Keys.ToArray())
        {
            if (incomingKeys.Contains(existingKey))
            {
                continue;
            }

            var exitingView = viewByVisualKey[existingKey];
            viewByVisualKey.Remove(existingKey);
            if (registry.TryGet(existingKey, out var currentEntry) &&
                currentEntry.View == exitingView &&
                string.Equals(currentEntry.ZoneViewKey, zoneViewKey, StringComparison.Ordinal))
            {
                exitingViewByVisualKey[existingKey] = exitingView;
                registry.MarkExiting(existingKey, exitingView, zoneViewKey);
                var exitingPose = BattlefieldZoneMotionAnchorMap.CreateSinkPose(
                    zoneViewKey,
                    this,
                    new CardPose(exitingView.transform.localPosition, exitingView.transform.localRotation, exitingView.transform.localScale),
                    layoutByZoneKey);
                var retainedKey = existingKey;
                exitingView.PlayExitTo(
                    exitingPose,
                    () =>
                    {
                        if (!exitingViewByVisualKey.TryGetValue(retainedKey, out var retainedView) ||
                            retainedView != exitingView ||
                            !registry.IsExitingSameView(retainedKey, exitingView))
                        {
                            return;
                        }

                        exitingViewByVisualKey.Remove(retainedKey);
                        registry.Forget(retainedKey, exitingView);
                        if (exitingView != null)
                        {
                            destroy(exitingView.gameObject);
                        }
                    },
                    settings);
            }
        }

        orderedViews.Clear();
        for (var index = 0; index < models.Count; index += 1)
        {
            var model = models[index];
            var visualKey = buildVisualKey(visualContextKey, model);
            var targetPose = createTargetPose(activeAnchor, index, models.Count);
            var zoneMotion = resolveViewForModel(
                visualKey,
                model,
                targetPose,
                zoneViewKey,
                registry,
                layoutByZoneKey,
                out var view);

            view.Bind(model);
            view.SetVisualContext(visualKey, zoneViewKey);
            orderedViews.Add(view);
            viewByVisualKey[visualKey] = view;
            animateOrSnapView(
                visualKey,
                view,
                model,
                targetPose,
                zoneViewKey,
                zoneMotion,
                registry,
                settings,
                index);
        }
    }

    private ZoneMotionKind resolveViewForModel(
        string visualKey,
        WorldCardViewModel model,
        CardPose targetPose,
        string zoneViewKey,
        BattlefieldCardRegistry registry,
        IReadOnlyDictionary<string, BattlefieldZoneLayout>? layoutByZoneKey,
        out WorldCardView view)
    {
        if (viewByVisualKey.TryGetValue(visualKey, out view))
        {
            view.transform.SetParent(transform, true);
            return ZoneMotionKind.sameZone;
        }

        if (exitingViewByVisualKey.TryGetValue(visualKey, out var exitingView))
        {
            exitingViewByVisualKey.Remove(visualKey);
            view = exitingView;
            view.transform.SetParent(transform, true);
            return ZoneMotionKind.sameZone;
        }

        if (registry.TryGet(visualKey, out var registryEntry) && registryEntry.View != null)
        {
            view = registryEntry.View;
            view.transform.SetParent(transform, true);
            return string.Equals(registryEntry.ZoneViewKey, zoneViewKey, StringComparison.Ordinal)
                ? ZoneMotionKind.sameZone
                : ZoneMotionKind.crossZone;
        }

        view = WorldCardViewFactory.Create($"WorldCardView_{model.cardInstanceNumericId}", transform);
        var sourcePose = BattlefieldZoneMotionAnchorMap.CreateSourcePose(zoneViewKey, this, targetPose, layoutByZoneKey);
        view.SnapTo(sourcePose);
        return ZoneMotionKind.enterFromSource;
    }

    private void animateOrSnapView(
        string visualKey,
        WorldCardView view,
        WorldCardViewModel model,
        CardPose targetPose,
        string zoneViewKey,
        ZoneMotionKind zoneMotion,
        BattlefieldCardRegistry registry,
        CardAnimationSettings settings,
        int index)
    {
        if (!Application.isPlaying)
        {
            view.SnapTo(targetPose);
            registry.Track(visualKey, view, zoneViewKey, targetPose, model);
            return;
        }

        var delay = index * settings.staggerInterval;
        if (registry.TryGet(visualKey, out var previousEntry) &&
            previousEntry.View == view &&
            !previousEntry.IsExiting &&
            poseAlmostEqual(previousEntry.LastTargetPose, targetPose, settings))
        {
            registry.Track(visualKey, view, zoneViewKey, targetPose, model);
            return;
        }

        var currentLocalPosition = view.transform.localPosition;
        var distance = Vector3.Distance(currentLocalPosition, targetPose.Position);
        if (distance <= settings.snapDistanceEpsilon)
        {
            view.SnapTo(targetPose);
            registry.Track(visualKey, view, zoneViewKey, targetPose, model);
            return;
        }

        switch (zoneMotion)
        {
            case ZoneMotionKind.crossZone:
                view.AnimateTo(
                    targetPose,
                    settings.crossZoneFlightDuration,
                    settings,
                    delay,
                    settings.arcHeightLarge,
                    settings.flightScalePeak);
                break;
            case ZoneMotionKind.enterFromSource:
                view.PlayEnterFromSource(
                    targetPose,
                    new CardPose(view.transform.localPosition, view.transform.localRotation, view.transform.localScale),
                    settings,
                    delay,
                    settings.enterFromSourceDuration);
                break;
            default:
                view.AnimateTo(
                    targetPose,
                    settings.sameZoneMoveDuration,
                    settings,
                    delay,
                    settings.arcHeightSmall,
                    1f);
                break;
        }

        registry.Track(visualKey, view, zoneViewKey, targetPose, model);
    }

    private void removeDestroyedViewReferences()
    {
        foreach (var entry in viewByVisualKey.Where(entry => entry.Value == null).ToArray())
        {
            viewByVisualKey.Remove(entry.Key);
        }

        foreach (var entry in exitingViewByVisualKey.Where(entry => entry.Value == null).ToArray())
        {
            exitingViewByVisualKey.Remove(entry.Key);
        }
    }

    private void removeUntrackedChildViews()
    {
        var trackedViews = new HashSet<WorldCardView>(viewByVisualKey.Values);
        foreach (var exitingView in exitingViewByVisualKey.Values)
        {
            trackedViews.Add(exitingView);
        }

        foreach (var childView in GetComponentsInChildren<WorldCardView>(true)
            .Where(view => view.transform.parent == transform)
            .ToArray())
        {
            if (trackedViews.Contains(childView))
            {
                continue;
            }

            destroy(childView.gameObject);
        }
    }

    public void ApplyLayout()
    {
        var activeAnchor = anchor;
        if (activeAnchor is null)
        {
            return;
        }

        var settings = activeAnimationSettings();
        for (var index = 0; index < orderedViews.Count; index += 1)
        {
            var view = orderedViews[index];
            var pose = createTargetPose(activeAnchor, index, orderedViews.Count);
            if (!Application.isPlaying)
            {
                view.SnapTo(pose);
                continue;
            }

            view.AnimateTo(pose, settings.sameZoneMoveDuration, settings, 0f, settings.arcHeightSmall, 1f);
        }
    }

    private CardAnimationSettings activeAnimationSettings()
    {
        if (animationSettings is null)
        {
            animationSettings = new CardAnimationSettings();
        }

        return animationSettings;
    }

    private static string buildVisualKey(string visualContextKey, WorldCardViewModel model)
    {
        return BattlefieldCardRegistry.BuildVisualKey(visualContextKey, model.cardInstanceNumericId);
    }

    private static CardPose createTargetPose(BattlefieldZoneAnchor activeAnchor, int index, int count)
    {
        return new CardPose(
            getLocalPosition(activeAnchor, index, count),
            Quaternion.Euler(activeAnchor.LocalRotation),
            activeAnchor.CardScale);
    }

    private static bool poseAlmostEqual(CardPose left, CardPose right, CardAnimationSettings settings)
    {
        var maxDistance = settings.snapDistanceEpsilon;
        return Vector3.SqrMagnitude(left.Position - right.Position) <= maxDistance * maxDistance &&
            Quaternion.Angle(left.Rotation, right.Rotation) <= settings.rotationEpsilonDegrees &&
            Vector3.SqrMagnitude(left.Scale - right.Scale) <= maxDistance * maxDistance;
    }

    private static Vector3 getLocalPosition(
        BattlefieldZoneAnchor activeAnchor,
        int index,
        int count)
    {
        return activeAnchor.LayoutType switch
        {
            BattlefieldZoneLayoutType.stack => new Vector3(
                index * activeAnchor.CardSpacing.x,
                index * 0.012f,
                index * activeAnchor.CardSpacing.y),
            BattlefieldZoneLayoutType.horizontalRow => new Vector3(
                (index - (count - 1) * 0.5f) * activeAnchor.CardSpacing.x,
                0f,
                0f),
            BattlefieldZoneLayoutType.verticalColumn => new Vector3(
                0f,
                0f,
                (index - (count - 1) * 0.5f) * activeAnchor.CardSpacing.y),
            BattlefieldZoneLayoutType.grid => getGridPosition(activeAnchor, index, count),
            _ => Vector3.zero,
        };
    }

    private static Vector3 getGridPosition(
        BattlefieldZoneAnchor activeAnchor,
        int index,
        int count)
    {
        var columns = Mathf.Min(5, Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(count, 1))));
        var rows = Mathf.CeilToInt(count / (float)columns);
        var column = index % columns;
        var row = index / columns;
        return new Vector3(
            (column - (columns - 1) * 0.5f) * activeAnchor.CardSpacing.x,
            0f,
            (row - (rows - 1) * 0.5f) * activeAnchor.CardSpacing.y);
    }

    private static void destroy(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private enum ZoneMotionKind
    {
        sameZone,
        crossZone,
        enterFromSource,
    }
}
}
