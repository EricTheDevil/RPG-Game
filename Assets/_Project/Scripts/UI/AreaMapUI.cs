using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;
using RPG.Map;

namespace RPG.UI
{
    /// <summary>
    /// Renders the local sector as a tile grid.
    ///
    ///   Entry (left) ──── tile grid ──── Exit (right)
    ///
    /// • Each move costs 1 Ration (or triggers starvation).
    /// • Events are resolved inline (Rest/Treasure/Shrine) or via overlays (Shop/LevelUp/Dilemma/Combat).
    /// • Threat bar fills as events are resolved. At ThreatMax, tiles lock.
    /// • Leave Area button always available.
    /// </summary>
    public class AreaMapUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Resource Bar")]
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI RationsText;
        public TextMeshProUGUI MoraleText;
        public TextMeshProUGUI AreaNameText;

        [Header("Threat Bar")]
        public Image           ThreatFill;
        public TextMeshProUGUI ThreatText;
        public Color           ThreatLowColor  = new Color(0.25f, 0.75f, 0.30f);
        public Color           ThreatMidColor  = new Color(0.90f, 0.75f, 0.20f);
        public Color           ThreatHighColor = new Color(0.85f, 0.20f, 0.20f);

        [Header("Starvation Indicator")]
        public GameObject      StarvationBadge;  // red warning badge in corner

        [Header("Tile Grid")]
        public RectTransform   GridContainer;
        public GameObject      TilePrefab;

        [Header("Layout")]
        public float TileWidth  = 120f;
        public float TileHeight = 100f;
        public float TileGap    = 6f;

        [Header("Buttons")]
        public Button          LeaveButton;
        public TextMeshProUGUI LeaveButtonLabel;

        [Header("Banner")]
        public GameObject      Banner;
        public TextMeshProUGUI BannerText;

        [Header("Overlays")]
        public ShopOverlayUI    ShopOverlay;
        public LevelUpOverlayUI LevelUpOverlay;
        public SectorEventUI    SectorEventPanel; // Field Decision overlay

        // ── Runtime ───────────────────────────────────────────────────────────
        private readonly List<AreaTileUI>                _tileViews = new();
        private readonly Dictionary<string, SectorEventSO> _eventCache = new();
        private AreaTileUI                               _pendingEventTile;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            LeaveButton?.onClick.AddListener(OnLeaveArea);
            Banner?.SetActive(false);
            SectorEventPanel?.Hide();

            var session = GameSession.Instance;
            if (session?.CurrentArea == null) { ShowBanner("No area data."); return; }

            LoadEventCache(session.CurrentArea);
            BuildGrid(session);
            RefreshHUD(session);
        }

        // ── Event Cache ───────────────────────────────────────────────────────

        private void LoadEventCache(AreaMapData area)
        {
            _eventCache.Clear();
            var all = Resources.LoadAll<SectorEventSO>("");
            foreach (var ev in all)
                _eventCache[ev.name] = ev;

            foreach (var tile in area.Tiles)
                if (!string.IsNullOrEmpty(tile.EventName) && !_eventCache.ContainsKey(tile.EventName))
                    Debug.LogWarning($"[AreaMapUI] SectorEvent '{tile.EventName}' not found in Resources.");
        }

        // ── Grid Build ────────────────────────────────────────────────────────

        private void BuildGrid(GameSession session)
        {
            foreach (var v in _tileViews) if (v != null) Destroy(v.gameObject);
            _tileViews.Clear();
            foreach (Transform child in GridContainer) Destroy(child.gameObject);

            var area = session.CurrentArea;

            // Size container to fit grid
            float totalW = area.Cols * (TileWidth  + TileGap) - TileGap;
            float totalH = area.Rows * (TileHeight + TileGap) - TileGap;
            GridContainer.sizeDelta = new Vector2(totalW, totalH);

            var reachable = GetReachableTiles(area);

            foreach (var tile in area.Tiles)
            {
                var go   = Instantiate(TilePrefab, GridContainer);
                var rect = go.GetComponent<RectTransform>();

                // Position: x grows right, y grows up from bottom
                float px = tile.X * (TileWidth  + TileGap) - totalW * 0.5f + TileWidth  * 0.5f;
                float py = tile.Y * (TileHeight + TileGap) - totalH * 0.5f + TileHeight * 0.5f;
                rect.anchoredPosition = new Vector2(px, py);

                _eventCache.TryGetValue(tile.EventName ?? "", out var sectorEvent);

                bool isPlayer    = tile.X == area.PlayerPosition.x && tile.Y == area.PlayerPosition.y;
                bool isReachable = reachable.Contains(new Vector2Int(tile.X, tile.Y));

                var view = go.GetComponent<AreaTileUI>();
                if (view != null)
                {
                    view.Setup(tile, sectorEvent, isPlayer, isReachable);
                    view.OnClicked += OnTileClicked;
                    _tileViews.Add(view);
                }
            }
        }

        private HashSet<Vector2Int> GetReachableTiles(AreaMapData area)
        {
            var set = new HashSet<Vector2Int>();
            var neighbours = area.GetMovableNeighbours(area.PlayerPosition);
            foreach (var n in neighbours) set.Add(n);
            set.Add(area.PlayerPosition); // player's own tile highlighted
            return set;
        }

        // ── Tile Interaction ──────────────────────────────────────────────────

        private void OnTileClicked(AreaTileUI view)
        {
            var session = GameSession.Instance;
            if (session?.CurrentArea == null || view?.Data == null) return;

            var tile = view.Data;
            var area = session.CurrentArea;

            // Can only move to reachable non-blocked tiles
            var pos = new Vector2Int(tile.X, tile.Y);
            var reachable = area.GetMovableNeighbours(area.PlayerPosition);
            if (!reachable.Contains(pos)) return;

            // Move the player
            session.MoveToTile(pos);
            tile.Visited = true;

            if (tile.TileType == TileType.Exit)
            {
                GameManager.Instance?.LeaveArea();
                return;
            }

            if (tile.TileType == TileType.Event && !string.IsNullOrEmpty(tile.EventName))
            {
                _eventCache.TryGetValue(tile.EventName, out var ev);
                if (ev != null)
                {
                    _pendingEventTile = view;
                    ResolveEvent(tile, ev, session);
                    return; // Event resolution handles refresh
                }
            }

            // Plain move — just rebuild
            BuildGrid(session);
            RefreshHUD(session);
        }

        // ── Event Resolution ──────────────────────────────────────────────────

        private void ResolveEvent(AreaTile tile, SectorEventSO ev, GameSession session)
        {
            switch (ev.Category)
            {
                case SectorEventCategory.Combat:
                    tile.Visited = true;
                    session.IncrementThreat();
                    GameManager.Instance?.PlaySectorCombat(ev);
                    return; // scene load

                case SectorEventCategory.Dilemma:
                    ShowDilemma(ev, tile);
                    return; // handled by SectorEventUI callback

                case SectorEventCategory.Shop:
                    OpenShop(ev, tile, session);
                    return;

                case SectorEventCategory.LevelUp:
                    OpenLevelUp(ev, tile, session);
                    return;

                case SectorEventCategory.Rest:
                    ResolveRest(ev, tile, session);
                    break;

                case SectorEventCategory.Treasure:
                    ResolveTreasure(ev, tile, session);
                    break;

                case SectorEventCategory.Shrine:
                    ResolveShrine(ev, tile, session);
                    break;
            }

            // For instant events, mark and rebuild immediately
            MarkTileComplete(tile, session);
            StartCoroutine(RefreshAfterDelay(0.8f));
        }

        // ── Instant Resolutions ───────────────────────────────────────────────

        private void ResolveRest(SectorEventSO ev, AreaTile tile, GameSession session)
        {
            session.ApplyResources(ev.InstantGrant);
            if (ev.HealFraction > 0f && session.HeroRT != null)
            {
                int maxHP   = session.HeroRT.MaxHP;
                int healAmt = Mathf.RoundToInt(maxHP * ev.HealFraction);
                session.HeroCurrentHP = Mathf.Clamp(
                    (session.HeroCurrentHP < 0 ? maxHP : session.HeroCurrentHP) + healAmt,
                    1, maxHP);
            }
            var g = ev.InstantGrant;
            string parts = "";
            if (g.Rations != 0) parts += $"+{g.Rations} Rations  ";
            if (g.Morale  != 0) parts += $"+{g.Morale} Morale  ";
            if (g.Gold    != 0) parts += $"+{g.Gold} Gold";
            ShowBanner($"Rested.  {parts.Trim()}");
        }

        private void ResolveTreasure(SectorEventSO ev, AreaTile tile, GameSession session)
        {
            session.ApplyResources(ev.InstantGrant);
            ShowBanner($"Treasure! +{ev.InstantGrant.Gold} Gold");
        }

        private void ResolveShrine(SectorEventSO ev, AreaTile tile, GameSession session)
        {
            session.ApplyResources(ev.InstantGrant);
            if (ev.ShrineClassXP > 0)
                session.AddClassXP(ev.ShrineClassXP);
            ShowBanner($"Shrine blessed! +{ev.ShrineClassXP} Class XP");
        }

        // ── Overlay Events ────────────────────────────────────────────────────

        private void ShowDilemma(SectorEventSO ev, AreaTile tile)
        {
            if (SectorEventPanel == null)
            {
                // Fallback: resolve first choice automatically
                var session = GameSession.Instance;
                if (ev.Choices.Count > 0)
                    ApplyChoice(ev.Choices[0], session);
                MarkTileComplete(tile, session);
                StartCoroutine(RefreshAfterDelay(0.8f));
                return;
            }

            SectorEventPanel.Show(ev, (choiceIndex) =>
            {
                var session = GameSession.Instance;
                if (choiceIndex >= 0 && choiceIndex < ev.Choices.Count)
                    ApplyChoice(ev.Choices[choiceIndex], session);
                MarkTileComplete(tile, session);
                StartCoroutine(RefreshAfterDelay(0.3f));
            });
        }

        private void ApplyChoice(SectorChoice choice, GameSession session)
        {
            if (session == null) return;
            session.ApplyResources(choice.ResourceDelta);
            session.AddClassXP(choice.ClassXPReward);

            if (choice.HeroHPDelta != 0f && session.HeroRT != null)
            {
                int maxHP  = session.HeroRT.MaxHP;
                int delta  = Mathf.RoundToInt(maxHP * choice.HeroHPDelta);
                session.HeroCurrentHP = Mathf.Clamp(
                    (session.HeroCurrentHP < 0 ? maxHP : session.HeroCurrentHP) + delta,
                    1, maxHP);
            }

            // Track citizen help for class tasks
            if (choice.ClassXPReward > 0 || choice.ResourceDelta.Morale > 0)
                session.RecordCitizenHelped();
        }

        private void OpenShop(SectorEventSO ev, AreaTile tile, GameSession session)
        {
            if (ShopOverlay == null) { ResolveTreasure(ev, tile, session); MarkTileComplete(tile, session); StartCoroutine(RefreshAfterDelay(0.8f)); return; }
            MarkTileComplete(tile, session);
            ShopOverlay.OnClosed -= OnOverlayClosed;
            ShopOverlay.OnClosed += OnOverlayClosed;
            // ShopOverlay.Open() needs an EventCardSO — pass null for now until Shop is ported to SectorEventSO
            // TODO: port ShopOverlayUI to SectorEventSO
            Debug.LogWarning("[AreaMapUI] ShopOverlayUI still expects EventCardSO. Resolving as treasure fallback.");
            ResolveTreasure(ev, tile, session);
            StartCoroutine(RefreshAfterDelay(0.8f));
        }

        private void OpenLevelUp(SectorEventSO ev, AreaTile tile, GameSession session)
        {
            if (LevelUpOverlay == null) { MarkTileComplete(tile, session); StartCoroutine(RefreshAfterDelay(0.8f)); return; }
            MarkTileComplete(tile, session);
            LevelUpOverlay.OnClosed -= OnOverlayClosed;
            LevelUpOverlay.OnClosed += OnOverlayClosed;
            // TODO: port LevelUpOverlayUI to SectorEventSO
            Debug.LogWarning("[AreaMapUI] LevelUpOverlayUI still expects EventCardSO. Skipping.");
            StartCoroutine(RefreshAfterDelay(0.8f));
        }

        private void OnOverlayClosed()
        {
            if (ShopOverlay)    ShopOverlay.OnClosed    -= OnOverlayClosed;
            if (LevelUpOverlay) LevelUpOverlay.OnClosed -= OnOverlayClosed;
            var session = GameSession.Instance;
            if (session != null) { BuildGrid(session); RefreshHUD(session); }
        }

        // ── Tile State ────────────────────────────────────────────────────────

        private void MarkTileComplete(AreaTile tile, GameSession session)
        {
            tile.Visited = true;
            if (tile.TileType == TileType.Event)
                session?.IncrementThreat();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private IEnumerator RefreshAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Banner?.SetActive(false);

            var session = GameSession.Instance;
            if (session?.CurrentArea == null) yield break;

            BuildGrid(session);
            RefreshHUD(session);

            if (session.CurrentArea.ThreatCurrent >= session.CurrentArea.ThreatMax)
                ShowBanner("THREAT MAXED — Remaining event tiles are locked. Head for the exit!");
        }

        // ── HUD ───────────────────────────────────────────────────────────────

        private void RefreshHUD(GameSession session)
        {
            var res = session.Resources;

            if (GoldText)    GoldText.text    = $"<b>GOLD</b>  {res.Gold}";
            if (ScrapText)   ScrapText.text   = $"<b>SCRAP</b>  {res.Scrap}";

            // Rations: red when critically low
            if (RationsText)
            {
                string rationColor = res.Rations <= 1 ? "#FF4444" : "#FFFFFF";
                RationsText.text = $"<b>RATIONS</b>  <color={rationColor}>{res.Rations}/{PlayerResources.MaxRations}</color>";
            }

            // Morale: orange when low
            if (MoraleText)
            {
                string moraleColor = res.Morale <= 2 ? "#FF8800" : "#FFFFFF";
                MoraleText.text = $"<b>MORALE</b>  <color={moraleColor}>{res.Morale}/{PlayerResources.MaxMorale}</color>";
            }

            var worldNode = session.FindWorldNode(session.CurrentWorldNodeId);
            if (AreaNameText)
            {
                string nodeType = worldNode != null
                    ? $"  <size=11><color=#AAAAAA>{worldNode.Type.ToString().ToUpper()}</color></size>"
                    : "";
                AreaNameText.text = $"<b>{worldNode?.DisplayName ?? "Unknown Area"}</b>{nodeType}";
            }

            RefreshThreatBar(session);
            RefreshStarvation(session);
            RefreshLeaveButton();
        }

        private void RefreshThreatBar(GameSession session)
        {
            var area = session.CurrentArea;
            if (area == null) return;

            float ratio = area.ThreatMax > 0 ? (float)area.ThreatCurrent / area.ThreatMax : 0f;
            if (ThreatFill)
            {
                ThreatFill.fillAmount = ratio;
                ThreatFill.color = ratio < 0.5f
                    ? Color.Lerp(ThreatLowColor, ThreatMidColor, ratio * 2f)
                    : Color.Lerp(ThreatMidColor, ThreatHighColor, (ratio - 0.5f) * 2f);
            }
            if (ThreatText) ThreatText.text = $"Threat  {area.ThreatCurrent}/{area.ThreatMax}";
        }

        private void RefreshStarvation(GameSession session)
        {
            StarvationBadge?.SetActive(session.IsStarving);
        }

        private void RefreshLeaveButton()
        {
            if (LeaveButton) LeaveButton.interactable = true;
            if (LeaveButtonLabel) LeaveButtonLabel.text = "Leave Area  \u2192";
        }

        // ── Banner ────────────────────────────────────────────────────────────

        private void ShowBanner(string msg)
        {
            if (Banner == null) return;
            if (BannerText) BannerText.text = msg;
            Banner.SetActive(true);
        }

        private void OnLeaveArea() => GameManager.Instance?.LeaveArea();
    }
}
