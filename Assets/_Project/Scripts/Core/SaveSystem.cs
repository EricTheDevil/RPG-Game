using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RPG.Data;
using RPG.Map;

namespace RPG.Core
{
    /// <summary>
    /// Serializes / deserializes GameSession to JSON on disk.
    /// Stable keys: SO asset names (buff.name, class.name).
    /// Map state is serialized inline as JSON via JsonUtility.
    /// Commander meta-progression is in a separate file (commander.json).
    /// </summary>
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        // ── DTO ───────────────────────────────────────────────────────────────

        [Serializable]
        private class SaveData
        {
            // Resources
            public int Gold;
            public int Scrap;
            public int Rations;
            public int Morale;

            // Run depth
            public int FightsWon;

            // Earned buffs (stored as SO asset names)
            public List<string> BuffNames = new();

            // World map state (serialized WorldMapData)
            public string WorldMapJson;
            public int    CurrentLayerIndex;
            public string CurrentWorldNodeId;

            // Area map state (serialized AreaMapData, null when not in an area)
            public string AreaMapJson;

            // Hero class (SO asset name — resolved via Resources.Load at load time)
            public string ActiveClassName;

            // Starvation state
            public float StarvationHPPenalty;
            public bool  IsStarving;

            // Class XP earned this run (pending delivery to CommanderProfile on victory)
            public int PendingClassXP;

            // Task progress counters
            public int   Task_CombatsWon;
            public int   Task_CitizensHelped;
            public bool  Task_ReachedBoss;
            public int   Task_PeakGold;
            public int   Task_StarvedCombats;
            public bool  Task_RunCompleted;
            public int   Task_ConsecutiveWinsNoBuff;
            public int   Task_PeakMorale;

            // Hero runtime stats snapshot
            public int   Hero_MaxHP;
            public int   Hero_MaxMP;
            public int   Hero_Attack;
            public int   Hero_Defense;
            public int   Hero_MagicAttack;
            public int   Hero_MagicDefense;
            public int   Hero_Speed;
            public int   Hero_Movement;
            public float Hero_CritChance;
            public float Hero_CritMultiplier;
            public int   Hero_CurrentHP;
            public bool  Hero_HasSnapshot;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Save(GameSession session)
        {
            if (session == null) return;

            var data = new SaveData
            {
                Gold      = session.Resources.Gold,
                Scrap     = session.Resources.Scrap,
                Rations   = session.Resources.Rations,
                Morale    = session.Resources.Morale,
                FightsWon = session.FightsWon,
                Hero_CurrentHP = session.HeroCurrentHP,

                // Map state
                CurrentLayerIndex  = session.CurrentLayerIndex,
                CurrentWorldNodeId = session.CurrentWorldNodeId ?? "",

                // Class state
                ActiveClassName      = session.ActiveClass?.name ?? "",
                StarvationHPPenalty  = session.StarvationHPPenalty,
                IsStarving           = session.IsStarving,
                PendingClassXP       = session.PendingClassXP,

                // Task progress
                Task_CombatsWon            = session.TaskProgress.CombatsWon,
                Task_CitizensHelped        = session.TaskProgress.CitizensHelped,
                Task_ReachedBoss           = session.TaskProgress.ReachedBoss,
                Task_PeakGold              = session.TaskProgress.PeakGold,
                Task_StarvedCombats        = session.TaskProgress.StarvedCombats,
                Task_RunCompleted          = session.TaskProgress.RunCompleted,
                Task_ConsecutiveWinsNoBuff = session.TaskProgress.ConsecutiveWinsNoBuff,
                Task_PeakMorale            = session.TaskProgress.PeakMorale,
            };

            // Serialize map data
            if (session.WorldMap != null)
                data.WorldMapJson = JsonUtility.ToJson(session.WorldMap);
            if (session.CurrentArea != null)
                data.AreaMapJson = JsonUtility.ToJson(session.CurrentArea);

            // Hero RT snapshot
            var rt = session.HeroRT;
            if (rt != null)
            {
                data.Hero_HasSnapshot    = true;
                data.Hero_MaxHP          = rt.MaxHP;
                data.Hero_MaxMP          = rt.MaxMP;
                data.Hero_Attack         = rt.Attack;
                data.Hero_Defense        = rt.Defense;
                data.Hero_MagicAttack    = rt.MagicAttack;
                data.Hero_MagicDefense   = rt.MagicDefense;
                data.Hero_Speed          = rt.Speed;
                data.Hero_Movement       = rt.Movement;
                data.Hero_CritChance     = rt.CritChance;
                data.Hero_CritMultiplier = rt.CritMultiplier;
            }

            foreach (var buff in session.EarnedBuffs)
                if (buff != null) data.BuffNames.Add(buff.name);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        public static bool Load(GameSession session, BuffRegistry buffRegistry)
        {
            if (session == null || !File.Exists(SavePath)) return false;

            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return false;

            session.LoadFromSave(
                data.Gold, data.Scrap, data.Rations, data.Morale,
                data.BuffNames, buffRegistry);

            session.FightsWon     = data.FightsWon;
            session.HeroCurrentHP = data.Hero_CurrentHP;

            // Restore map state
            session.CurrentLayerIndex  = data.CurrentLayerIndex;
            session.CurrentWorldNodeId = string.IsNullOrEmpty(data.CurrentWorldNodeId) ? null : data.CurrentWorldNodeId;

            if (!string.IsNullOrEmpty(data.WorldMapJson))
                session.WorldMap = JsonUtility.FromJson<WorldMapData>(data.WorldMapJson);
            if (!string.IsNullOrEmpty(data.AreaMapJson))
                session.CurrentArea = JsonUtility.FromJson<AreaMapData>(data.AreaMapJson);

            // Restore class state
            if (!string.IsNullOrEmpty(data.ActiveClassName))
            {
                var cls = Resources.Load<RPG.Data.ClassDefinitionSO>(data.ActiveClassName);
                if (cls == null)
                    Debug.LogWarning($"[SaveSystem] ClassDefinitionSO '{data.ActiveClassName}' not found in Resources.");
                session.ActiveClass = cls;
            }

            session.LoadStarvationState(data.StarvationHPPenalty, data.IsStarving);
            session.LoadPendingClassXP(data.PendingClassXP);

            // Restore task progress
            session.TaskProgress = new RPG.Data.RunTaskProgress
            {
                CombatsWon            = data.Task_CombatsWon,
                CitizensHelped        = data.Task_CitizensHelped,
                ReachedBoss           = data.Task_ReachedBoss,
                PeakGold              = data.Task_PeakGold,
                StarvedCombats        = data.Task_StarvedCombats,
                RunCompleted          = data.Task_RunCompleted,
                ConsecutiveWinsNoBuff = data.Task_ConsecutiveWinsNoBuff,
                PeakMorale            = data.Task_PeakMorale,
            };

            // Restore hero RT
            if (data.Hero_HasSnapshot)
            {
                session.HeroRT = new RPG.Units.RuntimeStats
                {
                    MaxHP          = data.Hero_MaxHP,
                    MaxMP          = data.Hero_MaxMP,
                    Attack         = data.Hero_Attack,
                    Defense        = data.Hero_Defense,
                    MagicAttack    = data.Hero_MagicAttack,
                    MagicDefense   = data.Hero_MagicDefense,
                    Speed          = data.Hero_Speed,
                    Movement       = data.Hero_Movement,
                    CritChance     = data.Hero_CritChance,
                    CritMultiplier = data.Hero_CritMultiplier,
                };
            }

            return true;
        }

        public static void DeleteSave() { if (File.Exists(SavePath)) File.Delete(SavePath); }
        public static bool SaveExists() => File.Exists(SavePath);
    }
}
