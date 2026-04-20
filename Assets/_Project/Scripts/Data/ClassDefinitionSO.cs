using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Class Tier  — which column in the 3×3 mastery tree this class sits in
    // ─────────────────────────────────────────────────────────────────────────

    public enum ClassTier
    {
        Beginner,       // Tier 0 — starting pick (Warrior / Mage / Ranger)
        Veteran,        // Tier 1 — unlocked by levelling a Beginner class
        Master,         // Tier 2 — unlocked by completing a Class Task on a Veteran class
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Stat Growth  — per-level stat gain for this class
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ClassStatGrowth
    {
        [Tooltip("Added to base MaxHP each class level.")]
        [Range(0, 100)] public int MaxHP       = 10;
        [Tooltip("Added to base MaxMP each class level.")]
        [Range(0, 50)]  public int MaxMP       = 5;
        [Tooltip("Added to Attack each class level.")]
        [Range(0, 30)]  public int Attack      = 2;
        [Tooltip("Added to Defense each class level.")]
        [Range(0, 30)]  public int Defense     = 1;
        [Tooltip("Added to MagicAttack each class level.")]
        [Range(0, 30)]  public int MagicAttack = 1;
        [Tooltip("Added to Speed each class level.")]
        [Range(0, 10)]  public int Speed       = 0;
        [Tooltip("Bonus to CritChance per level (e.g. 0.01 = +1%).")]
        [Range(0f, 0.1f)] public float CritChance = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Class Level Reward  — granted at a specific class level
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ClassLevelReward
    {
        [Range(1, 10)]
        public int     AtLevel = 1;

        [Tooltip("Buff permanently added to the hero's base loadout when this level is reached.")]
        public BuffSO  PermanentBuff;

        [Tooltip("Trait string added to the hero's UnitStatsSO Traits list at this level.")]
        public string  UnlockedTrait = "";

        [Tooltip("Flavour text shown when this level reward is granted.")]
        [TextArea(1, 2)]
        public string  RewardFlavorText = "";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ClassDefinitionSO  — one of the 9 hero classes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a hero class: identity, stat growth, level rewards, evolution chain.
    ///
    /// Class tree (3 tiers, 3 columns):
    ///
    ///   Tier 0 (Beginner)   Tier 1 (Veteran)    Tier 2 (Master)
    ///   ─────────────────   ─────────────────    ──────────────────
    ///   Warrior             Knight               Paladin
    ///   Mage                Sorcerer             Archmage
    ///   Ranger              Sentinel             Warden
    ///
    /// Each class has 10 levels. Level-up requires XP (earned in combat + sector events).
    /// Reaching max level on a Beginner class permanently unlocks the Veteran class.
    /// Completing a Class Task on a Veteran class permanently unlocks the Master class.
    ///
    /// The hero can only be ONE class at a time per run, but all unlocked classes
    /// carry their accumulated level bonuses into every run they are selected for.
    ///
    /// Create via  Assets > Create > RPG > Class Definition
    /// </summary>
    [CreateAssetMenu(fileName = "Class_", menuName = "RPG/Class Definition")]
    public class ClassDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string    ClassName    = "Warrior";
        [TextArea(2, 3)]
        public string    Description  = "";
        public Sprite    ClassIcon;
        public Color     ClassColor   = Color.white;
        public ClassTier Tier         = ClassTier.Beginner;

        [Header("Progression")]
        [Tooltip("Total class levels. Fixed at 10 for all classes.")]
        public int MaxLevel = 10;

        [Tooltip("XP required to reach each level. Index 0 = XP to reach level 1, etc.")]
        public int[] XPThresholds = { 100, 250, 450, 700, 1000, 1350, 1750, 2200, 2700, 3250 };

        [Header("Stat Growth (applied per level reached, cumulative)")]
        public ClassStatGrowth StatGrowthPerLevel;

        [Header("Level Rewards (buffs / traits at specific levels)")]
        public List<ClassLevelReward> LevelRewards = new();

        [Header("Starting Stats Override")]
        [Tooltip("If set, the hero uses these base stats when this class is active. " +
                 "Leave null to use the default HeroStats asset.")]
        public UnitStatsSO BaseStatsSO;

        [Header("Evolution")]
        [Tooltip("Class this one evolves into when MaxLevel is reached (Beginner→Veteran) " +
                 "or when a Class Task is completed (Veteran→Master).")]
        public ClassDefinitionSO EvolvesInto;

        [Tooltip("The Class Task that must be completed to unlock the Master-tier evolution. " +
                 "Leave null for Beginner→Veteran (level-based unlock).")]
        public ClassTaskSO EvolutionTask;

        [Header("Starting Abilities")]
        [Tooltip("Abilities the hero has at level 1 of this class. " +
                 "Additional abilities can be granted via LevelRewards.")]
        public List<AbilitySO> StartingAbilities = new();

        // ── Helpers ───────────────────────────────────────────────────────────

        public int MaxLevelClamped => Mathf.Clamp(MaxLevel, 1, 10);

        /// <summary>XP needed to advance from <paramref name="currentLevel"/> to the next.</summary>
        public int XPForNextLevel(int currentLevel)
        {
            int idx = Mathf.Clamp(currentLevel, 0, XPThresholds.Length - 1);
            return XPThresholds[idx];
        }

        /// <summary>
        /// Cumulative stat bonus from having reached <paramref name="classLevel"/>.
        /// Represents all stat growth from level 0 up to (but not including) the current level.
        /// </summary>
        public ClassStatGrowth CumulativeGrowthAt(int classLevel)
        {
            int levels = Mathf.Clamp(classLevel, 0, MaxLevelClamped);
            return new ClassStatGrowth
            {
                MaxHP       = StatGrowthPerLevel.MaxHP       * levels,
                MaxMP       = StatGrowthPerLevel.MaxMP       * levels,
                Attack      = StatGrowthPerLevel.Attack      * levels,
                Defense     = StatGrowthPerLevel.Defense     * levels,
                MagicAttack = StatGrowthPerLevel.MagicAttack * levels,
                Speed       = StatGrowthPerLevel.Speed       * levels,
                CritChance  = StatGrowthPerLevel.CritChance  * levels,
            };
        }

        /// <summary>All rewards that have been earned by reaching <paramref name="classLevel"/>.</summary>
        public List<ClassLevelReward> RewardsUpTo(int classLevel)
        {
            var result = new List<ClassLevelReward>();
            foreach (var r in LevelRewards)
                if (r.AtLevel <= classLevel) result.Add(r);
            return result;
        }
    }
}
