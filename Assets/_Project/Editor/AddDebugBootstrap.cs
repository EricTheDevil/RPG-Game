#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.Core;
using RPG.Data;

namespace RPG.Editor
{
    public static class AddDebugBootstrap
    {
        [MenuItem("RPG/Preview/Add WorldMap Debug Bootstrap")]
        public static void AddBootstrap()
        {
            // Make sure WorldMap scene is open
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Contains("WorldMap"))
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/WorldMap.unity");
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            }

            // Remove old one if present
            var existing = Object.FindFirstObjectByType<WorldMapDebugBootstrap>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Add fresh
            var go     = new GameObject("WorldMapDebugBootstrap");
            var boot   = go.AddComponent<WorldMapDebugBootstrap>();
            var config = AssetDatabase.LoadAssetAtPath<WorldMapConfigSO>(
                "Assets/_Project/ScriptableObjects/MapConfig/WorldMapConfig.asset");
            boot.WorldMapConfig = config;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[AddDebugBootstrap] Bootstrap added and scene saved.");
        }
    }
}
#endif
