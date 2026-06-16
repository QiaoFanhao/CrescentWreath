using System;
using System.IO;
using System.Linq;
using CrescentWreath.Client.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrescentWreath.Client.UI.Editor
{
public static class BattlefieldSceneSkeletonBuilder
{
    private const string ScenePath = "Assets/Scenes/GameClient.unity";
    private const string ThemeFolder = "Assets/Resources/UI";
    private const string ThemePath = ThemeFolder + "/BattlefieldUiTheme.asset";

    [MenuItem("Crescent Wreath/UI/Rebuild 3D Battlefield Skeleton")]
    public static void RebuildBattlefieldSkeleton()
    {
        ensureFolder("Assets/Resources", "UI");
        _ = loadOrCreateTheme();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = findInScene<GameClientRoot>(scene);
        if (root is null)
        {
            throw new InvalidOperationException("GameClientRoot is missing from the GameClient scene.");
        }

        removeFormalSkeleton(root.transform);
        var existingLayout = root.GetComponent<BattlefieldLayoutRoot>();
        if (existingLayout is not null)
        {
            UnityEngine.Object.DestroyImmediate(existingLayout);
        }

        var layout = BattlefieldSceneSkeletonFactory.Ensure(root);
        layout.SetDebugVisible(false);
        layout.SetOverlayPreviewVisible(false);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Crescent Wreath/UI/Capture 3D Battlefield Skeleton")]
    public static void CaptureBattlefieldScreenshots()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = findInScene<GameClientRoot>(scene);
        if (root is null)
        {
            throw new InvalidOperationException("GameClientRoot is missing from the GameClient scene.");
        }

        var layout = BattlefieldSceneSkeletonFactory.Ensure(root);
        var camera = layout.TableWorld?.MainCamera;
        if (camera is null)
        {
            throw new InvalidOperationException("The 3D battlefield Main Camera is missing.");
        }

        var outputFolder = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../../../Temp/BattlefieldCaptures"));
        Directory.CreateDirectory(outputFolder);
        capture(camera, 1920, 1080, Path.Combine(outputFolder, "Battlefield-1920x1080.png"));
        capture(camera, 1280, 720, Path.Combine(outputFolder, "Battlefield-1280x720.png"));
        Debug.Log($"Battlefield screenshots saved to {outputFolder}");
    }

    public static void RebuildAndCaptureBattlefieldSkeleton()
    {
        RebuildBattlefieldSkeleton();
        CaptureBattlefieldScreenshots();
    }

    private static BattlefieldUiTheme loadOrCreateTheme()
    {
        var theme = AssetDatabase.LoadAssetAtPath<BattlefieldUiTheme>(ThemePath);
        if (theme is null)
        {
            theme = ScriptableObject.CreateInstance<BattlefieldUiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        var sourceHanSans = SourceHanSansTmpFontBuilder.EnsureFontAsset();
        theme.ConfigureForEditor(
            sourceHanSans ?? TMP_Settings.defaultFontAsset,
            sourceHanSans is null
                ? "TODO: SourceHanSans - generation failed; using temporary TMP default fallback."
                : "SourceHanSansSC-Regular Dynamic SDF is active.");
        EditorUtility.SetDirty(theme);
        return theme;
    }

    private static void removeFormalSkeleton(Transform root)
    {
        foreach (var name in new[]
                 {
                     "SceneRig",
                     "TableWorld",
                     "ScreenHudCanvas",
                     "OverlayCanvas",
                     "DebugCanvas",
                 })
        {
            var child = root.Find(name);
            if (child is not null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void capture(Camera camera, int width, int height, string outputPath)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static T? findInScene<T>(Scene scene)
        where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(item => item.gameObject.scene == scene);
    }

    private static void ensureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
}
