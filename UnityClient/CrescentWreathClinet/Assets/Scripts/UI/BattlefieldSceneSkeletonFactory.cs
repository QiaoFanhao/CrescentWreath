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

        createHudPlaceholder("TurnInfoPanel", screenHudCanvas.transform,
            new Vector2(0.02f, 0.89f), new Vector2(0.22f, 0.98f), "当前回合 / 阶段", theme);
        createHudPlaceholder("PlayerHud", screenHudCanvas.transform,
            new Vector2(0.015f, 0.02f), new Vector2(0.2f, 0.18f), "本地玩家 HUD", theme);
        var localHandArea = createHudPlaceholder("LocalHandArea", screenHudCanvas.transform,
            new Vector2(0.22f, 0.01f), new Vector2(0.77f, 0.19f), "本地手牌区域", theme);
        createHudPlaceholder("ActionPanel", screenHudCanvas.transform,
            new Vector2(0.79f, 0.02f), new Vector2(0.985f, 0.19f), "行动面板", theme);
        createHudPlaceholder("CardDetailPanel", screenHudCanvas.transform,
            new Vector2(0.82f, 0.22f), new Vector2(0.985f, 0.48f), "卡牌详情", theme);

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
        gameClientRoot.ConfigureBattlefieldLayout(layout);

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
            layout.FlatLayoutDraft is not null;
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
            new Vector3(21f, 0.7f, 13f), theme.GraphiteBackground);
        createCube("TableInset", tableSurface, new Vector3(0f, -0.08f, 0f),
            new Vector3(19.7f, 0.18f, 11.7f), new Color(0.035f, 0.085f, 0.115f));
        createCube("TableRimTop", tableSurface, new Vector3(0f, 0.08f, 5.85f),
            new Vector3(20.5f, 0.22f, 0.25f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimBottom", tableSurface, new Vector3(0f, 0.08f, -5.85f),
            new Vector3(20.5f, 0.22f, 0.25f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimLeft", tableSurface, new Vector3(-10.1f, 0.08f, 0f),
            new Vector3(0.25f, 0.22f, 11.9f), new Color(0.08f, 0.16f, 0.2f));
        createCube("TableRimRight", tableSurface, new Vector3(10.1f, 0.08f, 0f),
            new Vector3(0.25f, 0.22f, 11.9f), new Color(0.08f, 0.16f, 0.2f));

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

        createZoneAnchor(parent, "SakuraCakeDeckAnchor", "sakuraCakeDeck",
            new Vector3(5.25f, 0.18f, 3.05f), BattlefieldZoneLayoutType.stack, 20,
            new Vector2(0.03f, 0.03f), true, theme.CoralTeam);
        createZoneAnchor(parent, "SakuraCakeZoneAnchor", "sakuraCakeZone",
            new Vector3(6.75f, 0.18f, 3.05f), BattlefieldZoneLayoutType.horizontalRow, 4,
            new Vector2(0.28f, 0.25f), true, theme.CoralTeam);
        createZoneAnchor(parent, "AnomalyDeckAnchor", "anomalyDeck",
            new Vector3(8.35f, 0.18f, 4.55f), BattlefieldZoneLayoutType.stack, 10,
            new Vector2(0.03f, 0.03f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "CurrentAnomalyAnchor", "currentAnomaly",
            new Vector3(8.35f, 0.18f, 2.9f), BattlefieldZoneLayoutType.single, 1,
            new Vector2(1.15f, 1.55f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "ResolvedAnomalyAnchor", "resolvedAnomaly",
            new Vector3(8.35f, 0.18f, 1.3f), BattlefieldZoneLayoutType.stack, 10,
            new Vector2(0.05f, 0.05f), true, theme.DarkRedAnomaly);
        createZoneAnchor(parent, "GapZoneAnchor", "gapZone",
            new Vector3(8.35f, 0.18f, -1.15f), BattlefieldZoneLayoutType.verticalColumn, 8,
            new Vector2(0.35f, 0.35f), true, theme.MutedText);
    }

    private static void createPlayerFieldAnchors(Transform parent, BattlefieldUiTheme theme)
    {
        createZoneAnchor(parent, "BottomPlayerFieldAnchor", "playerField:bottom",
            new Vector3(0f, 0.18f, -4.55f), BattlefieldZoneLayoutType.horizontalRow, 10,
            new Vector2(0.9f, 1.2f), true, theme.CyanTeam);
        createZoneAnchor(parent, "TopPlayerFieldAnchor", "playerField:top",
            new Vector3(0f, 0.18f, 4.75f), BattlefieldZoneLayoutType.horizontalRow, 10,
            new Vector2(0.9f, 1.2f), true, theme.CyanTeam);
        createZoneAnchor(parent, "LeftPlayerFieldAnchor", "playerField:left",
            new Vector3(-8.25f, 0.18f, -1.05f), BattlefieldZoneLayoutType.verticalColumn, 8,
            new Vector2(0.8f, 0.72f), true, theme.CoralTeam);
        createZoneAnchor(parent, "RightPlayerFieldAnchor", "playerField:right",
            new Vector3(6.85f, 0.18f, -1.05f), BattlefieldZoneLayoutType.verticalColumn, 8,
            new Vector2(0.8f, 0.72f), true, theme.CoralTeam);
    }

    private static void createMirrorArea(Transform parent, BattlefieldUiTheme theme)
    {
        createCube("CurrentPlayerMirrorPlatform", parent, new Vector3(0f, 0.08f, -0.85f),
            new Vector3(10.8f, 0.16f, 4.1f), new Color(0.04f, 0.16f, 0.2f));
        createZoneAnchor(parent, "CurrentPlayerMirrorAnchor", "currentPlayerMirror",
            new Vector3(0f, 0.22f, -0.85f), BattlefieldZoneLayoutType.grid, 15,
            new Vector2(1.15f, 1.45f), true, theme.GoldActive);
        createZoneAnchor(parent, "CurrentPlayerMirrorFieldBounds", "currentPlayerMirrorBounds",
            new Vector3(0f, 0.19f, -0.85f), BattlefieldZoneLayoutType.bounds, 15,
            new Vector2(10.2f, 3.55f), true, theme.GoldActive, false);
        createZoneAnchor(parent, "CurrentPlayerResourceAnchor", "currentPlayerResources",
            new Vector3(0f, 0.24f, 1.05f), BattlefieldZoneLayoutType.horizontalRow, 5,
            new Vector2(0.75f, 0.4f), true, theme.GoldActive);
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
        bool createPlate = true)
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
            new Vector3(0.72f, 0.02f, 1f),
            Vector3.zero,
            readOnly);

        if (createPlate)
        {
            var size = getPlateSize(layoutType, maxVisibleCards, spacing);
            createCube("DebugPlate", anchorObject.transform, new Vector3(0f, -0.06f, 0f),
                new Vector3(size.x, 0.05f, size.y), new Color(color.r, color.g, color.b, 0.52f));
        }

        return anchor;
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

    private static RectTransform createOverlay(Transform parent, BattlefieldUiTheme theme)
    {
        var dimmer = createPanel("InteractionDimmer", parent, Vector2.zero, Vector2.one,
            new Color(0.01f, 0.015f, 0.02f, 0.72f));
        createHudPlaceholder("ResponseWindowPlaceholder", parent,
            new Vector2(0.31f, 0.27f), new Vector2(0.69f, 0.73f), "响应窗口", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("InputContextPlaceholder", parent,
            new Vector2(0.27f, 0.22f), new Vector2(0.73f, 0.78f), "等待其他玩家选择", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("ToastArea", parent,
            new Vector2(0.33f, 0.88f), new Vector2(0.67f, 0.96f), "提示", theme)
            .gameObject.SetActive(false);
        createHudPlaceholder("LoadingBlocker", parent,
            new Vector2(0.42f, 0.45f), new Vector2(0.58f, 0.55f), "同步中...", theme)
            .gameObject.SetActive(false);
        return dimmer;
    }

    private static void createDebugLayer(Canvas canvas, BattlefieldUiTheme theme)
    {
        createHudPlaceholder("DebugBanner", canvas.transform,
            new Vector2(0.005f, 0.955f), new Vector2(0.29f, 0.995f),
            "调试层 / F10 开关 / SocketDebug 独立", theme);
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
}
}
