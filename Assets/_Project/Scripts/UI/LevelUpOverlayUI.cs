using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// Level-up overlay shown when the player plays a LevelUp event card.
    ///
    /// Presents a choice of stat upgrades that permanently modify HeroRT
    /// and re-snapshot into GameSession.HeroRT for persistence.
    ///
    /// Design: show StatPointGrant upgrade "cards"; each raises one stat by a fixed amount.
    /// Player clicks one choice, overlay closes.
    ///
    /// Wire to a full-screen Canvas overlay GO in the EventDeck scene.
    /// </summary>
    public class LevelUpOverlayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Layout")]
        public Transform         ChoiceContainer;  // Horizontal Layout Group
        public GameObject        ChoicePrefab;     // StatChoiceUI prefab

        [Header("Labels")]
        public TextMeshProUGUI   TitleText;
        public TextMeshProUGUI   HeroStatsText;    // current stats readout

        [Header("Buttons")]
        public Button            CloseButton;      // shown after a choice is made

        [Header("Data")]
        [Tooltip("Used as baseline RT if the hero hasn't fought yet (HeroRT is null). Assign HeroStats.asset.")]
        public RPG.Data.UnitStatsSO HeroStatsSO;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action OnClosed;

        // ── Stat upgrade definitions ──────────────────────────────────────────
        // Each entry: display name, description, and the delta it applies to HeroRT.
        static readonly List<StatUpgrade> AllUpgrades = new()
        {
            new StatUpgrade("Might",       "ATK +8",            rt => { rt.Attack       += 8; }),
            new StatUpgrade("Iron Skin",   "DEF +6",            rt => { rt.Defense      += 6; }),
            new StatUpgrade("Vitality",    "Max HP +30",        rt => { rt.MaxHP        += 30; }),
            new StatUpgrade("Arcane",      "MAG ATK +8",        rt => { rt.MagicAttack  += 8; }),
            new StatUpgrade("Ward",        "MAG DEF +6",        rt => { rt.MagicDefense += 6; }),
            new StatUpgrade("Swiftness",   "SPD +3",            rt => { rt.Speed        += 3; }),
            new StatUpgrade("Focus",       "CRIT +5%",          rt => { rt.CritChance   = Mathf.Clamp01(rt.CritChance + 0.05f); }),
            new StatUpgrade("Ruthless",    "CRIT DMG +0.2×",    rt => { rt.CritMultiplier += 0.2f; }),
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            CloseButton?.onClick.AddListener(Close);
            if (CloseButton) CloseButton.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open(EventCardSO card)
        {
            gameObject.SetActive(true);
            if (CloseButton) CloseButton.gameObject.SetActive(false);

            int choices = card != null ? Mathf.Clamp(card.StatPointGrant + 2, 2, 4) : 3;
            if (TitleText) TitleText.text = "Level Up — Choose an upgrade";

            BuildChoices(choices);
            RefreshHeroStats();
        }

        // ── Private ───────────────────────────────────────────────────────────

        void BuildChoices(int count)
        {
            foreach (Transform child in ChoiceContainer)
                Destroy(child.gameObject);

            // Pick random subset
            var pool   = new List<StatUpgrade>(AllUpgrades);
            count = Mathf.Min(count, pool.Count);
            var picks  = new List<StatUpgrade>();
            for (int i = 0; i < count; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                picks.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            foreach (var upgrade in picks)
            {
                var go   = Instantiate(ChoicePrefab, ChoiceContainer);
                go.name  = $"Choice_{upgrade.Name}";

                var nameLabel = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                var descLabel = go.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
                var btn       = go.GetComponentInChildren<Button>();

                if (nameLabel) nameLabel.text = upgrade.Name;
                if (descLabel) descLabel.text = upgrade.Desc;

                var captured = upgrade;
                btn?.onClick.AddListener(() => ApplyUpgrade(captured));
            }
        }

        void ApplyUpgrade(StatUpgrade upgrade)
        {
            var session = GameSession.Instance;
            if (session == null) { Close(); return; }

            // Apply to live HeroRT snapshot; create one if this is before first fight
            if (session.HeroRT == null)
            {
                if (HeroStatsSO != null)
                {
                    session.HeroRT = new RuntimeStats(HeroStatsSO);
                }
                else
                {
                    Debug.LogWarning("[LevelUpOverlay] HeroRT is null and HeroStatsSO not assigned — upgrade will have no baseline.");
                    Close();
                    return;
                }
            }

            upgrade.Apply(session.HeroRT);
            // Re-snapshot so the new values survive scene transitions
            session.HeroRT = new RuntimeStats(session.HeroRT);

            Debug.Log($"[LevelUpOverlay] Applied upgrade: {upgrade.Name} ({upgrade.Desc}).");

            // Disable all choice buttons — only one pick allowed
            foreach (Transform child in ChoiceContainer)
            {
                var btn = child.GetComponentInChildren<Button>();
                if (btn) btn.interactable = false;
            }

            RefreshHeroStats();
            if (CloseButton) CloseButton.gameObject.SetActive(true);
        }

        void RefreshHeroStats()
        {
            if (HeroStatsText == null) return;
            var rt = GameSession.Instance?.HeroRT;
            if (rt == null) { HeroStatsText.text = ""; return; }
            HeroStatsText.text =
                $"HP {rt.MaxHP}   ATK {rt.Attack}   DEF {rt.Defense}" +
                $"\nMAG {rt.MagicAttack}   SPD {rt.Speed}" +
                $"   CRIT {rt.CritChance * 100f:F0}%";
        }

        void Close()
        {
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }

        // ── Inner types ───────────────────────────────────────────────────────

        class StatUpgrade
        {
            public string Name;
            public string Desc;
            public Action<RuntimeStats> Apply;
            public StatUpgrade(string name, string desc, Action<RuntimeStats> apply)
            { Name = name; Desc = desc; Apply = apply; }
        }
    }
}
