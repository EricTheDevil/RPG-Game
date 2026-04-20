using UnityEngine;
using RPG.Map;

namespace RPG.Data
{
    /// <summary>
    /// Tuning knobs for procedural world map generation.
    /// Controls the branching node path from start to the Demon Lord.
    ///
    /// Create via  Assets > Create > RPG > World Map Config
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "RPG/World Map Config")]
    public class WorldMapConfigSO : ScriptableObject
    {
        [Header("Structure")]
        [Tooltip("Total layers before the boss. Layer 0 is the start, final layer is the Demon Lord.")]
        [Range(5, 20)]
        public int TotalLayers = 10;

        [Tooltip("Minimum nodes per layer (excluding start and boss layers).")]
        [Range(1, 5)]
        public int MinNodesPerLayer = 2;

        [Tooltip("Maximum nodes per layer.")]
        [Range(1, 5)]
        public int MaxNodesPerLayer = 4;

        [Header("Node Type Weights")]
        [Tooltip("Base weight for Hostile nodes.")]
        [Range(0f, 1f)]
        public float HostileWeight = 0.45f;

        [Tooltip("Base weight for Safe nodes.")]
        [Range(0f, 1f)]
        public float SafeWeight = 0.30f;

        [Tooltip("Base weight for Random nodes.")]
        [Range(0f, 1f)]
        public float RandomWeight = 0.25f;

        [Tooltip("Per-layer increase to Hostile weight (later layers get harder).")]
        [Range(0f, 0.1f)]
        public float HostileRampPerLayer = 0.03f;

        [Header("Area Configs (one per node type)")]
        public AreaMapConfigSO HostileAreaConfig;
        public AreaMapConfigSO SafeAreaConfig;
        public AreaMapConfigSO RandomAreaConfig;

        [Header("Boss")]
        [Tooltip("The encounter config for the final boss fight.")]
        public UnitSpawnConfig BossSpawnConfig;

        [Tooltip("Display name for the boss node.")]
        public string BossName = "Demon Lord";

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Get the area config for a given node type.</summary>
        public AreaMapConfigSO GetAreaConfig(NodeType type) => type switch
        {
            NodeType.Hostile => HostileAreaConfig,
            NodeType.Safe    => SafeAreaConfig,
            NodeType.Random  => RandomAreaConfig,
            _                => RandomAreaConfig,
        };

        /// <summary>Get the names used for randomly generated area nodes.</summary>
        public static string[] HostileNames = { "Dark Forest", "Cursed Ruins", "Bandit Camp", "Blighted Marsh", "Scorched Hollow", "Demon Gate" };
        public static string[] SafeNames    = { "Peaceful Village", "Traveler's Rest", "Sacred Grove", "Hidden Oasis", "Mountain Shrine", "Haven" };
        public static string[] RandomNames  = { "Crossroads", "Misty Valley", "Old Battlefield", "Wanderer's Path", "Forgotten Temple", "Twilight Glade" };

        public static string RandomDisplayName(NodeType type)
        {
            var pool = type switch
            {
                NodeType.Hostile => HostileNames,
                NodeType.Safe    => SafeNames,
                _                => RandomNames,
            };
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
    }
}
