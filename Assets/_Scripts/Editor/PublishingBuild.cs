#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Builds the actual profile, preserving WebGL texture subtarget.</summary>
public static class PublishingBuild
{
    [Serializable] private class AssetSize { public string asset; public long bytes; }
    [Serializable] private class Report
    {
        public string status;
        public string result;
        public string profile;
        public string subtarget;
        public string template;
        public bool development;
        public long totalSizeBytes;
        public double buildTimeMs;
        public int totalWarnings;
        public int totalErrors;
        public string[] scenes;
        public AssetSize[] largestAssets;
        public string[] warnings;
    }

    public static void Queue(string outputPath, string reportPath, bool development)
    {
        var profile = BuildProfile.GetActiveBuildProfile();
        if (profile == null) throw new InvalidOperationException("Select the WebGL profile first.");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, "{\"status\":\"queued\"}");
        EditorApplication.CallbackFunction callback = null;
        callback = () =>
        {
            EditorApplication.update -= callback;
            try
            {
                string subtarget = EditorUserBuildSettings.webGLBuildSubtarget.ToString();
                string template = PlayerSettings.WebGL.template;
                File.WriteAllText(reportPath, "{\"status\":\"building\"}");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = outputPath,
                    options = BuildOptions.DetailedBuildReport | (development ? BuildOptions.Development : BuildOptions.None)
                });
                var summary = report.summary;
                var data = new Report
                {
                    status = "completed", result = summary.result.ToString(),
                    profile = AssetDatabase.GetAssetPath(profile), subtarget = subtarget,
                    template = template, development = development,
                    totalSizeBytes = (long)summary.totalSize, buildTimeMs = summary.totalTime.TotalMilliseconds,
                    totalWarnings = (int)summary.totalWarnings, totalErrors = (int)summary.totalErrors,
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    largestAssets = report.packedAssets.SelectMany(p => p.contents)
                        .GroupBy(c => c.sourceAssetPath).Select(g => new AssetSize { asset = g.Key, bytes = g.Sum(c => (long)c.packedSize) })
                        .OrderByDescending(a => a.bytes).Take(30).ToArray(),
                    warnings = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Warning || m.type == LogType.Error || m.type == LogType.Exception)
                        .Select(m => m.content).ToArray()
                };
                File.WriteAllText(reportPath, JsonUtility.ToJson(data, true));
                if (File.Exists("Library/LastBuild.buildreport")) File.Copy("Library/LastBuild.buildreport", reportPath + ".buildreport", true);
            }
            catch (Exception error)
            {
                File.WriteAllText(reportPath, "{\"status\":\"failed\",\"result\":" + JsonUtility.ToJson(new Failure { message = error.ToString() }) + "}");
                Debug.LogException(error);
            }
        };
        EditorApplication.update += callback;
    }
    [Serializable] private class Failure { public string message; }
}
#endif
