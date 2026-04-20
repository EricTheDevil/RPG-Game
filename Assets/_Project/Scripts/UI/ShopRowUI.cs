using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// One row in the shop overlay. Wire to a prefab with:
    ///   Icon (Image), NameText (TMP), StatsText (TMP), PriceText (TMP), BuyButton (Button).
    /// </summary>
    public class ShopRowUI : MonoBehaviour
    {
        [Header("References")]
        public Image           Icon;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI StatsText;
        public TextMeshProUGUI PriceText;
        public Button          BuyButton;

        private BuffSO       _buff;
        private int          _goldCost;
        private int          _scrapCost;
        private ShopOverlayUI _owner;

        public void Setup(BuffSO buff, int goldCost, int scrapCost, ShopOverlayUI owner)
        {
            _buff      = buff;
            _goldCost  = goldCost;
            _scrapCost = scrapCost;
            _owner     = owner;

            if (NameText)  NameText.text  = buff.BuffName;
            if (StatsText) StatsText.text = BuildStatSummary(buff);
            if (PriceText) PriceText.text = $"{goldCost}G  /  {scrapCost}S";

            if (Icon)
            {
                Icon.gameObject.SetActive(buff.Icon != null);
                if (buff.Icon) Icon.sprite = buff.Icon;
            }

            // Rarity tint on the button background
            if (BuyButton)
            {
                var colors = BuyButton.colors;
                colors.normalColor = buff.Rarity switch
                {
                    BuffRarity.Epic  => new Color(0.75f, 0.35f, 1f, 0.25f),
                    BuffRarity.Rare  => new Color(0.25f, 0.75f, 1f, 0.25f),
                    _                => new Color(0.7f,  0.7f,  0.7f, 0.15f),
                };
                BuyButton.colors = colors;

                BuyButton.onClick.RemoveAllListeners();
                BuyButton.onClick.AddListener(OnBuyClicked);

                RefreshAffordability();
            }
        }

        void OnBuyClicked()
        {
            _owner?.TryBuy(_buff, _goldCost, _scrapCost, BuyButton);
        }

        public void RefreshAffordability()
        {
            if (BuyButton == null) return;
            var res = RPG.Core.GameSession.Instance?.Resources;
            if (res == null) return;
            BuyButton.interactable = res.Gold >= _goldCost || res.Scrap >= _scrapCost;
        }

        static string BuildStatSummary(BuffSO b)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (b.BonusAttack      != 0) parts.Add($"ATK {S(b.BonusAttack)}");
            if (b.BonusDefense     != 0) parts.Add($"DEF {S(b.BonusDefense)}");
            if (b.BonusMaxHP       != 0) parts.Add($"HP {S(b.BonusMaxHP)}");
            if (b.BonusMaxMP       != 0) parts.Add($"MP {S(b.BonusMaxMP)}");
            if (b.BonusMagicAttack != 0) parts.Add($"MAG {S(b.BonusMagicAttack)}");
            if (b.BonusSpeed       != 0) parts.Add($"SPD {S(b.BonusSpeed)}");
            float crit = b.BonusCritChance * 100f;
            if (crit > 0.01f)            parts.Add($"CRIT +{crit:F0}%");
            return string.Join("  ", parts);
        }

        static string S(int v) => v >= 0 ? $"+{v}" : $"{v}";
    }
}
