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

        foreach (var view in layout.OrderedViews)
        {
            view.Clicked -= handleCardClicked;
            view.DoubleClicked -= handleCardDoubleClicked;
        }

        layout.Reconcile(models);
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
