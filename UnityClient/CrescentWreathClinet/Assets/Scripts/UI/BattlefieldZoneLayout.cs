using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldZoneLayout : MonoBehaviour
{
    [SerializeField]
    private BattlefieldZoneAnchor? anchor;

    private readonly Dictionary<long, WorldCardView> viewByCardInstanceId = new();
    private readonly List<WorldCardView> orderedViews = new();

    public BattlefieldZoneAnchor? Anchor => anchor;
    public IReadOnlyList<WorldCardView> OrderedViews => orderedViews;

    public void Configure(BattlefieldZoneAnchor newAnchor)
    {
        anchor = newAnchor;
    }

    public void Reconcile(IReadOnlyList<WorldCardViewModel> models)
    {
        removeDestroyedViewReferences();
        removeUntrackedChildViews();

        var incomingIds = new HashSet<long>(models.Select(model => model.cardInstanceNumericId));
        foreach (var existing in viewByCardInstanceId.Keys.ToArray())
        {
            if (incomingIds.Contains(existing))
            {
                continue;
            }

            destroy(viewByCardInstanceId[existing].gameObject);
            viewByCardInstanceId.Remove(existing);
        }

        orderedViews.Clear();
        for (var index = 0; index < models.Count; index += 1)
        {
            var model = models[index];
            if (!viewByCardInstanceId.TryGetValue(model.cardInstanceNumericId, out var view))
            {
                view = WorldCardViewFactory.Create($"WorldCardView_{model.cardInstanceNumericId}", transform);
                viewByCardInstanceId[model.cardInstanceNumericId] = view;
            }

            view.Bind(model);
            orderedViews.Add(view);
        }

        ApplyLayout();
    }

    private void removeDestroyedViewReferences()
    {
        foreach (var entry in viewByCardInstanceId.Where(entry => entry.Value == null).ToArray())
        {
            viewByCardInstanceId.Remove(entry.Key);
        }
    }

    private void removeUntrackedChildViews()
    {
        var trackedViews = new HashSet<WorldCardView>(viewByCardInstanceId.Values);
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

        for (var index = 0; index < orderedViews.Count; index += 1)
        {
            var view = orderedViews[index];
            view.transform.SetParent(transform, false);
            view.transform.localScale = activeAnchor.CardScale;
            view.transform.localEulerAngles = activeAnchor.LocalRotation;
            var localPosition = getLocalPosition(activeAnchor, index, orderedViews.Count);
            view.transform.localPosition = localPosition;
            view.SetRestLocalPosition(localPosition);
        }
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

    private static void destroy(Object target)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

}
}
