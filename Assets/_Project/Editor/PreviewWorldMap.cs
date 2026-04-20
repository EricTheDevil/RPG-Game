#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.Core;
using RPG.Data;
using RPG.Map;

namespace RPG.Editor
{
    /// <summary>
    /// Plays directly into the WorldMap scene with a generated map.
    /// Menu: RPG > Preview > WorldMap
    /// </summary>
    public static class PreviewWorldMap
    {
        [MenuItem("RPG/Preview/WorldMap")]
        public static void Preview()
        {
            // Open the WorldMap scene and enter play mode
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/WorldMap.unity");

            // Hook into play mode to inject session data as soon as the scene starts
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;

            // Load WorldMapConfig and generate a map
            var config = AssetDatabase.LoadAssetAtPath<WorldMapConfigSO>(
                "Assets/_Project/ScriptableObjects/MapConfig/WorldMapConfig.asset");
            if (config == null) { Debug.LogError("[PreviewWorldMap] WorldMapConfig not found."); return; }

            var session = GameSession.Instance;
            if (session == null) { Debug.LogError("[PreviewWorldMap] GameSession not found at runtime."); return; }

            session.WorldMap          = WorldMapGenerator.Generate(config);
            session.CurrentLayerIndex = 0;

            // Also set up resources so the bar shows something real
            session.ApplyResources(new ResourceDelta { Gold = 50, Scrap = 10, Rations = 5, Morale = 7 });

            // Find WorldMapUI and force a refresh
            var mapUI = Object.FindFirstObjectByType<RPG.UI.WorldMapUI>();
            if (mapUI != null)
            {
                // Disable/enable to trigger Start() again with the now-populated session
                mapUI.enabled = false;
                mapUI.enabled = true;
            }

            Debug.Log($"[PreviewWorldMap] Map injected: {session.WorldMap.Layers.Count} layers.");
        }
    }
}
#endif
