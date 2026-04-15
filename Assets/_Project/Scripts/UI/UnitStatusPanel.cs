using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// HUD panel showing a unit's name, class, HP and MP bars.
    /// Subscribes to unit events at setup time and animates bar changes.
    /// </summary>
    public class UnitStatusPanel : MonoBehaviour
    {
        [Header("Text")]
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI ClassText;
        public TextMeshProUGUI HPValueText;
        public TextMeshProUGUI MPValueText;

        [Header("Bars (use Image with fillAmount, not Slider)")]
        public Image HPBarFill;
        public Image MPBarFill;

        [Header("Bar Colors")]
        public Color PlayerHPColor = new Color(0.22f, 0.82f, 0.30f);
        public Color EnemyHPColor  = new Color(0.90f, 0.22f, 0.22f);
        public Color LowHPColor    = new Color(1f,    0.35f, 0.10f);
        public Color MPBarColor    = new Color(0.25f, 0.55f, 1.00f);

        [Header("Animation")]
        public float BarAnimSpeed = 8f;   // lerp speed — higher = snappier

        private Unit  _unit;
        private float _targetHP = 1f;
        private float _targetMP = 1f;
        private float _currentHPDisplay = 1f;
        private float _currentMPDisplay = 1f;

        // ── Public API ────────────────────────────────────────────────────────
        public void SetUnit(Unit unit)
        {
            if (_unit != null)
            {
                _unit.OnDamaged -= OnDmg;
                _unit.OnHealed  -= OnHeal;
            }
            _unit = unit;
            _unit.OnDamaged += OnDmg;
            _unit.OnHealed  += OnHeal;

            Color hpColor = unit.Team == Team.Player ? PlayerHPColor : EnemyHPColor;
            if (HPBarFill) HPBarFill.color = hpColor;
            if (MPBarFill) MPBarFill.color = MPBarColor;

            // Snap bars to initial values immediately (no slide-in)
            float hpRatio = (float)unit.CurrentHP / Mathf.Max(1, unit.Stats.MaxHP);
            float mpRatio = (float)unit.CurrentMP / Mathf.Max(1, unit.Stats.MaxMP);
            _currentHPDisplay = _targetHP = hpRatio;
            _currentMPDisplay = _targetMP = mpRatio;
            ApplyBars();
            RefreshText();
        }

        private void OnDmg(int d, bool c) => ScheduleRefresh();
        private void OnHeal(int h)         => ScheduleRefresh();

        private void ScheduleRefresh()
        {
            if (_unit == null) return;
            _targetHP = (float)_unit.CurrentHP / Mathf.Max(1, _unit.Stats.MaxHP);
            _targetMP = (float)_unit.CurrentMP / Mathf.Max(1, _unit.Stats.MaxMP);
            RefreshText();
        }

        private void Update()
        {
            if (_unit == null) return;

            bool dirty = false;

            if (!Mathf.Approximately(_currentHPDisplay, _targetHP))
            {
                _currentHPDisplay = Mathf.MoveTowards(_currentHPDisplay, _targetHP, BarAnimSpeed * Time.deltaTime);
                dirty = true;
            }
            if (!Mathf.Approximately(_currentMPDisplay, _targetMP))
            {
                _currentMPDisplay = Mathf.MoveTowards(_currentMPDisplay, _targetMP, BarAnimSpeed * Time.deltaTime);
                dirty = true;
            }

            if (dirty) ApplyBars();
        }

        private void ApplyBars()
        {
            if (HPBarFill)
            {
                HPBarFill.fillAmount = _currentHPDisplay;
                Color baseColor = (_unit != null && _unit.Team == Team.Player) ? PlayerHPColor : EnemyHPColor;
                HPBarFill.color = _currentHPDisplay < 0.3f
                    ? Color.Lerp(LowHPColor, baseColor, _currentHPDisplay / 0.3f)
                    : baseColor;
            }
            if (MPBarFill) MPBarFill.fillAmount = _currentMPDisplay;
        }

        private void RefreshText()
        {
            if (_unit == null) return;
            if (NameText)    NameText.text    = _unit.Stats.UnitName;
            if (ClassText)   ClassText.text   = _unit.Stats.ClassName;
            if (HPValueText) HPValueText.text = $"{_unit.CurrentHP} / {_unit.Stats.MaxHP}";
            if (MPValueText) MPValueText.text = $"{_unit.CurrentMP} / {_unit.Stats.MaxMP}";
        }
    }
}
