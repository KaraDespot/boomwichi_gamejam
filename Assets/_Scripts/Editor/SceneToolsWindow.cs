using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneToolsWindow : EditorWindow
{
    private const string ScenesFolder = "Assets/_Scenes";
    private const string BootstrapSceneName = "Bootstrap";
    private static readonly string[] MainSceneNames =
    {
        "Bootstrap",
        "MainMenu",
        "Loading",
        "GameScene"
    };

    private Vector2 _scrollPosition;

    [MenuItem("Tools/Scene Tools/Play From Bootstrap", priority = 0)]
    private static void PlayFromBootstrapFromMenu()
    {
        if (!OpenSceneByName(BootstrapSceneName))
            return;

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Scene Tools/Open Window %#t", priority = 10)]
    private static void OpenWindow()
    {
        SceneToolsWindow window = GetWindow<SceneToolsWindow>("Scene Tools");
        window.minSize = new Vector2(320f, 260f);
        window.Show();
    }

    [MenuItem("Tools/Scene Tools/Open Bootstrap", priority = 20)]
    private static void OpenBootstrapFromMenu() => OpenSceneByName("Bootstrap");

    [MenuItem("Tools/Scene Tools/Open MainMenu", priority = 21)]
    private static void OpenMainMenuFromMenu() => OpenSceneByName("MainMenu");

    [MenuItem("Tools/Scene Tools/Open Loading", priority = 22)]
    private static void OpenLoadingFromMenu() => OpenSceneByName("Loading");

    [MenuItem("Tools/Scene Tools/Open GameScene", priority = 23)]
    private static void OpenGameSceneFromMenu() => OpenSceneByName("GameScene");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quick Scene Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Play Bootstrap"))
            PlayFromBootstrapFromMenu();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Bootstrap"))
            OpenSceneByName("Bootstrap");
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8f);
        EditorGUILayout.LabelField("Main Scenes", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        foreach (string sceneName in MainSceneNames)
        {
            if (GUILayout.Button(sceneName))
                OpenSceneByName(sceneName);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);
        EditorGUILayout.LabelField("All Scenes In Assets/_Scenes", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        foreach (string scenePath in FindScenePaths())
        {
            DrawSceneRow(scenePath);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawSceneRow(string scenePath)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(sceneName, GUILayout.Width(150f));
        if (GUILayout.Button("Open", GUILayout.Width(60f)))
            OpenScenePath(scenePath);
        if (GUILayout.Button("Ping", GUILayout.Width(60f)))
            PingSceneAsset(scenePath);
        EditorGUILayout.EndHorizontal();
    }

    private static bool OpenSceneByName(string sceneName)
    {
        string scenePath = FindScenePathByName(sceneName);
        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog("Scene Not Found", $"Scene '{sceneName}' was not found in {ScenesFolder}.", "OK");
            return false;
        }

        return OpenScenePath(scenePath);
    }

    private static bool OpenScenePath(string scenePath)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return true;
    }

    private static void PingSceneAsset(string scenePath)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (asset != null)
            EditorGUIUtility.PingObject(asset);
    }

    private static string FindScenePathByName(string sceneName)
    {
        foreach (string scenePath in FindScenePaths())
        {
            string fileName = Path.GetFileNameWithoutExtension(scenePath);
            if (fileName == sceneName)
                return scenePath;
        }

        return null;
    }

    private static List<string> FindScenePaths()
    {
        List<string> result = new List<string>();
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                result.Add(path);
        }

        result.Sort();
        return result;
    }
}
