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
    /// Overlay panel for Field Decision (Dilemma) events.
    ///
    /// Shows:
    ///   • Event name + prompt text
    ///   • 2–3 choice buttons, each showing label, flavor, and resource delta preview
    ///   • HP delta preview (e.g. "-10% HP" or "+20% HP")
    ///   • Class XP preview if applicable
    ///
    /// Fires a callback with the chosen index.
    /// Hidden via CanvasGroup — never SetActive(false).
    /// </summary>
    public class SectorEventUI : MonoBehaviour
    {
        [Header("Canvas Group")]
        public CanvasGroup RootGroup;

        [Header("Header")]
        public TextMeshProUGUI EventNameText;
        public TextMeshProUGUI PromptText;
        public Image           EventIcon;

        [Header("Choice Buttons Container")]
        public Transform   ChoiceContainer;
        public GameObject  ChoiceButtonPrefab;

        [Header("Skip / Close")]
        public Button SkipButton;

        // ── Runtime ───────────────────────────────────────────────────────────
        private Action<int>            _callback;
        private readonly List<Button>  _choiceButtons = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (RootGroup == null)
                RootGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Hide();
            SkipButton?.onClick.AddListener(() => Close(-1));
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(SectorEventSO ev, Action<int> onChoice)
        {
            _callback = onChoice;

            if (EventNameText) EventNameText.text = ev.EventName;
            if (PromptText)    PromptText.text    = ev.Prompt;
            if (EventIcon)
            {
                EventIcon.gameObject.SetActive(ev.Icon != null);
                if (ev.Icon != null) EventIcon.sprite = ev.Icon;
            }

            BuildChoices(ev);
            Reveal();
        }

        public void Hide()
        {
            if (RootGroup == null) return;
            RootGroup.alpha          = 0f;
            RootGroup.interactable   = false;
            RootGroup.blocksRaycasts = false;
        }

        // ── Choice Building ───────────────────────────────────────────────────

        private void BuildChoices(SectorEventSO ev)
        {
            // Clear old buttons
            foreach (var b in _choiceButtons) if (b != null) Destroy(b.gameObject);
            _choiceButtons.Clear();
            if (ChoiceContainer != null)
                foreach (Transform child in ChoiceContainer) Destroy(child.gameObject);

            var session = GameSession.Instance;

            for (int i = 0; i < ev.Choices.Count; i++)
            {
                var choice = ev.Choices[i];
                int idx    = i;

                // Skip choices that require a class level the player hasn't reached
                if (choice.RequiresClassLevel)
                {
                    int classLevel = 0;
                    if (session?.ActiveClass != null)
                        classLevel = CommanderProfile.Instance?.GetRecord(session.ActiveClass.name).Level ?? 0;
                    if (classLevel < choice.MinClassLevel) continue;
                }

                var go  = ChoiceButtonPrefab != null
                    ? Instantiate(ChoiceButtonPrefab, ChoiceContainer)
                    : CreateDefaultChoiceGO(choice.Label);

                go.transform.SetParent(ChoiceContainer, false);

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                btn.onClick.AddListener(() => Close(idx));
                _choiceButtons.Add(btn);

                // Fill in text fields by convention (Label/Flavor/Cost TMPs)
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length >= 1) texts[0].text = choice.Label;
                if (texts.Length >= 2) texts[1].text = choice.FlavorText;
                if (texts.Length >= 3) texts[2].text = BuildCostPreview(choice, session);
            }
        }

        private string BuildCostPreview(SectorChoice choice, GameSession session)
        {
            var parts = new List<string>();
            var d = choice.ResourceDelta;

            if (d.Gold    != 0) parts.Add(d.Gold    > 0 ? $"<color=#FFD700>+{d.Gold} Gold</color>"     : $"<color=#FF8888>{d.Gold} Gold</color>");
            if (d.Scrap   != 0) parts.Add(d.Scrap   > 0 ? $"<color=#AAAAAA>+{d.Scrap} Scrap</color>"   : $"<color=#FF8888>{d.Scrap} Scrap</color>");
            if (d.Rations != 0) parts.Add(d.Rations > 0 ? $"<color=#88FF88>+{d.Rations} Rations</color>" : $"<color=#FF8888>{d.Rations} Rations</color>");
            if (d.Morale  != 0) parts.Add(d.Morale  > 0 ? $"<color=#88CCFF>+{d.Morale} Morale</color>"  : $"<color=#FF8888>{d.Morale} Morale</color>");

            if (choice.HeroHPDelta != 0f)
            {
                float pct = choice.HeroHPDelta * 100f;
                parts.Add(pct > 0
                    ? $"<color=#88FF88>+{pct:0}% HP</color>"
                    : $"<color=#FF8888>{pct:0}% HP</color>");
            }

            if (choice.ClassXPReward > 0)
                parts.Add($"<color=#CCAAFF>+{choice.ClassXPReward} Class XP</color>");

            return string.Join("  ", parts);
        }

        private GameObject CreateDefaultChoiceGO(string label)
        {
            var go = new GameObject("ChoiceButton");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 60);
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.28f);
            go.AddComponent<Button>();

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0); textRect.offsetMax = new Vector2(-10, 0);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return go;
        }

        // ── Reveal / Close ────────────────────────────────────────────────────

        private void Reveal() => StartCoroutine(FadeIn());

        private void Close(int choiceIndex)
        {
            Hide();
            _callback?.Invoke(choiceIndex);
            _callback = null;
        }

        private IEnumerator FadeIn()
        {
            RootGroup.alpha          = 0f;
            RootGroup.interactable   = false;
            RootGroup.blocksRaycasts = true;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                RootGroup.alpha = Mathf.Clamp01(t / 0.25f);
                yield return null;
            }
            RootGroup.alpha        = 1f;
            RootGroup.interactable = true;
        }
    }
}
