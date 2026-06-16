using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CrescentWreath.Client.UI.Editor
{
[InitializeOnLoad]
public static class SourceHanSansTmpFontBuilder
{
    public const string SourceFontPath =
        "Assets/Art/Font/09_SourceHanSansSC/OTF/SimplifiedChinese/SourceHanSansSC-Regular.otf";

    public const string FontAssetFolder = "Assets/Resources/UI/Fonts";
    public const string FontAssetPath = FontAssetFolder + "/SourceHanSansSC-Regular SDF.asset";

    private const string BuilderVersion = "SourceHanSansTmpFontBuilder:v1";

    private const string ChineseSample =
        "当前回合行动阶段召唤阶段结束回合魔力技能点灵符灵脉响应窗口等待其他玩家选择"
        + "公共宝具区异变区角色头像生命值手牌牌库弃牌阵地区结界封印禁锢沉默魅惑穿透";

    static SourceHanSansTmpFontBuilder()
    {
        EditorApplication.delayCall += ensureAfterDomainReload;
    }

    [MenuItem("Crescent Wreath/UI/Rebuild Source Han Sans TMP Font")]
    public static TMP_FontAsset? RebuildFontAsset()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont is null)
        {
            Debug.LogWarning(
                $"Source Han Sans font is missing: {SourceFontPath}. "
                + "The battlefield UI will keep using the temporary TMP fallback.");
            return null;
        }

        ensureFolder("Assets/Resources/UI", "Fonts");
        AssetDatabase.DeleteAsset(FontAssetPath);

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);
        if (fontAsset is null)
        {
            Debug.LogError($"Failed to create TMP font asset from {SourceFontPath}.");
            return null;
        }

        fontAsset.name = "SourceHanSansSC-Regular SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        var atlasTextures = fontAsset.atlasTextures;
        var material = fontAsset.material;
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        foreach (var atlasTexture in atlasTextures)
        {
            if (atlasTexture is null || AssetDatabase.Contains(atlasTexture))
            {
                continue;
            }

            atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }

        if (material is not null && !AssetDatabase.Contains(material))
        {
            material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        if (!fontAsset.TryAddCharacters(ChineseSample, out var missingCharacters))
        {
            Debug.LogWarning(
                $"Source Han Sans TMP prewarm missed characters: {missingCharacters}");
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(FontAssetPath);
        if (importer is not null)
        {
            importer.userData = BuilderVersion;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    public static TMP_FontAsset? EnsureFontAsset()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset is not null &&
            fontAsset.sourceFontFile is not null &&
            fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic &&
            AssetImporter.GetAtPath(FontAssetPath)?.userData == BuilderVersion)
        {
            return fontAsset;
        }

        return RebuildFontAsset();
    }

    private static void ensureAfterDomainReload()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ensureAfterDomainReload;
            return;
        }

        var fontAsset = EnsureFontAsset();
        if (fontAsset is null)
        {
            return;
        }

        var theme = AssetDatabase.LoadAssetAtPath<BattlefieldUiTheme>(
            "Assets/Resources/UI/BattlefieldUiTheme.asset");
        if (theme is null)
        {
            return;
        }

        theme.ConfigureForEditor(
            fontAsset,
            "SourceHanSansSC-Regular Dynamic SDF is active.");
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
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
