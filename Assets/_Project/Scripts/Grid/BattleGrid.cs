using System.Collections.Generic;
using UnityEngine;

namespace RPG.Grid
{
    public class BattleGrid : MonoBehaviour
    {
        public static BattleGrid Instance { get; private set; }

        [Header("Grid Dimensions")]
        public int Width = 8;
        public int Depth = 8;
        public float TileSize = 1.2f;
        public float TileThickness = 0.18f;

        [Header("Prefabs")]
        public GridTile TilePrefab;

        [Header("Tile Materials")]
        public Material DefaultTileMat;
        public Material MoveTileMat;
        public Material AttackTileMat;
        public Material SelectedTileMat;

        [Header("Height Variation")]
        [Range(0f, 1f)] public float HeightFrequency = 0.45f;
        public int HeightLevels = 3;
        public float HeightStep = 0.4f;

        private GridTile[,] _tiles;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void GenerateGrid()
        {
            _tiles = new GridTile[Width, Depth];

            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    float rawHeight = Mathf.PerlinNoise(x * HeightFrequency + 0.5f, z * HeightFrequency + 0.5f);
                    float height = Mathf.Floor(rawHeight * HeightLevels) * HeightStep;

                    Vector3 worldPos = new Vector3(x * TileSize, height, z * TileSize);

                    GridTile tile = Instantiate(TilePrefab, worldPos, Quaternion.identity, transform);
                    tile.name = $"Tile_{x}_{z}";

                    Vector3 standPos = worldPos + Vector3.up * (TileThickness * 0.5f);
                    tile.Initialize(new Vector2Int(x, z), standPos, height);

                    tile.DefaultMat = DefaultTileMat;
                    tile.MoveMat = MoveTileMat;
                    tile.AttackMat = AttackTileMat;
                    tile.SelectedMat = SelectedTileMat;

                    // Scale: height makes the tile a cube-like block
                    tile.transform.localScale = new Vector3(
                        TileSize * 0.94f,
                        TileThickness + height,
                        TileSize * 0.94f
                    );

                    // Shift down so top surface is at world height
                    tile.transform.position = new Vector3(
                        x * TileSize,
                        (height + TileThickness) * 0.5f - TileThickness * 0.5f,
                        z * TileSize
                    );

                    _tiles[x, z] = tile;
                }
            }
        }

        public GridTile GetTile(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return null;
            return _tiles != null ? _tiles[x, z] : null;
        }

        public GridTile GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        public List<GridTile> GetTilesInManhattanRange(Vector2Int center, int range)
        {
            var result = new List<GridTile>();
            for (int x = -range; x <= range; x++)
            {
                int zRange = range - Mathf.Abs(x);
                for (int z = -zRange; z <= zRange; z++)
                {
                    if (x == 0 && z == 0) continue;
                    var tile = GetTile(center.x + x, center.y + z);
                    if (tile != null) result.Add(tile);
                }
            }
            return result;
        }

        public List<GridTile> GetMovableTiles(Vector2Int from, int moveRange)
        {
            var reachable = new List<GridTile>();
            var visited = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(from);
            visited[from] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int cost = visited[current];

                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in dirs)
                {
                    var neighbor = current + dir;
                    var tile = GetTile(neighbor);
                    if (tile == null || visited.ContainsKey(neighbor)) continue;
                    if (tile.IsOccupied) continue;

                    int newCost = cost + 1;
                    if (newCost <= moveRange)
                    {
                        visited[neighbor] = newCost;
                        queue.Enqueue(neighbor);
                        reachable.Add(tile);
                    }
                }
            }
            return reachable;
        }

        public void ClearAllHighlights()
        {
            if (_tiles == null) return;
            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                    _tiles[x, z]?.SetHighlight(TileHighlight.None);
        }

        public Vector3 GetGridCenter()
        {
            return new Vector3((Width - 1) * TileSize * 0.5f, 0f, (Depth - 1) * TileSize * 0.5f);
        }
    }
}
