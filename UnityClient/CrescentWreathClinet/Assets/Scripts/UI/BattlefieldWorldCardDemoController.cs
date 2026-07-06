using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldWorldCardDemoController : MonoBehaviour
{
    [SerializeField]
    private BattlefieldTableWorld? tableWorld;

    [SerializeField]
    private TMP_Text? cardDetailText;

    private readonly Dictionary<string, BattlefieldZoneLayout> layoutByZoneKey = new();
    private bool initialized;

    public IReadOnlyDictionary<string, BattlefieldZoneLayout> LayoutByZoneKey => layoutByZoneKey;

    public void Configure(BattlefieldTableWorld newTableWorld, TMP_Text? newCardDetailText)
    {
        tableWorld = newTableWorld;
        cardDetailText = newCardDetailText;
    }

    private void Start()
    {
        EnsureDemo();
    }

    public void EnsureDemo()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        buildLayouts();
        RefreshFakeData();
    }

    public void RefreshFakeData()
    {
        buildLayouts();
        reconcile("publicTreasureDeck", createFaceDownStack(7000, "T001", 5));
        reconcile("anomalyDeck", createFaceDownStack(7200, "A001", 3));
        reconcile("currentAnomaly", new[]
        {
            card(7300, "A001", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
        });
        reconcile("sakuraCakeZone", new[]
        {
            card(7400, "S001", true, false, true),
            card(7401, "S001", true, false, true),
            card(7402, "S001", true, false, true),
        });

        var summonIds = new[] { "T001", "T002", "T008", "T010", "T014", "T029" };
        for (var index = 0; index < summonIds.Length; index += 1)
        {
            reconcile($"summonZone:{index + 1}", new[]
            {
                card(7500 + index, summonIds[index], true, false, true),
            });
        }

        reconcile("playerDeck:bottom", createFaceDownStack(7610, "T001", 4));
        reconcile("playerDiscard:bottom", cards(7620, true, false, true, "T003", "T004"));
        reconcile("playerDeck:top", createFaceDownStack(7710, "T001", 4));
        reconcile("playerDiscard:top", cards(7720, true, false, true, "T005"));
        reconcile("playerDeck:left", createFaceDownStack(7810, "T001", 4));
        reconcile("playerDiscard:left", cards(7820, true, false, true, "T006"));
        reconcile("playerDeck:right", createFaceDownStack(7910, "T001", 4));
        reconcile("playerDiscard:right", cards(7920, true, false, true, "T007"));

        reconcile("playerField:bottom", cards(
            7600,
            true,
            true,
            false,
            "T008",
            "T002",
            "T009",
            "T010",
            "T014",
            "T029",
            "T016",
            "T023",
            "T026",
            "T027",
            "T003",
            "T004"));
        reconcile("playerField:top", cards(
            7700,
            true,
            false,
            true,
            "T010",
            "T014",
            "T018",
            "T019"));
        reconcile("playerField:left", cards(
            7800,
            true,
            false,
            true,
            "T003",
            "T004",
            "T005",
            "T006"));
        reconcile("playerField:right", cards(
            7900,
            true,
            false,
            true,
            "T011",
            "T012",
            "T013",
            "T015"));
        reconcile("currentPlayerMirror", new[]
        {
            card(8000, "T008", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8001, "T002", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8002, "T009", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8003, "T010", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8004, "T014", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8005, "T029", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8006, "T016", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8007, "T023", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8008, "T026", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
            card(8009, "T027", true, false, true, false, WorldCardDisplayMode.readOnlyMirror),
        });
    }

    private void buildLayouts()
    {
        if (tableWorld is null)
        {
            return;
        }

        layoutByZoneKey.Clear();
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

    private void reconcile(string zoneViewKey, IReadOnlyList<WorldCardViewModel> models)
    {
        if (!layoutByZoneKey.TryGetValue(zoneViewKey, out var layout))
        {
            return;
        }

        layout.Reconcile(models);
        foreach (var view in layout.OrderedViews)
        {
            view.Clicked -= handleCardClicked;
            view.Clicked += handleCardClicked;
        }
    }

    private void handleCardClicked(WorldCardView view)
    {
        if (view.IsInteractable && !view.IsReadOnly)
        {
            view.SetSelected(!view.IsSelected);
        }

        if (cardDetailText is not null)
        {
            cardDetailText.text = view.BuildDebugDescription();
        }
    }

    private static IReadOnlyList<WorldCardViewModel> createFaceDownStack(
        long startCardInstanceNumericId,
        string definitionId,
        int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => card(
                startCardInstanceNumericId + index,
                definitionId,
                false,
                false,
                true,
                false,
                WorldCardDisplayMode.deckBack))
            .ToArray();
    }

    private static IReadOnlyList<WorldCardViewModel> cards(
        long startCardInstanceNumericId,
        bool isFaceUp,
        bool isInteractable,
        bool isReadOnly,
        params string[] definitionIds)
    {
        return definitionIds
            .Select((definitionId, index) => card(
                startCardInstanceNumericId + index,
                definitionId,
                isFaceUp,
                isInteractable,
                isReadOnly))
            .ToArray();
    }

    private static WorldCardViewModel card(
        long cardInstanceNumericId,
        string definitionId,
        bool isFaceUp,
        bool isInteractable,
        bool isReadOnly,
        bool isSelected = false,
        WorldCardDisplayMode displayMode = WorldCardDisplayMode.normal)
    {
        return new WorldCardViewModel
        {
            cardInstanceNumericId = cardInstanceNumericId,
            definitionId = definitionId,
            isFaceUp = isFaceUp,
            isSelected = isSelected,
            isInteractable = isInteractable,
            isReadOnly = isReadOnly,
            displayMode = displayMode,
        };
    }

    private static void destroy(Object target)
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
