using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Map
{
    /// <summary>
    /// Generates a local sector tile grid for a single world node.
    ///
    /// Grid layout (example 6×4):
    ///
    ///   [E][ ][ ][S][ ][ ]    row 3 (top)
    ///   [E][B][ ][C][ ][X]    row 2
    ///   [E][ ][S][ ][C][X]    row 1
    ///   [E][ ][ ][R][ ][X]    row 0 (bottom)
    ///
    ///   E = Entry (x=0),  X = Exit (x=Cols-1),  B = Blocked,
    ///   C = Combat,  S = Shop/Safe,  R = Rest,  blank = Empty
    ///
    /// Player starts at a random Entry tile and moves east toward the Exit.
    /// Moving left (backtracking) is allowed but costs Rations.
    /// Blocked tiles create routing decisions ("go around the ruins").
    ///
    /// The ThreatLimit from AreaMapConfigSO controls how many events
    /// can be resolved before remaining event tiles lock.
    /// </summary>
    public static class AreaMapGenerator
    {
        public static AreaMapData Generate(AreaMapConfigSO config, NodeType worldNodeType)
        {
            if (config == null)
            {
                Debug.LogError("[AreaMapGenerator] Null config — returning minimal grid.");
                return MakeMinimal();
            }

            int cols = 6;
            int rows = Random.Range(3, 5); // 3 or 4 rows for variety

            var data = new AreaMapData
            {
                Cols         = cols,
                Rows         = rows,
                ThreatMax     = config.ThreatLimit,
                ThreatCurrent = 0,
            };

            // ── Initialise flat tile list ─────────────────────────────────────
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                data.Tiles.Add(new AreaTile
                {
                    X        = x,
                    Y        = y,
                    TileType = TileType.Empty,
                });
            }

            // ── Entry column (x = 0): all tiles are Entry ─────────────────────
            for (int y = 0; y < rows; y++)
                data.GetTile(0, y).TileType = TileType.Entry;

            // ── Exit column (x = cols-1): all tiles are Exit ──────────────────
            for (int y = 0; y < rows; y++)
                data.GetTile(cols - 1, y).TileType = TileType.Exit;

            // Player starts at middle-ish entry row
            int startY = rows / 2;
            data.PlayerPosition = new Vector2Int(0, startY);

            // Exit is at a random row on the right edge
            int exitY = Random.Range(0, rows);
            data.ExitPosition = new Vector2Int(cols - 1, exitY);

            // ── Blocked tiles (impassable obstacles) ─────────────────────────
            PlaceBlockedTiles(data, worldNodeType);

            // ── Event tiles ───────────────────────────────────────────────────
            int eventCount = Random.Range(config.MinEvents, config.MaxEvents + 1);
            PlaceEvents(data, config, eventCount, worldNodeType);

            // ── Guarantee a passable path exists ─────────────────────────────
            EnsurePassablePath(data);

            return data;
        }

        // ── Blocked Tiles ─────────────────────────────────────────────────────

        private static void PlaceBlockedTiles(AreaMapData data, NodeType nodeType)
        {
            // More blocked tiles in Hostile areas, fewer in Safe
            int maxBlocked = nodeType switch
            {
                NodeType.Hostile => data.Rows * 2,
                NodeType.Safe    => data.Rows,
                _                => data.Rows + 1,
            };
            int count = Random.Range(0, maxBlocked);

            for (int i = 0; i < count; i++)
            {
                // Only block middle columns (not entry or exit)
                int x = Random.Range(1, data.Cols - 1);
                int y = Random.Range(0, data.Rows);
                var tile = data.GetTile(x, y);
                if (tile != null && tile.TileType == TileType.Empty)
                    tile.TileType = TileType.Blocked;
            }
        }

        // ── Event Placement ───────────────────────────────────────────────────

        private static void PlaceEvents(AreaMapData data, AreaMapConfigSO config,
                                        int count, NodeType nodeType)
        {
            // Collect candidate tiles: Empty tiles in middle columns
            var candidates = new List<AreaTile>();
            for (int y = 0; y < data.Rows; y++)
            for (int x = 1; x < data.Cols - 1; x++)
            {
                var t = data.GetTile(x, y);
                if (t != null && t.TileType == TileType.Empty)
                    candidates.Add(t);
            }

            Shuffle(candidates);
            int placed = 0;

            foreach (var tile in candidates)
            {
                if (placed >= count) break;

                var eventType = config.RollEventType();
                var ev        = config.PickEvent(eventType);
                if (ev == null) continue;

                tile.TileType  = TileType.Event;
                tile.EventName = ev.name;
                placed++;
            }
        }

        // ── Path Guarantee ────────────────────────────────────────────────────
        // Simple flood-fill from player start. If exit is not reachable,
        // clear one blocked tile on the shortest path.

        private static void EnsurePassablePath(AreaMapData data)
        {
            if (IsReachable(data, data.PlayerPosition, data.ExitPosition)) return;

            // Try to open a direct east corridor at the start row
            int y = data.PlayerPosition.y;
            for (int x = 1; x < data.Cols - 1; x++)
            {
                var tile = data.GetTile(x, y);
                if (tile != null && tile.TileType == TileType.Blocked)
                    tile.TileType = TileType.Empty;
            }

            // If still not reachable, open a corridor at exit row
            if (!IsReachable(data, data.PlayerPosition, data.ExitPosition))
            {
                y = data.ExitPosition.y;
                for (int x = 1; x < data.Cols - 1; x++)
                {
                    var tile = data.GetTile(x, y);
                    if (tile != null && tile.TileType == TileType.Blocked)
                        tile.TileType = TileType.Empty;
                }
            }
        }

        private static bool IsReachable(AreaMapData data, Vector2Int from, Vector2Int to)
        {
            var visited = new HashSet<Vector2Int>();
            var queue   = new Queue<Vector2Int>();
            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == to) return true;

                var dirs = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.down, Vector2Int.left };
                foreach (var d in dirs)
                {
                    var next = cur + d;
                    if (visited.Contains(next)) continue;
                    var tile = data.GetTile(next);
                    if (tile == null || tile.TileType == TileType.Blocked) continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        // ── Fallback ──────────────────────────────────────────────────────────

        private static AreaMapData MakeMinimal()
        {
            var d = new AreaMapData { Cols = 4, Rows = 2, ThreatMax = 3 };
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 4; x++)
            {
                d.Tiles.Add(new AreaTile
                {
                    X        = x,
                    Y        = y,
                    TileType = x == 0 ? TileType.Entry : x == 3 ? TileType.Exit : TileType.Empty,
                });
            }
            d.PlayerPosition = new Vector2Int(0, 0);
            d.ExitPosition   = new Vector2Int(3, 0);
            return d;
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
