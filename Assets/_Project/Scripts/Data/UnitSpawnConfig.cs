using System;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Describes one unit slot in a battle lineup.
    /// CombatManager reads an array of these to spawn both teams.
    /// </summary>
    [Serializable]
    public class UnitSpawnEntry
    {
        [Tooltip("Prefab must have a Unit component (HeroUnit or EnemyUnit).")]
        public RPG.Units.Unit Prefab;
        public UnitStatsSO    Stats;
        public AbilitySO[]    Abilities = new AbilitySO[0];

        [Tooltip("Grid cell for this unit. Overrides CombatManager default spawn arrays.")]
        public Vector2Int     GridCell;
    }

    /// <summary>
    /// Data asset that fully describes a battle encounter.
    /// Drop one of these on CombatManager to configure the fight without code.
    ///
    /// Create via  Assets > Create > RPG > Unit Spawn Config
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "RPG/Unit Spawn Config")]
    public class UnitSpawnConfig : ScriptableObject
    {
        [Header("Player Team")]
        public UnitSpawnEntry[] PlayerUnits = new UnitSpawnEntry[0];

        [Header("Enemy Team")]
        public UnitSpawnEntry[] EnemyUnits  = new UnitSpawnEntry[0];
    }
}
