using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Data;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Roguelike buff selection overlay.
    /// Shows 3 randomly chosen BuffCards after victory.
    /// Player clicks one → it's added to GameSession → GameManager proceeds.
    ///
    /// Layout lives inside the CombatStage canvas so it inherits the scene
    /// and doesn't need a scene transition to appear.
    /// </summary>
    public class BuffSelectionUI : MonoBehaviour
    {
        [Header("Registry")]
        public BuffRegistry Registry;

        [Header("Layout")]
        public CanvasGroup    RootGroup;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI SubtitleText;

        [Header("Cards  (assign 3 card roots)")]
        public BuffCardUI[] Cards = new BuffCardUI[3];

        [Header("Skip button (optional)")]
        public Button SkipButton;

        public event Action OnSelectionComplete;
        public bool HasRegistry => Registry != null;

        private void Awake()
        {
            // Hide via CanvasGroup so this object stays active in the hierarchy —
            // SetActive(false) prevents StartCoroutine from working when Show() is called.
            if (RootGroup == null)
                RootGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            RootGroup.alpha          = 0f;
            RootGroup.interactable   = false;
            RootGroup.blocksRaycasts = false;
        }

        /// <summary>Shows the panel and offers 3 random buffs.</summary>
        public void Show()
        {
            if (Registry == null)
            {
                Debug.LogWarning("[BuffSelectionUI] No BuffRegistry assigned — skipping buff selection.");
                OnSelectionComplete?.Invoke();
                return;
            }

            var picks = Registry.PickRandom(Cards.Length);

            // Pad with nulls if pool is smaller than card count
            while (picks.Count < Cards.Length) picks.Add(null);

            for (int i = 0; i < Cards.Length; i++)
            {
                if (Cards[i] == null) continue;

                if (picks[i] != null)
                {
                    Cards[i].gameObject.SetActive(true);
                    Cards[i].Setup(picks[i], OnCardChosen);
                }
                else
                {
                    Cards[i].gameObject.SetActive(false);
                }
            }

            if (TitleText)    TitleText.text    = "CHOOSE YOUR BLESSING";
            if (SubtitleText) SubtitleText.text = "Pick one to carry forward into the next battle.";

            SkipButton?.onClick.RemoveAllListeners();
            SkipButton?.onClick.AddListener(Skip);

            RootGroup.interactable   = true;
            RootGroup.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }

        private void OnCardChosen(BuffSO buff)
        {
            GameSession.Instance?.AddBuff(buff);
            StartCoroutine(FadeOutAndComplete());
        }

        private void Skip() => StartCoroutine(FadeOutAndComplete());

        private IEnumerator FadeIn()
        {
            RootGroup.alpha = 0f;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                RootGroup.alpha = Mathf.Clamp01(t / 0.45f);
                yield return null;
            }
            RootGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndComplete()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                RootGroup.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            RootGroup.alpha          = 0f;
            RootGroup.interactable   = false;
            RootGroup.blocksRaycasts = false;
            OnSelectionComplete?.Invoke();
        }
    }
}
