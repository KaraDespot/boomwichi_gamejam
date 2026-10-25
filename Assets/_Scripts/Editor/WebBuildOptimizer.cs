#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>Repeatable WebGL settings and conservative, WebGL-only texture import overrides.</summary>
public static class WebBuildOptimizer
{
    private const string ReportRoot = "Reports/Publishing/Optimization";
    [Serializable] private class Change { public string path; public string before; public string after; }
    [Serializable] private class Changes { public string settingsBefore; public string settingsAfter; public Change[] textures; }

    public static string Settings()
    {
        var target = NamedBuildTarget.WebGL;
        return $"compression={PlayerSettings.WebGL.compressionFormat}; fallback={PlayerSettings.WebGL.decompressionFallback}; " +
            $"stripEngine={PlayerSettings.stripEngineCode}; stripping={PlayerSettings.GetManagedStrippingLevel(target)}; " +
            $"codeGeneration={PlayerSettings.GetIl2CppCodeGeneration(target)}; compiler={PlayerSettings.GetIl2CppCompilerConfiguration(target)}; " +
            $"codeOptimization={UnityEditor.WebGL.UserBuildSettings.codeOptimization}; exceptions={PlayerSettings.WebGL.exceptionSupport}; " +
            $"api={PlayerSettings.GetApiCompatibilityLevel(target)}; colorSpace={PlayerSettings.colorSpace}; spritePacker={EditorSettings.spritePackerMode}";
    }

    public static string[] IncludedAssetPaths()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);
        // Resources.Load uses string paths, so include those dependencies in the audit too.
        var resources = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
            p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p));
        return AssetDatabase.GetDependencies(scenes.Concat(resources).ToArray(), true)
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)).Distinct().OrderBy(p => p).ToArray();
    }

    [MenuItem("Tools/Publishing/Apply WebGL Size Optimization")]
    public static void Apply()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL || EditorApplication.isPlaying)
            throw new InvalidOperationException("Run while stopped on the WebGL build target.");
        Directory.CreateDirectory(ReportRoot);
        string changesPath = ReportRoot + "/applied-changes.json";
        var prior = File.Exists(changesPath) ? JsonUtility.FromJson<Changes>(File.ReadAllText(changesPath)) : null;
        string before = prior?.settingsBefore ?? Settings();
        ApplyPlayerSettings();
        var changes = prior?.textures?.ToList() ?? new List<Change>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in IncludedAssetPaths())
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (path.StartsWith("Assets/_Art/2D/", StringComparison.Ordinal) && importer.textureType == TextureImporterType.Sprite)
                {
                    var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (source.format != TextureFormat.DXT1 && source.format != TextureFormat.DXT5 &&
                        source.format != TextureFormat.DXT1Crunched && source.format != TextureFormat.DXT5Crunched) continue;
                    // Atlas compression must start from source pixels, not a second lossy DXT generation.
                    BackupMeta(path);
                    var spriteSettings = importer.GetPlatformTextureSettings("WebGL");
                    string oldSettings = JsonUtility.ToJson(spriteSettings);
                    spriteSettings.name = "WebGL";
                    spriteSettings.overridden = true;
                    spriteSettings.format = TextureImporterFormat.RGBA32;
                    spriteSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    spriteSettings.crunchedCompression = false;
                    spriteSettings.maxTextureSize = Math.Min(spriteSettings.maxTextureSize, importer.maxTextureSize);
                    importer.SetPlatformTextureSettings(spriteSettings);
                    importer.SaveAndReimport();
                    Record(changes, path, oldSettings, JsonUtility.ToJson(spriteSettings));
                    continue;
                }
                // Keep sprite resolution and aspect ratio. SpriteAtlas handles their NPOT compression.
                if (importer.textureType != TextureImporterType.Default && importer.textureType != TextureImporterType.NormalMap) continue;
                if (!path.StartsWith("Assets/_Art/", StringComparison.Ordinal) || importer.isReadable) continue;
                var format = importer.GetAutomaticFormat("WebGL");
                if (format != TextureImporterFormat.DXT1 && format != TextureImporterFormat.DXT5 &&
                    format != TextureImporterFormat.DXT1Crunched && format != TextureImporterFormat.DXT5Crunched) continue;
                var settings = importer.GetPlatformTextureSettings("WebGL");
                if (settings.overridden && settings.crunchedCompression && settings.compressionQuality == 90 && settings.maxTextureSize <= 1024) continue;
                BackupMeta(path);
                string previous = JsonUtility.ToJson(settings);
                settings.name = "WebGL";
                settings.overridden = true;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.crunchedCompression = true;
                settings.compressionQuality = 90;
                // WebGL material maps are capped independently of source art and other platforms.
                settings.maxTextureSize = Math.Min(1024, Math.Min(settings.maxTextureSize, importer.maxTextureSize));
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                Record(changes, path, previous, JsonUtility.ToJson(settings));
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        var report = new Changes { settingsBefore = before, settingsAfter = Settings(), textures = changes.ToArray() };
        File.WriteAllText(changesPath, JsonUtility.ToJson(report, true));
        Debug.Log($"[WebOptimization] WebGL settings saved; import overrides recorded: {changes.Count}. {Settings()}");
    }

    public static void ApplyPlayerSettings()
    {
        var target = NamedBuildTarget.WebGL;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        // Keep the existing fallback until the production host's Content-Encoding is verified.
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Master);
        // Save parsing and SDK calls handle explicitly thrown errors; None halts WebGL on any throw.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        AssetDatabase.SaveAssets();
    }

    private static void BackupMeta(string assetPath)
    {
        string destination = ReportRoot + "/RestorePoint/" + assetPath + ".meta";
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if (!File.Exists(destination)) File.Copy(assetPath + ".meta", destination);
    }

    private static void Record(List<Change> changes, string path, string before, string after)
    {
        var existing = changes.Find(c => c.path == path);
        if (existing != null) existing.after = after;
        else changes.Add(new Change { path = path, before = before, after = after });
    }
}
#endif
