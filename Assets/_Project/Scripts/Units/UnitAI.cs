using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Data;
using RPG.Grid;

namespace RPG.Units
{
    /// <summary>
    /// Data-driven AI decision layer shared by ALL unit types (hero and enemy).
    ///
    /// TFT autobattle design:
    ///   1. Prefer an ability whose range covers a target.
    ///   2. Among valid abilities choose by priority (index 0 = highest).
    ///   3. Special ability fires when HP < threshold OR on a random roll,
    ///      simulating the "mana charge" feel of TFT.
    ///   4. If no ability is in range, target the nearest opponent and move.
    ///   5. Healing abilities are used when an ally is below HealTriggerHP%.
    ///
    /// Extend this class (or add AI profiles as ScriptableObjects) to create
    /// different behaviour archetypes (Aggro, Support, Sniper, …).
    /// </summary>
    public class UnitAI : MonoBehaviour
    {
        [Header("AI Profile")]
        [Tooltip("0-1. Chance per turn to use ability index 1+ over index 0.")]
        [Range(0f, 1f)]
        public float SpecialAbilityChance = 0.30f;

        [Tooltip("HP% below which the unit always tries its special ability.")]
        [Range(0f, 1f)]
        public float SpecialTriggerHP = 0.35f;

        [Tooltip("HP% threshold below which a heal-support unit will heal an ally.")]
        [Range(0f, 1f)]
        public float HealTriggerHP = 0.50f;

        // ── Decision Entry Point ──────────────────────────────────────────────

        /// <summary>
        /// Selects the best (ability, target) pair for <paramref name="actor"/>.
        /// Returns (null, null) if no valid action is available this turn.
        /// </summary>
        public (AbilitySO ability, Unit target) SelectAction(
            Unit         actor,
            List<Unit>   opponents,
            List<Unit>   allies,
            BattleGrid   grid)
        {
            if (actor.Abilities.Count == 0) return (null, null);

            float hpRatio = (float)actor.CurrentHP / actor.RT.MaxHP;
            bool  wantSpecial = hpRatio < SpecialTriggerHP
                                || Random.value < SpecialAbilityChance;

            // ── Try healing an ally first ────────────────────────────────────
            var healAbility = actor.Abilities.FirstOrDefault(a =>
                (a.FlatHeal > 0 || a.HealMultiplier > 0) && actor.CanUseAbility(a));

            if (healAbility != null)
            {
                var woundedAlly = allies
                    .Where(u => u.IsAlive && (float)u.CurrentHP / u.RT.MaxHP < HealTriggerHP)
                    .OrderBy(u => (float)u.CurrentHP / u.RT.MaxHP)
                    .FirstOrDefault();

                if (woundedAlly != null &&
                    ManhattanDist(actor.GridPosition, woundedAlly.GridPosition) <= healAbility.Range)
                    return (healAbility, woundedAlly);
            }

            // ── Order abilities by priority ──────────────────────────────────
            // Build candidate list: if wantSpecial, try non-primary first
            var ordered = new List<AbilitySO>(actor.Abilities);
            if (wantSpecial && ordered.Count > 1)
            {
                // Move index 1+ to front, keep index 0 as fallback
                var primary = ordered[0];
                ordered.RemoveAt(0);
                ordered.Add(primary);
            }

            // ── Find nearest opponent (primary target) ───────────────────────
            var nearestOpponent = opponents
                .Where(u => u.IsAlive)
                .OrderBy(u => ManhattanDist(actor.GridPosition, u.GridPosition))
                .FirstOrDefault();

            if (nearestOpponent == null) return (null, null);

            // ── Try each ability in priority order ───────────────────────────
            foreach (var ab in ordered)
            {
                if (!actor.CanUseAbility(ab)) continue;
                if (ab.ApplyDefendBuff)       continue;   // defend is handled separately below

                var target = ab.Target switch
                {
                    AbilityTarget.Self       => actor,
                    AbilityTarget.SingleAlly => allies.Where(u => u.IsAlive)
                                                       .OrderBy(u => ManhattanDist(actor.GridPosition, u.GridPosition))
                                                       .FirstOrDefault(),
                    _                        => nearestOpponent,
                };

                if (target == null) continue;

                int dist = ManhattanDist(actor.GridPosition, target.GridPosition);
                if (dist <= ab.Range)
                    return (ab, target);

                // Out of range but this is the best ability — will move toward target
                if (ab == ordered.Last())
                    return (ab, target);
            }

            // ── Fallback: defend if nothing else ────────────────────────────
            var defendAb = actor.Abilities.FirstOrDefault(a => a.ApplyDefendBuff && actor.CanUseAbility(a));
            if (defendAb != null) return (defendAb, actor);

            return (ordered.FirstOrDefault(), nearestOpponent);
        }

        // ── Utility ───────────────────────────────────────────────────────────
        private static int ManhattanDist(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
