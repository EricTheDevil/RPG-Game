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
        public int MaxHP        = 100;
        public int MaxMP        = 50;
        public int Attack       = 10;
        public int Defense      = 5;
        public int MagicAttack  = 8;
        public int MagicDefense = 5;
        public int Speed        = 8;
        public int Movement     = 3;

        [Header("Rewards")]
        public int ExpReward = 50;
    }
}
