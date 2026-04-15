using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Units;
using RPG.Data;

namespace RPG.Combat
{
    /// <summary>
    /// TFT-style trait synergy system.
    ///
    /// Each UnitStatsSO carries a list of Traits (strings like "Warrior", "Mage").
    /// At battle start, TraitSystem counts how many units on each team share a
    /// trait and applies flat stat bonuses when thresholds are met.
    ///
    /// Add new synergies by expanding the TraitBonus list in the Inspector — no
    /// code changes needed.
    ///
    /// Example Inspector setup:
    ///   Trait: "Warrior"  Threshold: 2  BonusATK: 8  BonusDEF: 5
    ///   Trait: "Mage"     Threshold: 2  BonusMAG: 12 BonusMP: 20
    /// </summary>
    public class TraitSystem : MonoBehaviour
    {
        [System.Serializable]
        public class TraitBonus
        {
            [Tooltip("Trait name must match UnitStatsSO.Traits entries exactly.")]
            public string  TraitName;

            [Tooltip("Number of units with this trait required to activate the bonus.")]
            public int     Threshold = 2;

            [Header("Flat Stat Bonuses  (applied to each matching unit)")]
            public int     BonusAttack      = 0;
            public int     BonusDefense     = 0;
            public int     BonusMagicAttack = 0;
            public int     BonusMaxHP       = 0;
            public int     BonusMaxMP       = 0;
            public int     BonusSpeed       = 0;

            [Tooltip("Log-friendly display name, e.g. 'Warrior Synergy (×2)'")]
            public string  DisplayName;
            public Color   SynergyColor = Color.cyan;
        }

        [Header("Synergy Definitions")]
        public List<TraitBonus> Bonuses = new();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called once at combat start (after spawn, before first tick).
        /// Mutates each unit's RuntimeStats in-place.
        /// </summary>
        public void ApplySynergies(IEnumerable<Unit> allUnits)
        {
            var units = allUnits.ToList();

            // Evaluate per team so traits only count within your own team
            ApplySynergiesForTeam(units.Where(u => u.Team == Team.Player).ToList());
            ApplySynergiesForTeam(units.Where(u => u.Team == Team.Enemy).ToList());
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void ApplySynergiesForTeam(List<Unit> teamUnits)
        {
            if (teamUnits.Count == 0 || Bonuses.Count == 0) return;

            foreach (var bonus in Bonuses)
            {
                var matching = teamUnits
                    .Where(u => u.Stats != null && u.Stats.Traits.Contains(bonus.TraitName))
                    .ToList();

                if (matching.Count < bonus.Threshold) continue;

                string label = string.IsNullOrEmpty(bonus.DisplayName)
                    ? $"{bonus.TraitName} ×{matching.Count}"
                    : bonus.DisplayName;

                Debug.Log($"[TraitSystem] <color=#{ColorUtility.ToHtmlStringRGB(bonus.SynergyColor)}>" +
                          $"{label}</color> activated for {matching.Count} unit(s).");

                foreach (var unit in matching)
                    ApplyBonus(unit, bonus);
            }
        }

        private static void ApplyBonus(Unit unit, TraitBonus bonus)
        {
            if (unit.RT == null) return;
            unit.RT.Attack      += bonus.BonusAttack;
            unit.RT.Defense     += bonus.BonusDefense;
            unit.RT.MagicAttack += bonus.BonusMagicAttack;
            unit.RT.MaxHP       += bonus.BonusMaxHP;
            unit.RT.MaxMP       += bonus.BonusMaxMP;
            unit.RT.Speed       += bonus.BonusSpeed;
        }
    }
}
