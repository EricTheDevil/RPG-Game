using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.Combat;
using RPG.Units;
using RPG.UI;
using RPG.Grid;
using RPG.VFX;

/// <summary>
/// One-click patch: upgrades the CombatStage scene from the old turn-based
/// setup to the new TFT autobattle architecture.
///
/// Run via  RPG > Patch → Autobattle Scene
/// </summary>
public class RPGAutobattlePatch
{
    [MenuItem("RPG/Patch → Autobattle Scene")]
    public static void Execute()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("Combat"))
        {
            Debug.LogWarning("[AutobattlePatch] Open CombatStage scene first.");
            return;
        }

        PatchCombatSystems();
        PatchCombatManagerFields();
        PatchCombatHUD();
        BuildInitiativeBar();
        BuildBanner();
        BuildSpeedToggle();
        BuildFlashPanel();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AutobattlePatch] ✅ CombatStage patched for autobattle!");
    }

    // ── 1. Replace old TurnManager/EnemyAI with CombatTimeline + TraitSystem ──
    static void PatchCombatSystems()
    {
        // Remove deprecated objects
        RemoveIfExists("TurnManager");
        RemoveIfExists("EnemyAI");

        // Add CombatTimeline if missing
        if (GameObject.Find("CombatTimeline") == null)
        {
            var go = new GameObject("CombatTimeline");
            go.AddComponent<CombatTimeline>();
            Debug.Log("[AutobattlePatch] Created CombatTimeline.");
        }

        // Add TraitSystem if missing
        if (GameObject.Find("TraitSystem") == null)
        {
            var go = new GameObject("TraitSystem");
            go.AddComponent<TraitSystem>();
            Debug.Log("[AutobattlePatch] Created TraitSystem.");
        }
    }

    // ── 2. Wire CombatManager ─────────────────────────────────────────────────
    static void PatchCombatManagerFields()
    {
        var cmGo = GameObject.Find("CombatManager");
        if (cmGo == null) { Debug.LogError("[AutobattlePatch] CombatManager not found."); return; }
        var cm = cmGo.GetComponent<CombatManager>();
        if (cm == null) { Debug.LogError("[AutobattlePatch] CombatManager component missing."); return; }

        // Wire Timeline
        var timelineGo = GameObject.Find("CombatTimeline");
        if (timelineGo != null)
        {
            cm.Timeline = timelineGo.GetComponent<CombatTimeline>();
            Debug.Log("[AutobattlePatch] Wired CombatTimeline.");
        }

        // Wire TraitSystem
        var traitGo = GameObject.Find("TraitSystem");
        if (traitGo != null)
        {
            cm.TraitSystem = traitGo.GetComponent<TraitSystem>();
            Debug.Log("[AutobattlePatch] Wired TraitSystem.");
        }

        // Wire Grid if missing
        if (cm.Grid == null)
        {
            var gridGo = GameObject.Find("BattleGrid");
            if (gridGo != null) cm.Grid = gridGo.GetComponent<BattleGrid>();
        }

        // Wire VFXManager if missing
        if (cm.VFXManager == null)
        {
            var vfxGo = GameObject.Find("CombatVFXManager");
            if (vfxGo != null) cm.VFXManager = vfxGo.GetComponent<CombatVFXManager>();
        }

        // Set spawn positions (1 of each for demo)
        cm.PlayerSpawns = new[] { new Vector2Int(1, 3) };
        cm.EnemySpawns  = new[] { new Vector2Int(6, 4) };

        EditorUtility.SetDirty(cm);
        Debug.Log("[AutobattlePatch] CombatManager fields patched.");
    }

    // ── 3. Wire CombatHUD ─────────────────────────────────────────────────────
    static void PatchCombatHUD()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) { Debug.LogError("[AutobattlePatch] UICanvas not found."); return; }
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud == null) { Debug.LogError("[AutobattlePatch] CombatHUD not found."); return; }

        // Wire status panels (array-based)
        var heroStatus  = GameObject.Find("UICanvas/HeroStatus");
        var enemyStatus = GameObject.Find("UICanvas/EnemyStatus");
        var playerPanels = new System.Collections.Generic.List<UnitStatusPanel>();
        var enemyPanels  = new System.Collections.Generic.List<UnitStatusPanel>();
        if (heroStatus  != null) { var p = heroStatus.GetComponent<UnitStatusPanel>();  if (p) playerPanels.Add(p); }
        if (enemyStatus != null) { var p = enemyStatus.GetComponent<UnitStatusPanel>(); if (p) enemyPanels.Add(p);  }
        hud.PlayerPanels = playerPanels.ToArray();
        hud.EnemyPanels  = enemyPanels.ToArray();

        // Wire combat log
        var logText = GameObject.Find("UICanvas/CombatLog/LogText");
        if (logText != null) hud.LogText = logText.GetComponent<TextMeshProUGUI>();

        EditorUtility.SetDirty(hud);
        Debug.Log("[AutobattlePatch] CombatHUD wired.");
    }

    // ── 4. Build Initiative Bar ───────────────────────────────────────────────
    static void BuildInitiativeBar()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) return;

        // Don't duplicate
        if (GameObject.Find("UICanvas/InitiativeBar") != null)
        {
            Debug.Log("[AutobattlePatch] InitiativeBar already exists.");
            return;
        }

        // Root bar — bottom of screen, full width, 72px tall
        var barGo = new GameObject("InitiativeBar");
        barGo.transform.SetParent(canvasGo.transform, false);
        var barRect = barGo.AddComponent<RectTransform>();
        barRect.anchorMin        = new Vector2(0f, 0f);
        barRect.anchorMax        = new Vector2(1f, 0f);
        barRect.pivot            = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 0f);
        barRect.sizeDelta        = new Vector2(0f, 80f);

        var barBG = barGo.AddComponent<Image>();
        barBG.color = new Color(0.04f, 0.03f, 0.08f, 0.88f);

        // Horizontal layout group for entries
        var container = new GameObject("EntryContainer");
        container.transform.SetParent(barGo.transform, false);
        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin        = Vector2.zero;
        cRect.anchorMax        = Vector2.one;
        cRect.offsetMin        = new Vector2(12f, 6f);
        cRect.offsetMax        = new Vector2(-12f, -6f);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = 10f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment     = TextAnchor.MiddleLeft;

        // Entry prefab (built as a child template — will be pooled at runtime)
        var entryPrefabGo = BuildEntryPrefab(container.transform);

        // Wire InitiativeBarUI
        var bar = barGo.AddComponent<InitiativeBarUI>();
        bar.Container    = container.transform;
        bar.EntryPrefab  = entryPrefabGo;
        bar.PlayerColor  = new Color(0.25f, 0.70f, 1.00f);
        bar.EnemyColor   = new Color(1.00f, 0.30f, 0.30f);

        // Wire to HUD
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud != null) hud.InitiativeBar = bar;

        // Save as prefab asset for runtime instantiation
        string prefabDir  = "Assets/_Project/Prefabs/UI";
        if (!System.IO.Directory.Exists(prefabDir))
            System.IO.Directory.CreateDirectory(prefabDir);

        // Detach entry from scene — save standalone prefab
        entryPrefabGo.transform.SetParent(null);
        string prefabPath = $"{prefabDir}/InitiativeEntry.prefab";
        var savedPrefab   = PrefabUtility.SaveAsPrefabAsset(entryPrefabGo, prefabPath);
        GameObject.DestroyImmediate(entryPrefabGo);

        if (savedPrefab != null) bar.EntryPrefab = savedPrefab;

        EditorUtility.SetDirty(bar);
        if (hud != null) EditorUtility.SetDirty(hud);
        Debug.Log("[AutobattlePatch] InitiativeBar built.");
    }

    static GameObject BuildEntryPrefab(Transform parent)
    {
        var entry = new GameObject("InitiativeEntry");
        entry.transform.SetParent(parent, false);

        var entryRect = entry.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(68f, 68f);

        // Background
        var bg = entry.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        // Portrait image
        var portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(entry.transform, false);
        var portRect = portraitGo.AddComponent<RectTransform>();
        portRect.anchorMin        = new Vector2(0.1f, 0.3f);
        portRect.anchorMax        = new Vector2(0.9f, 0.95f);
        portRect.offsetMin        = Vector2.zero;
        portRect.offsetMax        = Vector2.zero;
        var portImg = portraitGo.AddComponent<Image>();
        portImg.preserveAspect    = true;

        // CT fill bar (bottom strip)
        var ctBarGo = new GameObject("CTBar");
        ctBarGo.transform.SetParent(entry.transform, false);
        var ctRect = ctBarGo.AddComponent<RectTransform>();
        ctRect.anchorMin   = new Vector2(0f, 0f);
        ctRect.anchorMax   = new Vector2(1f, 0.28f);
        ctRect.offsetMin   = new Vector2(2f, 2f);
        ctRect.offsetMax   = new Vector2(-2f, -1f);
        var ctImg = ctBarGo.AddComponent<Image>();
        ctImg.color        = new Color(0.25f, 0.70f, 1f);
        ctImg.type         = Image.Type.Filled;
        ctImg.fillMethod   = Image.FillMethod.Horizontal;
        ctImg.fillOrigin   = (int)Image.OriginHorizontal.Left;
        ctImg.fillAmount   = 0f;

        // Name label
        var nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(entry.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin   = new Vector2(0f, 0.25f);
        nameRect.anchorMax   = new Vector2(1f, 0.52f);
        nameRect.offsetMin   = new Vector2(2f, 0f);
        nameRect.offsetMax   = new Vector2(-2f, 0f);
        var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
        nameTMP.text         = "Unit";
        nameTMP.fontSize     = 9f;
        nameTMP.color        = Color.white;
        nameTMP.alignment    = TextAlignmentOptions.Center;
        nameTMP.overflowMode = TextOverflowModes.Truncate;

        // Wire InitiativeEntry component
        var entryComp       = entry.AddComponent<InitiativeEntry>();
        entryComp.Portrait  = portImg;
        entryComp.Background = bg;
        entryComp.CTBar     = ctImg;
        entryComp.NameLabel = nameTMP;

        return entry;
    }

    // ── 5. Build Phase Banner ─────────────────────────────────────────────────
    static void BuildBanner()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) return;
        if (GameObject.Find("UICanvas/PhaseBanner") != null)
        {
            WireBannerToHUD(canvasGo);
            return;
        }

        // Outer root (centered)
        var bannerGo = new GameObject("PhaseBanner");
        bannerGo.transform.SetParent(canvasGo.transform, false);
        var bRect = bannerGo.AddComponent<RectTransform>();
        bRect.anchorMin        = new Vector2(0.5f, 0.5f);
        bRect.anchorMax        = new Vector2(0.5f, 0.5f);
        bRect.pivot            = new Vector2(0.5f, 0.5f);
        bRect.anchoredPosition = new Vector2(0f, 60f);
        bRect.sizeDelta        = new Vector2(780f, 90f);

        var bg = bannerGo.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.02f, 0.10f, 0.85f);

        var cg = bannerGo.AddComponent<CanvasGroup>();

        // Text
        var textGo = new GameObject("BannerText");
        textGo.transform.SetParent(bannerGo.transform, false);
        var tRect = textGo.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(20f, 8f);
        tRect.offsetMax = new Vector2(-20f, -8f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Auto-Battle  —  Begin!";
        tmp.fontSize  = 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(0.88f, 0.88f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;

        bannerGo.SetActive(false);

        // Wire to HUD
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud != null)
        {
            hud.BannerRect  = bannerGo.GetComponent<RectTransform>();
            hud.BannerText  = tmp;
            hud.BannerGroup = cg;
            EditorUtility.SetDirty(hud);
        }

        Debug.Log("[AutobattlePatch] Phase banner built.");
    }

    static void WireBannerToHUD(GameObject canvasGo)
    {
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud == null) return;
        var bannerGo = GameObject.Find("UICanvas/PhaseBanner");
        if (bannerGo == null) return;
        hud.BannerRect  = bannerGo.GetComponent<RectTransform>();
        hud.BannerText  = bannerGo.GetComponentInChildren<TextMeshProUGUI>();
        hud.BannerGroup = bannerGo.GetComponent<CanvasGroup>();
        EditorUtility.SetDirty(hud);
    }

    // ── 6. Build Speed Toggle Button ─────────────────────────────────────────
    static void BuildSpeedToggle()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) return;
        if (GameObject.Find("UICanvas/SpeedToggle") != null)
        {
            WireSpeedToggleToHUD(canvasGo);
            return;
        }

        var btnGo = new GameObject("SpeedToggle");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin        = new Vector2(1f, 1f);
        btnRect.anchorMax        = new Vector2(1f, 1f);
        btnRect.pivot            = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-12f, -12f);
        btnRect.sizeDelta        = new Vector2(110f, 40f);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.08f, 0.22f, 0.90f);

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.12f, 0.08f, 0.22f, 0.90f);
        colors.highlightedColor = new Color(0.22f, 0.15f, 0.38f, 0.95f);
        colors.pressedColor     = new Color(0.08f, 0.05f, 0.15f, 1f);
        btn.colors = colors;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var lRect = labelGo.AddComponent<RectTransform>();
        lRect.anchorMin = Vector2.zero;
        lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(6f, 4f);
        lRect.offsetMax = new Vector2(-6f, -4f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = ">  x1";
        tmp.fontSize  = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(0.80f, 0.80f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;

        // Wire to HUD
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud != null)
        {
            hud.SpeedToggleButton = btn;
            hud.SpeedToggleLabel  = tmp;
            EditorUtility.SetDirty(hud);
        }

        Debug.Log("[AutobattlePatch] Speed toggle button built.");
    }

    static void WireSpeedToggleToHUD(GameObject canvasGo)
    {
        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud == null) return;
        var btnGo = GameObject.Find("UICanvas/SpeedToggle");
        if (btnGo == null) return;
        hud.SpeedToggleButton = btnGo.GetComponent<Button>();
        hud.SpeedToggleLabel  = btnGo.GetComponentInChildren<TextMeshProUGUI>();
        EditorUtility.SetDirty(hud);
    }

    // ── 7. Build Screen Flash Panel ───────────────────────────────────────────
    static void BuildFlashPanel()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) return;
        if (GameObject.Find("UICanvas/FlashPanel") != null)
        {
            WireFlashToVFX();
            return;
        }

        var flashGo = new GameObject("FlashPanel");
        flashGo.transform.SetParent(canvasGo.transform, false);

        var fRect = flashGo.AddComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero;
        fRect.anchorMax = Vector2.one;
        fRect.offsetMin = Vector2.zero;
        fRect.offsetMax = Vector2.zero;

        var img = flashGo.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;

        flashGo.SetActive(false);

        // Put flash behind everything else (sibling index 0)
        flashGo.transform.SetAsFirstSibling();

        // Wire to VFXManager
        var vfxGo = GameObject.Find("CombatVFXManager");
        if (vfxGo != null)
        {
            var vfx = vfxGo.GetComponent<CombatVFXManager>();
            if (vfx != null)
            {
                vfx.FlashPanel = img;
                EditorUtility.SetDirty(vfx);
            }
        }

        Debug.Log("[AutobattlePatch] Flash panel built.");
    }

    static void WireFlashToVFX()
    {
        var flashGo = GameObject.Find("UICanvas/FlashPanel");
        if (flashGo == null) return;
        var vfxGo = GameObject.Find("CombatVFXManager");
        if (vfxGo == null) return;
        var vfx = vfxGo.GetComponent<CombatVFXManager>();
        if (vfx != null) { vfx.FlashPanel = flashGo.GetComponent<Image>(); EditorUtility.SetDirty(vfx); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void RemoveIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            GameObject.DestroyImmediate(go);
            Debug.Log($"[AutobattlePatch] Removed deprecated '{name}'.");
        }
    }
}
