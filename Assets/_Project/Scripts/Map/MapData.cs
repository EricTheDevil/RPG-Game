using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Map
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────────────────────────────────

    public enum NodeType { Hostile, Safe, Random }

    public enum TileType
    {
        Empty,          // Open ground — costs 1 Ration to traverse
        Event,          // Has a SectorEvent attached
        Entry,          // Player starts here (left column)
        Exit,           // Player must reach here (right column)
        Blocked,        // Impassable (ruins, walls) — forces routing decisions
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  World Map  — the branching node path to the Demon Lord
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class WorldMapData
    {
        public List<WorldMapLayer> Layers = new();
    }

    [Serializable]
    public class WorldMapLayer
    {
        public int LayerIndex;
        public List<WorldMapNode> Nodes = new();
    }

    [Serializable]
    public class WorldMapNode
    {
        public string    Id;
        public NodeType  Type;
        public string    DisplayName;
        public bool      IsBossNode;
        public bool      Visited;
        /// <summary>IDs of nodes in the next layer this node connects to.</summary>
        public List<string> Connections = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Area Map  — the local sector tile grid
    //
    //  Layout: Cols × Rows grid. Entry on left column (x=0), Exit on right (x=Cols-1).
    //  Player moves one tile at a time. Each move costs 1 Ration.
    //  Starvation (Rations=0) applies stat penalties instead.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class AreaMapData
    {
        public int Cols = 6;
        public int Rows = 4;

        /// <summary>Flattened grid: index = x + y * Cols.</summary>
        public List<AreaTile> Tiles = new();

        /// <summary>Grid position the player is currently on.</summary>
        public Vector2Int PlayerPosition = new Vector2Int(0, 0);

        /// <summary>Grid position of the exit tile.</summary>
        public Vector2Int ExitPosition;

        // Threat pressure
        public int ThreatMax;
        public int ThreatCurrent;

        // ── Tile Access ───────────────────────────────────────────────────────

        public AreaTile GetTile(int x, int y)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows) return null;
            int idx = x + y * Cols;
            return idx < Tiles.Count ? Tiles[idx] : null;
        }

        public AreaTile GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        /// <summary>4-directional orthogonal neighbours the player can move to.</summary>
        public List<Vector2Int> GetMovableNeighbours(Vector2Int from)
        {
            var result = new List<Vector2Int>(4);
            var dirs   = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.down, Vector2Int.left };
            foreach (var d in dirs)
            {
                var pos  = from + d;
                var tile = GetTile(pos);
                if (tile != null && tile.TileType != TileType.Blocked && !tile.Locked)
                    result.Add(pos);
            }
            return result;
        }

        public bool IsAtExit() => PlayerPosition == ExitPosition;
    }

    [Serializable]
    public class AreaTile
    {
        public int        X;
        public int        Y;
        public TileType   TileType  = TileType.Empty;

        /// <summary>Asset name of the SectorEventSO on this tile. Empty = no event.</summary>
        public string     EventName = "";

        public bool       Visited   = false;
        public bool       Locked    = false;    // locked when ThreatMax reached
    }
}
