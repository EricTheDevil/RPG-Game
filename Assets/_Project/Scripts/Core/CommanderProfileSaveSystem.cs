using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Serializes CommanderProfile to a separate file (commander.json).
    /// Completely independent of the run save — survives game-over and reinstall backup.
    /// </summary>
    public static class CommanderProfileSaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "commander.json");

        // ── DTO ───────────────────────────────────────────────────────────────

        [Serializable]
        private class CommanderSaveData
        {
            public int CommanderXP;
            public int CommanderLevel;
            public int TotalRunsStarted;
            public int TotalRunsWon;
            public int TotalFightsWon;
            public List<ClassRecord> ClassRecords = new();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Save(CommanderProfile profile)
        {
            if (profile == null) return;

            var data = new CommanderSaveData
            {
                CommanderXP      = profile.CommanderXP,
                CommanderLevel   = profile.CommanderLevel,
                TotalRunsStarted = profile.TotalRunsStarted,
                TotalRunsWon     = profile.TotalRunsWon,
                TotalFightsWon   = profile.TotalFightsWon,
                ClassRecords     = profile.GetAllRecords(),
            };

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        public static bool Load(CommanderProfile profile)
        {
            if (profile == null || !File.Exists(SavePath)) return false;

            var data = JsonUtility.FromJson<CommanderSaveData>(File.ReadAllText(SavePath));
            if (data == null) return false;

            profile.LoadMeta(
                data.CommanderXP,
                data.CommanderLevel,
                data.TotalRunsStarted,
                data.TotalRunsWon,
                data.TotalFightsWon);

            if (data.ClassRecords != null)
                profile.LoadRecords(data.ClassRecords);

            return true;
        }

        public static bool SaveExists() => File.Exists(SavePath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
