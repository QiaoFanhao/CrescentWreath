using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldCardRegistry
{
    private readonly Dictionary<string, BattlefieldCardRegistryEntry> entriesByVisualKey = new();

    public static string BuildVisualKey(string visualContextKey, long cardInstanceNumericId)
    {
        return $"{visualContextKey}:{cardInstanceNumericId}";
    }

    public bool TryGet(string visualKey, out BattlefieldCardRegistryEntry entry)
    {
        return entriesByVisualKey.TryGetValue(visualKey, out entry);
    }

    public BattlefieldCardRegistryEntry Track(
        string visualKey,
        WorldCardView view,
        string zoneViewKey,
        CardPose targetPose,
        WorldCardViewModel viewModel)
    {
        var entry = new BattlefieldCardRegistryEntry(
            visualKey,
            view,
            zoneViewKey,
            targetPose,
            viewModel.Clone(),
            isExiting: false);
        entriesByVisualKey[visualKey] = entry;
        return entry;
    }

    public void MarkExiting(string visualKey, WorldCardView view, string previousZoneViewKey)
    {
        if (!entriesByVisualKey.TryGetValue(visualKey, out var existing) || existing.View != view)
        {
            entriesByVisualKey[visualKey] = new BattlefieldCardRegistryEntry(
                visualKey,
                view,
                previousZoneViewKey,
                new CardPose(view.transform.localPosition, view.transform.localRotation, view.transform.localScale),
                view.CurrentModel,
                isExiting: true);
            return;
        }

        existing.ZoneViewKey = previousZoneViewKey;
        existing.IsExiting = true;
    }

    public bool IsExitingSameView(string visualKey, WorldCardView view)
    {
        return entriesByVisualKey.TryGetValue(visualKey, out var existing) &&
            existing.View == view &&
            existing.IsExiting;
    }

    public void Forget(string visualKey, WorldCardView view)
    {
        if (!entriesByVisualKey.TryGetValue(visualKey, out var existing) || existing.View != view)
        {
            return;
        }

        entriesByVisualKey.Remove(visualKey);
    }
}

public sealed class BattlefieldCardRegistryEntry
{
    public BattlefieldCardRegistryEntry(
        string visualKey,
        WorldCardView view,
        string zoneViewKey,
        CardPose lastTargetPose,
        WorldCardViewModel lastViewModel,
        bool isExiting)
    {
        VisualKey = visualKey;
        View = view;
        ZoneViewKey = zoneViewKey;
        LastTargetPose = lastTargetPose;
        LastViewModel = lastViewModel;
        IsExiting = isExiting;
    }

    public string VisualKey { get; }
    public WorldCardView View { get; }
    public string ZoneViewKey { get; set; }
    public CardPose LastTargetPose { get; }
    public WorldCardViewModel LastViewModel { get; }
    public bool IsExiting { get; set; }
}
}
