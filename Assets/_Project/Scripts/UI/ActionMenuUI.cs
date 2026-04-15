// ActionMenuUI.cs
// In autobattle mode this panel is NOT used — combat is fully automated.
// Kept in the project for a future "Planning Phase" feature (pre-combat unit positioning).
// All CombatManager method calls are stubbed out so it compiles cleanly.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// Legacy player-action menu — disabled in autobattle.
    /// Kept for future Planning Phase implementation.
    /// </summary>
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Buttons  (not wired in autobattle)")]
        public Button MoveButton;
        public Button AttackButton;
        public Button DefendButton;
        public Button SpecialButton;
        public Button EndTurnButton;

        [Header("Labels")]
        public TextMeshProUGUI MoveLabel;
        public TextMeshProUGUI AttackLabel;
        public TextMeshProUGUI DefendLabel;
        public TextMeshProUGUI SpecialLabel;
        public TextMeshProUGUI MPLabel;
        public TextMeshProUGUI SpecialMPCostLabel;

        private void Awake()
        {
            // Autobattle: hide immediately and do nothing
            gameObject.SetActive(false);
        }

        /// <summary>Called externally if a planning-phase is implemented.</summary>
        public void Refresh(Unit unit) { }
    }
}
