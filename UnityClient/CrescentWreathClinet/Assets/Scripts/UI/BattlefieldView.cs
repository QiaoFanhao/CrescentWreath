using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldView : MonoBehaviour
{
    [SerializeField]
    private BattlefieldTableWorld? tableWorld;

    private readonly Dictionary<string, BattlefieldZoneLayout> layoutByZoneKey = new();
    private readonly BattlefieldCardRegistry cardRegistry = new();

    public IReadOnlyDictionary<string, BattlefieldZoneLayout> LayoutByZoneKey => layoutByZoneKey;
    public event Action<WorldCardView>? CardClicked;
    public event Action<WorldCardView>? CardDoubleClicked;

    public void Configure(BattlefieldTableWorld newTableWorld)
    {
        tableWorld = newTableWorld;
        RebuildLayouts();
    }

    public void RebuildLayouts()
    {
        layoutByZoneKey.Clear();
        if (tableWorld is null)
        {
            return;
        }

        foreach (var anchor in tableWorld.GetComponentsInChildren<BattlefieldZoneAnchor>(true))
        {
            if (string.IsNullOrWhiteSpace(anchor.ZoneViewKey))
            {
                continue;
            }

            var layout = getOrCreateSingleLayout(anchor);
            layout.Configure(anchor);
            layoutByZoneKey[anchor.ZoneViewKey] = layout;
        }
    }

    public void ReconcileZone(string zoneViewKey, IReadOnlyList<WorldCardViewModel> models)
    {
        if (!layoutByZoneKey.TryGetValue(zoneViewKey, out var layout))
        {
            return;
        }

        layout.Reconcile(models, cardRegistry, resolveVisualContextKey(zoneViewKey), layoutByZoneKey);
        foreach (var view in layout.OrderedViews)
        {
            view.Clicked -= handleCardClicked;
            view.Clicked += handleCardClicked;
            view.DoubleClicked -= handleCardDoubleClicked;
            view.DoubleClicked += handleCardDoubleClicked;
        }
    }

    public void ClearZone(string zoneViewKey)
    {
        ReconcileZone(zoneViewKey, Array.Empty<WorldCardViewModel>());
    }

    public void ClearAll()
    {
        foreach (var key in layoutByZoneKey.Keys.ToArray())
        {
            ClearZone(key);
        }
    }

    public bool TryFindFirstCardByDefinitionId(string definitionId, out WorldCardView? cardView)
    {
        cardView = null;
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return false;
        }

        foreach (var layout in layoutByZoneKey.Values)
        {
            foreach (var view in layout.OrderedViews)
            {
                if (view == null ||
                    !string.Equals(view.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                cardView = view;
                return true;
            }
        }

        return false;
    }

    public bool TryFindCardByInstanceId(long cardInstanceNumericId, out WorldCardView? cardView)
    {
        cardView = null;
        if (cardInstanceNumericId <= 0)
        {
            return false;
        }

        foreach (var layout in layoutByZoneKey.Values)
        {
            foreach (var view in layout.OrderedViews)
            {
                if (view == null || view.CardInstanceNumericId != cardInstanceNumericId)
                {
                    continue;
                }

                cardView = view;
                return true;
            }
        }

        return false;
    }

    public bool TryFindEffectSourceCardByInstanceId(
        long cardInstanceNumericId,
        string preferredZoneViewKey,
        out WorldCardView? cardView)
    {
        cardView = null;
        if (cardInstanceNumericId <= 0)
        {
            return false;
        }

        var candidates = new List<(WorldCardView View, string ZoneViewKey, int Score)>();
        foreach (var layoutEntry in layoutByZoneKey)
        {
            var zoneViewKey = layoutEntry.Key;
            foreach (var view in layoutEntry.Value.OrderedViews)
            {
                if (view == null || view.CardInstanceNumericId != cardInstanceNumericId)
                {
                    continue;
                }

                candidates.Add((view, zoneViewKey, scoreEffectSourceZone(zoneViewKey, preferredZoneViewKey)));
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        cardView = candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.ZoneViewKey, StringComparer.Ordinal)
            .First()
            .View;
        return cardView != null;
    }

    private void handleCardClicked(WorldCardView view)
    {
        CardClicked?.Invoke(view);
    }

    private void handleCardDoubleClicked(WorldCardView view)
    {
        CardDoubleClicked?.Invoke(view);
    }

    private static BattlefieldZoneLayout getOrCreateSingleLayout(BattlefieldZoneAnchor anchor)
    {
        var existingLayouts = anchor.GetComponentsInChildren<BattlefieldZoneLayout>(true)
            .Where(layout => layout.transform.parent == anchor.transform)
            .ToArray();
        BattlefieldZoneLayout? retainedLayout = null;

        foreach (var layout in existingLayouts)
        {
            if (retainedLayout is null)
            {
                retainedLayout = layout;
                retainedLayout.name = "WorldCardLayout";
                continue;
            }

            destroy(layout.gameObject);
        }

        if (retainedLayout is not null)
        {
            return retainedLayout;
        }

        var layoutObject = new GameObject("WorldCardLayout");
        layoutObject.transform.SetParent(anchor.transform, false);
        return layoutObject.AddComponent<BattlefieldZoneLayout>();
    }

    private static string resolveVisualContextKey(string zoneViewKey)
    {
        return string.Equals(zoneViewKey, "currentPlayerMirror", StringComparison.Ordinal)
            ? "mirror-current"
            : "table";
    }

    private static int scoreEffectSourceZone(string zoneViewKey, string preferredZoneViewKey)
    {
        if (!string.IsNullOrWhiteSpace(preferredZoneViewKey) &&
            string.Equals(zoneViewKey, preferredZoneViewKey, StringComparison.Ordinal))
        {
            return 100;
        }

        if (zoneViewKey.StartsWith("playerField:", StringComparison.Ordinal))
        {
            return 80;
        }

        if (zoneViewKey.StartsWith("summonZone:", StringComparison.Ordinal))
        {
            return 60;
        }

        if (string.Equals(zoneViewKey, "currentPlayerMirror", StringComparison.Ordinal))
        {
            return -100;
        }

        if (zoneViewKey.StartsWith("playerDiscard:", StringComparison.Ordinal) ||
            zoneViewKey.StartsWith("playerDeck:", StringComparison.Ordinal) ||
            string.Equals(zoneViewKey, "localHand", StringComparison.Ordinal))
        {
            return 10;
        }

        return 20;
    }

    private static void destroy(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
}
