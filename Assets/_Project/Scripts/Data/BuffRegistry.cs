using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Master list of every buff available in the game.
    /// BuffSelectionUI draws from this pool to offer 3 random choices after combat.
    ///
    /// Create via  Assets > Create > RPG > Buff Registry
    /// </summary>
    [CreateAssetMenu(fileName = "BuffRegistry", menuName = "RPG/Buff Registry")]
    public class BuffRegistry : ScriptableObject
    {
        public BuffSO[] Buffs = new BuffSO[0];

        /// <summary>
        /// Returns <paramref name="count"/> unique random buffs from the pool,
        /// weighted so Rare appears half as often as Common and Epic a quarter.
        /// Falls back gracefully if the pool is smaller than count.
        /// </summary>
        /// <summary>Find a buff by its asset name. Used by SaveSystem when restoring a run.</summary>
        public BuffSO FindByName(string buffName)
            => System.Array.Find(Buffs, b => b != null && b.name == buffName);

        public List<BuffSO> PickRandom(int count)
        {
            var pool = Buffs.Where(b => b != null).ToList();
            if (pool.Count == 0) return new List<BuffSO>();

            // Build weighted list
            var weighted = new List<BuffSO>();
            foreach (var b in pool)
            {
                int weight = b.Rarity switch
                {
                    BuffRarity.Common => 4,
                    BuffRarity.Rare   => 2,
                    BuffRarity.Epic   => 1,
                    _                  => 4
                };
                for (int i = 0; i < weight; i++) weighted.Add(b);
            }

            // Shuffle and pick unique
            var result  = new List<BuffSO>();
            var picked  = new HashSet<BuffSO>();
            int attempts = weighted.Count * 3;

            while (result.Count < count && attempts-- > 0)
            {
                var candidate = weighted[Random.Range(0, weighted.Count)];
                if (picked.Add(candidate))
                    result.Add(candidate);
            }

            return result;
        }
    }
}
