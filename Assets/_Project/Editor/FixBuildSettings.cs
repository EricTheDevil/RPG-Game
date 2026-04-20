#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RPG.Editor
{
    public static class FixBuildSettings
    {
        [MenuItem("RPG/Fix Build Settings")]
        public static void Fix()
        {
            var required = new[]
            {
                "Assets/_Project/Scenes/MainMenu.unity",
                "Assets/_Project/Scenes/ClassSelect.unity",
                "Assets/_Project/Scenes/WorldMap.unity",
                "Assets/_Project/Scenes/AreaMap.unity",
                "Assets/_Project/Scenes/CombatStage.unity",
            };

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;

            foreach (var path in required)
            {
                bool found = false;
                foreach (var s in existing) if (s.path == path) { found = true; break; }
                if (!found && System.IO.File.Exists(path))
                {
                    existing.Add(new EditorBuildSettingsScene(path, true));
                    Debug.Log($"[FixBuildSettings] Added: {path}");
                    changed = true;
                }
            }

            if (changed) EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log($"[FixBuildSettings] Done. Total scenes: {existing.Count}");
        }
    }
}
#endif
