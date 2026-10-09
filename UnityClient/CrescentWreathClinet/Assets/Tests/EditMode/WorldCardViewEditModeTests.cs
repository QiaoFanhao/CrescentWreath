using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using CrescentWreath.Client.UI;
using NUnit.Framework;
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

            Assert.That(view.transform.Find("VisualRoot"), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/CardBody"), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/FrontSurface"), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/BackSurface"), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/SelectionOutline"), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/HoverHighlight"), Is.Not.Null);
            Assert.That(view.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(view.transform.Find("VisualRoot/CardBody").localScale, Is.EqualTo(WorldCardView.CardBodyLocalScale));
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
    public void WorldCardView_EaseOutCubic_HasStableEndpoints()
    {
        Assert.That(WorldCardView.EaseOutCubic(0f), Is.EqualTo(0f).Within(0.001f));
        Assert.That(WorldCardView.EaseOutCubic(1f), Is.EqualTo(1f).Within(0.001f));
        Assert.That(WorldCardView.EaseOutCubic(-1f), Is.EqualTo(0f).Within(0.001f));
        Assert.That(WorldCardView.EaseOutCubic(2f), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void WorldCardView_SnapTo_AppliesCardPoseExactly()
    {
        var root = new GameObject("CardPoseSnapRoot");
        try
        {
            var view = WorldCardViewFactory.Create("WorldCardView", root.transform);
            var pose = new CardPose(
                new Vector3(1.25f, 0.35f, -2.5f),
                Quaternion.Euler(0f, 45f, 0f),
                new Vector3(0.6f, 0.02f, 0.84f));

            view.SnapTo(pose);

            Assert.That(view.transform.localPosition, Is.EqualTo(pose.Position));
            Assert.That(view.transform.localRotation.eulerAngles.y, Is.EqualTo(45f).Within(0.001f));
            Assert.That(view.transform.localScale, Is.EqualTo(pose.Scale));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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

            Assert.That(view.transform.Find("VisualRoot/FrontSurface").gameObject.activeSelf, Is.True);
            Assert.That(view.transform.Find("VisualRoot/BackSurface").gameObject.activeSelf, Is.False);

            view.SetFaceUp(false);

            Assert.That(view.transform.Find("VisualRoot/FrontSurface").gameObject.activeSelf, Is.False);
            Assert.That(view.transform.Find("VisualRoot/BackSurface").gameObject.activeSelf, Is.True);
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
            Assert.That(firstView.transform.localPosition, Is.EqualTo(arrangedPosition));
            Assert.That(firstView.transform.Find("VisualRoot").localPosition, Is.EqualTo(Vector3.up * 0.12f));

            firstView.SetHighlighted(false);
            Assert.That(firstView.transform.localPosition, Is.EqualTo(arrangedPosition));
            Assert.That(firstView.transform.Find("VisualRoot").localPosition, Is.EqualTo(Vector3.zero));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ZoneLayout_SharedRegistryReusesCardViewAcrossZones()
    {
        var root = new GameObject("ZoneLayoutRegistryRoot");
        try
        {
            var sourceLayout = createTestLayout(root.transform, "playerField:bottom", new Vector3(-2f, 0f, 0f));
            var targetLayout = createTestLayout(root.transform, "playerDiscard:bottom", new Vector3(2f, 0f, 0f));
            var layouts = new Dictionary<string, BattlefieldZoneLayout>
            {
                ["playerField:bottom"] = sourceLayout,
                ["playerDiscard:bottom"] = targetLayout,
            };
            var registry = new BattlefieldCardRegistry();
            var model = new WorldCardViewModel
            {
                cardInstanceNumericId = 91001,
                definitionId = "T001",
                zoneKey = "field",
                isFaceUp = true,
                isReadOnly = true,
            };

            sourceLayout.Reconcile(new[] { model }, registry, "table", layouts);
            var sourceView = sourceLayout.OrderedViews[0];
            targetLayout.Reconcile(new[] { model }, registry, "table", layouts);
            sourceLayout.Reconcile(System.Array.Empty<WorldCardViewModel>(), registry, "table", layouts);

            Assert.That(targetLayout.OrderedViews, Has.Count.EqualTo(1));
            Assert.That(targetLayout.OrderedViews[0], Is.SameAs(sourceView));
            Assert.That(sourceLayout.OrderedViews, Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ZoneMotionAnchorMap_PlayerFieldStartsFromSeatHandEdge()
    {
        var root = new GameObject("ZoneMotionHandSourceRoot");
        try
        {
            var layout = createTestLayout(root.transform, "playerField:bottom", Vector3.zero);
            var targetPose = new CardPose(new Vector3(1.2f, 0f, 0f), Quaternion.identity, Vector3.one);

            var sourcePose = BattlefieldZoneMotionAnchorMap.CreateSourcePose(
                "playerField:bottom",
                layout,
                targetPose,
                new Dictionary<string, BattlefieldZoneLayout>());

            Assert.That(sourcePose.Position.z, Is.LessThan(-1f));
            Assert.That(sourcePose.Position.x, Is.EqualTo(targetPose.Position.x * 0.55f).Within(0.001f));
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

    [Test]
    public void EffectPresentationClassifier_ClassifiesDamageText()
    {
        var category = EffectPresentationClassifier.Classify(
            string.Empty,
            "蕾米莉亚对莲造成3点体术伤害。");

        Assert.That(category, Is.EqualTo(EffectCategoryKeys.Damage));
    }

    [Test]
    public void EffectSfxCatalog_FallsBackToGenericEffect()
    {
        var catalog = ScriptableObject.CreateInstance<EffectSfxCatalog>();
        var clip = AudioClip.Create("GenericEffectTestClip", 1, 1, 44100, false);

        try
        {
            catalog.ReplaceEntriesForEditor(new[]
            {
                new EffectSfxCatalog.Entry
                {
                    effectCategoryKey = EffectCategoryKeys.GenericEffect,
                    clips = new List<AudioClip> { clip },
                    volume = 0.5f,
                },
            });

            var hasEntry = catalog.TryGetEntry("missingCategory", out var entry);

            Assert.That(hasEntry, Is.True);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.clips, Does.Contain(clip));
        }
        finally
        {
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(catalog);
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

    private static BattlefieldZoneLayout createTestLayout(Transform parent, string zoneViewKey, Vector3 position)
    {
        var anchorObject = new GameObject(zoneViewKey);
        anchorObject.transform.SetParent(parent, false);
        anchorObject.transform.localPosition = position;
        var anchor = anchorObject.AddComponent<BattlefieldZoneAnchor>();
        anchor.Configure(
            zoneViewKey,
            BattlefieldZoneLayoutType.horizontalRow,
            4,
            new Vector2(1.08f, 1.2f),
            WorldCardView.DefaultCardSize,
            Vector3.zero,
            true);
        var layout = anchorObject.AddComponent<BattlefieldZoneLayout>();
        layout.Configure(anchor);
        return layout;
    }

}
}
