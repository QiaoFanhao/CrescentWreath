using System.Linq;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrescentWreath.Client.Tests.EditMode
{
public sealed class BattlefieldSceneSkeletonEditModeTests
{
    private const string ScenePath = "Assets/Scenes/GameClient.unity";

    [Test]
    public void GameClientScene_HasPerspectiveCameraTableWorldAndHybridCanvasLayers()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var tableWorld = layout.TableWorld;

        Assert.That(tableWorld, Is.Not.Null);
        Assert.That(tableWorld!.MainCamera, Is.Not.Null);
        Assert.That(tableWorld.MainCamera!.orthographic, Is.False);
        Assert.That(tableWorld.MainCamera.targetDisplay, Is.EqualTo(0));
        Assert.That(tableWorld.MainCamera.fieldOfView, Is.InRange(35f, 50f));
        Assert.That(tableWorld.DirectionalLight, Is.Not.Null);
        Assert.That(tableWorld.DirectionalLight!.type, Is.EqualTo(LightType.Directional));
        Assert.That(tableWorld.TableSurface, Is.Not.Null);
        Assert.That(tableWorld.PublicArea, Is.Not.Null);
        Assert.That(tableWorld.PlayerFieldAreas, Is.Not.Null);
        Assert.That(tableWorld.CurrentPlayerMirrorArea, Is.Not.Null);

        Assert.That(layout.ScreenHudCanvas?.name, Is.EqualTo("ScreenHudCanvas"));
        Assert.That(layout.OverlayCanvas?.name, Is.EqualTo("OverlayCanvas"));
        Assert.That(layout.DebugCanvas?.name, Is.EqualTo("DebugCanvas"));
        Assert.That(layout.FlatLayoutDraft, Is.Not.Null);
        Assert.That(layout.FlatLayoutDraft!.activeSelf, Is.False);
    }

    [Test]
    public void RequiredZoneAnchors_ExistWithUniqueKeysAndReadonlyMirror()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var anchors = layout.TableWorld!.GetComponentsInChildren<BattlefieldZoneAnchor>(true);
        var expectedNames = new[]
        {
            "PublicTreasureDeckAnchor",
            "SummonZoneAnchor01",
            "SummonZoneAnchor02",
            "SummonZoneAnchor03",
            "SummonZoneAnchor04",
            "SummonZoneAnchor05",
            "SummonZoneAnchor06",
            "SakuraCakeAnchor",
            "AnomalyDeckAnchor",
            "CurrentAnomalyAnchor",
            "ResolvedAnomalyAnchor",
            "GapZoneAnchor",
            "BottomPlayerFieldAnchor",
            "BottomPlayerDeckAnchor",
            "BottomPlayerDiscardAnchor",
            "LeftPlayerFieldAnchor",
            "LeftPlayerDeckAnchor",
            "LeftPlayerDiscardAnchor",
            "TopPlayerFieldAnchor",
            "TopPlayerDeckAnchor",
            "TopPlayerDiscardAnchor",
            "RightPlayerFieldAnchor",
            "RightPlayerDeckAnchor",
            "RightPlayerDiscardAnchor",
            "CurrentPlayerMirrorAnchor",
            "CurrentPlayerMirrorFieldBounds",
            "CurrentPlayerResourceAnchor",
        };

        CollectionAssert.IsSubsetOf(expectedNames, anchors.Select(anchor => anchor.name).ToArray());
        Assert.That(anchors.Select(anchor => anchor.ZoneViewKey).Distinct().Count(), Is.EqualTo(anchors.Length));
        Assert.That(anchors.All(anchor => !string.IsNullOrWhiteSpace(anchor.ZoneViewKey)), Is.True);
        var mirror = anchors.Single(anchor => anchor.name == "CurrentPlayerMirrorAnchor");
        Assert.That(mirror.ReadOnly, Is.True);
        Assert.That(mirror.MaxVisibleCards, Is.EqualTo(15));
        Assert.That(mirror.LayoutType, Is.EqualTo(BattlefieldZoneLayoutType.horizontalRow));
    }

    [Test]
    public void TopPlayerField_IsSeparatedBehindThePublicSummonRow()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var anchors = layout.TableWorld!.GetComponentsInChildren<BattlefieldZoneAnchor>(true);
        var topPlayerField = anchors.Single(anchor => anchor.name == "TopPlayerFieldAnchor");
        var summonRow = anchors.Single(anchor => anchor.name == "SummonZoneAnchor01");

        Assert.That(
            topPlayerField.transform.position.z,
            Is.GreaterThan(summonRow.transform.position.z + 1f),
            "The top player's private field must stay near the far table edge, behind the public summon row.");
    }

    [Test]
    public void PlayerFieldAnchors_UseSeatReadableCardRotations()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var anchors = layout.TableWorld!.GetComponentsInChildren<BattlefieldZoneAnchor>(true);

        Assert.That(anchors.Single(anchor => anchor.name == "BottomPlayerFieldAnchor").LocalRotation.y, Is.EqualTo(0f));
        Assert.That(anchors.Single(anchor => anchor.name == "TopPlayerFieldAnchor").LocalRotation.y, Is.EqualTo(180f));
        Assert.That(anchors.Single(anchor => anchor.name == "LeftPlayerFieldAnchor").LocalRotation.y, Is.EqualTo(90f));
        Assert.That(anchors.Single(anchor => anchor.name == "RightPlayerFieldAnchor").LocalRotation.y, Is.EqualTo(-90f));
    }

    [Test]
    public void PlayerDeckAndDiscardAnchors_AreSeparatedFromFieldAndPublicAreas()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var anchors = layout.TableWorld!.GetComponentsInChildren<BattlefieldZoneAnchor>(true);

        assertStackAnchor(anchors, "BottomPlayerDeckAnchor");
        assertStackAnchor(anchors, "BottomPlayerDiscardAnchor");
        assertStackAnchor(anchors, "TopPlayerDeckAnchor");
        assertStackAnchor(anchors, "TopPlayerDiscardAnchor");
        assertStackAnchor(anchors, "LeftPlayerDeckAnchor");
        assertStackAnchor(anchors, "LeftPlayerDiscardAnchor");
        assertStackAnchor(anchors, "RightPlayerDeckAnchor");
        assertStackAnchor(anchors, "RightPlayerDiscardAnchor");

        Assert.That(
            anchors.Single(anchor => anchor.name == "BottomPlayerDeckAnchor").transform.position.x,
            Is.LessThan(anchors.Single(anchor => anchor.name == "BottomPlayerFieldAnchor").transform.position.x - 5f));
        Assert.That(
            anchors.Single(anchor => anchor.name == "BottomPlayerDiscardAnchor").transform.position.x,
            Is.GreaterThan(anchors.Single(anchor => anchor.name == "BottomPlayerFieldAnchor").transform.position.x + 5f));
        Assert.That(
            anchors.Single(anchor => anchor.name == "RightPlayerDeckAnchor").transform.position.x,
            Is.GreaterThan(anchors.Single(anchor => anchor.name == "RightPlayerFieldAnchor").transform.position.x));
        Assert.That(
            anchors.Single(anchor => anchor.name == "LeftPlayerDeckAnchor").transform.position.x,
            Is.LessThan(anchors.Single(anchor => anchor.name == "LeftPlayerFieldAnchor").transform.position.x));
    }

    [Test]
    public void PublicUtilityZones_StaySeparatedFromRightPlayerPrivateField()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var anchors = layout.TableWorld!.GetComponentsInChildren<BattlefieldZoneAnchor>(true);
        var currentAnomaly = anchors.Single(anchor => anchor.name == "CurrentAnomalyAnchor");
        var resolvedAnomaly = anchors.Single(anchor => anchor.name == "ResolvedAnomalyAnchor");
        var sakuraCake = anchors.Single(anchor => anchor.name == "SakuraCakeAnchor");
        var gapZone = anchors.Single(anchor => anchor.name == "GapZoneAnchor");

        Assert.That(resolvedAnomaly.transform.position.z, Is.EqualTo(currentAnomaly.transform.position.z).Within(0.05f));
        Assert.That(resolvedAnomaly.transform.position.x, Is.LessThan(currentAnomaly.transform.position.x));
        Assert.That(gapZone.transform.position.x, Is.EqualTo(sakuraCake.transform.position.x).Within(0.05f));
        Assert.That(gapZone.transform.position.z, Is.LessThan(sakuraCake.transform.position.z));
        Assert.That(gapZone.transform.position.z, Is.GreaterThan(1.2f));

        var gapPlate = gapZone.transform.Find("DebugPlate");
        Assert.That(gapPlate, Is.Not.Null);
        Assert.That(gapPlate!.localScale, Is.EqualTo(new Vector3(1.05f, 0.05f, 1.35f)));
    }

    [Test]
    public void CurrentPlayerMirrorPlatform_DoesNotCoverRightPlayerField()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var platform = layout.TableWorld!.CurrentPlayerMirrorArea!.Find("CurrentPlayerMirrorPlatform");
        var anchors = layout.TableWorld.GetComponentsInChildren<BattlefieldZoneAnchor>(true);
        var bounds = anchors.Single(anchor => anchor.name == "CurrentPlayerMirrorFieldBounds");

        Assert.That(platform, Is.Not.Null);
        Assert.That(platform!.localScale.x, Is.EqualTo(11f));
        Assert.That(bounds.CardSpacing.x, Is.EqualTo(10.8f));
    }

    private static void assertStackAnchor(BattlefieldZoneAnchor[] anchors, string name)
    {
        var anchor = anchors.Single(item => item.name == name);
        Assert.That(anchor.LayoutType, Is.EqualTo(BattlefieldZoneLayoutType.stack));
        Assert.That(anchor.ReadOnly, Is.True);
    }

    [Test]
    public void ScreenCanvases_UseUnifiedScalerTmpAndNoFullscreenBattlefieldPanel()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        foreach (var canvas in new[]
                 {
                     layout.ScreenHudCanvas!,
                     layout.OverlayCanvas!,
                     layout.DebugCanvas!,
                 })
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null, canvas.name);
            Assert.That(scaler!.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
            Assert.That(canvas.GetComponentsInChildren<Text>(true), Is.Empty, canvas.name);
            Assert.That(canvas.GetComponentsInChildren<TMP_Text>(true), Is.Not.Empty, canvas.name);
        }

        var hudImages = layout.ScreenHudCanvas!.GetComponentsInChildren<Image>(true);
        Assert.That(
            hudImages.Any(image =>
                image.rectTransform.anchorMin == Vector2.zero &&
                image.rectTransform.anchorMax == Vector2.one),
            Is.False,
            "ScreenHudCanvas must not recreate a fullscreen flat battlefield.");
    }

    [Test]
    public void CameraFramesAllCoreAreasAtSixteenByNine()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        var camera = layout.TableWorld!.MainCamera!;
        camera.aspect = 16f / 9f;
        foreach (var name in new[]
                 {
                     "PublicTreasureDeckAnchor",
                     "CurrentAnomalyAnchor",
                     "BottomPlayerFieldAnchor",
                     "LeftPlayerFieldAnchor",
                     "TopPlayerFieldAnchor",
                     "RightPlayerFieldAnchor",
                     "CurrentPlayerMirrorAnchor",
                 })
        {
            var anchor = layout.TableWorld.GetComponentsInChildren<BattlefieldZoneAnchor>(true)
                .Single(item => item.name == name);
            var viewport = camera.WorldToViewportPoint(anchor.transform.position);
            Assert.That(viewport.z, Is.GreaterThan(0f), name);
            Assert.That(viewport.x, Is.InRange(0f, 1f), name);
            Assert.That(viewport.y, Is.InRange(0.12f, 1f), name);
        }
    }

    [Test]
    public void DebugCanvas_CanBeToggledWithoutAffectingTableOrHud()
    {
        var layout = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        layout.SetDebugVisible(true);
        Assert.That(layout.IsDebugVisible, Is.True);
        Assert.That(layout.TableWorld!.gameObject.activeSelf, Is.True);
        Assert.That(layout.ScreenHudCanvas!.gameObject.activeSelf, Is.True);

        layout.SetDebugVisible(false);
        Assert.That(layout.IsDebugVisible, Is.False);
        Assert.That(layout.TableWorld.gameObject.activeSelf, Is.True);
        Assert.That(layout.ScreenHudCanvas.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void BattlefieldTheme_UsesSourceHanSansDynamicFont()
    {
        var theme = ensureLayout(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)).Theme;
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Resources/UI/Fonts/SourceHanSansSC-Regular SDF.asset");

        Assert.That(theme, Is.Not.Null);
        Assert.That(fontAsset, Is.Not.Null);
        Assert.That(fontAsset!.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        Assert.That(theme!.PrimaryFont, Is.SameAs(fontAsset));
    }

    private static T? findInScene<T>(Scene scene)
        where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(item => item.gameObject.scene == scene);
    }

    private static BattlefieldLayoutRoot ensureLayout(Scene scene)
    {
        var root = findInScene<GameClientRoot>(scene);
        Assert.That(root, Is.Not.Null);
        return BattlefieldSceneSkeletonFactory.Ensure(root!);
    }
}
}
