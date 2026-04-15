using UnityEngine;

namespace RPG.Data
{
    public enum AbilityType { Physical, Magical, Support }
    public enum AbilityTarget { SingleEnemy, SingleAlly, Self, AllEnemies, AOE }

    [CreateAssetMenu(fileName = "Ability", menuName = "RPG/Ability")]
    public class AbilitySO : ScriptableObject
    {
        [Header("Identity")]
        public string AbilityName = "Attack";
        [TextArea(2, 4)] public string Description;
        public Sprite Icon;

        [Header("Classification")]
        public AbilityType Type = AbilityType.Physical;
        public AbilityTarget Target = AbilityTarget.SingleEnemy;

        [Header("Range")]
        public int Range = 1;
        public int AOERadius = 0;

        [Header("Cost")]
        public int MPCost = 0;

        [Header("Power")]
        public float DamageMultiplier = 1.0f;
        public int FlatDamage = 0;
        public float HealMultiplier = 0f;
        public int FlatHeal = 0;

        [Header("Special Flags")]
        public bool ApplyDefendBuff = false;

        [Header("VFX")]
        public string VFXKey = "attack";
        public Color EffectColor = Color.white;

        [Header("Audio")]
        public AudioClip SFX;
    }
}
