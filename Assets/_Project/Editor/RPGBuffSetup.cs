using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.Data;
using RPG.UI;

/// <summary>
/// Creates all BuffSO assets, the BuffRegistry, and wires BuffSelectionUI
/// into the existing CombatStage scene.
///
/// Run via  RPG > Setup Buff System
/// </summary>
public class RPGBuffSetup
{
    [MenuItem("RPG/Setup Buff System")]
    public static void Execute()
    {
        EnsureFolders();
        var registry = CreateBuffAssets();
        PatchCombatScene(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGBuffSetup] ✅ Buff system setup complete.");
    }

    // ── Folders ───────────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        foreach (var path in new[] { "Assets/_Project/ScriptableObjects/Buffs" })
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    // ── Buff assets ───────────────────────────────────────────────────────────
    static BuffRegistry CreateBuffAssets()
    {
        var buffs = new[]
        {
            // ── Common ──────────────────────────────────────────────────────
            MakeBuff("Buff_IronSkin",
                "Iron Skin",
                "Your skin hardens like tempered steel.",
                BuffRarity.Common,
                bonusMaxHP: 15, bonusDefense: 3),

            MakeBuff("Buff_WarriorSpirit",
                "Warrior's Spirit",
                "Rage fuels every strike.",
                BuffRarity.Common,
                bonusAttack: 5),

            MakeBuff("Buff_ArcaneReserve",
                "Arcane Reserve",
                "A deeper well of magical energy.",
                BuffRarity.Common,
                bonusMaxMP: 10, bonusMagicAttack: 3),

            MakeBuff("Buff_Swiftness",
                "Swiftness",
                "Your feet barely touch the ground.",
                BuffRarity.Common,
                bonusSpeed: 3, bonusMovement: 1),

            MakeBuff("Buff_StoneFortress",
                "Stone Fortress",
                "Defense holds firm under any assault.",
                BuffRarity.Common,
                bonusDefense: 5, bonusMagicDefense: 2),

            MakeBuff("Buff_VitalSurge",
                "Vital Surge",
                "Life pulses stronger within you.",
                BuffRarity.Common,
                bonusMaxHP: 25),

            // ── Rare ────────────────────────────────────────────────────────
            MakeBuff("Buff_BerserkerBrand",
                "Berserker Brand",
                "Raw power at the cost of caution.",
                BuffRarity.Rare,
                bonusAttack: 10, bonusDefense: -3),

            MakeBuff("Buff_SpellSurge",
                "Spell Surge",
                "Magic crackles at your fingertips.",
                BuffRarity.Rare,
                bonusMagicAttack: 8, bonusMaxMP: 15),

            MakeBuff("Buff_Wanderer",
                "Wanderer",
                "You move with uncanny agility.",
                BuffRarity.Rare,
                bonusMovement: 2, bonusSpeed: 5),

            MakeBuff("Buff_PhoenixVow",
                "Phoenix Vow",
                "Bound to life itself.",
                BuffRarity.Rare,
                bonusMaxHP: 40, bonusMaxMP: 20),

            // ── Epic ────────────────────────────────────────────────────────
            MakeBuff("Buff_VoidEssence",
                "Void Essence",
                "Power from beyond the realm floods every cell.",
                BuffRarity.Epic,
                bonusAttack: 12, bonusMagicAttack: 12, bonusMaxHP: 30),

            MakeBuff("Buff_TimelessBody",
                "Timeless Body",
                "You act before the enemy can blink.",
                BuffRarity.Epic,
                bonusSpeed: 12, bonusMovement: 2, bonusDefense: 5),
        };

        // Registry
        var registry = LoadOrCreate<BuffRegistry>(
            "Assets/_Project/ScriptableObjects/Buffs/BuffRegistry.asset");
        registry.Buffs = buffs;
        EditorUtility.SetDirty(registry);
        return registry;
    }

    static BuffSO MakeBuff(string fileName, string buffName, string desc,
        BuffRarity rarity,
        int bonusMaxHP = 0, int bonusMaxMP = 0,
        int bonusAttack = 0, int bonusDefense = 0,
        int bonusMagicAttack = 0, int bonusMagicDefense = 0,
        int bonusSpeed = 0, int bonusMovement = 0)
    {
        string path = $"Assets/_Project/ScriptableObjects/Buffs/{fileName}.asset";
        var b = LoadOrCreate<BuffSO>(path);
        b.BuffName       = buffName;
        b.Description    = desc;
        b.Rarity         = rarity;
        b.AccentColor    = rarity switch
        {
            BuffRarity.Rare => new Color(0.25f, 0.55f, 1f),
            BuffRarity.Epic => new Color(0.65f, 0.20f, 1f),
            _               => new Color(0.55f, 0.55f, 0.65f),
        };
        b.BonusMaxHP        = bonusMaxHP;
        b.BonusMaxMP        = bonusMaxMP;
        b.BonusAttack       = bonusAttack;
        b.BonusDefense      = bonusDefense;
        b.BonusMagicAttack  = bonusMagicAttack;
        b.BonusMagicDefense = bonusMagicDefense;
        b.BonusSpeed        = bonusSpeed;
        b.BonusMovement     = bonusMovement;
        EditorUtility.SetDirty(b);
        return b;
    }

    // ── Patch CombatStage ─────────────────────────────────────────────────────
    static void PatchCombatScene(BuffRegistry registry)
    {
        string scenePath = "Assets/_Project/Scenes/CombatStage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find or create the HUD canvas
        var hud = Object.FindFirstObjectByType<RPG.UI.CombatHUD>();
        if (hud == null)
        {
            Debug.LogWarning("[RPGBuffSetup] CombatHUD not found in CombatStage — skipping buff UI patch.");
            EditorSceneManager.SaveScene(scene, scenePath);
            return;
        }

        // Build BuffSelectionUI on a fullscreen canvas group inside the HUD canvas
        var hudCanvas = hud.GetComponentInParent<Canvas>();
        Transform canvasRoot = hudCanvas != null ? hudCanvas.transform : hud.transform;

        var buffRoot = BuildBuffSelectionUI(canvasRoot, registry);
        var buffUI   = buffRoot.GetComponent<BuffSelectionUI>();

        // Wire into HUD
        hud.BuffSelection = buffUI;

        EditorUtility.SetDirty(hud);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    static GameObject BuildBuffSelectionUI(Transform parent, BuffRegistry registry)
    {
        // Remove old one if re-running
        var existing = parent.Find("BuffSelectionUI");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // ── Root (fullscreen dark overlay) ────────────────────────────────
        var root = MakePanel(parent, "BuffSelectionUI",
            new Color(0.02f, 0.01f, 0.06f, 0.96f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Vector2(0.5f, 0.5f));

        var cg = root.AddComponent<CanvasGroup>();
        root.SetActive(false);  // starts hidden

        // ── Title ─────────────────────────────────────────────────────────
        var titleTmp = MakeText(root.transform, "TitleText", "CHOOSE YOUR BLESSING",
            60f, new Color(1f, 0.84f, 0f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(900f, 80f));

        var subTmp = MakeText(root.transform, "SubtitleText",
            "Pick one to carry forward into the next battle.",
            22f, new Color(0.75f, 0.72f, 0.90f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -148f), new Vector2(700f, 36f));

        // ── 3 Cards ───────────────────────────────────────────────────────
        float cardW = 300f, cardH = 400f, gap = 30f;
        float totalW = cardW * 3 + gap * 2;
        float startX = -totalW / 2f + cardW / 2f;

        var cards = new BuffCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            float x = startX + i * (cardW + gap);
            var card = BuildCard(root.transform, $"Card_{i}", x, -20f, cardW, cardH);
            cards[i] = card;
        }

        // ── Skip button ───────────────────────────────────────────────────
        var skipBtn = MakeButton(root.transform, "SkipBtn", "Skip",
            new Color(0.2f, 0.2f, 0.25f), new Color(0.7f, 0.7f, 0.8f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(160f, 44f), new Vector2(0.5f, 0f));

        // ── Wire BuffSelectionUI component ────────────────────────────────
        var ui         = root.AddComponent<BuffSelectionUI>();
        ui.Registry    = registry;
        ui.RootGroup   = cg;
        ui.TitleText   = titleTmp;
        ui.SubtitleText = subTmp;
        ui.Cards        = cards;
        ui.SkipButton   = skipBtn;

        return root;
    }

    static BuffCardUI BuildCard(Transform parent, string name,
        float x, float yCenter, float w, float h)
    {
        var card = MakePanel(parent, name,
            new Color(0.08f, 0.06f, 0.16f, 0.97f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, yCenter), new Vector2(w, h),
            new Vector2(0.5f, 0.5f));

        // Rarity label
        var rarityTmp = MakeText(card.transform, "RarityText", "COMMON",
            14f, new Color(0.55f, 0.55f, 0.65f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(w - 20f, 24f));

        // Name
        var nameTmp = MakeText(card.transform, "NameText", "Buff Name",
            26f, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(w - 20f, 40f));

        // Divider
        MakePanel(card.transform, "Divider",
            new Color(0.4f, 0.3f, 0.6f, 0.5f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -98f), new Vector2(w - 40f, 2f),
            new Vector2(0.5f, 1f));

        // Description
        var descTmp = MakeText(card.transform, "DescText", "Description goes here.",
            17f, new Color(0.78f, 0.76f, 0.92f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -118f), new Vector2(w - 30f, 100f));
        descTmp.alignment = TextAlignmentOptions.Top;

        // Stat deltas
        var statsTmp = MakeText(card.transform, "StatsText", "",
            15f, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -240f), new Vector2(w - 20f, 80f));
        statsTmp.richText = true;

        // Select button
        var btn = MakeButton(card.transform, "SelectBtn", "CHOOSE",
            new Color(0.45f, 0.25f, 0.70f), Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 18f), new Vector2(220f, 50f), new Vector2(0.5f, 0f));

        var cardUI             = card.AddComponent<BuffCardUI>();
        cardUI.CardBackground  = card.GetComponent<Image>();
        cardUI.NameText        = nameTmp;
        cardUI.RarityText      = rarityTmp;
        cardUI.DescText        = descTmp;
        cardUI.StatsText       = statsTmp;
        cardUI.SelectButton    = btn;

        return cardUI;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>(); img.color = color;
        var r    = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.anchoredPosition = anchoredPos; r.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float size, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos; r.sizeDelta = sizeDelta;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Color bg, Color textColor,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, Vector2 pivot)
    {
        var go  = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = bg;
        var btn = go.AddComponent<Button>();
        var col = btn.colors;
        col.highlightedColor = bg * 1.3f;
        col.pressedColor     = bg * 0.7f;
        btn.colors = col;
        btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.anchoredPosition = anchoredPos; r.sizeDelta = size;

        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = label; lbl.fontSize = 20f; lbl.color = textColor;
        lbl.alignment = TextAlignmentOptions.Center;
        var lr = lblGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        return btn;
    }
}
