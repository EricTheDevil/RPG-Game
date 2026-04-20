using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.Core;

/// <summary>
/// Assigns the GameManager prefab to the SceneBootstrap component in every scene that has one.
/// </summary>
public class WireSceneBootstraps
{
    public static void Execute()
    {
        var gmPrefab = AssetDatabase.LoadAssetAtPath<GameManager>("Assets/_Project/Prefabs/GameManager.prefab");
        if (gmPrefab == null) { Debug.LogError("[WireSceneBootstraps] GameManager prefab not found."); return; }

        string[] scenePaths = new[]
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/EventDeck.unity",
            "Assets/_Project/Scenes/CombatStage.unity",
        };

        foreach (var scenePath in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;

            foreach (var go in scene.GetRootGameObjects())
            {
                var bootstrap = go.GetComponentInChildren<SceneBootstrap>(true);
                if (bootstrap == null) continue;

                // Use SerializedObject to set the private-serialized field
                var so = new SerializedObject(bootstrap);
                var prop = so.FindProperty("GameManagerPrefab");
                if (prop == null) prop = so.FindProperty("gameManagerPrefab");
                if (prop == null) { Debug.LogWarning($"[WireSceneBootstraps] Could not find GameManagerPrefab field on SceneBootstrap in {scenePath}"); continue; }

                prop.objectReferenceValue = gmPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
                Debug.Log($"[WireSceneBootstraps] Wired GameManager prefab in {scenePath} on '{go.name}'");
            }

            if (dirty)
                EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("[WireSceneBootstraps] Done.");
    }
}
