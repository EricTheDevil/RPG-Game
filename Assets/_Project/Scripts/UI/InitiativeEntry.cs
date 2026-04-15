using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RPG.UI
{
    /// <summary>
    /// One slot in the InitiativeBarUI.
    /// Supports an "active" highlight state (gold border + scale pulse).
    /// </summary>
    public class InitiativeEntry : MonoBehaviour
    {
        public Image           Portrait;
        public Image           Background;
        public Image           CTBar;
        public Image           ActiveBorder;   // optional — gold border, hidden when inactive
        public TextMeshProUGUI NameLabel;

        private bool          _isActive;
        private RectTransform _rect;
        private Vector3       _baseScale;

        private void Awake()
        {
            _rect      = GetComponent<RectTransform>();
            _baseScale = _rect != null ? _rect.localScale : Vector3.one;
        }

        public void Setup(string label, Sprite portrait, Color tint, float ctPct, bool isActive = false)
        {
            if (NameLabel) NameLabel.text = label;
            if (Portrait)
            {
                Portrait.gameObject.SetActive(portrait != null);
                if (portrait != null) Portrait.sprite = portrait;
            }
            if (Background)
                Background.color = new Color(tint.r * 0.25f, tint.g * 0.25f, tint.b * 0.35f, 0.92f);
            if (CTBar)
            {
                CTBar.color      = tint;
                CTBar.fillAmount = ctPct;
            }

            SetActive(isActive);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (ActiveBorder != null)
                ActiveBorder.gameObject.SetActive(active);
            if (_rect != null)
                _rect.localScale = active ? _baseScale * 1.12f : _baseScale;
        }

        private void Update()
        {
            if (!_isActive || _rect == null) return;
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;
            _rect.localScale = _baseScale * 1.12f * pulse;
        }
    }
}
