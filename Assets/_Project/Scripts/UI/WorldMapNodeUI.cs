using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RPG.Map;

namespace RPG.UI
{
    /// <summary>
    /// Per-node component on the world map. Shows icon + color by type.
    /// Click fires OnClicked with the node ID.
    /// </summary>
    public class WorldMapNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visuals")]
        public Image           NodeIcon;
        public Image           NodeBorder;
        public TextMeshProUGUI NodeLabel;
        public GameObject      VisitedOverlay;
        public GameObject      BossIcon;

        [Header("Colors")]
        public Color HostileColor = new Color(0.85f, 0.20f, 0.20f);
        public Color SafeColor    = new Color(0.25f, 0.75f, 0.30f);
        public Color RandomColor  = new Color(0.90f, 0.75f, 0.20f);
        public Color VisitedColor = new Color(0.40f, 0.40f, 0.40f);
        public Color LockedColor  = new Color(0.25f, 0.25f, 0.25f, 0.5f);

        // ── Runtime ───────────────────────────────────────────────────────────
        public string NodeId  { get; private set; }
        public WorldMapNode Data { get; private set; }
        public event Action<WorldMapNodeUI> OnClicked;

        private bool _interactable = true;

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Setup(WorldMapNode node, bool isCurrentLayer, bool isPastLayer)
        {
            Data   = node;
            NodeId = node.Id;

            if (BossIcon) BossIcon.SetActive(node.IsBossNode);

            // Color by type
            Color col = node.Type switch
            {
                NodeType.Hostile => HostileColor,
                NodeType.Safe    => SafeColor,
                NodeType.Random  => RandomColor,
                _                => RandomColor,
            };

            if (node.Visited)
            {
                col = VisitedColor;
                if (VisitedOverlay) VisitedOverlay.SetActive(true);
            }
            else
            {
                if (VisitedOverlay) VisitedOverlay.SetActive(false);
            }

            if (NodeIcon)   NodeIcon.color   = col;
            if (NodeBorder) NodeBorder.color = col;

            // Label: type prefix on a new line so it reads at a glance
            if (NodeLabel)
            {
                string typeTag = node.IsBossNode ? "BOSS" : node.Type switch
                {
                    NodeType.Hostile => "HOSTILE",
                    NodeType.Safe    => "SAFE",
                    NodeType.Random  => "RANDOM",
                    _                => "",
                };
                NodeLabel.text      = node.Visited ? $"<color=#888888>{typeTag}</color>"
                                                   : $"<b>{typeTag}</b>";
                NodeLabel.fontSize  = 12f;
                NodeLabel.color     = Color.white;
            }

            _interactable = isCurrentLayer && !node.Visited;

            var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (node.Visited)
                cg.alpha = 0.50f;
            else if (!_interactable && !isPastLayer)
                cg.alpha = 0.45f;   // future layers — clearly visible but dimmed
            else
                cg.alpha = 1f;
        }

        // ── Interaction ───────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable) return;
            OnClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable) return;
            transform.localScale = Vector3.one * 1.12f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
