using System.Linq;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Single source of truth for all levels in the game.
    /// Assign one LevelDataSO per entry; the MapSelectorUI reads this
    /// at runtime and spawns nodes dynamically — no scene rebuilds needed
    /// when you add levels.
    ///
    /// Create via  Assets > Create > RPG > Level Registry
    /// </summary>
    [CreateAssetMenu(fileName = "LevelRegistry", menuName = "RPG/Level Registry")]
    public class LevelRegistry : ScriptableObject
    {
        public LevelDataSO[] Levels = new LevelDataSO[0];

        /// <summary>Returns the LevelDataSO with the given index, or null.</summary>
        public LevelDataSO GetLevel(int index) =>
            Levels.FirstOrDefault(l => l != null && l.LevelIndex == index);

        /// <summary>All levels that unlock when <paramref name="completedIndex"/> is finished.</summary>
        public LevelDataSO[] GetUnlockedBy(int completedIndex)
        {
            var completed = GetLevel(completedIndex);
            if (completed == null) return new LevelDataSO[0];
            return completed.UnlocksLevels
                .Select(i => GetLevel(i))
                .Where(l => l != null)
                .ToArray();
        }
    }
}
