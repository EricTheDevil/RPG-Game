// Legacy file — renamed to avoid conflict with RPG.Data.UnitStatsSO.
using UnityEngine;

public class LegacyUnitStatsSO : ScriptableObject
{
    [Header("General")]
    public string UnitName;
    public float MaxHealth = 100f;

    [Header("Combat")]
    public float AttackRange = 2f;
    public float AttackSpeed = 1.0f;
    public float Damage      = 10f;
    public float MoveSpeed   = 3.5f;
}
