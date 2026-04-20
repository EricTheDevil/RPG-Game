using UnityEngine;

namespace RPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Task Condition Type  — how the task progress is measured
    // ─────────────────────────────────────────────────────────────────────────

    public enum TaskConditionType
    {
        WinCombats,             // Win N combats in a single run with this class
        HealCitizens,           // Choose the "help" option in N Dilemma events
        ReachBoss,              // Reach the Demon Lord's sector in a single run
        AccumulateGold,         // Have N Gold at any point in a single run
        SurviveStarvation,      // Complete a combat while under the Starvation debuff
        CompleteRunOnClass,     // Complete a full run using only this class
        WinWithoutBuff,         // Win 3 combats in a row without ever picking a buff
        MaxMorale,              // Reach Morale 10 at any point
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ClassTaskSO  — a permanent unlock condition for a class evolution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a challenge the player must complete to evolve a Veteran class
    /// into its Master form.
    ///
    /// Progress is tracked PER RUN but the completion is PERMANENT (stored in
    /// CommanderProfile). Once completed, the Master class is always available.
    ///
    /// Design note: Tasks should tell a story about the class. A Knight's task
    /// ("Heal 5 citizens in a single run") reflects the Paladin's identity before
    /// the player even sees the Paladin. Discovery through play.
    ///
    /// Create via  Assets > Create > RPG > Class Task
    /// </summary>
    [CreateAssetMenu(fileName = "ClassTask_", menuName = "RPG/Class Task")]
    public class ClassTaskSO : ScriptableObject
    {
        [Header("Identity")]
        public string    TaskName    = "Unnamed Task";
        [TextArea(2, 4)]
        public string    Description = "Complete the challenge to unlock the next class tier.";
        public Sprite    Icon;

        [Header("Condition")]
        public TaskConditionType ConditionType;

        [Tooltip("Target value. E.g. for WinCombats: win 5 combats.")]
        [Range(1, 100)]
        public int TargetValue = 5;

        [Header("Rewards on Completion")]
        [Tooltip("The class that is permanently unlocked when this task is completed.")]
        public ClassDefinitionSO UnlocksClass;

        [Tooltip("Optional: one-time bonus buff granted when first completed.")]
        public BuffSO CompletionBuff;

        [TextArea(1, 2)]
        public string CompletionFlavorText = "A new path opens before you.";

        // ── Progress Evaluation ───────────────────────────────────────────────

        /// <summary>
        /// Check whether the task is satisfied given current run stats.
        /// Called at the end of each run and after relevant events.
        /// </summary>
        public bool IsSatisfied(RunTaskProgress progress) => ConditionType switch
        {
            TaskConditionType.WinCombats          => progress.CombatsWon      >= TargetValue,
            TaskConditionType.HealCitizens        => progress.CitizensHelped  >= TargetValue,
            TaskConditionType.ReachBoss           => progress.ReachedBoss,
            TaskConditionType.AccumulateGold      => progress.PeakGold        >= TargetValue,
            TaskConditionType.SurviveStarvation   => progress.StarvedCombats  >= TargetValue,
            TaskConditionType.CompleteRunOnClass  => progress.RunCompleted,
            TaskConditionType.WinWithoutBuff      => progress.ConsecutiveWinsNoBuff >= TargetValue,
            TaskConditionType.MaxMorale           => progress.PeakMorale      >= TargetValue,
            _                                     => false,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RunTaskProgress  — per-run counters fed to IsSatisfied()
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks all progress counters needed for ClassTask evaluation.
    /// Lives on GameSession and is reset each run.
    /// </summary>
    [System.Serializable]
    public class RunTaskProgress
    {
        public int  CombatsWon;
        public int  CitizensHelped;       // Dilemma events where player chose "help" option
        public bool ReachedBoss;
        public int  PeakGold;
        public int  StarvedCombats;       // Combats started while Rations = 0
        public bool RunCompleted;         // Demon Lord defeated
        public int  ConsecutiveWinsNoBuff; // Resets when player picks any buff
        public int  PeakMorale;

        public void Reset()
        {
            CombatsWon             = 0;
            CitizensHelped         = 0;
            ReachedBoss            = false;
            PeakGold               = 0;
            StarvedCombats         = 0;
            RunCompleted           = false;
            ConsecutiveWinsNoBuff  = 0;
            PeakMorale             = 0;
        }
    }
}
