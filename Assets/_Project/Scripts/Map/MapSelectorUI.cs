using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.Map
{
    /// <summary>
    /// Dynamically spawns MapNode objects from a LevelRegistry asset.
    /// Add new levels to the registry — no scene changes needed.
    ///
    /// Flow:
    ///   1. Reads LevelRegistry.Levels
    ///   2. Instantiates NodePrefab at each level's MapPosition
    ///   3. Draws LineRenderer connections for each unlock edge
    ///   4. Refreshes node state from GameSession on enable
    /// </summary>
    public class MapSelectorUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Registry")]
        public LevelRegistry Registry;

        [Header("Prefabs")]
        public GameObject NodePrefab;          // must have MapNode component
        public GameObject ConnectionPrefab;    // must have LineRenderer component

        [Header("Map Root  (nodes are parented here)")]
        public Transform  MapRoot;

        [Header("Preview Panel")]
        public GameObject      PreviewPanel;
        public TextMeshProUGUI LevelNameText;
        public TextMeshProUGUI LevelDescText;
        public TextMeshProUGUI DifficultyText;
        public Image           ThumbnailImage;
        public TextMeshProUGUI RewardXPText;
        public TextMeshProUGUI RewardGoldText;
        public Button          EnterButton;

        [Header("Session Summary (top bar)")]
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI XPText;

        [Header("Navigation")]
        public Button MainMenuButton;

        // ── Private ───────────────────────────────────────────────────────────
        readonly Dictionary<int, MapNode> _nodes = new Dictionary<int, MapNode>();
        MapNode _selectedNode;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Start()
        {
            if (Registry == null)
            {
                Debug.LogError("[MapSelectorUI] No LevelRegistry assigned!");
                return;
            }

            // Unlock default levels before spawning so initial state is correct
            if (GameSession.Instance != null)
                foreach (var level in Registry.Levels)
                    if (level != null && level.UnlockedByDefault)
                        GameSession.Instance.UnlockLevel(level.LevelIndex);

            SpawnNodes();
            SpawnConnections();

            EnterButton?.onClick.AddListener(OnEnterPressed);
            MainMenuButton?.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());

            PreviewPanel?.SetActive(false);
            RefreshSessionBar();
        }

        private void OnEnable()
        {
            // Refresh when returning from combat / reward
            foreach (var node in _nodes.Values)
                node.RefreshState();

            RefreshSessionBar();

            // Roguelike auto-chain: if we're returning after a victory,
            // pick a random unlocked-but-not-completed level and start it.
            if (_nodes.Count > 0 && GameSession.Instance != null)
                TryAutoChain();
        }

        void TryAutoChain()
        {
            // Collect all levels that are unlocked and not yet completed this loop
            var available = new System.Collections.Generic.List<LevelDataSO>();
            foreach (var kv in _nodes)
            {
                var level = kv.Value.LevelData;
                if (level == null) continue;
                var session = GameSession.Instance;
                bool unlocked = level.UnlockedByDefault || session.IsLevelUnlocked(level.LevelIndex);
                if (unlocked) available.Add(level);
            }

            if (available.Count == 0) return;

            // Pick random
            var chosen = available[UnityEngine.Random.Range(0, available.Count)];
            // Brief delay so the player sees the map before auto-launching
            StartCoroutine(AutoLaunchAfterDelay(chosen, 1.2f));
        }

        private System.Collections.IEnumerator AutoLaunchAfterDelay(LevelDataSO level, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            GameManager.Instance?.StartLevel(level);
        }

        // ── Node Spawning ─────────────────────────────────────────────────────
        void SpawnNodes()
        {
            if (NodePrefab == null)
            {
                Debug.LogError("[MapSelectorUI] NodePrefab is not assigned.");
                return;
            }

            _nodes.Clear();

            foreach (var level in Registry.Levels)
            {
                if (level == null) continue;

                // MapPosition is XZ; Y stays at 0 (or map plane height)
                Vector3 worldPos = new Vector3(level.MapPosition.x, 0f, level.MapPosition.y);
                if (MapRoot != null) worldPos += MapRoot.position;

                GameObject go   = Object.Instantiate(NodePrefab, worldPos, Quaternion.identity, MapRoot);
                go.name         = $"Node_{level.LevelIndex}_{level.LevelName}";

                var node        = go.GetComponent<MapNode>();
                node.LevelData  = level;
                node.RefreshState();
                node.OnClicked += SelectNode;

                _nodes[level.LevelIndex] = node;
            }
        }

        void SpawnConnections()
        {
            if (ConnectionPrefab == null) return;

            foreach (var level in Registry.Levels)
            {
                if (level == null || level.UnlocksLevels == null) continue;
                if (!_nodes.TryGetValue(level.LevelIndex, out var fromNode)) continue;

                foreach (int toIndex in level.UnlocksLevels)
                {
                    if (!_nodes.TryGetValue(toIndex, out var toNode)) continue;

                    GameObject lineGo = Object.Instantiate(ConnectionPrefab, MapRoot);
                    lineGo.name       = $"Connection_{level.LevelIndex}→{toIndex}";

                    var lr = lineGo.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        lr.positionCount = 2;
                        lr.SetPosition(0, fromNode.transform.position);
                        lr.SetPosition(1, toNode.transform.position);
                    }
                }
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────
        void SelectNode(MapNode node)
        {
            _selectedNode = node;
            ShowPreview(node.LevelData);
        }

        void ShowPreview(LevelDataSO data)
        {
            if (data == null) return;
            PreviewPanel?.SetActive(true);

            if (LevelNameText)  LevelNameText.text  = data.LevelName;
            if (LevelDescText)  LevelDescText.text  = data.Description;
            if (DifficultyText)
            {
                DifficultyText.text  = data.DifficultyLabel;
                DifficultyText.color = data.DifficultyColor;
            }
            if (ThumbnailImage && data.Thumbnail) ThumbnailImage.sprite = data.Thumbnail;

            bool hasReward = data.Reward != null;
            if (RewardXPText)   RewardXPText.text   = hasReward ? $"+{data.Reward.XPReward} XP"    : "";
            if (RewardGoldText) RewardGoldText.text = hasReward ? $"+{data.Reward.GoldReward} Gold" : "";

            bool completed = GameSession.Instance?.IsLevelCompleted(data.LevelIndex) ?? false;
            if (EnterButton)
            {
                var label = EnterButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label) label.text = completed ? "REPLAY" : "ENTER BATTLE";
            }
        }

        void OnEnterPressed()
        {
            if (_selectedNode?.LevelData != null)
                GameManager.Instance?.StartLevel(_selectedNode.LevelData);
        }

        void RefreshSessionBar()
        {
            var s = GameSession.Instance;
            if (s == null) return;
            if (GoldText) GoldText.text = $"Gold: {s.TotalGold}";
            if (XPText)   XPText.text   = $"XP: {s.TotalXP}";
        }
    }
}
