using System;
using UnityEngine;
using RPG.Data;
using RPG.Core;

namespace RPG.Map
{
    public enum NodeState { Locked, Available, Completed }

    /// <summary>
    /// A single encounter node on the world map.
    /// Drives its own visual state (glow colour, pulse speed, icon).
    /// </summary>
    public class MapNode : MonoBehaviour
    {
        [Header("Data")]
        public LevelDataSO LevelData;

        [Header("Visual")]
        public Renderer  CrystalRenderer;
        public Light     NodeLight;
        public GameObject CompletedIcon;   // checkmark or star
        public GameObject LockedIcon;      // padlock overlay

        [Header("Pulse")]
        public float PulseSpeed     = 1.8f;
        public float PulseIntensity = 0.5f;

        // Emissive colours per state
        static readonly Color ColAvailable = new Color(0.2f, 0.6f, 1.0f);
        static readonly Color ColCompleted = new Color(0.2f, 0.9f, 0.3f);
        static readonly Color ColLocked    = new Color(0.2f, 0.2f, 0.2f);

        private static readonly int EmissiveID = Shader.PropertyToID("_EmissiveColor");

        public NodeState State { get; private set; }

        public event Action<MapNode> OnClicked;

        private MaterialPropertyBlock _mpb;
        private Color _peakEmissive;
        private bool  _pulse;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            RefreshState();
        }

        public void RefreshState()
        {
            if (LevelData == null) return;

            var session = GameSession.Instance;

            if (session != null && session.IsLevelCompleted(LevelData.LevelIndex))
                ApplyState(NodeState.Completed);
            else if (session != null && session.IsLevelUnlocked(LevelData.LevelIndex))
                ApplyState(NodeState.Available);
            else if (LevelData.UnlockedByDefault)
                ApplyState(NodeState.Available);
            else
                ApplyState(NodeState.Locked);
        }

        void ApplyState(NodeState state)
        {
            State = state;

            Color baseEmissive;
            float lightIntensity;
            bool  canPulse;

            switch (state)
            {
                case NodeState.Available:
                    baseEmissive   = ColAvailable;
                    lightIntensity = 1.2f;
                    canPulse       = true;
                    break;
                case NodeState.Completed:
                    baseEmissive   = ColCompleted;
                    lightIntensity = 0.6f;
                    canPulse       = false;
                    break;
                default: // Locked
                    baseEmissive   = ColLocked;
                    lightIntensity = 0f;
                    canPulse       = false;
                    break;
            }

            _peakEmissive = baseEmissive * 5f;
            _pulse        = canPulse;

            if (!canPulse && CrystalRenderer != null)
            {
                CrystalRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissiveID, _peakEmissive * (state == NodeState.Locked ? 0.15f : 0.6f));
                CrystalRenderer.SetPropertyBlock(_mpb);
            }

            if (NodeLight != null)
            {
                NodeLight.color     = baseEmissive;
                NodeLight.intensity = lightIntensity;
                NodeLight.enabled   = lightIntensity > 0f;
            }

            if (CompletedIcon != null) CompletedIcon.SetActive(state == NodeState.Completed);
            if (LockedIcon    != null) LockedIcon.SetActive(state    == NodeState.Locked);
        }

        private void Update()
        {
            if (!_pulse || CrystalRenderer == null) return;

            float t    = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            float mult = Mathf.Lerp(1f - PulseIntensity, 1f, t);

            CrystalRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissiveID, _peakEmissive * mult);
            CrystalRenderer.SetPropertyBlock(_mpb);
        }

        private void OnMouseDown()
        {
            if (State != NodeState.Locked)
                OnClicked?.Invoke(this);
        }

        private void OnMouseEnter()
        {
            if (State == NodeState.Available && CrystalRenderer != null)
            {
                CrystalRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissiveID, _peakEmissive * 1.4f);
                CrystalRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void OnMouseExit() => RefreshState();
    }
}
