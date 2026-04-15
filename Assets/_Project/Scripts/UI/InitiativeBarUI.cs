using System.Collections.Generic;
using UnityEngine;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// TFT-style initiative / CT bar.
    /// Horizontal strip of unit portrait slots sorted by CT descending.
    /// The active unit's slot is highlighted in gold with a scale pulse.
    /// Refresh() is called by CombatHUD whenever a unit acts.
    /// Update() smoothly animates CT fill bars every frame.
    /// </summary>
    public class InitiativeBarUI : MonoBehaviour
    {
        [Header("Layout")]
        public Transform  Container;
        public GameObject EntryPrefab;

        [Header("Colors")]
        public Color PlayerColor = new Color(0.25f, 0.70f, 1.00f);
        public Color EnemyColor  = new Color(1.00f, 0.30f, 0.30f);
        public Color ActiveColor = new Color(1.00f, 0.85f, 0.00f);

        private readonly List<InitiativeEntry> _entries     = new();
        private          List<Unit>            _sortedUnits = new();
        private          Unit                  _activeUnit;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Rebuild entries. Pass activeUnit to highlight it gold.</summary>
        public void Refresh(List<Unit> sortedUnits, Unit activeUnit = null)
        {
            _sortedUnits = sortedUnits ?? new List<Unit>();
            _activeUnit  = activeUnit;
            RebuildEntries();
        }

        // ── Per-frame smooth CT fill ──────────────────────────────────────────
        private void Update()
        {
            if (_sortedUnits.Count == 0) return;

            int count = Mathf.Min(_entries.Count, _sortedUnits.Count);
            for (int i = 0; i < count; i++)
            {
                var unit  = _sortedUnits[i];
                var entry = _entries[i];
                if (unit == null || entry == null || entry.CTBar == null) continue;
                entry.CTBar.fillAmount = Mathf.Lerp(
                    entry.CTBar.fillAmount,
                    Mathf.Clamp01(unit.CT / 100f),
                    Time.deltaTime * 10f);
            }
        }

        // ── Pool management ───────────────────────────────────────────────────
        private void RebuildEntries()
        {
            if (Container == null || EntryPrefab == null) return;

            int needed = _sortedUnits.Count;

            while (_entries.Count > needed)
            {
                var last = _entries[^1];
                if (last != null) Destroy(last.gameObject);
                _entries.RemoveAt(_entries.Count - 1);
            }

            while (_entries.Count < needed)
            {
                var go    = Instantiate(EntryPrefab, Container);
                var entry = go.GetComponent<InitiativeEntry>();
                if (entry == null)
                {
                    Debug.LogWarning("[InitiativeBarUI] EntryPrefab missing InitiativeEntry component.");
                    Destroy(go);
                    break;
                }
                _entries.Add(entry);
            }

            for (int i = 0; i < _entries.Count && i < _sortedUnits.Count; i++)
            {
                var unit  = _sortedUnits[i];
                var entry = _entries[i];
                if (unit == null || entry == null) continue;

                bool isActive = unit == _activeUnit;
                Color tint    = isActive          ? ActiveColor
                              : unit.Team == Team.Player ? PlayerColor : EnemyColor;

                entry.Setup(
                    label:    unit.Stats?.UnitName ?? "?",
                    portrait: unit.Stats?.Portrait,
                    tint:     tint,
                    ctPct:    Mathf.Clamp01(unit.CT / 100f),
                    isActive: isActive);
            }
        }
    }
}
