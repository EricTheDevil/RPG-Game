using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Combat;
using RPG.Units;

namespace RPG.UI
{
    /// <summary>
    /// TFT-style autobattle HUD.
    ///
    /// Listens to CombatManager events — never polls.
    /// Supports N player units and N enemy units via panel arrays.
    /// Banner shows scale-punch animation on phase transitions.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        [Header("Status Panels — Player Team")]
        [Tooltip("Wire one UnitStatusPanel per expected player unit slot.")]
        public UnitStatusPanel[] PlayerPanels = new UnitStatusPanel[0];

        [Header("Status Panels — Enemy Team")]
        [Tooltip("Wire one UnitStatusPanel per expected enemy unit slot.")]
        public UnitStatusPanel[] EnemyPanels  = new UnitStatusPanel[0];

        [Header("Phase Banner")]
        public RectTransform   BannerRect;
        public TextMeshProUGUI BannerText;
        public CanvasGroup     BannerGroup;

        [Header("Combat Log")]
        public TextMeshProUGUI LogText;
        public int             MaxLogLines = 8;
        public ScrollRect      LogScroll;

        [Header("Result / Buff Screens")]
        public ResultScreenUI  ResultScreen;
        public BuffSelectionUI BuffSelection;

        [Header("Battle Speed Button")]
        public Button          SpeedToggleButton;
        public TextMeshProUGUI SpeedToggleLabel;

        // ── Private ───────────────────────────────────────────────────────────
        private readonly Queue<string> _logQueue = new();
        private Coroutine _bannerRoutine;
        private float _speedMultiplier = 1f;

        private static readonly float[] SpeedOptions = { 1f, 1.5f, 2f };
        private int _speedIndex = 0;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Start()
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;

            cm.OnPhaseChanged += HandlePhase;
            cm.OnUnitActed    += HandleUnitActed;
            cm.OnCombatLog    += AddLog;

            cm.OnSpawned += (players, enemies) =>
            {
                // Wire player panels
                for (int i = 0; i < PlayerPanels.Length; i++)
                {
                    if (PlayerPanels[i] == null) continue;
                    if (i < players.Count)
                    {
                        PlayerPanels[i].gameObject.SetActive(true);
                        PlayerPanels[i].SetUnit(players[i]);
                    }
                    else
                    {
                        PlayerPanels[i].gameObject.SetActive(false);
                    }
                }

                // Wire enemy panels
                for (int i = 0; i < EnemyPanels.Length; i++)
                {
                    if (EnemyPanels[i] == null) continue;
                    if (i < enemies.Count)
                    {
                        EnemyPanels[i].gameObject.SetActive(true);
                        EnemyPanels[i].SetUnit(enemies[i]);
                    }
                    else
                    {
                        EnemyPanels[i].gameObject.SetActive(false);
                    }
                }

                // InitiativeBar removed — real-time system has no CT ordering to display
            };

            cm.OnVictory += () =>
            {
                ShowBanner("<color=#FFD700>VICTORY!</color>", 2.5f);
                StartCoroutine(ShowVictoryAfterDelay(1.5f));
            };

            cm.OnDefeat += () =>
            {
                ShowBanner("<color=#FF4444>DEFEAT</color>", 2.5f);
                StartCoroutine(ShowDefeatAfterDelay(1.5f));
            };

            SpeedToggleButton?.onClick.AddListener(CycleSpeed);
            UpdateSpeedLabel();

            if (BannerRect != null)
                BannerRect.gameObject.SetActive(false);
        }

        private IEnumerator ShowVictoryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (BuffSelection != null && BuffSelection.HasRegistry)
            {
                BuffSelection.OnSelectionComplete -= OnBuffDone;
                BuffSelection.OnSelectionComplete += OnBuffDone;
                BuffSelection.Show();
            }
            else
            {
                ResultScreen?.Show(true);
            }
        }

        private IEnumerator ShowDefeatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResultScreen?.Show(false);
        }

        // ── Phase Handling ────────────────────────────────────────────────────
        private void HandlePhase(CombatPhase phase)
        {
            if (phase == CombatPhase.Intro)
                ShowBanner("<color=#88CCFF>Auto-Battle  —  Begin!</color>", 2f);
        }

        private void HandleUnitActed(Unit unit) { }

        // ── Log ───────────────────────────────────────────────────────────────
        public void AddLog(string message)
        {
            _logQueue.Enqueue(message);
            while (_logQueue.Count > MaxLogLines) _logQueue.Dequeue();

            if (LogText) LogText.text = string.Join("\n", _logQueue);

            if (LogScroll != null)
                StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (LogScroll) LogScroll.verticalNormalizedPosition = 0f;
        }

        // ── Banner (scale-punch + fade) ───────────────────────────────────────
        private void ShowBanner(string text, float holdDuration)
        {
            if (BannerRect == null) return;
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            if (BannerText) BannerText.text = text;
            BannerRect.gameObject.SetActive(true);
            _bannerRoutine = StartCoroutine(AnimateBanner(holdDuration));
        }

        private IEnumerator AnimateBanner(float holdDuration)
        {
            if (BannerGroup != null)
            {
                BannerGroup.alpha     = 0f;
                BannerRect.localScale = Vector3.one * 1.4f;

                // Punch in: scale 1.4 → 1.0, fade 0 → 1
                float t = 0f;
                while (t < 0.28f)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.SmoothStep(0f, 1f, t / 0.28f);
                    BannerGroup.alpha     = p;
                    BannerRect.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, p);
                    yield return null;
                }
                BannerGroup.alpha     = 1f;
                BannerRect.localScale = Vector3.one;

                // Overshoot bounce back
                t = 0f;
                while (t < 0.1f)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Sin(t / 0.1f * Mathf.PI);
                    BannerRect.localScale = Vector3.one * (1f - p * 0.05f);
                    yield return null;
                }
                BannerRect.localScale = Vector3.one;

                yield return new WaitForSecondsRealtime(holdDuration);

                // Fade out + scale down slightly
                t = 0f;
                while (t < 0.35f)
                {
                    t += Time.unscaledDeltaTime;
                    float p = t / 0.35f;
                    BannerGroup.alpha     = 1f - p;
                    BannerRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.88f, p);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            BannerRect.gameObject.SetActive(false);
            BannerRect.localScale = Vector3.one;
        }

        // ── Speed Toggle ──────────────────────────────────────────────────────
        private void CycleSpeed()
        {
            _speedIndex      = (_speedIndex + 1) % SpeedOptions.Length;
            _speedMultiplier = SpeedOptions[_speedIndex];
            Time.timeScale   = _speedMultiplier;
            UpdateSpeedLabel();
        }

        private void UpdateSpeedLabel()
        {
            if (SpeedToggleLabel)
                SpeedToggleLabel.text = _speedMultiplier >= 2f   ? ">>  x2"   :
                                        _speedMultiplier >= 1.5f ? ">  x1.5" : ">  x1";
        }

        private void OnDestroy() => Time.timeScale = 1f;

        // ── Buff / Result callbacks ───────────────────────────────────────────
        private void OnBuffDone()
        {
            if (RPG.Core.GameManager.Instance != null)
                RPG.Core.GameManager.Instance.OnCombatVictory();
            else
                ResultScreen?.Show(true);
        }
    }
}
