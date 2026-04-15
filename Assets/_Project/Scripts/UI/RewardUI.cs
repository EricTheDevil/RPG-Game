using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// Reward scene controller — animates XP / gold counters and reveals any item drop.
    /// Reads LevelRewardSO from GameSession.CurrentLevel.
    /// </summary>
    public class RewardUI : MonoBehaviour
    {
        [Header("Title")]
        public TextMeshProUGUI VictoryText;
        public TextMeshProUGUI FlavorText;

        [Header("Counters")]
        public TextMeshProUGUI XPLabel;
        public TextMeshProUGUI XPValue;
        public TextMeshProUGUI GoldLabel;
        public TextMeshProUGUI GoldValue;
        public TextMeshProUGUI TotalXPValue;
        public TextMeshProUGUI TotalGoldValue;

        [Header("Item Card  (hide root if no item)")]
        public GameObject     ItemCard;
        public Image          ItemIcon;
        public TextMeshProUGUI ItemName;
        public TextMeshProUGUI ItemDesc;

        [Header("Session Totals Header")]
        public TextMeshProUGUI SessionXPText;
        public TextMeshProUGUI SessionGoldText;

        [Header("Navigation")]
        public Button ContinueButton;

        [Header("Animation")]
        public float CounterDuration = 1.4f;
        public float RevealDelay     = 0.6f;
        public CanvasGroup RootGroup;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Start()
        {
            ContinueButton?.onClick.AddListener(() => GameManager.Instance?.OnRewardAcknowledged());

            var level   = GameSession.Instance?.CurrentLevel;
            var reward  = level?.Reward;
            var session = GameSession.Instance;

            if (reward != null)
                StartCoroutine(RevealRewards(reward, session));
            else
            {
                // No reward data — skip straight to continue
                if (FlavorText) FlavorText.text = "A battle concluded.";
            }
        }

        // ── Reveal Sequence ───────────────────────────────────────────────────
        private IEnumerator RevealRewards(LevelRewardSO reward, GameSession session)
        {
            // Fade panel in
            if (RootGroup != null)
            {
                RootGroup.alpha = 0f;
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    RootGroup.alpha = Mathf.Clamp01(t / 0.5f);
                    yield return null;
                }
                RootGroup.alpha = 1f;
            }

            // Titles
            if (VictoryText) VictoryText.text = "VICTORY!";
            if (FlavorText)  FlavorText.text  = reward.FlavorText;

            // Tint using reward accent
            if (VictoryText) VictoryText.color = reward.AccentColor;

            yield return new WaitForSeconds(RevealDelay);

            // Animate XP counter
            if (XPLabel)  XPLabel.text  = "Experience Gained";
            if (GoldLabel) GoldLabel.text = "Gold Found";

            yield return StartCoroutine(AnimateCounter(XPValue,   0, reward.XPReward,   CounterDuration));
            yield return StartCoroutine(AnimateCounter(GoldValue,  0, reward.GoldReward, CounterDuration * 0.8f));

            yield return new WaitForSeconds(0.3f);

            // Session totals
            if (session != null)
            {
                if (SessionXPText)   SessionXPText.text   = $"Total XP:    {session.TotalXP}";
                if (SessionGoldText) SessionGoldText.text = $"Total Gold:  {session.TotalGold}";
            }

            // Item reveal
            if (!string.IsNullOrEmpty(reward.ItemName))
            {
                yield return new WaitForSeconds(0.4f);

                ItemCard?.SetActive(true);
                if (ItemIcon && reward.ItemIcon) ItemIcon.sprite = reward.ItemIcon;
                if (ItemName) ItemName.text = reward.ItemName;
                if (ItemDesc) ItemDesc.text = reward.ItemDescription;
            }
            else
            {
                ItemCard?.SetActive(false);
            }
        }

        private IEnumerator AnimateCounter(TextMeshProUGUI label, int from, int to, float duration)
        {
            if (label == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int val = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
                label.text = val.ToString();
                yield return null;
            }
            label.text = to.ToString();
        }
    }
}
