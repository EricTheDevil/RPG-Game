using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Defines a single encounter / level node on the map.
    /// Create one asset per level; assign to MapNode in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "RPG/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Identity")]
        public int    LevelIndex;
        public string LevelName        = "Unnamed Level";
        [TextArea(2, 3)]
        public string Description;
        public Sprite Thumbnail;

        [Header("Difficulty Tag  (shown on map node)")]
        public string DifficultyLabel  = "Normal";
        public Color  DifficultyColor  = new Color(0.3f, 0.8f, 0.3f);

        [Header("Scene")]
        public string CombatSceneName  = "CombatStage";

        [Header("Rewards")]
        public LevelRewardSO Reward;

        [Header("Map Position  (world-space XZ on the map plane)")]
        public Vector2 MapPosition     = Vector2.zero;

        [Header("Unlock")]
        public bool UnlockedByDefault  = false;
        /// <summary>Level indices unlocked when this one is completed.</summary>
        public int[] UnlocksLevels     = new int[0];
    }
}
