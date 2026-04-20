#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RPG.Data;

namespace RPG.Editor
{
    /// <summary>
    /// Moves all SectorEventSO assets into Assets/_Project/Resources/SectorEvents/
    /// so AreaMapUI.LoadEventCache (which uses Resources.LoadAll) can find them.
    /// Also re-wires the AreaMapConfig pools to the new paths.
    /// </summary>
    public static class MoveSectorEventsToResources
    {
        private const string SrcDir = "Assets/_Project/ScriptableObjects/SectorEvents";
        private const string DstDir = "Assets/_Project/Resources/SectorEvents";

        [MenuItem("RPG/Fix/Move SectorEvents to Resources")]
        public static void Move()
        {
            // Ensure destination folder
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            if (!AssetDatabase.IsValidFolder(DstDir))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "SectorEvents");

            var guids = AssetDatabase.FindAssets("t:SectorEventSO", new[] { SrcDir });
            int moved = 0;
            foreach (var guid in guids)
            {
                string src  = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileName(src);
                string dst  = $"{DstDir}/{name}";

                if (AssetDatabase.LoadAssetAtPath<SectorEventSO>(dst) != null)
                    continue; // already there

                string err = AssetDatabase.MoveAsset(src, dst);
                if (string.IsNullOrEmpty(err))
                    moved++;
                else
                    Debug.LogWarning($"[MoveSectorEvents] Failed to move {src}: {err}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-wire the AreaMapConfig pools so they point to new locations
            RewireConfigs();

            Debug.Log($"[MoveSectorEvents] Moved {moved} SectorEventSO assets to {DstDir}.");
        }

        private static void RewireConfigs()
        {
            string mapDir = "Assets/_Project/ScriptableObjects/MapConfig";
            string[] configPaths = {
                $"{mapDir}/AreaMapConfig_Hostile.asset",
                $"{mapDir}/AreaMapConfig_Safe.asset",
                $"{mapDir}/AreaMapConfig_Random.asset",
            };

            var guids = AssetDatabase.FindAssets("t:SectorEventSO", new[] { DstDir });
            var combat   = new System.Collections.Generic.List<SectorEventSO>();
            var rest     = new System.Collections.Generic.List<SectorEventSO>();
            var treasure = new System.Collections.Generic.List<SectorEventSO>();
            var dilemma  = new System.Collections.Generic.List<SectorEventSO>();

            foreach (var guid in guids)
            {
                var ev = AssetDatabase.LoadAssetAtPath<SectorEventSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (ev == null) continue;
                switch (ev.Category)
                {
                    case SectorEventCategory.Combat:   combat.Add(ev);   break;
                    case SectorEventCategory.Rest:     rest.Add(ev);     break;
                    case SectorEventCategory.Treasure: treasure.Add(ev); break;
                    case SectorEventCategory.Dilemma:  dilemma.Add(ev);  break;
                }
            }

            foreach (var path in configPaths)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<AreaMapConfigSO>(path);
                if (cfg == null) continue;
                cfg.CombatEvents   = combat.ToArray();
                cfg.RestEvents     = rest.ToArray();
                cfg.HealEvents     = rest.ToArray();
                cfg.TreasureEvents = treasure.ToArray();
                cfg.DilemmaEvents  = dilemma.ToArray();
                EditorUtility.SetDirty(cfg);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MoveSectorEvents] Rewired configs: {combat.Count} combat, {rest.Count} rest, {treasure.Count} treasure, {dilemma.Count} dilemma.");
        }
    }
}
#endif
