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
    /// Renders the branching world map — a vertical node path from start to Demon Lord.
    ///
    /// No ScrollRect. The MapContainer is a simple RectTransform centred in the map body.
    /// Nodes are placed in it using anchoredPosition; the container is auto-scaled
    /// at runtime so all 10 layers fit the available height.
    ///
    /// Layer 0 (start) = bottom.  Final layer (Demon Lord) = top.
    /// Player clicks a node on the current layer → GameManager.EnterAreaNode().
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        [Header("Resource Bar")]
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI RationsText;
        public TextMeshProUGUI MoraleText;
        public TextMeshProUGUI ProgressText;

        [Header("Map Body")]
        [Tooltip("RectTransform that fills the space between top and bottom bars.")]
        public RectTransform   MapBody;

        [Header("Map Container (child of MapBody, scaled to fit)")]
        public RectTransform   MapContainer;
        public GameObject      NodePrefab;
        public GameObject      ConnectionPrefab;

        [Header("Layout (unscaled units)")]
        public float LayerSpacing    = 140f;
        public float NodeSpacing     = 220f;
        public float VerticalPadding = 80f;

        [Header("Buttons")]
        public Button MainMenuButton;

        [Header("Banner")]
        public GameObject      Banner;
        public TextMeshProUGUI BannerText;

        // keep MapScroll as optional null-safe field so old scene refs don't break
        [HideInInspector] public ScrollRect MapScroll;

        private readonly List<WorldMapNodeUI>              _nodeViews = new();
        private readonly Dictionary<string, RectTransform> _nodeRects = new();

        private void Start()
        {
            MainMenuButton?.onClick.AddListener(OnMainMenu);
            Banner?.SetActive(false);

            var session = GameSession.Instance;
            if (session?.WorldMap == null)
            {
                ShowBanner("No world map — return to main menu.");
                return;
            }

            RefreshResourceBar();
            BuildMap(session);
            Debug.Log($"[WorldMapUI] Built {_nodeViews.Count} nodes across {session.WorldMap.Layers.Count} layers.");
        }

        // ── Map Building ──────────────────────────────────────────────────────

        private void BuildMap(GameSession session)
        {
            foreach (var v in _nodeViews) if (v != null) Destroy(v.gameObject);
            _nodeViews.Clear();
            _nodeRects.Clear();
            foreach (Transform child in MapContainer) Destroy(child.gameObject);

            var map        = session.WorldMap;
            int layerCount = map.Layers.Count;

            float totalH = layerCount * LayerSpacing + VerticalPadding * 2f;
            float totalW = NodeSpacing * 3f + 240f;   // fits up to 4 nodes

            // Scale container so it fits inside MapBody
            float bodyH   = MapBody != null ? MapBody.rect.height : Screen.height * 0.85f;
            float bodyW   = MapBody != null ? MapBody.rect.width  : Screen.width;
            float scaleH  = bodyH / totalH;
            float scaleW  = bodyW / totalW;
            float scale   = Mathf.Min(scaleH, scaleW, 1f);   // never upscale

            MapContainer.localScale    = Vector3.one * scale;
            MapContainer.sizeDelta     = new Vector2(totalW, totalH);
            MapContainer.anchoredPosition = Vector2.zero;

            // Build bottom-to-top
            for (int i = 0; i < layerCount; i++)
            {
                var  layer     = map.Layers[i];
                int  nodeCount = layer.Nodes.Count;
                bool isCurrent = i == session.CurrentLayerIndex;
                bool isPast    = i <  session.CurrentLayerIndex;

                float yPos = -totalH * 0.5f + VerticalPadding + i * LayerSpacing;

                for (int j = 0; j < nodeCount; j++)
                {
                    float xOffset = (j - (nodeCount - 1) * 0.5f) * NodeSpacing;

                    var go   = Instantiate(NodePrefab, MapContainer);
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(xOffset, yPos);

                    var view = go.GetComponent<WorldMapNodeUI>();
                    if (view != null)
                    {
                        view.Setup(layer.Nodes[j], isCurrent, isPast);
                        view.OnClicked += OnNodeClicked;
                        _nodeViews.Add(view);
                    }
                    _nodeRects[layer.Nodes[j].Id] = rect;
                }
            }

            DrawConnections(map);
        }

        private void DrawConnections(WorldMapData map)
        {
            if (ConnectionPrefab == null) return;

            foreach (var layer in map.Layers)
                foreach (var node in layer.Nodes)
                {
                    if (!_nodeRects.TryGetValue(node.Id, out var fromRect)) continue;
                    foreach (var targetId in node.Connections)
                    {
                        if (!_nodeRects.TryGetValue(targetId, out var toRect)) continue;
                        DrawLine(fromRect.anchoredPosition, toRect.anchoredPosition, node.Visited);
                    }
                }
        }

        private void DrawLine(Vector2 from, Vector2 to, bool dimmed)
        {
            var go   = Instantiate(ConnectionPrefab, MapContainer);
            var rect = go.GetComponent<RectTransform>();
            var img  = go.GetComponent<Image>();

            Vector2 dir   = to - from;
            float   dist  = dir.magnitude;
            float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta        = new Vector2(dist, 3f);
            rect.localRotation    = Quaternion.Euler(0, 0, angle);

            if (img != null)
                img.color = dimmed ? new Color(0.3f, 0.3f, 0.3f, 0.4f)
                                   : new Color(0.6f, 0.6f, 0.6f, 0.7f);

            go.transform.SetAsFirstSibling();
        }

        // ── Resource Bar ──────────────────────────────────────────────────────

        private void RefreshResourceBar()
        {
            var session = GameSession.Instance;
            var res     = session?.Resources;
            if (res == null) return;

            if (GoldText)    GoldText.text    = $"<b>GOLD</b>  {res.Gold}";
            if (ScrapText)   ScrapText.text   = $"<b>SCRAP</b>  {res.Scrap}";
            if (RationsText) RationsText.text = $"<b>RATIONS</b>  {res.Rations}/{PlayerResources.MaxRations}";
            if (MoraleText)  MoraleText.text  = $"<b>MORALE</b>  {res.Morale}/{PlayerResources.MaxMorale}";

            if (ProgressText != null && session?.WorldMap != null)
                ProgressText.text = $"<b>LAYER {session.CurrentLayerIndex + 1} / {session.WorldMap.Layers.Count}</b>  —  Journey to the Demon Lord";
        }

        // ── Interaction ───────────────────────────────────────────────────────

        private void OnNodeClicked(WorldMapNodeUI view)
        {
            if (view?.Data == null) return;
            GameManager.Instance?.EnterAreaNode(view.NodeId);
        }

        private void OnMainMenu() => GameManager.Instance?.GoToMainMenu();

        // ── Banner ────────────────────────────────────────────────────────────

        private void ShowBanner(string msg)
        {
            if (Banner == null) return;
            if (BannerText) BannerText.text = msg;
            Banner.SetActive(true);
        }
    }
}
