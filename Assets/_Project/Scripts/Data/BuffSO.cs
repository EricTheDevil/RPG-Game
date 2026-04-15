using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// A single roguelike buff that can be offered as a reward after combat.
    /// Buffs modify flat stat values additively and stack indefinitely across runs.
    ///
    /// Create via  Assets > Create > RPG > Buff
    /// </summary>
    [CreateAssetMenu(fileName = "Buff_New", menuName = "RPG/Buff")]
    public class BuffSO : ScriptableObject
    {
        [Header("Identity")]
        public string BuffName        = "Unnamed Buff";
        [TextArea(2, 3)]
        public string Description     = "";
        public Sprite Icon;
        public Color  AccentColor     = new Color(0.8f, 0.6f, 1f);

        [Header("Rarity")]
        public BuffRarity Rarity      = BuffRarity.Common;

        [Header("Stat Modifiers  (flat additive, stacks each pickup)")]
        public int BonusMaxHP         = 0;
        public int BonusMaxMP         = 0;
        public int BonusAttack        = 0;
        public int BonusDefense       = 0;
        public int BonusMagicAttack   = 0;
        public int BonusMagicDefense  = 0;
        public int BonusSpeed         = 0;
        public int BonusMovement      = 0;
    }

    public enum BuffRarity { Common, Rare, Epic }
}
