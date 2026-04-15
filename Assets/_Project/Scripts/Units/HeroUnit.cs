using UnityEngine;

namespace RPG.Units
{
    /// <summary>
    /// The player's Hero unit — class: Hero.
    ///
    /// In autobattle mode the hero acts autonomously via UnitAI.
    /// Abilities and stats are injected by CombatManager from ScriptableObjects.
    /// Subclass this to create distinct hero archetypes (Paladin, Mage, Archer…).
    /// </summary>
    public class HeroUnit : Unit
    {
        protected override void SetupAbilities()
        {
            // Abilities are assigned externally by CombatManager.
            // Override here for hero-specific default loadouts.
        }

        public override void Initialize(Team team)
        {
            base.Initialize(Team.Player);
        }
    }
}
