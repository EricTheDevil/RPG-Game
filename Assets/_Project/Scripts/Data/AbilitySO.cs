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
        [Range(1, 10)] public int Range = 1;
        [Range(0, 5)]  public int AOERadius = 0;

        [Header("Cost")]
        [Range(0, 999)] public int MPCost = 0;

        [Header("Power")]
        [Range(0f, 10f)] public float DamageMultiplier = 1.0f;
        [Range(0, 999)]  public int   FlatDamage = 0;
        [Range(0f, 5f)]  public float HealMultiplier = 0f;
        [Range(0, 999)]  public int   FlatHeal = 0;

        [Header("Special Flags")]
        public bool ApplyDefendBuff = false;

        [Header("VFX")]
        public string VFXKey = "attack";
        public Color EffectColor = Color.white;

        [Header("Audio")]
        public AudioClip SFX;
    }
}
