using UnityEngine;

namespace RPG.Units
{
    /// <summary>
    /// Enemy unit — driven by UnitAI, abilities injected by CombatManager.
    ///
    /// Subclass this to create enemy archetypes with different AI profiles
    /// (e.g. override UnitAI.SpecialAbilityChance or HealTriggerHP).
    /// </summary>
    public class EnemyUnit : Unit
    {
        protected override void SetupAbilities()
        {
            // Abilities are assigned externally by CombatManager.
        }

        public override void Initialize(Team team)
        {
            base.Initialize(Team.Enemy);
        }
    }
}
