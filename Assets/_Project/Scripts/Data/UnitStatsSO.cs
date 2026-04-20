using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    [CreateAssetMenu(fileName = "UnitStats", menuName = "RPG/Unit Stats")]
    public class UnitStatsSO : ScriptableObject
    {
        [Header("Identity")]
        public string UnitName  = "Unit";
        public string ClassName = "Warrior";
        public Sprite Portrait;
        public Color  UnitColor = Color.white;

        [Header("Traits  (used by TraitSystem for TFT synergy bonuses)")]
        [Tooltip("E.g. 'Warrior', 'Mage', 'Hero', 'Knight'. Match exactly with TraitBonus definitions.")]
        public List<string> Traits = new();

        [Header("Base Stats")]
        [Range(1, 9999)] public int MaxHP        = 100;
        [Range(0, 999)]  public int MaxMP        = 50;
        [Range(0, 999)]  public int Attack       = 10;
        [Range(0, 999)]  public int Defense      = 5;
        [Range(0, 999)]  public int MagicAttack  = 8;
        [Range(0, 999)]  public int MagicDefense = 5;
        [Range(1, 99)]   public int Speed        = 8;
        [Range(1, 20)]   public int Movement     = 3;

        [Header("Crit")]
        [Range(0f, 1f)]  public float BaseCritChance     = 0.10f;
        [Range(1f, 5f)]  public float BaseCritMultiplier = 1.60f;

        [Header("Rewards")]
        [Range(0, 9999)] public int ExpReward = 50;
    }
}
