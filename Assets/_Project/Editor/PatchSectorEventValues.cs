#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RPG.Data;

namespace RPG.Editor
{
    /// <summary>
    /// Patches existing SectorEventSO assets in Resources/SectorEvents with
    /// better-tuned values: Rest events grant rations, Treasure events are
    /// more rewarding, Combat victory grants rations too.
    /// Run once after MoveSectorEventsToResources has been executed.
    /// Menu: RPG/Fix/Patch Sector Event Values
    /// </summary>
    public static class PatchSectorEventValues
    {
        private const string Dir = "Assets/_Project/Resources/SectorEvents";

        [MenuItem("RPG/Fix/Patch Sector Event Values")]
        public static void Patch()
        {
            int patched = 0;

            var guids = AssetDatabase.FindAssets("t:SectorEventSO", new[] { Dir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ev   = AssetDatabase.LoadAssetAtPath<SectorEventSO>(path);
                if (ev == null) continue;

                bool dirty = false;

                switch (ev.Category)
                {
                    case SectorEventCategory.Rest:
                        // Rest tiles should always grant rations — they ARE the restock mechanic
                        if (ev.InstantGrant.Rations < 2)
                        {
                            ev.InstantGrant = new ResourceDelta
                            {
                                Rations = 3,
                                Morale  = ev.InstantGrant.Morale > 0 ? ev.InstantGrant.Morale : 1,
                                Gold    = ev.InstantGrant.Gold,
                            };
                            // Shrine Blessing heals more and gives extra rations
                            if (ev.HealFraction >= 0.35f)
                                ev.InstantGrant = new ResourceDelta { Rations = 4, Morale = 2 };
                            dirty = true;
                        }
                        break;

                    case SectorEventCategory.Treasure:
                        // Treasure caches should include rations to reward exploration
                        if (ev.InstantGrant.Rations < 2)
                        {
                            ev.InstantGrant = new ResourceDelta
                            {
                                Gold    = Mathf.Max(ev.InstantGrant.Gold, 15),
                                Rations = 2,
                                Scrap   = ev.InstantGrant.Scrap,
                            };
                            dirty = true;
                        }
                        break;

                    case SectorEventCategory.Combat:
                        // Combat victory should reward at least 1 ration (looting bodies)
                        if (ev.VictoryGrant.Rations < 1)
                        {
                            ev.VictoryGrant = new ResourceDelta
                            {
                                Gold    = ev.VictoryGrant.Gold,
                                Scrap   = ev.VictoryGrant.Scrap,
                                Rations = 1,
                                Morale  = ev.VictoryGrant.Morale,
                            };
                            dirty = true;
                        }
                        break;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(ev);
                    patched++;
                    Debug.Log($"[PatchSectorEventValues] Patched: {ev.name} ({ev.Category})");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PatchSectorEventValues] Done. {patched} assets patched.");
        }
    }
}
#endif
