using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public static class BattlefieldSceneSkeletonFactory
{
    private const string ThemeResourcePath = "UI/BattlefieldUiTheme";
    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
    private static readonly string[] RequiredZoneAnchorNames =
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
        "BottomPlayerCharacterAnchor",
        "BottomPlayerDeckAnchor",
        "BottomPlayerDiscardAnchor",
        "BottomPlayerStatusAnchor",
        "LeftPlayerFieldAnchor",
        "LeftPlayerCharacterAnchor",
        "LeftPlayerDeckAnchor",
        "LeftPlayerDiscardAnchor",
        "LeftPlayerStatusAnchor",
        "TopPlayerFieldAnchor",
        "TopPlayerCharacterAnchor",
        "TopPlayerDeckAnchor",
        "TopPlayerDiscardAnchor",
        "TopPlayerStatusAnchor",
        "RightPlayerFieldAnchor",
        "RightPlayerCharacterAnchor",
        "RightPlayerDeckAnchor",
        "RightPlayerDiscardAnchor",
        "RightPlayerStatusAnchor",
        "CurrentPlayerMirrorAnchor",
        "CurrentPlayerMirrorFieldBounds",
        "CurrentPlayerResourceAnchor",
    };

    public static BattlefieldLayoutRoot Ensure(GameClientRoot gameClientRoot)
    {
        var theme = Resources.Load<BattlefieldUiTheme>(ThemeResourcePath);
        if (theme is null)
        {
            theme = ScriptableObject.CreateInstance<BattlefieldUiTheme>();
            theme.name = "TemporaryBattlefieldUiTheme";
        }

        var existing = gameClientRoot.GetComponent<BattlefieldLayoutRoot>();
        if (hasCompleteLayout(existing))
        {
            applyThemeFont(existing!, theme);
            ensureFormalHudPanelLayout(existing!);
            existing!.SetDebugVisible(false);
            ensureDebugToggle(existing!);
            ensureSeatHudViews(existing!, theme);
            ensurePlayerHud(existing!);
            var existingNetworkController = ensureNetworkController(gameClientRoot);
            ensureConnectionPanel(existing!, theme, existingNetworkController);
            ensureLobbyRootView(existing!, theme, existingNetworkController);
            ensureCharacterSelectionOverlay(existing!, theme, existingNetworkController);
            ensureResponseWindowOverlay(existing!, theme, existingNetworkController);
            ensureInputContextOverlay(existing!, theme, existingNetworkController);
            ensureZoneCardListOverlay(existing!, theme);
            ensureMatchEndOverlay(existing!, theme);
            ensureCardInspectPreview(existing!, theme);
            ensureOverlayDefaultHidden(existing!);
            ensureCharacterZoneAnchorLayout(existing!, theme);
            ensureWorldCardDemo(existing!);
            ensureProjectionPresenter(existing!);
            return existing!;
        }

        var flatLayoutDraft = preserveFlatLayoutDraft(gameClientRoot.transform);
        removeFormalSkeleton(gameClientRoot.transform);

        configureEnvironment();
        ensureEventSystem(gameClientRoot);
        var sceneRig = createSceneRig(gameClientRoot.transform, out var mainCamera, out var directionalLight);
        var tableWorld = createTableWorld(gameClientRoot.transform, mainCamera, directionalLight, theme);
        var screenHudCanvas = createCanvas("ScreenHudCanvas", gameClientRoot.transform, 100);
        var overlayCanvas = createCanvas("OverlayCanvas", gameClientRoot.transform, 200);
        var debugCanvas = createCanvas("DebugCanvas", gameClientRoot.transform, 300);

        var turnInfoPanel = createHudPlaceholder("TurnInfoPanel", screenHudCanvas.transform,
            new Vector2(0.01f, 0.70f), new Vector2(0.34f, 0.985f), "Turn / Phase", theme);
        configureTurnInfoPanelRect(turnInfoPanel);
        ensureTeamResourcePanel(turnInfoPanel, theme);
        var networkController = ensureNetworkController(gameClientRoot);
        ensureConnectionPanel(screenHudCanvas.transform, theme, networkController);
        var playerHud = createHudPlaceholder("PlayerHud", screenHudCanvas.transform,
            new Vector2(0.015f, 0.02f), new Vector2(0.2f, 0.18f), "Local Player HUD", theme);
        ensureLocalPlayerHudView(playerHud);
        var localHandArea = createHudPlaceholder("LocalHandArea", screenHudCanvas.transform,
            new Vector2(0.22f, 0.01f), new Vector2(0.77f, 0.19f), "Local Hand Area", theme);
        createHudPlaceholder("ActionPanel", screenHudCanvas.transform,
            new Vector2(0.78f, 0.02f), new Vector2(0.985f, 0.36f), "Action Panel", theme);
        ensureSeatHudViews(screenHudCanvas.transform, theme);

        var interactionDimmer = createOverlay(overlayCanvas.transform, theme);
        createDebugLayer(debugCanvas, theme);

        var layout = existing ?? gameClientRoot.gameObject.AddComponent<BattlefieldLayoutRoot>();
        layout.Configure(
            theme,
            tableWorld,
            screenHudCanvas,
            overlayCanvas,
            debugCanvas,
            localHandArea,
            interactionDimmer,
            flatLayoutDraft);
        layout.SetOverlayPreviewVisible(false);
        layout.SetDebugVisible(false);
        ensureFormalHudPanelLayout(layout);
        gameClientRoot.ConfigureBattlefieldLayout(layout);
        ensureDebugToggle(layout);
        ensureConnectionPanel(layout, theme, networkController);
        ensureLobbyRootView(layout, theme, networkController);
        ensureCharacterSelectionOverlay(layout, theme, networkController);
        ensureResponseWindowOverlay(layout, theme, networkController);
        ensureInputContextOverlay(layout, theme, networkController);
        ensureZoneCardListOverlay(layout, theme);
        ensureMatchEndOverlay(layout, theme);
        ensureCardInspectPreview(layout, theme);
        ensureOverlayDefaultHidden(layout);
        ensureWorldCardDemo(layout);
        ensureProjectionPresenter(layout);

        _ = sceneRig;
        return layout;
    }

    private static bool hasCompleteLayout(BattlefieldLayoutRoot? layout)
    {
        return layout is not null &&
            layout.TableWorld is not null &&
            layout.TableWorld.MainCamera is not null &&
            layout.ScreenHudCanvas is not null &&
            layout.OverlayCanvas is not null &&
            layout.DebugCanvas is not null &&
            layout.FlatLayoutDraft is not null &&
            hasRequiredZoneAnchors(layout.TableWorld);
    }

    private static bool hasRequiredZoneAnchors(BattlefieldTableWorld tableWorld)
    {
        var anchorNames = tableWorld.GetComponentsInChildren<BattlefieldZoneAnchor>(true)
            .Select(anchor => anchor.name)
            .ToHashSet();
        return RequiredZoneAnchorNames.All(anchorNames.Contains);
    }

    private static void configureEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.19f, 0.23f, 0.29f);
        RenderSettings.ambientEquatorColor = new Color(0.09f, 0.12f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.03f, 0.04f);
        RenderSettings.ambientIntensity = 0.82f;
        RenderSettings.fog = false;
    }

    private static void ensureEventSystem(GameClientRoot gameClientRoot)
    {
        var eventSystem = gameClientRoot.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
            .FirstOrDefault();
        if (eventSystem is null)
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule is not null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(legacyModule);
            }
            else
            {
                Object.DestroyImmediate(legacyModule);
            }
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() is null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static Transform createSceneRig(
        Transform parent,
        out Camera mainCamera,
        out Light directionalLight)
    {
        var sceneRig = new GameObject("SceneRig").transform;
        sceneRig.SetParent(parent, false);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(sceneRig, false);
        cameraObject.transform.localPosition = new Vector3(0f, 13.8f, -13.2f);
        cameraObject.transform.localEulerAngles = new Vector3(52f, 0f, 0f);
        cameraObject.AddComponent<PhysicsRaycaster>();
        mainCamera = cameraObject.GetComponent<Camera>();
        mainCamera.orthographic = false;
        mainCamera.fieldOfView = 43f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
        mainCamera.targetDisplay = 0;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.018f, 0.025f, 0.035f);
        mainCamera.allowHDR = true;

        var lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.transform.SetParent(sceneRig, false);
        lightObject.transform.localEulerAngles = new Vector3(48f, -32f, 0f);
        directionalLight = lightObject.GetComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.color = new Color(1f, 0.93f, 0.84f);
        directionalLight.intensity = 1.08f;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.55f;
        RenderSettings.sun = directionalLight;
        return sceneRig;
    }

    private static BattlefieldTableWorld createTableWorld(
        Transform parent,
        Camera mainCamera,
        Light directionalLight,
        BattlefieldUiTheme theme)
    {
        var tableWorldObject = new GameObject("TableWorld");
        tableWorldObject.transform.SetParent(parent, false);
        var tableWorld = tableWorldObject.AddComponent<BattlefieldTableWorld>();

        var tableSurface = new GameObject("TableSurface").transform;
        tableSurface.SetParent(tableWorldObject.transform, false);
        createCube("TableBase", tableSurface, new Vector3(0f, -0.48f, 0f),
            new Vector3(21f, 0.7f, 14.2f), theme.GraphiteBackground);
        createCube("TableInset", tableSurface, new Vector3(0f, -0.08f, 0f),
            new Vector3(19.7f, 0.18f, 12.9f), new Color(0.035f, 0.085f, 0.115f));
        createCube("TableRimTop", tableSurface, new Vector3(0f, 0.08f, 6.45f),
            new Vector3(20.5f, 0.22f, 0.25f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimBottom", tableSurface, new Vector3(0f, 0.08f, -6.45f),
            new Vector3(20.5f, 0.22f, 0.25f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimLeft", tableSurface, new Vector3(-10.1f, 0.08f, 0f),
            new Vector3(0.25f, 0.22f, 13.1f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimRight", tableSurface, new Vector3(10.1f, 0.08f, 0f),
            new Vector3(0.25f, 0.22f, 13.1f), new Color(0.08f, 0.16f, 0.2f));

        var publicArea = new GameObject("PublicArea").transform;
        publicArea.SetParent(tableWorldObject.transform, false);
        createPublicAnchors(publicArea, theme);

        var playerFieldAreas = new GameObject("PlayerFieldAreas").transform;
        playerFieldAreas.SetParent(tableWorldObject.transform, false);
        createPlayerFieldAnchors(playerFieldAreas, theme);

        var mirrorArea = new GameObject("CurrentPlayerMirrorArea").transform;
        mirrorArea.SetParent(tableWorldObject.transform, false);
        createMirrorArea(mirrorArea, theme);

        tableWorld.Configure(
            mainCamera,
            directionalLight,
            tableSurface,
            publicArea,
            playerFieldAreas,
            mirrorArea);
        return tableWorld;
    }

    private static void createPublicAnchors(Transform parent, BattlefieldUiTheme theme)
    {
        createZoneAnchor(parent, "PublicTreasureDeckAnchor", "publicTreasureDeck",
            new Vector3(-8.2f, 0.18f, 3.05f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CyanTeam);

        var summonPositions = new[] { -5f, -3.35f, -1.7f, -0.05f, 1.6f, 3.25f };
        for (var index = 0; index < summonPositions.Length; index += 1)
        {
            createZoneAnchor(
                parent,
                $"SummonZoneAnchor{index + 1:00}",
                $"summonZone:{index + 1}",
                new Vector3(summonPositions[index], 0.18f, 3.05f),
                BattlefieldZoneLayoutType.single,
                1,
                new Vector2(1.15f, 1.55f),
                true,
                theme.GoldActive);
        }

        createZoneAnchor(parent, "SakuraCakeAnchor", "sakuraCakeZone",
            new Vector3(5.65f, 0.18f, 3.05f), BattlefieldZoneLayoutType.stack, 20,
            new Vector2(0.02f, 0.02f), true, theme.CoralTeam);
        createZoneAnchor(parent, "AnomalyDeckAnchor", "anomalyDeck",
            new Vector3(8.35f, 0.18f, 4.55f), BattlefieldZoneLayoutType.stack, 10,
            new Vector2(0.03f, 0.03f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "CurrentAnomalyAnchor", "currentAnomaly",
            new Vector3(8.35f, 0.18f, 2.9f), BattlefieldZoneLayoutType.single, 1,
            new Vector2(1.15f, 1.55f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "ResolvedAnomalyAnchor", "resolvedAnomaly",
            new Vector3(7.05f, 0.18f, 2.9f), BattlefieldZoneLayoutType.stack, 10,
            new Vector2(0.05f, 0.05f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "GapZoneAnchor", "gapZone",
            new Vector3(5.65f, 0.18f, 1.6f), BattlefieldZoneLayoutType.stack, 8,
            new Vector2(0.04f, 0.04f), true, theme.MutedText, plateSize: new Vector2(1.05f, 1.35f));
    }

    private static void createPlayerFieldAnchors(Transform parent, BattlefieldUiTheme theme)
    {
        createZoneAnchor(parent, "BottomPlayerFieldAnchor", "playerField:bottom",
            new Vector3(0f, 0.18f, -4.55f), BattlefieldZoneLayoutType.horizontalRow, 10,
            new Vector2(1.08f, 1.2f), true, theme.CyanTeam);
        createCharacterZoneAnchor(parent, "BottomPlayerCharacterAnchor", "playerCharacter:bottom",
            new Vector3(-5.25f, 0.2f, -3.52f), BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(0.86f, 0.86f), theme.CyanTeam);
        createZoneAnchor(parent, "BottomPlayerDeckAnchor", "playerDeck:bottom",
            new Vector3(-6.35f, 0.18f, -4.55f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CyanTeam);
        createZoneAnchor(parent, "BottomPlayerDiscardAnchor", "playerDiscard:bottom",
            new Vector3(6.35f, 0.18f, -4.55f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CyanTeam);
        createStatusZoneAnchor(parent, "BottomPlayerStatusAnchor", "playerStatus:bottom",
            new Vector3(0f, 0.2f, -3.55f), BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(0.78f, 0.78f), theme.GoldActive);

        createZoneAnchor(parent, "TopPlayerFieldAnchor", "playerField:top",
            new Vector3(0f, 0.18f, 5.3f), BattlefieldZoneLayoutType.horizontalRow, 10,
            new Vector2(1.08f, 1.2f), true, theme.CyanTeam, localRotation: new Vector3(0f, 180f, 0f));
        createCharacterZoneAnchor(parent, "TopPlayerCharacterAnchor", "playerCharacter:top",
            new Vector3(5.25f, 0.2f, 4.25f), BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(0.86f, 0.86f), theme.CyanTeam, localRotation: new Vector3(0f, 180f, 0f));
        createZoneAnchor(parent, "TopPlayerDeckAnchor", "playerDeck:top",
            new Vector3(6.35f, 0.18f, 5.3f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CyanTeam, localRotation: new Vector3(0f, 180f, 0f));
        createZoneAnchor(parent, "TopPlayerDiscardAnchor", "playerDiscard:top",
            new Vector3(-6.35f, 0.18f, 5.3f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CyanTeam, localRotation: new Vector3(0f, 180f, 0f));
        createStatusZoneAnchor(parent, "TopPlayerStatusAnchor", "playerStatus:top",
            new Vector3(0f, 0.2f, 4.25f), BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(0.78f, 0.78f), theme.GoldActive, localRotation: new Vector3(0f, 180f, 0f));

        createZoneAnchor(parent, "LeftPlayerFieldAnchor", "playerField:left",
            new Vector3(-8.25f, 0.18f, -1.05f), BattlefieldZoneLayoutType.verticalColumn, 8,
            new Vector2(0.8f, 1.08f), true, theme.CoralTeam, localRotation: new Vector3(0f, 90f, 0f));
        createCharacterZoneAnchor(parent, "LeftPlayerCharacterAnchor", "playerCharacter:left",
            new Vector3(-7.15f, 0.2f, -3.1f), BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(0.86f, 0.86f), theme.CoralTeam, localRotation: new Vector3(0f, 90f, 0f));
        createZoneAnchor(parent, "LeftPlayerDeckAnchor", "playerDeck:left",
            new Vector3(-9.2f, 0.18f, -3.45f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CoralTeam, localRotation: new Vector3(0f, 90f, 0f));
        createZoneAnchor(parent, "LeftPlayerDiscardAnchor", "playerDiscard:left",
            new Vector3(-9.2f, 0.18f, 1.45f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CoralTeam, localRotation: new Vector3(0f, 90f, 0f));
        createStatusZoneAnchor(parent, "LeftPlayerStatusAnchor", "playerStatus:left",
            new Vector3(-7.15f, 0.2f, -1.05f), BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(0.78f, 0.78f), theme.GoldActive, localRotation: new Vector3(0f, 90f, 0f));

        createZoneAnchor(parent, "RightPlayerFieldAnchor", "playerField:right",
            new Vector3(6.85f, 0.18f, -1.05f), BattlefieldZoneLayoutType.verticalColumn, 8,
            new Vector2(0.8f, 1.08f), true, theme.CoralTeam, localRotation: new Vector3(0f, -90f, 0f));
        createCharacterZoneAnchor(parent, "RightPlayerCharacterAnchor", "playerCharacter:right",
            new Vector3(5.75f, 0.2f, -3.1f), BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(0.86f, 0.86f), theme.CoralTeam, localRotation: new Vector3(0f, -90f, 0f));
        createZoneAnchor(parent, "RightPlayerDeckAnchor", "playerDeck:right",
            new Vector3(9f, 0.18f, -3.45f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CoralTeam, localRotation: new Vector3(0f, -90f, 0f));
        createZoneAnchor(parent, "RightPlayerDiscardAnchor", "playerDiscard:right",
            new Vector3(9f, 0.18f, 1.45f), BattlefieldZoneLayoutType.stack, 30,
            new Vector2(0.03f, 0.03f), true, theme.CoralTeam, localRotation: new Vector3(0f, -90f, 0f));
        createStatusZoneAnchor(parent, "RightPlayerStatusAnchor", "playerStatus:right",
            new Vector3(5.75f, 0.2f, -1.05f), BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(0.78f, 0.78f), theme.GoldActive, localRotation: new Vector3(0f, -90f, 0f));

    }

    private static void createMirrorArea(Transform parent, BattlefieldUiTheme theme)
    {
        createCube("CurrentPlayerMirrorPlatform", parent, new Vector3(0f, 0.08f, -0.85f),
            new Vector3(11f, 0.16f, 4.1f), new Color(0.04f, 0.16f, 0.2f));
        createZoneAnchor(parent, "CurrentPlayerMirrorAnchor", "currentPlayerMirror",
            new Vector3(0f, 0.22f, -0.85f), BattlefieldZoneLayoutType.horizontalRow, 15,
            new Vector2(1.08f, 1.45f), true, theme.GoldActive);
        createZoneAnchor(parent, "CurrentPlayerMirrorFieldBounds", "currentPlayerMirrorBounds",
            new Vector3(0f, 0.19f, -0.85f), BattlefieldZoneLayoutType.bounds, 15,
            new Vector2(10.8f, 3.55f), true, theme.GoldActive, false);
        createZoneAnchor(parent, "CurrentPlayerResourceAnchor", "currentPlayerResources",
            new Vector3(0f, 0.24f, 1.05f), BattlefieldZoneLayoutType.horizontalRow, 5,
            new Vector2(0.75f, 0.4f), true, theme.GoldActive, plateSize: new Vector2(5.6f, 0.72f));
    }

    private static BattlefieldZoneAnchor createZoneAnchor(
        Transform parent,
        string name,
        string key,
        Vector3 position,
        BattlefieldZoneLayoutType layoutType,
        int maxVisibleCards,
        Vector2 spacing,
        bool readOnly,
        Color color,
        bool createPlate = true,
        Vector3? localRotation = null,
        Vector2? plateSize = null,
        Vector3? cardScale = null)
    {
        var anchorObject = new GameObject(name);
        anchorObject.transform.SetParent(parent, false);
        anchorObject.transform.localPosition = position;
        var anchor = anchorObject.AddComponent<BattlefieldZoneAnchor>();
        anchor.Configure(
            key,
            layoutType,
            maxVisibleCards,
            spacing,
            cardScale ?? new Vector3(WorldCardView.StandardCardWidth, 0.02f, WorldCardView.StandardCardHeight),
            localRotation ?? Vector3.zero,
            readOnly);

        if (createPlate)
        {
            var size = plateSize ?? getPlateSize(layoutType, maxVisibleCards, spacing);
            createCube("DebugPlate", anchorObject.transform, new Vector3(0f, -0.06f, 0f),
                new Vector3(size.x, 0.05f, size.y), new Color(color.r, color.g, color.b, 0.52f));
        }

        return anchor;
    }

    private static void ensureCharacterZoneAnchorLayout(BattlefieldLayoutRoot layout, BattlefieldUiTheme theme)
    {
        if (layout.TableWorld is null)
        {
            return;
        }

        configureExistingCharacterAnchor(
            layout.TableWorld.transform,
            "BottomPlayerCharacterAnchor",
            "playerCharacter:bottom",
            new Vector3(-5.25f, 0.2f, -3.52f),
            BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(1.08f, 1.2f),
            theme.CyanTeam);
        configureExistingCharacterAnchor(
            layout.TableWorld.transform,
            "TopPlayerCharacterAnchor",
            "playerCharacter:top",
            new Vector3(5.25f, 0.2f, 4.25f),
            BattlefieldZoneLayoutType.horizontalRow,
            new Vector2(1.08f, 1.2f),
            theme.CyanTeam,
            new Vector3(0f, 180f, 0f));
        configureExistingCharacterAnchor(
            layout.TableWorld.transform,
            "LeftPlayerCharacterAnchor",
            "playerCharacter:left",
            new Vector3(-7.15f, 0.2f, -3.1f),
            BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(1.08f, 1.2f),
            theme.CoralTeam,
            new Vector3(0f, 90f, 0f));
        configureExistingCharacterAnchor(
            layout.TableWorld.transform,
            "RightPlayerCharacterAnchor",
            "playerCharacter:right",
            new Vector3(5.75f, 0.2f, -3.1f),
            BattlefieldZoneLayoutType.verticalColumn,
            new Vector2(1.08f, 1.2f),
            theme.CoralTeam,
            new Vector3(0f, -90f, 0f));
    }

    private static void configureExistingCharacterAnchor(
        Transform root,
        string anchorName,
        string zoneViewKey,
        Vector3 localPosition,
        BattlefieldZoneLayoutType layoutType,
        Vector2 spacing,
        Color color,
        Vector3? localRotation = null)
    {
        var anchorTransform = root.GetComponentsInChildren<BattlefieldZoneAnchor>(true)
            .FirstOrDefault(anchor => anchor.name == anchorName)
            ?.transform;
        if (anchorTransform is null)
        {
            return;
        }

        anchorTransform.localPosition = localPosition;
        var anchor = anchorTransform.GetComponent<BattlefieldZoneAnchor>();
        anchor.Configure(
            zoneViewKey,
            layoutType,
            2,
            spacing,
            new Vector3(WorldCardView.StandardCardWidth, 0.02f, WorldCardView.StandardCardHeight),
            localRotation ?? Vector3.zero,
            true);

        var plate = anchorTransform.Find("DebugPlate");
        if (plate is not null)
        {
            var plateSize = layoutType == BattlefieldZoneLayoutType.verticalColumn
                ? new Vector2(1.2f, 2.7f)
                : new Vector2(2.7f, 1.2f);
            plate.localScale = new Vector3(plateSize.x, 0.05f, plateSize.y);
            var renderer = plate.GetComponent<Renderer>();
            if (renderer is not null)
            {
                renderer.sharedMaterial ??= createRuntimeMaterial("CharacterAnchorPlateMaterial", color);
                renderer.sharedMaterial.color = new Color(color.r, color.g, color.b, 0.52f);
            }
        }
    }

    private static BattlefieldZoneAnchor createStatusZoneAnchor(
        Transform parent,
        string name,
        string key,
        Vector3 position,
        BattlefieldZoneLayoutType layoutType,
        Vector2 spacing,
        Color color,
        Vector3? localRotation = null)
    {
        return createZoneAnchor(
            parent,
            name,
            key,
            position,
            layoutType,
            8,
            spacing,
            true,
            color,
            localRotation: localRotation,
            plateSize: layoutType == BattlefieldZoneLayoutType.verticalColumn
                ? new Vector2(0.82f, 5.4f)
                : new Vector2(5.4f, 0.82f),
            cardScale: new Vector3(0.54f, 0.02f, 0.76f));
    }

    private static BattlefieldZoneAnchor createCharacterZoneAnchor(
        Transform parent,
        string name,
        string key,
        Vector3 position,
        BattlefieldZoneLayoutType layoutType,
        Vector2 spacing,
        Color color,
        Vector3? localRotation = null)
    {
        return createZoneAnchor(
            parent,
            name,
            key,
            position,
            layoutType,
            2,
            spacing,
            true,
            color,
            localRotation: localRotation,
            plateSize: layoutType == BattlefieldZoneLayoutType.verticalColumn
                ? new Vector2(0.95f, 2.25f)
                : new Vector2(2.25f, 0.95f),
            cardScale: new Vector3(WorldCardView.StandardCardWidth, 0.02f, WorldCardView.StandardCardHeight));
    }

    private static Vector2 getPlateSize(
        BattlefieldZoneLayoutType layoutType,
        int maxVisibleCards,
        Vector2 spacing)
    {
        return layoutType switch
        {
            BattlefieldZoneLayoutType.horizontalRow =>
                new Vector2(Mathf.Min(9.5f, 0.9f + spacing.x * Mathf.Max(0, maxVisibleCards - 1)), 1.35f),
            BattlefieldZoneLayoutType.verticalColumn =>
                new Vector2(1.25f, Mathf.Min(6f, 1.2f + spacing.y * Mathf.Max(0, maxVisibleCards - 1))),
            BattlefieldZoneLayoutType.grid => new Vector2(9.8f, 3.5f),
            BattlefieldZoneLayoutType.bounds => new Vector2(spacing.x, spacing.y),
            _ => new Vector2(1.05f, 1.35f),
        };
    }

    private static void createCube(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        var renderer = cube.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = createRuntimeMaterial(name + "Material", color);
        var collider = cube.GetComponent<Collider>();
        if (collider is not null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }
    }

    private static Material createRuntimeMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Unlit/Color");
        var material = new Material(shader)
        {
            name = name,
            color = color,
        };
        material.SetFloat("_Smoothness", 0.18f);
        material.SetFloat("_Metallic", 0.08f);
        return material;
    }

    private static GameObject preserveFlatLayoutDraft(Transform root)
    {
        var draft = root.Find("FlatLayoutDraft");
        if (draft is null)
        {
            draft = new GameObject("FlatLayoutDraft").transform;
            draft.SetParent(root, false);
        }

        var candidates = new[]
        {
            "LegacyDraftCanvas",
            "GameCanvas",
            "WorldCanvas",
            "HudCanvas",
            "OverlayCanvas",
            "DebugCanvas",
        };
        foreach (var candidateName in candidates)
        {
            var candidate = root.Find(candidateName);
            if (candidate is null || candidate == draft)
            {
                continue;
            }

            candidate.SetParent(draft, false);
            candidate.name = candidateName switch
            {
                "WorldCanvas" => "FlatWorldCanvasDraft",
                "HudCanvas" => "FlatHudCanvasDraft",
                "OverlayCanvas" => "FlatOverlayCanvasDraft",
                "DebugCanvas" => "FlatDebugCanvasDraft",
                "GameCanvas" => "LegacyDraftCanvas",
                _ => candidateName,
            };
        }

        foreach (var sceneRoot in root.gameObject.scene.GetRootGameObjects())
        {
            if (sceneRoot.transform == root || sceneRoot.name is not ("GameCanvas" or "LegacyDraftCanvas"))
            {
                continue;
            }

            sceneRoot.transform.SetParent(draft, false);
            sceneRoot.name = "LegacyDraftCanvas";
        }

        draft.gameObject.SetActive(false);
        return draft.gameObject;
    }

    private static void removeFormalSkeleton(Transform root)
    {
        foreach (var name in new[] { "SceneRig", "TableWorld", "ScreenHudCanvas", "OverlayCanvas", "DebugCanvas" })
        {
            removeChild(root, name);
        }
    }

    private static void removeChild(Transform root, string name)
    {
        var child = root.Find(name);
        if (child is null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(child.gameObject);
        }
        else
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static Canvas createCanvas(string name, Transform parent, int sortingOrder)
    {
        var canvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = false;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform createHudPlaceholder(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string label,
        BattlefieldUiTheme theme)
    {
        var panel = createPanel(name, parent, anchorMin, anchorMax,
            new Color(theme.DeepBluePanel.r, theme.DeepBluePanel.g, theme.DeepBluePanel.b, 0.78f));
        createText("Label", panel, label, 18f, TextAlignmentOptions.Center, theme);
        return panel;
    }

    private static void ensureLocalPlayerHudView(RectTransform playerHud)
    {
        var label = playerHud.Find("Label");
        if (label is not null)
        {
            label.gameObject.SetActive(false);
        }

        if (playerHud.GetComponent<LocalPlayerHudView>() is null)
        {
            playerHud.gameObject.AddComponent<LocalPlayerHudView>();
        }
    }

    private static void ensurePlayerHud(BattlefieldLayoutRoot layout)
    {
        var playerHud = layout.ScreenHudCanvas is null
            ? null
            : layout.ScreenHudCanvas.transform.Find("PlayerHud")?.GetComponent<RectTransform>();
        if (playerHud is null)
        {
            return;
        }

        playerHud.anchorMin = new Vector2(0.015f, 0.02f);
        playerHud.anchorMax = new Vector2(0.2f, 0.18f);
        playerHud.offsetMin = Vector2.zero;
        playerHud.offsetMax = Vector2.zero;
        ensureLocalPlayerHudView(playerHud);
    }

    private static void ensureSeatHudViews(BattlefieldLayoutRoot layout, BattlefieldUiTheme theme)
    {
        if (layout.ScreenHudCanvas is not null)
        {
            ensureSeatHudViews(layout.ScreenHudCanvas.transform, theme);
        }
    }

    private static void ensureSeatHudViews(Transform parent, BattlefieldUiTheme theme)
    {
        var bottomSeatHud = parent.Find("BottomPlayerSeatHud");
        if (bottomSeatHud is not null)
        {
            bottomSeatHud.gameObject.SetActive(false);
        }

        ensureSeatHudView(parent, BattlefieldSeatDirection.top, "TopPlayerSeatHud",
            new Vector2(0.4f, 0.88f), new Vector2(0.6f, 0.985f), theme);
        ensureSeatHudView(parent, BattlefieldSeatDirection.left, "LeftPlayerSeatHud",
            new Vector2(0.015f, 0.42f), new Vector2(0.18f, 0.68f), theme);
        ensureSeatHudView(parent, BattlefieldSeatDirection.right, "RightPlayerSeatHud",
            new Vector2(0.82f, 0.42f), new Vector2(0.985f, 0.68f), theme);
    }

    private static void ensureSeatHudView(
        Transform parent,
        BattlefieldSeatDirection direction,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        BattlefieldUiTheme theme)
    {
        var existing = parent.Find(name);
        var panel = existing is null
            ? createPanel(name, parent, anchorMin, anchorMax, new Color(theme.DeepBluePanel.r, theme.DeepBluePanel.g, theme.DeepBluePanel.b, 0.62f))
            : existing.GetComponent<RectTransform>();
        if (panel is null)
        {
            return;
        }

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        var portrait = ensureSeatRect(panel, "CharacterPortraitPlaceholder", new Vector2(0.02f, 0.08f), new Vector2(0.45f, 0.94f));
        var resource = ensureSeatRect(panel, "ResourceBarPlaceholder", new Vector2(0.47f, 0.54f), new Vector2(0.97f, 0.72f));
        var statuses = ensureSeatRect(panel, "StatusContainer", new Vector2(0.47f, 0.74f), new Vector2(0.97f, 0.95f));
        var counters = ensureSeatRect(panel, "CounterContainer", new Vector2(0.02f, 0.02f), new Vector2(0.45f, 0.07f));
        var publicField = ensureSeatRect(panel, "PublicFieldContainer", new Vector2(0.47f, 0.06f), new Vector2(0.97f, 0.48f));
        var playerName = ensureSeatText(panel, "PlayerNameText", "座位", new Vector2(0.47f, 0.78f), new Vector2(0.97f, 0.96f), 16f, theme);
        var hp = ensureSeatText(panel, "HpText", "HP -", new Vector2(0.47f, 0.60f), new Vector2(0.97f, 0.78f), 15f, theme);
        var hand = ensureSeatText(panel, "HandCountText", "手牌 -", new Vector2(0.47f, 0.36f), new Vector2(0.97f, 0.52f), 14f, theme);
        var deck = ensureSeatText(panel, "DeckCountText", "牌库 -", new Vector2(0.47f, 0.20f), new Vector2(0.97f, 0.36f), 14f, theme);
        var discard = ensureSeatText(panel, "DiscardCountText", "弃牌 -", new Vector2(0.47f, 0.04f), new Vector2(0.97f, 0.20f), 14f, theme);

        var seatView = panel.GetComponent<BattlefieldPlayerSeatView>();
        if (seatView is null)
        {
            seatView = panel.gameObject.AddComponent<BattlefieldPlayerSeatView>();
        }

        seatView.Configure(direction, portrait, playerName, hp, resource, statuses, counters, hand, deck, discard, publicField);
    }

    private static RectTransform ensureSeatRect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var existing = parent.Find(name);
        RectTransform rect;
        if (existing is null)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }
        else
        {
            rect = existing.GetComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text ensureSeatText(
        RectTransform parent,
        string name,
        string value,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        BattlefieldUiTheme theme)
    {
        var existing = parent.Find(name);
        TMP_Text text;
        if (existing is null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TMP_Text>();
        }

        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(4f, 2f);
        rect.offsetMax = new Vector2(-4f, -2f);
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Left;
        text.color = theme.NormalText;
        text.raycastTarget = false;
        if (theme.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private static RectTransform createOverlay(Transform parent, BattlefieldUiTheme theme)
    {
        var dimmer = createPanel("InteractionDimmer", parent, Vector2.zero, Vector2.one,
            new Color(0.01f, 0.015f, 0.02f, 0.72f));
        createHudPlaceholder("ResponseWindowPlaceholder", parent,
            new Vector2(0.31f, 0.27f), new Vector2(0.69f, 0.73f), "Response Window", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("InputContextPlaceholder", parent,
            new Vector2(0.27f, 0.22f), new Vector2(0.73f, 0.78f), "Waiting for player choice", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("ToastArea", parent,
            new Vector2(0.33f, 0.88f), new Vector2(0.67f, 0.96f), "Toast", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("LoadingBlocker", parent,
            new Vector2(0.42f, 0.45f), new Vector2(0.58f, 0.55f), "Syncing...", theme)
            .gameObject.SetActive(false);
        return dimmer;
    }

    private static GameClientNetworkController ensureNetworkController(GameClientRoot gameClientRoot)
    {
        var projectionViewState = gameClientRoot.ProjectionViewState ?? gameClientRoot.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = gameClientRoot.gameObject.AddComponent<ProjectionViewState>();
        }

        var networkController = gameClientRoot.GameClientNetworkController ??
            gameClientRoot.GetComponent<GameClientNetworkController>();
        if (networkController is null)
        {
            networkController = gameClientRoot.gameObject.AddComponent<GameClientNetworkController>();
        }

        networkController.Configure(projectionViewState);
        gameClientRoot.ConfigureNetworkController(networkController);
        return networkController;
    }

    private static void ensureConnectionPanel(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        if (layout.ScreenHudCanvas is null)
        {
            return;
        }

        ensureConnectionPanel(layout.ScreenHudCanvas.transform, theme, networkController);
    }

    private static void ensureLobbyRootView(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var panel = layout.OverlayCanvas.transform.Find("LobbyRoot") as RectTransform;
        if (panel is null)
        {
            panel = createPanel(
                "LobbyRoot",
                layout.OverlayCanvas.transform,
                Vector2.zero,
                Vector2.one,
                new Color(0.005f, 0.012f, 0.018f, 0.88f));
        }

        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        panel.SetAsLastSibling();
        if (panel.GetComponent<CanvasGroup>() is null)
        {
            panel.gameObject.AddComponent<CanvasGroup>();
        }

        var image = panel.GetComponent<Image>();
        if (image is not null)
        {
            image.color = new Color(0.005f, 0.012f, 0.018f, 0.88f);
            image.raycastTarget = true;
        }

        var lobbyRootView = panel.GetComponent<LobbyRootView>();
        if (lobbyRootView is null)
        {
            lobbyRootView = panel.gameObject.AddComponent<LobbyRootView>();
        }

        lobbyRootView.Configure(networkController, theme);
    }

    private static void ensureInputContextOverlay(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("InputContextPlaceholder") as RectTransform;
        if (placeholder is null)
        {
            placeholder = createHudPlaceholder(
                "InputContextPlaceholder",
                layout.OverlayCanvas.transform,
                new Vector2(0.27f, 0.22f),
                new Vector2(0.73f, 0.78f),
                "Waiting for player choice",
                theme);
        }

        placeholder.gameObject.SetActive(true);
        if (placeholder.GetComponent<CanvasGroup>() == null)
        {
            placeholder.gameObject.AddComponent<CanvasGroup>();
        }

        var image = placeholder.GetComponent<Image>();
        if (image is not null)
        {
            image.color = new Color(0.015f, 0.035f, 0.05f, 0.96f);
            image.raycastTarget = true;
        }

        var overlay = placeholder.GetComponent<InputContextOverlayView>();
        if (overlay is null)
        {
            overlay = placeholder.gameObject.AddComponent<InputContextOverlayView>();
        }

        var root = layout.GetComponent<GameClientRoot>();
        var projectionViewState = root?.ProjectionViewState ?? layout.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = layout.gameObject.AddComponent<ProjectionViewState>();
        }

        overlay.Configure(projectionViewState, networkController, theme);
    }

    private static void ensureCharacterSelectionOverlay(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("CharacterSelectionPlaceholder") as RectTransform;
        if (placeholder is null)
        {
            placeholder = createHudPlaceholder(
                "CharacterSelectionPlaceholder",
                layout.OverlayCanvas.transform,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.92f),
                "开局角色选择",
                theme);
        }
        else
        {
            placeholder.anchorMin = new Vector2(0.06f, 0.08f);
            placeholder.anchorMax = new Vector2(0.94f, 0.92f);
            placeholder.offsetMin = Vector2.zero;
            placeholder.offsetMax = Vector2.zero;
        }

        placeholder.gameObject.SetActive(true);
        if (placeholder.GetComponent<CanvasGroup>() == null)
        {
            placeholder.gameObject.AddComponent<CanvasGroup>();
        }

        var image = placeholder.GetComponent<Image>();
        if (image is not null)
        {
            image.color = new Color(0.015f, 0.035f, 0.05f, 0.96f);
            image.raycastTarget = true;
        }

        var overlay = placeholder.GetComponent<CharacterSelectionOverlayView>();
        if (overlay is null)
        {
            overlay = placeholder.gameObject.AddComponent<CharacterSelectionOverlayView>();
        }

        var root = layout.GetComponent<GameClientRoot>();
        var projectionViewState = root?.ProjectionViewState ?? layout.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = layout.gameObject.AddComponent<ProjectionViewState>();
        }

        overlay.Configure(projectionViewState, networkController, theme);
    }

    private static void ensureResponseWindowOverlay(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("ResponseWindowPlaceholder") as RectTransform;
        if (placeholder is null)
        {
            placeholder = createHudPlaceholder(
                "ResponseWindowPlaceholder",
                layout.OverlayCanvas.transform,
                new Vector2(0.31f, 0.27f),
                new Vector2(0.69f, 0.73f),
                "Response Window",
                theme);
        }

        placeholder.gameObject.SetActive(true);
        var overlay = placeholder.GetComponent<ResponseWindowOverlayView>();
        if (overlay is null)
        {
            overlay = placeholder.gameObject.AddComponent<ResponseWindowOverlayView>();
        }

        var root = layout.GetComponent<GameClientRoot>();
        var projectionViewState = root?.ProjectionViewState ?? layout.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = layout.gameObject.AddComponent<ProjectionViewState>();
        }

        overlay.Configure(projectionViewState, networkController, theme);
    }

    private static void ensureZoneCardListOverlay(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("ZoneCardListPlaceholder") as RectTransform;
        if (placeholder is null)
        {
            var placeholderObject = new GameObject(
                "ZoneCardListPlaceholder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            placeholderObject.transform.SetParent(layout.OverlayCanvas.transform, false);
            placeholder = placeholderObject.GetComponent<RectTransform>();
        }

        if (placeholder.GetComponent<CanvasGroup>() is null)
        {
            placeholder.gameObject.AddComponent<CanvasGroup>();
        }

        var overlay = placeholder.GetComponent<ZoneCardListOverlayView>();
        if (overlay is null)
        {
            overlay = placeholder.gameObject.AddComponent<ZoneCardListOverlayView>();
        }

        overlay.Configure(theme);
    }

    private static void ensureMatchEndOverlay(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("MatchEndOverlay") as RectTransform;
        if (placeholder is null)
        {
            var placeholderObject = new GameObject(
                "MatchEndOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            placeholderObject.transform.SetParent(layout.OverlayCanvas.transform, false);
            placeholder = placeholderObject.GetComponent<RectTransform>();
        }

        placeholder.anchorMin = Vector2.zero;
        placeholder.anchorMax = Vector2.one;
        placeholder.offsetMin = Vector2.zero;
        placeholder.offsetMax = Vector2.zero;

        var overlay = placeholder.GetComponent<MatchEndOverlayView>();
        if (overlay is null)
        {
            overlay = placeholder.gameObject.AddComponent<MatchEndOverlayView>();
        }

        var root = layout.GetComponent<GameClientRoot>();
        var projectionViewState = root?.ProjectionViewState ?? layout.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = layout.gameObject.AddComponent<ProjectionViewState>();
        }

        overlay.Configure(projectionViewState, theme);
    }

    private static void ensureCardInspectPreview(
        BattlefieldLayoutRoot layout,
        BattlefieldUiTheme theme)
    {
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        var placeholder = layout.OverlayCanvas.transform.Find("CardInspectPreview") as RectTransform;
        if (placeholder is null)
        {
            var placeholderObject = new GameObject(
                "CardInspectPreview",
                typeof(RectTransform),
                typeof(CardInspectPreviewView));
            placeholderObject.transform.SetParent(layout.OverlayCanvas.transform, false);
            placeholder = placeholderObject.GetComponent<RectTransform>();
        }

        placeholder.anchorMin = Vector2.zero;
        placeholder.anchorMax = Vector2.one;
        placeholder.offsetMin = Vector2.zero;
        placeholder.offsetMax = Vector2.zero;

        var previewView = placeholder.GetComponent<CardInspectPreviewView>();
        if (previewView is null)
        {
            previewView = placeholder.gameObject.AddComponent<CardInspectPreviewView>();
        }

        previewView.Configure(theme);

        var controller = layout.GetComponent<CardInspectPreviewController>();
        if (controller is null)
        {
            controller = layout.gameObject.AddComponent<CardInspectPreviewController>();
        }

        controller.Configure(layout.TableWorld?.MainCamera, previewView);
    }

    private static void ensureOverlayDefaultHidden(BattlefieldLayoutRoot layout)
    {
        layout.SetOverlayPreviewVisible(false);
        if (layout.OverlayCanvas is null)
        {
            return;
        }

        setOverlayChildActive(layout.OverlayCanvas.transform, "ToastArea", false);
        setOverlayChildActive(layout.OverlayCanvas.transform, "LoadingBlocker", false);
    }

    private static void setOverlayChildActive(Transform parent, string childName, bool active)
    {
        var child = parent.Find(childName);
        if (child is not null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static void ensureConnectionPanel(
        Transform parent,
        BattlefieldUiTheme theme,
        GameClientNetworkController networkController)
    {
        var panel = parent.Find("ConnectionPanel") as RectTransform;
        if (panel is null)
        {
            panel = createPanel(
                "ConnectionPanel",
                parent,
                new Vector2(0.235f, 0.895f),
                new Vector2(0.765f, 0.985f),
                new Color(0.02f, 0.08f, 0.12f, 0.86f));
        }

        panel.anchorMin = new Vector2(0.235f, 0.895f);
        panel.anchorMax = new Vector2(0.765f, 0.985f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        if (panel.GetComponent<CanvasGroup>() is null)
        {
            panel.gameObject.AddComponent<CanvasGroup>();
        }

        var title = getOrCreateTmpText(panel, "Title", "正式连接", 13f, TextAlignmentOptions.Left, theme);
        setChildAnchors(title.rectTransform, new Vector2(0.02f, 0.58f), new Vector2(0.12f, 0.95f));

        var wsInput = getOrCreateInputField(
            panel,
            "WsUrlInput",
            "ws://127.0.0.1:18080/ws",
            "WebSocket 地址",
            theme);
        setChildAnchors(wsInput.GetComponent<RectTransform>(), new Vector2(0.13f, 0.54f), new Vector2(0.47f, 0.92f));

        var viewerInput = getOrCreateInputField(panel, "ViewerIdInput", "1", "玩家ID", theme);
        setChildAnchors(viewerInput.GetComponent<RectTransform>(), new Vector2(0.48f, 0.54f), new Vector2(0.56f, 0.92f));

        var connectButton = getOrCreateButton(panel, "ConnectButton", "连接", theme);
        setChildAnchors(connectButton.GetComponent<RectTransform>(), new Vector2(0.57f, 0.54f), new Vector2(0.66f, 0.92f));

        var disconnectButton = getOrCreateButton(panel, "DisconnectButton", "断开", theme);
        setChildAnchors(disconnectButton.GetComponent<RectTransform>(), new Vector2(0.67f, 0.54f), new Vector2(0.76f, 0.92f));

        var statusText = getOrCreateTmpText(panel, "ConnectionStatusText", "连接状态：未连接", 12f, TextAlignmentOptions.Left, theme);
        setChildAnchors(statusText.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.48f, 0.48f));

        var errorText = getOrCreateTmpText(panel, "ConnectionErrorText", string.Empty, 11f, TextAlignmentOptions.Left, theme);
        errorText.color = theme.Warning;
        setChildAnchors(errorText.rectTransform, new Vector2(0.49f, 0.12f), new Vector2(0.98f, 0.48f));

        var connectionPanel = panel.GetComponent<GameClientConnectionPanel>();
        if (connectionPanel is null)
        {
            connectionPanel = panel.gameObject.AddComponent<GameClientConnectionPanel>();
        }

        connectionPanel.Configure(
            networkController,
            wsInput,
            viewerInput,
            connectButton,
            disconnectButton,
            statusText,
            errorText);
    }

    private static TMP_InputField getOrCreateInputField(
        RectTransform parent,
        string name,
        string defaultValue,
        string placeholder,
        BattlefieldUiTheme theme)
    {
        var existing = parent.Find(name);
        GameObject inputObject;
        TMP_InputField inputField;
        if (existing is null)
        {
            inputObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);
            inputField = inputObject.GetComponent<TMP_InputField>();
            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObject.transform, false);
            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(6f, 2f);
            textAreaRect.offsetMax = new Vector2(-6f, -2f);

            var text = createText("Text", textArea.transform, defaultValue, 12f, TextAlignmentOptions.Left, theme);
            var placeholderText = createText("Placeholder", textArea.transform, placeholder, 12f, TextAlignmentOptions.Left, theme);
            placeholderText.color = theme.MutedText;
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.text = defaultValue;
        }
        else
        {
            inputObject = existing.gameObject;
            inputField = inputObject.GetComponent<TMP_InputField>();
            if (inputField is null)
            {
                inputField = inputObject.AddComponent<TMP_InputField>();
            }
        }

        var inputImage = inputObject.GetComponent<Image>();
        if (inputImage is null)
        {
            inputImage = inputObject.AddComponent<Image>();
        }

        inputImage.color = new Color(0.01f, 0.025f, 0.035f, 0.92f);
        if (theme.PrimaryFont is not null && inputField.textComponent is not null)
        {
            inputField.textComponent.font = theme.PrimaryFont;
        }

        return inputField;
    }

    private static Button getOrCreateButton(RectTransform parent, string name, string label, BattlefieldUiTheme theme)
    {
        var existing = parent.Find(name);
        GameObject buttonObject;
        Button button;
        if (existing is null)
        {
            buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();
            var labelText = createText("Label", buttonObject.transform, label, 12f, TextAlignmentOptions.Center, theme);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = new Vector2(3f, 1f);
            labelText.rectTransform.offsetMax = new Vector2(-3f, -1f);
        }
        else
        {
            buttonObject = existing.gameObject;
            button = buttonObject.GetComponent<Button>();
            if (button is null)
            {
                button = buttonObject.AddComponent<Button>();
            }
        }

        var buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage is null)
        {
            buttonImage = buttonObject.AddComponent<Image>();
        }

        buttonImage.color = new Color(0.12f, 0.34f, 0.42f, 0.96f);
        var text = buttonObject.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (text is not null)
        {
            text.text = label;
            if (theme.PrimaryFont is not null)
            {
                text.font = theme.PrimaryFont;
            }
        }

        return button;
    }

    private static TMP_Text getOrCreateTmpText(
        RectTransform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        BattlefieldUiTheme theme)
    {
        var existing = parent.Find(name);
        TMP_Text text;
        if (existing is null)
        {
            text = createText(name, parent, value, fontSize, alignment, theme);
        }
        else
        {
            text = existing.GetComponent<TMP_Text>();
            if (text is null)
            {
                text = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        if (theme.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private static void setChildAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void createDebugLayer(Canvas canvas, BattlefieldUiTheme theme)
    {
        createHudPlaceholder("DebugBanner", canvas.transform,
            new Vector2(0.005f, 0.955f), new Vector2(0.29f, 0.995f),
            "Debug Layer / F10 Toggle / SocketDebug", theme);
        if (canvas.GetComponent<SocketDebugPanel>() is null)
        {
            canvas.gameObject.AddComponent<SocketDebugPanel>();
        }
    }

    private static RectTransform createPanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        var rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text createText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        BattlefieldUiTheme theme)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 6f);
        rect.offsetMax = new Vector2(-10f, -6f);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = theme.NormalText;
        text.raycastTarget = false;
        if (theme.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }

        return text;
    }

    private static void applyThemeFont(BattlefieldLayoutRoot layout, BattlefieldUiTheme theme)
    {
        if (theme.PrimaryFont is null)
        {
            return;
        }

        foreach (var canvas in new[] { layout.ScreenHudCanvas, layout.OverlayCanvas, layout.DebugCanvas })
        {
            if (canvas is null)
            {
                continue;
            }

            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = theme.PrimaryFont;
            }
        }
    }

    private static void ensureFormalHudPanelLayout(BattlefieldLayoutRoot layout)
    {
        if (layout.ScreenHudCanvas is null)
        {
            return;
        }

        var turnInfoPanel = layout.ScreenHudCanvas.transform.Find("TurnInfoPanel") as RectTransform;
        if (turnInfoPanel is not null)
        {
            configureTurnInfoPanelRect(turnInfoPanel);
            ensureTeamResourcePanel(turnInfoPanel, layout.Theme);
        }

        removeLegacyHudPanel(layout.ScreenHudCanvas.transform, "CardDetailPanel");

        setHudPanelAnchors(
            layout.ScreenHudCanvas.transform,
            "ActionPanel",
            new Vector2(0.78f, 0.02f),
            new Vector2(0.985f, 0.36f));
    }

    private static void setHudPanelAnchors(
        Transform canvasTransform,
        string panelName,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        var rect = canvasTransform.Find(panelName) as RectTransform;
        if (rect is null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void removeLegacyHudPanel(Transform canvasTransform, string panelName)
    {
        var target = canvasTransform.Find(panelName);
        if (target is null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target.gameObject);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(target.gameObject);
        }
    }

    private static void configureTurnInfoPanelRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -8f);
        rect.sizeDelta = new Vector2(720f, 340f);
    }

    private static void ensureWorldCardDemo(BattlefieldLayoutRoot layout)
    {
        if (layout.TableWorld is null)
        {
            return;
        }

        if (layout.TableWorld.MainCamera is not null &&
            layout.TableWorld.MainCamera.GetComponent<PhysicsRaycaster>() is null)
        {
            layout.TableWorld.MainCamera.gameObject.AddComponent<PhysicsRaycaster>();
        }

        var demo = layout.TableWorld.GetComponent<BattlefieldWorldCardDemoController>();
        if (demo is null)
        {
            demo = layout.TableWorld.gameObject.AddComponent<BattlefieldWorldCardDemoController>();
        }

        demo.Configure(layout.TableWorld, null);
        demo.EnsureDemo();
    }

    private static void ensureTeamResourcePanel(RectTransform panel, BattlefieldUiTheme theme)
    {
        var image = panel.GetComponent<Image>();
        if (image is not null)
        {
            image.enabled = false;
        }

        var label = panel.Find("Label")?.GetComponent<TMP_Text>();
        if (label is not null)
        {
            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        var canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var view = panel.GetComponent<TeamResourcePanelView>();
        if (view is null)
        {
            view = panel.gameObject.AddComponent<TeamResourcePanelView>();
        }

        view.Configure(theme.PrimaryFont);
    }

    private static void ensureProjectionPresenter(BattlefieldLayoutRoot layout)
    {
        if (layout.TableWorld is null)
        {
            return;
        }

        var root = layout.GetComponent<GameClientRoot>();
        var projectionViewState = root is null
            ? layout.GetComponent<ProjectionViewState>()
            : root.ProjectionViewState ?? layout.GetComponent<ProjectionViewState>();
        if (projectionViewState is null)
        {
            projectionViewState = layout.gameObject.AddComponent<ProjectionViewState>();
        }

        var battlefieldView = layout.TableWorld.GetComponent<BattlefieldView>();
        if (battlefieldView is null)
        {
            battlefieldView = layout.TableWorld.gameObject.AddComponent<BattlefieldView>();
        }

        battlefieldView.Configure(layout.TableWorld);

        var statusText = layout.ScreenHudCanvas is null
            ? null
            : layout.ScreenHudCanvas.transform
                .Find("TurnInfoPanel/Label")
                ?.GetComponent<TMP_Text>();
        var turnInfoPanel = layout.ScreenHudCanvas is null
            ? null
            : layout.ScreenHudCanvas.transform.Find("TurnInfoPanel") as RectTransform;
        if (turnInfoPanel is not null)
        {
            ensureTeamResourcePanel(turnInfoPanel, layout.Theme);
        }
        var presenter = layout.TableWorld.GetComponent<BattlefieldProjectionPresenter>();
        if (presenter is null)
        {
            presenter = layout.TableWorld.gameObject.AddComponent<BattlefieldProjectionPresenter>();
        }

        presenter.Configure(
            projectionViewState,
            layout,
            battlefieldView,
            statusText);

        var demo = layout.TableWorld.GetComponent<BattlefieldWorldCardDemoController>();
        if (demo is not null)
        {
            demo.enabled = false;
        }

        if (projectionViewState.Current is null)
        {
            battlefieldView.ClearAll();
        }
    }

    private static void ensureDebugToggle(BattlefieldLayoutRoot layout)
    {
        var toggle = layout.GetComponent<BattlefieldDebugCanvasToggle>();
        if (toggle is null)
        {
            toggle = layout.gameObject.AddComponent<BattlefieldDebugCanvasToggle>();
        }

        toggle.Configure(layout);
    }
}
}

