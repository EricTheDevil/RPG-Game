using System;
using UnityEngine;

namespace RPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────────────────────────────────

    public enum CardType
    {
        Combat,     // Triggers an autobattle encounter next
        Elite,      // Harder combat, better rewards
        Heal,       // Restore HP/Rations between fights
        Rest,       // Small buff + morale gain
        Shop,       // Spend Gold / Scrap to buy buffs
        Treasure,   // Flat resource windfall (rare)
        LevelUp,    // Upgrade a stat or gain a new ability (very rare)
        Curse,      // Negative event — forced into deck by some Elite rewards
    }

    public enum CardRarity
    {
        Common,     // Base weight 100 — shows up constantly
        Uncommon,   //            40
        Rare,       //            12
        Epic,       //             3
        Legendary,  //             1
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Resource cost / grant payload (used by both EventCardSO and DropTableSO)
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ResourceDelta
    {
        [Tooltip("Gold gained (+) or spent (-) when this card is played.")]
        public int Gold;
        [Tooltip("Scrap gained or spent.")]
        public int Scrap;
        [Tooltip("Rations gained or spent.")]
        public int Rations;
        [Tooltip("Morale gained or spent.")]
        public int Morale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EventCardSO  — the atomic unit of the FTL-style event deck
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines one event card the player can hold and play between battles.
    ///
    /// Design rules:
    ///   • CardType drives the gameplay outcome (scene load, stat change, etc.).
    ///   • ResourceCost is checked before the card can be played.
    ///   • ResourceGrant is applied when the card resolves.
    ///   • SpawnConfig overrides the encounter for Combat / Elite cards.
    ///   • HealAmount / MoraleAmount drive Heal / Rest outcomes directly.
    ///   • CanDuplicate: some cards (Curse) can appear multiple times in hand.
    ///
    /// Create via  Assets > Create > RPG > Event Card
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "RPG/Event Card")]
    public class EventCardSO : ScriptableObject
    {
        [Header("Identity")]
        public string      CardName    = "Unnamed Card";
        [TextArea(2, 4)]
        public string      Description = "";
        public Sprite      Art;                 // card illustration
        public CardType    Type        = CardType.Combat;
        public CardRarity  Rarity      = CardRarity.Common;

        [Header("Presentation")]
        public Color       AccentColor = new Color(0.8f, 0.7f, 0.2f);
        [Tooltip("Short tag shown on the card badge, e.g. 'COMBAT', 'HEAL', 'ELITE'.")]
        public string      TypeLabel   = "COMBAT";

        [Header("Cost (paid when card is played)")]
        public ResourceDelta ResourceCost;

        [Header("Grant (received when card resolves)")]
        public ResourceDelta ResourceGrant;

        [Header("Combat / Elite cards")]
        [Tooltip("If set, overrides the default combat encounter for this card.")]
        public UnitSpawnConfig SpawnConfig;
        [Tooltip("Multiplier on enemy stats for difficulty scaling (1 = normal).")]
        [Range(0.5f, 3f)]
        public float DifficultyScale = 1f;

        [Header("Heal / Rest cards")]
        [Tooltip("HP restored as a fraction of max HP (0–1).")]
        [Range(0f, 1f)]
        public float HealFraction    = 0f;
        [Tooltip("Flat morale bonus on resolution.")]
        public int   MoraleBonus     = 0;

        [Header("Shop cards")]
        [Tooltip("Number of buff choices offered in the shop.")]
        [Range(1, 6)]
        public int   ShopOfferCount  = 3;

        [Header("LevelUp cards")]
        [Tooltip("Stat points awarded on use.")]
        public int   StatPointGrant  = 1;

        [Header("Deck rules")]
        [Tooltip("Player can hold more than one copy. True for Curses, false for most others.")]
        public bool  CanDuplicate    = false;
        [Tooltip("Card is consumed (removed from hand) after being played.")]
        public bool  SingleUse       = true;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Base drop weight before rarity modifier is applied.</summary>
        public static int RarityWeight(CardRarity r) => r switch
        {
            CardRarity.Common    => 100,
            CardRarity.Uncommon  =>  40,
            CardRarity.Rare      =>  12,
            CardRarity.Epic      =>   3,
            CardRarity.Legendary =>   1,
            _                    => 100,
        };

        public bool CanAfford(PlayerResources res) =>
            res.Gold    >= -ResourceCost.Gold    &&
            res.Scrap   >= -ResourceCost.Scrap   &&
            res.Rations >= -ResourceCost.Rations &&
            res.Morale  >= -ResourceCost.Morale;

        // Negative cost values mean "spending"; positive means "free bonus on play"
        public bool HasCost =>
            ResourceCost.Gold    < 0 ||
            ResourceCost.Scrap   < 0 ||
            ResourceCost.Rations < 0 ||
            ResourceCost.Morale  < 0;
    }
}
