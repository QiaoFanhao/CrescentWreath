using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrescentWreath.Client.Presentation;
using CrescentWreath.Client.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI.Editor
{
public sealed class CardArtCatalogBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        CardArtCatalogBuilder.RebuildCatalog();
    }
}

public static class CardArtCatalogBuilder
{
    public const string CatalogAssetPath = "Assets/Resources/CardArt/CardArtCatalog.asset";
    public const string GameClientScenePath = "Assets/Scenes/GameClient.unity";

    private const string CardViewPrefabPath = "Assets/Prefabs/UI/CardView.prefab";
    private const string PlayerAreaPrefabPath = "Assets/Prefabs/UI/PlayerAreaView.prefab";
    private const string PublicZonePrefabPath = "Assets/Prefabs/UI/PublicZoneView.prefab";
    private const string InteractionOverlayPrefabPath = "Assets/Prefabs/UI/InteractionOverlay.prefab";

    private const string CardBackPath = "Assets/Art/Cards/Illustrations/CardBack.png";
    private const string BasicFolder = "Assets/Art/Cards/Illustrations/Relics/Basic";
    private const string SummonFolder = "Assets/Art/Cards/Illustrations/Relics/Summon";
    private const string SakuraFolder = "Assets/Art/Cards/Illustrations/Relics/Sakuracake";
    private const string AnomalyFolder = "Assets/Art/Cards/Illustrations/Anomaly";

    [InitializeOnLoadMethod]
    private static void ensureGeneratedAssetsAfterImport()
    {
        if (File.Exists(CatalogAssetPath) &&
            File.Exists(CardViewPrefabPath) &&
            File.Exists(PlayerAreaPrefabPath) &&
            File.Exists(PublicZonePrefabPath) &&
            File.Exists(InteractionOverlayPrefabPath) &&
            File.Exists(GameClientScenePath))
        {
            return;
        }

        EditorApplication.update -= tryGenerateMissingAssets;
        EditorApplication.update += tryGenerateMissingAssets;
    }

    private static void tryGenerateMissingAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.update -= tryGenerateMissingAssets;
        CreateFormalUiSkeleton();
    }

    [MenuItem("Crescent Wreath/UI/Rebuild Card Art Catalog")]
    public static void RebuildCatalog()
    {
        ensureFolder("Assets/Resources");
        ensureFolder("Assets/Resources/CardArt");

        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);
        if (catalog is null)
        {
            catalog = ScriptableObject.CreateInstance<CardArtCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        }

        var cardBack = loadSprite(CardBackPath);
        var entries = buildExpectedEntries();
        catalog.ReplaceEntriesForEditor(cardBack, entries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        validateIllustrationDirectory(entries);
        Debug.Log($"[CardArtCatalogBuilder] Rebuilt {CatalogAssetPath} with {entries.Count} card entries.");
    }

    [MenuItem("Crescent Wreath/UI/Create Formal UI Skeleton")]
    public static void CreateFormalUiSkeleton()
    {
        RebuildCatalog();
        ensureFolder("Assets/Prefabs");
        ensureFolder("Assets/Prefabs/UI");

        var cardViewPrefab = createCardViewPrefab();
        createPlayerAreaPrefab();
        createPublicZonePrefab(cardViewPrefab);
        var interactionOverlayPrefab = createInteractionOverlayPrefab();
        createGameClientScene(interactionOverlayPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CardArtCatalogBuilder] Formal uGUI skeleton was created.");
    }

    public static void BuildAllFromCommandLine()
    {
        CreateFormalUiSkeleton();
    }

    private static List<CardArtCatalog.Entry> buildExpectedEntries()
    {
        var entries = new List<CardArtCatalog.Entry>();
        for (var index = 1; index <= 29; index++)
        {
            var definitionId = $"T{index:000}";
            entries.Add(createEntry(definitionId, $"{SummonFolder}/{definitionId}.png"));
        }

        entries.Add(createEntry("T001B", $"{BasicFolder}/T001B.png"));
        entries.Add(createEntry("T002B", $"{BasicFolder}/T002B.png"));
        entries.Add(createEntry("S001", $"{SakuraFolder}/S001.png"));

        for (var index = 1; index <= 10; index++)
        {
            var definitionId = $"A{index:000}";
            entries.Add(createEntry(definitionId, $"{AnomalyFolder}/{definitionId}.png"));
        }

        var duplicateDefinitionIds = entries
            .GroupBy(entry => entry.definitionId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateDefinitionIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate card art definition IDs: {string.Join(", ", duplicateDefinitionIds)}");
        }

        return entries;
    }

    private static CardArtCatalog.Entry createEntry(string definitionId, string assetPath)
    {
        return new CardArtCatalog.Entry
        {
            definitionId = definitionId,
            sprite = loadSprite(assetPath),
        };
    }

    private static Sprite loadSprite(string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite is null)
        {
            throw new InvalidOperationException($"Required card art sprite is missing: {assetPath}");
        }

        return sprite;
    }

    private static void validateIllustrationDirectory(IReadOnlyCollection<CardArtCatalog.Entry> entries)
    {
        var expectedNames = new HashSet<string>(
            entries.Select(entry => $"{entry.definitionId}.png"),
            StringComparer.OrdinalIgnoreCase)
        {
            "CardBack.png",
        };

        var illustrationRoot = Path.GetFullPath("Assets/Art/Cards/Illustrations");
        var unknownFiles = Directory
            .EnumerateFiles(illustrationRoot, "*.png", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(fileName => !expectedNames.Contains(fileName))
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        if (unknownFiles.Length > 0)
        {
            Debug.LogWarning(
                $"[CardArtCatalogBuilder] Uncatalogued illustration files: {string.Join(", ", unknownFiles)}");
        }
    }

    private static CardView createCardViewPrefab()
    {
        var root = createUiObject("CardView", null, new Vector2(150f, 210f));
        var background = root.AddComponent<Image>();
        background.color = new Color(0.12f, 0.13f, 0.15f, 1f);
        var button = root.AddComponent<Button>();
        button.targetGraphic = background;

        var artwork = createUiObject("Artwork", root.transform, new Vector2(134f, 130f));
        var artworkRect = (RectTransform)artwork.transform;
        artworkRect.anchorMin = new Vector2(0.5f, 1f);
        artworkRect.anchorMax = new Vector2(0.5f, 1f);
        artworkRect.pivot = new Vector2(0.5f, 1f);
        artworkRect.anchoredPosition = new Vector2(0f, -8f);
        var artworkImage = artwork.AddComponent<Image>();
        artworkImage.preserveAspect = true;
        artworkImage.raycastTarget = false;

        var definitionText = createText("DefinitionId", root.transform, new Vector2(134f, 22f), 18);
        setAnchoredPosition(definitionText.rectTransform, new Vector2(0f, -145f));
        var instanceText = createText("InstanceId", root.transform, new Vector2(134f, 18f), 14);
        setAnchoredPosition(instanceText.rectTransform, new Vector2(0f, -169f));
        var zoneText = createText("ZoneKey", root.transform, new Vector2(134f, 18f), 13);
        setAnchoredPosition(zoneText.rectTransform, new Vector2(0f, -190f));

        var cardView = root.AddComponent<CardView>();
        cardView.ConfigureForEditor(
            background,
            artworkImage,
            definitionText,
            instanceText,
            zoneText,
            button);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardViewPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<CardView>();
    }

    private static void createPlayerAreaPrefab()
    {
        var root = createUiObject("PlayerAreaView", null, new Vector2(320f, 120f));
        var image = root.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
        var summaryText = createText("Summary", root.transform, new Vector2(296f, 96f), 16);
        summaryText.alignment = TextAnchor.MiddleLeft;
        var playerAreaView = root.AddComponent<PlayerAreaView>();
        playerAreaView.ConfigureForEditor(summaryText);
        PrefabUtility.SaveAsPrefabAsset(root, PlayerAreaPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void createPublicZonePrefab(CardView cardViewPrefab)
    {
        var root = createUiObject("PublicZoneView", null, new Vector2(900f, 240f));
        var image = root.AddComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.09f, 0.9f);

        var content = createUiObject("Content", root.transform, new Vector2(876f, 216f));
        var contentRect = (RectTransform)content.transform;
        var layout = content.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var publicZoneView = root.AddComponent<PublicZoneView>();
        publicZoneView.ConfigureForEditor(contentRect, cardViewPrefab);
        PrefabUtility.SaveAsPrefabAsset(root, PublicZonePrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static InteractionOverlay createInteractionOverlayPrefab()
    {
        var root = createUiObject("InteractionOverlay", null, new Vector2(620f, 280f));
        var background = root.AddComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
        var canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var title = createText("Title", root.transform, new Vector2(580f, 42f), 24);
        setAnchoredPosition(title.rectTransform, new Vector2(0f, -28f));
        var detail = createText("Detail", root.transform, new Vector2(580f, 180f), 18);
        detail.alignment = TextAnchor.UpperLeft;
        setAnchoredPosition(detail.rectTransform, new Vector2(0f, -90f));

        var overlay = root.AddComponent<InteractionOverlay>();
        overlay.ConfigureForEditor(canvasGroup, title, detail);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, InteractionOverlayPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<InteractionOverlay>();
    }

    private static void createGameClientScene(InteractionOverlay interactionOverlayPrefab)
    {
        var previousActiveScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "GameClient";
        SceneManager.SetActiveScene(scene);

        var rootObject = new GameObject("GameClientRoot");
        var cardArtService = rootObject.AddComponent<CardArtService>();
        var projectionViewState = rootObject.AddComponent<ProjectionViewState>();
        var gameClientRoot = rootObject.AddComponent<GameClientRoot>();

        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);
        cardArtService.SetCatalogForEditor(catalog);

        var canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var overlay = PrefabUtility.InstantiatePrefab(interactionOverlayPrefab.gameObject, scene) as GameObject;
        if (overlay is null)
        {
            throw new InvalidOperationException("Failed to instantiate InteractionOverlay prefab.");
        }

        overlay.transform.SetParent(canvasObject.transform, false);
        var overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        gameClientRoot.ConfigureForEditor(
            cardArtService,
            projectionViewState,
            overlay.GetComponent<InteractionOverlay>());

        EditorSceneManager.SaveScene(scene, GameClientScenePath);
        ensureSceneInBuildSettings(GameClientScenePath);
        EditorSceneManager.CloseScene(scene, removeScene: true);
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static GameObject createUiObject(string name, Transform? parent, Vector2 size)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        var rectTransform = (RectTransform)gameObject.transform;
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = size;
        return gameObject;
    }

    private static Text createText(string name, Transform parent, Vector2 size, int fontSize)
    {
        var textObject = createUiObject(name, parent, size);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void setAnchoredPosition(RectTransform rectTransform, Vector2 position)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = position;
    }

    private static void ensureSceneInBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => !string.Equals(scene.path, scenePath, StringComparison.Ordinal)))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static void ensureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        var folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderName))
        {
            throw new InvalidOperationException($"Invalid Unity folder path: {folderPath}");
        }

        ensureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
}
