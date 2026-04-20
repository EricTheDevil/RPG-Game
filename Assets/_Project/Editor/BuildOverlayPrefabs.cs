using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using RPG.UI;

/// <summary>
/// Builds ShopOverlayUI and LevelUpOverlayUI prefabs from scratch and saves them
/// to Assets/_Project/Prefabs/. Run via RPG > Build Overlay Prefabs.
/// </summary>
public class BuildOverlayPrefabs
{
    const string PREFAB_DIR = "Assets/_Project/Prefabs";

    [MenuItem("RPG/Build Overlay Prefabs")]
    public static void Execute()
    {
        BuildShopOverlay();
        BuildLevelUpOverlay();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildOverlayPrefabs] ✅ ShopOverlay + LevelUpOverlay prefabs created.");
    }

    // ── Shop Overlay ─────────────────────────────────────────────────────────

    static void BuildShopOverlay()
    {
        var root = CreateFullscreenPanel("ShopOverlay", new Color(0.04f, 0.02f, 0.12f, 0.95f));
        var shopUI = root.AddComponent<ShopOverlayUI>();

        // Title
        var title = CreateLabel(root.transform, "TitleText", "Wandering Merchant", 28, TextAlignmentOptions.Center);
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -24f));
        shopUI.TitleText = title.GetComponent<TextMeshProUGUI>();

        // Resource bar
        var resBar = CreatePanel(root.transform, "ResourceBar", new Color(0f, 0f, 0f, 0.4f));
        SetRect(resBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -120f), new Vector2(0f, -80f));
        var goldTxt  = CreateLabel(resBar.transform, "GoldText",  "Gold: 0",  18, TextAlignmentOptions.Left);
        SetRect(goldTxt, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(10f, 4f), new Vector2(-4f, -4f));
        var scrapTxt = CreateLabel(resBar.transform, "ScrapText", "Scrap: 0", 18, TextAlignmentOptions.Right);
        SetRect(scrapTxt, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(4f, 4f), new Vector2(-10f, -4f));
        shopUI.GoldText  = goldTxt.GetComponent<TextMeshProUGUI>();
        shopUI.ScrapText = scrapTxt.GetComponent<TextMeshProUGUI>();

        // Scroll view for buff rows
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(root.transform, false);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        SetRect(scrollGo, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);

        var viewport = CreatePanel(scrollGo.transform, "Viewport", Color.clear);
        SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 8f;
        vlg.padding             = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment      = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        shopUI.RowContainer = content.transform;

        // Row prefab (ShopRowUI)
        var rowPrefab = BuildShopRowPrefab();
        shopUI.RowPrefab = rowPrefab;

        // Close button
        var closeBtn = CreateButton(root.transform, "CloseButton", "Close", 20);
        SetRect(closeBtn, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), new Vector2(0f, 12f), new Vector2(0f, 52f));
        shopUI.CloseButton = closeBtn.GetComponent<Button>();

        root.SetActive(false);

        // Wire BuffRegistry
        var registry = AssetDatabase.LoadAssetAtPath<RPG.Data.BuffRegistry>(
            "Assets/_Project/ScriptableObjects/Buffs/BuffRegistry.asset");
        if (registry != null) shopUI.Registry = registry;

        SavePrefab(root, "ShopOverlay");
        Object.DestroyImmediate(root);
    }

    static GameObject BuildShopRowPrefab()
    {
        var row = new GameObject("ShopRow");
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 80f);
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.06f, 0.20f, 0.80f);

        var rowUI = row.AddComponent<ShopRowUI>();

        // Icon
        var iconGo = CreatePanel(row.transform, "Icon", Color.white);
        SetRect(iconGo, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 8f), new Vector2(68f, -8f));
        iconGo.GetComponent<Image>().preserveAspect = true;
        rowUI.Icon = iconGo.GetComponent<Image>();

        // Name
        var nameLbl = CreateLabel(row.transform, "NameText", "Buff Name", 18, TextAlignmentOptions.Left);
        SetRect(nameLbl, new Vector2(0f, 0.5f), new Vector2(0.65f, 1f), new Vector2(76f, 0f), new Vector2(-4f, -4f));
        rowUI.NameText = nameLbl.GetComponent<TextMeshProUGUI>();

        // Stats
        var statsLbl = CreateLabel(row.transform, "StatsText", "+0 ATK", 14, TextAlignmentOptions.Left);
        SetRect(statsLbl, new Vector2(0f, 0f), new Vector2(0.65f, 0.5f), new Vector2(76f, 4f), new Vector2(-4f, 0f));
        var statsTmp = statsLbl.GetComponent<TextMeshProUGUI>();
        statsTmp.color = new Color(0.7f, 0.9f, 0.7f);
        rowUI.StatsText = statsTmp;

        // Price
        var priceLbl = CreateLabel(row.transform, "PriceText", "30G / 12S", 16, TextAlignmentOptions.Center);
        SetRect(priceLbl, new Vector2(0.65f, 0.1f), new Vector2(0.85f, 0.9f), Vector2.zero, Vector2.zero);
        rowUI.PriceText = priceLbl.GetComponent<TextMeshProUGUI>();

        // Buy button
        var buyBtn = CreateButton(row.transform, "BuyButton", "Buy", 16);
        SetRect(buyBtn, new Vector2(0.85f, 0.1f), new Vector2(1f, 0.9f), new Vector2(-4f, 0f), new Vector2(-8f, 0f));
        rowUI.BuyButton = buyBtn.GetComponent<Button>();

        var rowPrefab = SavePrefab(row, "ShopRow");
        Object.DestroyImmediate(row);
        return rowPrefab;
    }

    // ── LevelUp Overlay ───────────────────────────────────────────────────────

    static void BuildLevelUpOverlay()
    {
        var root  = CreateFullscreenPanel("LevelUpOverlay", new Color(0.03f, 0.01f, 0.08f, 0.97f));
        var lvlUI = root.AddComponent<LevelUpOverlayUI>();

        // Title
        var title = CreateLabel(root.transform, "TitleText", "Level Up — Choose an upgrade", 30, TextAlignmentOptions.Center);
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -90f), new Vector2(0f, -30f));
        lvlUI.TitleText = title.GetComponent<TextMeshProUGUI>();

        // Hero stats readout
        var statsLbl = CreateLabel(root.transform, "HeroStatsText", "", 16, TextAlignmentOptions.Center);
        SetRect(statsLbl, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -140f), new Vector2(0f, -90f));
        statsLbl.GetComponent<TextMeshProUGUI>().color = new Color(0.75f, 0.75f, 1f);
        lvlUI.HeroStatsText = statsLbl.GetComponent<TextMeshProUGUI>();

        // Choice container (horizontal)
        var choiceContainer = new GameObject("ChoiceContainer");
        choiceContainer.transform.SetParent(root.transform, false);
        SetRect(choiceContainer, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
        var hlg = choiceContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 16f;
        hlg.padding              = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        lvlUI.ChoiceContainer = choiceContainer.transform;

        // Choice prefab
        var choicePrefab = BuildChoicePrefab();
        lvlUI.ChoicePrefab = choicePrefab;

        // HeroStats SO
        var heroStats = AssetDatabase.LoadAssetAtPath<RPG.Data.UnitStatsSO>(
            "Assets/_Project/ScriptableObjects/Stats/HeroStats.asset");
        if (heroStats != null) lvlUI.HeroStatsSO = heroStats;

        // Close button (hidden until choice made)
        var closeBtn = CreateButton(root.transform, "CloseButton", "Continue", 20);
        SetRect(closeBtn, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), new Vector2(0f, 12f), new Vector2(0f, 52f));
        closeBtn.SetActive(false);
        lvlUI.CloseButton = closeBtn.GetComponent<Button>();

        root.SetActive(false);

        SavePrefab(root, "LevelUpOverlay");
        Object.DestroyImmediate(root);
    }

    static GameObject BuildChoicePrefab()
    {
        var choice = new GameObject("StatChoice");
        choice.AddComponent<RectTransform>();
        var bg = choice.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.04f, 0.18f, 0.90f);

        var btn = choice.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.08f, 0.04f, 0.18f, 0.90f);
        colors.highlightedColor = new Color(0.18f, 0.10f, 0.35f, 0.95f);
        colors.pressedColor     = new Color(0.05f, 0.02f, 0.10f, 1f);
        btn.colors = colors;
        btn.targetGraphic = bg;

        var nameLbl = CreateLabel(choice.transform, "NameText", "Might", 22, TextAlignmentOptions.Center);
        SetRect(nameLbl, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-8f, -8f));
        nameLbl.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var descLbl = CreateLabel(choice.transform, "DescText", "ATK +8", 18, TextAlignmentOptions.Center);
        SetRect(descLbl, new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(8f, 8f), new Vector2(-8f, 0f));
        descLbl.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 1f, 0.6f);

        var prefab = SavePrefab(choice, "StatChoice");
        Object.DestroyImmediate(choice);
        return prefab;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject CreateFullscreenPanel(string name, Color bg)
    {
        var go   = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bg;
        go.AddComponent<CanvasGroup>();
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = bg;
        return go;
    }

    static GameObject CreateLabel(Transform parent, string name, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = new Color(0.92f, 0.92f, 1f);
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label, float fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.08f, 0.30f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.15f, 0.45f, 1f);
        colors.pressedColor     = new Color(0.08f, 0.04f, 0.15f, 1f);
        btn.colors = colors;

        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var lRect = lblGo.AddComponent<RectTransform>();
        lRect.anchorMin = Vector2.zero;
        lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(8f, 4f);
        lRect.offsetMax = new Vector2(-8f, -4f);
        var tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.9f, 0.9f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = $"{PREFAB_DIR}/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"[BuildOverlayPrefabs] Saved {path}");
        return prefab;
    }
}
