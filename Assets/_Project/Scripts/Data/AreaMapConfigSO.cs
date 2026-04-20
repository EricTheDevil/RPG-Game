using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Tuning knobs for procedural area map generation.
    /// One asset per world node type (Hostile, Safe, Random).
    ///
    /// Event pools use SectorEventSO — the new multi-choice event type.
    /// EventCardSO assets are no longer referenced here; sector events
    /// are authored as SectorEventSO assets in ScriptableObjects/SectorEvents/.
    ///
    /// Create via  Assets > Create > RPG > Area Map Config
    /// </summary>
    [CreateAssetMenu(fileName = "AreaMapConfig_", menuName = "RPG/Area Map Config")]
    public class AreaMapConfigSO : ScriptableObject
    {
        [Header("Grid Size")]
        [Tooltip("Minimum event tiles spawned in the grid (excluding entry/exit/blocked).")]
        [Range(2, 12)]
        public int MinEvents = 4;

        [Tooltip("Maximum event tiles spawned.")]
        [Range(2, 12)]
        public int MaxEvents = 7;

        [Header("Threat Pressure")]
        [Tooltip("Events the player can resolve before remaining tiles lock. Lower = more pressure.")]
        [Range(1, 10)]
        public int ThreatLimit = 3;

        [Header("Event Type Weights")]
        [Range(0f, 1f)] public float CombatWeight   = 0.35f;
        [Range(0f, 1f)] public float EliteWeight     = 0.05f;
        [Range(0f, 1f)] public float DilemmaWeight   = 0.20f;  // NEW — Field Decisions
        [Range(0f, 1f)] public float ShopWeight      = 0.12f;
        [Range(0f, 1f)] public float HealWeight      = 0.10f;
        [Range(0f, 1f)] public float RestWeight      = 0.08f;
        [Range(0f, 1f)] public float TreasureWeight  = 0.05f;
        [Range(0f, 1f)] public float ShrineWeight    = 0.03f;  // NEW — Class XP shrines
        [Range(0f, 1f)] public float LevelUpWeight   = 0.02f;

        [Header("Event Pools (assign SectorEventSO assets)")]
        public SectorEventSO[] CombatEvents   = new SectorEventSO[0];
        public SectorEventSO[] EliteEvents    = new SectorEventSO[0];
        public SectorEventSO[] DilemmaEvents  = new SectorEventSO[0];
        public SectorEventSO[] ShopEvents     = new SectorEventSO[0];
        public SectorEventSO[] HealEvents     = new SectorEventSO[0];
        public SectorEventSO[] RestEvents     = new SectorEventSO[0];
        public SectorEventSO[] TreasureEvents = new SectorEventSO[0];
        public SectorEventSO[] ShrineEvents   = new SectorEventSO[0];
        public SectorEventSO[] LevelUpEvents  = new SectorEventSO[0];

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Pick a random event category using the configured weights.</summary>
        public SectorEventCategory RollEventType()
        {
            float total = CombatWeight + EliteWeight + DilemmaWeight + ShopWeight
                        + HealWeight + RestWeight + TreasureWeight + ShrineWeight + LevelUpWeight;
            if (total <= 0f) return SectorEventCategory.Combat;

            float roll = UnityEngine.Random.Range(0f, total);
            float acc  = 0f;

            acc += CombatWeight;   if (roll < acc) return SectorEventCategory.Combat;
            acc += EliteWeight;    if (roll < acc) return SectorEventCategory.Combat; // elite uses Combat pool for now
            acc += DilemmaWeight;  if (roll < acc) return SectorEventCategory.Dilemma;
            acc += ShopWeight;     if (roll < acc) return SectorEventCategory.Shop;
            acc += HealWeight;     if (roll < acc) return SectorEventCategory.Rest;
            acc += RestWeight;     if (roll < acc) return SectorEventCategory.Rest;
            acc += TreasureWeight; if (roll < acc) return SectorEventCategory.Treasure;
            acc += ShrineWeight;   if (roll < acc) return SectorEventCategory.Shrine;
                                   return SectorEventCategory.LevelUp;
        }

        /// <summary>Pick a random SectorEventSO for the given category.</summary>
        public SectorEventSO PickEvent(SectorEventCategory category)
        {
            var pool = category switch
            {
                SectorEventCategory.Combat   => CombatEvents,
                SectorEventCategory.Dilemma  => DilemmaEvents,
                SectorEventCategory.Shop     => ShopEvents,
                SectorEventCategory.Rest     => RestEvents.Length > 0 ? RestEvents : HealEvents,
                SectorEventCategory.Treasure => TreasureEvents,
                SectorEventCategory.Shrine   => ShrineEvents,
                SectorEventCategory.LevelUp  => LevelUpEvents,
                _                            => CombatEvents,
            };

            // Fallback: if specific pool is empty, grab from any pool with entries
            if (pool == null || pool.Length == 0)
                pool = GetAnyNonEmptyPool();

            if (pool == null || pool.Length == 0) return null;

            // Weighted pick using SectorEventSO.EffectiveWeight
            int total = 0;
            foreach (var e in pool) if (e != null) total += e.EffectiveWeight;
            if (total == 0) return pool[UnityEngine.Random.Range(0, pool.Length)];

            int roll = UnityEngine.Random.Range(0, total);
            int acc  = 0;
            foreach (var e in pool)
            {
                if (e == null) continue;
                acc += e.EffectiveWeight;
                if (roll < acc) return e;
            }
            return pool[^1];
        }

        private SectorEventSO[] GetAnyNonEmptyPool()
        {
            if (CombatEvents.Length  > 0) return CombatEvents;
            if (DilemmaEvents.Length > 0) return DilemmaEvents;
            if (RestEvents.Length    > 0) return RestEvents;
            return HealEvents;
        }
    }
}
