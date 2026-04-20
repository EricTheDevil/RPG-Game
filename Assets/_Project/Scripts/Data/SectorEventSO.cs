using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────────────────────────────────

    public enum SectorEventCategory
    {
        Combat,     // Fight — loads CombatStage
        Dilemma,    // Multi-choice narrative event (Field Decision)
        Shop,       // Opens shop overlay
        Rest,       // Heals hero + morale
        Treasure,   // Resource windfall
        LevelUp,    // Stat upgrade overlay
        Shrine,     // Unique: grants a permanent Class XP bonus
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ChoiceOutcome  — one branch of a Dilemma event
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class SectorChoice
    {
        [Tooltip("Short label shown on the button, e.g. 'Save the family'.")]
        public string Label = "Option A";

        [TextArea(2, 3)]
        [Tooltip("Flavour text shown below the label.")]
        public string FlavorText = "";

        [Tooltip("Resource changes applied when this choice is made.")]
        public ResourceDelta ResourceDelta;

        [Tooltip("HP change as fraction of hero MaxHP. Negative = damage. E.g. -0.10 = lose 10%.")]
        [Range(-1f, 1f)]
        public float HeroHPDelta = 0f;

        [Tooltip("Class XP awarded to the hero's current class on this choice.")]
        [Range(0, 200)]
        public int ClassXPReward = 0;

        [Tooltip("If true, only show this option when Commander has the required class level.")]
        public bool RequiresClassLevel = false;
        [Range(0, 10)]
        public int  MinClassLevel = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SectorEventSO  — atomic event placed on a local sector tile
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines one event that can appear on a local sector tile.
    ///
    /// Design rules:
    ///   • Category drives resolution: Combat loads CombatStage, Dilemma shows choice panel, etc.
    ///   • For Dilemma events: 2-3 Choices each with a ResourceDelta + HP delta.
    ///   • For Combat/Elite: SpawnConfig + DifficultyScale define the encounter.
    ///   • ClassXPReward on choices ties exploration to the class progression system.
    ///   • Weight controls how often this event appears during procedural generation.
    ///
    /// Create via  Assets > Create > RPG > Sector Event
    /// </summary>
    [CreateAssetMenu(fileName = "SectorEvent_", menuName = "RPG/Sector Event")]
    public class SectorEventSO : ScriptableObject
    {
        [Header("Identity")]
        public string              EventName   = "Unnamed Event";
        [TextArea(2, 4)]
        public string              Prompt      = "What do you do?";
        public Sprite              Icon;
        public SectorEventCategory Category    = SectorEventCategory.Dilemma;
        public CardRarity          Rarity      = CardRarity.Common;

        [Header("Weight (procedural generation)")]
        [Tooltip("Base spawn weight. Higher = appears more often. 0 = disabled.")]
        [Range(0, 200)]
        public int Weight = 100;

        [Header("Dilemma — Choices (2–3)")]
        [Tooltip("Shown as buttons. Used only when Category == Dilemma.")]
        public List<SectorChoice> Choices = new();

        [Header("Combat / Elite")]
        [Tooltip("Encounter definition. If null, CombatManager uses its own defaults.")]
        public UnitSpawnConfig SpawnConfig;
        [Range(0.5f, 5f)]
        public float DifficultyScale = 1f;
        [Tooltip("Resource bonus granted to the player on victory.")]
        public ResourceDelta VictoryGrant;
        [Tooltip("Class XP awarded to hero's current class on combat victory.")]
        [Range(0, 300)]
        public int CombatClassXPReward = 50;

        [Header("Non-Combat Instant Outcomes (Rest / Treasure / Shrine)")]
        [Tooltip("HP restored as fraction of hero MaxHP.")]
        [Range(0f, 1f)]
        public float HealFraction   = 0f;
        [Tooltip("Flat resource grant applied immediately.")]
        public ResourceDelta InstantGrant;
        [Tooltip("Class XP bonus granted at a Shrine.")]
        [Range(0, 500)]
        public int ShrineClassXP = 100;

        [Header("Shop")]
        [Range(1, 6)]
        public int ShopOfferCount = 3;

        // ── Helpers ───────────────────────────────────────────────────────────

        public static int RarityWeight(CardRarity r) => r switch
        {
            CardRarity.Common    => 100,
            CardRarity.Uncommon  =>  40,
            CardRarity.Rare      =>  12,
            CardRarity.Epic      =>   3,
            CardRarity.Legendary =>   1,
            _                    => 100,
        };

        public int EffectiveWeight => Weight > 0 ? Weight : RarityWeight(Rarity);
    }
}
