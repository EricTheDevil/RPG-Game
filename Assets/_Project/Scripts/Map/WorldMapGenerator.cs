using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Map
{
    /// <summary>
    /// Procedurally generates a branching world map (Slay the Spire style).
    ///
    /// Structure:
    ///   Layer 0          — single Safe start node
    ///   Layers 1..N-1    — 2-4 nodes each (Hostile / Safe / Random)
    ///   Layer N          — single Boss node (Demon Lord)
    ///
    /// Every node connects to 1-2 nodes in the next layer.
    /// Every node in each layer is guaranteed at least one incoming connection.
    /// </summary>
    public static class WorldMapGenerator
    {
        public static WorldMapData Generate(WorldMapConfigSO config)
        {
            var data = new WorldMapData();
            int total = Mathf.Max(3, config.TotalLayers);

            // ── Layer 0: start ───────────────────────────────────────────────
            var startLayer = new WorldMapLayer { LayerIndex = 0 };
            startLayer.Nodes.Add(new WorldMapNode
            {
                Id          = "node_0_0",
                Type        = NodeType.Safe,
                DisplayName = "Starting Camp",
                IsBossNode  = false,
            });
            data.Layers.Add(startLayer);

            // ── Middle layers ────────────────────────────────────────────────
            for (int i = 1; i < total - 1; i++)
            {
                int count = Random.Range(config.MinNodesPerLayer, config.MaxNodesPerLayer + 1);
                var layer = new WorldMapLayer { LayerIndex = i };

                for (int j = 0; j < count; j++)
                {
                    var type = RollNodeType(config, i, total);
                    layer.Nodes.Add(new WorldMapNode
                    {
                        Id          = $"node_{i}_{j}",
                        Type        = type,
                        DisplayName = WorldMapConfigSO.RandomDisplayName(type),
                        IsBossNode  = false,
                    });
                }
                data.Layers.Add(layer);
            }

            // ── Final layer: boss ────────────────────────────────────────────
            var bossLayer = new WorldMapLayer { LayerIndex = total - 1 };
            bossLayer.Nodes.Add(new WorldMapNode
            {
                Id          = $"node_{total - 1}_0",
                Type        = NodeType.Hostile,
                DisplayName = config.BossName,
                IsBossNode  = true,
            });
            data.Layers.Add(bossLayer);

            // ── Wire connections ─────────────────────────────────────────────
            ConnectLayers(data);

            return data;
        }

        // ── Node Type Roll ───────────────────────────────────────────────────

        private static NodeType RollNodeType(WorldMapConfigSO config, int layer, int totalLayers)
        {
            float progress = (float)layer / (totalLayers - 1);
            float hostile  = config.HostileWeight + progress * config.HostileRampPerLayer * totalLayers;
            float safe     = Mathf.Max(0.05f, config.SafeWeight - progress * 0.02f);
            float random   = config.RandomWeight;
            float total    = hostile + safe + random;

            float roll = Random.Range(0f, total);
            if (roll < hostile) return NodeType.Hostile;
            if (roll < hostile + safe) return NodeType.Safe;
            return NodeType.Random;
        }

        // ── Connection Wiring ────────────────────────────────────────────────

        private static void ConnectLayers(WorldMapData data)
        {
            for (int i = 0; i < data.Layers.Count - 1; i++)
            {
                var cur  = data.Layers[i].Nodes;
                var next = data.Layers[i + 1].Nodes;

                // Track which next-layer nodes have incoming connections
                var connected = new HashSet<int>();

                // Each current node connects to 1-2 random nodes ahead
                foreach (var node in cur)
                {
                    int conCount = Random.Range(1, Mathf.Min(3, next.Count + 1));
                    var indices  = PickDistinct(next.Count, conCount);
                    foreach (int idx in indices)
                    {
                        node.Connections.Add(next[idx].Id);
                        connected.Add(idx);
                    }
                }

                // Backfill: ensure every next-layer node has at least one incoming
                for (int j = 0; j < next.Count; j++)
                {
                    if (connected.Contains(j)) continue;
                    var donor = cur[Random.Range(0, cur.Count)];
                    if (!donor.Connections.Contains(next[j].Id))
                        donor.Connections.Add(next[j].Id);
                }
            }
        }

        /// <summary>Pick <paramref name="count"/> distinct indices from [0, max).</summary>
        private static List<int> PickDistinct(int max, int count)
        {
            count = Mathf.Min(count, max);
            var result = new List<int>(count);
            var pool   = new List<int>(max);
            for (int i = 0; i < max; i++) pool.Add(i);

            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
