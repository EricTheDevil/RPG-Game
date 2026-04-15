using UnityEngine;

namespace RPG.Grid
{
    /// <summary>
    /// Animates the emissive color on a highlighted tile so it pulses/breathes.
    /// Attached automatically by BattleGrid when a tile changes highlight state.
    /// </summary>
    public class TilePulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        public float PulseSpeed = 2.4f;
        [Range(0f, 1f)] public float PulseDepth = 0.45f;   // how much it dims at the trough

        private Renderer _renderer;
        private Color _peakColor;
        private bool _active;

        private static readonly int EmissiveID = Shader.PropertyToID("_EmissiveColor");
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb      = new MaterialPropertyBlock();
        }

        public void StartPulse(Color peakEmissive)
        {
            _peakColor = peakEmissive;
            _active    = true;
            enabled    = true;
        }

        public void StopPulse()
        {
            _active = false;
            enabled = false;

            // Clear emissive override
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissiveID, Color.black);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        private void Update()
        {
            if (!_active || _renderer == null) return;

            // Sine wave between PulseDepth and 1.0
            float t    = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            float mult = Mathf.Lerp(PulseDepth, 1f, t);

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissiveID, _peakColor * mult);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
