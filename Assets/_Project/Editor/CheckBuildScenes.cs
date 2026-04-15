using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CheckBuildScenes
{
    static readonly string[] Required = { "MainMenu", "CombatStage", "EventDeck" };
    static readonly string[] Deprecated = { "RewardScene", "MapSelector" };

    [MenuItem("RPG/Check → Build Scenes")]
    public static void Execute()
    {
        var scenes = EditorBuildSettings.scenes;
        var list = new List<EditorBuildSettingsScene>(scenes);

        // Remove deprecated scenes
        list.RemoveAll(s =>
        {
            foreach (var dep in Deprecated)
                if (s.path.Contains(dep)) { Debug.Log($"[CheckBuildScenes] Removed deprecated '{dep}' from Build Settings."); return true; }
            return false;
        });

        // Add missing required scenes
        foreach (var name in Required)
        {
            string path = $"Assets/_Project/Scenes/{name}.unity";
            bool found = false;
            foreach (var s in list)
                if (s.path == path) { found = true; break; }

            if (!found)
            {
                list.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"[CheckBuildScenes] Added '{name}' to Build Settings.");
            }
            else
            {
                Debug.Log($"[CheckBuildScenes] '{name}' already present.");
            }
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[CheckBuildScenes] Build Settings updated.");
    }
}
