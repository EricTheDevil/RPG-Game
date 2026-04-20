using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// Shop overlay shown when the player plays a Shop event card.
    ///
    /// Offers ShopOfferCount random buffs from the BuffRegistry; prices by rarity.
    /// The player can buy any number they can afford, then press Close.
    ///
    /// Pricing:
    ///   Common  →  30 Gold
    ///   Rare    →  55 Gold
    ///   Epic    →  90 Gold  (or 40 Scrap)
    ///
    /// Usage:
    ///   1. Add this component to a full-screen Canvas overlay GO.
    ///   2. Wire RowContainer (Vertical Layout Group), RowPrefab, resource texts, CloseButton.
    ///   3. Call Open(card) from EventDeckUI; subscribe to OnClosed to refresh.
    /// </summary>
    public class ShopOverlayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Layout")]
        public Transform         RowContainer;   // Vertical Layout Group
        public GameObject        RowPrefab;      // ShopRowUI prefab

        [Header("Resource Display")]
        public TextMeshProUGUI   GoldText;
        public TextMeshProUGUI   ScrapText;

        [Header("Title / Footer")]
        public TextMeshProUGUI   TitleText;
        public Button            CloseButton;

        [Header("Data")]
        public BuffRegistry      Registry;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action OnClosed;

        // ── Pricing table ─────────────────────────────────────────────────────
        static int GoldPrice(BuffRarity r) => r switch
        {
            BuffRarity.Rare  => 55,
            BuffRarity.Epic  => 90,
            _                => 30,   // Common
        };
        static int ScrapPrice(BuffRarity r) => r switch
        {
            BuffRarity.Rare  => 25,
            BuffRarity.Epic  => 40,
            _                => 12,
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            CloseButton?.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Open the shop for this event card.</summary>
        public void Open(EventCardSO card)
        {
            if (Registry == null || Registry.Buffs == null || Registry.Buffs.Length == 0)
            {
                Debug.LogWarning("[ShopOverlay] No BuffRegistry assigned or registry is empty.");
                Close();
                return;
            }

            int offerCount = card != null ? card.ShopOfferCount : 3;
            if (TitleText) TitleText.text = card != null ? $"Wandering Merchant  —  {card.CardName}" : "Shop";

            gameObject.SetActive(true);
            BuildOffers(offerCount);
            RefreshResources();
        }

        // ── Private ───────────────────────────────────────────────────────────

        void BuildOffers(int count)
        {
            // Clear existing rows
            foreach (Transform child in RowContainer)
                Destroy(child.gameObject);

            var session = GameSession.Instance;
            if (session == null) return;

            // Pick random buffs (no duplicates)
            var pool = new List<BuffSO>(Registry.Buffs);
            var picks = new List<BuffSO>();
            count = Mathf.Min(count, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                picks.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            foreach (var buff in picks)
            {
                var row = Instantiate(RowPrefab, RowContainer);
                row.name = $"ShopRow_{buff.name}";
                var rowUI = row.GetComponent<ShopRowUI>();
                if (rowUI != null)
                    rowUI.Setup(buff, GoldPrice(buff.Rarity), ScrapPrice(buff.Rarity), this);
                else
                    SetupRowFallback(row, buff, session);
            }
        }

        /// Fallback wiring for prefabs that don't have a ShopRowUI component.
        void SetupRowFallback(GameObject row, BuffSO buff, GameSession session)
        {
            var nameLabel  = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var priceLabel = row.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            var buyBtn     = row.transform.Find("BuyButton")?.GetComponent<Button>();
            var icon       = row.transform.Find("Icon")?.GetComponent<Image>();

            int gold  = GoldPrice(buff.Rarity);
            int scrap = ScrapPrice(buff.Rarity);

            if (nameLabel)  nameLabel.text  = buff.BuffName;
            if (priceLabel) priceLabel.text = $"{gold}G / {scrap}S";
            if (icon && buff.Icon) { icon.sprite = buff.Icon; icon.gameObject.SetActive(true); }

            if (buyBtn)
            {
                buyBtn.onClick.AddListener(() => TryBuy(buff, gold, scrap, buyBtn));
                bool canAfford = session.Resources.Gold >= gold || session.Resources.Scrap >= scrap;
                buyBtn.interactable = canAfford;
            }
        }

        /// Called by ShopRowUI or fallback buttons.
        public void TryBuy(BuffSO buff, int goldCost, int scrapCost, Button buyBtn)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            // Prefer Gold; fall back to Scrap
            bool payGold  = session.Resources.Gold  >= goldCost;
            bool payScrap = session.Resources.Scrap >= scrapCost;

            if (!payGold && !payScrap)
            {
                Debug.Log("[ShopOverlay] Cannot afford this buff.");
                return;
            }

            if (payGold)
                session.ApplyResources(new ResourceDelta { Gold = -goldCost });
            else
                session.ApplyResources(new ResourceDelta { Scrap = -scrapCost });

            session.AddBuff(buff);

            // Disable button to prevent double-buy
            if (buyBtn) buyBtn.interactable = false;

            RefreshResources();
            Debug.Log($"[ShopOverlay] Bought {buff.BuffName} for {(payGold ? $"{goldCost}G" : $"{scrapCost}S")}.");
        }

        public void RefreshResources()
        {
            var res = GameSession.Instance?.Resources;
            if (res == null) return;
            if (GoldText)  GoldText.text  = $"Gold: {res.Gold}";
            if (ScrapText) ScrapText.text = $"Scrap: {res.Scrap}";
        }

        void Close()
        {
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }
    }
}
