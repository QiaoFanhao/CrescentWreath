using System.IO;
using System.Linq;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrescentWreath.Client.Tests.EditMode
{
public sealed class WorldCardViewEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        CardArtService.ClearCacheForTests();
    }

    [TearDown]
    public void TearDown()
    {
        CardArtService.ClearCacheForTests();
    }

    [Test]
    public void WorldCardViewFactory_CreatesRequiredHierarchyAndCollider()
    {
        var root = new GameObject("WorldCardViewTestRoot");
        try
        {
            var view = WorldCardViewFactory.Create("WorldCardView", root.transform);

            Assert.That(view.transform.Find("CardBody"), Is.Not.Null);
            Assert.That(view.transform.Find("FrontSurface"), Is.Not.Null);
            Assert.That(view.transform.Find("BackSurface"), Is.Not.Null);
            Assert.That(view.transform.Find("SelectionOutline"), Is.Not.Null);
            Assert.That(view.transform.Find("HoverHighlight"), Is.Not.Null);
            Assert.That(view.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(view.transform.Find("CardBody").localScale, Is.EqualTo(WorldCardView.CardBodyLocalScale));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void WorldCardView_UsesReadablePrototypeFootprint()
    {
        Assert.That(WorldCardView.DefaultCardSize.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(WorldCardView.DefaultCardSize.y, Is.EqualTo(0.02f).Within(0.001f));
        Assert.That(WorldCardView.DefaultCardSize.z, Is.EqualTo(1f).Within(0.001f));
        Assert.That(WorldCardView.CardBodyLocalScale.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(WorldCardView.CardBodyLocalScale.y, Is.EqualTo(0.02f).Within(0.001f));
        Assert.That(WorldCardView.CardBodyLocalScale.z, Is.EqualTo(1.4f).Within(0.001f));
    }

    [Test]
    public void WorldCardView_BindTogglesFrontAndBackSurfaces()
    {
        var catalog = loadCatalog();
        CardArtService.SetCatalogForTests(catalog);
        var root = new GameObject("WorldCardViewTestRoot");
        try
        {
            var view = WorldCardViewFactory.Create("WorldCardView", root.transform);
            view.Bind(new WorldCardViewModel
            {
                cardInstanceNumericId = 1,
                definitionId = "T001",
                isFaceUp = true,
                isInteractable = true,
            });

            Assert.That(view.transform.Find("FrontSurface").gameObject.activeSelf, Is.True);
            Assert.That(view.transform.Find("BackSurface").gameObject.activeSelf, Is.False);

            view.SetFaceUp(false);

            Assert.That(view.transform.Find("FrontSurface").gameObject.activeSelf, Is.False);
            Assert.That(view.transform.Find("BackSurface").gameObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CardArtService_ReturnsCardBackForUnknownDefinition()
    {
        var catalog = loadCatalog();
        CardArtService.SetCatalogForTests(catalog);

        var sprite = CardArtService.GetSpriteForDefinition("UNKNOWN_TEST_ID");

        Assert.That(sprite, Is.SameAs(catalog.CardBack));
    }

    [Test]
    public void HorizontalZoneLayout_ArrangesSixCardsWithoutFullRebuild()
    {
        var root = new GameObject("ZoneLayoutTestRoot");
        try
        {
            var anchorObject = new GameObject("SummonZoneLayoutAnchor");
            anchorObject.transform.SetParent(root.transform, false);
            var anchor = anchorObject.AddComponent<BattlefieldZoneAnchor>();
            anchor.Configure(
                "summonZone:test",
                BattlefieldZoneLayoutType.horizontalRow,
                6,
                new Vector2(1.1f, 1.4f),
                WorldCardView.DefaultCardSize,
                Vector3.zero,
                true);

            var layout = anchorObject.AddComponent<BattlefieldZoneLayout>();
            layout.Configure(anchor);
            var models = Enumerable.Range(0, 6)
                .Select(index => new WorldCardViewModel
                {
                    cardInstanceNumericId = 100 + index,
                    definitionId = $"T00{index + 1}",
                    isFaceUp = true,
                    isReadOnly = true,
                })
                .ToArray();

            layout.Reconcile(models);
            var firstView = layout.OrderedViews[0];
            layout.Reconcile(models);

            Assert.That(layout.OrderedViews, Has.Count.EqualTo(6));
            Assert.That(layout.OrderedViews[0], Is.SameAs(firstView));
            Assert.That(layout.OrderedViews.Select(view => view.transform.localPosition.x).Distinct().Count(), Is.EqualTo(6));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ZoneLayout_ReconcileRemovesUntrackedLegacyCardViews()
    {
        var root = new GameObject("ZoneLayoutLegacyCleanupRoot");
        try
        {
            var anchorObject = new GameObject("FieldLayoutAnchor");
            anchorObject.transform.SetParent(root.transform, false);
            var anchor = anchorObject.AddComponent<BattlefieldZoneAnchor>();
            anchor.Configure(
                "playerField:test",
                BattlefieldZoneLayoutType.horizontalRow,
                12,
                new Vector2(1.08f, 1.2f),
                WorldCardView.DefaultCardSize,
                Vector3.zero,
                true);

            var layout = anchorObject.AddComponent<BattlefieldZoneLayout>();
            layout.Configure(anchor);
            _ = WorldCardViewFactory.Create("WorldCardView_legacy", layout.transform);

            var models = Enumerable.Range(0, 12)
                .Select(index => new WorldCardViewModel
                {
                    cardInstanceNumericId = 7600 + index,
                    definitionId = "T001",
                    isFaceUp = true,
                    isReadOnly = true,
                })
                .ToArray();

            layout.Reconcile(models);

            var directViews = layout.GetComponentsInChildren<WorldCardView>(true)
                .Where(view => view.transform.parent == layout.transform)
                .ToArray();
            Assert.That(layout.OrderedViews, Has.Count.EqualTo(12));
            Assert.That(directViews, Has.Length.EqualTo(12));
            Assert.That(directViews.Any(view => view.name == "WorldCardView_legacy"), Is.False);
            Assert.That(layout.OrderedViews.Select(view => view.transform.localPosition.x).Distinct().Count(), Is.EqualTo(12));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ZoneLayout_HoverKeepsCardAtArrangedSlotInsteadOfAnchorCenter()
    {
        var root = new GameObject("ZoneLayoutHoverRoot");
        try
        {
            var anchorObject = new GameObject("HoverLayoutAnchor");
            anchorObject.transform.SetParent(root.transform, false);
            var anchor = anchorObject.AddComponent<BattlefieldZoneAnchor>();
            anchor.Configure(
                "playerField:hover",
                BattlefieldZoneLayoutType.horizontalRow,
                3,
                new Vector2(1.08f, 1.2f),
                WorldCardView.DefaultCardSize,
                Vector3.zero,
                true);

            var layout = anchorObject.AddComponent<BattlefieldZoneLayout>();
            layout.Configure(anchor);
            var models = Enumerable.Range(0, 3)
                .Select(index => new WorldCardViewModel
                {
                    cardInstanceNumericId = 9000 + index,
                    definitionId = "T001",
                    isFaceUp = true,
                    isReadOnly = true,
                })
                .ToArray();

            layout.Reconcile(models);
            var firstView = layout.OrderedViews[0];
            var arrangedPosition = firstView.transform.localPosition;

            Assert.That(arrangedPosition, Is.Not.EqualTo(Vector3.zero));
            firstView.SetHighlighted(true);
            Assert.That(firstView.transform.localPosition, Is.EqualTo(arrangedPosition + Vector3.up * 0.12f));

            firstView.SetHighlighted(false);
            Assert.That(firstView.transform.localPosition, Is.EqualTo(arrangedPosition));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ProjectionPresenter_ClearsDemoDataWhenWaitingForProjection()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameClient.unity", OpenSceneMode.Single);
        var root = Resources.FindObjectsOfTypeAll<GameClientRoot>()
            .First(item => item.gameObject.scene == scene);
        var layout = BattlefieldSceneSkeletonFactory.Ensure(root);
        var demo = layout.TableWorld!.GetComponent<BattlefieldWorldCardDemoController>();
        var presenter = layout.TableWorld.GetComponent<BattlefieldProjectionPresenter>();
        var view = layout.TableWorld.GetComponent<BattlefieldView>();

        Assert.That(demo, Is.Not.Null);
        Assert.That(demo!.enabled, Is.False);
        Assert.That(presenter, Is.Not.Null);
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.LayoutByZoneKey["currentPlayerMirror"].OrderedViews, Is.Empty);
    }

    [Test]
    public void BattlefieldCardMapper_MapsProjectionSummonFields()
    {
        var model = BattlefieldCardViewModelMapper.FromProjectionCard(
            new ProjectionCardViewModel
            {
                cardInstanceNumericId = 42,
                definitionId = "T008",
                ownerPlayerNumericId = 2,
                zoneKey = "summonZone",
                isFaceUp = true,
                isDefensePlacedOnField = false,
                baseSummonSigilCost = 5,
                effectiveSummonSigilCost = 3,
                isSummonableByViewer = true,
            },
            readOnly: true);

        Assert.That(model.cardInstanceNumericId, Is.EqualTo(42));
        Assert.That(model.ownerPlayerNumericId, Is.EqualTo(2));
        Assert.That(model.zoneKey, Is.EqualTo("summonZone"));
        Assert.That(model.baseSummonSigilCost, Is.EqualTo(5));
        Assert.That(model.effectiveSummonSigilCost, Is.EqualTo(3));
        Assert.That(model.isSummonableByViewer, Is.True);
        Assert.That(model.isSelected, Is.True);
        Assert.That(model.isReadOnly, Is.True);
    }

    [Test]
    public void BattlefieldCardMapper_ShowsKnownProjectionCardsFaceUpEvenWhenPhysicalFaceDown()
    {
        var model = BattlefieldCardViewModelMapper.FromProjectionCard(
            new ProjectionCardViewModel
            {
                cardInstanceNumericId = 100008,
                definitionId = "starter:kourindouCoupon",
                zoneKey = "field",
                isFaceUp = false,
            },
            readOnly: true);

        Assert.That(model.isFaceUp, Is.True);
    }

    [Test]
    public void CardDetailPanel_ShowsProjectionCardFields()
    {
        var root = new GameObject("CardDetailPanelTestRoot");
        try
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            var panel = root.AddComponent<CardDetailPanel>();
            panel.Configure(label);

            panel.Show(new WorldCardViewModel
            {
                cardInstanceNumericId = 7,
                definitionId = "T002",
                ownerPlayerNumericId = 1,
                zoneKey = "hand",
                isFaceUp = true,
                isDefensePlacedOnField = true,
                baseSummonSigilCost = 2,
                effectiveSummonSigilCost = 1,
                isSummonableByViewer = false,
                summonUnavailableReasonKey = "wrongPhase",
            });

            Assert.That(label.text, Does.Contain("T002"));
            Assert.That(label.text, Does.Contain("owner=1"));
            Assert.That(label.text, Does.Contain("zone=hand"));
            Assert.That(label.text, Does.Contain("summonCost=2 -> 1"));
            Assert.That(label.text, Does.Contain("wrongPhase"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RuntimeWorldCardScripts_DoNotReferenceAssetDatabase()
    {
        var uiScriptFolder = Path.Combine(Application.dataPath, "Scripts/UI");
        var runtimeScripts = Directory.GetFiles(uiScriptFolder, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !path.Contains("Editor"));

        foreach (var scriptPath in runtimeScripts)
        {
            var text = File.ReadAllText(scriptPath);
            Assert.That(text.Contains("AssetDatabase"), Is.False, Path.GetFileName(scriptPath));
        }
    }

    private static CardArtCatalog loadCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(
            "Assets/Resources/CardArt/CardArtCatalog.asset");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog!.CardBack, Is.Not.Null);
        return catalog;
    }

}
}
