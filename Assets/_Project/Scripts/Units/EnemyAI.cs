// EnemyAI.cs — DEPRECATED
// AI is now handled by UnitAI.cs, which is auto-added as a component to every Unit.
// This file is retained to avoid breaking existing prefab references.
// DO NOT add new logic here. Extend UnitAI instead.

using UnityEngine;

namespace RPG.Units
{
    /// <summary>
    /// Stub kept for backwards-compatibility with pre-existing prefabs.
    /// All AI logic lives in UnitAI. This component does nothing.
    /// </summary>
    [System.Obsolete("Use UnitAI instead. This stub exists only for prefab compatibility.")]
    public class EnemyAI : MonoBehaviour { }
}
