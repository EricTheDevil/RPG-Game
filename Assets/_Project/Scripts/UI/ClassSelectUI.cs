using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.UI
{
    /// <summary>
    /// Shown once per run before the World Map. Player picks one of 3 beginner classes.
    ///
    /// Layout:
    ///   • 3 class card panels side-by-side (ClassCardPanel prefab or auto-created)
    ///   • Each card shows: class name, tier badge, current level/XP, stat preview,
    ///     next level reward, and evolution task status
    ///   • Confirm button activates after selection
    ///   • Commander XP / level shown in top bar
    /// </summary>
    public class ClassSelectUI : MonoBehaviour
    {
        [Header("Commander Bar")]
        public TextMeshProUGUI CommanderNameText;
        public TextMeshProUGUI CommanderLevelText;
        public Image           CommanderXPFill;

        [Header("Class Cards")]
        public Transform  CardContainer;
        public GameObject ClassCardPrefab;  // assign in inspector; auto-created if null

        [Header("Confirm")]
        public Button          ConfirmButton;
        public TextMeshProUGUI ConfirmButtonLabel;

        [Header("Description Panel")]
        public TextMeshProUGUI SelectedClassNameText;
        public TextMeshProUGUI SelectedClassDescText;
        public TextMeshProUGUI EvolutionTaskText;

        // ── Runtime ───────────────────────────────────────────────────────────
        private ClassDefinitionSO   _selected;
        private readonly List<ClassCardEntry> _cards = new();

        private struct ClassCardEntry
        {
            public ClassDefinitionSO Class;
            public GameObject        Root;
            public Button            CardButton;
            public Image             SelectionBorder;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            ConfirmButton?.onClick.AddListener(OnConfirm);
            SetConfirmInteractable(false);

            RefreshCommanderBar();
            BuildCards();
        }

        // ── Commander Bar ─────────────────────────────────────────────────────

        private void RefreshCommanderBar()
        {
            var profile = CommanderProfile.Instance;
            if (profile == null) return;

            if (CommanderLevelText)
                CommanderLevelText.text = $"Commander  Lv.{profile.CommanderLevel}";

            if (CommanderNameText)
                CommanderNameText.text = "Commander";

            if (CommanderXPFill)
            {
                float ratio = CommanderProfile.CommanderXPPerLevel > 0
                    ? (float)(profile.CommanderXP % CommanderProfile.CommanderXPPerLevel) / CommanderProfile.CommanderXPPerLevel
                    : 0f;
                CommanderXPFill.fillAmount = ratio;
            }
        }

        // ── Card Building ─────────────────────────────────────────────────────

        private void BuildCards()
        {
            foreach (var entry in _cards) if (entry.Root != null) Destroy(entry.Root);
            _cards.Clear();

            var classes = GameManager.Instance?.BeginnerClasses;
            if (classes == null) return;

            var profile = CommanderProfile.Instance;

            foreach (var cls in classes)
            {
                if (cls == null) continue;

                var go = ClassCardPrefab != null
                    ? Instantiate(ClassCardPrefab, CardContainer)
                    : CreateDefaultCardGO(cls.ClassName);

                go.transform.SetParent(CardContainer, false);

                var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                var border = go.transform.Find("SelectionBorder")?.GetComponent<Image>();

                var entry = new ClassCardEntry
                {
                    Class           = cls,
                    Root            = go,
                    CardButton      = btn,
                    SelectionBorder = border,
                };
                _cards.Add(entry);

                var record = profile?.GetRecord(cls.name);
                int level  = record?.Level ?? 0;
                int xp     = record?.XP    ?? 0;
                int xpNext = cls.XPForNextLevel(level);

                // Fill text fields by name convention
                SetChildText(go, "ClassName",    cls.ClassName);
                SetChildText(go, "TierBadge",    cls.Tier.ToString());
                SetChildText(go, "LevelText",    level > 0 ? $"Lv.{level}" : "New");
                SetChildText(go, "XPText",       level < cls.MaxLevel ? $"{xp}/{xpNext} XP" : "MAX");
                SetChildText(go, "StatsPreview", BuildStatPreview(cls, level));

                // XP bar
                var xpFill = go.transform.Find("XPFill")?.GetComponent<Image>();
                if (xpFill != null)
                    xpFill.fillAmount = (xpNext > 0 && level < cls.MaxLevel)
                        ? Mathf.Clamp01((float)xp / xpNext) : 1f;

                // Evolution task hint
                string taskHint = "";
                if (cls.EvolutionTask != null)
                    taskHint = profile != null && profile.IsTaskCompleted(cls, cls.EvolutionTask)
                        ? $"<color=#88FF88>✓ {cls.EvolutionTask.TaskName}</color>"
                        : $"<color=#FFCC44>{cls.EvolutionTask.TaskName}</color>";
                SetChildText(go, "TaskHint", taskHint);

                var captured = entry;
                btn.onClick.AddListener(() => OnCardClicked(captured));

                if (border != null) border.enabled = false;
            }
        }

        // ── Interaction ───────────────────────────────────────────────────────

        private void OnCardClicked(ClassCardEntry entry)
        {
            _selected = entry.Class;

            // Update selection borders
            foreach (var c in _cards)
                if (c.SelectionBorder != null)
                    c.SelectionBorder.enabled = c.Class == _selected;

            // Update description panel
            if (SelectedClassNameText)
                SelectedClassNameText.text = _selected.ClassName;

            if (SelectedClassDescText)
                SelectedClassDescText.text = BuildStatPreview(_selected,
                    CommanderProfile.Instance?.GetRecord(_selected.name).Level ?? 0);

            if (EvolutionTaskText)
            {
                var task = _selected.EvolutionTask;
                if (task == null)
                    EvolutionTaskText.text = "";
                else
                {
                    bool done = CommanderProfile.Instance?.IsTaskCompleted(_selected, task) ?? false;
                    EvolutionTaskText.text = done
                        ? $"<color=#88FF88>Task complete: {task.TaskName}</color>"
                        : $"<color=#FFCC44>Task: {task.TaskName}\n<size=80%>{task.Description}</size></color>";
                }
            }

            SetConfirmInteractable(true);
        }

        private void OnConfirm()
        {
            if (_selected == null) return;
            GameManager.Instance?.OnClassSelected(_selected);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string BuildStatPreview(ClassDefinitionSO cls, int classLevel)
        {
            if (cls.BaseStatsSO == null) return "";

            var growth = cls.CumulativeGrowthAt(classLevel);
            var b      = cls.BaseStatsSO;

            return $"HP {b.MaxHP + growth.MaxHP}  " +
                   $"ATK {b.Attack + growth.Attack}  " +
                   $"DEF {b.Defense + growth.Defense}  " +
                   $"SPD {b.Speed + growth.Speed}";
        }

        private void SetChildText(GameObject root, string childName, string text)
        {
            var child = root.transform.Find(childName);
            if (child == null) return;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void SetConfirmInteractable(bool on)
        {
            if (ConfirmButton) ConfirmButton.interactable = on;
            if (ConfirmButtonLabel)
                ConfirmButtonLabel.text = on ? $"Begin as {_selected?.ClassName}  →" : "Select a Class";
        }

        // ── Default Card Prefab Fallback ──────────────────────────────────────

        private GameObject CreateDefaultCardGO(string className)
        {
            var go   = new GameObject($"Card_{className}");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 300);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f);

            go.AddComponent<Button>();

            // Selection border child
            var borderGO = new GameObject("SelectionBorder");
            borderGO.transform.SetParent(go.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3, -3); borderRect.offsetMax = new Vector2(3, 3);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.85f, 0.2f);
            borderImg.enabled = false;

            // Text fields stacked vertically
            string[] fields = { "ClassName", "TierBadge", "LevelText", "XPText", "StatsPreview", "TaskHint" };
            for (int i = 0; i < fields.Length; i++)
            {
                var tGO   = new GameObject(fields[i]);
                tGO.transform.SetParent(go.transform, false);
                var tRect = tGO.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 1); tRect.anchorMax = new Vector2(1, 1);
                tRect.pivot     = new Vector2(0.5f, 1);
                tRect.anchoredPosition = new Vector2(0, -10 - i * 36);
                tRect.sizeDelta = new Vector2(0, 32);
                var tmp = tGO.AddComponent<TextMeshProUGUI>();
                tmp.fontSize  = i == 0 ? 18 : 13;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            return go;
        }
    }
}
