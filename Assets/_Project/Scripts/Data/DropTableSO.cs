using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Drop entry — one card in the table with an optional weight override
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class DropEntry
    {
        public EventCardSO Card;

        [Tooltip("0 = use the card's rarity weight automatically.")]
        public int WeightOverride = 0;

        [Tooltip("Minimum number of this card guaranteed per roll (usually 0).")]
        public int MinGuaranteed  = 0;

        public int EffectiveWeight =>
            WeightOverride > 0 ? WeightOverride : EventCardSO.RarityWeight(Card.Rarity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DropTableSO  — a weighted pool that yields EventCardSO picks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines which cards can drop from a given encounter tier.
    ///
    /// Usage:
    ///   var drops = table.Roll(count: 3, rng: someRandom);
    ///
    /// Scalability:
    ///   • Create one asset per tier: DropTable_Tier1, DropTable_Elite, DropTable_Boss …
    ///   • Adjust WeightOverride per entry to tune per-table drop chances.
    ///   • Morale modifier shifts all Uncommon+ weights at runtime.
    ///
    /// Create via  Assets > Create > RPG > Drop Table
    /// </summary>
    [CreateAssetMenu(fileName = "DropTable_", menuName = "RPG/Drop Table")]
    public class DropTableSO : ScriptableObject
    {
        [Header("Pool")]
        public List<DropEntry> Entries = new();

        [Header("Roll Settings")]
        [Tooltip("How many cards are drawn per roll call (e.g. after combat).")]
        [Range(1, 5)]
        public int CardsPerRoll = 2;

        [Tooltip("Maximum copies of the same non-duplicate card in a single roll.")]
        public int MaxDuplicatesPerRoll = 1;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Roll <paramref name="count"/> cards from this table.
        /// Respects MaxDuplicatesPerRoll and CanDuplicate flags.
        /// Morale shifts Uncommon+ weights: each morale point above 5 = +10% weight.
        /// </summary>
        public List<EventCardSO> Roll(int count = -1, int morale = 5)
        {
            int drawCount = count > 0 ? count : CardsPerRoll;
            var result    = new List<EventCardSO>(drawCount);
            var drawn     = new Dictionary<EventCardSO, int>();

            // Build weighted list with morale modifier
            float moraleBonus = 1f + (morale - 5) * 0.10f;  // ±10% per morale point from 5
            var weighted = new List<(EventCardSO card, int weight)>();

            foreach (var entry in Entries)
            {
                if (entry.Card == null) continue;

                // Guaranteed entries are added to result first
                for (int g = 0; g < entry.MinGuaranteed; g++)
                    TryAdd(entry.Card, result, drawn, drawCount);

                int w = entry.EffectiveWeight;
                // Morale bonus applies to Uncommon and above
                if (entry.Card.Rarity >= CardRarity.Uncommon)
                    w = Mathf.RoundToInt(w * moraleBonus);

                if (w > 0) weighted.Add((entry.Card, w));
            }

            int attempts = drawCount * weighted.Count * 2 + 10;
            while (result.Count < drawCount && attempts-- > 0)
            {
                var card = WeightedRandom(weighted);
                if (card != null)
                    TryAdd(card, result, drawn, drawCount);
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void TryAdd(EventCardSO card,
                            List<EventCardSO> result,
                            Dictionary<EventCardSO, int> drawn,
                            int maxCount)
        {
            if (result.Count >= maxCount) return;

            drawn.TryGetValue(card, out int existing);
            int cap = card.CanDuplicate ? maxCount : MaxDuplicatesPerRoll;
            if (existing >= cap) return;

            result.Add(card);
            drawn[card] = existing + 1;
        }

        private static EventCardSO WeightedRandom(List<(EventCardSO card, int weight)> pool)
        {
            if (pool.Count == 0) return null;

            int total = 0;
            foreach (var (_, w) in pool) total += w;
            if (total <= 0) return null;

            int roll = UnityEngine.Random.Range(0, total);
            int acc  = 0;
            foreach (var (card, w) in pool)
            {
                acc += w;
                if (roll < acc) return card;
            }
            return pool[^1].card;
        }
    }
}
