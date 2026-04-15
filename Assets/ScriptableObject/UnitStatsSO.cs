using UnityEngine;

public class Unit2StatsSO : ScriptableObject
{
    [Header("General")]
    public string UnitName;
    public float MaxHealth = 100f;
    
    [Header("Combat")]
    public float AttackRange = 2f; // Melee = ~1.5f, Ranged = ~5f+
    public float AttackSpeed = 1.0f; // Attacks per second
    public float Damage = 10f;
    public float MoveSpeed = 3.5f;
}