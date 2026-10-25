// [UNITY-SKILL:SPRITEATLAS]
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>Built-in V2 atlases for referenced project sprites; no remote loading or extra source art.</summary>
public sealed class WebSpriteAtlasPrebuild : IPreprocessBuildWithReport
{
    private const string Folder = "Assets/_Generated/WebSprites";
    private const string Marker = "PublishingWebAtlas-v1:";
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL) return;
        WebBuildOptimizer.ApplyPlayerSettings();
        EnsureAtlases();
    }

    public static void EnsureAtlases()
    {
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        EnsureFolder("Assets/_Generated");
        EnsureFolder(Folder);
        var sources = WebBuildOptimizer.IncludedAssetPaths()
            .Where(p => p.StartsWith("Assets/_Art/2D/", StringComparison.Ordinal) &&
                AssetImporter.GetAtPath(p) is TextureImporter i && i.textureType == TextureImporterType.Sprite)
            .GroupBy(p => p.Contains("/Characters/") ? "Characters" :
                !((TextureImporter)AssetImporter.GetAtPath(p)).DoesSourceTextureHaveAlpha() ? "OpaqueUI" : "UI").ToArray();
        foreach (var group in sources)
        {
            var paths = group.OrderBy(p => p).ToArray();
            string atlasPath = Folder + "/" + group.Key + ".spriteatlasv2";
            string signature = Marker + string.Join("|", paths.Select(AssetDatabase.AssetPathToGUID));
            if (AssetImporter.GetAtPath(atlasPath) is SpriteAtlasImporter existing && existing.userData == signature && existing.includeInBuild) continue;
            // The generator owns only these specific assets, so never replace an unrelated atlas.
            if (File.Exists(atlasPath) && (AssetImporter.GetAtPath(atlasPath) is not SpriteAtlasImporter owned || !owned.userData.StartsWith(Marker, StringComparison.Ordinal)))
                throw new InvalidOperationException("Refusing to overwrite an atlas not owned by this generator: " + atlasPath);
            var sprites = paths.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Sprite>().Cast<UnityEngine.Object>().ToArray();
            var atlas = new SpriteAtlasAsset();
            atlas.Add(sprites);
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
            importer.includeInBuild = true;
            var texture = importer.textureSettings;
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.filterMode = FilterMode.Bilinear;
            importer.textureSettings = texture;
            var packing = importer.packingSettings;
            packing.padding = 4;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.enableAlphaDilation = true;
            importer.packingSettings = packing;
            var settings = importer.GetPlatformSettings("WebGL");
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = 4096;
            settings.format = group.Key == "OpaqueUI" ? TextureImporterFormat.DXT1Crunched : TextureImporterFormat.DXT5Crunched;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.crunchedCompression = true;
            settings.compressionQuality = 90;
            importer.SetPlatformSettings(settings);
            importer.userData = signature;
            importer.SaveAndReimport();
            Debug.Log($"[WebOptimization] Built-in atlas {group.Key}: {sprites.Length} sprites; {settings.format}; source resolution preserved.");
        }
        // Keep generated assets recoverable, but don't ship groups that no scene/resource references anymore.
        foreach (string path in Directory.GetFiles(Folder, "*.spriteatlasv2"))
        {
            string normalized = path.Replace('\\', '/');
            if (sources.Any(g => normalized == Folder + "/" + g.Key + ".spriteatlasv2")) continue;
            if (AssetImporter.GetAtPath(normalized) is SpriteAtlasImporter unused && unused.userData.StartsWith(Marker, StringComparison.Ordinal) && unused.includeInBuild)
            {
                unused.includeInBuild = false;
                unused.SaveAndReimport();
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
#endif
