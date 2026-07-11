using UnityEditor;
using UnityEngine;

public static class CheckpointSaveDevTools
{
    [MenuItem("Tools/Save Tools/Clear Checkpoint Saves", priority = 200)]
    private static void ClearCheckpointSaves()
    {
        Debug.Log("Checkpoint saves were removed from the MVP scope.");
    }
}
