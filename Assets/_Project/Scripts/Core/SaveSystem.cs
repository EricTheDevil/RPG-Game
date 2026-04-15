using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RPG.Data;

namespace RPG.Core
{
    /// <summary>
    /// Serializes / deserializes GameSession to JSON on disk.
    /// Saves: Gold/XP, resources, active buffs, card hand, level progress.
    /// Stable keys: SO asset names for buffs; SO asset names for cards.
    /// </summary>
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        // ── DTO ───────────────────────────────────────────────────────────────

        [Serializable]
        private class SaveData
        {
            // Legacy / progression
            public int          TotalGold;
            public int          TotalXP;
            public int          CurrentLevelIndex = -1;
            public List<int>    CompletedLevelIndices = new();
            public List<int>    UnlockedLevelIndices  = new();

            // Roguelike buffs
            public List<string> BuffNames  = new();

            // Resources
            public int Gold;
            public int Scrap;
            public int Rations;
            public int Morale;

            // Card hand (stored as asset names)
            public List<string> CardNames = new();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Save(GameSession session)
        {
            if (session == null) return;

            var data = new SaveData
            {
                TotalGold         = session.TotalGold,
                TotalXP           = session.TotalXP,
                CurrentLevelIndex = session.CurrentLevel != null ? session.CurrentLevel.LevelIndex : -1,
                Gold    = session.Resources.Gold,
                Scrap   = session.Resources.Scrap,
                Rations = session.Resources.Rations,
                Morale  = session.Resources.Morale,
            };

            foreach (var buff in session.ActiveBuffs)
                if (buff != null) data.BuffNames.Add(buff.name);

            foreach (var card in session.Hand)
                if (card != null) data.CardNames.Add(card.name);

            foreach (int idx in session.CompletedLevelIndices)
                data.CompletedLevelIndices.Add(idx);

            foreach (int idx in session.UnlockedLevelIndices)
                data.UnlockedLevelIndices.Add(idx);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"[SaveSystem] Saved → {SavePath}");
        }

        public static bool Load(GameSession session, BuffRegistry buffRegistry,
                                EventCardRegistry cardRegistry = null)
        {
            if (session == null || !File.Exists(SavePath)) return false;

            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return false;

            // Resolve buff names
            var buffNames = new List<string>();
            if (buffRegistry != null)
                foreach (string n in data.BuffNames)
                    if (buffRegistry.FindByName(n) != null)
                        buffNames.Add(n);

            session.LoadFromSave(
                data.TotalGold, data.TotalXP,
                data.CompletedLevelIndices, data.UnlockedLevelIndices,
                buffNames, data.CurrentLevelIndex,
                buffRegistry);

            // Restore resources
            session.Resources.Gold    = Mathf.Max(0, data.Gold);
            session.Resources.Scrap   = Mathf.Max(0, data.Scrap);
            session.Resources.Rations = Mathf.Clamp(data.Rations, 0, PlayerResources.MaxRations);
            session.Resources.Morale  = Mathf.Clamp(data.Morale,  0, PlayerResources.MaxMorale);

            // Restore card hand
            if (cardRegistry != null)
                foreach (string n in data.CardNames)
                {
                    var card = cardRegistry.FindByName(n);
                    if (card != null) session.AddCardToHand(card);
                }

            Debug.Log($"[SaveSystem] Loaded ← {SavePath}");
            return true;
        }

        public static void DeleteSave() { if (File.Exists(SavePath)) File.Delete(SavePath); }
        public static bool SaveExists() => File.Exists(SavePath);
    }
}
