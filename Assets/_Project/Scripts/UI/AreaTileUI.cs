using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RPG.Map;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// Visual component for a single tile on the area sector grid.
    ///
    /// Visual states:
    ///   • Player position  — white outline + pulsing scale
    ///   • Reachable        — dim highlight, clickable
    ///   • Event tile       — colored by event category
    ///   • Blocked          — dark, not clickable
    ///   • Visited          — dimmed, not clickable
    ///   • Locked (threat)  — red tint + lock icon
    ///   • Entry/Exit       — distinct icons
    /// </summary>
    public class AreaTileUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visuals")]
        public Image           Background;
        public Image           Border;
        public Image           IconImage;
        public TextMeshProUGUI TileLabel;
        public GameObject      PlayerMarker;
        public GameObject      LockedOverlay;
        public GameObject      VisitedOverlay;

        [Header("Type Colors")]
        public Color EmptyColor    = new Color(0.15f, 0.15f, 0.20f);
        public Color EntryColor    = new Color(0.25f, 0.70f, 0.30f);
        public Color ExitColor     = new Color(0.20f, 0.50f, 0.90f);
        public Color BlockedColor  = new Color(0.08f, 0.08f, 0.10f);
        public Color CombatColor   = new Color(0.75f, 0.18f, 0.18f);
        public Color DilemmaColor  = new Color(0.75f, 0.60f, 0.10f);
        public Color ShopColor     = new Color(0.80f, 0.65f, 0.10f);
        public Color RestColor     = new Color(0.20f, 0.65f, 0.35f);
        public Color TreasureColor = new Color(0.90f, 0.75f, 0.10f);
        public Color ShrineColor   = new Color(0.45f, 0.15f, 0.80f);
        public Color LevelUpColor  = new Color(0.15f, 0.55f, 0.90f);
        public Color ReachableTint = new Color(1f, 1f, 1f, 0.20f);

        // ── Runtime ───────────────────────────────────────────────────────────
        public AreaTile     Data    { get; private set; }
        public SectorEventSO Event  { get; private set; }
        public event Action<AreaTileUI> OnClicked;

        private bool _clickable;
        private bool _isPlayer;

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Setup(AreaTile tile, SectorEventSO sectorEvent, bool isPlayerHere, bool isReachable)
        {
            Data      = tile;
            Event     = sectorEvent;
            _isPlayer = isPlayerHere;

            Color  bg    = EmptyColor;
            string label = "";

            switch (tile.TileType)
            {
                case TileType.Entry:
                    bg    = EntryColor;
                    label = "START";
                    break;

                case TileType.Exit:
                    bg    = ExitColor;
                    label = "EXIT";
                    break;

                case TileType.Blocked:
                    bg    = BlockedColor;
                    label = "";
                    break;

                case TileType.Event when sectorEvent != null:
                    bg    = GetEventColor(sectorEvent.Category);
                    // Always show the category so the player can plan their route
                    label = GetCategoryLabel(sectorEvent.Category);
                    break;

                case TileType.Empty:
                    label = "";
                    break;
            }

            if (tile.Visited && tile.TileType != TileType.Entry && tile.TileType != TileType.Exit)
                bg = Color.Lerp(bg, Color.black, 0.55f);

            if (Background) Background.color = bg;

            if (TileLabel)
            {
                TileLabel.text     = label;
                TileLabel.fontSize = tile.TileType == TileType.Event ? 13f : 11f;
                // Strong white on all colored tiles; dim on unvisited empty
                TileLabel.color = tile.TileType == TileType.Empty
                    ? new Color(1f, 1f, 1f, 0.35f)
                    : Color.white;
            }

            if (PlayerMarker)   PlayerMarker.SetActive(isPlayerHere);
            if (LockedOverlay)  LockedOverlay.SetActive(tile.Locked);
            if (VisitedOverlay) VisitedOverlay.SetActive(tile.Visited && !isPlayerHere);

            _clickable = isReachable && !tile.Locked && !tile.Visited
                      && tile.TileType != TileType.Blocked;

            if (Border)
            {
                if (isPlayerHere)
                    Border.color = Color.white;
                else if (_clickable)
                    Border.color = new Color(1f, 1f, 1f, 0.75f);   // brighter border = easier to see moves
                else
                    Border.color = new Color(0.3f, 0.3f, 0.3f, 0.25f);
            }

            var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (isPlayerHere)
                cg.alpha = 1f;
            else if (tile.TileType == TileType.Blocked)
                cg.alpha = 0.35f;
            else if (tile.Visited)
                cg.alpha = 0.40f;
            else if (!isReachable)
                cg.alpha = 0.55f;   // future tiles visible but dimmed
            else
                cg.alpha = 1f;
        }

        private Color GetEventColor(SectorEventCategory cat) => cat switch
        {
            SectorEventCategory.Combat   => CombatColor,
            SectorEventCategory.Dilemma  => DilemmaColor,
            SectorEventCategory.Shop     => ShopColor,
            SectorEventCategory.Rest     => RestColor,
            SectorEventCategory.Treasure => TreasureColor,
            SectorEventCategory.Shrine   => ShrineColor,
            SectorEventCategory.LevelUp  => LevelUpColor,
            _                            => CombatColor,
        };

        private static string GetCategoryLabel(SectorEventCategory cat) => cat switch
        {
            SectorEventCategory.Combat   => "COMBAT",
            SectorEventCategory.Dilemma  => "EVENT",
            SectorEventCategory.Shop     => "SHOP",
            SectorEventCategory.Rest     => "REST",
            SectorEventCategory.Treasure => "TREASURE",
            SectorEventCategory.Shrine   => "SHRINE",
            SectorEventCategory.LevelUp  => "LEVEL UP",
            _                            => "?",
        };

        // ── Interaction ───────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            // Allow clicking exit even if "visited"
            bool isExit = Data?.TileType == TileType.Exit;
            if (!_clickable && !isExit) return;
            OnClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_clickable && Data?.TileType != TileType.Exit) return;
            transform.localScale = Vector3.one * 1.06f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
