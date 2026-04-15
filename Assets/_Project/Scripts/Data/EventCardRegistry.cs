using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Master list of every EventCardSO in the project.
    /// Used by SaveSystem to restore card hand from saved asset names.
    ///
    /// Create via  Assets > Create > RPG > Event Card Registry
    /// </summary>
    [CreateAssetMenu(fileName = "EventCardRegistry", menuName = "RPG/Event Card Registry")]
    public class EventCardRegistry : ScriptableObject
    {
        public EventCardSO[] Cards = new EventCardSO[0];

        /// <summary>Find a card by its asset name. Used by SaveSystem when restoring a run.</summary>
        public EventCardSO FindByName(string cardName)
            => System.Array.Find(Cards, c => c != null && c.name == cardName);
    }
}
