using System;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// The four run resources.  Stored on GameSession, serialized by SaveSystem.
    /// All values are clamped to [0, cap] wherever they are modified.
    /// </summary>
    [Serializable]
    public class PlayerResources
    {
        [Tooltip("Primary currency — looted from enemies and treasures.")]
        public int Gold    = 0;

        [Tooltip("Crafting material — salvaged from destroyed enemies.")]
        public int Scrap   = 0;

        [Tooltip("Consumed on Rest cards; reaching 0 inflicts Morale loss.")]
        public int Rations = 3;

        [Tooltip("Affects hero stats and buff card quality. Clamp 0–10.")]
        public int Morale  = 5;

        // ── Caps ──────────────────────────────────────────────────────────────
        public const int MaxRations = 10;
        public const int MaxMorale  = 10;

        // ── Mutation ──────────────────────────────────────────────────────────

        /// <summary>Apply a ResourceDelta, clamping to valid ranges.</summary>
        public void Apply(ResourceDelta delta)
        {
            Gold    = Mathf.Max(0, Gold    + delta.Gold);
            Scrap   = Mathf.Max(0, Scrap   + delta.Scrap);
            Rations = Mathf.Clamp(Rations  + delta.Rations, 0, MaxRations);
            Morale  = Mathf.Clamp(Morale   + delta.Morale,  0, MaxMorale);
        }

        public override string ToString() =>
            $"Gold:{Gold}  Scrap:{Scrap}  Rations:{Rations}  Morale:{Morale}";
    }
}
