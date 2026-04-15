using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// Displays the hero's currently active buffs on the MapSelector screen.
    /// Instantiates one row per unique buff (stacks show a count badge).
    /// Wire to a vertical LayoutGroup panel in the MapSelector scene.
    ///
    /// The panel auto-hides when there are no buffs.
    /// </summary>
    public class BuffStackUI : MonoBehaviour
    {
        [Header("Layout")]
        public Transform    RowContainer;    // Vertical Layout Group
        public GameObject   RowPrefab;       // one row per buff type
        public TextMeshProUGUI HeaderText;   // "Active Blessings" or null

        [Header("Empty state")]
        public GameObject   EmptyLabel;      // shown when no buffs

        private void OnEnable() => Refresh();

        /// <summary>Rebuild the buff list from GameSession.</summary>
        public void Refresh()
        {
            if (RowContainer == null || RowPrefab == null) return;

            // Clear existing rows
            foreach (Transform child in RowContainer)
                Destroy(child.gameObject);

            var session = GameSession.Instance;
            if (session == null || session.ActiveBuffs.Count == 0)
            {
                EmptyLabel?.SetActive(true);
                HeaderText?.gameObject.SetActive(false);
                return;
            }

            EmptyLabel?.SetActive(false);
            HeaderText?.gameObject.SetActive(true);

            // Aggregate stacks
            var counts = new Dictionary<BuffSO, int>();
            foreach (var buff in session.ActiveBuffs)
            {
                if (buff == null) continue;
                counts.TryGetValue(buff, out int c);
                counts[buff] = c + 1;
            }

            foreach (var kv in counts)
            {
                var buff  = kv.Key;
                int count = kv.Value;

                var row = Instantiate(RowPrefab, RowContainer);
                row.name = $"BuffRow_{buff.name}";

                // Name label
                var nameLabel = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameLabel) nameLabel.text = count > 1 ? $"{buff.BuffName}  ×{count}" : buff.BuffName;

                // Rarity color accent
                var rarityTag = row.transform.Find("RarityTag")?.GetComponent<Image>();
                if (rarityTag)
                    rarityTag.color = buff.Rarity switch
                    {
                        BuffRarity.Epic   => new Color(0.75f, 0.35f, 1f),
                        BuffRarity.Rare   => new Color(0.25f, 0.75f, 1f),
                        _                  => new Color(0.7f,  0.7f,  0.7f),
                    };

                // Stat summary
                var statsLabel = row.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
                if (statsLabel) statsLabel.text = BuildStatSummary(buff, count);

                // Icon
                var icon = row.transform.Find("Icon")?.GetComponent<Image>();
                if (icon && buff.Icon) { icon.sprite = buff.Icon; icon.gameObject.SetActive(true); }
                else if (icon)          icon.gameObject.SetActive(false);
            }
        }

        private static string BuildStatSummary(BuffSO b, int stacks)
        {
            var parts = new List<string>();
            if (b.BonusAttack      != 0) parts.Add($"ATK {Sign(b.BonusAttack      * stacks)}");
            if (b.BonusDefense     != 0) parts.Add($"DEF {Sign(b.BonusDefense     * stacks)}");
            if (b.BonusMaxHP       != 0) parts.Add($"HP  {Sign(b.BonusMaxHP       * stacks)}");
            if (b.BonusMaxMP       != 0) parts.Add($"MP  {Sign(b.BonusMaxMP       * stacks)}");
            if (b.BonusMagicAttack != 0) parts.Add($"MAG {Sign(b.BonusMagicAttack * stacks)}");
            if (b.BonusSpeed       != 0) parts.Add($"SPD {Sign(b.BonusSpeed       * stacks)}");
            if (b.BonusMovement    != 0) parts.Add($"MOV {Sign(b.BonusMovement    * stacks)}");
            return string.Join("  ", parts);
        }

        private static string Sign(int v) => v >= 0 ? $"+{v}" : $"{v}";
    }
}
