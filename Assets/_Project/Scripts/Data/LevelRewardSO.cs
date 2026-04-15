using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Defines everything the player earns for completing a level.
    /// Swap this asset per level to give different encounters different rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelReward", menuName = "RPG/Level Reward")]
    public class LevelRewardSO : ScriptableObject
    {
        [Header("Currency / Progression")]
        public int XPReward    = 100;
        public int GoldReward  = 50;

        [Header("Item Drop  (leave ItemName blank for no item)")]
        public string ItemName;
        [TextArea(2, 3)]
        public string ItemDescription;
        public Sprite ItemIcon;

        [Header("Presentation")]
        [TextArea(2, 3)]
        public string FlavorText = "Victory!";
        public Color  AccentColor = new Color(1f, 0.84f, 0f);

        [Header("Audio")]
        public AudioClip FanfareSFX;
    }
}
