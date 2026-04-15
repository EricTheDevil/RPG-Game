using UnityEditor;
using UnityEngine;

public class RPGBuildSettings
{
    [MenuItem("RPG/Update Build Settings")]
    public static void Execute()
    {
        string[] scenePaths = new[]
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/MapSelector.unity",
            "Assets/_Project/Scenes/CombatStage.unity",
            "Assets/_Project/Scenes/RewardScene.unity",
        };

        var scenes = new EditorBuildSettingsScene[scenePaths.Length];
        for (int i = 0; i < scenePaths.Length; i++)
            scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[RPGBuildSettings] ✅ Build settings updated with 4 scenes.");
    }
}
