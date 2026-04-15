using UnityEngine;

namespace RPG.Grid
{
    public enum TileHighlight { None, Movable, Attackable, Selected }
    public enum TileType { Normal, Elevated, Water }

    [RequireComponent(typeof(Renderer))]
    public class GridTile : MonoBehaviour
    {
        [Header("References")]
        public Renderer TileRenderer;

        // Runtime data
        public Vector2Int GridPos     { get; private set; }
        public Vector3    WorldPosition { get; private set; }
        public float      TileHeight  { get; private set; }
        public TileType   Type        { get; set; } = TileType.Normal;

        public bool            IsOccupied    => OccupyingUnit != null;
        public RPG.Units.Unit  OccupyingUnit { get; set; }

        // Materials injected by BattleGrid
        [HideInInspector] public Material DefaultMat;
        [HideInInspector] public Material MoveMat;
        [HideInInspector] public Material AttackMat;
        [HideInInspector] public Material SelectedMat;

        // Emissive peak colours for pulse (must match material emissive)
        [HideInInspector] public Color MoveEmissive     = new Color(0f,   0.5f, 1f)   * 6f;
        [HideInInspector] public Color AttackEmissive   = new Color(1f,   0.1f, 0.0f) * 6f;
        [HideInInspector] public Color SelectedEmissive = new Color(1f,   0.8f, 0.0f) * 6f;

        private TileHighlight _highlight = TileHighlight.None;
        private TilePulse     _pulse;

        private void Awake()
        {
            if (TileRenderer == null) TileRenderer = GetComponent<Renderer>();
            _pulse = GetComponent<TilePulse>();
            if (_pulse == null) _pulse = gameObject.AddComponent<TilePulse>();
        }

        public void Initialize(Vector2Int pos, Vector3 worldPos, float height)
        {
            GridPos      = pos;
            WorldPosition = worldPos;
            TileHeight   = height;
        }

        public void SetHighlight(TileHighlight highlight)
        {
            _highlight = highlight;
            if (TileRenderer == null) return;

            _pulse?.StopPulse();

            switch (highlight)
            {
                case TileHighlight.None:
                    TileRenderer.sharedMaterial = DefaultMat;
                    break;
                case TileHighlight.Movable:
                    TileRenderer.sharedMaterial = MoveMat ?? DefaultMat;
                    _pulse?.StartPulse(MoveEmissive);
                    break;
                case TileHighlight.Attackable:
                    TileRenderer.sharedMaterial = AttackMat ?? DefaultMat;
                    _pulse?.StartPulse(AttackEmissive);
                    break;
                case TileHighlight.Selected:
                    TileRenderer.sharedMaterial = SelectedMat ?? DefaultMat;
                    _pulse?.StartPulse(SelectedEmissive);
                    break;
            }
        }

        public TileHighlight CurrentHighlight => _highlight;

        private void OnMouseEnter()
        {
            // Reserved for Planning Phase hover preview
        }

        private void OnMouseDown()
        {
            // Reserved for Planning Phase tile selection
        }
    }
}
