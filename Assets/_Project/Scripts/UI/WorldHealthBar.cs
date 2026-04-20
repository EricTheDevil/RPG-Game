using UnityEngine;
using UnityEngine.UI;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// World-space health/MP bar attached above a unit.
    ///
    /// Design notes:
    ///   • Driven by Image.fillAmount (horizontal fill), NOT Slider — Slider breaks under
    ///     arbitrary world-space rotation because its RectTransform fill area gets distorted.
    ///   • Billboard is done by matching camera rotation in LateUpdate (rotation only, not position).
    ///   • Bar values animate smoothly via MoveTowards each frame.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [Header("Fill Images (set Image.Type = Filled, Fill Method = Horizontal)")]
        public Image HPFill;
        public Image MPFill;

        [Header("Colors")]
        public Color PlayerHPColor = new Color(0.20f, 0.85f, 0.25f);
        public Color EnemyHPColor  = new Color(0.90f, 0.20f, 0.20f);
        public Color LowHPColor    = new Color(1.00f, 0.35f, 0.05f);
        public Color MPColor       = new Color(0.25f, 0.50f, 1.00f);

        [Header("Animation")]
        public float BarAnimSpeed = 6f;

        private Unit      _unit;
        private Transform _cam;
        private Color     _baseHPColor;

        private float _targetHP  = 1f;
        private float _targetMP  = 1f;
        private float _displayHP = 1f;
        private float _displayMP = 1f;

        // ── Setup ─────────────────────────────────────────────────────────────
        public void Setup(Unit unit)
        {
            _unit        = unit;
            _cam         = Camera.main?.transform;
            _baseHPColor = unit.Team == Team.Player ? PlayerHPColor : EnemyHPColor;

            if (HPFill) HPFill.color = _baseHPColor;
            if (MPFill) MPFill.color = MPColor;

            unit.OnDamaged += (_, __) => ScheduleUpdate();
            unit.OnHealed  += _        => ScheduleUpdate();

            // Snap to full health immediately
            _displayHP = _targetHP = 1f;
            _displayMP = _targetMP = 1f;
            ApplyFills();
        }

        private void ScheduleUpdate()
        {
            if (_unit == null) return;
            _targetHP = (float)_unit.CurrentHP / Mathf.Max(1, _unit.RT?.MaxHP ?? _unit.Stats.MaxHP);
            _targetMP = (float)_unit.CurrentMP / Mathf.Max(1, _unit.RT?.MaxMP ?? _unit.Stats.MaxMP);
        }

        // ── Per-frame ─────────────────────────────────────────────────────────
        private void LateUpdate()
        {
            // Smooth bar animation
            bool dirty = false;
            if (!Mathf.Approximately(_displayHP, _targetHP))
            {
                _displayHP = Mathf.MoveTowards(_displayHP, _targetHP, BarAnimSpeed * Time.deltaTime);
                dirty = true;
            }
            if (!Mathf.Approximately(_displayMP, _targetMP))
            {
                _displayMP = Mathf.MoveTowards(_displayMP, _targetMP, BarAnimSpeed * Time.deltaTime);
                dirty = true;
            }
            if (dirty) ApplyFills();

            // Billboard: always face the camera, overriding any parent rotation
            if (_cam != null)
                transform.rotation = _cam.rotation;
        }

        private void ApplyFills()
        {
            if (HPFill)
            {
                HPFill.fillAmount = _displayHP;
                HPFill.color = _displayHP < 0.3f
                    ? Color.Lerp(LowHPColor, _baseHPColor, _displayHP / 0.3f)
                    : _baseHPColor;
            }
            if (MPFill) MPFill.fillAmount = _displayMP;
        }
    }
}
