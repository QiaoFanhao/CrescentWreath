using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrescentWreath.Client.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrescentWreath.Client.UI.Editor
{
public sealed class EffectPresentationCatalogBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 10;

    public void OnPreprocessBuild(BuildReport report)
    {
        EffectPresentationCatalogBuilder.RebuildCatalogs();
    }
}

public static class EffectPresentationCatalogBuilder
{
    public const string CatalogAssetPath = "Assets/Resources/EffectPresentation/DefaultEffectPresentationCatalog.asset";
    public const string VfxCatalogAssetPath = "Assets/Resources/EffectPresentation/DefaultEffectVfxCatalog.asset";
    public const string SfxCatalogAssetPath = "Assets/Resources/EffectPresentation/DefaultEffectSfxCatalog.asset";

    private const string VfxRootPath = "Assets/Art/Eric VFX Studio/Game VFX - Card Effects Collection";
    private const string VfxUrpPrefabPath = VfxRootPath + "/Prefabs(URP)";
    private const string VfxBuiltInPrefabPath = VfxRootPath + "/Prefabs(Built-In)";
    private const string SfxRootPath = "Assets/Art/SE";

    [InitializeOnLoadMethod]
    private static void ensureGeneratedAssetsAfterImport()
    {
        if (File.Exists(CatalogAssetPath) && File.Exists(VfxCatalogAssetPath) && File.Exists(SfxCatalogAssetPath))
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
        RebuildCatalogs();
    }

    [MenuItem("Crescent Wreath/UI/Rebuild Effect Presentation Catalogs")]
    public static void RebuildCatalogs()
    {
        ensureFolder("Assets/Resources");
        ensureFolder("Assets/Resources/EffectPresentation");

        var vfxCatalog = loadOrCreate<EffectVfxCatalog>(VfxCatalogAssetPath);
        var sfxCatalog = loadOrCreate<EffectSfxCatalog>(SfxCatalogAssetPath);
        var presentationCatalog = loadOrCreate<EffectPresentationCatalog>(CatalogAssetPath);

        vfxCatalog.ReplaceEntriesForEditor(buildVfxEntries());
        sfxCatalog.ReplaceEntriesForEditor(buildSfxEntries());
        presentationCatalog.ReplaceForEditor(vfxCatalog, sfxCatalog, buildPresentationEntries());

        EditorUtility.SetDirty(vfxCatalog);
        EditorUtility.SetDirty(sfxCatalog);
        EditorUtility.SetDirty(presentationCatalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EffectPresentationCatalogBuilder] Rebuilt effect catalogs from {VfxRootPath} and {SfxRootPath}.");
    }

    private static List<EffectVfxCatalog.Entry> buildVfxEntries()
    {
        return new List<EffectVfxCatalog.Entry>
        {
            vfx(EffectCategoryKeys.CardActivate, "FX_CardFlash_Y", EffectVfxAttachMode.cardSurface, 0.55f, 0.22f),
            vfx(EffectCategoryKeys.Damage, "FX_CardBrushLine_P", EffectVfxAttachMode.targetCharacterArea, 0.55f, 0.32f),
            vfx(EffectCategoryKeys.DirectDamage, "FX_CardFlare_P", EffectVfxAttachMode.targetCharacterArea, 0.55f, 0.32f),
            vfx(EffectCategoryKeys.Heal, "FX_CardStar_Loop_G", EffectVfxAttachMode.targetCharacterArea, 0.75f, 0.28f),
            vfx(EffectCategoryKeys.Defense, "FX_CardRimLine_B", EffectVfxAttachMode.targetCharacterArea, 0.55f, 0.28f),
            vfx(EffectCategoryKeys.Draw, "FX_CardFlowLoop_Y", EffectVfxAttachMode.cardSurface, 0.55f, 0.24f),
            vfx(EffectCategoryKeys.GainResource, "FX_CardCubeUp_Y", EffectVfxAttachMode.tableCenterFallback, 0.65f, 0.35f),
            vfx(EffectCategoryKeys.Seal, "FX_CardRimLine_P", EffectVfxAttachMode.targetCharacterArea, 0.55f, 0.28f),
            vfx(EffectCategoryKeys.Banish, "FX_CardDissolve_P", EffectVfxAttachMode.cardSurface, 0.7f, 0.3f),
            vfx(EffectCategoryKeys.Anomaly, "FX_CardRimStar_P", EffectVfxAttachMode.tableCenterFallback, 0.8f, 0.5f),
            vfx(EffectCategoryKeys.TurnChange, "FX_CardSunLight_Loop_Y", EffectVfxAttachMode.tableCenterFallback, 0.65f, 0.32f),
            vfx(EffectCategoryKeys.GenericEffect, "FX_CardRimLine_Y", EffectVfxAttachMode.cardSurface, 0.5f, 0.22f),
        };
    }

    private static List<EffectSfxCatalog.Entry> buildSfxEntries()
    {
        var entries = new List<EffectSfxCatalog.Entry>
        {
            sfx(EffectCategoryKeys.CardActivate, 0.75f, "Magic Card Placing", "Special Card Reveal"),
            sfx(EffectCategoryKeys.Damage, 0.8f, "Magic Card Placing", "Special Card Reveal"),
            sfx(EffectCategoryKeys.DirectDamage, 0.85f, "Magic Card Placing", "Special Card Reveal"),
            sfx(EffectCategoryKeys.Heal, 0.75f, "Magic Coin Fall"),
            sfx(EffectCategoryKeys.Defense, 0.75f, "Magic Card Flip", "Magic Card Placing"),
            sfx(EffectCategoryKeys.Draw, 0.7f, "Magic Card Draw"),
            sfx(EffectCategoryKeys.GainResource, 0.75f, "Magic Coin Fall", "Magic Coin Flip"),
            sfx(EffectCategoryKeys.Seal, 0.75f, "Special Card Reveal"),
            sfx(EffectCategoryKeys.Banish, 0.75f, "Magic Card Discard"),
            sfx(EffectCategoryKeys.Anomaly, 0.8f, "Special Card Reveal"),
            sfx(EffectCategoryKeys.TurnChange, 0.7f, "Change Of Turns", "Pass Of Turn"),
            sfx(EffectCategoryKeys.GenericEffect, 0.65f, "Magic Card Deal", "Magic Card Slide", "Magic Card Placing"),
        };

        foreach (var entry in entries.Where(entry => entry.clips.Count == 0))
        {
            Debug.LogWarning($"[EffectPresentationCatalogBuilder] No SFX clips found for category {entry.effectCategoryKey} under {SfxRootPath}.");
        }

        return entries;
    }

    private static List<EffectPresentationCatalog.Entry> buildPresentationEntries()
    {
        return new List<EffectPresentationCatalog.Entry>
        {
            presentation(EffectCategoryKeys.CardActivate, "卡牌发动", 0.35f),
            presentation(EffectCategoryKeys.Damage, "伤害结算", 0.45f),
            presentation(EffectCategoryKeys.DirectDamage, "直接伤害", 0.45f),
            presentation(EffectCategoryKeys.Heal, "生命回复", 0.35f),
            presentation(EffectCategoryKeys.Defense, "防御响应", 0.35f),
            presentation(EffectCategoryKeys.Draw, "抓牌", 0.25f),
            presentation(EffectCategoryKeys.GainResource, "资源变化", 0.25f),
            presentation(EffectCategoryKeys.Seal, "封印/状态", 0.35f),
            presentation(EffectCategoryKeys.Banish, "放逐", 0.35f),
            presentation(EffectCategoryKeys.Anomaly, "异变效果", 0.45f),
            presentation(EffectCategoryKeys.TurnChange, "回合切换", 0.25f),
            presentation(EffectCategoryKeys.GenericEffect, "效果处理", 0.3f),
        };
    }

    private static EffectVfxCatalog.Entry vfx(
        string categoryKey,
        string prefabName,
        EffectVfxAttachMode attachMode,
        float duration,
        float scale)
    {
        return new EffectVfxCatalog.Entry
        {
            effectCategoryKey = categoryKey,
            prefab = loadVfxPrefab(prefabName),
            duration = duration,
            attachMode = attachMode,
            instanceAttachMode = EffectVfxInstanceAttachMode.attachLocal,
            localOffset = new Vector3(0f, 0.02f, 0f),
            localEulerAngles = Vector3.zero,
            localScale = Vector3.one * scale,
            followTarget = true,
            useBillboardToCamera = false,
            simulationSpaceMode = EffectVfxSimulationSpaceMode.local,
            autoDestroy = true,
            debugColor = Color.cyan,
            debugName = $"{categoryKey}:{prefabName}",
            stopLoopOnComplete = true,
        };
    }

    private static EffectSfxCatalog.Entry sfx(string categoryKey, float volume, params string[] namePrefixes)
    {
        return new EffectSfxCatalog.Entry
        {
            effectCategoryKey = categoryKey,
            clips = loadAudioClips(namePrefixes),
            volume = volume,
            pitchMin = 0.96f,
            pitchMax = 1.04f,
            randomizeClip = true,
        };
    }

    private static EffectPresentationCatalog.Entry presentation(string categoryKey, string label, float delay)
    {
        return new EffectPresentationCatalog.Entry
        {
            effectCategoryKey = categoryKey,
            vfxKey = categoryKey,
            sfxKey = categoryKey,
            labelText = label,
            totalDelayBeforeInteraction = delay,
        };
    }

    private static GameObject loadVfxPrefab(string prefabName)
    {
        var prefab = findPrefabByName(prefabName, VfxUrpPrefabPath) ?? findPrefabByName(prefabName, VfxBuiltInPrefabPath);
        if (prefab is null)
        {
            Debug.LogWarning($"[EffectPresentationCatalogBuilder] VFX prefab not found: {prefabName}");
        }

        return prefab;
    }

    private static GameObject findPrefabByName(string prefabName, string searchPath)
    {
        if (!Directory.Exists(searchPath))
        {
            return null;
        }

        var guids = AssetDatabase.FindAssets($"t:GameObject {prefabName}", new[] { searchPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), prefabName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    private static List<AudioClip> loadAudioClips(IEnumerable<string> namePrefixes)
    {
        if (!Directory.Exists(SfxRootPath))
        {
            Debug.LogWarning($"[EffectPresentationCatalogBuilder] SFX path is missing: {SfxRootPath}");
            return new List<AudioClip>();
        }

        var prefixes = namePrefixes.ToArray();
        return AssetDatabase
            .FindAssets("t:AudioClip", new[] { SfxRootPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => prefixes.Any(prefix => Path.GetFileNameWithoutExtension(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
            .Where(clip => clip != null)
            .ToList();
    }

    private static T loadOrCreate<T>(string assetPath)
        where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void ensureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
        {
            throw new InvalidOperationException($"Invalid Unity folder path: {path}");
        }

        ensureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
}
