#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RPG.Data;
using RPG.Core;

namespace RPG.Editor
{
    /// <summary>
    /// Wires the complete gameplay loop end-to-end:
    ///   1. WorldMapConfig → GameManager.WorldMapConfig
    ///   2. Creates essential SectorEventSO assets (Combat × 3, Rest × 2, Treasure × 2, Dilemma × 3)
    ///   3. Populates all 3 AreaMapConfig event pools from those assets
    ///
    /// Menu: RPG > Wire Full Game Loop
    /// </summary>
    public static class WireFullGameLoop
    {
        private const string EventDir = "Assets/_Project/ScriptableObjects/SectorEvents";
        private const string MapDir   = "Assets/_Project/ScriptableObjects/MapConfig";

        [MenuItem("RPG/Wire Full Game Loop")]
        public static void Execute()
        {
            EnsureFolder(EventDir);

            // ── 1. Wire WorldMapConfig into GameManager ───────────────────────
            WireWorldMapConfig();

            // ── 2. Create SectorEventSO assets ───────────────────────────────
            var combatEvents   = CreateCombatEvents();
            var restEvents     = CreateRestEvents();
            var treasureEvents = CreateTreasureEvents();
            var dilemmaEvents  = CreateDilemmaEvents();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 3. Wire pools into AreaMapConfigs ─────────────────────────────
            WireAreaConfigs(combatEvents, restEvents, treasureEvents, dilemmaEvents);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WireFullGameLoop] Complete. WorldMap, SectorEvents, and AreaMapConfigs are all wired.");
        }

        // ── 1. WorldMapConfig ─────────────────────────────────────────────────

        private static void WireWorldMapConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<WorldMapConfigSO>($"{MapDir}/WorldMapConfig.asset");
            if (config == null)
            {
                Debug.LogError("[WireFullGameLoop] WorldMapConfig.asset not found. Run RPG > Map > Create Config Assets first.");
                return;
            }

            string prefabPath = "Assets/_Project/Resources/GameManager.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var gm   = root.GetComponent<GameManager>();
            if (gm == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[WireFullGameLoop] GameManager component not found on prefab.");
                return;
            }

            gm.WorldMapConfig = config;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[WireFullGameLoop] WorldMapConfig wired into GameManager.");
        }

        // ── 2. SectorEventSO creation ─────────────────────────────────────────

        private static SectorEventSO[] CreateCombatEvents()
        {
            var list = new List<SectorEventSO>();

            list.Add(GetOrCreateEvent("SectorEvent_Combat_Skirmish", ev =>
            {
                ev.EventName          = "Skirmish";
                ev.Prompt             = "A patrol of bandits blocks the road.";
                ev.Category           = SectorEventCategory.Combat;
                ev.Weight             = 100;
                ev.DifficultyScale    = 1.0f;
                ev.CombatClassXPReward = 50;
                ev.VictoryGrant       = new ResourceDelta { Gold = 10, Rations = 1 };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Combat_Ambush", ev =>
            {
                ev.EventName          = "Ambush";
                ev.Prompt             = "Enemies spring from the shadows!";
                ev.Category           = SectorEventCategory.Combat;
                ev.Weight             = 80;
                ev.DifficultyScale    = 1.2f;
                ev.CombatClassXPReward = 65;
                ev.VictoryGrant       = new ResourceDelta { Gold = 15, Scrap = 5 };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Combat_Warband", ev =>
            {
                ev.EventName          = "Warband";
                ev.Prompt             = "A full warband stands between you and the path ahead.";
                ev.Category           = SectorEventCategory.Combat;
                ev.Weight             = 60;
                ev.DifficultyScale    = 1.5f;
                ev.CombatClassXPReward = 80;
                ev.VictoryGrant       = new ResourceDelta { Gold = 20, Scrap = 10 };
            }));

            return list.ToArray();
        }

        private static SectorEventSO[] CreateRestEvents()
        {
            var list = new List<SectorEventSO>();

            list.Add(GetOrCreateEvent("SectorEvent_Rest_Campfire", ev =>
            {
                ev.EventName    = "Campfire";
                ev.Prompt       = "A warm fire. Rest a while and recover.";
                ev.Category     = SectorEventCategory.Rest;
                ev.Weight       = 100;
                ev.HealFraction = 0.25f;
                ev.InstantGrant = new ResourceDelta { Morale = 1 };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Rest_Shrine_Blessing", ev =>
            {
                ev.EventName    = "Shrine Blessing";
                ev.Prompt       = "A roadside shrine radiates calm. You feel restored.";
                ev.Category     = SectorEventCategory.Rest;
                ev.Weight       = 60;
                ev.HealFraction = 0.40f;
                ev.InstantGrant = new ResourceDelta { Morale = 2, Rations = 1 };
            }));

            return list.ToArray();
        }

        private static SectorEventSO[] CreateTreasureEvents()
        {
            var list = new List<SectorEventSO>();

            list.Add(GetOrCreateEvent("SectorEvent_Treasure_Cache", ev =>
            {
                ev.EventName    = "Hidden Cache";
                ev.Prompt       = "Tucked beneath a rock: supplies left by a traveler.";
                ev.Category     = SectorEventCategory.Treasure;
                ev.Weight       = 100;
                ev.InstantGrant = new ResourceDelta { Gold = 15, Rations = 2 };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Treasure_Relic", ev =>
            {
                ev.EventName    = "Ancient Relic";
                ev.Prompt       = "A crumbling ruin yields a relic worth trading.";
                ev.Category     = SectorEventCategory.Treasure;
                ev.Weight       = 60;
                ev.InstantGrant = new ResourceDelta { Gold = 30, Scrap = 8 };
            }));

            return list.ToArray();
        }

        private static SectorEventSO[] CreateDilemmaEvents()
        {
            var list = new List<SectorEventSO>();

            list.Add(GetOrCreateEvent("SectorEvent_Dilemma_Refugees", ev =>
            {
                ev.EventName = "Refugees on the Road";
                ev.Prompt    = "A family flees a burning village. They beg for food and coin.";
                ev.Category  = SectorEventCategory.Dilemma;
                ev.Weight    = 100;
                ev.Choices   = new List<SectorChoice>
                {
                    new SectorChoice
                    {
                        Label          = "Give them rations and gold",
                        FlavorText     = "They thank you with tears. Morale lifts.",
                        ResourceDelta  = new ResourceDelta { Rations = -2, Gold = -5, Morale = 3 },
                        ClassXPReward  = 30,
                    },
                    new SectorChoice
                    {
                        Label         = "Offer only kind words",
                        FlavorText    = "They nod and move on. You feel uneasy.",
                        ResourceDelta = new ResourceDelta { Morale = -1 },
                        ClassXPReward = 10,
                    },
                    new SectorChoice
                    {
                        Label         = "Ignore them and press on",
                        FlavorText    = "Time is short. No time for distractions.",
                        ResourceDelta = new ResourceDelta { },
                        ClassXPReward = 0,
                    },
                };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Dilemma_WoundedSoldier", ev =>
            {
                ev.EventName = "Wounded Soldier";
                ev.Prompt    = "An injured soldier asks you to carry a message to his commander. It will take you off-route.";
                ev.Category  = SectorEventCategory.Dilemma;
                ev.Weight    = 80;
                ev.Choices   = new List<SectorChoice>
                {
                    new SectorChoice
                    {
                        Label         = "Deliver the message",
                        FlavorText    = "The commander rewards your detour.",
                        ResourceDelta = new ResourceDelta { Gold = 20, Rations = -1 },
                        ClassXPReward = 40,
                    },
                    new SectorChoice
                    {
                        Label          = "Treat his wounds instead",
                        FlavorText     = "He can make it himself. You patch him up and move on.",
                        ResourceDelta  = new ResourceDelta { Morale = 2, Rations = -1 },
                        HeroHPDelta    = 0f,
                        ClassXPReward  = 25,
                    },
                    new SectorChoice
                    {
                        Label         = "Leave him — you have a mission",
                        FlavorText    = "Every second counts.",
                        ResourceDelta = new ResourceDelta { },
                        ClassXPReward = 0,
                    },
                };
            }));

            list.Add(GetOrCreateEvent("SectorEvent_Dilemma_BanditToll", ev =>
            {
                ev.EventName = "Bandit Toll";
                ev.Prompt    = "A group of bandits demands coin to pass their territory — or you fight.";
                ev.Category  = SectorEventCategory.Dilemma;
                ev.Weight    = 90;
                ev.Choices   = new List<SectorChoice>
                {
                    new SectorChoice
                    {
                        Label         = "Pay the toll",
                        FlavorText    = "They pocket the gold and wave you through.",
                        ResourceDelta = new ResourceDelta { Gold = -15 },
                        ClassXPReward = 0,
                    },
                    new SectorChoice
                    {
                        Label         = "Refuse and fight",
                        FlavorText    = "You draw your weapon. They scatter — but not without cost.",
                        ResourceDelta = new ResourceDelta { Gold = 10, Scrap = 5 },
                        HeroHPDelta   = -0.15f,
                        ClassXPReward = 35,
                    },
                    new SectorChoice
                    {
                        Label         = "Bluff your way through",
                        FlavorText    = "You flash a fake seal. They hesitate — then let you pass.",
                        ResourceDelta = new ResourceDelta { Morale = 1 },
                        ClassXPReward = 20,
                    },
                };
            }));

            return list.ToArray();
        }

        // ── 3. Wire AreaMapConfigs ────────────────────────────────────────────

        private static void WireAreaConfigs(
            SectorEventSO[] combat, SectorEventSO[] rest,
            SectorEventSO[] treasure, SectorEventSO[] dilemma)
        {
            string[] configPaths = {
                $"{MapDir}/AreaMapConfig_Hostile.asset",
                $"{MapDir}/AreaMapConfig_Safe.asset",
                $"{MapDir}/AreaMapConfig_Random.asset",
            };

            foreach (var path in configPaths)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<AreaMapConfigSO>(path);
                if (cfg == null) { Debug.LogWarning($"[WireFullGameLoop] Not found: {path}"); continue; }

                cfg.CombatEvents   = combat;
                cfg.RestEvents     = rest;
                cfg.HealEvents     = rest;
                cfg.TreasureEvents = treasure;
                cfg.DilemmaEvents  = dilemma;
                EditorUtility.SetDirty(cfg);
                Debug.Log($"[WireFullGameLoop] Pools wired into {cfg.name}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SectorEventSO GetOrCreateEvent(string assetName, System.Action<SectorEventSO> configure)
        {
            string path = $"{EventDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SectorEventSO>(path);
            if (existing != null) return existing;

            var ev = ScriptableObject.CreateInstance<SectorEventSO>();
            configure(ev);
            AssetDatabase.CreateAsset(ev, path);
            return ev;
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
