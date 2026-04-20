using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ClassRecord  — persistent progress for one class
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ClassRecord
    {
        /// <summary>Asset name of the ClassDefinitionSO.</summary>
        public string ClassName;

        /// <summary>Current class level (0–10). Zero = class exists but hasn't been used.</summary>
        public int    Level = 0;

        /// <summary>Total XP accumulated toward the next level.</summary>
        public int    XP    = 0;

        /// <summary>Has this class been permanently unlocked (visible in class select)?</summary>
        public bool   Unlocked = false;

        /// <summary>Names of Class Tasks that have been permanently completed for this class.</summary>
        public List<string> CompletedTaskNames = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CommanderProfile  — cross-run persistent meta state
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The Commander's permanent record. Persists across all runs and never resets on death.
    ///
    /// Stored as a SEPARATE file from the run save (commander.json).
    /// This means losing a run never erases your class progress.
    ///
    /// Owns:
    ///   • Per-class Level + XP + completed tasks
    ///   • Total runs attempted / completed (for Commander XP)
    ///   • Commander-level (meta-level unlocking new starting options + events)
    ///   • Which classes are unlocked
    ///
    /// Singleton — accessed via CommanderProfile.Instance.
    /// </summary>
    public class CommanderProfile : MonoBehaviour
    {
        public static CommanderProfile Instance { get; private set; }

        // ── Class Records ─────────────────────────────────────────────────────
        private readonly Dictionary<string, ClassRecord> _classes = new();

        // ── Commander-level meta ──────────────────────────────────────────────
        public int CommanderXP       { get; private set; } = 0;
        public int CommanderLevel    { get; private set; } = 0;
        public int TotalRunsStarted  { get; private set; } = 0;
        public int TotalRunsWon      { get; private set; } = 0;
        public int TotalFightsWon    { get; private set; } = 0;

        // Commander levels up every N XP. Simple flat threshold for now.
        public  const int CommanderXPPerLevel = 500;
        public  const int MaxCommanderLevel   = 20;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("CommanderProfile");
            go.AddComponent<CommanderProfile>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CommanderProfileSaveSystem.Load(this);
        }

        // ── Class API ─────────────────────────────────────────────────────────

        /// <summary>Get or create the ClassRecord for the given class name.</summary>
        public ClassRecord GetRecord(string className)
        {
            if (!_classes.TryGetValue(className, out var rec))
            {
                rec = new ClassRecord { ClassName = className };
                _classes[className] = rec;
            }
            return rec;
        }

        public ClassRecord GetRecord(ClassDefinitionSO def) => GetRecord(def.name);

        /// <summary>Unlock a class (makes it selectable at run start).</summary>
        public void UnlockClass(ClassDefinitionSO def)
        {
            var rec = GetRecord(def.name);
            if (rec.Unlocked) return;
            rec.Unlocked = true;
            Debug.Log($"[Commander] Class unlocked: {def.ClassName}");
            CommanderProfileSaveSystem.Save(this);
        }

        /// <summary>
        /// Add class XP. Returns the new record.
        /// Fires level-up logic if thresholds are crossed.
        /// </summary>
        public ClassRecord AddClassXP(ClassDefinitionSO def, int xp)
        {
            var rec = GetRecord(def.name);
            rec.XP += xp;

            while (rec.Level < def.MaxLevelClamped)
            {
                int needed = def.XPForNextLevel(rec.Level);
                if (rec.XP < needed) break;
                rec.XP    -= needed;
                rec.Level++;
                OnClassLevelUp(def, rec);
            }

            CommanderProfileSaveSystem.Save(this);
            return rec;
        }

        /// <summary>Mark a Class Task as permanently completed.</summary>
        public void CompleteTask(ClassDefinitionSO classDef, ClassTaskSO task)
        {
            var rec = GetRecord(classDef.name);
            if (rec.CompletedTaskNames.Contains(task.name)) return;

            rec.CompletedTaskNames.Add(task.name);
            Debug.Log($"[Commander] Task completed: {task.TaskName} → unlocks {task.UnlocksClass?.ClassName}");

            if (task.UnlocksClass != null)
                UnlockClass(task.UnlocksClass);

            CommanderProfileSaveSystem.Save(this);
        }

        public bool IsTaskCompleted(ClassDefinitionSO classDef, ClassTaskSO task)
        {
            return GetRecord(classDef.name).CompletedTaskNames.Contains(task.name);
        }

        /// <summary>All unlocked ClassDefinitionSO names.</summary>
        public IEnumerable<string> UnlockedClassNames()
        {
            foreach (var kv in _classes)
                if (kv.Value.Unlocked) yield return kv.Key;
        }

        // ── Commander Meta ────────────────────────────────────────────────────

        public void OnRunStarted()
        {
            TotalRunsStarted++;
            CommanderProfileSaveSystem.Save(this);
        }

        public void OnRunCompleted(bool won, int fightsWon)
        {
            if (won) TotalRunsWon++;
            TotalFightsWon += fightsWon;

            // Commander XP scales with how far the player got
            int xpGain = fightsWon * 20 + (won ? 200 : 50);
            AddCommanderXP(xpGain);
        }

        private void AddCommanderXP(int xp)
        {
            CommanderXP += xp;
            while (CommanderLevel < MaxCommanderLevel && CommanderXP >= CommanderXPPerLevel)
            {
                CommanderXP    -= CommanderXPPerLevel;
                CommanderLevel++;
                Debug.Log($"[Commander] Commander level up → {CommanderLevel}");
            }
            CommanderProfileSaveSystem.Save(this);
        }

        // ── Serialization Support ─────────────────────────────────────────────

        public List<ClassRecord> GetAllRecords()
        {
            var list = new List<ClassRecord>(_classes.Values);
            return list;
        }

        public void LoadRecords(List<ClassRecord> records)
        {
            _classes.Clear();
            foreach (var rec in records)
                _classes[rec.ClassName] = rec;
        }

        public void LoadMeta(int cmdXP, int cmdLevel, int runsStarted, int runsWon, int fightsWon)
        {
            CommanderXP      = cmdXP;
            CommanderLevel   = cmdLevel;
            TotalRunsStarted = runsStarted;
            TotalRunsWon     = runsWon;
            TotalFightsWon   = fightsWon;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void OnClassLevelUp(ClassDefinitionSO def, ClassRecord rec)
        {
            Debug.Log($"[Commander] {def.ClassName} levelled up to {rec.Level}!");

            // Auto-unlock evolution at max level (Beginner → Veteran)
            if (rec.Level >= def.MaxLevelClamped
                && def.EvolvesInto != null
                && def.EvolutionTask == null)  // level-based (no task required)
            {
                UnlockClass(def.EvolvesInto);
            }
        }
    }
}
