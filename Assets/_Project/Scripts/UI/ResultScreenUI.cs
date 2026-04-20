using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Post-combat result overlay shown inside CombatStage.
    ///
    /// Game loop:
    ///   Victory → "Continue →" → AreaMap (return to exploring the current area)
    ///   Defeat  → "Retry"       → same combat again
    ///           → "Abandon Run" → Main Menu (run ends)
    ///
    /// Hidden via CanvasGroup.alpha — never SetActive(false), preserves coroutines.
    /// </summary>
    public class ResultScreenUI : MonoBehaviour
    {
        [Header("Canvas Group  (on this GameObject)")]
        public CanvasGroup CanvasGroup;

        [Header("Text")]
        public TextMeshProUGUI ResultText;
        public TextMeshProUGUI FlavorText;

        [Header("Victory")]
        public Button          ContinueButton;
        public TextMeshProUGUI ContinueLabel;

        [Header("Defeat")]
        public Button          RetryButton;
        public Button          AbandonButton;
        public TextMeshProUGUI AbandonLabel;

        [Header("Colors")]
        public Color VictoryColor = new Color(1f, 0.84f, 0f);
        public Color DefeatColor  = new Color(0.85f, 0.15f, 0.15f);

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (CanvasGroup == null)
                CanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        private void Start()
        {
            ContinueButton?.onClick.AddListener(OnContinue);
            RetryButton?.onClick.AddListener(OnRetry);
            AbandonButton?.onClick.AddListener(OnAbandon);
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        public void Show(bool isVictory)
        {
            if (isVictory)
            {
                if (ResultText) { ResultText.text = "VICTORY!"; ResultText.color = VictoryColor; }
                if (FlavorText)   FlavorText.text = "The enemies are defeated.\nOnward through the area.";

                ContinueButton?.gameObject.SetActive(true);
                RetryButton?.gameObject.SetActive(false);
                AbandonButton?.gameObject.SetActive(false);

                if (ContinueLabel) ContinueLabel.text = "Continue  \u2192";
            }
            else
            {
                if (ResultText) { ResultText.text = "DEFEAT"; ResultText.color = DefeatColor; }
                if (FlavorText)   FlavorText.text = "The Hero has fallen.\nFight on or end the run?";

                ContinueButton?.gameObject.SetActive(false);
                RetryButton?.gameObject.SetActive(true);
                AbandonButton?.gameObject.SetActive(true);

                if (AbandonLabel) AbandonLabel.text = "Abandon Run";
            }

            CanvasGroup.interactable   = true;
            CanvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }

        private void Hide()
        {
            CanvasGroup.alpha          = 0f;
            CanvasGroup.interactable   = false;
            CanvasGroup.blocksRaycasts = false;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnContinue()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCombatVictory();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("AreaMap");
        }

        private void OnRetry()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RetryCombat();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnAbandon()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // ── Animation ─────────────────────────────────────────────────────────

        private IEnumerator FadeIn()
        {
            CanvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                CanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t / 0.5f);
                yield return null;
            }
            CanvasGroup.alpha = 1f;
        }
    }
}
