using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// FTL-style between-battle hub.
    ///
    /// Layout:
    ///   • Top bar       — resource readout (Gold, Scrap, Rations, Morale)
    ///   • Centre        — horizontal scrollable card hand
    ///   • Drop reveal   — new cards earned from the last battle slide in on entry
    ///   • Card detail   — right-side panel shows hovered card details
    ///   • Bottom bar    — "Play Selected Card" button + Main Menu button
    ///
    /// Flow:
    ///   1. On Awake, accept pending drops (animate them into hand).
    ///   2. Player hovers/clicks a card — detail panel updates.
    ///   3. Player presses Play → GameManager.PlayCombatCard / resolves non-combat card.
    /// </summary>
    public class EventDeckUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Card Hand")]
        public Transform       CardContainer;   // Horizontal Layout Group
        public GameObject      CardPrefab;      // must have EventCardUI component
        public ScrollRect      HandScrollRect;

        [Header("Resource Bar")]
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI RationsText;
        public TextMeshProUGUI MoraleText;

        [Header("Detail Panel (right side)")]
        public GameObject      DetailPanel;
        public TextMeshProUGUI DetailName;
        public TextMeshProUGUI DetailDesc;
        public TextMeshProUGUI DetailRarity;
        public TextMeshProUGUI DetailCost;
        public TextMeshProUGUI DetailGrant;
        public Image           DetailArt;

        [Header("Drop Notification")]
        public GameObject      DropBanner;          // "New cards acquired!" banner
        public TextMeshProUGUI DropBannerText;

        [Header("Buttons")]
        public Button          PlayButton;
        public TextMeshProUGUI PlayButtonLabel;
        public Button          MainMenuButton;
        public Button          DiscardButton;        // discard selected card

        [Header("Empty Hand Message")]
        public GameObject      EmptyHandMessage;    // shown when hand is empty

        [Header("Animation")]
        public float           CardDealDelay  = 0.08f;   // stagger per card
        public float           DropRevealTime = 0.5f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private readonly List<EventCardUI> _cardViews = new();
        private EventCardUI                _selected;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Start()
        {
            PlayButton?.onClick.AddListener(OnPlayPressed);
            MainMenuButton?.onClick.AddListener(OnMainMenuPressed);
            DiscardButton?.onClick.AddListener(OnDiscardPressed);

            DetailPanel?.SetActive(false);
            DropBanner?.SetActive(false);
            EmptyHandMessage?.SetActive(false);

            StartCoroutine(InitSequence());
        }

        private IEnumerator InitSequence()
        {
            var session = GameSession.Instance;
            if (session == null) yield break;

            // ── Show drop notification if we earned cards last battle ──────────
            var pending = new List<EventCardSO>(session.PendingDrops);
            if (pending.Count > 0)
            {
                session.AcceptPendingDrops();

                if (DropBanner != null)
                {
                    string names = string.Join(", ", pending.ConvertAll(c => c.CardName));
                    if (DropBannerText) DropBannerText.text = $"Cards acquired: {names}";
                    DropBanner.SetActive(true);
                    yield return new WaitForSecondsRealtime(2.2f);
                    DropBanner.SetActive(false);
                }
            }

            // ── Soft-lock guard: ensure player always has at least one card ────
            if (session.Hand.Count == 0)
            {
                var fallback = GameManager.Instance?.FallbackCombatCard;
                if (fallback != null)
                {
                    session.AddCardToHand(fallback);
                    Debug.Log("[EventDeckUI] Hand was empty — dealt fallback combat card.");
                }
            }

            // ── Build card views ──────────────────────────────────────────────
            yield return BuildHand(session);
            RefreshResourceBar(session.Resources);
            RefreshPlayButton();
        }

        // ── Hand Building ─────────────────────────────────────────────────────

        private IEnumerator BuildHand(GameSession session)
        {
            // Clear stale views
            foreach (var v in _cardViews)
                if (v != null) Destroy(v.gameObject);
            _cardViews.Clear();
            _selected = null;

            var hand = session.Hand;
            bool hasCards = hand.Count > 0;
            EmptyHandMessage?.SetActive(!hasCards);

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card == null) continue;

                var go   = Instantiate(CardPrefab, CardContainer);
                var view = go.GetComponent<EventCardUI>();
                if (view == null) { Destroy(go); continue; }

                view.Setup(card, session.Resources);
                view.OnSelected += OnCardSelected;

                // Stagger deal animation
                var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                _cardViews.Add(view);

                yield return new WaitForSecondsRealtime(CardDealDelay);
                yield return StartCoroutine(FadeIn(cg, 0.2f));
            }
        }

        // ── Card Selection ────────────────────────────────────────────────────

        private void OnCardSelected(EventCardUI view)
        {
            // Deselect previous
            if (_selected != null && _selected != view)
                SetCardSelectedVisual(_selected, false);

            _selected = (_selected == view) ? null : view;
            if (_selected != null)
                SetCardSelectedVisual(_selected, true);

            RefreshDetailPanel(_selected?.Card);
            RefreshPlayButton();
        }

        private void SetCardSelectedVisual(EventCardUI view, bool on)
        {
            if (view == null) return;
            var frame = view.CardFrame;
            if (frame == null) return;
            frame.color = on
                ? Color.white
                : view.GetComponent<EventCardUI>() != null
                    ? view.CardFrame.color
                    : Color.white;

            // Scale bump
            var rect = view.GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = on ? Vector3.one * 1.06f : Vector3.one;
        }

        // ── Resource Bar ──────────────────────────────────────────────────────

        private void RefreshResourceBar(PlayerResources res)
        {
            if (GoldText)    GoldText.text    = $"Gold  {res.Gold}";
            if (ScrapText)   ScrapText.text   = $"Scrap  {res.Scrap}";
            if (RationsText) RationsText.text = $"Rations  {res.Rations}/{PlayerResources.MaxRations}";
            if (MoraleText)  MoraleText.text  = $"Morale  {res.Morale}/{PlayerResources.MaxMorale}";
        }

        // ── Detail Panel ──────────────────────────────────────────────────────

        private void RefreshDetailPanel(EventCardSO card)
        {
            if (DetailPanel == null) return;

            if (card == null) { DetailPanel.SetActive(false); return; }
            DetailPanel.SetActive(true);

            if (DetailName)   DetailName.text   = card.CardName;
            if (DetailDesc)   DetailDesc.text   = card.Description;
            if (DetailRarity) DetailRarity.text = card.Rarity.ToString().ToUpper();
            if (DetailArt)
            {
                DetailArt.gameObject.SetActive(card.Art != null);
                if (card.Art != null) DetailArt.sprite = card.Art;
            }

            if (DetailCost)
            {
                if (card.HasCost)
                {
                    var c = card.ResourceCost;
                    var parts = new List<string>();
                    if (c.Gold    < 0) parts.Add($"<color=#FFD700>{-c.Gold} Gold</color>");
                    if (c.Scrap   < 0) parts.Add($"<color=#AAAAAA>{-c.Scrap} Scrap</color>");
                    if (c.Rations < 0) parts.Add($"<color=#88FF88>{-c.Rations} Rations</color>");
                    if (c.Morale  < 0) parts.Add($"<color=#FF8888>{-c.Morale} Morale</color>");
                    DetailCost.text = "Cost: " + string.Join("  ", parts);
                }
                else DetailCost.text = "";
            }

            if (DetailGrant)
            {
                var g = card.ResourceGrant;
                var parts = new List<string>();
                if (g.Gold    > 0) parts.Add($"<color=#FFD700>+{g.Gold} Gold</color>");
                if (g.Scrap   > 0) parts.Add($"<color=#AAAAAA>+{g.Scrap} Scrap</color>");
                if (g.Rations > 0) parts.Add($"<color=#88FF88>+{g.Rations} Rations</color>");
                if (g.Morale  > 0) parts.Add($"<color=#88CCFF>+{g.Morale} Morale</color>");
                DetailGrant.text = parts.Count > 0 ? "Grants: " + string.Join("  ", parts) : "";
            }
        }

        // ── Play Button ───────────────────────────────────────────────────────

        private void RefreshPlayButton()
        {
            if (PlayButton == null) return;
            bool canPlay = _selected != null && (_selected.Card?.CanAfford(GameSession.Instance?.Resources) ?? false);
            PlayButton.interactable = canPlay;

            if (PlayButtonLabel && _selected?.Card != null)
                PlayButtonLabel.text = $"Play  {_selected.Card.CardName}";
            else if (PlayButtonLabel)
                PlayButtonLabel.text = "Select a Card";
        }

        private void OnPlayPressed()
        {
            if (_selected?.Card == null) return;
            var card = _selected.Card;
            var gm   = GameManager.Instance;
            if (gm == null) return;

            switch (card.Type)
            {
                case CardType.Combat:
                case CardType.Elite:
                    // Remove from hand before loading — SingleUse is the authoring flag
                    if (card.SingleUse) GameSession.Instance?.RemoveCardFromHand(card);
                    gm.PlayCombatCard(card);
                    break;

                case CardType.Heal:
                    ResolveHealCard(card);
                    break;

                case CardType.Rest:
                    ResolveRestCard(card);
                    break;

                case CardType.Treasure:
                    ResolveTreasureCard(card);
                    break;

                case CardType.Shop:
                    // Future: open shop overlay
                    ResolveInstant(card, "Shop coming soon!");
                    break;

                case CardType.LevelUp:
                    // Future: open stat upgrade overlay
                    ResolveInstant(card, "Level-up coming soon!");
                    break;

                case CardType.Curse:
                    // Curses cannot be played voluntarily — just discard
                    ResolveInstant(card, "Curse removed.");
                    break;
            }
        }

        private void ResolveHealCard(EventCardSO card)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            session.ApplyResources(card.ResourceCost);
            session.ApplyResources(card.ResourceGrant);

            // Heal hero by fraction (applied at combat start via RuntimeStats — store in session)
            // For now bump Rations which feeds into next combat init
            int rations = Mathf.RoundToInt(card.HealFraction * 10f);
            session.ApplyResources(new ResourceDelta { Rations = rations });

            if (card.SingleUse) session.RemoveCardFromHand(card);
            StartCoroutine(RefreshAfterResolve("Healed!", card));
        }

        private void ResolveRestCard(EventCardSO card)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            session.ApplyResources(card.ResourceCost);
            session.ApplyResources(card.ResourceGrant);
            session.ApplyResources(new ResourceDelta { Morale = card.MoraleBonus });

            if (card.SingleUse) session.RemoveCardFromHand(card);
            StartCoroutine(RefreshAfterResolve("Rested.", card));
        }

        private void ResolveTreasureCard(EventCardSO card)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            session.ApplyResources(card.ResourceCost);
            session.ApplyResources(card.ResourceGrant);

            if (card.SingleUse) session.RemoveCardFromHand(card);
            StartCoroutine(RefreshAfterResolve("Treasure claimed!", card));
        }

        private void ResolveInstant(EventCardSO card, string msg)
        {
            var session = GameSession.Instance;
            if (session == null) return;
            session.ApplyResources(card.ResourceCost);
            session.ApplyResources(card.ResourceGrant);
            if (card.SingleUse) session.RemoveCardFromHand(card);
            StartCoroutine(RefreshAfterResolve(msg, card));
        }

        private IEnumerator RefreshAfterResolve(string msg, EventCardSO _)
        {
            // Brief notification
            if (DropBanner != null && DropBannerText != null)
            {
                DropBannerText.text = msg;
                DropBanner.SetActive(true);
                yield return new WaitForSecondsRealtime(1.2f);
                DropBanner.SetActive(false);
            }
            else yield return null;

            _selected = null;
            var session = GameSession.Instance;
            if (session != null)
            {
                yield return BuildHand(session);
                RefreshResourceBar(session.Resources);
            }
            RefreshDetailPanel(null);
            RefreshPlayButton();
        }

        // ── Discard ───────────────────────────────────────────────────────────

        private void OnDiscardPressed()
        {
            if (_selected?.Card == null) return;
            GameSession.Instance?.RemoveCardFromHand(_selected.Card);
            _selected = null;
            var session = GameSession.Instance;
            if (session != null)
            {
                StartCoroutine(BuildHand(session));
                RefreshResourceBar(session.Resources);
            }
            RefreshDetailPanel(null);
            RefreshPlayButton();
        }

        // ── Main Menu ─────────────────────────────────────────────────────────

        private void OnMainMenuPressed() => GameManager.Instance?.GoToMainMenu();

        // ── Utility ───────────────────────────────────────────────────────────

        private IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }
}
