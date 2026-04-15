using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RPG.Data;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Renders one EventCardSO in the hand.
    /// Handles hover scale, click selection, and affordability dimming.
    ///
    /// Call Setup() then wire OnSelected to respond to player choice.
    /// </summary>
    public class EventCardUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        public Image           CardArt;
        public Image           CardFrame;         // tinted by rarity
        public Image           TypeIcon;          // optional icon per CardType
        public TextMeshProUGUI TypeLabel;
        public TextMeshProUGUI CardNameText;
        public TextMeshProUGUI DescriptionText;
        public TextMeshProUGUI RarityLabel;
        public TextMeshProUGUI CostLabel;         // shows resource cost if any
        public CanvasGroup     CanvasGroup;

        [Header("Rarity Frame Colors")]
        public Color CommonColor    = new Color(0.55f, 0.55f, 0.60f);
        public Color UncommonColor  = new Color(0.15f, 0.75f, 0.30f);
        public Color RareColor      = new Color(0.20f, 0.50f, 1.00f);
        public Color EpicColor      = new Color(0.70f, 0.20f, 1.00f);
        public Color LegendaryColor = new Color(1.00f, 0.75f, 0.10f);

        [Header("Hover Animation")]
        public float HoverScale    = 1.12f;
        public float AnimSpeed     = 8f;

        // ── Runtime ───────────────────────────────────────────────────────────
        public EventCardSO Card { get; private set; }
        public event Action<EventCardUI> OnSelected;

        private bool   _hovered;
        private bool   _affordable = true;
        private Vector3 _baseScale;
        private RectTransform _rect;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _rect      = GetComponent<RectTransform>();
            _baseScale = _rect != null ? _rect.localScale : Vector3.one;
        }

        private void Update()
        {
            if (_rect == null) return;
            float target = _hovered && _affordable ? HoverScale : 1f;
            float current = _rect.localScale.x / _baseScale.x;
            float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * AnimSpeed);
            _rect.localScale = _baseScale * next;
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Setup(EventCardSO card, PlayerResources resources)
        {
            Card = card;
            if (card == null) return;

            if (CardNameText)    CardNameText.text    = card.CardName;
            if (DescriptionText) DescriptionText.text = card.Description;
            if (TypeLabel)       TypeLabel.text       = card.TypeLabel;
            if (RarityLabel)     RarityLabel.text     = card.Rarity.ToString().ToUpper();

            // Frame tint = rarity color
            Color rarityCol = RarityColor(card.Rarity);
            if (CardFrame) CardFrame.color = rarityCol;
            if (RarityLabel) RarityLabel.color = rarityCol;

            // Art
            if (CardArt)
            {
                CardArt.gameObject.SetActive(card.Art != null);
                if (card.Art != null) CardArt.sprite = card.Art;
            }

            // Cost label
            if (CostLabel)
            {
                if (card.HasCost)
                {
                    var c = card.ResourceCost;
                    var parts = new System.Collections.Generic.List<string>();
                    if (c.Gold    < 0) parts.Add($"{-c.Gold} Gold");
                    if (c.Scrap   < 0) parts.Add($"{-c.Scrap} Scrap");
                    if (c.Rations < 0) parts.Add($"{-c.Rations} Rations");
                    if (c.Morale  < 0) parts.Add($"{-c.Morale} Morale");
                    CostLabel.text = "Cost: " + string.Join("  ", parts);
                }
                else
                {
                    CostLabel.text = "";
                }
            }

            // Affordability
            _affordable = resources == null || card.CanAfford(resources);
            if (CanvasGroup)
                CanvasGroup.alpha = _affordable ? 1f : 0.45f;
        }

        // ── Events ────────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _) => _hovered = true;
        public void OnPointerExit(PointerEventData _)  => _hovered = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_affordable) return;
            OnSelected?.Invoke(this);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Color RarityColor(CardRarity r) => r switch
        {
            CardRarity.Common    => CommonColor,
            CardRarity.Uncommon  => UncommonColor,
            CardRarity.Rare      => RareColor,
            CardRarity.Epic      => EpicColor,
            CardRarity.Legendary => LegendaryColor,
            _                    => CommonColor,
        };
    }
}
