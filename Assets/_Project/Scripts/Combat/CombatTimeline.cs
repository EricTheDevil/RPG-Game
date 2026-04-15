using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Units;

namespace RPG.Combat
{
    /// <summary>
    /// CT-based autobattle timeline.
    ///
    /// Replaces TurnManager.  Each call to Tick() advances every living unit's
    /// CT by their Speed stat.  When a unit reaches CT ≥ 100 it is returned as
    /// "ready to act" and its CT is reset to 0.
    ///
    /// Supports any number of units on both teams.  Dead units are silently
    /// removed on the next Tick().
    ///
    /// UI can call GetSortedUnits() at any time for the initiative bar.
    /// </summary>
    public class CombatTimeline : MonoBehaviour
    {
        public static CombatTimeline Instance { get; private set; }

        [Header("CT Settings")]
        [Tooltip("CT threshold to act (default FFT value = 100)")]
        public float CTThreshold = 100f;

        private readonly List<Unit> _units = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Registration ──────────────────────────────────────────────────────

        public void RegisterUnits(IEnumerable<Unit> units)
        {
            _units.Clear();
            _units.AddRange(units);

            // Stagger initial CT so different speeds feel natural at round 1
            foreach (var u in _units)
                u.CT = Random.Range(0f, CTThreshold * 0.3f);
        }

        public void RemoveUnit(Unit unit) => _units.Remove(unit);

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Advance CT by one step for all living units.
        /// Returns the first unit whose CT reached the threshold, or null if none
        /// are ready yet (caller should tick again on the next frame).
        /// </summary>
        public Unit Tick()
        {
            _units.RemoveAll(u => u == null || !u.IsAlive);
            if (_units.Count == 0) return null;

            foreach (var unit in _units)
            {
                unit.CT += unit.RT.Speed * 0.5f;   // 0.5 multiplier smooths bursts
                if (unit.CT >= CTThreshold)
                {
                    unit.CT = 0f;
                    return unit;
                }
            }
            return null;
        }

        // ── UI Support ────────────────────────────────────────────────────────

        /// <summary>Returns all living units sorted by CT descending (for initiative bar).</summary>
        public List<Unit> GetSortedUnits()
            => _units.Where(u => u != null && u.IsAlive)
                     .OrderByDescending(u => u.CT)
                     .ToList();

        /// <summary>Estimated ticks until a unit next acts (for HUD tooltip).</summary>
        public int TicksUntilAct(Unit unit)
        {
            float remaining = CTThreshold - unit.CT;
            float gain      = unit.RT.Speed * 0.5f;
            return gain <= 0f ? int.MaxValue : Mathf.CeilToInt(remaining / gain);
        }
    }
}
