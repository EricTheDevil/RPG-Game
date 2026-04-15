using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// A single buff card in the BuffSelectionUI.
    /// Shows name, description, stat deltas, and rarity colour.
    /// </summary>
    public class BuffCardUI : MonoBehaviour
    {
        [Header("Layout")]
        public Image          CardBackground;
        public Image          IconImage;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI RarityText;
        public TextMeshProUGUI DescText;
        public TextMeshProUGUI StatsText;
        public Button         SelectButton;

        // Rarity border colours
        static readonly Color ColCommon = new Color(0.55f, 0.55f, 0.60f);
        static readonly Color ColRare   = new Color(0.25f, 0.55f, 1.00f);
        static readonly Color ColEpic   = new Color(0.65f, 0.20f, 1.00f);

        private BuffSO _buff;
        private Action<BuffSO> _onChosen;

        public void Setup(BuffSO buff, Action<BuffSO> onChosen)
        {
            _buff     = buff;
            _onChosen = onChosen;

            if (NameText)   NameText.text   = buff.BuffName;
            if (DescText)   DescText.text   = buff.Description;
            if (IconImage && buff.Icon) IconImage.sprite = buff.Icon;

            // Rarity label + border tint
            Color rarityCol = buff.Rarity switch
            {
                BuffRarity.Rare => ColRare,
                BuffRarity.Epic => ColEpic,
                _               => ColCommon
            };
            if (RarityText)
            {
                RarityText.text  = buff.Rarity.ToString().ToUpper();
                RarityText.color = rarityCol;
            }
            if (CardBackground) CardBackground.color = new Color(
                rarityCol.r * 0.15f, rarityCol.g * 0.15f, rarityCol.b * 0.25f, 0.97f);

            // Build concise stat delta string
            if (StatsText) StatsText.text = BuildStatLine(buff);

            SelectButton?.onClick.RemoveAllListeners();
            SelectButton?.onClick.AddListener(() => _onChosen?.Invoke(_buff));
        }

        static string BuildStatLine(BuffSO b)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (b.BonusMaxHP        != 0) parts.Add($"HP <color=#88FF88>+{b.BonusMaxHP}</color>");
            if (b.BonusMaxMP        != 0) parts.Add($"MP <color=#88CCFF>+{b.BonusMaxMP}</color>");
            if (b.BonusAttack       != 0) parts.Add($"ATK <color=#FFAA44>+{b.BonusAttack}</color>");
            if (b.BonusDefense      != 0) parts.Add($"DEF <color=#44AAFF>+{b.BonusDefense}</color>");
            if (b.BonusMagicAttack  != 0) parts.Add($"MAG <color=#DD88FF>+{b.BonusMagicAttack}</color>");
            if (b.BonusMagicDefense != 0) parts.Add($"MDEF <color=#AACCFF>+{b.BonusMagicDefense}</color>");
            if (b.BonusSpeed        != 0) parts.Add($"SPD <color=#FFFF66>+{b.BonusSpeed}</color>");
            if (b.BonusMovement     != 0) parts.Add($"MOV <color=#66FFAA>+{b.BonusMovement}</color>");
            return string.Join("  ", parts);
        }
    }
}
